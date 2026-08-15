using System.Collections.Concurrent;
using System.Text.Json;
using DentalClinic.API.Domain.Interfaces.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Infrastructure.Services;

public class FirebasePushNotificationService : IFirebasePushNotificationService
{
    private readonly ILogger<FirebasePushNotificationService> _logger;
    private readonly IHostEnvironment _env;
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, DateTimeOffset>> UserTokens = new();
    private static bool _isFirebaseInitialized;
    private static readonly object InitLock = new();
    private const string TokenStorageFile = "device_tokens.json";

    public FirebasePushNotificationService(
        ILogger<FirebasePushNotificationService> logger,
        IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
        EnsureFirebaseInitialized();
        LoadPersistedTokens();
    }

    private void EnsureFirebaseInitialized()
    {
        if (_isFirebaseInitialized) return;

        lock (InitLock)
        {
            if (_isFirebaseInitialized) return;

            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    _isFirebaseInitialized = true;
                    return;
                }

                // 1. Ưu tiên nạp từ biến môi trường (dành cho Render / Docker / CI/CD)
                var envJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON")
                           ?? Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT");

                if (!string.IsNullOrWhiteSpace(envJson))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromJson(envJson)
                    });
                    _isFirebaseInitialized = true;
                    _logger.LogInformation("Firebase Admin SDK initialized successfully from environment variable.");
                    return;
                }

                var candidatePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "firebase-admin.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "firebase-admin.json"),
                    Path.Combine(_env.ContentRootPath, "firebase-admin.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "Presentation", "firebase-admin.json"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "firebase-admin.json"),
                    "c:\\Máy tính\\SEP\\dental-clinic\\apps\\api\\firebase-admin.json"
                };

                string? credentialPath = candidatePaths.FirstOrDefault(File.Exists);

                if (credentialPath != null)
                {
                    using var stream = File.OpenRead(credentialPath);
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromStream(stream)
                    });
                    _isFirebaseInitialized = true;
                    _logger.LogInformation("Firebase Admin SDK initialized successfully from {Path}", credentialPath);
                }
                else
                {
                    _logger.LogWarning("firebase-admin.json not found in any candidate path. Push notifications via FCM will be disabled.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
            }
        }
    }

    public Task RegisterTokenAsync(Guid userId, string token, string? deviceType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return Task.CompletedTask;

        var tokens = UserTokens.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        tokens[token] = DateTimeOffset.UtcNow;
        _logger.LogInformation("Registered device token for user {UserId} ({DeviceType})", userId, deviceType ?? "Unknown");

        PersistTokens();
        return Task.CompletedTask;
    }

    public async Task SendPushNotificationAsync(
        Guid userId,
        string title,
        string body,
        string type,
        string? relatedEntityId,
        CancellationToken ct = default)
    {
        if (!_isFirebaseInitialized || FirebaseApp.DefaultInstance == null) return;

        if (!UserTokens.TryGetValue(userId, out var tokens) || tokens.IsEmpty)
        {
            _logger.LogDebug("No registered FCM tokens found for user {UserId}", userId);
            return;
        }

        var activeTokens = tokens.Keys.ToList();
        var expiredTokens = new List<string>();

        foreach (var token in activeTokens)
        {
            try
            {
                var message = new Message
                {
                    Token = token,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["type"] = type,
                        ["title"] = title,
                        ["body"] = body,
                        ["relatedEntityId"] = relatedEntityId ?? "",
                        ["click_action"] = "FLUTTER_NOTIFICATION_CLICK"
                    },
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            ChannelId = $"dental_clinic_{type.ToLower()}_channel",
                            Sound = "default",
                            Visibility = NotificationVisibility.PUBLIC,
                            Priority = NotificationPriority.HIGH,
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                            DefaultSound = true,
                            DefaultVibrateTimings = true
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
                _logger.LogInformation("Successfully sent FCM push notification {Response} to user {UserId}", response, userId);
            }
            catch (FirebaseMessagingException fEx) when (fEx.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                                                         fEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                _logger.LogWarning("Expired or invalid FCM token for user {UserId}, removing", userId);
                expiredTokens.Add(token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send FCM push notification to token for user {UserId}", userId);
            }
        }

        if (expiredTokens.Count > 0)
        {
            foreach (var expired in expiredTokens)
            {
                tokens.TryRemove(expired, out _);
            }
            PersistTokens();
        }
    }

    public async Task SendPushNotificationToMultipleAsync(
        IEnumerable<Guid> userIds,
        string title,
        string body,
        string type,
        string? relatedEntityId,
        CancellationToken ct = default)
    {
        foreach (var uid in userIds)
        {
            await SendPushNotificationAsync(uid, title, body, type, relatedEntityId, ct);
        }
    }

    private void PersistTokens()
    {
        try
        {
            var data = UserTokens.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.Keys.ToList());
            var json = JsonSerializer.Serialize(data);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), TokenStorageFile);
            File.WriteAllText(filePath, json);
        }
        catch (Exception) {}
    }

    private void LoadPersistedTokens()
    {
        try
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), TokenStorageFile);
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (data == null) return;

            foreach (var (userIdStr, tokenList) in data)
            {
                if (Guid.TryParse(userIdStr, out var userId))
                {
                    var dict = UserTokens.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
                    foreach (var t in tokenList)
                    {
                        dict[t] = DateTimeOffset.UtcNow;
                    }
                }
            }
        }
        catch (Exception) {}
    }
}

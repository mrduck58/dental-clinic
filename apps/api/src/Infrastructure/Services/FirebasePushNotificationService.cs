using System.Collections.Concurrent;
using System.Text.Json;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Infrastructure.Services;

public class FirebasePushNotificationService : IFirebasePushNotificationService
{
    private readonly ILogger<FirebasePushNotificationService> _logger;
    private readonly IHostEnvironment _env;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, DateTimeOffset>> UserTokens = new();
    private static bool _isFirebaseInitialized;
    private static readonly object InitLock = new();

    public FirebasePushNotificationService(
        ILogger<FirebasePushNotificationService> logger,
        IHostEnvironment env,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _env = env;
        _scopeFactory = scopeFactory;
        EnsureFirebaseInitialized();
        _ = PreloadTokensFromDatabaseAsync();
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

    private async Task PreloadTokensFromDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var allTokens = await db.UserDeviceTokens.AsNoTracking().ToListAsync();
            foreach (var dt in allTokens)
            {
                var dict = UserTokens.GetOrAdd(dt.UserId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
                dict[dt.Token] = dt.UpdatedAt;
            }
            _logger.LogInformation("Preloaded {Count} FCM device tokens from PostgreSQL", allTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preload device tokens from PostgreSQL on startup");
        }
    }

    public async Task RegisterTokenAsync(Guid userId, string token, string? deviceType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var tokens = UserTokens.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        tokens[token] = DateTimeOffset.UtcNow;
        _logger.LogInformation("Registered device token for user {UserId} ({DeviceType})", userId, deviceType ?? "Unknown");

        // Lưu vĩnh viễn vào PostgreSQL để không bị mất khi Render restart
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = await db.UserDeviceTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token, ct);

            if (existing != null)
            {
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                existing.DeviceType = deviceType;
                db.UserDeviceTokens.Update(existing);
            }
            else
            {
                db.UserDeviceTokens.Add(new UserDeviceToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = token,
                    DeviceType = deviceType,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist device token to PostgreSQL for user {UserId}", userId);
        }
    }

    public async Task SendPushNotificationAsync(
        Guid userId,
        string title,
        string body,
        string type,
        string? relatedEntityId,
        CancellationToken ct = default)
    {
        EnsureFirebaseInitialized();
        if (!_isFirebaseInitialized || FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("Firebase Admin SDK is not initialized. Skipping push notification.");
            return;
        }

        // Lấy danh sách token của user từ bộ nhớ cache hoặc từ Database
        List<string> activeTokens;
        if (UserTokens.TryGetValue(userId, out var tokens) && !tokens.IsEmpty)
        {
            activeTokens = tokens.Keys.ToList();
        }
        else
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                activeTokens = await db.UserDeviceTokens
                    .Where(t => t.UserId == userId)
                    .Select(t => t.Token)
                    .ToListAsync(ct);

                if (activeTokens.Count > 0)
                {
                    var userDict = UserTokens.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
                    foreach (var t in activeTokens) userDict[t] = DateTimeOffset.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load device tokens from DB for user {UserId}", userId);
                activeTokens = new List<string>();
            }
        }

        if (activeTokens.Count == 0)
        {
            _logger.LogWarning("No registered FCM tokens found in memory or database for user {UserId}", userId);
            return;
        }

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
                            ChannelId = "dental_clinic_high_importance_channel",
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
            if (UserTokens.TryGetValue(userId, out var userDict))
            {
                foreach (var expired in expiredTokens) userDict.TryRemove(expired, out _);
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var toDelete = await db.UserDeviceTokens
                    .Where(t => t.UserId == userId && expiredTokens.Contains(t.Token))
                    .ToListAsync(ct);
                if (toDelete.Count > 0)
                {
                    db.UserDeviceTokens.RemoveRange(toDelete);
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove expired tokens from DB for user {UserId}", userId);
            }
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
}

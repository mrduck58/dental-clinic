using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Infrastructure.Tests;

[TestFixture]
public class FirebasePushNotificationTests
{
    [Test]
    public async Task SendTestPushNotification()
    {
        var jsonPath = @"c:\Máy tính\SEP\dental-clinic\apps\api\firebase-admin.json";
        if (FirebaseApp.DefaultInstance == null)
        {
            using var stream = File.OpenRead(jsonPath);
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromStream(stream)
            });
        }

        var message = new Message
        {
            Token = "e1HbBr_kSG-2DHMXpPcLQa:APA91bEw0tNT8cuU6ZMCd6yx2GQMUFtLV3wpnATyWmimDTr3pJsQ7AN7cDwdZTjJpeidu_4BJwUggKVh9nlKURvMRKDGD0KbPaILBOJ7wYRWV98wo9z_dJI",
            Notification = new Notification
            {
                Title = "Nha Khoa Sơn Giang",
                Body = "🔔 Thông báo khi đã tắt app: Lịch hẹn của bạn đã được phòng khám xác nhận thành công!"
            },
            Data = new Dictionary<string, string>
            {
                ["type"] = "appointment",
                ["title"] = "Nha Khoa Sơn Giang",
                ["body"] = "Test thông báo đẩy",
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
                    DefaultSound = true,
                    DefaultVibrateTimings = true
                }
            }
        };

        var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        TestContext.Progress.WriteLine($"Firebase Send Response: {response}");
        Assert.That(response, Is.Not.Null);
    }

    [Test]
    public async Task CheckDatabaseUserDeviceTokens()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DentalClinic.API.Infrastructure.Persistence.AppDbContext>();
        optionsBuilder.UseNpgsql("Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.iyuwmzlolzsdqcucgufr;Password=Huan0508@2004;SslMode=Require;TrustServerCertificate=true;Maximum Pool Size=3;Minimum Pool Size=0;Connection Idle Lifetime=15;");
        using var db = new DentalClinic.API.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options);

        var tokens = await db.UserDeviceTokens.ToListAsync();
        TestContext.Progress.WriteLine($"Total UserDeviceTokens in DB: {tokens.Count}");
        foreach (var t in tokens)
        {
            TestContext.Progress.WriteLine($"UserId: {t.UserId}, Device: {t.DeviceType}, Token: {t.Token.Substring(0, Math.Min(25, t.Token.Length))}..., UpdatedAt: {t.UpdatedAt}");
        }

        var latestAppt = await db.Appointments
            .Include(a => a.Patient)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
        if (latestAppt != null)
        {
            TestContext.Progress.WriteLine($"Latest Appointment: Id={latestAppt.Id}, Status={latestAppt.Status}, PatientId={latestAppt.PatientId}, PatientUserId={latestAppt.Patient?.UserId}");
        }
    }
}

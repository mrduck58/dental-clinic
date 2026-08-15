using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
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
}

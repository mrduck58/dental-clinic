import 'dart:async';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:mobile_app/core/services/local_notification_helper.dart';
import 'package:mobile_app/features/home/data/notification_service.dart';

@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  try {
    await Firebase.initializeApp();
  } catch (_) {}
}

class FirebaseMessagingService {
  static final FirebaseMessagingService instance = FirebaseMessagingService._internal();
  FirebaseMessagingService._internal();

  final _notificationService = NotificationService();
  bool _isInitialized = false;

  Future<void> init() async {
    if (kIsWeb) return;
    if (_isInitialized) return;

    try {
      await Firebase.initializeApp();
      FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

      // Tạo kênh thông báo Android Notification Channel độ ưu tiên cao nhất
      final localNotifications = FlutterLocalNotificationsPlugin();
      await localNotifications
          .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
          ?.createNotificationChannel(
            const AndroidNotificationChannel(
              'dental_clinic_high_importance_channel',
              'Thông báo Nha Khoa Sơn Giang',
              description: 'Kênh nhận thông báo lịch hẹn, tái khám và thanh toán',
              importance: Importance.max,
              playSound: true,
              enableVibration: true,
            ),
          );

      // Yêu cầu quyền thông báo từ người dùng
      final settings = await FirebaseMessaging.instance.requestPermission(
        alert: true,
        announcement: false,
        badge: true,
        carPlay: false,
        criticalAlert: false,
        provisional: false,
        sound: true,
      );

      debugPrint('FirebaseMessaging permission status: ${settings.authorizationStatus}');

      // Lấy FCM token và gửi lên Backend API
      await syncTokenWithBackend();

      // Lắng nghe khi token đổi mới
      FirebaseMessaging.instance.onTokenRefresh.listen((newToken) async {
        await _notificationService.registerDeviceToken(newToken);
      });

      // 1. Khi người dùng click vào thông báo lúc app đã bị TẮT HẲN (Terminated)
      final initialMessage = await FirebaseMessaging.instance.getInitialMessage();
      if (initialMessage != null) {
        Future.delayed(const Duration(milliseconds: 600), () {
          _handleRemoteMessage(initialMessage);
        });
      }

      // 2. Khi người dùng click vào thông báo lúc app đang chạy nền (Background/Minimized)
      FirebaseMessaging.onMessageOpenedApp.listen((message) {
        _handleRemoteMessage(message);
      });

      // 3. Khi nhận thông báo lúc app đang mở (Foreground)
      FirebaseMessaging.onMessage.listen((RemoteMessage message) {
        final title = message.notification?.title ?? message.data['title'] ?? 'Thông báo mới';
        final body = message.notification?.body ?? message.data['body'] ?? '';
        final type = message.data['type'] ?? 'appointment';
        final payload = message.data['relatedEntityId'];

        LocalNotificationHelper.instance.showNotification(
          title: title,
          body: body,
          type: type,
          payload: payload,
        );
      });

      _isInitialized = true;
    } catch (e) {
      debugPrint('FirebaseMessagingService init failed: $e');
    }
  }

  Future<void> syncTokenWithBackend() async {
    try {
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null && token.isNotEmpty) {
        debugPrint('FCM Device Token: $token');
        await _notificationService.registerDeviceToken(token);
      }
    } catch (e) {
      debugPrint('Failed to get/sync FCM token: $e');
    }
  }

  void _handleRemoteMessage(RemoteMessage message) {
    final title = message.notification?.title ?? message.data['title'];
    final body = message.notification?.body ?? message.data['body'];
    final type = message.data['type'] ?? 'appointment';
    final payload = message.data['relatedEntityId'];

    LocalNotificationHelper.handleNotificationNavigation(
      type: type,
      title: title,
      body: body,
      payload: payload,
    );
  }
}

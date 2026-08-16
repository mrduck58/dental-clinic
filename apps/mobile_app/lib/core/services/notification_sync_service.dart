import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:mobile_app/core/services/local_notification_helper.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/notification_service.dart';
import 'package:shared_preferences/shared_preferences.dart';

class NotificationSyncService with WidgetsBindingObserver {
  static final NotificationSyncService instance = NotificationSyncService._internal();
  NotificationSyncService._internal();

  final _notificationService = NotificationService();
  final _auth = AuthService();

  Future<void> init() async {
    // FCM là nguồn phát thông báo đẩy duy nhất (FirebaseMessagingService),
    // không chạy polling đè lên FCM để tránh trùng lặp thông báo.
  }

  void start() {}

  void stop() {}

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {}

  Future<void> checkForNewNotifications() async {}
}

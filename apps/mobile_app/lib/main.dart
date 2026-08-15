import 'package:flutter/material.dart';
import 'app/app.dart';
import 'app/settings_manager.dart';
import 'core/services/firebase_messaging_service.dart';
import 'core/services/local_notification_helper.dart';
import 'core/services/notification_sync_service.dart';

void main() async {
  // Đảm bảo Flutter binding được khởi tạo trước khi gọi các native plugin
  WidgetsFlutterBinding.ensureInitialized();

  await SettingsManager.instance.init();
  await LocalNotificationHelper.instance.init();
  await NotificationSyncService.instance.init();
  await FirebaseMessagingService.instance.init();

  runApp(const DentalClinicApp());
}

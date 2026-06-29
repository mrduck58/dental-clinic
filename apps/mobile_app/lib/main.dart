import 'package:flutter/material.dart';
import 'app/app.dart';
import 'app/settings_manager.dart';

void main() async {
  // Đảm bảo Flutter binding được khởi tạo trước khi gọi các native plugin
  WidgetsFlutterBinding.ensureInitialized();

  await SettingsManager.instance.init();

  runApp(const DentalClinicApp());
}

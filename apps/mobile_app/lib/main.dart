import 'package:flutter/material.dart';
import 'app/app.dart';

void main() {
  // Đảm bảo Flutter binding được khởi tạo trước khi gọi các native plugin
  WidgetsFlutterBinding.ensureInitialized();

  runApp(const DentalClinicApp());
}

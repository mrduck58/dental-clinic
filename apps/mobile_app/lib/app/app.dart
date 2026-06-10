import 'package:flutter/material.dart';
import 'routers.dart';

/// Widget gốc của toàn bộ ứng dụng Dental Clinic.
/// Khai báo theme, router và các provider ở đây.
class DentalClinicApp extends StatelessWidget {
  const DentalClinicApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Dental Clinic',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF1E6FD9), // Màu xanh nha khoa
          brightness: Brightness.light,
        ),
        useMaterial3: true,
        fontFamily: 'Inter',
      ),
      // TODO: Thay bằng GoRouter khi tích hợp đầy đủ các màn hình
      routerConfig: appRouter,
    );
  }
}

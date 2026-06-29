import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import 'routers.dart';

class DentalClinicApp extends StatelessWidget {
  const DentalClinicApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Dental Clinic',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
      routerConfig: appRouter,
    );
  }

  ThemeData _buildTheme() {
    final base = ThemeData(
      colorScheme: ColorScheme.fromSeed(
        seedColor: const Color(0xFFDC2626),
        brightness: Brightness.light,
      ),
      useMaterial3: true,
    );
    return base.copyWith(
      textTheme: GoogleFonts.nunitoTextTheme(base.textTheme),
      appBarTheme: const AppBarTheme(
        systemOverlayStyle: SystemUiOverlayStyle(
          statusBarColor: Colors.transparent,
          statusBarIconBrightness: Brightness.light,
        ),
      ),
    );
  }
}

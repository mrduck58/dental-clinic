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
      // Tăng cỡ chữ toàn app 10% — áp dụng kể cả text hardcode style
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(
          textScaler: const TextScaler.linear(1.10),
        ),
        child: child!,
      ),
    );
  }

  ThemeData _buildTheme() {
    final base = ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFFDC2626)),
      useMaterial3: true,
    );
    final nunito = GoogleFonts.nunitoTextTheme(base.textTheme);
    return base.copyWith(
      textTheme: _boldify(nunito),
      appBarTheme: const AppBarTheme(
        systemOverlayStyle: SystemUiOverlayStyle(
          statusBarColor: Colors.transparent,
          statusBarIconBrightness: Brightness.light,
        ),
      ),
    );
  }

  // Tăng độ đậm mỗi style lên 1 bậc (w400→w500, w500→w600, ...)
  TextTheme _boldify(TextTheme t) {
    TextStyle up(TextStyle? s) =>
        (s ?? const TextStyle()).copyWith(fontWeight: _heavier(s?.fontWeight));
    return t.copyWith(
      displayLarge: up(t.displayLarge),
      displayMedium: up(t.displayMedium),
      displaySmall: up(t.displaySmall),
      headlineLarge: up(t.headlineLarge),
      headlineMedium: up(t.headlineMedium),
      headlineSmall: up(t.headlineSmall),
      titleLarge: up(t.titleLarge),
      titleMedium: up(t.titleMedium),
      titleSmall: up(t.titleSmall),
      bodyLarge: up(t.bodyLarge),
      bodyMedium: up(t.bodyMedium),
      bodySmall: up(t.bodySmall),
      labelLarge: up(t.labelLarge),
      labelMedium: up(t.labelMedium),
      labelSmall: up(t.labelSmall),
    );
  }

  FontWeight _heavier(FontWeight? w) {
    const all = FontWeight.values; // w100…w900
    final i = all.indexOf(w ?? FontWeight.w400);
    return i < all.length - 1 ? all[i + 1] : all.last;
  }
}

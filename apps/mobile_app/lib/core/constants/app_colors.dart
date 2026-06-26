import 'package:flutter/material.dart';

/// Bảng màu chính của ứng dụng — đồng nhất với theme web (Tailwind CSS).
abstract class AppColors {
  // Primary — Red 600/700/100
  static const primary = Color(0xFFDC2626);
  static const primaryDark = Color(0xFFB91C1C);
  static const primaryLight = Color(0xFFFEE2E2);

  // Secondary — Sky 600/100
  static const secondary = Color(0xFF0284C7);
  static const secondaryLight = Color(0xFFE0F2FE);

  // Accent — Amber 500/100
  static const accent = Color(0xFFF59E0B);
  static const accentLight = Color(0xFFFEF3C7);

  // Success — Green 600/100
  static const success = Color(0xFF16A34A);
  static const successLight = Color(0xFFDCFCE7);

  // Neutrals — Slate scale
  static const background = Color(0xFFF8FAFC); // Slate 50
  static const surface = Color(0xFFFFFFFF);
  static const textPrimary = Color(0xFF0F172A); // Slate 900
  static const textSecondary = Color(0xFF475569); // Slate 600
  static const textMuted = Color(0xFF94A3B8); // Slate 400
  static const divider = Color(0xFFE2E8F0); // Slate 200
}

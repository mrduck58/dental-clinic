import 'package:flutter/material.dart';
import 'package:mobile_app/app/settings_manager.dart';

/// Bảng màu chính của ứng dụng — đồng nhất với theme web (Tailwind CSS).
abstract class AppColors {
  static bool get _isDark => SettingsManager.instance.isDarkMode.value;

  // Primary — Red 600/700/100
  static Color get primary => const Color(0xFFDC2626);
  static Color get primaryDark => const Color(0xFFB91C1C);
  static Color get primaryLight => _isDark ? const Color(0xFF450A0A) : const Color(0xFFFEE2E2);

  // Secondary — Sky 600/100
  static Color get secondary => const Color(0xFF0284C7);
  static Color get secondaryLight => _isDark ? const Color(0xFF082F49) : const Color(0xFFE0F2FE);

  // Accent — Amber 500/100
  static Color get accent => const Color(0xFFF59E0B);
  static Color get accentLight => _isDark ? const Color(0xFF451A03) : const Color(0xFFFEF3C7);

  // Success — Green 600/100
  static Color get success => const Color(0xFF16A34A);
  static Color get successLight => _isDark ? const Color(0xFF064E3B) : const Color(0xFFDCFCE7);

  // Neutrals — Slate scale
  static Color get background => _isDark ? const Color(0xFF0F172A) : const Color(0xFFF8FAFC); // Slate 900 vs Slate 50
  static Color get surface => _isDark ? const Color(0xFF1E293B) : const Color(0xFFFFFFFF); // Slate 800 vs White
  static Color get textPrimary => _isDark ? const Color(0xFFF8FAFC) : const Color(0xFF0F172A); // Slate 50 vs Slate 900
  static Color get textSecondary => _isDark ? const Color(0xFF94A3B8) : const Color(0xFF475569); // Slate 400 vs Slate 600
  static Color get textMuted => _isDark ? const Color(0xFF64748B) : const Color(0xFF94A3B8); // Slate 500 vs Slate 400
  static Color get divider => _isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0); // Slate 700 vs Slate 200
}

import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';

/// Unified modern Toast / SnackBar for the Dental Clinic Mobile App.
class AppToast {
  AppToast._();

  /// Hiển thị thông báo thành công (Màu xanh lá emerald sang trọng)
  static void showSuccess(
    BuildContext context,
    String message, {
    Duration duration = const Duration(seconds: 3),
  }) {
    _show(
      context,
      message: message,
      icon: Iconsax.tick_circle,
      iconColor: const Color(0xFF34D399),
      backgroundColor: const Color(0xFF064E3B),
      borderColor: const Color(0xFF059669),
      duration: duration,
    );
  }

  /// Hiển thị thông báo lỗi (Màu đỏ rose thanh lịch)
  static void showError(
    BuildContext context,
    String message, {
    Duration duration = const Duration(seconds: 4),
  }) {
    _show(
      context,
      message: message,
      icon: Iconsax.warning_2,
      iconColor: const Color(0xFFF87171),
      backgroundColor: const Color(0xFF450A0A),
      borderColor: const Color(0xFFDC2626),
      duration: duration,
    );
  }

  /// Hiển thị thông báo cảnh báo / giới hạn (Màu vàng hổ phách amber)
  static void showWarning(
    BuildContext context,
    String message, {
    Duration duration = const Duration(seconds: 4),
  }) {
    _show(
      context,
      message: message,
      icon: Iconsax.info_circle,
      iconColor: const Color(0xFFFBBF24),
      backgroundColor: const Color(0xFF451A03),
      borderColor: const Color(0xFFD97706),
      duration: duration,
    );
  }

  /// Hiển thị thông báo thông tin (Màu xanh dương primary)
  static void showInfo(
    BuildContext context,
    String message, {
    Duration duration = const Duration(seconds: 3),
  }) {
    _show(
      context,
      message: message,
      icon: Iconsax.info_circle,
      iconColor: const Color(0xFF60A5FA),
      backgroundColor: const Color(0xFF172554),
      borderColor: const Color(0xFF2563EB),
      duration: duration,
    );
  }

  static void _show(
    BuildContext context, {
    required String message,
    required IconData icon,
    required Color iconColor,
    required Color backgroundColor,
    required Color borderColor,
    required Duration duration,
  }) {
    final messenger = ScaffoldMessenger.maybeOf(context);
    if (messenger == null) return;

    messenger.hideCurrentSnackBar();
    messenger.showSnackBar(
      SnackBar(
        behavior: SnackBarBehavior.floating,
        margin: const EdgeInsets.fromLTRB(16, 0, 16, 16),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        elevation: 6,
        backgroundColor: backgroundColor,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: BorderSide(color: borderColor, width: 1),
        ),
        duration: duration,
        content: Row(
          children: [
            Icon(icon, color: iconColor, size: 20),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                message,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  height: 1.35,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

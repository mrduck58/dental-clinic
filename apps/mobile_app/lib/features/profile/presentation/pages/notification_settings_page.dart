import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/services/local_notification_helper.dart';
import 'package:permission_handler/permission_handler.dart';

class NotificationSettingsPage extends StatefulWidget {
  const NotificationSettingsPage({super.key});

  @override
  State<NotificationSettingsPage> createState() => _NotificationSettingsPageState();
}

class _NotificationSettingsPageState extends State<NotificationSettingsPage>
    with WidgetsBindingObserver {
  final _settings = SettingsManager.instance;

  bool _isSystemPermissionGranted = true;
  bool _isCheckingPermission = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _checkSystemPermission();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Khi người dùng quay lại từ Cài đặt hệ thống (Android App Settings),
    // tự động kiểm tra lại trạng thái quyền để cập nhật giao diện tức thì.
    if (state == AppLifecycleState.resumed) {
      _checkSystemPermission();
    }
  }

  Future<void> _checkSystemPermission() async {
    if (kIsWeb) return;
    if (!Platform.isAndroid && !Platform.isIOS) return;

    try {
      final status = await Permission.notification.status;
      if (mounted) {
        setState(() {
          _isSystemPermissionGranted = status.isGranted || status.isLimited;
        });
      }
    } catch (_) {
      // Bỏ qua lỗi kiểm tra quyền trên các nền tảng không hỗ trợ
    }
  }

  Future<void> _requestOrOpenSettings() async {
    if (kIsWeb) return;
    setState(() => _isCheckingPermission = true);
    try {
      final status = await Permission.notification.status;

      if (status.isDenied) {
        final requestResult = await Permission.notification.request();
        if (requestResult.isGranted) {
          if (mounted) {
            setState(() => _isSystemPermissionGranted = true);
            await _settings.setPushNotificationsEnabled(true);
            _showSnackBar(
              _settings.locale.value.languageCode == 'vi'
                  ? 'Đã cấp quyền nhận thông báo thành công!'
                  : 'Notification permission granted successfully!',
              isSuccess: true,
            );
          }
          return;
        }
      }

      // Nếu đã từ chối trước đó hoặc bị chặn vĩnh viễn (permanently denied):
      // Mở trực tiếp trang Cài đặt ứng dụng của hệ điều hành Android/iOS.
      final opened = await openAppSettings();
      if (!opened && mounted) {
        _showSnackBar(
          _settings.locale.value.languageCode == 'vi'
              ? 'Vui lòng mở Cài đặt thiết bị > Ứng dụng > Nha Khoa Sơn Giang > Thông báo để bật quyền.'
              : 'Please open Device Settings > Apps > Dental Clinic > Notifications to allow.',
        );
      }
    } catch (_) {
      await openAppSettings();
    } finally {
      if (mounted) setState(() => _isCheckingPermission = false);
    }
  }

  Future<void> _onMasterPushChanged(bool value) async {
    if (value) {
      // Khi người dùng bật thông báo đẩy:
      if (!kIsWeb && (Platform.isAndroid || Platform.isIOS)) {
        final status = await Permission.notification.status;
        if (!status.isGranted && !status.isLimited) {
          final req = await Permission.notification.request();
          if (!req.isGranted && !req.isLimited) {
            // Hiển thị hộp thoại hướng dẫn chuyển sang cài đặt hệ thống
            if (mounted) {
              _showPermissionRequiredDialog();
            }
            return;
          }
          if (mounted) setState(() => _isSystemPermissionGranted = true);
        }
      }
      await _settings.setPushNotificationsEnabled(true);
    } else {
      await _settings.setPushNotificationsEnabled(false);
    }
    if (mounted) setState(() {});
  }

  void _showPermissionRequiredDialog() {
    final isVi = _settings.locale.value.languageCode == 'vi';
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: context.card,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.amber.withValues(alpha: 0.15),
                shape: BoxShape.circle,
              ),
              child: const Icon(Iconsax.notification_1, color: Colors.amber, size: 22),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                isVi ? 'Cần quyền thông báo' : 'Permission Required',
                style: TextStyle(
                  color: context.textPrimary,
                  fontWeight: FontWeight.w800,
                  fontSize: 17,
                ),
              ),
            ),
          ],
        ),
        content: Text(
          isVi
              ? 'Ứng dụng cần quyền gửi thông báo để nhắc lịch khám, thông báo hóa đơn và cập nhật điều trị. Bạn có muốn chuyển sang Cài đặt hệ thống để cấp quyền ngay không?'
              : 'The app requires notification permission for appointment reminders and invoice updates. Would you like to open system settings to grant permission?',
          style: TextStyle(color: context.textSecondary, fontSize: 14, height: 1.45),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(
              context.l10n('cancel'),
              style: TextStyle(color: context.textMuted, fontWeight: FontWeight.w600),
            ),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(ctx);
              openAppSettings();
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              elevation: 0,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
            child: Text(
              context.l10n('open_settings_btn'),
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }

  void _showSnackBar(String message, {bool isSuccess = false}) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            Icon(
              isSuccess ? Icons.check_circle_rounded : Icons.info_outline_rounded,
              color: Colors.white,
              size: 20,
            ),
            const SizedBox(width: 10),
            Expanded(child: Text(message)),
          ],
        ),
        backgroundColor: isSuccess ? const Color(0xFF059669) : const Color(0xFF334155),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        duration: const Duration(seconds: 3),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Iconsax.arrow_left, color: context.textPrimary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          context.l10n('notification_settings_title'),
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w900,
            color: context.textPrimary,
          ),
        ),
      ),
      body: ValueListenableBuilder<bool>(
        valueListenable: _settings.pushNotificationsEnabled,
        builder: (context, pushEnabled, _) {
          return SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // ── 1. Thẻ Cảnh báo quyền hệ thống Android (nếu chưa bật ở OS) ──
                if (!_isSystemPermissionGranted && !kIsWeb) ...[
                  _buildSystemPermissionWarningCard(),
                  const SizedBox(height: 16),
                ],

                // ── 2. Master Push Notification Toggle Card ─────────────────
                _buildMasterPushCard(pushEnabled),
                const SizedBox(height: 24),

                // ── 3. Nhóm: CHẾ ĐỘ LỜI NHẮC ─────────────────────────────────
                _buildSectionHeader(
                  context.l10n('reminder_mode_section'),
                  icon: Iconsax.direct_notification,
                ),
                _buildReminderModesCard(pushEnabled),
                const SizedBox(height: 24),

                // ── 4. Nhóm: CÁC LOẠI THÔNG BÁO ──────────────────────────────
                _buildSectionHeader(
                  context.l10n('notification_types_section'),
                  icon: Iconsax.category,
                ),
                _buildNotificationCategoriesCard(pushEnabled),
                const SizedBox(height: 24),

                // ── 5. Test Notification Action Button ───────────────────────
                _buildTestNotificationCard(pushEnabled),
                const SizedBox(height: 16),

                // ── 6. System Settings Shortcut Footer ───────────────────────
                _buildSystemSettingsFooter(),
                const SizedBox(height: 60),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildSectionHeader(String title, {required IconData icon}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10, left: 4),
      child: Row(
        children: [
          Icon(icon, size: 16, color: AppColors.primary),
          const SizedBox(width: 6),
          Text(
            title,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w900,
              color: context.textSecondary,
              letterSpacing: 0.8,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSystemPermissionWarningCard() {
    final isVi = _settings.locale.value.languageCode == 'vi';
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFFFEF3C7),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFF59E0B), width: 1.2),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFFF59E0B).withValues(alpha: 0.12),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: const BoxDecoration(
                  color: Color(0xFFF59E0B),
                  shape: BoxShape.circle,
                ),
                child: const Icon(Iconsax.warning_2, color: Colors.white, size: 16),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  context.l10n('system_permission_title'),
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    fontSize: 15,
                    color: Color(0xFF92400E),
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: const Color(0xFFFDE68A),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  context.l10n('permission_denied_tag'),
                  style: const TextStyle(
                    color: Color(0xFFB45309),
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            context.l10n('system_permission_disabled_msg'),
            style: const TextStyle(
              fontSize: 13,
              color: Color(0xFF78350F),
              height: 1.4,
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            height: 40,
            child: ElevatedButton.icon(
              onPressed: _isCheckingPermission ? null : _requestOrOpenSettings,
              icon: _isCheckingPermission
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                    )
                  : const Icon(Iconsax.setting_2, size: 16),
              label: Text(
                isVi ? 'Cấp quyền thông báo ngay' : 'Grant Permission Now',
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFFD97706),
                foregroundColor: Colors.white,
                elevation: 0,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMasterPushCard(bool pushEnabled) {
    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(
          color: pushEnabled ? AppColors.primaryLight : context.divider,
          width: 1.5,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: context.isDark ? 0.2 : 0.04),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      padding: const EdgeInsets.all(18),
      child: Row(
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: pushEnabled
                    ? [const Color(0xFFDC2626), const Color(0xFFB91C1C)]
                    : [Colors.grey.shade400, Colors.grey.shade500],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              shape: BoxShape.circle,
            ),
            child: const Icon(Iconsax.notification_bing, color: Colors.white, size: 24),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  context.l10n('push_notifications'),
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    color: context.textPrimary,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  context.l10n('push_notifications_desc'),
                  style: TextStyle(
                    fontSize: 12.5,
                    color: context.textSecondary,
                    height: 1.35,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Switch(
            value: pushEnabled,
            activeThumbColor: AppColors.primary,
            activeTrackColor: AppColors.primary.withValues(alpha: 0.35),
            onChanged: _onMasterPushChanged,
          ),
        ],
      ),
    );
  }

  Widget _buildReminderModesCard(bool isEnabled) {
    return Opacity(
      opacity: isEnabled ? 1.0 : 0.45,
      child: Container(
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: context.divider, width: 1.2),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.03),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          children: [
            // Màn hình khóa
            ValueListenableBuilder<bool>(
              valueListenable: _settings.reminderLockScreen,
              builder: (context, val, _) {
                return _buildToggleTile(
                  icon: Iconsax.lock,
                  iconBgColor: const Color(0xFF6366F1),
                  title: context.l10n('lock_screen_reminder'),
                  subtitle: context.l10n('lock_screen_reminder_desc'),
                  value: val,
                  isEnabled: isEnabled,
                  onChanged: (newVal) => _settings.setReminderLockScreen(newVal),
                );
              },
            ),
            Divider(height: 1, color: context.divider.withValues(alpha: 0.7)),
            // Cửa sổ bật lên
            ValueListenableBuilder<bool>(
              valueListenable: _settings.reminderPopup,
              builder: (context, val, _) {
                return _buildToggleTile(
                  icon: Iconsax.message_notif,
                  iconBgColor: const Color(0xFF06B6D4),
                  title: context.l10n('popup_reminder'),
                  subtitle: context.l10n('popup_reminder_desc'),
                  value: val,
                  isEnabled: isEnabled,
                  onChanged: (newVal) => _settings.setReminderPopup(newVal),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildNotificationCategoriesCard(bool isEnabled) {
    return Opacity(
      opacity: isEnabled ? 1.0 : 0.45,
      child: Container(
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: context.divider, width: 1.2),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.03),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          children: [
            // 1. Thông báo đặt lịch
            ValueListenableBuilder<bool>(
              valueListenable: _settings.notifyBooking,
              builder: (context, val, _) {
                return _buildToggleTile(
                  icon: Iconsax.calendar_tick,
                  iconBgColor: const Color(0xFF0284C7),
                  title: context.l10n('notify_booking_title'),
                  subtitle: context.l10n('notify_booking_desc'),
                  value: val,
                  isEnabled: isEnabled,
                  onChanged: (newVal) => _settings.setNotifyBooking(newVal),
                );
              },
            ),
            Divider(height: 1, color: context.divider.withValues(alpha: 0.7)),

            // 2. Thông báo thanh toán hóa đơn
            ValueListenableBuilder<bool>(
              valueListenable: _settings.notifyPayment,
              builder: (context, val, _) {
                return _buildToggleTile(
                  icon: Iconsax.receipt_1,
                  iconBgColor: const Color(0xFF059669),
                  title: context.l10n('notify_payment_title'),
                  subtitle: context.l10n('notify_payment_desc'),
                  value: val,
                  isEnabled: isEnabled,
                  onChanged: (newVal) => _settings.setNotifyPayment(newVal),
                );
              },
            ),
            Divider(height: 1, color: context.divider.withValues(alpha: 0.7)),

            // 3. Thông báo nhắc nhở
            ValueListenableBuilder<bool>(
              valueListenable: _settings.notifyReminder,
              builder: (context, val, _) {
                return _buildToggleTile(
                  icon: Iconsax.clock,
                  iconBgColor: const Color(0xFFD97706),
                  title: context.l10n('notify_reminder_title'),
                  subtitle: context.l10n('notify_reminder_desc'),
                  value: val,
                  isEnabled: isEnabled,
                  onChanged: (newVal) => _settings.setNotifyReminder(newVal),
                );
              },
            ),
            Divider(height: 1, color: context.divider.withValues(alpha: 0.7)),

            // 4. Thông báo tái khám
            ValueListenableBuilder<bool>(
              valueListenable: _settings.notifyFollowup,
              builder: (context, val, _) {
                return _buildToggleTile(
                  icon: Iconsax.health,
                  iconBgColor: const Color(0xFF7C3AED),
                  title: context.l10n('notify_followup_title'),
                  subtitle: context.l10n('notify_followup_desc'),
                  value: val,
                  isEnabled: isEnabled,
                  onChanged: (newVal) => _settings.setNotifyFollowup(newVal),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildToggleTile({
    required IconData icon,
    required Color iconBgColor,
    required String title,
    required String subtitle,
    required bool value,
    required bool isEnabled,
    required ValueChanged<bool> onChanged,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              color: iconBgColor.withValues(alpha: 0.12),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: iconBgColor, size: 20),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    fontSize: 14.5,
                    fontWeight: FontWeight.w700,
                    color: context.textPrimary,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  subtitle,
                  style: TextStyle(
                    fontSize: 12,
                    color: context.textSecondary,
                    height: 1.35,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Switch(
            value: isEnabled ? value : false,
            activeThumbColor: AppColors.primary,
            activeTrackColor: AppColors.primary.withValues(alpha: 0.35),
            onChanged: isEnabled ? onChanged : null,
          ),
        ],
      ),
    );
  }

  Widget _buildTestNotificationCard(bool pushEnabled) {
    final isVi = _settings.locale.value.languageCode == 'vi';
    return SizedBox(
      width: double.infinity,
      height: 48,
      child: OutlinedButton.icon(
        onPressed: pushEnabled
            ? () async {
                if (!_isSystemPermissionGranted && !kIsWeb) {
                  _showPermissionRequiredDialog();
                  return;
                }
                await LocalNotificationHelper.instance.showNotification(
                  title: isVi ? 'Nha Khoa Sơn Giang 🔔' : 'Dental Clinic 🔔',
                  body: isVi
                      ? 'Thông báo thử nghiệm! Cài đặt thông báo đang hoạt động hoàn hảo.'
                      : 'Test notification! Your notification settings are working perfectly.',
                  type: 'booking',
                );
                _showSnackBar(
                  isVi
                      ? 'Đã gửi thông báo thử nghiệm! Hãy kiểm tra thanh thông báo & màn hình khóa.'
                      : 'Test notification sent! Check notification shade & lock screen.',
                  isSuccess: true,
                );
              }
            : null,
        icon: const Icon(Iconsax.notification_status, size: 18),
        label: Text(
          isVi ? 'Gửi thử nghiệm 1 thông báo đẩy' : 'Send a Test Notification',
          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13.5),
        ),
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          side: BorderSide(
            color: pushEnabled ? AppColors.primary : context.divider,
            width: 1.5,
          ),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        ),
      ),
    );
  }

  Widget _buildSystemSettingsFooter() {
    final isVi = _settings.locale.value.languageCode == 'vi';
    return InkWell(
      onTap: openAppSettings,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: context.divider, width: 1),
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                shape: BoxShape.circle,
              ),
              child: Icon(Iconsax.setting_4, size: 18, color: context.textSecondary),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    context.l10n('open_settings_btn'),
                    style: TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w700,
                      color: context.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    isVi
                        ? 'Tùy chỉnh quyền, âm thanh & rung trong Cài đặt của máy'
                        : 'Manage sound, vibration and OS permissions in system settings',
                    style: TextStyle(
                      fontSize: 11.5,
                      color: context.textMuted,
                    ),
                  ),
                ],
              ),
            ),
            Icon(
              Icons.arrow_forward_ios_rounded,
              size: 14,
              color: context.textMuted.withValues(alpha: 0.7),
            ),
          ],
        ),
      ),
    );
  }
}

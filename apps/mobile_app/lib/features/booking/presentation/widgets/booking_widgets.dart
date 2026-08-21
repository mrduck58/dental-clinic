import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';

// ─── AppBar ───────────────────────────────────────────────────────────────────

class BookingAppBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;
  final VoidCallback? onBack;
  final bool showHome;
  final bool showBack;
  final VoidCallback? onHome;

  const BookingAppBar({
    super.key,
    required this.title,
    this.onBack,
    this.showHome = true,
    this.showBack = true,
    this.onHome,
  });

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  @override
  Widget build(BuildContext context) {
    final titleColor = context.isDark ? Colors.white : AppColors.primary;
    final iconColor = context.isDark ? Colors.white : AppColors.primary;

    return AppBar(
      backgroundColor: context.card,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      scrolledUnderElevation: 0,
      automaticallyImplyLeading: false,
      leading: showBack
          ? GestureDetector(
              onTap: onBack ?? () => Navigator.of(context).pop(),
              child: Icon(Iconsax.arrow_left, color: iconColor, size: 28),
            )
          : null,
      title: Text(
        title,
        style: TextStyle(
          fontSize: 22,
          fontWeight: FontWeight.w800,
          color: titleColor,
        ),
      ),
      centerTitle: true,
      actions: showHome
          ? [
              GestureDetector(
                onTap: () {
                  BookingService().clearActiveDraft();
                  if (onHome != null) {
                    onHome!();
                  } else {
                    context.go(AppRoutes.home);
                  }
                },
                child: Padding(
                  padding: const EdgeInsets.only(right: 16),
                  child: Icon(Iconsax.home_2, color: iconColor, size: 28),
                ),
              ),
            ]
          : null,
      bottom: PreferredSize(
        preferredSize: const Size.fromHeight(1),
        child: Container(height: 1, color: context.divider),
      ),
    );
  }
}

// ─── Bottom Button Bar ────────────────────────────────────────────────────────

class BookingBottomBar extends StatelessWidget {
  final String label;
  final VoidCallback? onTap;
  final bool isLoading;
  final Widget? leading;

  const BookingBottomBar({
    super.key,
    required this.label,
    this.onTap,
    this.isLoading = false,
    this.leading,
  });

  @override
  Widget build(BuildContext context) {
    final bottomPad = MediaQuery.of(context).padding.bottom;
    return Container(
      padding: EdgeInsets.fromLTRB(16, 12, 16, 12 + bottomPad),
      decoration: BoxDecoration(
        color: context.card,
        border: Border(top: BorderSide(color: context.divider)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.06),
            blurRadius: 12,
            offset: Offset(0, -4),
          ),
        ],
      ),
      child: leading != null
          ? Row(
              children: [
                leading!,
                SizedBox(width: 10),
                Expanded(child: _Button(label: label, onTap: onTap, isLoading: isLoading)),
              ],
            )
          : _Button(label: label, onTap: onTap, isLoading: isLoading),
    );
  }
}

class _Button extends StatelessWidget {
  final String label;
  final VoidCallback? onTap;
  final bool isLoading;
  const _Button({required this.label, this.onTap, this.isLoading = false});

  @override
  Widget build(BuildContext context) {
    final inactiveBg = context.isDark ? const Color(0xFF334155) : AppColors.divider;

    return SizedBox(
      height: 52,
      child: ElevatedButton(
        onPressed: isLoading ? null : onTap,
        style: ElevatedButton.styleFrom(
          backgroundColor: onTap != null ? AppColors.primary : inactiveBg,
          foregroundColor: Colors.white,
          disabledBackgroundColor: inactiveBg,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
        child: isLoading
            ? SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
              )
            : Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    label,
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                  ),
                  if (onTap != null) ...[
                    SizedBox(width: 6),
                    Icon(Iconsax.arrow_right, size: 18),
                  ],
                ],
              ),
      ),
    );
  }
}

// ─── Section Divider with label ───────────────────────────────────────────────

class BookingSectionLabel extends StatelessWidget {
  final String text;
  const BookingSectionLabel({super.key, required this.text});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      color: AppColors.background,
      child: Text(
        text,
        style: TextStyle(
          fontSize: 13,
          fontWeight: FontWeight.w600,
          color: context.textSecondary,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}

// ─── Hold Countdown Banner ───────────────────────────────────────────────────

class HoldCountdownBanner extends StatefulWidget {
  final DateTime? holdExpiresAt;
  final VoidCallback? onExpired;
  final EdgeInsetsGeometry margin;

  const HoldCountdownBanner({
    super.key,
    required this.holdExpiresAt,
    this.onExpired,
    this.margin = const EdgeInsets.fromLTRB(16, 8, 16, 8),
  });

  @override
  State<HoldCountdownBanner> createState() => _HoldCountdownBannerState();
}

class _HoldCountdownBannerState extends State<HoldCountdownBanner> {
  int _remainingSeconds = 0;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _initTimer();
  }

  @override
  void didUpdateWidget(covariant HoldCountdownBanner oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.holdExpiresAt != widget.holdExpiresAt) {
      _initTimer();
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  void _initTimer() {
    _timer?.cancel();
    _timer = null;

    if (widget.holdExpiresAt == null) {
      if (mounted) setState(() => _remainingSeconds = 0);
      return;
    }

    final diff = widget.holdExpiresAt!.difference(DateTime.now()).inSeconds;
    final initialRemaining = diff > 0 ? diff : 0;
    if (mounted) {
      setState(() => _remainingSeconds = initialRemaining);
    }

    if (initialRemaining <= 0) {
      if (mounted) setState(() => _remainingSeconds = 0);
      return;
    }

    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) {
        timer.cancel();
        return;
      }
      if (widget.holdExpiresAt == null) {
        timer.cancel();
        setState(() => _remainingSeconds = 0);
        return;
      }
      final curDiff = widget.holdExpiresAt!.difference(DateTime.now()).inSeconds;
      final newRemaining = curDiff > 0 ? curDiff : 0;

      if (newRemaining != _remainingSeconds) {
        setState(() => _remainingSeconds = newRemaining);
      }

      if (newRemaining <= 0) {
        timer.cancel();
        _timer = null;
        widget.onExpired?.call();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_remainingSeconds <= 0 || widget.holdExpiresAt == null) {
      return const SizedBox.shrink();
    }

    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final minutes = _remainingSeconds ~/ 60;
    final seconds = _remainingSeconds % 60;
    final timeStr = '${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
    final isUrgent = _remainingSeconds <= 60;

    return Container(
      margin: widget.margin,
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: isUrgent
            ? (context.isDark ? const Color(0xFF3B1515) : const Color(0xFFFEF2F2))
            : (context.isDark ? const Color(0xFF38290D) : const Color(0xFFFFFBEB)),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: isUrgent
              ? (context.isDark ? const Color(0xFF7F1D1D) : const Color(0xFFFCA5A5))
              : (context.isDark ? const Color(0xFF78350F) : const Color(0xFFFDE68A)),
        ),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(6),
            decoration: BoxDecoration(
              color: isUrgent
                  ? (context.isDark ? const Color(0xFF5A1A1A) : const Color(0xFFFEE2E2))
                  : (context.isDark ? const Color(0xFF451A03) : const Color(0xFFFEF3C7)),
              shape: BoxShape.circle,
            ),
            child: Icon(
              Iconsax.timer_1,
              size: 18,
              color: isUrgent ? const Color(0xFFEF4444) : const Color(0xFFD97706),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              isVi
                  ? 'Thời gian giữ chỗ còn lại:'
                  : 'Hold time remaining:',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: isUrgent
                    ? (context.isDark ? const Color(0xFFFCA5A5) : const Color(0xFF991B1B))
                    : (context.isDark ? const Color(0xFFFDE68A) : const Color(0xFF92400E)),
              ),
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(
              color: isUrgent ? const Color(0xFFDC2626) : const Color(0xFFD97706),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Text(
              timeStr,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w800,
                color: Colors.white,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

// ─── AppBar ───────────────────────────────────────────────────────────────────

class BookingAppBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;
  final VoidCallback? onBack;
  final bool showHome;

  const BookingAppBar({
    super.key,
    required this.title,
    this.onBack,
    this.showHome = true,
  });

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  @override
  Widget build(BuildContext context) {
    return AppBar(
      backgroundColor: AppColors.surface,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      scrolledUnderElevation: 0,
      leading: GestureDetector(
        onTap: onBack ?? () => Navigator.of(context).pop(),
        child: Icon(Iconsax.arrow_left, color: AppColors.primary, size: 28, fontWeight: FontWeight.w800),
      ),
      title: Text(
        title,
        style: TextStyle(
          fontSize: 22,
          fontWeight: FontWeight.w800,
          color: AppColors.primary,
        ),
      ),
      centerTitle: true,
      actions: showHome
          ? [
              GestureDetector(
                onTap: () => context.go(AppRoutes.home),
                child: Padding(
                  padding: EdgeInsets.only(right: 16),
                  child: Icon(Iconsax.home_2, color: AppColors.primary, size: 28, fontWeight: FontWeight.w800),
                ),
              ),
            ]
          : null,
      bottom: PreferredSize(
        preferredSize: const Size.fromHeight(1),
        child: Container(height: 1, color: AppColors.divider),
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
        color: AppColors.surface,
        border: Border(top: BorderSide(color: AppColors.divider)),
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
    return SizedBox(
      height: 52,
      child: ElevatedButton(
        onPressed: isLoading ? null : onTap,
        style: ElevatedButton.styleFrom(
          backgroundColor: onTap != null ? AppColors.primary : AppColors.divider,
          foregroundColor: Colors.white,
          disabledBackgroundColor: AppColors.divider,
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
          color: AppColors.textMuted,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}

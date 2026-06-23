import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class MainShell extends StatelessWidget {
  final Widget child;
  final String location;

  const MainShell({super.key, required this.child, required this.location});

  int get _currentIndex {
    if (location.startsWith('/appointments')) return 1;
    if (location.startsWith('/medical-records')) return 2;
    if (location.startsWith('/profile')) return 3;
    return 0;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      extendBody: true,
      body: child,
      bottomNavigationBar: _BottomNavBar(
        currentIndex: _currentIndex,
        onTap: (i) {
          switch (i) {
            case 0:
              context.go(AppRoutes.home);
            case 1:
              context.go(AppRoutes.appointments);
            case 2:
              context.go(AppRoutes.medicalRecords);
            case 3:
              context.go(AppRoutes.profile);
          }
        },
        onFabTap: () => context.push(AppRoutes.bookingSelectPatient),
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Custom Bottom Navigation Bar
// ─────────────────────────────────────────────────
class _BottomNavBar extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;
  final VoidCallback onFabTap;

  const _BottomNavBar({
    required this.currentIndex,
    required this.onTap,
    required this.onFabTap,
  });

  static const _items = [
    _NavItem(icon: Iconsax.home_2, activeIcon: Iconsax.home_25, label: 'Trang chủ'),
    _NavItem(icon: Iconsax.calendar_2, activeIcon: Iconsax.calendar_25, label: 'Lịch hẹn'),
    _NavItem(icon: Iconsax.document, activeIcon: Iconsax.document_text, label: 'Hồ sơ'),
    _NavItem(icon: Iconsax.user, activeIcon: Iconsax.user_octagon, label: 'Cá nhân'),
  ];

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).padding.bottom;
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.10),
            blurRadius: 24,
            offset: const Offset(0, -6),
          ),
        ],
        borderRadius: const BorderRadius.only(
          topLeft: Radius.circular(28),
          topRight: Radius.circular(28),
        ),
      ),
      child: ClipRRect(
        borderRadius: const BorderRadius.only(
          topLeft: Radius.circular(28),
          topRight: Radius.circular(28),
        ),
        child: Padding(
          padding: EdgeInsets.only(top: 14, bottom: bottomInset + 14),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _NavButton(item: _items[0], isActive: currentIndex == 0, onTap: () => onTap(0)),
              _NavButton(item: _items[1], isActive: currentIndex == 1, onTap: () => onTap(1)),
              // Nút + ngang hàng với các icon
              _CenterFab(onTap: onFabTap),
              _NavButton(item: _items[2], isActive: currentIndex == 2, onTap: () => onTap(2)),
              _NavButton(item: _items[3], isActive: currentIndex == 3, onTap: () => onTap(3)),
            ],
          ),
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Center FAB — trắng, shadow đỏ
// ─────────────────────────────────────────────────
class _CenterFab extends StatelessWidget {
  final VoidCallback onTap;
  const _CenterFab({required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 54,
        height: 54,
        decoration: BoxDecoration(
          color: Colors.white,
          shape: BoxShape.circle,
          border: Border.all(color: AppColors.primary.withValues(alpha: 0.15), width: 1.5),
          boxShadow: [
            BoxShadow(
              color: AppColors.primary.withValues(alpha: 0.28),
              blurRadius: 16,
              offset: const Offset(0, 5),
            ),
            BoxShadow(
              color: AppColors.primary.withValues(alpha: 0.10),
              blurRadius: 28,
              offset: const Offset(0, 10),
            ),
          ],
        ),
        child: const Icon(Icons.add_rounded, color: AppColors.primary, size: 30),
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Nav Button
// ─────────────────────────────────────────────────
class _NavButton extends StatelessWidget {
  final _NavItem item;
  final bool isActive;
  final VoidCallback onTap;

  const _NavButton({required this.item, required this.isActive, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      behavior: HitTestBehavior.opaque,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 240),
        curve: Curves.easeOut,
        padding: const EdgeInsets.symmetric(horizontal: 30, vertical: 13),
        decoration: BoxDecoration(
          color: isActive ? AppColors.primary : Colors.transparent,
          borderRadius: BorderRadius.circular(999),
          boxShadow: isActive
              ? [
                  BoxShadow(
                    color: AppColors.primary.withValues(alpha: 0.32),
                    blurRadius: 14,
                    offset: const Offset(0, 4),
                  ),
                ]
              : null,
        ),
        child: Icon(
          isActive ? item.activeIcon : item.icon,
          color: isActive ? Colors.white : AppColors.textMuted,
          size: 28,
        ),
      ),
    );
  }
}

class _NavItem {
  final IconData icon;
  final IconData activeIcon;
  final String label;
  const _NavItem({required this.icon, required this.activeIcon, required this.label});
}

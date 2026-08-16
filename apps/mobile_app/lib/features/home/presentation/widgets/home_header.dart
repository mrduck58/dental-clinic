import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/notification_service.dart';

class HomeHeader extends StatelessWidget {
  final String userName;
  final String? avatarUrl;

  const HomeHeader({super.key, required this.userName, this.avatarUrl});

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 14, 20, 8),
        child: Row(
          children: [
            _Avatar(name: userName, avatarUrl: avatarUrl),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    context.l10n('hello'),
                    style: TextStyle(color: context.textSecondary, fontSize: 15),
                  ),
                  Text(
                    userName.isNotEmpty ? userName : context.l10n('user'),
                    style: TextStyle(
                      color: context.textPrimary,
                      fontSize: 20,
                      fontWeight: FontWeight.w900,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
            const _NotificationBell(),
          ],
        ),
      ),
    );
  }
}

class _Avatar extends StatelessWidget {
  final String name;
  final String? avatarUrl;

  const _Avatar({required this.name, this.avatarUrl});

  String _initials() {
    final words = name.trim().split(' ').where((w) => w.isNotEmpty).toList();
    if (words.isEmpty) return 'U';
    if (words.length == 1) return words[0][0].toUpperCase();
    return '${words.first[0]}${words.last[0]}'.toUpperCase();
  }

  ImageProvider? _getAvatarProvider() {
    final resolved = ApiConstants.resolveAssetUrl(avatarUrl);
    return resolved == null ? null : NetworkImage(resolved);
  }

  @override
  Widget build(BuildContext context) {
    final provider = _getAvatarProvider();
    return Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        gradient: provider == null
            ? const LinearGradient(
                colors: [Color(0xFFDC2626), Color(0xFFB91C1C)],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              )
            : null,
        image: provider != null
            ? DecorationImage(
                image: provider,
                fit: BoxFit.cover,
              )
            : null,
        shape: BoxShape.circle,
      ),
      child: provider == null
          ? Center(
              child: Text(
                _initials(),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 16,
                  fontWeight: FontWeight.w900,
                ),
              ),
            )
          : null,
    );
  }
}

class _NotificationBell extends StatefulWidget {
  const _NotificationBell();

  @override
  State<_NotificationBell> createState() => _NotificationBellState();
}

class _NotificationBellState extends State<_NotificationBell> {
  int _unreadCount = 0;

  @override
  void initState() {
    super.initState();
    _loadUnreadCount();
  }

  Future<void> _loadUnreadCount() async {
    try {
      final result = await NotificationService().getNotifications(pageSize: 1);
      if (mounted) setState(() => _unreadCount = result.unreadCount);
    } catch (_) {
      // Không tải được số lượng chưa đọc — bỏ qua lặng lẽ, không chặn trang chủ hiển thị.
    }
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      // Quay lại trang chủ từ trang thông báo sẽ tạo lại widget này (route mới trong go_router
      // navigation stack), nên số lượng chưa đọc tự làm mới mà không cần cơ chế polling riêng.
      onTap: () async {
        await context.push(AppRoutes.notifications);
        _loadUnreadCount();
      },
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: context.card,
              shape: BoxShape.circle,
              border: Border.all(color: context.divider, width: 1.5),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: context.isDark ? 0.2 : 0.05),
                  blurRadius: 8,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: Icon(Iconsax.notification, size: 24, color: context.textPrimary),
          ),
          if (_unreadCount > 0)
            Positioned(
              top: -2,
              right: -2,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 4),
                constraints: const BoxConstraints(minWidth: 18, minHeight: 18),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(9),
                  border: Border.all(color: context.card, width: 1.5),
                ),
                alignment: Alignment.center,
                child: Text(
                  _unreadCount > 9 ? '9+' : '$_unreadCount',
                  style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w800),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

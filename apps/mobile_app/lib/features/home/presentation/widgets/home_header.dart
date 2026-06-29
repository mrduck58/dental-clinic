import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';

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
    if (avatarUrl != null && avatarUrl!.isNotEmpty) {
      if (avatarUrl!.startsWith('http')) {
        return NetworkImage(avatarUrl!);
      }
      final baseUrlHost = ApiConstants.baseUrl.replaceAll('/api', '');
      return NetworkImage('$baseUrlHost$avatarUrl');
    }
    return null;
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

class _NotificationBell extends StatelessWidget {
  const _NotificationBell();

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => context.push(AppRoutes.notifications),
      child: Container(
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
    );
  }
}

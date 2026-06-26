import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class NotificationItem {
  final String id;
  final String titleKey;
  final String messageVi;
  final String messageEn;
  final DateTime timestamp;
  bool isRead;
  final IconData icon;
  final Color iconBg;

  NotificationItem({
    required this.id,
    required this.titleKey,
    required this.messageVi,
    required this.messageEn,
    required this.timestamp,
    this.isRead = false,
    required this.icon,
    required this.iconBg,
  });
}

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});

  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage> {
  final List<NotificationItem> _notifications = [
    NotificationItem(
      id: '1',
      titleKey: 'upcoming_appointment',
      messageVi: 'Lịch hẹn khám răng định kỳ của bạn với BS. Sarah Williams vào lúc 09:00 ngày mai.',
      messageEn: 'Your routine checkup with Dr. Sarah Williams is scheduled at 09:00 tomorrow.',
      timestamp: DateTime.now().subtract(const Duration(hours: 2)),
      isRead: false,
      icon: Iconsax.calendar_tick,
      iconBg: const Color(0xFFFEE2E2),
    ),
    NotificationItem(
      id: '2',
      titleKey: 'special_offer',
      messageVi: 'Ưu đãi 20% các dịch vụ thẩm mỹ răng sứ nhân dịp khai trương chi nhánh mới.',
      messageEn: 'Get 20% off cosmetic porcelain teeth services for our new branch grand opening.',
      timestamp: DateTime.now().subtract(const Duration(days: 1)),
      isRead: true,
      icon: Iconsax.discount_shape,
      iconBg: const Color(0xFFFEF3C7),
    ),
    NotificationItem(
      id: '3',
      titleKey: 'rescheduled_appointment',
      messageVi: 'Lịch hẹn khám ngày 28/06/2026 của bạn đã được dời sang 14:00 theo yêu cầu.',
      messageEn: 'Your appointment on 06/28/2026 has been rescheduled to 14:00 as requested.',
      timestamp: DateTime.now().subtract(const Duration(days: 2)),
      isRead: true,
      icon: Iconsax.clock,
      iconBg: const Color(0xFFE0F2FE),
    ),
    NotificationItem(
      id: '4',
      titleKey: 'new_tech_title',
      messageVi: 'Phòng khám đã đưa công nghệ Dental AI mới nhất vào phân tích và chẩn đoán cấu trúc hàm.',
      messageEn: 'We have integrated the latest Dental AI technology for jaw structure analysis.',
      timestamp: DateTime.now().subtract(const Duration(days: 3)),
      isRead: true,
      icon: Iconsax.cpu,
      iconBg: const Color(0xFFDCFCE7),
    ),
  ];

  String _formatTime(DateTime dt, String lang) {
    final diff = DateTime.now().difference(dt);
    if (diff.inMinutes < 60) {
      return lang == 'vi' ? '${diff.inMinutes} phút trước' : '${diff.inMinutes}m ago';
    } else if (diff.inHours < 24) {
      return lang == 'vi' ? '${diff.inHours} giờ trước' : '${diff.inHours}h ago';
    } else {
      return lang == 'vi' ? '${diff.inDays} ngày trước' : '${diff.inDays}d ago';
    }
  }

  void _markAllRead() {
    setState(() {
      for (var item in _notifications) {
        item.isRead = true;
      }
    });
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(context.l10n('mark_all_read')),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  void _deleteAll() {
    setState(() {
      _notifications.clear();
    });
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        backgroundColor: context.bg,
        appBar: AppBar(
          backgroundColor: context.card,
          elevation: 0.5,
          leading: IconButton(
            icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
            onPressed: () => context.pop(),
          ),
          title: Text(
            context.l10n('notifications_title'),
            style: TextStyle(
              color: context.textPrimary,
              fontWeight: FontWeight.w900,
              fontSize: 20,
            ),
          ),
          actions: [
            if (_notifications.isNotEmpty)
              PopupMenuButton<String>(
                icon: Icon(Icons.more_vert_rounded, color: context.textPrimary),
                color: context.card,
                surfaceTintColor: Colors.transparent,
                onSelected: (val) {
                  if (val == 'read_all') {
                    _markAllRead();
                  } else if (val == 'delete_all') {
                    _deleteAll();
                  }
                },
                itemBuilder: (context) => [
                  PopupMenuItem(
                    value: 'read_all',
                    child: Text(
                      context.l10n('mark_all_read'),
                      style: TextStyle(color: context.textPrimary),
                    ),
                  ),
                  PopupMenuItem(
                    value: 'delete_all',
                    child: Text(
                      context.l10n('delete_all_notifications'),
                      style: TextStyle(color: AppColors.primary),
                    ),
                  ),
                ],
              ),
          ],
          bottom: TabBar(
            labelColor: AppColors.primary,
            unselectedLabelColor: context.textSecondary,
            indicatorColor: AppColors.primary,
            tabs: [
              Tab(text: context.l10n('all')),
              Tab(text: '${context.l10n('unread')} (${_notifications.where((n) => !n.isRead).length})'),
            ],
          ),
        ),
        body: TabBarView(
          children: [
            _buildList(context, _notifications, isVi),
            _buildList(context, _notifications.where((n) => !n.isRead).toList(), isVi),
          ],
        ),
      ),
    );
  }

  Widget _buildList(BuildContext context, List<NotificationItem> items, bool isVi) {
    if (items.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 40.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 90,
                height: 90,
                decoration: BoxDecoration(
                  color: context.isDark ? Colors.grey[800] : const Color(0xFFF1F5F9),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  Iconsax.notification_status,
                  size: 40,
                  color: context.textMuted,
                ),
              ),
              const SizedBox(height: 24),
              Text(
                context.l10n('notifications_empty'),
                style: TextStyle(
                  color: context.textPrimary,
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                context.l10n('no_notifications_desc'),
                style: TextStyle(
                  color: context.textSecondary,
                  fontSize: 14,
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    }

    return ListView.separated(
      itemCount: items.length,
      padding: const EdgeInsets.all(20),
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (context, index) {
        final item = items[index];
        return Container(
          decoration: BoxDecoration(
            color: item.isRead ? context.card.withValues(alpha: 0.7) : context.card,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: !item.isRead
                  ? AppColors.primary.withValues(alpha: 0.15)
                  : (context.isDark ? context.divider.withValues(alpha: 0.5) : Colors.transparent),
              width: 1.5,
            ),
            boxShadow: [
              if (!item.isRead)
                BoxShadow(
                  color: AppColors.primary.withValues(alpha: 0.03),
                  blurRadius: 10,
                  offset: const Offset(0, 4),
                )
            ],
          ),
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              onTap: () {
                setState(() {
                  item.isRead = true;
                });
              },
              borderRadius: BorderRadius.circular(16),
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Icon
                    Container(
                      width: 48,
                      height: 48,
                      decoration: BoxDecoration(
                        color: context.isDark ? Colors.red[900]?.withValues(alpha: 0.3) : item.iconBg,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        item.icon,
                        color: AppColors.primary,
                        size: 22,
                      ),
                    ),
                    const SizedBox(width: 14),
                    // Content
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                context.l10n(item.titleKey),
                                style: TextStyle(
                                  color: context.textPrimary,
                                  fontWeight: item.isRead ? FontWeight.w500 : FontWeight.w800,
                                  fontSize: 15,
                                ),
                              ),
                              if (!item.isRead)
                                Container(
                                  width: 8,
                                  height: 8,
                                  decoration: const BoxDecoration(
                                    color: AppColors.primary,
                                    shape: BoxShape.circle,
                                  ),
                                ),
                            ],
                          ),
                          const SizedBox(height: 6),
                          Text(
                            isVi ? item.messageVi : item.messageEn,
                            style: TextStyle(
                              color: item.isRead ? context.textSecondary : context.textPrimary.withValues(alpha: 0.9),
                              fontSize: 13,
                              height: 1.4,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            _formatTime(item.timestamp, isVi ? 'vi' : 'en'),
                            style: TextStyle(
                              color: context.textMuted,
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}

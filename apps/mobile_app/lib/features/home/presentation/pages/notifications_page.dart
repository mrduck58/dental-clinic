import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class NotificationItem {
  final String id;
  final String titleVi;
  final String titleEn;
  final String messageVi;
  final String messageEn;
  final DateTime timestamp;
  bool isRead;
  final IconData icon;
  final Color iconBg;
  final bool isReminder;

  NotificationItem({
    required this.id,
    required this.titleVi,
    required this.titleEn,
    required this.messageVi,
    required this.messageEn,
    required this.timestamp,
    this.isRead = false,
    required this.icon,
    required this.iconBg,
    this.isReminder = false,
  });
}

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});

  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage> {
  int _selectedTab = 0; // 0=All, 1=Unread, 2=Reminders

  final List<NotificationItem> _notifications = [
    NotificationItem(
      id: '1',
      titleVi: 'Lịch khám sắp tới',
      titleEn: 'Upcoming Appointment',
      messageVi: 'Lịch hẹn khám răng định kỳ với BS. Sarah Williams vào lúc 09:00 ngày mai.',
      messageEn: 'Your routine checkup with Dr. Sarah Williams is scheduled at 09:00 tomorrow.',
      timestamp: DateTime.now().subtract(const Duration(hours: 2)),
      isRead: false,
      icon: Iconsax.calendar_tick,
      iconBg: const Color(0xFFFEE2E2),
      isReminder: true,
    ),
    NotificationItem(
      id: '2',
      titleVi: 'Giảm giá Đặc biệt',
      titleEn: 'Special Discount',
      messageVi: 'Ưu đãi 20% các dịch vụ thẩm mỹ răng sứ nhân dịp khai trương chi nhánh mới.',
      messageEn: 'Get 20% off cosmetic porcelain teeth services for our new branch grand opening.',
      timestamp: DateTime.now().subtract(const Duration(days: 1)),
      isRead: true,
      icon: Iconsax.discount_shape,
      iconBg: const Color(0xFFFEF3C7),
    ),
    NotificationItem(
      id: '3',
      titleVi: 'Thay đổi Lịch hẹn',
      titleEn: 'Schedule Changes',
      messageVi: 'Lịch hẹn khám ngày 28/06/2026 đã được dời sang 14:00 theo yêu cầu.',
      messageEn: 'Your appointment on 06/28/2026 has been rescheduled to 14:00 as requested.',
      timestamp: DateTime.now().subtract(const Duration(days: 2)),
      isRead: false,
      icon: Iconsax.clock,
      iconBg: const Color(0xFFE0F2FE),
      isReminder: true,
    ),
    NotificationItem(
      id: '4',
      titleVi: 'Công nghệ Nha khoa mới',
      titleEn: 'New Dental Technology',
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

  void _showOptionsBottomSheet(BuildContext ctx) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    showModalBottomSheet(
      context: ctx,
      backgroundColor: context.card,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: context.divider,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              const SizedBox(height: 16),
              ListTile(
                leading: Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: Colors.green.withValues(alpha: 0.12),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.done_all_rounded, color: Colors.green, size: 20),
                ),
                title: Text(
                  isVi ? 'Đánh dấu đọc hết' : 'Mark all as read',
                  style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w600),
                ),
                onTap: () {
                  Navigator.pop(ctx);
                  _markAllRead();
                },
              ),
              ListTile(
                leading: Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.12),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(Icons.delete_outline_rounded, color: AppColors.primary, size: 20),
                ),
                title: Text(
                  isVi ? 'Xóa tất cả' : 'Delete all',
                  style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w600),
                ),
                onTap: () {
                  Navigator.pop(ctx);
                  _deleteAll();
                },
              ),
              const SizedBox(height: 4),
            ],
          ),
        ),
      ),
    );
  }

  List<NotificationItem> get _filteredList {
    switch (_selectedTab) {
      case 1:
        return _notifications.where((n) => !n.isRead).toList();
      case 2:
        return _notifications.where((n) => n.isReminder).toList();
      default:
        return _notifications;
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final tabs = isVi
        ? ['Tất cả', 'Chưa đọc', 'Nhắc nhở']
        : ['All', 'Unread', 'Reminders'];
    final unreadCount = _notifications.where((n) => !n.isRead).length;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        scrolledUnderElevation: 0,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          context.l10n('notifications_title'),
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w900, fontSize: 20),
        ),
        actions: [
          if (_notifications.isNotEmpty)
            IconButton(
              icon: Icon(Icons.more_vert_rounded, color: context.textPrimary),
              onPressed: () => _showOptionsBottomSheet(context),
            ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(56),
          child: Container(
            color: context.card,
            child: Column(
              children: [
                // 3 Pill chip tabs
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
                  child: Row(
                    children: List.generate(3, (i) {
                      final isActive = _selectedTab == i;
                      String label = tabs[i];
                      if (i == 1 && unreadCount > 0) label += ' ($unreadCount)';
                      return Padding(
                        padding: EdgeInsets.only(right: i < 2 ? 8 : 0),
                        child: GestureDetector(
                          onTap: () => setState(() => _selectedTab = i),
                          child: AnimatedContainer(
                            duration: const Duration(milliseconds: 200),
                            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                            decoration: BoxDecoration(
                              color: isActive ? AppColors.primary : context.bg,
                              borderRadius: BorderRadius.circular(30),
                              border: Border.all(
                                color: isActive ? AppColors.primary : context.divider,
                                width: 1.5,
                              ),
                            ),
                            child: Text(
                              label,
                              style: TextStyle(
                                color: isActive ? Colors.white : context.textSecondary,
                                fontWeight: FontWeight.w700,
                                fontSize: 13,
                              ),
                            ),
                          ),
                        ),
                      );
                    }),
                  ),
                ),
                Divider(height: 1, thickness: 1, color: context.divider),
              ],
            ),
          ),
        ),
      ),
      body: _buildList(context, _filteredList, isVi),
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
                child: Icon(Iconsax.notification_status, size: 40, color: context.textMuted),
              ),
              const SizedBox(height: 24),
              Text(
                context.l10n('notifications_empty'),
                style: TextStyle(color: context.textPrimary, fontSize: 18, fontWeight: FontWeight.w800),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                context.l10n('no_notifications_desc'),
                style: TextStyle(color: context.textSecondary, fontSize: 14),
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
        // Use uniform border + ClipRRect for left accent to avoid Flutter constraint
        return ClipRRect(
          borderRadius: BorderRadius.circular(16),
          child: Container(
            decoration: BoxDecoration(
              color: item.isRead ? context.card.withValues(alpha: 0.7) : context.card,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: !item.isRead
                    ? AppColors.primary.withValues(alpha: 0.2)
                    : context.divider.withValues(alpha: 0.5),
                width: 1.5,
              ),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                  blurRadius: 8,
                  offset: const Offset(0, 3),
                ),
              ],
            ),
            child: Row(
              children: [
                // Left accent bar for unread
                if (!item.isRead)
                  Container(width: 4, color: AppColors.primary),
                Expanded(
                  child: Material(
                    color: Colors.transparent,
                    child: InkWell(
                      onTap: () => setState(() => item.isRead = true),
                      child: Padding(
                        padding: const EdgeInsets.all(16.0),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Container(
                              width: 48,
                              height: 48,
                              decoration: BoxDecoration(
                                color: context.isDark ? Colors.red[900]?.withValues(alpha: 0.3) : item.iconBg,
                                shape: BoxShape.circle,
                              ),
                              child: Icon(item.icon, color: AppColors.primary, size: 22),
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: [
                                      Expanded(
                                        child: Text(
                                          isVi ? item.titleVi : item.titleEn,
                                          style: TextStyle(
                                            color: context.textPrimary,
                                            fontWeight: item.isRead ? FontWeight.w500 : FontWeight.w800,
                                            fontSize: 15,
                                          ),
                                        ),
                                      ),
                                      if (!item.isRead)
                                        Container(
                                          width: 8,
                                          height: 8,
                                          margin: const EdgeInsets.only(left: 8),
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
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

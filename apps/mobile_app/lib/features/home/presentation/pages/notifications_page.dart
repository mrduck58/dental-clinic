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
  final String category; // 'appointment', 'offer', 'reminder'

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
    required this.category,
  });
}

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});

  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage> {
  int _selectedTab = 0; // 0: All, 1: Unread, 2: Reminders

  final List<NotificationItem> _notifications = [
    NotificationItem(
      id: '1',
      titleVi: 'Lịch khám sắp tới',
      titleEn: 'Upcoming Appointment',
      messageVi: 'Lịch khám răng định kỳ của bạn được lên lịch hôm nay lúc 10:30 AM',
      messageEn: 'Your dental checkup is scheduled for today at 10:30 AM',
      timestamp: DateTime.now().subtract(const Duration(hours: 2)),
      isRead: false,
      icon: Iconsax.calendar_tick,
      iconBg: const Color(0xFFFEE2E2),
      category: 'appointment',
    ),
    NotificationItem(
      id: '2',
      titleVi: 'Giảm giá Đặc biệt',
      titleEn: 'Special Discount',
      messageVi: 'Giảm giá 20% dịch vụ tẩy trắng răng trong tháng này!',
      messageEn: 'Get 20% off on Teeth Whitening this month!',
      timestamp: DateTime.now().subtract(const Duration(days: 1)),
      isRead: true,
      icon: Iconsax.discount_shape,
      iconBg: const Color(0xFFFEF3C7),
      category: 'offer',
    ),
    NotificationItem(
      id: '3',
      titleVi: 'Thay đổi Lịch hẹn',
      titleEn: 'Schedule Changes',
      messageVi: 'Lịch hẹn của bạn với BS. Sarah đã được dời lại thành công.',
      messageEn: 'Your appointment with Dr. Sarah has been successfully rescheduled.',
      timestamp: DateTime.now().subtract(const Duration(days: 3)),
      isRead: true,
      icon: Iconsax.clock,
      iconBg: const Color(0xFFE0F2FE),
      category: 'reminder',
    ),
  ];

  String _formatTime(DateTime dt, String lang) {
    final diff = DateTime.now().difference(dt);
    if (diff.inMinutes < 60) {
      return lang == 'vi' ? '${diff.inMinutes} phút trước' : '${diff.inMinutes} hours ago'; // mockup says "2 hours ago"
    } else if (diff.inHours < 24) {
      return lang == 'vi' ? '${diff.inHours} giờ trước' : '${diff.inHours} hours ago';
    } else {
      return lang == 'vi' ? '${diff.inDays} ngày trước' : '${diff.inDays} days ago';
    }
  }

  void _markAllRead() {
    setState(() {
      for (var item in _notifications) {
        item.isRead = true;
      }
    });
  }

  void _deleteAll() {
    setState(() {
      _notifications.clear();
    });
  }

  List<NotificationItem> _getFilteredNotifications() {
    switch (_selectedTab) {
      case 1:
        return _notifications.where((n) => !n.isRead).toList();
      case 2:
        return _notifications.where((n) => n.category == 'reminder' || n.category == 'appointment').toList();
      default:
        return _notifications;
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final filteredItems = _getFilteredNotifications();

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Thông báo' : 'Notifications',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.w900,
            fontSize: 20,
          ),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.more_vert_rounded, color: context.textPrimary),
            onPressed: () {
              showModalBottomSheet(
                context: context,
                backgroundColor: context.card,
                shape: const RoundedRectangleBorder(
                  borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
                ),
                builder: (context) {
                  return SafeArea(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const SizedBox(height: 8),
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
                          leading: const Icon(Icons.done_all_rounded, color: Colors.green),
                          title: Text(
                            isVi ? 'Đánh dấu đọc hết' : 'Mark all as read',
                            style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w600),
                          ),
                          onTap: () {
                            Navigator.pop(context);
                            _markAllRead();
                          },
                        ),
                        Divider(color: context.divider, height: 1),
                        ListTile(
                          leading: const Icon(Icons.delete_sweep_rounded, color: AppColors.primary),
                          title: Text(
                            isVi ? 'Xóa tất cả' : 'Delete all',
                            style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.w600),
                          ),
                          onTap: () {
                            Navigator.pop(context);
                            _deleteAll();
                          },
                        ),
                        const SizedBox(height: 12),
                      ],
                    ),
                  );
                },
              );
            },
          ),
        ],
      ),
      body: Column(
        children: [
          // Horizontal Pill Tabs
          Container(
            padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 16),
            height: 64,
            child: Row(
              children: [
                _buildPillTab(0, isVi ? 'Tất cả' : 'All'),
                const SizedBox(width: 10),
                _buildPillTab(1, isVi ? 'Chưa đọc' : 'Unread'),
                const SizedBox(width: 10),
                _buildPillTab(2, isVi ? 'Nhắc nhở' : 'Reminders'),
              ],
            ),
          ),

          // Main Notifications List
          Expanded(
            child: filteredItems.isEmpty
                ? _buildEmptyState(isVi)
                : ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    itemCount: filteredItems.length + 1,
                    itemBuilder: (context, index) {
                      if (index == filteredItems.length) {
                        return _buildTechPromoBanner(isVi);
                      }
                      final item = filteredItems[index];
                      return _buildNotificationCard(item, isVi);
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _buildPillTab(int index, String label) {
    final isSelected = _selectedTab == index;
    final activeBg = context.isDark ? const Color(0xFFDC2626) : const Color(0xFF8B1D2F);
    final inactiveBg = context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9);

    return GestureDetector(
      onTap: () {
        setState(() {
          _selectedTab = index;
        });
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 8),
        decoration: BoxDecoration(
          color: isSelected ? activeBg : inactiveBg,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: isSelected ? Colors.transparent : context.divider,
          ),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: isSelected ? Colors.white : context.textSecondary,
            fontWeight: FontWeight.bold,
            fontSize: 13,
          ),
        ),
      ),
    );
  }

  Widget _buildNotificationCard(NotificationItem item, bool isVi) {
    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.02),
            blurRadius: 10,
            offset: const Offset(0, 4),
          )
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Red left line for unread notifications
              if (!item.isRead)
                Container(
                  width: 4,
                  color: AppColors.primary,
                ),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Icon circle (dark red/burgundy)
                      Container(
                        width: 44,
                        height: 44,
                        decoration: BoxDecoration(
                          color: context.isDark ? const Color(0xFF451A1A) : const Color(0xFF8B1D2F),
                          shape: BoxShape.circle,
                        ),
                        child: Icon(
                          item.icon,
                          color: Colors.white,
                          size: 18,
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
                                Expanded(
                                  child: Text(
                                    isVi ? item.titleVi : item.titleEn,
                                    style: TextStyle(
                                      color: context.textPrimary,
                                      fontWeight: FontWeight.w800,
                                      fontSize: 14.5,
                                    ),
                                  ),
                                ),
                                if (!item.isRead)
                                  Container(
                                    width: 6,
                                    height: 6,
                                    decoration: const BoxDecoration(
                                      color: AppColors.primary,
                                      shape: BoxShape.circle,
                                    ),
                                  ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            Text(
                              isVi ? item.messageVi : item.messageEn,
                              style: TextStyle(
                                color: context.textSecondary,
                                fontSize: 13,
                                height: 1.4,
                              ),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              _formatTime(item.timestamp, isVi ? 'vi' : 'en'),
                              style: TextStyle(
                                color: context.textSecondary.withValues(alpha: 0.6),
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTechPromoBanner(bool isVi) {
    return Container(
      margin: const EdgeInsets.only(top: 8, bottom: 24),
      width: double.infinity,
      height: 120,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16),
        gradient: const LinearGradient(
          colors: [
            Color(0xFF8B1D2F),
            Color(0xFF1E293B),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.1),
            blurRadius: 10,
            offset: const Offset(0, 4),
          )
        ],
      ),
      child: Stack(
        children: [
          // Medical Technology Lamp overlay icon/drawing
          Positioned(
            right: 12,
            top: 0,
            bottom: 0,
            child: Opacity(
              opacity: 0.25,
              child: Icon(
                Icons.light_mode_rounded,
                size: 110,
                color: Colors.white.withValues(alpha: 0.5),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(18.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                Text(
                  isVi ? 'Công nghệ mới sắp ra mắt' : 'New Technology Available',
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.w900,
                    fontSize: 16,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  isVi ? 'Khám phá dịch vụ chẩn đoán hình ảnh 3D' : 'Discover our new 3D imaging services',
                  style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.8),
                    fontSize: 12,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyState(bool isVi) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Iconsax.notification_status, size: 48, color: context.textSecondary),
          const SizedBox(height: 16),
          Text(
            isVi ? 'Không có thông báo nào' : 'No notifications',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: context.textPrimary,
            ),
          ),
        ],
      ),
    );
  }
}

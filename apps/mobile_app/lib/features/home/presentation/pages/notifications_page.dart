import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/core/services/local_notification_helper.dart';
import 'package:mobile_app/features/home/data/models/notification_models.dart';
import 'package:mobile_app/features/home/data/notification_service.dart';
import 'package:dio/dio.dart';

/// Icon/màu hiển thị theo NotificationType từ backend (system/account/schedule/service/
/// security/reminder/appointment/invoice) — khớp cách phân loại ở NotificationBell của admin_website.
class _TypeStyle {
  final IconData icon;
  final Color color;
  final Color bg;
  const _TypeStyle(this.icon, this.color, this.bg);
}

const Map<String, _TypeStyle> _typeStyles = {
  'appointment': _TypeStyle(Iconsax.calendar_tick, Color(0xFF0284C7), Color(0xFFE0F2FE)),
  'reminder': _TypeStyle(Iconsax.clock, Color(0xFFD97706), Color(0xFFFEF3C7)),
  'schedule': _TypeStyle(Iconsax.calendar, Color(0xFFD97706), Color(0xFFFEF3C7)),
  'security': _TypeStyle(Iconsax.shield_tick, Color(0xFFE11D48), Color(0xFFFFE4E6)),
  'account': _TypeStyle(Iconsax.user, Color(0xFFDC2626), Color(0xFFFEE2E2)),
  'system': _TypeStyle(Icons.settings_rounded, Color(0xFF7C3AED), Color(0xFFEDE9FE)),
  'invoice': _TypeStyle(Iconsax.receipt_1, Color(0xFF059669), Color(0xFFD1FAE5)),
  'service': _TypeStyle(Iconsax.health, Color(0xFF2563EB), Color(0xFFDBEAFE)),
};
const _fallbackStyle = _TypeStyle(Iconsax.notification, Colors.grey, Color(0xFFF1F5F9));

_TypeStyle _styleFor(String type) => _typeStyles[type] ?? _fallbackStyle;

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});

  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage> {
  final _service = NotificationService();

  int _selectedTab = 0; // 0=All, 1=Unread, 2=Reminders
  List<NotificationItem> _notifications = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final result = await _service.getNotifications(pageSize: 50);
      if (!mounted) return;
      setState(() => _notifications = result.items);
    } catch (e) {
      if (!mounted) return;
      final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
      setState(() {
        _error = e is DioException
            ? ApiClient.errorMessage(e)
            : (isVi ? 'Không thể tải thông báo.' : 'Could not load notifications.');
      });
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _markOneRead(NotificationItem item) async {
    if (item.isRead) return;
    setState(() {
      _notifications = _notifications
          .map((n) => n.id == item.id ? n.copyWith(isRead: true) : n)
          .toList();
    });
    try {
      await _service.markAsRead(item.id);
    } catch (_) {
      // Lỗi mạng khi đánh dấu đã đọc không đáng để rollback UI hay làm phiền người dùng
      // bằng thông báo lỗi — lần tải lại trang sau sẽ tự đồng bộ đúng trạng thái từ server.
    }
  }

  /// Điều hướng tới màn hình tương ứng với nội dung thông báo (RelatedEntityType do backend gắn
  /// khi tạo thông báo — xem PROJECT.md mục "Notification triggers").
  // Chặn double-tap: nếu không giữ trạng thái này, bấm nhanh 2 lần có thể push cùng 1 route
  // 2 lần liên tiếp trước khi trang đầu kịp lên, gây lỗi Flutter "duplicate page key"
  // (assertion trong navigator.dart). Chỉ mở khóa lại sau khi route đã push xong VÀ người
  // dùng quay lại (await context.push hoàn thành khi route đó bị pop).
  bool _isOpening = false;

  Future<void> _openNotification(NotificationItem item) async {
    if (_isOpening) return;
    _isOpening = true;
    unawaited(_markOneRead(item));
    try {
      LocalNotificationHelper.handleNotificationNavigation(
        type: item.type,
        title: item.title,
        body: item.body,
        payload: item.relatedEntityId,
      );
    } finally {
      _isOpening = false;
    }
  }

  bool _hasDestination(NotificationItem item) => true;

  Future<void> _markAllRead() async {
    final previous = _notifications;
    setState(() {
      _notifications = _notifications.map((n) => n.copyWith(isRead: true)).toList();
    });
    try {
      await _service.markAllAsRead();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.l10n('mark_all_read')), behavior: SnackBarBehavior.floating),
      );
    } catch (_) {
      if (!mounted) return;
      setState(() => _notifications = previous);
      final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isVi ? 'Không thể đánh dấu đã đọc. Thử lại sau.' : 'Could not mark as read. Try again later.'),
      ));
    }
  }

  Future<void> _deleteAll() async {
    final previous = _notifications;
    setState(() => _notifications = []);
    try {
      await _service.deleteAll();
      if (!mounted) return;
      final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(isVi ? 'Đã xóa tất cả thông báo.' : 'All notifications deleted.'),
          behavior: SnackBarBehavior.floating,
        ),
      );
    } catch (_) {
      if (!mounted) return;
      setState(() => _notifications = previous);
      final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(isVi ? 'Không thể xóa thông báo. Thử lại sau.' : 'Could not delete notifications. Try again later.'),
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
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
                  unawaited(_markAllRead());
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
                  unawaited(_deleteAll());
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
        return _notifications.where((n) => n.type == 'reminder').toList();
      default:
        return _notifications;
    }
  }

  String _formatTime(DateTime dt, String lang) {
    final diff = DateTime.now().difference(dt);
    if (diff.inMinutes < 1) {
      return lang == 'vi' ? 'Vừa xong' : 'Just now';
    } else if (diff.inMinutes < 60) {
      return lang == 'vi' ? '${diff.inMinutes} phút trước' : '${diff.inMinutes}m ago';
    } else if (diff.inHours < 24) {
      return lang == 'vi' ? '${diff.inHours} giờ trước' : '${diff.inHours}h ago';
    } else {
      return lang == 'vi' ? '${diff.inDays} ngày trước' : '${diff.inDays}d ago';
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
      body: RefreshIndicator(
        onRefresh: _load,
        child: _buildBody(context, isVi),
      ),
    );
  }

  Widget _buildBody(BuildContext context, bool isVi) {
    if (_isLoading && _notifications.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null && _notifications.isEmpty) {
      return ListView(
        // Bọc trong ListView để RefreshIndicator (kéo để tải lại) vẫn hoạt động ở trạng thái lỗi.
        children: [
          SizedBox(
            height: MediaQuery.of(context).size.height * 0.6,
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Iconsax.warning_2, size: 40, color: context.textMuted),
                  const SizedBox(height: 12),
                  Text(_error!, style: TextStyle(color: context.textSecondary), textAlign: TextAlign.center),
                  const SizedBox(height: 12),
                  TextButton(
                    onPressed: _load,
                    child: Text(isVi ? 'Thử lại' : 'Retry'),
                  ),
                ],
              ),
            ),
          ),
        ],
      );
    }

    return _buildList(context, _filteredList, isVi);
  }

  Widget _buildList(BuildContext context, List<NotificationItem> items, bool isVi) {
    if (items.isEmpty) {
      return ListView(
        children: [
          SizedBox(
            height: MediaQuery.of(context).size.height * 0.6,
            child: Center(
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
            ),
          ),
        ],
      );
    }

    return ListView.separated(
      itemCount: items.length,
      padding: const EdgeInsets.all(20),
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (context, index) {
        final item = items[index];
        final style = _styleFor(item.type);
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
                if (!item.isRead) Container(width: 4, color: AppColors.primary),
                Expanded(
                  child: Material(
                    color: Colors.transparent,
                    child: InkWell(
                      onTap: () => unawaited(_openNotification(item)),
                      child: Padding(
                        padding: const EdgeInsets.all(16.0),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Container(
                              width: 48,
                              height: 48,
                              decoration: BoxDecoration(
                                color: context.isDark ? style.color.withValues(alpha: 0.2) : style.bg,
                                shape: BoxShape.circle,
                              ),
                              child: Icon(style.icon, color: style.color, size: 22),
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
                                          item.title,
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
                                    item.body,
                                    style: TextStyle(
                                      color: item.isRead ? context.textSecondary : context.textPrimary.withValues(alpha: 0.9),
                                      fontSize: 13,
                                      height: 1.4,
                                    ),
                                  ),
                                  const SizedBox(height: 8),
                                  Row(
                                    children: [
                                      Text(
                                        _formatTime(item.createdAt, isVi ? 'vi' : 'en'),
                                        style: TextStyle(
                                          color: context.textMuted,
                                          fontSize: 11,
                                          fontWeight: FontWeight.w600,
                                        ),
                                      ),
                                      if (_hasDestination(item)) ...[
                                        const SizedBox(width: 8),
                                        Text(
                                          isVi ? '· Xem chi tiết →' : '· View detail →',
                                          style: const TextStyle(
                                            color: AppColors.primary,
                                            fontSize: 11,
                                            fontWeight: FontWeight.w700,
                                          ),
                                        ),
                                      ],
                                    ],
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

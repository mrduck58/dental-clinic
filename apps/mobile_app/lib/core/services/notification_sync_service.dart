import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:mobile_app/core/services/local_notification_helper.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/notification_service.dart';
import 'package:shared_preferences/shared_preferences.dart';

class NotificationSyncService with WidgetsBindingObserver {
  static final NotificationSyncService instance = NotificationSyncService._internal();
  NotificationSyncService._internal();

  final _notificationService = NotificationService();
  final _auth = AuthService();
  Timer? _timer;
  bool _isChecking = false;
  final Set<String> _shownNotificationIds = {};
  bool _isFirstRun = true;

  bool _isRunning = false;

  Future<void> init() async {
    if (kIsWeb) return;
    WidgetsBinding.instance.addObserver(this);

    try {
      final prefs = await SharedPreferences.getInstance();
      final savedIds = prefs.getStringList('shown_notification_ids') ?? [];
      _shownNotificationIds.addAll(savedIds);
    } catch (_) {}

    start();
  }

  void start() {
    _isRunning = true;
    _timer?.cancel();
    checkForNewNotifications();
    _timer = Timer.periodic(const Duration(seconds: 3), (_) {
      checkForNewNotifications();
    });
    _runAsyncLoop();
  }

  Future<void> _runAsyncLoop() async {
    while (_isRunning) {
      await checkForNewNotifications();
      await Future.delayed(const Duration(seconds: 3));
    }
  }

  void stop() {
    _isRunning = false;
    _timer?.cancel();
    _timer = null;
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Tự động kiểm tra ngay khi app chuyển trạng thái (mở lại, chạy nền, mở khóa màn hình)
    checkForNewNotifications();
  }

  Future<void> checkForNewNotifications() async {
    if (_isChecking || kIsWeb) return;
    _isChecking = true;

    try {
      final token = await _auth.getToken();
      if (token == null) {
        _isChecking = false;
        return;
      }

      final result = await _notificationService.getNotifications(pageSize: 15);
      final items = result.items;

      if (_isFirstRun) {
        _isFirstRun = false;
        if (_shownNotificationIds.isEmpty) {
          // Chỉ đánh dấu đã xem đối với thông báo cũ hơn 10 phút trước
          final now = DateTime.now();
          for (final n in items) {
            if (now.difference(n.createdAt).inMinutes > 10) {
              _shownNotificationIds.add(n.id);
            }
          }
          await _saveShownIds();
        }
      }

      // Tìm các thông báo mới chưa đọc và chưa từng hiển thị qua push notification
      final newItems = items.where((n) => !n.isRead && !_shownNotificationIds.contains(n.id)).toList();

      for (final item in newItems) {
        _shownNotificationIds.add(item.id);

        // Phát thông báo native Android (Heads-up banner & Lock screen)
        await LocalNotificationHelper.instance.showNotification(
          title: item.title,
          body: item.body,
          type: item.type,
          payload: item.relatedEntityId,
        );
      }

      if (newItems.isNotEmpty) {
        await _saveShownIds();
      }
    } catch (_) {
      // Lỗi mạng hoặc chưa đăng nhập - bỏ qua lặng lẽ
    } finally {
      _isChecking = false;
    }
  }

  Future<void> _saveShownIds() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      // Giữ tối đa 200 ID gần nhất
      final list = _shownNotificationIds.toList();
      final toSave = list.length > 200 ? list.sublist(list.length - 200) : list;
      await prefs.setStringList('shown_notification_ids', toSave);
    } catch (_) {}
  }
}

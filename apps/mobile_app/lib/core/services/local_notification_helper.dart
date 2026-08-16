import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';

class LocalNotificationHelper {
  static final LocalNotificationHelper instance = LocalNotificationHelper._internal();
  LocalNotificationHelper._internal();

  final FlutterLocalNotificationsPlugin _notificationsPlugin = FlutterLocalNotificationsPlugin();
  bool _isInitialized = false;

  Future<void> init() async {
    if (_isInitialized) return;
    if (kIsWeb) return;

    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings(
      requestAlertPermission: true,
      requestBadgePermission: true,
      requestSoundPermission: true,
    );

    const initSettings = InitializationSettings(
      android: androidSettings,
      iOS: iosSettings,
    );

    try {
      await _notificationsPlugin.initialize(
        initSettings,
        onDidReceiveNotificationResponse: (NotificationResponse response) {
          if (response.payload != null && response.payload!.isNotEmpty) {
            try {
              final data = jsonDecode(response.payload!) as Map<String, dynamic>;
              handleNotificationNavigation(
                type: data['type'] as String? ?? 'booking',
                title: data['title'] as String?,
                body: data['body'] as String?,
                payload: data['extra'] as String?,
              );
            } catch (_) {
              handleNotificationNavigation(
                type: 'booking',
                payload: response.payload,
              );
            }
          }
        },
      );
      _isInitialized = true;
    } catch (_) {
      // Bỏ qua lỗi khởi tạo trên nền tảng không hỗ trợ
    }
  }

  /// Điều hướng chính xác theo 5 loại thông báo người dùng yêu cầu:
  /// 1. Booking thành công / xác nhận -> Vào tab 'Sắp tới' của Lịch hẹn
  /// 2. Hủy lịch hẹn -> Vào tab 'Đã hủy' của Lịch hẹn
  /// 3. Nhắc lịch / nhắc thuốc -> Vào trang 'Nhắc nhở'
  /// 4. Thanh toán hóa đơn -> Vào tab 'Lịch sử GD' trong Thanh toán & Công nợ
  /// 5. Thông báo công nợ -> Vào tab 'Công nợ chờ' trong Thanh toán & Công nợ
  static void handleNotificationNavigation({
    required String type,
    String? title,
    String? body,
    String? payload,
  }) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final t = type.toLowerCase();
      final lowerTitle = (title ?? '').toLowerCase();
      final lowerBody = (body ?? '').toLowerCase();
      final lowerPayload = (payload ?? '').toLowerCase();

      try {
        // 1. Kiểm tra trường hợp CHECK-IN THÀNH CÔNG -> Vào Hàng đợi khám (Queue)
        if (lowerTitle.contains('check-in') ||
            lowerTitle.contains('checkin') ||
            lowerBody.contains('check-in') ||
            lowerBody.contains('checkin') ||
            lowerBody.contains('vào khám') ||
            lowerPayload == 'queue') {
          appRouter.push(AppRoutes.queue);
          return;
        }

        // 2. Kiểm tra trường hợp HỦY LỊCH HẸN -> Vào tab Đã hủy (index 2)
        if (lowerTitle.contains('hủy') ||
            lowerTitle.contains('huỷ') ||
            lowerTitle.contains('cancel') ||
            lowerBody.contains('hủy') ||
            lowerBody.contains('huỷ') ||
            lowerPayload == 'cancelled') {
          appRouter.go(AppRoutes.appointments, extra: {'initialTab': 2});
          return;
        }

        // 3. Kiểm tra trường hợp XÁC NHẬN LỊCH HẸN / ĐẶT LỊCH THÀNH CÔNG -> Mở chi tiết booking
        if (lowerTitle.contains('xác nhận') ||
            lowerTitle.contains('đặt lịch') ||
            t == 'booking' ||
            t == 'appointment') {
          if (payload != null &&
              payload.isNotEmpty &&
              payload != 'booking' &&
              payload != 'appointment' &&
              payload != 'null') {
            appRouter.push(AppRoutes.appointmentDetails, extra: payload);
          } else {
            appRouter.go(AppRoutes.appointments, extra: {'initialTab': 0});
          }
          return;
        }

        // 4. Kiểm tra trường hợp NHẮC LỊCH / NHẮC THUỐC / TÁI KHÁM -> Vào trang Nhắc nhở
        if (t == 'reminder' ||
            t == 'followup' ||
            lowerTitle.contains('nhắc') ||
            lowerTitle.contains('reminder') ||
            lowerBody.contains('thuốc') ||
            lowerBody.contains('uống thuốc') ||
            lowerBody.contains('nhắc lịch') ||
            lowerBody.contains('tái khám') ||
            lowerPayload == 'reminder') {
          appRouter.push(AppRoutes.reminders);
          return;
        }

        // 5. Kiểm tra trường hợp CÔNG NỢ CHỜ -> Vào tab Công nợ chờ (index 0)
        if (lowerTitle.contains('công nợ') ||
            lowerTitle.contains('chờ thanh toán') ||
            lowerTitle.contains('chưa thanh toán') ||
            lowerBody.contains('công nợ') ||
            lowerBody.contains('chờ thanh toán') ||
            lowerBody.contains('chưa thanh toán') ||
            lowerPayload == 'debt') {
          appRouter.push(AppRoutes.paymentHistory, extra: {'initialTab': 0});
          return;
        }

        // 6. Kiểm tra trường hợp THANH TOÁN HÓA ĐƠN / LỊCH SỬ GIAO DỊCH -> Vào tab Lịch sử GD (index 1)
        if (t == 'payment' ||
            t == 'invoice' ||
            lowerTitle.contains('thanh toán') ||
            lowerBody.contains('thanh toán') ||
            lowerPayload == 'payment_history') {
          appRouter.push(AppRoutes.paymentHistory, extra: {'initialTab': 1});
          return;
        }

        // Mặc định chung
        appRouter.go(AppRoutes.home);
      } catch (_) {
        // Fallback an toàn
        try {
          appRouter.go(AppRoutes.home);
        } catch (_) {}
      }
    });
  }

  final Map<String, DateTime> _recentShown = {};

  bool _isDuplicate(String key) {
    final now = DateTime.now();
    _recentShown.removeWhere((_, time) => now.difference(time).inSeconds > 15);
    if (_recentShown.containsKey(key)) {
      return true;
    }
    _recentShown[key] = now;
    return false;
  }

  /// Hiển thị thông báo đẩy trên thiết bị (Status bar, Popup banner, Lock screen)
  /// Tuân thủ đúng các cấu hình trong SettingsManager:
  /// - pushNotificationsEnabled: Tắt thì không hiển thị
  /// - notifyBooking / notifyPayment / notifyReminder / notifyFollowup: Kiểm tra theo từng loại
  /// - reminderPopup: Bật -> Popup banner (Heads-up) thả xuống
  /// - reminderLockScreen: Bật -> Hiển thị trên màn hình khóa
  Future<void> showNotification({
    required String title,
    required String body,
    String type = 'booking',
    String? payload,
    String? notificationId,
  }) async {
    if (kIsWeb) return;

    final dedupeKey = notificationId ?? '${title.trim()}|${body.trim()}|$payload';
    if (_isDuplicate(dedupeKey)) return;

    if (!_isInitialized) await init();

    final sm = SettingsManager.instance;

    // 1. Kiểm tra Master switch Thông báo đẩy
    if (!sm.pushNotificationsEnabled.value) return;

    // 2. Kiểm tra switch theo từng loại thông báo
    switch (type.toLowerCase()) {
      case 'booking':
      case 'appointment':
        if (!sm.notifyBooking.value) return;
        break;
      case 'payment':
      case 'invoice':
        if (!sm.notifyPayment.value) return;
        break;
      case 'reminder':
        if (!sm.notifyReminder.value) return;
        break;
      case 'followup':
        if (!sm.notifyFollowup.value) return;
        break;
    }

    // 3. Cấu hình chế độ hiển thị (Cửa sổ bật lên & Màn hình khóa)
    final showPopup = sm.reminderPopup.value;
    final showLockScreen = sm.reminderLockScreen.value;

    final androidDetails = AndroidNotificationDetails(
      'dental_clinic_${type.toLowerCase()}_channel',
      'Thông báo $type',
      channelDescription: 'Kênh nhận thông báo từ Nha Khoa Sơn Giang',
      importance: showPopup ? Importance.max : Importance.defaultImportance,
      priority: showPopup ? Priority.high : Priority.defaultPriority,
      visibility: showLockScreen ? NotificationVisibility.public : NotificationVisibility.secret,
      playSound: true,
      enableVibration: true,
      fullScreenIntent: false,
    );

    const iosDetails = DarwinNotificationDetails(
      presentAlert: true,
      presentBadge: true,
      presentSound: true,
    );

    final notificationDetails = NotificationDetails(
      android: androidDetails,
      iOS: iosDetails,
    );

    final id = DateTime.now().millisecondsSinceEpoch ~/ 1000;
    final payloadJson = jsonEncode({
      'type': type,
      'title': title,
      'body': body,
      'extra': payload,
    });

    try {
      await _notificationsPlugin.show(
        id,
        title,
        body,
        notificationDetails,
        payload: payloadJson,
      );
    } catch (_) {
      // Xử lý an toàn nếu hệ thống chặn
    }
  }
}


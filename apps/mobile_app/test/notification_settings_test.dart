import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/features/profile/presentation/pages/notification_settings_page.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await SettingsManager.instance.init();
  });

  group('SettingsManager Notification Preferences', () {
    test('Tất cả các cài đặt thông báo mặc định là TRUE', () {
      final sm = SettingsManager.instance;
      expect(sm.pushNotificationsEnabled.value, isTrue);
      expect(sm.reminderLockScreen.value, isTrue);
      expect(sm.reminderPopup.value, isTrue);
      expect(sm.notifyBooking.value, isTrue);
      expect(sm.notifyPayment.value, isTrue);
      expect(sm.notifyReminder.value, isTrue);
      expect(sm.notifyFollowup.value, isTrue);
    });

    test('Thay đổi cài đặt thông báo cập nhật đúng giá trị và lưu trữ', () async {
      final sm = SettingsManager.instance;

      await sm.setPushNotificationsEnabled(false);
      expect(sm.pushNotificationsEnabled.value, isFalse);

      await sm.setReminderLockScreen(false);
      expect(sm.reminderLockScreen.value, isFalse);

      await sm.setReminderPopup(false);
      expect(sm.reminderPopup.value, isFalse);

      await sm.setNotifyBooking(false);
      expect(sm.notifyBooking.value, isFalse);

      await sm.setNotifyPayment(false);
      expect(sm.notifyPayment.value, isFalse);

      await sm.setNotifyReminder(false);
      expect(sm.notifyReminder.value, isFalse);

      await sm.setNotifyFollowup(false);
      expect(sm.notifyFollowup.value, isFalse);

      // Re-init to verify SharedPreferences persistence
      await sm.init();
      expect(sm.pushNotificationsEnabled.value, isFalse);
      expect(sm.reminderLockScreen.value, isFalse);
      expect(sm.reminderPopup.value, isFalse);
      expect(sm.notifyBooking.value, isFalse);
      expect(sm.notifyPayment.value, isFalse);
      expect(sm.notifyReminder.value, isFalse);
      expect(sm.notifyFollowup.value, isFalse);
    });
  });

  group('NotificationSettingsPage Widget Tests', () {
    testWidgets('Hiển thị đầy đủ tiêu đề, các switch và nhóm thông báo', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: NotificationSettingsPage(),
        ),
      );
      await tester.pumpAndSettle();

      // Tiêu đề trang
      expect(find.text('Cài đặt thông báo'), findsWidgets);

      // Master switch: Thông báo đẩy
      expect(find.text('Thông báo đẩy'), findsOneWidget);

      // Nhóm: Chế độ lời nhắc
      expect(find.text('CHẾ ĐỘ LỜI NHẮC'), findsOneWidget);
      expect(find.text('Màn hình khóa'), findsOneWidget);
      expect(find.text('Cửa sổ bật lên'), findsOneWidget);

      // Nhóm: Loại thông báo
      expect(find.text('LOẠI THÔNG BÁO'), findsOneWidget);
      expect(find.text('Thông báo đặt lịch'), findsOneWidget);
      expect(find.text('Thông báo thanh toán hóa đơn'), findsOneWidget);
      expect(find.text('Thông báo nhắc nhở'), findsOneWidget);
      expect(find.text('Thông báo tái khám'), findsOneWidget);

      // Footer mở cài đặt hệ thống
      expect(find.text('Mở Cài đặt hệ thống'), findsOneWidget);
    });
  });
}

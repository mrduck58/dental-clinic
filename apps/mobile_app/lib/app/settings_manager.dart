import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

class SettingsManager {
  static final SettingsManager instance = SettingsManager._internal();
  SettingsManager._internal();

  late SharedPreferences _prefs;
  
  final ValueNotifier<bool> isDarkMode = ValueNotifier<bool>(false);
  final ValueNotifier<Locale> locale = ValueNotifier<Locale>(const Locale('vi'));

  Future<void> init() async {
    _prefs = await SharedPreferences.getInstance();
    isDarkMode.value = _prefs.getBool('is_dark_mode') ?? false;
    final langCode = _prefs.getString('language_code') ?? 'vi';
    locale.value = Locale(langCode);
  }

  Future<void> setDarkMode(bool value) async {
    isDarkMode.value = value;
    await _prefs.setBool('is_dark_mode', value);
  }

  Future<void> setLocale(String languageCode) async {
    locale.value = Locale(languageCode);
    await _prefs.setString('language_code', languageCode);
  }
}

class AppLocalizations {
  final Locale locale;
  AppLocalizations(this.locale);

  static AppLocalizations of(BuildContext context) {
    return AppLocalizations(SettingsManager.instance.locale.value);
  }

  static const _localizedValues = {
    'vi': {
      'settings': 'Cài đặt',
      'profile_settings': 'CÀI ĐẶT HỒ SƠ',
      'medical_info': 'Thông tin y tế',
      'family_members': 'Thành viên gia đình',
      'payment_method': 'Phương thức thanh toán',
      'payment_history': 'Lịch sử thanh toán & Công nợ',
      'app_preferences': 'TÙY CHỌN ỨNG DỤNG',
      'dark_mode': 'Chế độ tối',
      'notifications': 'Cài đặt thông báo',
      'language': 'Ngôn ngữ',
      'security': 'BẢO MẬT',
      'change_password': 'Thay đổi mật khẩu',
      'active_sessions': 'Quản lý phiên hoạt động',
      'support_legal': 'HỖ TRỢ & PHÁP LÝ',
      'help_faq': 'Trung tâm trợ giúp & FAQ',
      'tos': 'Điều khoản dịch vụ',
      'privacy_policy': 'Chính sách bảo mật',
      'logout': 'Đăng xuất',
      'logout_confirm': 'Bạn muốn đăng xuất?',
      'logout_desc': 'Bạn sẽ cần đăng nhập lại để tiếp tục sử dụng.',
      'cancel': 'Huỷ',
      'select_language': 'Chọn ngôn ngữ',
      'edit_profile': 'Chỉnh sửa hồ sơ',
      'fullname': 'Họ và tên',
      'email': 'Địa chỉ Email',
      'phone': 'Số điện thoại',
      'dob': 'Ngày sinh',
      'gender': 'Giới tính',
      'save_changes': 'Cập nhật hồ sơ',
      'patient_id': 'Mã bệnh nhân',
      'select': 'Chọn',
      'home': 'Trang chủ',
      'appointments': 'Lịch hẹn',
      'profile': 'Cá nhân',
      'quick_access': 'Truy cập nhanh',
      'view_all': 'Xem tất cả',
      'news': 'Tin tức',
      'services': 'Dịch vụ',
      'active_sessions_title': 'Phiên đăng nhập hoạt động',
      'active_sessions_desc': 'Bạn đang đăng nhập tài khoản của mình trên các thiết bị này. Hãy kiểm tra và đăng xuất khỏi các thiết bị lạ.',
      'current_device': 'Thiết bị này',
      'logout_other_devices': 'Đăng xuất khỏi tất cả các thiết bị khác',
      'medical_record': 'Hồ sơ',
      'quick_booking': 'Đặt lịch khám nhanh',
      'featured_dentists': 'Nha sĩ nổi bật',
      'no_appointments': 'Chưa có lịch hẹn sắp tới',
      'book_now': 'Đặt lịch ngay để gặp bác sĩ của bạn.',
      'book_button': 'Đặt lịch',
      'search_hint': 'Tìm kiếm...',
      'search_services': 'Tìm kiếm dịch vụ nha khoa...',
      'see_all': 'Xem tất cả',
      'notifications_title': 'Thông báo',
      'all': 'Tất cả',
      'unread': 'Chưa đọc',
      'reminders': 'Nhắc nhở',
      'mark_all_read': 'Đánh dấu đã đọc tất cả',
      'delete_all_notifications': 'Xóa tất cả thông báo',
      'notifications_empty': 'Hòm thư thông báo trống',
      'no_notifications_desc': 'Bạn không có thông báo nào vào lúc này.',
      'upcoming_appointment': 'Lịch hẹn sắp tới',
      'special_offer': 'Ưu đãi đặc biệt',
      'rescheduled_appointment': 'Thay đổi lịch hẹn',
      'new_tech_title': 'Công nghệ mới sẵn sàng',
      'tech_tag': 'CÔNG NGHỆ MỚI',
      'my_appointments': 'Lịch hẹn của tôi',
      'select_service': 'Chọn dịch vụ',
      'select_date': 'Chọn ngày khám',
      'select_time': 'Chọn giờ khám',
      'confirm_booking': 'Xác nhận dịch vụ',
      'booking_success_title': 'Đặt lịch hẹn thành công',
      'queue_title': 'Theo dõi hàng chờ',
      'current_number': 'Số khám hiện tại',
      'your_number': 'Số khám của bạn',
      'estimated_wait': 'Thời gian chờ dự kiến',
      'queue_status_waiting': 'Đang đợi khám',
      'queue_status_done': 'Đã hoàn thành',
      'queue_status_current': 'Đang trong phòng khám',
      'current_password': 'Mật khẩu hiện tại',
      'new_password': 'Mật khẩu mới',
      'confirm_password': 'Xác nhận mật khẩu mới',
      'search_hint_home': 'Tìm kiếm dịch vụ, bác sĩ...',
      'book_appointment': 'Đặt khám',
      'select_profile': 'Chọn hồ sơ',
      'add_new_profile': 'Thêm mới hồ sơ',
      'view_my_appointments': 'Xem lịch hẹn của tôi',
      'back_to_home': 'Về trang chủ',
      'symptom_desc_optional': 'Mô tả triệu chứng (Tùy chọn)',
      'symptom_placeholder': 'Ví dụ: Đau răng hàm, ê buốt...',
      'confirm_booking_btn': 'Xác nhận đặt lịch',
      'medical_history': 'Tiền sử bệnh lý',
      'invoice_details': 'Chi tiết lịch hẹn',
      'invoice_code': 'Mã lịch',
      'invoice_confirmed': 'Đã xác nhận',
      'invoice_reminder': 'Bạn sẽ nhận thông báo nhắc lịch trước 24 giờ và 1 giờ trước giờ khám.',
      'our_services': 'Dịch vụ của chúng tôi',
      'other_services': 'Dịch vụ khác',
      'no_matching_services': 'Chưa có dịch vụ nào phù hợp.',
      'category_all': 'Tất cả',
      'category_orthodontics': 'Chỉnh nha',
      'category_general': 'Tổng quát',
      'category_cosmetic': 'Thẩm mỹ',
      'featured_service_title': 'DỊCH VỤ NỔI BẬT NHẤT',
      'at_clinic': 'Tại phòng khám',
      'free_checkup': 'Khám sơ bộ miễn phí',
      'hello': 'Xin chào',
      'user': 'Bạn',
    },
    'en': {
      'settings': 'Settings',
      'profile_settings': 'PROFILE SETTINGS',
      'medical_info': 'Medical Info',
      'family_members': 'Family Members',
      'payment_method': 'Payment Methods',
      'payment_history': 'Payment History & Debt',
      'app_preferences': 'APP PREFERENCES',
      'dark_mode': 'Dark Mode',
      'notifications': 'Notification Settings',
      'language': 'Language',
      'security': 'SECURITY',
      'change_password': 'Change Password',
      'active_sessions': 'Active Sessions',
      'support_legal': 'SUPPORT & LEGAL',
      'help_faq': 'Help Center & FAQ',
      'tos': 'Terms of Service',
      'privacy_policy': 'Privacy Policy',
      'logout': 'Log Out',
      'logout_confirm': 'Are you sure you want to log out?',
      'logout_desc': 'You will need to login again to continue.',
      'cancel': 'Cancel',
      'select_language': 'Select Language',
      'edit_profile': 'Edit Profile',
      'fullname': 'Full Name',
      'email': 'Email Address',
      'phone': 'Phone Number',
      'dob': 'Date of Birth',
      'gender': 'Gender',
      'save_changes': 'Update Profile',
      'patient_id': 'Patient ID',
      'select': 'Select',
      'home': 'Home',
      'appointments': 'Appointments',
      'profile': 'Profile',
      'quick_access': 'Quick Access',
      'view_all': 'View All',
      'news': 'News',
      'services': 'Services',
      'active_sessions_title': 'Active Login Sessions',
      'active_sessions_desc': 'You are logged into your account on these devices. Please check and log out from unfamiliar devices.',
      'current_device': 'This device',
      'logout_other_devices': 'Log out from all other devices',
      'medical_record': 'Records',
      'quick_booking': 'Quick Booking',
      'featured_dentists': 'Featured Dentists',
      'no_appointments': 'No upcoming appointments',
      'book_now': 'Book now to meet your dentist.',
      'book_button': 'Book',
      'search_hint': 'Search...',
      'search_services': 'Search dental services...',
      'see_all': 'See All',
      'notifications_title': 'Notifications',
      'all': 'All',
      'unread': 'Unread',
      'reminders': 'Reminders',
      'mark_all_read': 'Mark all as read',
      'delete_all_notifications': 'Delete all notifications',
      'notifications_empty': 'Notification inbox is empty',
      'no_notifications_desc': 'You have no notifications at this time.',
      'upcoming_appointment': 'Upcoming Appointment',
      'special_offer': 'Special Offer',
      'rescheduled_appointment': 'Rescheduled Appointment',
      'new_tech_title': 'New Tech Available',
      'tech_tag': 'NEW TECH',
      'my_appointments': 'My Appointments',
      'select_service': 'Select Service',
      'select_date': 'Select Date',
      'select_time': 'Select Time',
      'confirm_booking': 'Confirm Booking',
      'booking_success_title': 'Booking Successful',
      'queue_title': 'Live Queue',
      'current_number': 'Current Queue Number',
      'your_number': 'Your Queue Number',
      'estimated_wait': 'Estimated Wait Time',
      'queue_status_waiting': 'Waiting',
      'queue_status_done': 'Completed',
      'queue_status_current': 'Currently in progress',
      'current_password': 'Current Password',
      'new_password': 'New Password',
      'confirm_password': 'Confirm New Password',
      'search_hint_home': 'Search services, dentists...',
      'book_appointment': 'Book Appointment',
      'select_profile': 'Select Profile',
      'add_new_profile': 'Add New Profile',
      'view_my_appointments': 'View My Appointments',
      'back_to_home': 'Back to Home',
      'symptom_desc_optional': 'Symptom Description (Optional)',
      'symptom_placeholder': 'E.g., Toothache, tooth sensitivity...',
      'confirm_booking_btn': 'Confirm Booking',
      'medical_history': 'Medical History',
      'invoice_details': 'Appointment Details',
      'invoice_code': 'Appointment Code',
      'invoice_confirmed': 'Confirmed',
      'invoice_reminder': 'You will receive reminders 24 hours and 1 hour before the appointment.',
      'our_services': 'Our Services',
      'other_services': 'Other Services',
      'no_matching_services': 'No matching services found.',
      'category_all': 'All',
      'category_orthodontics': 'Orthodontics',
      'category_general': 'General',
      'category_cosmetic': 'Cosmetic',
      'featured_service_title': 'MOST FEATURED SERVICE',
      'at_clinic': 'At Clinic',
      'free_checkup': 'Free preliminary exam',
      'hello': 'Hello',
      'user': 'You',
    }
  };

  String get(String key) {
    return _localizedValues[locale.languageCode]?[key] ?? key;
  }
}

extension AppThemeAndLocaleExtension on BuildContext {
  String l10n(String key) => AppLocalizations.of(this).get(key);

  bool get isDark => Theme.of(this).brightness == Brightness.dark;
  Color get bg => isDark ? const Color(0xFF0F172A) : const Color(0xFFF8FAFC);
  Color get card => isDark ? const Color(0xFF1E293B) : const Color(0xFFFFFFFF);
  Color get textPrimary => isDark ? const Color(0xFFF8FAFC) : const Color(0xFF0F172A);
  Color get textSecondary => isDark ? const Color(0xFF94A3B8) : const Color(0xFF475569);
  Color get textMuted => isDark ? const Color(0xFF64748B) : const Color(0xFF94A3B8);
  Color get divider => isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0);
  Color get primaryLight => isDark ? const Color(0xFF450A0A) : const Color(0xFFFEE2E2);
}

The above content shows the entire, complete file contents of the requested file.

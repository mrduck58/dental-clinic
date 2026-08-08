import 'package:mobile_app/app/settings_manager.dart';

abstract class ApiConstants {
  // Mặc định (USB/emulator): xem kDefaultApiBaseUrl trong settings_manager.dart.
  // Có thể đổi runtime qua trang Cài đặt > Địa chỉ máy chủ (vd: test qua Cloudflare
  // Tunnel) — không cần build lại app. ⚠️ Nếu quay lại test bằng Android Emulator,
  // đổi giá trị trong Cài đặt thành 'http://10.0.2.2:5239/api'.
  static String get baseUrl => SettingsManager.instance.apiBaseUrl.value;

  /// Backend trả về path tương đối cho ảnh upload (ví dụ "/uploads/xxx.jpg") — hàm này ghép
  /// đúng host của APP (không phải host lưu trong DB) để ảnh load được trên mọi thiết bị.
  ///
  /// Xử lý thêm 1 trường hợp lỗi dữ liệu cũ: một số ảnh (dịch vụ, bài viết, avatar nhân viên)
  /// từng bị web admin lưu nhầm thành URL tuyệt đối trỏ về "localhost" của máy dev — không app
  /// nào tải được URL đó. Nhận diện host localhost/127.0.0.1 và tự ghép lại bằng host thật của
  /// app thay vì tin theo host đã lưu trong DB.
  static String? resolveAssetUrl(String? url) {
    if (url == null || url.isEmpty) return null;
    final host = baseUrl.replaceAll('/api', '');
    if (url.startsWith('/')) return '$host$url';

    final uri = Uri.tryParse(url);
    final isLocalhost = uri != null && (uri.host == 'localhost' || uri.host == '127.0.0.1');
    if (isLocalhost && uri.path.isNotEmpty) return '$host${uri.path}';

    return url;
  }

  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String verifyOtp = '/auth/verify-otp';
  static const String resendOtp = '/auth/resend-otp';
  static const String fillProfile = '/auth/me/profile';
  static const String changePassword = '/auth/me/change-password';
  static const String logout = '/auth/logout';
  static const String googleLogin = '/auth/google-login';
  static const String forgotPasswordOtp = '/auth/patient/forgot-password';
  static const String verifyResetOtp = '/auth/patient/verify-reset-otp';
  static const String resetPassword = '/auth/reset-password';

  static const String dentists = '/dentists';
  static const String dentistSlots = '/dentists/slots';
  static String dentistDetail(String id) => '/dentists/$id';
  static String dentistReviews(String id) => '/dentists/$id/reviews';
  static String dentistReviewEligibility(String id) => '/dentists/$id/review-eligibility';
  static const String feedbacks = '/feedbacks';
  static const String featuredFeedbacks = '/feedbacks/featured';
  static const String clinicFeedbackEligibility = '/feedbacks/eligibility';
  static const String services = '/services';
  static const String posts = '/posts';
  static const String appointments = '/appointments';
  static const String myAppointments = '/appointments/my';
  static const String patientQueue = '/appointments/queue/patient';
  static const String medicalHistory = '/patients/my-medical-history';
  static const String myMedicalHistoryRecords = '/appointments/my/medical-history';
  static const String myTreatmentPlans = '/appointments/my/treatment-plans';
  static const String myMedicationReminders = '/appointments/my/medication-reminders';
  static const String familyMembers = '/patients/family-members';

  static const String myInvoices = '/payments/invoices/my';
  static const String myPaymentHistory = '/payments/invoices/my/history';
  static String paymentRequest(String invoiceId) =>
      '/payments/invoices/$invoiceId/request';
  static String paymentStatus(String invoiceId) =>
      '/payments/invoices/$invoiceId/status';

  static const String chatConversations = '/chat/conversations';
  static String chatConversation(String conversationId) =>
      '/chat/conversations/$conversationId';
  static String chatMessages(String conversationId) =>
      '/chat/conversations/$conversationId/messages';

  static const String notifications = '/notifications';
  static String notificationRead(String id) => '/notifications/$id/read';
  static const String notificationsReadAll = '/notifications/read-all';
  static String notificationDelete(String id) => '/notifications/$id';
}

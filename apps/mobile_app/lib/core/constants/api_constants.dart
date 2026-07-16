import 'package:flutter/foundation.dart';

abstract class ApiConstants {
  // Web/desktop → localhost | Android emulator → 10.0.2.2 | iOS simulator → localhost
  static final String baseUrl =
      kIsWeb ? 'http://localhost:5239/api' : 'http://10.0.2.2:5239/api';

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
  static const String services = '/services';
  static const String posts = '/posts';
  static const String appointments = '/appointments';
  static const String myAppointments = '/appointments/my';
  static const String medicalHistory = '/patients/my-medical-history';
  static const String familyMembers = '/patients/family-members';

  static const String myInvoices = '/payments/invoices/my';
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

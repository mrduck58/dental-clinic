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

  static const String logout = '/auth/logout';

  static const String dentists = '/dentists';
  static const String services = '/services';
  static const String posts = '/posts';
}

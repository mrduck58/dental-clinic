import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:shared_preferences/shared_preferences.dart';

// ── Response models ──────────────────────────────────────────────────────────

class LoginResult {
  final String accessToken;
  final String role;
  final String id;
  final String email;
  final String? fullName;

  const LoginResult({
    required this.accessToken,
    required this.role,
    required this.id,
    required this.email,
    this.fullName,
  });

  factory LoginResult.fromJson(Map<String, dynamic> json) {
    final user = json['user'] as Map<String, dynamic>;
    return LoginResult(
      accessToken: json['accessToken'] as String,
      role: user['role'] as String,
      id: user['id'].toString(),
      email: user['email'] as String,
      fullName: user['fullName'] as String?,
    );
  }
}

/// Trả về sau khi đăng ký — chưa có token, cần xác thực OTP trước.
class RegisterResult {
  final String email;
  final String message;

  const RegisterResult({required this.email, required this.message});

  factory RegisterResult.fromJson(Map<String, dynamic> json) {
    return RegisterResult(
      email: json['email'] as String,
      message: json['message'] as String,
    );
  }
}

/// Trả về sau khi xác thực OTP thành công — tài khoản đã kích hoạt.
class VerifyOtpResult {
  final String accessToken;
  final String id;
  final String role;

  const VerifyOtpResult({
    required this.accessToken,
    required this.id,
    required this.role,
  });

  factory VerifyOtpResult.fromJson(Map<String, dynamic> json) {
    return VerifyOtpResult(
      accessToken: json['accessToken'] as String,
      id: json['id'] as String,
      role: json['role'] as String,
    );
  }
}

class UserProfile {
  final String? id;
  final String fullName;
  final String email;
  final String? phoneNumber;
  final String? dateOfBirth; // "YYYY-MM-DD"
  final String? gender;
  final String? profilePictureUrl;

  const UserProfile({
    this.id,
    required this.fullName,
    required this.email,
    this.phoneNumber,
    this.dateOfBirth,
    this.gender,
    this.profilePictureUrl,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) => UserProfile(
        id: json['id']?.toString(),
        fullName: json['fullName'] as String? ?? '',
        email: json['email'] as String? ?? '',
        phoneNumber: json['phoneNumber'] as String?,
        dateOfBirth: json['dateOfBirth'] as String?,
        gender: json['gender'] as String?,
        profilePictureUrl: json['profilePictureUrl'] as String?,
      );
}

// ── Service ──────────────────────────────────────────────────────────────────

class AuthService {
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  final _client = ApiClient();
  static const _tokenKey = 'auth_token';
  static const _nameKey = 'user_name';
  static const _emailKey = 'user_email';

  // ── Auth calls ─────────────────────────────────────────────────────────────

  Future<LoginResult> login(String email, String password) async {
    final res = await _client.post(ApiConstants.login, {
      'email': email,
      'password': password,
    });
    return LoginResult.fromJson(res.data as Map<String, dynamic>);
  }

  Future<RegisterResult> register(
    String email,
    String password,
    String confirmPassword,
  ) async {
    final res = await _client.post(ApiConstants.register, {
      'email': email,
      'password': password,
      'confirmPassword': confirmPassword,
    });
    return RegisterResult.fromJson(res.data as Map<String, dynamic>);
  }

  Future<VerifyOtpResult> verifyOtp(String email, String code) async {
    final res = await _client.post(ApiConstants.verifyOtp, {
      'email': email,
      'code': code,
    });
    return VerifyOtpResult.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> resendOtp(String email) async {
    await _client.post(ApiConstants.resendOtp, {'email': email});
  }

  Future<UserProfile> getMyProfile() async {
    final token = await getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.get(ApiConstants.fillProfile, token: token);
    return UserProfile.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> fillProfile({
    required String token,
    required String firstName,
    required String lastName,
    required String phoneNumber,
    DateTime? dateOfBirth,
    String? gender,
  }) async {
    final dob = dateOfBirth == null
        ? null
        : '${dateOfBirth.year.toString().padLeft(4, '0')}'
            '-${dateOfBirth.month.toString().padLeft(2, '0')}'
            '-${dateOfBirth.day.toString().padLeft(2, '0')}';

    await _client.put(
      ApiConstants.fillProfile,
      {
        'firstName': firstName,
        'lastName': lastName,
        'phoneNumber': phoneNumber,
        'dateOfBirth': dob,
        'gender': gender,
      },
      token: token,
    );
  }

  Future<void> updateProfile({
    required String token,
    required String fullName,
    required String phoneNumber,
    DateTime? dateOfBirth,
    String? gender,
    String? profilePictureUrl,
  }) async {
    final dob = dateOfBirth == null
        ? null
        : '${dateOfBirth.year.toString().padLeft(4, '0')}'
            '-${dateOfBirth.month.toString().padLeft(2, '0')}'
            '-${dateOfBirth.day.toString().padLeft(2, '0')}';

    await _client.put(
      ApiConstants.fillProfile,
      {
        'fullName': fullName,
        'phoneNumber': phoneNumber,
        'dateOfBirth': dob,
        'gender': gender,
        'profilePictureUrl': profilePictureUrl,
      },
      token: token,
    );
  }

  Future<void> changePassword({
    required String token,
    required String currentPassword,
    required String newPassword,
  }) async {
    await _client.put(
      ApiConstants.changePassword,
      {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      },
      token: token,
    );
  }

  // ── Token storage ──────────────────────────────────────────────────────────

  Future<void> saveToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, token);
  }

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_tokenKey);
  }

  Future<void> clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
  }

  Future<void> logout({required String token}) async {
    await _client.post(ApiConstants.logout, {}, token: token);
  }

  Future<void> saveUserName(String name) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_nameKey, name);
  }

  Future<String?> getUserName() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_nameKey);
  }

  Future<void> saveUserEmail(String email) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_emailKey, email);
  }

  Future<String?> getUserEmail() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_emailKey);
  }

  static const _avatarKey = 'user_avatar';

  Future<void> saveUserAvatar(String url) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_avatarKey, url);
  }

  Future<String?> getUserAvatar() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_avatarKey);
  }

  Future<void> clearAll() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_nameKey);
    await prefs.remove(_emailKey);
    await prefs.remove(_avatarKey);
  }
}

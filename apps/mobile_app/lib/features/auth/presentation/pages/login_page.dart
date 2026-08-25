import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/core/services/firebase_messaging_service.dart';
import 'package:mobile_app/core/services/notification_sync_service.dart';
import 'package:mobile_app/core/utils/app_toast.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/auth/presentation/widgets/auth_widgets.dart';
import 'package:mobile_app/features/auth/presentation/widgets/google_signin_web_button.dart';

class LoginPage extends StatefulWidget {
  final bool expiredNotice;
  const LoginPage({super.key, this.expiredNotice = false});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  // Trên Web, Client ID lấy từ thẻ meta google-signin-client_id (web/index.html).
  // Trên native (Android/iOS), serverClientId phải là client Web Application
  // (khớp GoogleAuthSettings:ClientId ở backend) để idToken trả về xác thực được.
  final _googleSignIn = kIsWeb
      ? GoogleSignIn()
      : GoogleSignIn(
          serverClientId:
              '170456318656-4t2r29iejm1mnhbcu9raogdibehlnehm.apps.googleusercontent.com',
        );

  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();

  bool _rememberMe = false;
  bool _obscurePassword = true;
  bool _isLoading = false;
  bool _isGoogleLoading = false;

  String? _emailError;
  String? _passwordError;
  String? _generalError;

  final _auth = AuthService();
  StreamSubscription<GoogleSignInAccount?>? _googleAccountSub;

  @override
  void initState() {
    super.initState();
    if (widget.expiredNotice) {
      _generalError = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
    }
    _loadSavedCredentials();
    if (kIsWeb) {
      // Trên Web, nút Google (renderButton) tự xử lý popup/consent và phát
      // tài khoản qua stream này — không gọi signIn() trực tiếp được.
      _googleAccountSub = _googleSignIn.onCurrentUserChanged.listen((account) {
        if (account != null) _handleGoogleAccount(account);
      });
    }
  }

  Future<void> _loadSavedCredentials() async {
    try {
      final remember = await _auth.getRememberMe();
      if (remember) {
        final email = await _auth.getSavedEmail();
        final password = await _auth.getSavedPassword();
        if (email != null && email.isNotEmpty) {
          setState(() {
            _rememberMe = remember;
            _emailCtrl.text = email;
            if (password != null) {
              _passwordCtrl.text = password;
            }
          });
        }
      }
    } catch (e) {
      debugPrint("LoginPage: Failed to load saved credentials: $e");
    }
  }

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
    _googleAccountSub?.cancel();
    super.dispose();
  }

  bool _validate() {
    String? emailErr, passErr;

    final email = _emailCtrl.text.trim();
    if (email.isEmpty) {
      emailErr = 'Email không được để trống.';
    } else if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) {
      emailErr = 'Email không đúng định dạng.';
    }

    if (_passwordCtrl.text.isEmpty) passErr = 'Mật khẩu không được để trống.';

    setState(() {
      _emailError = emailErr;
      _passwordError = passErr;
      _generalError = null;
    });
    return emailErr == null && passErr == null;
  }

  Future<void> _login() async {
    if (!_validate()) return;
    setState(() => _isLoading = true);
    try {
      final result = await _auth.login(
        _emailCtrl.text.trim(),
        _passwordCtrl.text,
      );
      await _auth.saveToken(result.accessToken, remember: _rememberMe);
      await _auth.saveUserEmail(result.email);
      await _auth.saveRememberMe(_rememberMe);
      if (_rememberMe) {
        await _auth.saveSavedEmail(_emailCtrl.text.trim());
        await _auth.saveSavedPassword(_passwordCtrl.text);
      } else {
        await _auth.clearSavedEmail();
      }
      if (result.fullName != null && result.fullName!.isNotEmpty) {
        await _auth.saveUserName(result.fullName!);
      }
      if (!mounted) return;

      // Tài khoản do phòng khám lập có mật khẩu tạm gửi qua email — mật khẩu đó nằm trong hộp thư
      // của bệnh nhân vĩnh viễn, nên bắt đổi ngay trước khi vào app.
      if (result.mustChangePassword) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Vui lòng đổi mật khẩu do phòng khám cấp trước khi tiếp tục.'),
          ),
        );
        context.go('${AppRoutes.changePassword}?firstTime=true');
        return;
      }

      NotificationSyncService.instance.start();
      unawaited(FirebaseMessagingService.instance.syncTokenWithBackend());
      context.go(AppRoutes.home);
    } on DioException catch (e) {
      setState(() => _generalError = ApiClient.errorMessage(e));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  /// Luồng native (Android/iOS/desktop) — gọi signIn() trực tiếp.
  Future<void> _handleGoogleSignIn() async {
    try {
      await _googleSignIn.signOut();
      final googleUser = await _googleSignIn.signIn();
      if (googleUser == null) return; // Người dùng huỷ đăng nhập
      await _handleGoogleAccount(googleUser);
    } catch (e) {
      debugPrint('Google sign-in error: $e');
      if (mounted) {
        AppToast.showError(context, 'Lỗi đăng nhập Google: $e');
      }
    }
  }



  /// Xử lý chung sau khi có tài khoản Google (từ signIn() native hoặc nút render trên Web).
  Future<void> _handleGoogleAccount(GoogleSignInAccount googleUser) async {
    setState(() => _isGoogleLoading = true);
    try {
      final googleAuth = await googleUser.authentication;
      final idToken = googleAuth.idToken;
      if (idToken == null) {
        throw Exception('Không lấy được token từ Google.');
      }

      final result = await _auth.googleLogin(idToken);
      await _auth.saveToken(result.accessToken);
      await _auth.saveUserEmail(result.email);
      if (result.fullName != null && result.fullName!.isNotEmpty) {
        await _auth.saveUserName(result.fullName!);
      }
      if (result.avatarUrl != null && result.avatarUrl!.isNotEmpty) {
        await _auth.saveUserAvatar(result.avatarUrl!);
      }

      if (!mounted) return;
      if (!result.isNewUser) {
        AppToast.showSuccess(context, 'Chào mừng quay trở lại.');
      }
      NotificationSyncService.instance.start();
      unawaited(FirebaseMessagingService.instance.syncTokenWithBackend());
      context.go(AppRoutes.home);
    } on DioException catch (e) {
      if (mounted) {
        AppToast.showError(context, ApiClient.errorMessage(e));
      }
    } catch (e) {
      debugPrint('Google sign-in error: $e');
      if (mounted) {
        AppToast.showError(context, 'Đăng nhập Google thất bại: $e');
      }
    } finally {
      if (mounted) setState(() => _isGoogleLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AuthBackButton(
                onTap: () {
                  if (context.canPop()) context.pop();
                },
              ),
              const SizedBox(height: 32),
              const SizedBox(
                width: double.infinity,
                child: Text(
                  'Đăng nhập vào tài khoản',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.w800,
                    color: AppColors.textPrimary,
                    height: 1.25,
                  ),
                ),
              ),
              const SizedBox(height: 8),
              const SizedBox(
                width: double.infinity,
                child: Text(
                  'Chào mừng trở lại! Vui lòng nhập thông tin.',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
                ),
              ),
              const SizedBox(height: 36),
              AuthTextField(
                label: 'Email',
                hint: 'Nhập địa chỉ email',
                controller: _emailCtrl,
                keyboardType: TextInputType.emailAddress,
                prefixIcon: Iconsax.sms,
                errorText: _emailError,
              ),
              const SizedBox(height: 20),
              AuthTextField(
                label: 'Mật khẩu',
                hint: 'Nhập mật khẩu',
                controller: _passwordCtrl,
                obscureText: _obscurePassword,
                prefixIcon: Iconsax.lock,
                suffixIcon: _obscurePassword ? Iconsax.eye_slash : Iconsax.eye,
                onSuffixTap: () =>
                    setState(() => _obscurePassword = !_obscurePassword),
                errorText: _passwordError,
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  AuthCheckbox(
                    value: _rememberMe,
                    onChanged: (v) => setState(() => _rememberMe = v),
                    label: 'Ghi nhớ đăng nhập',
                  ),
                  const Spacer(),
                  TextButton(
                    onPressed: () => context.push(AppRoutes.forgotPassword),
                    style: TextButton.styleFrom(
                      padding: EdgeInsets.zero,
                      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    ),
                    child: const Text(
                      'Quên mật khẩu?',
                      style: TextStyle(
                        color: AppColors.primary,
                        fontWeight: FontWeight.w600,
                        fontSize: 13,
                      ),
                    ),
                  ),
                ],
              ),
              if (_generalError != null) ...[
                const SizedBox(height: 12),
                AuthErrorBanner(message: _generalError!),
              ],
              const SizedBox(height: 24),
              PrimaryButton(
                label: 'Đăng nhập',
                onTap: _login,
                isLoading: _isLoading,
              ),
              const SizedBox(height: 24),
              const OrDivider(),
              const SizedBox(height: 24),
              if (kIsWeb)
                SizedBox(
                  width: double.infinity,
                  height: 56,
                  child: Stack(
                    alignment: Alignment.center,
                    children: [
                      renderGoogleWebButton(),
                      if (_isGoogleLoading)
                        Container(
                          color: Colors.white.withValues(alpha: 0.7),
                          child: const Center(
                            child: CircularProgressIndicator(
                              strokeWidth: 2.5,
                              color: AppColors.primary,
                            ),
                          ),
                        ),
                    ],
                  ),
                )
              else
                GoogleSignInButton(
                  onTap: _handleGoogleSignIn,
                  isLoading: _isGoogleLoading,
                ),
              const SizedBox(height: 32),
              Center(
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text(
                      'Chưa có tài khoản? ',
                      style: TextStyle(
                        color: AppColors.textSecondary,
                        fontSize: 14,
                      ),
                    ),
                    // Bệnh nhân không tự đăng ký được nữa — tài khoản do phòng khám lập khi đến
                    // khám lần đầu. Đây là chốt chặn duy nhất ngăn việc lập hàng loạt tài khoản
                    // rồi giữ kín khung giờ của người khác.
                    const Text(
                      'Liên hệ phòng khám',
                      style: TextStyle(
                        color: AppColors.primary,
                        fontWeight: FontWeight.w700,
                        fontSize: 14,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}


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
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/auth/presentation/widgets/auth_widgets.dart';
import 'package:mobile_app/features/auth/presentation/widgets/google_signin_web_button.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
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
    _loadSavedCredentials();
    if (kIsWeb) {
      // Trên Web, nút Google (renderButton) tự xử lý popup/consent và phát
      // tài khoản qua stream này — không gọi signIn() trực tiếp được.
      _googleAccountSub = GoogleSignIn().onCurrentUserChanged.listen((account) {
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
      if (mounted) context.go(AppRoutes.home);
    } on DioException catch (e) {
      setState(() => _generalError = ApiClient.errorMessage(e));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  /// Luồng native (Android/iOS/desktop) — gọi signIn() trực tiếp.
  Future<void> _handleGoogleSignIn() async {
    final googleUser = await GoogleSignIn().signIn();
    if (googleUser == null) return; // Người dùng huỷ đăng nhập
    await _handleGoogleAccount(googleUser);
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
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Chào mừng quay trở lại.')),
        );
      }
      context.go(AppRoutes.home);
    } on DioException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(ApiClient.errorMessage(e))),
        );
      }
    } catch (e) {
      debugPrint('Google sign-in error: $e');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đăng nhập Google thất bại. Vui lòng thử lại.')),
        );
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
                    GestureDetector(
                      onTap: () => context.push(AppRoutes.register),
                      child: const Text(
                        'Tạo tài khoản',
                        style: TextStyle(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w700,
                          fontSize: 14,
                        ),
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


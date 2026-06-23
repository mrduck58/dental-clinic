import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/auth/presentation/widgets/auth_widgets.dart';

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

  String? _emailError;
  String? _passwordError;
  String? _generalError;

  final _auth = AuthService();

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
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
      await _auth.saveToken(result.accessToken);
      await _auth.saveUserEmail(result.email);
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
                    onPressed: () {},
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
              GoogleSignInButton(onTap: () {}),
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


import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/auth/presentation/widgets/auth_widgets.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();

  bool _rememberMe = false;
  bool _obscurePassword = true;
  bool _obscureConfirm = true;
  bool _isLoading = false;

  String? _emailError;
  String? _passwordError;
  String? _confirmError;
  String? _generalError;

  final _auth = AuthService();

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  bool _validate() {
    String? emailErr, passErr, confirmErr;

    final email = _emailCtrl.text.trim();
    if (email.isEmpty) {
      emailErr = 'Email không được để trống.';
    } else if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) {
      emailErr = 'Email không đúng định dạng.';
    }

    final pass = _passwordCtrl.text;
    if (pass.isEmpty) {
      passErr = 'Mật khẩu không được để trống.';
    } else if (pass.length < 8) {
      passErr = 'Mật khẩu phải có ít nhất 8 ký tự.';
    } else if (!RegExp(r'[A-Z]').hasMatch(pass)) {
      passErr = 'Mật khẩu phải có ít nhất một chữ hoa.';
    } else if (!RegExp(r'[a-z]').hasMatch(pass)) {
      passErr = 'Mật khẩu phải có ít nhất một chữ thường.';
    } else if (!RegExp(r'[0-9]').hasMatch(pass)) {
      passErr = 'Mật khẩu phải có ít nhất một chữ số.';
    }

    if (_confirmCtrl.text.isEmpty) {
      confirmErr = 'Vui lòng xác nhận mật khẩu.';
    } else if (_confirmCtrl.text != pass) {
      confirmErr = 'Mật khẩu xác nhận không khớp.';
    }

    setState(() {
      _emailError = emailErr;
      _passwordError = passErr;
      _confirmError = confirmErr;
      _generalError = null;
    });
    return emailErr == null && passErr == null && confirmErr == null;
  }

  Future<void> _register() async {
    if (!_validate()) return;
    setState(() => _isLoading = true);
    try {
      final result = await _auth.register(
        _emailCtrl.text.trim(),
        _passwordCtrl.text,
        _confirmCtrl.text,
      );
      if (mounted) context.push(AppRoutes.otp, extra: result.email);
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
              AuthBackButton(onTap: () => context.pop()),
              const SizedBox(height: 20),
              const SizedBox(
                width: double.infinity,
                child: Text(
                  'Tạo tài khoản mới',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.w800,
                    color: AppColors.textPrimary,
                  ),
                ),
              ),
              const SizedBox(height: 8),
              const SizedBox(
                width: double.infinity,
                child: Text(
                  'Điền thông tin để bắt đầu sử dụng.',
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
                label: 'Mật khẩu mới',
                hint: 'Ít nhất 8 ký tự, có hoa + thường + số',
                controller: _passwordCtrl,
                obscureText: _obscurePassword,
                prefixIcon: Iconsax.lock,
                suffixIcon: _obscurePassword ? Iconsax.eye_slash : Iconsax.eye,
                onSuffixTap: () =>
                    setState(() => _obscurePassword = !_obscurePassword),
                errorText: _passwordError,
              ),
              const SizedBox(height: 20),
              AuthTextField(
                label: 'Xác nhận mật khẩu',
                hint: 'Nhập lại mật khẩu',
                controller: _confirmCtrl,
                obscureText: _obscureConfirm,
                prefixIcon: Iconsax.lock_1,
                suffixIcon: _obscureConfirm ? Iconsax.eye_slash : Iconsax.eye,
                onSuffixTap: () =>
                    setState(() => _obscureConfirm = !_obscureConfirm),
                errorText: _confirmError,
              ),
              const SizedBox(height: 20),
              AuthCheckbox(
                value: _rememberMe,
                onChanged: (v) => setState(() => _rememberMe = v),
                label: 'Ghi nhớ đăng nhập',
              ),
              if (_generalError != null) ...[
                const SizedBox(height: 12),
                AuthErrorBanner(message: _generalError!),
              ],
              const SizedBox(height: 36),
              PrimaryButton(
                label: 'Tạo tài khoản',
                onTap: _register,
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
                      'Đã có tài khoản? ',
                      style: TextStyle(
                        color: AppColors.textSecondary,
                        fontSize: 14,
                      ),
                    ),
                    GestureDetector(
                      onTap: () => context.pop(),
                      child: const Text(
                        'Đăng nhập',
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


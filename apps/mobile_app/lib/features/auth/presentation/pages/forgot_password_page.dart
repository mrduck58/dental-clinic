import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/auth/presentation/widgets/auth_widgets.dart';

class ForgotPasswordPage extends StatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  State<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends State<ForgotPasswordPage> {
  final _emailCtrl = TextEditingController();

  bool _isLoading = false;
  String? _emailError;
  String? _generalError;

  final _auth = AuthService();

  @override
  void dispose() {
    _emailCtrl.dispose();
    super.dispose();
  }

  bool _validate() {
    final email = _emailCtrl.text.trim();
    String? emailErr;
    if (email.isEmpty) {
      emailErr = 'Email không được để trống.';
    } else if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) {
      emailErr = 'Email không đúng định dạng.';
    }
    setState(() {
      _emailError = emailErr;
      _generalError = null;
    });
    return emailErr == null;
  }

  Future<void> _submit() async {
    if (!_validate()) return;
    setState(() => _isLoading = true);
    try {
      final email = _emailCtrl.text.trim();
      await _auth.requestPasswordResetOtp(email);
      if (mounted) context.push(AppRoutes.resetOtp, extra: email);
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
              const SizedBox(height: 32),
              const SizedBox(
                width: double.infinity,
                child: Text(
                  'Quên mật khẩu',
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
                  'Nhập email đã đăng ký để nhận mã OTP đặt lại mật khẩu.',
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
              if (_generalError != null) ...[
                const SizedBox(height: 16),
                AuthErrorBanner(message: _generalError!),
              ],
              const SizedBox(height: 24),
              PrimaryButton(
                label: 'Gửi mã OTP',
                onTap: _submit,
                isLoading: _isLoading,
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}

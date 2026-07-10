import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/auth/presentation/widgets/auth_widgets.dart';

class ResetNewPasswordPage extends StatefulWidget {
  final String email;
  final String resetToken;

  const ResetNewPasswordPage({
    super.key,
    required this.email,
    required this.resetToken,
  });

  @override
  State<ResetNewPasswordPage> createState() => _ResetNewPasswordPageState();
}

class _ResetNewPasswordPageState extends State<ResetNewPasswordPage> {
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();

  bool _obscurePassword = true;
  bool _obscureConfirm = true;
  bool _isLoading = false;

  String? _passwordError;
  String? _confirmError;
  String? _generalError;

  final _auth = AuthService();

  @override
  void dispose() {
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  bool _validate() {
    final password = _passwordCtrl.text;
    String? passErr;
    if (password.isEmpty) {
      passErr = 'Mật khẩu mới không được để trống.';
    } else if (password.length < 8) {
      passErr = 'Mật khẩu phải có ít nhất 8 ký tự.';
    } else if (!RegExp(r'[A-Z]').hasMatch(password)) {
      passErr = 'Mật khẩu phải có ít nhất 1 chữ hoa.';
    } else if (!RegExp(r'[a-z]').hasMatch(password)) {
      passErr = 'Mật khẩu phải có ít nhất 1 chữ thường.';
    } else if (!RegExp(r'[0-9]').hasMatch(password)) {
      passErr = 'Mật khẩu phải có ít nhất 1 chữ số.';
    }

    String? confirmErr;
    if (_confirmCtrl.text.isEmpty) {
      confirmErr = 'Vui lòng xác nhận mật khẩu.';
    } else if (_confirmCtrl.text != password) {
      confirmErr = 'Mật khẩu xác nhận không khớp.';
    }

    setState(() {
      _passwordError = passErr;
      _confirmError = confirmErr;
      _generalError = null;
    });
    return passErr == null && confirmErr == null;
  }

  Future<void> _submit() async {
    if (!_validate()) return;
    setState(() => _isLoading = true);
    try {
      await _auth.resetPasswordWithToken(
        email: widget.email,
        resetToken: widget.resetToken,
        newPassword: _passwordCtrl.text,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đổi mật khẩu thành công.')),
        );
        context.go(AppRoutes.login);
      }
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
                  'Đặt mật khẩu mới',
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
                  'Mật khẩu mới phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường và số.',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
                ),
              ),
              const SizedBox(height: 36),
              AuthTextField(
                label: 'Mật khẩu mới',
                hint: 'Nhập mật khẩu mới',
                controller: _passwordCtrl,
                obscureText: _obscurePassword,
                prefixIcon: Iconsax.lock,
                suffixIcon: _obscurePassword ? Iconsax.eye_slash : Iconsax.eye,
                onSuffixTap: () => setState(() => _obscurePassword = !_obscurePassword),
                errorText: _passwordError,
              ),
              const SizedBox(height: 20),
              AuthTextField(
                label: 'Xác nhận mật khẩu',
                hint: 'Nhập lại mật khẩu mới',
                controller: _confirmCtrl,
                obscureText: _obscureConfirm,
                prefixIcon: Iconsax.lock,
                suffixIcon: _obscureConfirm ? Iconsax.eye_slash : Iconsax.eye,
                onSuffixTap: () => setState(() => _obscureConfirm = !_obscureConfirm),
                errorText: _confirmError,
              ),
              if (_generalError != null) ...[
                const SizedBox(height: 16),
                AuthErrorBanner(message: _generalError!),
              ],
              const SizedBox(height: 24),
              PrimaryButton(
                label: 'Đổi mật khẩu',
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

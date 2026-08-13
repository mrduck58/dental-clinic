import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';

class ChangePasswordPage extends StatefulWidget {
  const ChangePasswordPage({super.key});

  @override
  State<ChangePasswordPage> createState() => _ChangePasswordPageState();
}

class _ChangePasswordPageState extends State<ChangePasswordPage> {
  final _auth = AuthService();
  final _currentPasswordCtrl = TextEditingController();
  final _newPasswordCtrl = TextEditingController();
  final _confirmPasswordCtrl = TextEditingController();

  bool _obscureCurrent = true;
  bool _obscureNew = true;
  bool _obscureConfirm = true;
  bool _isSaving = false;
  
  String _newPassword = '';
  String _userName = '';

  @override
  void initState() {
    super.initState();
    _newPasswordCtrl.addListener(() {
      setState(() {
        _newPassword = _newPasswordCtrl.text;
      });
    });
    _loadUserInfo();
  }

  @override
  void dispose() {
    _currentPasswordCtrl.dispose();
    _newPasswordCtrl.dispose();
    _confirmPasswordCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadUserInfo() async {
    final name = await _auth.getUserName();
    if (mounted && name != null) {
      setState(() {
        _userName = name;
      });
    }
  }

  String _initials() {
    final words = _userName.trim().split(' ').where((w) => w.isNotEmpty).toList();
    if (words.isEmpty) return 'U';
    if (words.length == 1) return words[0][0].toUpperCase();
    return '${words.first[0]}${words.last[0]}'.toUpperCase();
  }

  int _calculateStrength(String value) {
    if (value.isEmpty) return 0;
    if (value.length < 8) return 1; // Weak

    int criteria = 0;
    if (RegExp(r'[A-Z]').hasMatch(value)) criteria++;
    if (RegExp(r'[a-z]').hasMatch(value)) criteria++;
    if (RegExp(r'[0-9]').hasMatch(value)) criteria++;
    if (RegExp(r'[!@#\$%^&*(),.?":{}|<>]').hasMatch(value)) criteria++;

    if (criteria >= 4) return 3; // Strong
    if (criteria >= 2) return 2; // Medium
    return 1; // Weak
  }

  String _getStrengthText(int strength, bool isVi) {
    switch (strength) {
      case 1:
        return isVi ? 'Yếu' : 'Weak';
      case 2:
        return isVi ? 'Trung bình' : 'Medium';
      case 3:
        return isVi ? 'Mạnh' : 'Strong';
      default:
        return '';
    }
  }

  Color _getStrengthColor(int strength) {
    switch (strength) {
      case 1:
        return Colors.red;
      case 2:
        return Colors.amber;
      case 3:
        return Colors.green;
      default:
        return const Color(0xFFE2E8F0);
    }
  }

  Future<void> _submit() async {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final current = _currentPasswordCtrl.text;
    final newPass = _newPasswordCtrl.text;
    final confirm = _confirmPasswordCtrl.text;

    if (current.isEmpty) {
      _showSnackbar(isVi ? 'Mật khẩu hiện tại không được để trống.' : 'Current password cannot be empty.');
      return;
    }
    if (newPass.isEmpty) {
      _showSnackbar(isVi ? 'Mật khẩu mới không được để trống.' : 'New password cannot be empty.');
      return;
    }
    if (newPass.length < 8) {
      _showSnackbar(isVi ? 'Mật khẩu mới phải có ít nhất 8 ký tự.' : 'New password must be at least 8 characters.');
      return;
    }
    if (!RegExp(r'[A-Z]').hasMatch(newPass) || 
        !RegExp(r'[a-z]').hasMatch(newPass) || 
        !RegExp(r'[0-9]').hasMatch(newPass)) {
      _showSnackbar(isVi ? 'Mật khẩu phải bao gồm cả chữ hoa, chữ thường và chữ số.' : 'Password must contain uppercase, lowercase, and numbers.');
      return;
    }
    if (confirm != newPass) {
      _showSnackbar(isVi ? 'Mật khẩu xác nhận không khớp.' : 'Confirm password does not match.');
      return;
    }

    setState(() => _isSaving = true);

    try {
      final token = await _auth.getToken();
      if (token == null) {
        throw Exception(isVi ? 'Phiên đăng nhập hết hạn.' : 'Login session expired.');
      }

      await _auth.changePassword(
        token: token,
        currentPassword: current,
        newPassword: newPass,
      );

      // Mật khẩu đã lưu cho "ghi nhớ đăng nhập" giờ là mật khẩu CŨ — không xóa thì lần đăng nhập
      // sau app tự điền mật khẩu sai và người dùng tưởng tài khoản hỏng.
      await _auth.clearSavedEmail();

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Row(
              children: [
                Icon(Icons.check_circle_rounded, color: Colors.white, size: 20),
                SizedBox(width: 8),
                Text(isVi ? 'Đổi mật khẩu thành công!' : 'Password changed successfully!'),
              ],
            ),
            backgroundColor: AppColors.success,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        );
        _leave();
      }
    } on DioException catch (e) {
      _showSnackbar(ApiClient.errorMessage(e));
    } catch (e) {
      _showSnackbar(isVi ? 'Đã xảy ra lỗi khi cập nhật mật khẩu.' : 'An error occurred while updating the password.');
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  /// Màn này vào được từ hai đường: bấm từ trang cá nhân (push — quay lại được), hoặc bị đẩy tới
  /// ngay sau khi đăng nhập bằng mật khẩu tạm do phòng khám cấp (go — thay cả stack, không còn gì
  /// để quay lại). Gọi thẳng context.pop() ở trường hợp thứ hai sẽ NÉM LỖI, và vì nó nằm trong
  /// khối try nên lỗi đó bị bắt rồi hiện thành "Đã xảy ra lỗi khi cập nhật mật khẩu" — dù mật khẩu
  /// đã đổi xong. Người dùng thấy vừa báo thành công vừa báo lỗi.
  void _leave() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRoutes.home);
    }
  }

  /// Bị buộc đổi mật khẩu (vào bằng go, không quay lại được) thì không cho thoát ra giữa chừng —
  /// thoát được nghĩa là vẫn dùng app với mật khẩu tạm còn nằm trong hộp thư.
  bool get _isForced => !context.canPop();

  void _showSnackbar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: AppColors.primary,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final strength = _calculateStrength(_newPassword);
    final strengthText = _getStrengthText(strength, isVi);
    final strengthColor = _getStrengthColor(strength);

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        automaticallyImplyLeading: false,
        leading: _isForced
            ? null
            : IconButton(
                icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
                onPressed: _leave,
              ),
        title: Text(
          isVi ? 'Đổi mật khẩu' : 'Change Password',
          style: TextStyle(
            color: AppColors.primary,
            fontWeight: FontWeight.w800,
            fontSize: 20,
          ),
        ),
        centerTitle: false,
        actions: [
          Padding(
            padding: EdgeInsets.only(right: 16.0),
            child: CircleAvatar(
              radius: 18,
              backgroundColor: AppColors.primary,
              child: Text(
                _initials(),
                style: TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                ),
              ),
            ),
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(
            color: context.divider,
            height: 1,
          ),
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: EdgeInsets.symmetric(horizontal: 24, vertical: 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [


                    // Secure Protection Card
                    Container(
                      padding: EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: context.card,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 44,
                            height: 44,
                            decoration: BoxDecoration(
                              color: context.primaryLight,
                              shape: BoxShape.circle,
                            ),
                            child: Icon(
                              Iconsax.shield_tick,
                              color: AppColors.primary,
                              size: 22,
                            ),
                          ),
                          SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  isVi ? 'Bảo vệ mật khẩu' : 'Password Protection',
                                  style: TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w700,
                                    color: context.textPrimary,
                                  ),
                                ),
                                SizedBox(height: 2),
                                Text(
                                  isVi 
                                      ? 'Mật khẩu tối thiểu 8 ký tự bao gồm chữ hoa, chữ thường và chữ số.'
                                      : 'Password must be at least 8 characters and include uppercase, lowercase, and numbers.',
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: context.textSecondary,
                                    height: 1.3,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(height: 24),

                    // Current Password Field
                    _buildLabel(context.l10n('current_password')),
                    _buildPasswordField(
                      controller: _currentPasswordCtrl,
                      obscure: _obscureCurrent,
                      onToggle: () => setState(() => _obscureCurrent = !_obscureCurrent),
                    ),
                    SizedBox(height: 20),

                    // New Password Field
                    _buildLabel(context.l10n('new_password')),
                    _buildPasswordField(
                      controller: _newPasswordCtrl,
                      obscure: _obscureNew,
                      onToggle: () => setState(() => _obscureNew = !_obscureNew),
                    ),
                    SizedBox(height: 20),

                    // Confirm Password Field
                    _buildLabel(context.l10n('confirm_password')),
                    _buildPasswordField(
                      controller: _confirmPasswordCtrl,
                      obscure: _obscureConfirm,
                      onToggle: () => setState(() => _obscureConfirm = !_obscureConfirm),
                    ),
                    SizedBox(height: 20),

                    // Password Strength
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          isVi ? 'Độ mạnh mật khẩu' : 'Password Strength',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: context.textSecondary,
                          ),
                        ),
                        if (strengthText.isNotEmpty)
                          Text(
                            strengthText,
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w700,
                              color: strengthColor,
                            ),
                          ),
                      ],
                    ),
                    SizedBox(height: 8),
                    Row(
                      children: List.generate(3, (index) {
                        final filled = index < strength;
                        return Expanded(
                          child: Container(
                            height: 6,
                            margin: EdgeInsets.only(
                              right: index < 2 ? 8 : 0,
                            ),
                            decoration: BoxDecoration(
                              color: filled ? strengthColor : context.divider,
                              borderRadius: BorderRadius.circular(999),
                            ),
                          ),
                        );
                      }),
                    ),
                    SizedBox(height: 24),

                    // Security Tip Information Banner
                    Container(
                      padding: EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                      ),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Icon(
                            Iconsax.info_circle,
                            color: context.textSecondary,
                            size: 20,
                          ),
                          SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              isVi 
                                  ? 'Thay đổi mật khẩu sẽ đăng xuất bạn khỏi tất cả các phiên hoạt động khác trên thiết bị di động và máy tính vì lý do bảo mật.'
                                  : 'Changing password will log you out of all other active sessions on mobile and desktop for security reasons.',
                              style: TextStyle(
                                fontSize: 12,
                                color: context.textSecondary,
                                height: 1.4,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(height: 24),
                  ],
                ),
              ),
            ),
            
            // Cancel and Save buttons at the bottom
            Container(
              padding: EdgeInsets.fromLTRB(24, 16, 24, 24),
              decoration: BoxDecoration(
                color: context.card,
                border: Border(
                  top: BorderSide(color: context.divider),
                ),
              ),
              child: Row(
                children: [
                  // Không hiện nút Huỷ khi đang bị buộc đổi mật khẩu — bấm được nghĩa là thoát ra
                  // mà vẫn giữ mật khẩu tạm còn nằm trong hộp thư.
                  if (!_isForced) ...[
                    Expanded(
                      child: OutlinedButton(
                        onPressed: _isSaving ? null : _leave,
                        style: OutlinedButton.styleFrom(
                          side: BorderSide(color: context.divider),
                          padding: EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(999),
                          ),
                        ),
                        child: Text(
                          context.l10n('cancel'),
                          style: TextStyle(
                            color: context.textSecondary,
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                    ),
                    SizedBox(width: 16),
                  ],
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _isSaving ? null : _submit,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        padding: EdgeInsets.symmetric(vertical: 16),
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(999),
                        ),
                      ),
                      child: _isSaving
                          ? const SizedBox(
                              width: 22,
                              height: 22,
                              child: CircularProgressIndicator(
                                color: Colors.white,
                                strokeWidth: 2.5,
                              ),
                            )
                          : Text(
                              isVi ? 'Lưu thay đổi' : 'Save Changes',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 15,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLabel(String text) {
    return Padding(
      padding: EdgeInsets.only(bottom: 8.0),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.w700,
          color: context.textPrimary,
        ),
      ),
    );
  }

  Widget _buildPasswordField({
    required TextEditingController controller,
    required bool obscure,
    required VoidCallback onToggle,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: context.divider),
      ),
      child: TextField(
        controller: controller,
        obscureText: obscure,
        style: TextStyle(
          color: context.textPrimary,
          fontSize: 15,
        ),
        decoration: InputDecoration(
          hintText: '••••••••',
          hintStyle: TextStyle(color: context.textMuted, fontSize: 15),
          contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          border: InputBorder.none,
          suffixIcon: IconButton(
            icon: Icon(
              obscure ? Icons.visibility_off_outlined : Icons.visibility_outlined,
              color: context.textMuted,
              size: 20,
            ),
            onPressed: onToggle,
          ),
        ),
      ),
    );
  }
}

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/auth/presentation/widgets/auth_widgets.dart';

class FillProfilePage extends StatefulWidget {
  const FillProfilePage({super.key});

  @override
  State<FillProfilePage> createState() => _FillProfilePageState();
}

class _FillProfilePageState extends State<FillProfilePage> {
  final _lastNameCtrl = TextEditingController();
  final _firstNameCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  DateTime? _dob;
  String? _gender;
  bool _isLoading = false;

  String? _lastNameError;
  String? _firstNameError;
  String? _phoneError;
  String? _generalError;

  static const _genders = ['Nam', 'Nữ', 'Khác'];
  final _auth = AuthService();

  @override
  void dispose() {
    _lastNameCtrl.dispose();
    _firstNameCtrl.dispose();
    _phoneCtrl.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _dob ?? DateTime(now.year - 25),
      firstDate: DateTime(1920),
      lastDate: DateTime(now.year - 1, now.month, now.day),
      helpText: 'Chọn ngày sinh',
      cancelText: 'Hủy',
      confirmText: 'Chọn',
      builder: (context, child) => Theme(
        data: Theme.of(context).copyWith(
          colorScheme: const ColorScheme.light(
            primary: AppColors.primary,
            onPrimary: Colors.white,
            surface: AppColors.surface,
          ),
        ),
        child: child!,
      ),
    );
    if (picked != null) setState(() => _dob = picked);
  }

  String get _dobText {
    if (_dob == null) return '';
    final d = _dob!;
    return '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
  }

  bool _validate() {
    String? lastNameErr, firstNameErr, phoneErr;

    final lastName = _lastNameCtrl.text.trim();
    if (lastName.isEmpty) {
      lastNameErr = 'Họ không được để trống.';
    } else if (!RegExp(r'^[\p{L}\s]+$', unicode: true).hasMatch(lastName)) {
      lastNameErr = 'Họ chỉ được chứa chữ cái.';
    } else if (lastName.length > 100) {
      lastNameErr = 'Họ không được vượt quá 100 ký tự.';
    }

    final firstName = _firstNameCtrl.text.trim();
    if (firstName.isEmpty) {
      firstNameErr = 'Tên không được để trống.';
    } else if (!RegExp(r'^[\p{L}\s]+$', unicode: true).hasMatch(firstName)) {
      firstNameErr = 'Tên chỉ được chứa chữ cái.';
    } else if (firstName.length > 100) {
      firstNameErr = 'Tên không được vượt quá 100 ký tự.';
    }

    final phone = _phoneCtrl.text.trim();
    if (phone.isEmpty) {
      phoneErr = 'Số điện thoại không được để trống.';
    } else if (!RegExp(r'^(0[3|5|7|8|9])+([0-9]{8})$').hasMatch(phone)) {
      phoneErr = 'Số điện thoại không hợp lệ (VD: 0912345678).';
    }

    setState(() {
      _lastNameError = lastNameErr;
      _firstNameError = firstNameErr;
      _phoneError = phoneErr;
      _generalError = null;
    });
    return lastNameErr == null && firstNameErr == null && phoneErr == null;
  }

  Future<void> _submit() async {
    if (!_validate()) return;
    setState(() => _isLoading = true);
    try {
      final token = await _auth.getToken();
      if (token == null) {
        setState(() => _generalError = 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
        return;
      }
      await _auth.fillProfile(
        token: token,
        firstName: _firstNameCtrl.text.trim(),
        lastName: _lastNameCtrl.text.trim(),
        phoneNumber: _phoneCtrl.text.trim(),
        dateOfBirth: _dob,
        gender: _gender,
      );
      await _auth.saveUserName(
        '${_lastNameCtrl.text.trim()} ${_firstNameCtrl.text.trim()}',
      );
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
              AuthBackButton(onTap: () => context.pop()),
              const SizedBox(height: 20),
              const SizedBox(
                width: double.infinity,
                child: Text(
                  'Điền thông tin cá nhân',
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
                  'Thông tin này giúp bác sĩ phục vụ bạn tốt hơn.',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
                ),
              ),
              const SizedBox(height: 28),

              // Avatar
              Center(
                child: Stack(
                  children: [
                    Container(
                      width: 104,
                      height: 104,
                      decoration: BoxDecoration(
                        color: AppColors.primaryLight,
                        shape: BoxShape.circle,
                        border: Border.all(color: AppColors.divider, width: 2),
                      ),
                      child: const Icon(
                        Icons.person,
                        size: 54,
                        color: AppColors.primary,
                      ),
                    ),
                    Positioned(
                      bottom: 0,
                      right: 0,
                      child: GestureDetector(
                        onTap: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('Chức năng chọn ảnh sẽ sớm ra mắt'),
                              duration: Duration(seconds: 2),
                            ),
                          );
                        },
                        child: Container(
                          width: 32,
                          height: 32,
                          decoration: BoxDecoration(
                            color: AppColors.primary,
                            shape: BoxShape.circle,
                            border: Border.all(color: AppColors.surface, width: 2.5),
                          ),
                          child: const Icon(Iconsax.edit_2, size: 14, color: Colors.white),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 32),

              AuthTextField(
                label: 'Họ',
                hint: 'Nhập họ',
                controller: _lastNameCtrl,
                prefixIcon: Iconsax.user,
                errorText: _lastNameError,
              ),
              const SizedBox(height: 20),

              AuthTextField(
                label: 'Tên',
                hint: 'Nhập tên',
                controller: _firstNameCtrl,
                prefixIcon: Iconsax.user,
                errorText: _firstNameError,
              ),
              const SizedBox(height: 20),

              _DatePickerField(
                label: 'Ngày sinh',
                value: _dobText,
                onTap: _pickDate,
              ),
              const SizedBox(height: 20),

              _GenderDropdown(
                value: _gender,
                genders: _genders,
                onChanged: (v) => setState(() => _gender = v),
              ),
              const SizedBox(height: 20),

              AuthTextField(
                label: 'Số điện thoại',
                hint: 'VD: 0912345678',
                controller: _phoneCtrl,
                keyboardType: TextInputType.phone,
                prefixIcon: Iconsax.call,
                errorText: _phoneError,
              ),

              if (_generalError != null) ...[
                const SizedBox(height: 12),
                AuthErrorBanner(message: _generalError!),
              ],
              const SizedBox(height: 40),

              PrimaryButton(
                label: 'Tiếp tục',
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

class _DatePickerField extends StatelessWidget {
  final String label;
  final String value;
  final VoidCallback onTap;

  const _DatePickerField({
    required this.label,
    required this.value,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final hasValue = value.isNotEmpty;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 8),
        GestureDetector(
          onTap: onTap,
          child: Container(
            width: double.infinity,
            height: 54,
            padding: const EdgeInsets.symmetric(horizontal: 20),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(999),
              border: Border.all(color: AppColors.divider),
            ),
            child: Row(
              children: [
                const Icon(Iconsax.calendar, color: AppColors.textMuted, size: 20),
                const SizedBox(width: 12),
                Text(
                  hasValue ? value : 'Chọn ngày sinh (không bắt buộc)',
                  style: TextStyle(
                    fontSize: 14,
                    color: hasValue ? AppColors.textPrimary : AppColors.textMuted,
                    fontWeight: hasValue ? FontWeight.w500 : FontWeight.w400,
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _GenderDropdown extends StatelessWidget {
  final String? value;
  final List<String> genders;
  final ValueChanged<String?> onChanged;

  const _GenderDropdown({
    required this.value,
    required this.genders,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Giới tính',
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 20),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(999),
            border: Border.all(color: AppColors.divider),
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              isExpanded: true,
              value: value,
              hint: const Text(
                'Chọn giới tính (không bắt buộc)',
                style: TextStyle(color: AppColors.textMuted, fontSize: 14),
              ),
              icon: const Icon(Iconsax.arrow_down_1, size: 18, color: AppColors.textMuted),
              items: genders
                  .map((g) => DropdownMenuItem(
                        value: g,
                        child: Text(
                          g,
                          style: const TextStyle(
                            fontSize: 15,
                            color: AppColors.textPrimary,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ))
                  .toList(),
              onChanged: onChanged,
            ),
          ),
        ),
      ],
    );
  }
}


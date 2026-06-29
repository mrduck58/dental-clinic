import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:image_picker/image_picker.dart';
import 'package:http_parser/http_parser.dart';

class EditProfilePage extends StatefulWidget {
  const EditProfilePage({super.key});

  @override
  State<EditProfilePage> createState() => _EditProfilePageState();
}

class _EditProfilePageState extends State<EditProfilePage> {
  final _auth = AuthService();
  final _nameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  
  DateTime? _dob;
  String? _gender;
  
  bool _isLoading = true;
  bool _isSaving = false;
  String? _errorMessage;
  UserProfile? _profile;

  final List<String> _genderOptions = ['Nam', 'Nữ', 'Khác'];
  String? _avatarUrl;

  ImageProvider _getAvatarProvider() {
    if (_avatarUrl != null && _avatarUrl!.isNotEmpty) {
      if (_avatarUrl!.startsWith('http')) {
        return NetworkImage(_avatarUrl!);
      }
      final baseUrlHost = ApiConstants.baseUrl.replaceAll('/api', '');
      return NetworkImage('$baseUrlHost$_avatarUrl');
    }
    return const AssetImage('assets/images/bac_si_1.png');
  }

  Future<void> _pickAndUploadAvatar() async {
    final picker = ImagePicker();
    try {
      final file = await picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 512,
        maxHeight: 512,
        imageQuality: 80,
      );
      if (file == null) return;

      setState(() => _isSaving = true);
      
      final bytes = await file.readAsBytes();
      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(
          bytes,
          filename: file.name,
          contentType: MediaType('image', file.name.split('.').last),
        ),
      });

      final response = await ApiClient().post('/files/upload', formData);
      final url = response.data['url'] as String;

      setState(() {
        _avatarUrl = url;
        _isSaving = false;
      });
      _showSnackbar('Chọn ảnh đại diện mới thành công!');
    } catch (e) {
      setState(() => _isSaving = false);
      _showSnackbar('Không thể tải ảnh lên: $e');
    }
  }

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadProfile() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });
    try {
      final p = await _auth.getMyProfile();
      setState(() {
        _profile = p;
        _nameCtrl.text = p.fullName;
        _emailCtrl.text = p.email;
        _phoneCtrl.text = p.phoneNumber ?? '';
        
        if (p.dateOfBirth != null && p.dateOfBirth!.isNotEmpty) {
          _dob = DateTime.tryParse(p.dateOfBirth!);
        }
        
        _gender = p.gender; // Directly maps Nam / Nữ / Khác
        _avatarUrl = p.profilePictureUrl;
        if (_avatarUrl != null) {
          _auth.saveUserAvatar(_avatarUrl!);
        }
        _isLoading = false;
      });
    } on DioException catch (e) {
      setState(() {
        _errorMessage = ApiClient.errorMessage(e);
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _errorMessage = 'Không thể tải thông tin cá nhân.';
        _isLoading = false;
      });
    }
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _dob ?? DateTime(now.year - 25, now.month, now.day),
      firstDate: DateTime(1900),
      lastDate: now,
      helpText: 'Chọn ngày sinh',
      cancelText: 'Hủy',
      confirmText: 'Chọn',
      builder: (context, child) => Theme(
        data: Theme.of(context).copyWith(
          colorScheme: Theme.of(context).colorScheme.copyWith(
            primary: AppColors.primary,
            onPrimary: Colors.white,
          ),
        ),
        child: child!,
      ),
    );
    if (picked != null) {
      setState(() => _dob = picked);
    }
  }

  String _formatDate(DateTime? date) {
    if (date == null) return 'Chọn ngày sinh';
    return '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
  }

  String _getPatientId(String? userId) {
    if (userId == null || userId.isEmpty) return '#DC-99201';
    final cleanId = userId.replaceAll('-', '');
    if (cleanId.length >= 5) {
      return '#DC-${cleanId.substring(cleanId.length - 5).toUpperCase()}';
    }
    return '#DC-99201';
  }

  Future<void> _submit() async {
    final name = _nameCtrl.text.trim();
    final phone = _phoneCtrl.text.trim();

    if (name.isEmpty) {
      _showSnackbar('Họ và tên không được để trống');
      return;
    }
    if (phone.isEmpty) {
      _showSnackbar('Số điện thoại không được để trống');
      return;
    }

    setState(() => _isSaving = true);

    try {
      final token = await _auth.getToken();
      if (token == null) {
        throw Exception('Phiên đăng nhập hết hạn.');
      }

      await _auth.updateProfile(
        token: token,
        fullName: name,
        phoneNumber: phone,
        dateOfBirth: _dob,
        gender: _gender,
        profilePictureUrl: _avatarUrl,
      );

      await _auth.saveUserName(name);
      if (_avatarUrl != null) {
        await _auth.saveUserAvatar(_avatarUrl!);
      }

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Row(
              children: [
                Icon(Icons.check_circle_rounded, color: Colors.white, size: 20),
                SizedBox(width: 8),
                Text('Cập nhật hồ sơ thành công!'),
              ],
            ),
            backgroundColor: AppColors.success,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        );
        context.pop(true); // Trả về true để reload lại trang profile
      }
    } on DioException catch (e) {
      _showSnackbar(ApiClient.errorMessage(e));
    } catch (e) {
      _showSnackbar('Đã xảy ra lỗi khi cập nhật hồ sơ.');
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

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
    final patientId = _getPatientId(_profile?.id);

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          context.l10n('edit_profile'),
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 20,
          ),
        ),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: CircleAvatar(
              radius: 18,
              backgroundColor: context.divider,
              backgroundImage: _getAvatarProvider(),
            ),
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : _errorMessage != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(_errorMessage!, style: const TextStyle(color: Colors.red, fontSize: 16)),
                      const SizedBox(height: 16),
                      ElevatedButton(
                        onPressed: _loadProfile,
                        style: ElevatedButton.styleFrom(backgroundColor: AppColors.primary),
                        child: const Text('Thử lại'),
                      ),
                    ],
                  ),
                )
              : SingleChildScrollView(
                  padding: const EdgeInsets.fromLTRB(24, 24, 24, 120),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Circular Profile Photo with Camera icon overlay
                      Center(
                        child: Stack(
                          children: [
                            Container(
                              width: 120,
                              height: 120,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                border: Border.all(color: context.divider, width: 3),
                                image: DecorationImage(
                                  image: _getAvatarProvider(),
                                  fit: BoxFit.cover,
                                ),
                              ),
                            ),
                            if (_isSaving)
                              Positioned.fill(
                                child: Container(
                                  decoration: const BoxDecoration(
                                    color: Colors.black38,
                                    shape: BoxShape.circle,
                                  ),
                                  child: const Center(
                                    child: CircularProgressIndicator(color: Colors.white),
                                  ),
                                ),
                              ),
                            Positioned(
                              bottom: 0,
                              right: 4,
                              child: GestureDetector(
                                onTap: _pickAndUploadAvatar,
                                child: Container(
                                  width: 34,
                                  height: 34,
                                  decoration: BoxDecoration(
                                    color: AppColors.primary,
                                    shape: BoxShape.circle,
                                    border: Border.all(color: context.card, width: 2),
                                  ),
                                  child: const Icon(
                                    Icons.camera_alt_outlined,
                                    color: Colors.white,
                                    size: 16,
                                  ),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                      
                      // Name & Patient ID
                      Center(
                        child: Column(
                          children: [
                            Text(
                              _nameCtrl.text.isNotEmpty ? _nameCtrl.text : 'Alex Reed',
                              style: TextStyle(
                                fontSize: 22,
                                fontWeight: FontWeight.w800,
                                color: context.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              '${context.l10n('patient_id')}: $patientId',
                              style: TextStyle(
                                fontSize: 14,
                                color: context.textSecondary,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 28),

                      // Full Name Field
                      _buildLabel(context.l10n('fullname')),
                      _buildTextField(
                        controller: _nameCtrl,
                        icon: Iconsax.user,
                        hint: context.l10n('fullname'),
                      ),
                      const SizedBox(height: 20),

                      // Email Address Field (Disabled/Read-only)
                      _buildLabel(context.l10n('email')),
                      _buildTextField(
                        controller: _emailCtrl,
                        icon: Iconsax.sms,
                        hint: 'email@example.com',
                        enabled: false,
                      ),
                      const SizedBox(height: 20),

                      // Phone Number Field
                      _buildLabel(context.l10n('phone')),
                      _buildTextField(
                        controller: _phoneCtrl,
                        icon: Iconsax.call,
                        hint: context.l10n('phone'),
                        keyboardType: TextInputType.phone,
                      ),
                      const SizedBox(height: 20),

                      // Date of Birth & Gender Fields Side by Side
                      Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                _buildLabel(context.l10n('dob')),
                                GestureDetector(
                                  onTap: _pickDate,
                                  child: Container(
                                    height: 56,
                                    padding: const EdgeInsets.symmetric(horizontal: 16),
                                    decoration: BoxDecoration(
                                      color: context.card,
                                      borderRadius: BorderRadius.circular(12),
                                      border: Border.all(color: context.divider),
                                    ),
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          _dob != null ? _formatDate(_dob) : context.l10n('select'),
                                          style: TextStyle(
                                            color: _dob != null
                                                ? context.textPrimary
                                                : context.textMuted,
                                            fontSize: 15,
                                          ),
                                        ),
                                        Icon(
                                          Iconsax.calendar,
                                          color: context.textMuted,
                                          size: 20,
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                _buildLabel(context.l10n('gender')),
                                Container(
                                  height: 56,
                                  padding: const EdgeInsets.symmetric(horizontal: 16),
                                  decoration: BoxDecoration(
                                    color: context.card,
                                    borderRadius: BorderRadius.circular(12),
                                    border: Border.all(color: context.divider),
                                  ),
                                  child: DropdownButtonHideUnderline(
                                    child: DropdownButton<String>(
                                      dropdownColor: context.card,
                                      value: _genderOptions.contains(_gender) ? _gender : null,
                                      hint: Text(
                                        context.l10n('select'),
                                        style: TextStyle(color: context.textMuted, fontSize: 15),
                                      ),
                                      icon: Icon(
                                        Icons.keyboard_arrow_down_rounded,
                                        color: context.textMuted,
                                        size: 20,
                                      ),
                                      isExpanded: true,
                                      style: TextStyle(
                                        color: context.textPrimary,
                                        fontSize: 15,
                                      ),
                                      items: _genderOptions.map((String val) {
                                        return DropdownMenuItem<String>(
                                          value: val,
                                          child: Text(val),
                                        );
                                      }).toList(),
                                      onChanged: (val) {
                                        setState(() {
                                          _gender = val;
                                        });
                                      },
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 32),

                      // Update Profile Button
                      SizedBox(
                        width: double.infinity,
                        height: 54,
                        child: ElevatedButton(
                          onPressed: _isSaving ? null : _submit,
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            elevation: 0,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(27),
                            ),
                          ),
                          child: _isSaving
                              ? const SizedBox(
                                  width: 24,
                                  height: 24,
                                  child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                                )
                              : Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    const Icon(Icons.check_circle_outline_rounded, color: Colors.white, size: 22),
                                    const SizedBox(width: 8),
                                    Text(
                                      context.l10n('save_changes'),
                                      style: const TextStyle(
                                        color: Colors.white,
                                        fontSize: 16,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                  ],
                                ),
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }

  Widget _buildLabel(String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8.0),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.w600,
          color: context.textSecondary,
        ),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required IconData icon,
    required String hint,
    bool enabled = true,
    TextInputType keyboardType = TextInputType.text,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: enabled ? context.card : (context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9)),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: context.divider,
        ),
      ),
      child: TextField(
        controller: controller,
        enabled: enabled,
        keyboardType: keyboardType,
        style: TextStyle(
          color: enabled ? context.textPrimary : context.textSecondary,
          fontSize: 15,
        ),
        decoration: InputDecoration(
          hintText: hint,
          hintStyle: TextStyle(color: context.textMuted, fontSize: 15),
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          border: InputBorder.none,
          suffixIcon: Icon(
            icon,
            color: context.textMuted,
            size: 20,
          ),
        ),
      ),
    );
  }
}


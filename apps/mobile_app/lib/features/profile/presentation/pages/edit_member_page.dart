import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';

class EditMemberPage extends StatefulWidget {
  final FamilyMember member;
  const EditMemberPage({super.key, required this.member});

  @override
  State<EditMemberPage> createState() => _EditMemberPageState();
}

class _EditMemberPageState extends State<EditMemberPage> {
  final _familyService = FamilyService();
  
  late final TextEditingController _nameCtrl;
  late final TextEditingController _phoneCtrl;

  String? _relationship;
  DateTime? _dob;
  late String _gender;

  final List<String> _relationshipOptions = [
    'Bố',
    'Mẹ',
    'Vợ/Chồng',
    'Con',
    'Anh/Chị/Em',
    'Khác'
  ];

  @override
  void initState() {
    super.initState();
    _nameCtrl = TextEditingController(text: widget.member.fullName);
    _phoneCtrl = TextEditingController(text: widget.member.phoneNumber ?? '');
    _relationship = widget.member.relationship;
    _dob = widget.member.dateOfBirth;
    _gender = widget.member.gender;
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _phoneCtrl.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final picked = await showDatePicker(
      context: context,
      initialDate: _dob ?? DateTime(now.year - 15, now.month, now.day),
      firstDate: DateTime(1900),
      lastDate: now,
      helpText: isVi ? 'Chọn ngày sinh' : 'Select Date of Birth',
      cancelText: context.l10n('cancel'),
      confirmText: isVi ? 'Chọn' : 'Select',
      builder: (context, child) => Theme(
        data: Theme.of(context).copyWith(
          colorScheme: const ColorScheme.light(
            primary: AppColors.primary,
            onPrimary: Colors.white,
            surface: Colors.white,
            onSurface: AppColors.textPrimary,
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
    if (date == null) return 'dd/mm/yyyy';
    return '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
  }

  Future<void> _save() async {
    final name = _nameCtrl.text.trim();
    if (name.isEmpty) {
      _showSnackbar('Họ và tên không được để trống');
      return;
    }
    if (_relationship == null) {
      _showSnackbar('Vui lòng chọn mối quan hệ');
      return;
    }
    if (_dob == null) {
      _showSnackbar('Vui lòng chọn ngày sinh');
      return;
    }

    final updated = FamilyMember(
      id: widget.member.id,
      fullName: name,
      relationship: _relationship!,
      dateOfBirth: _dob!,
      gender: _gender,
      phoneNumber: _phoneCtrl.text.trim().isNotEmpty ? _phoneCtrl.text.trim() : null,
      profilePictureUrl: widget.member.profilePictureUrl,
    );

    try {
      await _familyService.updateMember(updated);
    } catch (_) {
      if (!mounted) return;
      _showSnackbar('Không thể cập nhật thành viên. Vui lòng thử lại.');
      return;
    }

    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: const Row(
          children: [
            Icon(Icons.check_circle_rounded, color: Colors.white, size: 20),
            SizedBox(width: 8),
            Text('Đã cập nhật hồ sơ thành viên thành công!'),
          ],
        ),
        backgroundColor: AppColors.success,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    );

    context.pop(true); // Return success to reload
  }

  Future<void> _remove() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xóa thành viên'),
        content: Text('Bạn có chắc chắn muốn xóa hồ sơ thành viên "${widget.member.fullName}"? Hành động này không thể hoàn tác.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy', style: TextStyle(color: Colors.grey)),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Xóa', style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );

    if (confirm == true) {
      try {
        await _familyService.removeMember(widget.member.id);
      } catch (_) {
        if (mounted) _showSnackbar('Không thể xóa thành viên. Vui lòng thử lại.');
        return;
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Row(
              children: [
                Icon(Icons.delete_forever_rounded, color: Colors.white, size: 20),
                SizedBox(width: 8),
                Text('Đã xóa thành viên thành công!'),
              ],
            ),
            backgroundColor: AppColors.primary,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        );
        context.pop(true); // Return success to reload
      }
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
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.primary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Chỉnh sửa thành viên' : 'Edit Member',
          style: TextStyle(
            color: AppColors.primary,
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
              backgroundColor: const Color(0xFFE2E8F0),
              backgroundImage: const AssetImage('assets/images/bac_si_1.png'),
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
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 24, 24, 140),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Circular Avatar photo with edit icon
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
                        image: AssetImage(widget.member.profilePictureUrl ?? 'assets/images/bac_si_4.png'),
                        fit: BoxFit.cover,
                      ),
                    ),
                  ),
                  Positioned(
                    bottom: 0,
                    right: 4,
                    child: GestureDetector(
                      onTap: () => _showSnackbar(isVi ? 'Tính năng chỉnh sửa ảnh thành viên đang được phát triển.' : 'Edit member photo feature is under development.'),
                      child: Container(
                        width: 34,
                        height: 34,
                        decoration: BoxDecoration(
                          color: AppColors.primary,
                          shape: BoxShape.circle,
                          border: Border.all(color: context.card, width: 2),
                        ),
                        child: const Icon(
                          Icons.edit,
                          color: Colors.white,
                          size: 16,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            Center(
              child: Text(
                isVi ? 'Cập nhật ảnh thành viên' : 'Update Member Photo',
                style: TextStyle(
                  color: context.textSecondary,
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            const SizedBox(height: 28),

            // Full Name Field
            _buildLabel(context.l10n('fullname')),
            _buildTextField(
              controller: _nameCtrl,
              hint: isVi ? 'Nhập họ và tên' : 'Enter full name',
            ),
            const SizedBox(height: 20),

            // Relationship Field
            _buildLabel(isVi ? 'Mối quan hệ' : 'Relationship'),
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
                  value: _relationship,
                  dropdownColor: context.card,
                  hint: Text(
                    isVi ? 'Chọn mối quan hệ' : 'Select relationship',
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
                  items: _relationshipOptions.map((String val) {
                    // Translate relationships in dropdown UI
                    String labelVi = val;
                    String labelEn = val;
                    if (val == 'Bố') labelEn = 'Father';
                    if (val == 'Mẹ') labelEn = 'Mother';
                    if (val == 'Vợ/Chồng') labelEn = 'Spouse';
                    if (val == 'Con') labelEn = 'Child';
                    if (val == 'Anh/Chị/Em') labelEn = 'Sibling';
                    if (val == 'Khác') labelEn = 'Other';

                    return DropdownMenuItem<String>(
                      value: val,
                      child: Text(isVi ? labelVi : labelEn),
                    );
                  }).toList(),
                  onChanged: (val) {
                    setState(() {
                      _relationship = val;
                    });
                  },
                ),
              ),
            ),
            const SizedBox(height: 20),

            // Date of Birth Field
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
                      _dob != null ? _formatDate(_dob) : (isVi ? 'Chọn ngày sinh' : 'Select date of birth'),
                      style: TextStyle(
                        color: _dob != null ? context.textPrimary : context.textMuted,
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
            const SizedBox(height: 20),

            // Gender Field
            _buildLabel(context.l10n('gender')),
            Container(
              height: 50,
              decoration: BoxDecoration(
                color: context.divider,
                borderRadius: BorderRadius.circular(12),
              ),
              padding: const EdgeInsets.all(4),
              child: Row(
                children: [
                  _buildGenderSegment('Nam'),
                  _buildGenderSegment('Nữ'),
                  _buildGenderSegment('Khác'),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Phone Number Field
            _buildLabel(context.l10n('phone')),
            _buildTextField(
              controller: _phoneCtrl,
              hint: isVi ? 'Nhập số điện thoại của thành viên' : 'Enter phone number',
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 36),

            // Save changes button
            SizedBox(
              width: double.infinity,
              height: 54,
              child: ElevatedButton(
                onPressed: _save,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(27),
                  ),
                ),
                child: Text(
                  isVi ? 'Lưu thay đổi' : 'Save Changes',
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 16),

            // Remove Member Button
            SizedBox(
              width: double.infinity,
              height: 54,
              child: OutlinedButton.icon(
                onPressed: _remove,
                icon: const Icon(Icons.delete_outline_rounded, color: Color(0xFFEF4444), size: 20),
                label: Text(
                  isVi ? 'Xóa thành viên' : 'Delete Member',
                  style: const TextStyle(
                    color: Color(0xFFEF4444),
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                style: OutlinedButton.styleFrom(
                  backgroundColor: context.isDark ? const Color(0xFF450A0A) : const Color(0xFFFEF2F2),
                  side: BorderSide(color: context.isDark ? Colors.transparent : const Color(0xFFFEE2E2)),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(27),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildGenderSegment(String val) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final isSelected = _gender == val;
    String displayVal = val;
    if (val == 'Nam') displayVal = isVi ? 'Nam' : 'Male';
    if (val == 'Nữ') displayVal = isVi ? 'Nữ' : 'Female';
    if (val == 'Khác') displayVal = isVi ? 'Khác' : 'Other';

    return Expanded(
      child: GestureDetector(
        onTap: () => setState(() => _gender = val),
        child: Container(
          decoration: BoxDecoration(
            color: isSelected ? context.card : Colors.transparent,
            borderRadius: BorderRadius.circular(8),
            boxShadow: isSelected
                ? [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.08),
                      blurRadius: 4,
                      offset: const Offset(0, 2),
                    ),
                  ]
                : null,
          ),
          alignment: Alignment.center,
          child: Text(
            displayVal,
            style: TextStyle(
              color: isSelected ? AppColors.primary : context.textSecondary,
              fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
              fontSize: 14,
            ),
          ),
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
          color: context.textPrimary,
        ),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String hint,
    TextInputType keyboardType = TextInputType.text,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: context.divider,
        ),
      ),
      child: TextField(
        controller: controller,
        keyboardType: keyboardType,
        style: TextStyle(
          color: context.textPrimary,
          fontSize: 15,
        ),
        decoration: InputDecoration(
          hintText: hint,
          hintStyle: TextStyle(color: context.textMuted, fontSize: 15),
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          border: InputBorder.none,
        ),
      ),
    );
  }
}

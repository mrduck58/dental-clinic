import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';

class AddMemberPage extends StatefulWidget {
  const AddMemberPage({super.key});

  @override
  State<AddMemberPage> createState() => _AddMemberPageState();
}

class _AddMemberPageState extends State<AddMemberPage> {
  final _familyService = FamilyService();
  final _nameCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();

  String? _relationship;
  DateTime? _dob;
  String _gender = 'Nam'; // Default to Nam (Male)

  final List<String> _relationshipOptions = [
    'Bố',
    'Mẹ',
    'Vợ/Chồng',
    'Con',
    'Anh/Chị/Em',
    'Khác'
  ];

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

  void _save() {
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

    final newMember = FamilyMember(
      id: 'member_${DateTime.now().millisecondsSinceEpoch}',
      fullName: name,
      relationship: _relationship!,
      dateOfBirth: _dob!,
      gender: _gender,
      phoneNumber: _phoneCtrl.text.trim().isNotEmpty ? _phoneCtrl.text.trim() : null,
      profilePictureUrl: 'assets/images/bac_si_4.png', // Default female avatar or placeholder
    );

    _familyService.addMember(newMember);

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: const Row(
          children: [
            Icon(Icons.check_circle_rounded, color: Colors.white, size: 20),
            SizedBox(width: 8),
            Text('Đã thêm thành viên gia đình thành công!'),
          ],
        ),
        backgroundColor: AppColors.success,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    );

    context.pop(true); // Return success to reload
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
          isVi ? 'Thêm thành viên' : 'Add Family Member',
          style: TextStyle(
            color: AppColors.primary,
            fontWeight: FontWeight.w800,
            fontSize: 20,
          ),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(
            color: context.divider,
            height: 1,
          ),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 24, 24, 120),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Expand Care Circle Card ──────────────────────────────────────
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: context.primaryLight,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.isDark ? Colors.transparent : const Color(0xFFFDE2E2)),
              ),
              child: Row(
                children: [
                  Container(
                    width: 50,
                    height: 50,
                    decoration: const BoxDecoration(
                      color: Colors.white,
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Iconsax.people5,
                      color: AppColors.primary,
                      size: 24,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isVi ? 'Mở rộng vòng kết nối chăm sóc' : 'Expand Care Circle',
                          style: const TextStyle(
                            color: AppColors.primary,
                            fontWeight: FontWeight.w700,
                            fontSize: 15,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          isVi
                              ? 'Quản lý hồ sơ nha khoa và lịch hẹn cho người thân của bạn tại một nơi.'
                              : 'Manage dental records and appointments for your loved ones in one place.',
                          style: TextStyle(
                            color: context.textSecondary,
                            fontSize: 12,
                            height: 1.35,
                          ),
                        ),
                      ],
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
                    // Translate relationships in dropdown UI if needed
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
            _buildLabel('${context.l10n('phone')} ${isVi ? '(tùy chọn)' : '(optional)'}'),
            _buildTextField(
              controller: _phoneCtrl,
              hint: isVi ? 'Nhập số điện thoại của thành viên' : 'Enter phone number',
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 28),

            // Info Box
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: context.divider),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info_outline_rounded, color: context.textSecondary, size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      isVi
                          ? 'Sau khi thêm, bạn có thể chuyển đổi hồ sơ từ tab "Cá nhân" để đặt lịch hẹn cho thành viên này.'
                          : 'After adding, you can switch profiles from the "Profile" tab to book appointments for this member.',
                      style: TextStyle(
                        color: context.textSecondary,
                        fontSize: 13,
                        height: 1.4,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: Container(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 30),
        decoration: BoxDecoration(
          color: context.card,
          border: Border(top: BorderSide(color: context.divider)),
        ),
        child: Row(
          children: [
            Expanded(
              child: SizedBox(
                height: 52,
                child: OutlinedButton(
                  onPressed: () => context.pop(),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppColors.primary,
                    side: const BorderSide(color: AppColors.primary),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(26),
                    ),
                  ),
                  child: Text(
                    context.l10n('cancel'),
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: SizedBox(
                height: 52,
                child: ElevatedButton(
                  onPressed: _save,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(26),
                    ),
                  ),
                  child: Text(
                    isVi ? 'Lưu thành viên' : 'Save Member',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
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

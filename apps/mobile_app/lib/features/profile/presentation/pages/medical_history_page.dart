import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';

class MedicalHistoryPage extends StatefulWidget {
  const MedicalHistoryPage({super.key});

  @override
  State<MedicalHistoryPage> createState() => _MedicalHistoryPageState();
}

class _MedicalHistoryPageState extends State<MedicalHistoryPage> {
  final _auth = AuthService();
  final _familyService = FamilyService();
  bool _isLoading = true;
  String _userName = 'Alex Johnson';
  String _patientId = '#8821';

  @override
  void initState() {
    super.initState();
    _loadUserInfo();
  }

  Future<void> _loadUserInfo() async {
    try {
      final p = await _auth.getMyProfile();
      setState(() {
        _userName = p.fullName.isNotEmpty ? p.fullName : 'Alex Johnson';
        if (p.id != null) {
          final cleanId = p.id!.replaceAll('-', '');
          if (cleanId.length >= 5) {
            _patientId = '#DC-${cleanId.substring(cleanId.length - 5).toUpperCase()}';
          }
        }
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  void _showComingSoon(String feature) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(isVi ? 'Tính năng chỉnh sửa "$feature" đang được phát triển.' : 'Edit "$feature" feature is under development.'),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        duration: const Duration(seconds: 2),
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
          context.l10n('medical_info'),
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
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(20, 20, 20, 140),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── Red Header Card ──
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: AppColors.primary,
                      borderRadius: BorderRadius.circular(20),
                      boxShadow: [
                        BoxShadow(
                          color: AppColors.primary.withValues(alpha: 0.25),
                          blurRadius: 16,
                          offset: const Offset(0, 8),
                        ),
                      ],
                    ),
                    child: Row(
                      children: [
                        Container(
                          width: 56,
                          height: 56,
                          decoration: const BoxDecoration(
                            color: Colors.white,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(
                            Icons.assignment_ind_outlined,
                            color: AppColors.primary,
                            size: 28,
                          ),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _userName,
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 20,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                isVi ? 'Mã BN: $_patientId  •  Cập nhật: 2 ngày trước' : 'Patient ID: $_patientId  •  Updated: 2 days ago',
                                style: TextStyle(
                                  color: Colors.white.withValues(alpha: 0.9),
                                  fontSize: 13,
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Allergies Section
                  _buildSectionContainer(
                    title: isVi ? 'Dị ứng' : 'Allergies',
                    icon: Icons.error_outline_rounded,
                    iconColor: Colors.red,
                    onEdit: () => _showComingSoon(isVi ? 'Dị ứng' : 'Allergies'),
                    child: Column(
                      children: [
                        _buildSubItem(
                          title: 'Penicillin',
                          badgeText: isVi ? 'CAO' : 'HIGH',
                          badgeColor: const Color(0xFFFEE2E2),
                          badgeTextColor: const Color(0xFFEF4444),
                        ),
                        const SizedBox(height: 12),
                        _buildSubItem(
                          title: isVi ? 'Nhựa cao su (Latex)' : 'Latex',
                          badgeText: isVi ? 'NHẸ' : 'LOW',
                          badgeColor: const Color(0xFFF1F5F9),
                          badgeTextColor: const Color(0xFF64748B),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Chronic Conditions Section
                  _buildSectionContainer(
                    title: isVi ? 'Bệnh mãn tính' : 'Chronic Conditions',
                    icon: Icons.contact_page_outlined,
                    iconColor: Colors.red,
                    onEdit: () => _showComingSoon(isVi ? 'Bệnh mãn tính' : 'Chronic Conditions'),
                    child: Column(
                      children: [
                        _buildChronicItem(
                          title: isVi ? 'Tiểu đường tuýp 2' : 'Diabetes Type 2',
                          description: isVi ? 'Chẩn đoán: 2019  •  BS. Sarah Williams' : 'Diagnosed: 2019  •  Dr. Sarah Williams',
                        ),
                        const SizedBox(height: 12),
                        _buildChronicItem(
                          title: isVi ? 'Cao huyết áp' : 'Hypertension',
                          description: isVi ? 'Kiểm soát từ 2021  •  Theo dõi hàng ngày' : 'Controlled since 2021  •  Daily tracking',
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Current Medications Section
                  _buildSectionContainer(
                    title: isVi ? 'Thuốc đang sử dụng' : 'Current Medications',
                    icon: Icons.medication_outlined,
                    iconColor: Colors.red,
                    onEdit: () => _showComingSoon(isVi ? 'Thuốc đang sử dụng' : 'Current Medications'),
                    child: Column(
                      children: [
                        _buildMedicationItem(
                          title: 'Metformin',
                          description: isVi ? '500mg  •  2 lần / ngày' : '500mg  •  2 times / day',
                        ),
                        Divider(color: context.divider, height: 24, thickness: 1),
                        _buildMedicationItem(
                          title: 'Lisinopril',
                          description: isVi ? '10mg  •  Mỗi buổi sáng' : '10mg  •  Every morning',
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                ],
              ),
            ),
    );
  }

  Widget _buildSectionContainer({
    required String title,
    required IconData icon,
    required Color iconColor,
    required VoidCallback onEdit,
    required Widget child,
  }) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(icon, color: iconColor, size: 22),
                  const SizedBox(width: 8),
                  Text(
                    title,
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                      color: context.textPrimary,
                    ),
                  ),
                ],
              ),
              GestureDetector(
                onTap: onEdit,
                child: Icon(
                  Icons.edit_outlined,
                  color: context.textMuted,
                  size: 20,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          child,
        ],
      ),
    );
  }

  Widget _buildSubItem({
    required String title,
    required String badgeText,
    required Color badgeColor,
    required Color badgeTextColor,
  }) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: context.bg,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            title,
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w700,
              color: context.textPrimary,
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: badgeColor,
              borderRadius: BorderRadius.circular(6),
            ),
            child: Text(
              badgeText,
              style: TextStyle(
                color: badgeTextColor,
                fontSize: 11,
                fontWeight: FontWeight.w900,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildChronicItem({
    required String title,
    required String description,
  }) {
    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: context.bg,
        borderRadius: BorderRadius.circular(12),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: Container(
          decoration: const BoxDecoration(
            border: Border(
              left: BorderSide(color: AppColors.primary, width: 4),
            ),
          ),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: context.textPrimary,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                description,
                style: TextStyle(
                  fontSize: 13,
                  color: context.textSecondary,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildMedicationItem({
    required String title,
    required String description,
  }) {
    return Row(
      children: [
        Container(
          width: 40,
          height: 40,
          decoration: BoxDecoration(
            color: context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
            shape: BoxShape.circle,
          ),
          child: Icon(
            Icons.local_pharmacy_outlined,
            color: context.textSecondary,
            size: 20,
          ),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: context.textPrimary,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                description,
                style: TextStyle(
                  fontSize: 13,
                  color: context.textSecondary,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

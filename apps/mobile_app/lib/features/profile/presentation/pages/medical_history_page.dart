import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';
import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';

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

  String _ownerName = 'Alex Johnson';
  String _ownerPatientId = '#8821';
  FamilyMember? _selectedMember;
  List<FamilyMember> _familyMembers = [];
  bool _isDropdownOpen = false;

  final Map<String, List<String>> _allergiesMap = {};
  final Map<String, List<String>> _chronicMap = {};
  final Map<String, List<String>> _medicationsMap = {};

  @override
  void initState() {
    super.initState();
    _loadUserInfo();
    _loadMedicalData();
  }

  String _getPatientId(String? userId) {
    if (userId == null || userId.isEmpty) return '#DC-99201';
    final cleanId = userId.replaceAll('-', '');
    if (cleanId.length >= 5) {
      return '#DC-${cleanId.substring(cleanId.length - 5).toUpperCase()}';
    }
    return '#DC-99201';
  }

  String? _userAvatarUrl;

  String _getInitials(String name) {
    final parts = name.trim().split(' ');
    if (parts.isEmpty || parts[0].isEmpty) return 'T';
    if (parts.length == 1) return parts[0][0].toUpperCase();
    return '${parts[0][0]}${parts.last[0]}'.toUpperCase();
  }

  Widget _buildAvatarCircle(String? avatarUrl, {required double size, required bool isSelected, String? name}) {
    final initials = _getInitials(name ?? 'Tôi');
    if (avatarUrl != null && avatarUrl.isNotEmpty) {
      if (avatarUrl.startsWith('assets/')) {
        return Container(
          width: size,
          height: size,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            border: isSelected ? Border.all(color: AppColors.primary, width: 2) : null,
          ),
          child: ClipOval(
            child: Image.asset(
              avatarUrl,
              fit: BoxFit.cover,
              errorBuilder: (_, __, _) => _buildInitialsFallback(size, isSelected, initials),
            ),
          ),
        );
      }
      final url = ApiConstants.resolveAssetUrl(avatarUrl)!;
      return Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          border: isSelected ? Border.all(color: AppColors.primary, width: 2) : null,
        ),
        child: ClipOval(
          child: Image.network(
            url,
            fit: BoxFit.cover,
            errorBuilder: (_, __, _) => _buildInitialsFallback(size, isSelected, initials),
          ),
        ),
      );
    }
    return _buildInitialsFallback(size, isSelected, initials);
  }

  Widget _buildInitialsFallback(double size, bool isSelected, String initials) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: isSelected ? AppColors.primary : AppColors.primaryLight,
        shape: BoxShape.circle,
      ),
      child: Center(
        child: Text(
          initials,
          style: TextStyle(
            color: isSelected ? Colors.white : AppColors.primary,
            fontWeight: FontWeight.bold,
            fontSize: size * 0.4,
          ),
        ),
      ),
    );
  }

  Widget _buildHeaderAvatarWidget() {
    return _buildAvatarCircle(
      _selectedMember?.profilePictureUrl ?? _userAvatarUrl,
      size: 36,
      isSelected: true,
      name: _selectedMember?.fullName ?? _ownerName,
    );
  }

  Future<void> _loadUserInfo() async {
    try {
      final p = await _auth.getMyProfile();
      final cachedAvatar = await _auth.getUserAvatar();
      await _familyService.loadFromServer();
      setState(() {
        _ownerName = p.fullName.isNotEmpty ? p.fullName : 'Alex Johnson';
        _ownerPatientId = _getPatientId(p.id);
        _userAvatarUrl = (p.profilePictureUrl != null && p.profilePictureUrl!.isNotEmpty)
            ? p.profilePictureUrl
            : cachedAvatar;

        _userName = _ownerName;
        _patientId = _ownerPatientId;

        _familyMembers = _familyService.getMembers();
        _isLoading = false;
      });
    } catch (_) {
      final cachedAvatar = await _auth.getUserAvatar();
      try {
        await _familyService.loadFromServer();
      } catch (_) {}
      if (mounted) {
        setState(() {
          _userAvatarUrl = cachedAvatar;
          _familyMembers = _familyService.getMembers();
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _loadMedicalData() async {
    final memberId = _selectedMember?.id;
    final key = memberId ?? 'self';

    try {
      final jsonStr = memberId == null
          ? await _auth.getMyMedicalHistory()
          : await _auth.getFamilyMemberMedicalHistory(memberId);

      if (jsonStr != null && jsonStr.isNotEmpty) {
        final decoded = jsonDecode(jsonStr) as Map<String, dynamic>;
        
        _allergiesMap[key] = decoded['allergies'] != null
            ? List<String>.from(decoded['allergies'] as List)
            : [];
        _chronicMap[key] = decoded['chronic'] != null
            ? List<String>.from(decoded['chronic'] as List)
            : [];
        _medicationsMap[key] = decoded['medications'] != null
            ? List<String>.from(decoded['medications'] as List)
            : [];
      } else {
        // Start empty if database has no record yet
        _allergiesMap[key] = [];
        _chronicMap[key] = [];
        _medicationsMap[key] = [];
      }
      setState(() {});
    } catch (e) {
      debugPrint("Failed to load medical history: $e");
    }
  }

  Future<void> _saveMedicalData(String type, String memberKey, List<String> items) async {
    if (type == 'allergies') {
      _allergiesMap[memberKey] = items;
    } else if (type == 'chronic') {
      _chronicMap[memberKey] = items;
    } else if (type == 'medications') {
      _medicationsMap[memberKey] = items;
    }
    
    try {
      final jsonStr = jsonEncode({
        'allergies': _allergiesMap[memberKey] ?? [],
        'chronic': _chronicMap[memberKey] ?? [],
        'medications': _medicationsMap[memberKey] ?? [],
      });
      
      final memberId = _selectedMember?.id;
      if (memberId == null) {
        await _auth.updateMyMedicalHistory(jsonStr);
      } else {
        await _auth.updateFamilyMemberMedicalHistory(memberId, jsonStr);
      }
    } catch (e) {
      debugPrint("Failed to save medical history: $e");
    }
    setState(() {});
  }

  void _editSection(String type) {
    final memberKey = _selectedMember?.id ?? 'self';
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    List<String> currentItems;
    if (type == 'allergies') {
      currentItems = _allergiesMap[memberKey] ?? [];
    } else if (type == 'chronic') {
      currentItems = _chronicMap[memberKey] ?? [];
    } else {
      currentItems = _medicationsMap[memberKey] ?? [];
    }

    showDialog(
      context: context,
      builder: (context) {
        return _MedicalEditDialog(
          title: type == 'allergies'
              ? (isVi ? 'Chỉnh sửa dị ứng' : 'Edit Allergies')
              : type == 'chronic'
                  ? (isVi ? 'Chỉnh sửa bệnh mãn tính' : 'Edit Chronic Conditions')
                  : (isVi ? 'Chỉnh sửa thuốc sử dụng' : 'Edit Medications'),
          type: type,
          initialItems: currentItems,
          isVi: isVi,
          onSave: (newItems) {
            _saveMedicalData(type, memberKey, newItems);
          },
        );
      },
    );
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
    final memberKey = _selectedMember?.id ?? 'self';
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
            child: _buildHeaderAvatarWidget(),
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
                  // ── Member Dropdown ──
                  GestureDetector(
                    onTap: () {
                      setState(() {
                        _isDropdownOpen = !_isDropdownOpen;
                      });
                    },
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      decoration: BoxDecoration(
                        color: context.card,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                            blurRadius: 8,
                            offset: const Offset(0, 3),
                          ),
                        ],
                      ),
                      child: Row(
                        children: [
                          _buildAvatarCircle(
                            _selectedMember?.profilePictureUrl ?? _userAvatarUrl,
                            size: 44,
                            isSelected: true,
                            name: _selectedMember?.fullName ?? _ownerName,
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _selectedMember == null ? _ownerName : _selectedMember!.fullName,
                                  style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w800,
                                    color: context.textPrimary,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  _selectedMember == null
                                      ? '${isVi ? "Tôi" : "Self"} · $_ownerPatientId'
                                      : '${_selectedMember!.relationship} · $_patientId',
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: context.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          AnimatedRotation(
                            turns: _isDropdownOpen ? 0.5 : 0.0,
                            duration: const Duration(milliseconds: 200),
                            child: Icon(
                              Icons.keyboard_arrow_down_rounded,
                              color: context.textMuted,
                              size: 22,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  if (_isDropdownOpen) ...[
                    const SizedBox(height: 8),
                    Container(
                      decoration: BoxDecoration(
                        color: context.card,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                            blurRadius: 8,
                            offset: const Offset(0, 3),
                          ),
                        ],
                      ),
                      child: Column(
                        children: [
                          ListTile(
                            leading: _buildAvatarCircle(
                              _userAvatarUrl,
                              size: 36,
                              isSelected: _selectedMember == null,
                              name: _ownerName,
                            ),
                            title: Text(
                              _ownerName,
                              style: TextStyle(
                                fontSize: 14.5,
                                fontWeight: _selectedMember == null ? FontWeight.w800 : FontWeight.w600,
                                color: context.textPrimary,
                              ),
                            ),
                            subtitle: Text(
                              isVi ? "Tôi · $_ownerPatientId" : "Self · $_ownerPatientId",
                              style: TextStyle(
                                fontSize: 12,
                                color: context.textSecondary,
                              ),
                            ),
                            trailing: _selectedMember == null
                                ? const Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 20)
                                : null,
                            onTap: () {
                              setState(() {
                                _selectedMember = null;
                                _userName = _ownerName;
                                _patientId = _ownerPatientId;
                                _isDropdownOpen = false;
                              });
                              _loadMedicalData();
                            },
                          ),
                          if (_familyMembers.isNotEmpty) Divider(color: context.divider, height: 1),
                          ..._familyMembers.map((member) {
                            final isSelected = _selectedMember?.id == member.id;
                            final memberCode = _getPatientId(member.id);
                            return Column(
                              children: [
                                ListTile(
                                  leading: _buildAvatarCircle(
                                    member.profilePictureUrl,
                                    size: 36,
                                    isSelected: isSelected,
                                    name: member.fullName,
                                  ),
                                  title: Text(
                                    member.fullName,
                                    style: TextStyle(
                                      fontSize: 14.5,
                                      fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
                                      color: context.textPrimary,
                                    ),
                                  ),
                                  subtitle: Text(
                                    '${member.relationship} · $memberCode',
                                    style: TextStyle(
                                      fontSize: 12,
                                      color: context.textSecondary,
                                    ),
                                  ),
                                  trailing: isSelected
                                      ? const Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 20)
                                      : null,
                                  onTap: () {
                                    setState(() {
                                      _selectedMember = member;
                                      _userName = member.fullName;
                                      _patientId = memberCode;
                                      _isDropdownOpen = false;
                                    });
                                    _loadMedicalData();
                                  },
                                ),
                                if (member != _familyMembers.last)
                                  Divider(color: context.divider, height: 1),
                              ],
                            );
                          }),
                        ],
                      ),
                    ),
                  ],
                  const SizedBox(height: 16),

                  // ── Examination History Banner ──
                  GestureDetector(
                    onTap: () => context.push(AppRoutes.medicalRecords),
                    child: Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          colors: [
                            AppColors.primary,
                            AppColors.primary.withValues(alpha: 0.85),
                          ],
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                        ),
                        borderRadius: BorderRadius.circular(16),
                        boxShadow: [
                          BoxShadow(
                            color: AppColors.primary.withValues(alpha: 0.25),
                            blurRadius: 10,
                            offset: const Offset(0, 4),
                          ),
                        ],
                      ),
                      child: Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.2),
                              shape: BoxShape.circle,
                            ),
                            child: const Icon(Iconsax.health, color: Colors.white, size: 22),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  isVi ? 'Lịch sử khám & Điều trị' : 'Examination & Treatment History',
                                  style: const TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w800,
                                    color: Colors.white,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  isVi
                                      ? 'Xem chẩn đoán, tiến độ liệu trình, đơn thuốc & lịch tái khám'
                                      : 'View diagnoses, treatment progress, prescriptions & follow-ups',
                                  style: TextStyle(
                                    fontSize: 11.5,
                                    color: Colors.white.withValues(alpha: 0.9),
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const Icon(Icons.arrow_forward_ios_rounded, color: Colors.white, size: 16),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Allergies Section
                  _buildSectionContainer(
                    title: isVi ? 'Dị ứng' : 'Allergies',
                    icon: Icons.error_outline_rounded,
                    iconColor: Colors.red,
                    onEdit: () => _editSection('allergies'),
                    child: Column(
                      children: [
                        ...(_allergiesMap[memberKey] ?? []).map((item) {
                          final parts = item.split('|');
                          final name = parts.first;
                          final level = parts.length > 1 ? parts[1] : (isVi ? 'NHẸ' : 'LOW');
                          final isHigh = level.toUpperCase() == 'CAO' || level.toUpperCase() == 'HIGH';
                          return Padding(
                            padding: const EdgeInsets.only(bottom: 12.0),
                            child: _buildSubItem(
                              title: name,
                              badgeText: level,
                              badgeColor: isHigh ? const Color(0xFFFEE2E2) : const Color(0xFFF1F5F9),
                              badgeTextColor: isHigh ? const Color(0xFFEF4444) : const Color(0xFF64748B),
                            ),
                          );
                        }),
                        if ((_allergiesMap[memberKey] ?? []).isEmpty)
                          _buildSubItem(
                            title: isVi ? 'Không có dị ứng nào ghi nhận' : 'No recorded allergies',
                            badgeText: isVi ? 'KHÔNG' : 'NONE',
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
                    onEdit: () => _editSection('chronic'),
                    child: Column(
                      children: [
                        ...(_chronicMap[memberKey] ?? []).map((item) {
                          final parts = item.split('|');
                          final name = parts.first;
                          final desc = parts.length > 1 ? parts[1] : '';
                          return Padding(
                            padding: const EdgeInsets.only(bottom: 12.0),
                            child: _buildChronicItem(
                              title: name,
                              description: desc,
                            ),
                          );
                        }),
                        if ((_chronicMap[memberKey] ?? []).isEmpty)
                          _buildChronicItem(
                            title: isVi ? 'Không có bệnh lý mãn tính' : 'No chronic conditions',
                            description: isVi ? 'Sức khỏe tốt' : 'Good health status',
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
                    onEdit: () => _editSection('medications'),
                    child: Column(
                      children: [
                        ...(_medicationsMap[memberKey] ?? []).asMap().entries.map((entry) {
                          final idx = entry.key;
                          final item = entry.value;
                          final parts = item.split('|');
                          final name = parts.first;
                          final desc = parts.length > 1 ? parts[1] : '';
                          return Column(
                            children: [
                              if (idx > 0) Divider(color: context.divider, height: 24, thickness: 1),
                              _buildMedicationItem(
                                title: name,
                                description: desc,
                              ),
                            ],
                          );
                        }),
                        if ((_medicationsMap[memberKey] ?? []).isEmpty)
                          _buildMedicationItem(
                            title: isVi ? 'Không sử dụng thuốc thường xuyên' : 'No regular medications',
                            description: '-',
                          ),
                      ],
                    ),
                  ),
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
          Expanded(
            child: Text(
              title,
              style: TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.w700,
                color: context.textPrimary,
              ),
            ),
          ),
          const SizedBox(width: 8),
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

class _MedicalEditDialog extends StatefulWidget {
  final String title;
  final String type;
  final List<String> initialItems;
  final bool isVi;
  final ValueChanged<List<String>> onSave;

  const _MedicalEditDialog({
    required this.title,
    required this.type,
    required this.initialItems,
    required this.isVi,
    required this.onSave,
  });

  @override
  State<_MedicalEditDialog> createState() => _MedicalEditDialogState();
}

class _MedicalEditDialogState extends State<_MedicalEditDialog> {
  late List<Map<String, String>> _items;

  @override
  void initState() {
    super.initState();
    _items = widget.initialItems.map((item) {
      final parts = item.split('|');
      return {
        'name': parts.first,
        'detail': parts.length > 1 ? parts[1] : '',
      };
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: context.card,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  widget.title,
                  style: TextStyle(
                    color: context.textPrimary,
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.pop(context),
                ),
              ],
            ),
            const Divider(),
            const SizedBox(height: 8),
            Flexible(
              child: SingleChildScrollView(
                child: Column(
                  children: [
                    ...List.generate(_items.length, (index) {
                      final item = _items[index];
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 12.0),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              flex: 3,
                              child: TextField(
                                controller: TextEditingController(text: item['name'])
                                  ..selection = TextSelection.fromPosition(TextPosition(offset: item['name']!.length)),
                                decoration: InputDecoration(
                                  labelText: widget.type == 'allergies'
                                      ? (widget.isVi ? 'Tên dị ứng' : 'Allergy Name')
                                      : widget.type == 'chronic'
                                          ? (widget.isVi ? 'Tên bệnh' : 'Condition')
                                          : (widget.isVi ? 'Tên thuốc' : 'Medication'),
                                  isDense: true,
                                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                                ),
                                onChanged: (val) => item['name'] = val,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Expanded(
                              flex: 2,
                              child: TextField(
                                controller: TextEditingController(text: item['detail'])
                                  ..selection = TextSelection.fromPosition(TextPosition(offset: item['detail']!.length)),
                                decoration: InputDecoration(
                                  labelText: widget.type == 'allergies'
                                      ? (widget.isVi ? 'Mức độ (CAO/NHẸ)' : 'Level')
                                      : widget.type == 'chronic'
                                          ? (widget.isVi ? 'Ghi chú / Mô tả' : 'Note / Description')
                                          : (widget.isVi ? 'Liều lượng / Cách dùng' : 'Dosage / Usage'),
                                  isDense: true,
                                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                                ),
                                onChanged: (val) => item['detail'] = val,
                              ),
                            ),
                            IconButton(
                              icon: const Icon(Icons.delete_outline, color: Colors.red),
                              onPressed: () {
                                setState(() {
                                  _items.removeAt(index);
                                });
                              },
                            ),
                          ],
                        ),
                      );
                    }),
                    if (_items.isEmpty)
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 20),
                        child: Text(
                          widget.isVi ? 'Chưa có thông tin nào' : 'No records yet',
                          style: TextStyle(color: context.textMuted),
                        ),
                      ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: () {
                setState(() {
                  _items.add({'name': '', 'detail': ''});
                });
              },
              icon: const Icon(Icons.add),
              label: Text(widget.isVi ? 'Thêm dòng' : 'Add row'),
              style: OutlinedButton.styleFrom(
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
            ),
            const SizedBox(height: 20),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: Text(widget.isVi ? 'Hủy' : 'Cancel'),
                ),
                const SizedBox(width: 12),
                ElevatedButton(
                  onPressed: () {
                    final savedList = _items
                        .where((item) => item['name']!.trim().isNotEmpty)
                        .map((item) => '${item['name']!.trim()}|${item['detail']!.trim()}')
                        .toList();
                    widget.onSave(savedList);
                    Navigator.pop(context);
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                  ),
                  child: Text(widget.isVi ? 'Lưu' : 'Save', style: const TextStyle(color: Colors.white)),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

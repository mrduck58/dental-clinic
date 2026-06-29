import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
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

  Map<String, List<String>> _allergiesMap = {};
  Map<String, List<String>> _chronicMap = {};
  Map<String, List<String>> _medicationsMap = {};

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

  Future<void> _loadUserInfo() async {
    try {
      final p = await _auth.getMyProfile();
      setState(() {
        _ownerName = p.fullName.isNotEmpty ? p.fullName : 'Alex Johnson';
        _ownerPatientId = _getPatientId(p.id);

        _userName = _ownerName;
        _patientId = _ownerPatientId;

        _familyMembers = _familyService.getMembers();
        _isLoading = false;
      });
    } catch (_) {
      setState(() {
        _familyMembers = _familyService.getMembers();
        _isLoading = false;
      });
    }
  }

  Future<void> _loadMedicalData() async {
    final prefs = await SharedPreferences.getInstance();
    
    final rawAllergies = prefs.getString('medical_allergies');
    if (rawAllergies != null) {
      _allergiesMap = Map<String, List<String>>.from(
        (jsonDecode(rawAllergies) as Map).map((k, v) => MapEntry(k as String, List<String>.from(v as List))),
      );
    } else {
      _allergiesMap = {
        'self': ['Penicillin|CAO', 'Nhựa cao su (Latex)|NHẸ'],
        'member_1': ['Aspirin|CAO', 'Mạt bụi|NHẸ'],
        'member_2': ['Đậu phộng|CAO'],
        'member_3': ['Hải sản|CAO', 'Phấn hoa|NHẸ'],
      };
      await prefs.setString('medical_allergies', jsonEncode(_allergiesMap));
    }

    final rawChronic = prefs.getString('medical_chronic');
    if (rawChronic != null) {
      _chronicMap = Map<String, List<String>>.from(
        (jsonDecode(rawChronic) as Map).map((k, v) => MapEntry(k as String, List<String>.from(v as List))),
      );
    } else {
      _chronicMap = {
        'self': [
          'Tiểu đường tuýp 2|Chẩn đoán: 2019  •  BS. Sarah Williams',
          'Cao huyết áp|Kiểm soát từ 2021  •  Theo dõi hàng ngày'
        ],
        'member_1': [
          'Hen suyễn|Chẩn đoán: 2015  •  BS. Sarah Williams',
          'Đau nửa đầu (Migraine)|Kiểm soát từ 2020'
        ],
        'member_2': ['Không có bệnh lý mãn tính|Sức khỏe tốt'],
        'member_3': ['Viêm mũi dị ứng|Theo dõi theo mùa'],
      };
      await prefs.setString('medical_chronic', jsonEncode(_chronicMap));
    }

    final rawMedications = prefs.getString('medical_medications');
    if (rawMedications != null) {
      _medicationsMap = Map<String, List<String>>.from(
        (jsonDecode(rawMedications) as Map).map((k, v) => MapEntry(k as String, List<String>.from(v as List))),
      );
    } else {
      _medicationsMap = {
        'self': ['Metformin|500mg  •  2 lần / ngày', 'Lisinopril|10mg  •  Mỗi buổi sáng'],
        'member_1': ['Albuterol Inhaler|Sử dụng khi cần thiết', 'Sumatriptan|50mg  •  Khi bắt đầu đau đầu'],
        'member_2': ['Multivitamin Gummies|1 viên / ngày'],
        'member_3': ['Claritin|10mg  •  Mỗi buổi tối'],
      };
      await prefs.setString('medical_medications', jsonEncode(_medicationsMap));
    }
    
    setState(() {});
  }

  Future<void> _saveMedicalData(String type, String memberKey, List<String> items) async {
    final prefs = await SharedPreferences.getInstance();
    if (type == 'allergies') {
      _allergiesMap[memberKey] = items;
      await prefs.setString('medical_allergies', jsonEncode(_allergiesMap));
    } else if (type == 'chronic') {
      _chronicMap[memberKey] = items;
      await prefs.setString('medical_chronic', jsonEncode(_chronicMap));
    } else if (type == 'medications') {
      _medicationsMap[memberKey] = items;
      await prefs.setString('medical_medications', jsonEncode(_medicationsMap));
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
                          decoration: BoxDecoration(
                            color: Colors.white,
                            shape: BoxShape.circle,
                            image: _selectedMember != null && _selectedMember!.profilePictureUrl != null
                                ? DecorationImage(
                                    image: AssetImage(_selectedMember!.profilePictureUrl!),
                                    fit: BoxFit.cover,
                                  )
                                : null,
                          ),
                          child: _selectedMember == null || _selectedMember!.profilePictureUrl == null
                              ? const Icon(
                                  Icons.assignment_ind_outlined,
                                  color: AppColors.primary,
                                  size: 28,
                                )
                              : null,
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Theme(
                                data: Theme.of(context).copyWith(
                                  canvasColor: AppColors.primary,
                                ),
                                child: DropdownButtonHideUnderline(
                                  child: DropdownButton<FamilyMember?>(
                                    value: _selectedMember,
                                    isExpanded: true,
                                    icon: const Icon(Icons.keyboard_arrow_down_rounded, color: Colors.white, size: 24),
                                    style: const TextStyle(
                                      color: Colors.white,
                                      fontSize: 18,
                                      fontWeight: FontWeight.w800,
                                      fontFamily: 'Nunito',
                                    ),
                                    dropdownColor: AppColors.primary,
                                    selectedItemBuilder: (BuildContext context) {
                                      return [
                                        Text(
                                          '$_ownerName (${isVi ? "Bản thân" : "Self"})',
                                          style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.w800, overflow: TextOverflow.ellipsis),
                                        ),
                                        ..._familyMembers.map((member) {
                                          return Text(
                                            '${member.fullName} (${member.relationship})',
                                            style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.w800, overflow: TextOverflow.ellipsis),
                                          );
                                        }),
                                      ];
                                    },
                                    items: [
                                      DropdownMenuItem<FamilyMember?>(
                                        value: null,
                                        child: Text(
                                          '$_ownerName (${isVi ? "Bản thân" : "Self"})',
                                          style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
                                        ),
                                      ),
                                      ..._familyMembers.map((member) {
                                        return DropdownMenuItem<FamilyMember?>(
                                          value: member,
                                          child: Text(
                                            '${member.fullName} (${member.relationship})',
                                            style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
                                          ),
                                        );
                                      }),
                                    ],
                                    onChanged: (val) {
                                      setState(() {
                                        _selectedMember = val;
                                        if (val == null) {
                                          _userName = _ownerName;
                                          _patientId = _ownerPatientId;
                                        } else {
                                          _userName = val.fullName;
                                          _patientId = _getPatientId(val.id);
                                        }
                                      });
                                    },
                                  ),
                                ),
                              ),
                              const SizedBox(height: 2),
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
                        }).toList(),
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
                        }).toList(),
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
                        }).toList(),
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
                                      : (widget.isVi ? 'Mô tả / Liều lượng' : 'Detail / Dosage'),
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

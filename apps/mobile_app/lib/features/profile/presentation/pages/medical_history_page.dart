import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
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
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('T├¡nh n─âng chß╗ënh sß╗¡a "$feature" ─æang ─æ╞░ß╗úc ph├ít triß╗ân.'),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        duration: const Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: Color(0xFFDC2626), size: 20),
          onPressed: () => context.pop(),
        ),
        title: const Text(
          'Tiß╗ün sß╗¡ bß╗çnh l├╜',
          style: TextStyle(
            color: Color(0xFFDC2626),
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
            color: const Color(0xFFE2E8F0),
            height: 1,
          ),
        ),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFFDC2626)))
          : SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(20, 20, 20, 140),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ΓöÇΓöÇ Red Header Card ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: const Color(0xFFDC2626),
                      borderRadius: BorderRadius.circular(20),
                      boxShadow: [
                        BoxShadow(
                          color: const Color(0xFFDC2626).withOpacity(0.25),
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
                            color: Color(0xFFDC2626),
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
                                'M├ú BN: $_patientId  ΓÇó  Cß║¡p nhß║¡t: 2 ng├áy tr╞░ß╗¢c',
                                style: TextStyle(
                                  color: Colors.white.withOpacity(0.9),
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

                  // ΓöÇΓöÇ Allergies Section ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
                  _buildSectionContainer(
                    title: 'Dß╗ï ß╗⌐ng',
                    icon: Icons.error_outline_rounded,
                    iconColor: Colors.red,
                    onEdit: () => _showComingSoon('Dß╗ï ß╗⌐ng'),
                    child: Column(
                      children: [
                        _buildSubItem(
                          title: 'Penicillin',
                          badgeText: 'CAO',
                          badgeColor: const Color(0xFFFEE2E2),
                          badgeTextColor: const Color(0xFFEF4444),
                        ),
                        const SizedBox(height: 12),
                        _buildSubItem(
                          title: 'Nhß╗▒a cao su (Latex)',
                          badgeText: 'NHß║╕',
                          badgeColor: const Color(0xFFF1F5F9),
                          badgeTextColor: const Color(0xFF64748B),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // ΓöÇΓöÇ Chronic Conditions Section ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
                  _buildSectionContainer(
                    title: 'Bß╗çnh m├ún t├¡nh',
                    icon: Icons.contact_page_outlined,
                    iconColor: Colors.red,
                    onEdit: () => _showComingSoon('Bß╗çnh m├ún t├¡nh'),
                    child: Column(
                      children: [
                        _buildChronicItem(
                          title: 'Tiß╗âu ─æ╞░ß╗¥ng tu├╜p 2',
                          description: 'Chß║⌐n ─æo├ín: 2019  ΓÇó  BS. Sarah Williams',
                        ),
                        const SizedBox(height: 12),
                        _buildChronicItem(
                          title: 'Cao huyß║┐t ├íp',
                          description: 'Kiß╗âm so├ít tß╗½ 2021  ΓÇó  Theo d├╡i h├áng ng├áy',
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // ΓöÇΓöÇ Current Medications Section ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
                  _buildSectionContainer(
                    title: 'Thuß╗æc ─æang sß╗¡ dß╗Ñng',
                    icon: Icons.medication_outlined,
                    iconColor: Colors.red,
                    onEdit: () => _showComingSoon('Thuß╗æc ─æang sß╗¡ dß╗Ñng'),
                    child: Column(
                      children: [
                        _buildMedicationItem(
                          title: 'Metformin',
                          description: '500mg  ΓÇó  2 lß║ºn / ng├áy',
                        ),
                        const Divider(color: Color(0xFFF1F5F9), height: 24, thickness: 1),
                        _buildMedicationItem(
                          title: 'Lisinopril',
                          description: '10mg  ΓÇó  Mß╗ùi buß╗òi s├íng',
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),

                  // ΓöÇΓöÇ Connected Profiles Section ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
                  const Text(
                    'Hß╗Æ S╞á LI├èN Kß║╛T',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF94A3B8),
                      letterSpacing: 1.1,
                    ),
                  ),
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      ..._familyService.getMembers().map((member) {
                        return Padding(
                          padding: const EdgeInsets.only(right: 20.0),
                          child: GestureDetector(
                            onTap: () async {
                              final updated = await context.push(
                                '${AppRoutes.editFamilyMember}?id=${member.id}',
                                extra: member,
                              );
                              if (updated == true && mounted) {
                                setState(() {});
                              }
                            },
                            child: Column(
                              children: [
                                Container(
                                  width: 60,
                                  height: 60,
                                  decoration: BoxDecoration(
                                    shape: BoxShape.circle,
                                    border: Border.all(color: const Color(0xFFEF4444), width: 2),
                                    image: DecorationImage(
                                      image: AssetImage(member.profilePictureUrl ?? 'assets/images/bac_si_4.png'),
                                      fit: BoxFit.cover,
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  member.fullName.split(' ').last,
                                  style: const TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.w700,
                                    color: Color(0xFF334155),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      }).toList(),
                      // Add new button
                      GestureDetector(
                        onTap: () async {
                          final added = await context.push(AppRoutes.addFamilyMember);
                          if (added == true && mounted) {
                            setState(() {});
                          }
                        },
                        child: Column(
                          children: [
                            Container(
                              width: 60,
                              height: 60,
                              decoration: BoxDecoration(
                                color: const Color(0xFFF1F5F9),
                                shape: BoxShape.circle,
                                border: Border.all(color: const Color(0xFFE2E8F0)),
                              ),
                              child: const Icon(
                                Icons.add_rounded,
                                color: Color(0xFF64748B),
                                size: 28,
                              ),
                            ),
                            const SizedBox(height: 6),
                            const Text(
                              'Th├¬m mß╗¢i',
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                                color: Color(0xFF64748B),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
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
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
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
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF0F172A),
                    ),
                  ),
                ],
              ),
              GestureDetector(
                onTap: onEdit,
                child: const Icon(
                  Icons.edit_outlined,
                  color: Color(0xFF94A3B8),
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
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w700,
              color: Color(0xFF334155),
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
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(12),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: Container(
          decoration: const BoxDecoration(
            border: Border(
              left: BorderSide(color: Color(0xFFDC2626), width: 4),
            ),
          ),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF0F172A),
                ),
              ),
              const SizedBox(height: 4),
              Text(
                description,
                style: const TextStyle(
                  fontSize: 13,
                  color: Color(0xFF64748B),
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
          decoration: const BoxDecoration(
            color: Color(0xFFE2E8F0),
            shape: BoxShape.circle,
          ),
          child: const Icon(
            Icons.local_pharmacy_outlined,
            color: Color(0xFF64748B),
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
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF0F172A),
                ),
              ),
              const SizedBox(height: 2),
              Text(
                description,
                style: const TextStyle(
                  fontSize: 13,
                  color: Color(0xFF64748B),
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

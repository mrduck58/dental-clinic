import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';

class SelectPatientPage extends StatefulWidget {
  const SelectPatientPage({super.key});

  @override
  State<SelectPatientPage> createState() => _SelectPatientPageState();
}

class _SelectPatientPageState extends State<SelectPatientPage> {
  final _auth = AuthService();
  List<PatientInfo> _patients = [];
  bool _loading = true;
  String? _selectedId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    try {
      final profile = await _auth.getMyProfile();
      final me = PatientInfo(
        id: 'self',
        name: profile.fullName.isEmpty 
            ? (isVi ? 'Người dùng' : 'User') 
            : profile.fullName.toUpperCase(),
        relationship: isVi ? 'Tôi' : 'Self',
        phone: profile.phoneNumber,
        dob: _formatDob(profile.dateOfBirth),
        gender: profile.gender ?? 'Nam',
      );
      if (mounted) setState(() { _patients = [me]; _loading = false; });
    } catch (_) {
      // fallback: dùng tên đã lưu local nếu API lỗi
      final name = await _auth.getUserName() ?? '';
      final me = PatientInfo(
        id: 'self',
        name: name.isEmpty ? (isVi ? 'Người dùng' : 'User') : name.toUpperCase(),
        relationship: isVi ? 'Tôi' : 'Self',
        gender: 'Nam',
      );
      if (mounted) setState(() { _patients = [me]; _loading = false; });
    }
  }

  String? _formatDob(String? iso) {
    if (iso == null) return null;
    final parts = iso.split('-');
    if (parts.length < 3) return null;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }

  void _select(int index) {
    setState(() => _selectedId = _patients[index].id);
    context.push(
      AppRoutes.bookingSelectService,
      extra: BookingDraft(patient: _patients[index]),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: context.l10n('book_appointment'), onBack: () => context.pop()),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : CustomScrollView(
        slivers: [
          // ── Header + avatar row ──────────────────────────────
          SliverToBoxAdapter(
            child: ColoredBox(
              color: context.card,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // "Chọn hồ sơ" + nút thêm
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 20, 16, 14),
                    child: Row(
                      children: [
                        Text(
                          context.l10n('select_profile'),
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                        const Spacer(),
                        GestureDetector(
                          onTap: () async {
                            final added = await context.push(AppRoutes.addFamilyMember);
                            if (added == true && mounted) {
                              _load();
                            }
                          },
                          child: Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: AppColors.primary,
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Icon(
                                  Iconsax.user_cirlce_add,
                                  color: Colors.white,
                                  size: 20,
                                ),
                                const SizedBox(width: 6),
                                Text(
                                  context.l10n('add_new_profile'),
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontSize: 13,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),

                  // Avatar scroll row
                  SizedBox(
                    height: 100,
                    child: ListView.separated(
                      scrollDirection: Axis.horizontal,
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      itemCount: _patients.length,
                      separatorBuilder: (_, _) => const SizedBox(width: 20),
                      itemBuilder: (_, i) {
                        final p = _patients[i];
                        final active = _selectedId == p.id;
                        final avatarBg = active
                            ? (context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight)
                            : context.bg;

                        return GestureDetector(
                          onTap: () => _select(i),
                          child: Column(
                            children: [
                              AnimatedContainer(
                                duration: const Duration(milliseconds: 200),
                                width: 60,
                                height: 60,
                                decoration: BoxDecoration(
                                  color: avatarBg,
                                  shape: BoxShape.circle,
                                  border: Border.all(
                                    color: active
                                        ? AppColors.primary
                                        : context.divider,
                                    width: active ? 2.5 : 1.5,
                                  ),
                                ),
                                child: Icon(
                                  Iconsax.profile_circle,
                                  color: active
                                      ? AppColors.primary
                                      : context.textSecondary,
                                  size: 30,
                                ),
                              ),
                              const SizedBox(height: 5),
                              Text(
                                p.name.split(' ').last,
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w700,
                                  color: active
                                      ? AppColors.primary
                                      : context.textPrimary,
                                ),
                              ),
                              if (p.patientCode != null)
                                Text(
                                  p.patientCode!,
                                  style: TextStyle(
                                    fontSize: 10,
                                    color: active
                                        ? AppColors.primary
                                        : context.textSecondary,
                                  ),
                                ),
                            ],
                          ),
                        );
                      },
                    ),
                  ),
                  const SizedBox(height: 16),
                ],
              ),
            ),
          ),

          // ── Patient cards ─────────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
            sliver: SliverList(
              delegate: SliverChildBuilderDelegate(
                (_, i) => _PatientCard(
                  patient: _patients[i],
                  selected: _selectedId == _patients[i].id,
                  onTap: () => _select(i),
                ),
                childCount: _patients.length,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ─── Patient Card ─────────────────────────────────────────────────────────────

class _PatientCard extends StatelessWidget {
  final PatientInfo patient;
  final bool selected;
  final VoidCallback onTap;

  const _PatientCard({
    required this.patient,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        margin: const EdgeInsets.only(bottom: 10),
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: selected ? AppColors.primary : context.divider),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Name row ────────────────────────────────────────────────────
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 12, 14),
              child: Row(
                children: [
                  Icon(
                    Iconsax.element_4,
                    size: 18,
                    color: selected ? AppColors.primary : context.textSecondary,
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      patient.name,
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w800,
                        color: selected
                            ? (context.isDark ? Colors.white : AppColors.primary)
                            : context.textPrimary,
                      ),
                    ),
                  ),
                  Icon(
                    Iconsax.arrow_right_2,
                    size: 20,
                    color: context.textSecondary,
                  ),
                ],
              ),
            ),

            // ── Info pill ──────────────────────────────────
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 14),
              child: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 10,
                ),
                decoration: BoxDecoration(
                  color: context.bg,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Row(
                  children: [
                    // Mã bệnh nhân
                    if (patient.patientCode != null) ...[
                      Icon(
                        Iconsax.personalcard,
                        size: 15,
                        color: context.textSecondary,
                      ),
                      const SizedBox(width: 5),
                      Text(
                        patient.patientCode!,
                        style: TextStyle(
                          fontSize: 13,
                          color: context.textSecondary,
                        ),
                      ),
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 10),
                        child: Container(
                          width: 1,
                          height: 14,
                          color: context.divider,
                        ),
                      ),
                    ],
                    // SĐT
                    if (patient.phone != null) ...[
                      Icon(
                        Iconsax.call,
                        size: 15,
                        color: context.textSecondary,
                      ),
                      const SizedBox(width: 5),
                      Text(
                        patient.phone!,
                        style: TextStyle(
                          fontSize: 13,
                          color: context.textSecondary,
                        ),
                      ),
                    ],
                    const Spacer(),
                    // Badge quan hệ
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: selected
                            ? (context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight)
                            : context.card,
                        borderRadius: BorderRadius.circular(999),
                        border: Border.all(
                          color: selected
                              ? AppColors.primary
                              : context.divider,
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Iconsax.user,
                            size: 12,
                            color: selected
                                ? AppColors.primary
                                : context.textSecondary,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            patient.relationship,
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: selected
                                  ? (context.isDark ? Colors.white : AppColors.primary)
                                  : context.textSecondary,
                            ),
                          ),
                        ],
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
}

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';

class SelectPatientPage extends StatefulWidget {
  /// Draft điền sẵn từ chatbot AI (dịch vụ/ngày/triệu chứng đã trích xuất được từ hội thoại,
  /// nếu có) — null khi vào từ luồng đặt lịch bình thường ở trang chủ.
  final BookingDraft? initialDraft;

  const SelectPatientPage({super.key, this.initialDraft});

  @override
  State<SelectPatientPage> createState() => _SelectPatientPageState();
}

class _SelectPatientPageState extends State<SelectPatientPage> {
  final _auth = AuthService();
  final _bookingService = BookingService();
  List<PatientInfo> _patients = [];
  bool _loading = true;
  String? _selectedId;
  BookingEligibility? _eligibility;

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
        avatarUrl: profile.profilePictureUrl,
      );

      await FamilyService().loadFromServer();
      final family = FamilyService().getMembers().map((m) {
        return PatientInfo(
          id: m.id,
          name: m.fullName.toUpperCase(),
          relationship: m.relationship,
          phone: m.phoneNumber,
          dob: '${m.dateOfBirth.day.toString().padLeft(2, '0')}/${m.dateOfBirth.month.toString().padLeft(2, '0')}/${m.dateOfBirth.year}',
          gender: m.gender,
          avatarUrl: m.profilePictureUrl,
        );
      }).toList();

      final eligibility = await _bookingService.getBookingEligibility().catchError((_) => const BookingEligibility(
        activeBookingCount: 0,
        maxActiveBookings: 2,
        canBookNew: true,
        isInCooldown: false,
        cooldownRemainingSeconds: 0,
        cancellationCount: 0,
        rescheduleCount: 0,
      ));

      if (mounted) {
        setState(() {
          _patients = [me, ...family];
          _eligibility = eligibility;
          _loading = false;
        });
      }
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

  Future<void> _select(int index) async {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    // 1. Kiểm tra nếu tài khoản đã có 2 lịch hẹn đang hoạt động
    if (_eligibility != null && _eligibility!.activeBookingCount >= 2) {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          title: Row(
            children: [
              const Icon(Icons.error_outline_rounded, color: Color(0xFFDC2626)),
              const SizedBox(width: 8),
              Text(isVi ? 'Đạt giới hạn đặt lịch' : 'Booking Limit Reached', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            ],
          ),
          content: Text(
            isVi
                ? 'Tài khoản của bạn đã có 2 lịch hẹn đang hoạt động (tối đa 2 lịch cùng lúc).\n\nVui lòng hoàn thành hoặc dời/hủy bớt lịch hẹn cũ trước khi đặt lịch mới.'
                : 'You already have 2 active appointments (maximum 2 allowed). Please complete or cancel existing appointments first.',
            style: const TextStyle(fontSize: 14, height: 1.4),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: Text(isVi ? 'Đóng' : 'Close')),
          ],
        ),
      );
      return;
    }

    // 2. Kiểm tra nếu bệnh nhân được chọn đang bị cooldown 30 phút
    final patientId = _patients[index].id == 'self' ? null : _patients[index].id;
    final patientEligibility = await _bookingService.getBookingEligibility(patientId: patientId);
    if (patientEligibility.isInCooldown) {
      if (!mounted) return;
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          title: Row(
            children: [
              const Icon(Icons.access_time_rounded, color: Color(0xFFD97706)),
              const SizedBox(width: 8),
              Text(isVi ? 'Thời gian chờ (Cooldown)' : 'Cooldown Active', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            ],
          ),
          content: Text(
            isVi
                ? 'Bệnh nhân này đang trong thời gian chờ sau khi đổi/hủy lịch (còn ${patientEligibility.cooldownRemainingMinutes} phút).\n\nVui lòng thử lại sau hoặc chọn bệnh nhân khác trong gia đình.'
                : 'This patient is in a 30-minute cooldown period (${patientEligibility.cooldownRemainingMinutes} mins remaining). Please try again later or select another family member.',
            style: const TextStyle(fontSize: 14, height: 1.4),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: Text(isVi ? 'Đóng' : 'Close')),
          ],
        ),
      );
      return;
    }

    setState(() => _selectedId = _patients[index].id);

    final initial = widget.initialDraft;
    var draft = initial != null
        ? initial.copyWith(patient: _patients[index])
        : BookingDraft(patient: _patients[index]);

    if (!mounted) return;

    // Đã biết đủ bác sĩ + ngày + khung giờ còn trống (từ chatbot hoặc khi sửa đổi lịch) → vào thẳng màn tổng hợp/xác nhận.
    if (draft.doctor != null && draft.date != null && draft.timeSlot != null) {
      try {
        final res = await _bookingService.holdSlot(
          patientId: _patients[index].id == 'self' ? '' : _patients[index].id,
          dentistId: draft.doctor!.id,
          date: draft.date!,
          timeSlot: draft.timeSlot!.range,
          serviceId: draft.service?.id,
        );
        if (res.isSuccess && res.expiresAt != null) {
          draft = draft.copyWith(holdExpiresAt: res.expiresAt);
        }
      } catch (e) {
        debugPrint('Hold transfer error: $e');
      }
      if (!mounted) return;
      context.push(AppRoutes.bookingReview, extra: draft);
    } else if (draft.doctor != null && draft.date != null) {
      context.push(AppRoutes.bookingSelectTimeSlot, extra: draft);
    } else if (draft.service != null && draft.date != null) {
      context.push(AppRoutes.bookingSelectDoctor, extra: draft);
    } else if (draft.service != null) {
      context.push(AppRoutes.bookingSelectDatetime, extra: draft);
    } else {
      context.push(AppRoutes.bookingSelectService, extra: draft);
    }
  }

  Widget _buildInitialsBadge(String name, bool active, {double size = 28, double fontSize = 12}) {
    final parts = name.trim().split(' ');
    String initial = 'T';
    if (parts.isNotEmpty && parts[0].isNotEmpty) {
      initial = parts.length == 1 ? parts[0][0] : '${parts[0][0]}${parts.last[0]}';
    }
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: active ? AppColors.primary : AppColors.primaryLight,
        shape: BoxShape.circle,
      ),
      child: Center(
        child: Text(
          initial.toUpperCase(),
          style: TextStyle(
            color: active ? Colors.white : AppColors.primary,
            fontWeight: FontWeight.bold,
            fontSize: fontSize,
          ),
        ),
      ),
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
          if (widget.initialDraft?.holdExpiresAt != null)
            SliverToBoxAdapter(
              child: HoldCountdownBanner(holdExpiresAt: widget.initialDraft?.holdExpiresAt),
            ),
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
                                child: ClipOval(
                                  child: p.avatarUrl != null && p.avatarUrl!.isNotEmpty
                                      ? Image.network(
                                          ApiConstants.resolveAssetUrl(p.avatarUrl)!,
                                          fit: BoxFit.cover,
                                          errorBuilder: (_, __, ___) => _buildInitialsBadge(p.name, active, size: 60, fontSize: 20),
                                        )
                                      : _buildInitialsBadge(p.name, active, size: 60, fontSize: 20),
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

  Widget _buildInitialsBadge(String name, bool active, {double size = 28, double fontSize = 12}) {
    final parts = name.trim().split(' ');
    String initial = 'T';
    if (parts.isNotEmpty && parts[0].isNotEmpty) {
      initial = parts.length == 1 ? parts[0][0] : '${parts[0][0]}${parts.last[0]}';
    }
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: active ? AppColors.primary : AppColors.primaryLight,
        shape: BoxShape.circle,
      ),
      child: Center(
        child: Text(
          initial.toUpperCase(),
          style: TextStyle(
            color: active ? Colors.white : AppColors.primary,
            fontWeight: FontWeight.bold,
            fontSize: fontSize,
          ),
        ),
      ),
    );
  }

  Widget _buildPatientAvatar(PatientInfo patient, bool selected) {
    if (patient.avatarUrl != null && patient.avatarUrl!.isNotEmpty) {
      final avatarUrl = patient.avatarUrl!;
      if (avatarUrl.startsWith('assets/')) {
        return Container(
          width: 28,
          height: 28,
          decoration: const BoxDecoration(shape: BoxShape.circle),
          child: ClipOval(
            child: Image.asset(
              avatarUrl,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => _buildInitialsBadge(patient.name, selected, size: 28, fontSize: 11),
            ),
          ),
        );
      }
      final url = ApiConstants.resolveAssetUrl(avatarUrl)!;
      return Container(
        width: 28,
        height: 28,
        decoration: const BoxDecoration(shape: BoxShape.circle),
        child: ClipOval(
          child: Image.network(
            url,
            fit: BoxFit.cover,
            errorBuilder: (_, __, ___) => _buildInitialsBadge(patient.name, selected, size: 28, fontSize: 11),
          ),
        ),
      );
    }
    return _buildInitialsBadge(patient.name, selected, size: 28, fontSize: 11);
  }

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
                  _buildPatientAvatar(patient, selected),
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

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';

class SelectDoctorPage extends StatefulWidget {
  final BookingDraft draft;
  const SelectDoctorPage({super.key, required this.draft});

  @override
  State<SelectDoctorPage> createState() => _SelectDoctorPageState();
}

class _SelectDoctorPageState extends State<SelectDoctorPage> {
  final _service = BookingService();
  late DateTime _currentDate;
  List<ApiDoctorWithSlots> _doctors = [];
  bool _loading = true;
  String? _error;

  static const _weekdaysVi = [
    '', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'
  ];
  static const _weekdaysShortVi = [
    '', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'
  ];
  static const _weekdaysEn = [
    '', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'
  ];
  static const _weekdaysShortEn = [
    '', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'
  ];

  @override
  void initState() {
    super.initState();
    _currentDate = widget.draft.date ?? DateTime.now();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final list = await _service.getDoctorsWithSlots(_currentDate);
      if (mounted) {
        setState(() {
          _doctors = List.from(list);
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = 'Không thể tải thông tin bác sĩ.';
          _loading = false;
        });
      }
    }
  }

  void _onDateSelected(DateTime newDate) {
    if (_currentDate.year == newDate.year &&
        _currentDate.month == newDate.month &&
        _currentDate.day == newDate.day) {
      return;
    }
    setState(() {
      _currentDate = newDate;
    });
    _load();
  }

  Future<void> _pickCustomDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _currentDate.isBefore(now) ? now : _currentDate,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: DateTime(now.year + 1, now.month, now.day),
    );
    if (picked != null) {
      _onDateSelected(picked);
    }
  }

  String _fmtDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  String _dayLabel(DateTime d, bool isVi) {
    final weekdays = isVi ? _weekdaysVi : _weekdaysEn;
    return '${weekdays[d.weekday]}, ${_fmtDate(d)}';
  }

  static String _weekdayShort(DateTime d, bool isVi) {
    final list = isVi ? _weekdaysShortVi : _weekdaysShortEn;
    return list[d.weekday];
  }

  void _onSelectDoctor(ApiDoctorWithSlots doc, bool isVi) {
    final availableCount = doc.slots.where((s) => !s.isBooked).length;
    if (availableCount == 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isVi
                ? 'Bác sĩ ${doc.fullName} đã kín lịch vào ngày này. Vui lòng chọn bác sĩ khác hoặc đổi ngày khám.'
                : 'Dr. ${doc.fullName} has no available slots on this date. Please choose another doctor or date.',
          ),
          behavior: SnackBarBehavior.floating,
          backgroundColor: const Color(0xFFDC2626),
        ),
      );
      return;
    }

    final doctorInfo = doc.toDoctorInfo();
    final updatedDraft = widget.draft.copyWith(
      date: _currentDate,
      doctor: doctorInfo,
    );

    context.push(AppRoutes.bookingSelectTimeSlot, extra: updatedDraft);
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Chọn Nha sĩ' : 'Select Dentist'),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        isVi ? 'Không thể tải danh sách nha sĩ.' : 'Unable to load dentists.',
                        style: TextStyle(color: context.textSecondary),
                      ),
                      const SizedBox(height: 12),
                      TextButton(
                        onPressed: _load,
                        child: Text(isVi ? 'Thử lại' : 'Retry'),
                      ),
                    ],
                  ),
                )
              : CustomScrollView(
                  slivers: [
                    if (widget.draft.holdExpiresAt != null)
                      SliverToBoxAdapter(
                        child: HoldCountdownBanner(holdExpiresAt: widget.draft.holdExpiresAt),
                      ),
                    // ── Date Selector Strip ─────────────────────────────────
                    SliverToBoxAdapter(
                      child: Container(
                        margin: const EdgeInsets.fromLTRB(16, 14, 16, 10),
                        decoration: BoxDecoration(
                          color: context.card,
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(color: context.divider),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Padding(
                              padding: const EdgeInsets.fromLTRB(14, 12, 14, 6),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Row(
                                    children: [
                                      const Icon(Iconsax.calendar_1, size: 16, color: AppColors.primary),
                                      const SizedBox(width: 6),
                                      Text(
                                        _dayLabel(_currentDate, isVi),
                                        style: TextStyle(
                                          fontSize: 14,
                                          fontWeight: FontWeight.w700,
                                          color: context.textPrimary,
                                        ),
                                      ),
                                    ],
                                  ),
                                  GestureDetector(
                                    onTap: _pickCustomDate,
                                    child: Text(
                                      isVi ? 'Đổi ngày' : 'Change',
                                      style: const TextStyle(
                                        fontSize: 12,
                                        fontWeight: FontWeight.w700,
                                        color: AppColors.primary,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Divider(color: context.divider, height: 1),
                            SizedBox(
                              height: 66,
                              child: ListView.builder(
                                scrollDirection: Axis.horizontal,
                                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                                itemCount: 14,
                                itemBuilder: (context, index) {
                                  final d = DateTime.now().add(Duration(days: index));
                                  final isSelected = d.year == _currentDate.year &&
                                      d.month == _currentDate.month &&
                                      d.day == _currentDate.day;
                                  final isToday = index == 0;
                                  final shortWeekday = _weekdayShort(d, isVi);

                                  return GestureDetector(
                                    onTap: () => _onDateSelected(d),
                                    child: Container(
                                      width: 60,
                                      margin: const EdgeInsets.symmetric(horizontal: 4),
                                      decoration: BoxDecoration(
                                        color: isSelected
                                            ? AppColors.primary
                                            : (context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC)),
                                        borderRadius: BorderRadius.circular(10),
                                        border: Border.all(
                                          color: isSelected ? AppColors.primary : context.divider,
                                        ),
                                      ),
                                      child: Column(
                                        mainAxisAlignment: MainAxisAlignment.center,
                                        children: [
                                          Text(
                                            isToday ? (isVi ? 'Hôm nay' : 'Today') : shortWeekday,
                                            style: TextStyle(
                                              fontSize: 10,
                                              fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                                              color: isSelected
                                                  ? Colors.white.withValues(alpha: 0.9)
                                                  : context.textSecondary,
                                            ),
                                          ),
                                          const SizedBox(height: 2),
                                          Text(
                                            '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}',
                                            style: TextStyle(
                                              fontSize: 12,
                                              fontWeight: FontWeight.w800,
                                              color: isSelected ? Colors.white : context.textPrimary,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  );
                                },
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),

                    // ── Doctor Cards List ───────────────────────────────────
                    if (_doctors.isEmpty)
                      SliverFillRemaining(
                        hasScrollBody: false,
                        child: Center(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 32.0),
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(16),
                                  decoration: const BoxDecoration(
                                    color: AppColors.primaryLight,
                                    shape: BoxShape.circle,
                                  ),
                                  child: const Icon(Iconsax.calendar_remove, size: 44, color: AppColors.primary),
                                ),
                                const SizedBox(height: 16),
                                Text(
                                  widget.draft.doctor != null
                                      ? (isVi
                                          ? 'Bác sĩ không có lịch làm việc vào ngày này'
                                          : 'Doctor has no schedule on this date')
                                      : (isVi
                                          ? 'Không có nha sĩ nào có lịch trực vào ngày này'
                                          : 'No dentists available on this date'),
                                  textAlign: TextAlign.center,
                                  style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w700,
                                    color: context.textPrimary,
                                  ),
                                ),
                                const SizedBox(height: 8),
                                Text(
                                  isVi
                                      ? 'Vui lòng chọn ngày khác ở thanh ngày bên trên để tiếp tục.'
                                      : 'Please select another date from the bar above.',
                                  textAlign: TextAlign.center,
                                  style: TextStyle(
                                    fontSize: 13,
                                    color: context.textSecondary,
                                  ),
                                ),
                                const SizedBox(height: 16),
                                ElevatedButton.icon(
                                  onPressed: _pickCustomDate,
                                  icon: const Icon(Iconsax.calendar_1, size: 18),
                                  label: Text(isVi ? 'Mở lịch chọn ngày' : 'Open calendar'),
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: AppColors.primary,
                                    foregroundColor: Colors.white,
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      )
                    else
                      SliverPadding(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                        sliver: SliverList(
                          delegate: SliverChildBuilderDelegate(
                            (_, i) {
                              final doc = _doctors[i];
                              final isPreferred = widget.draft.preferredDentistId != null &&
                                  doc.dentistId == widget.draft.preferredDentistId;
                              return _DoctorItemCard(
                                doctor: doc,
                                isPreferred: isPreferred,
                                isVi: isVi,
                                onTap: () => _onSelectDoctor(doc, isVi),
                              );
                            },
                            childCount: _doctors.length,
                          ),
                        ),
                      ),

                    const SliverToBoxAdapter(child: SizedBox(height: 40)),
                  ],
                ),
    );
  }
}

// ─── Doctor Item Card ─────────────────────────────────────────────────────────

class _DoctorItemCard extends StatelessWidget {
  final ApiDoctorWithSlots doctor;
  final bool isPreferred;
  final bool isVi;
  final VoidCallback onTap;

  const _DoctorItemCard({
    required this.doctor,
    required this.isPreferred,
    required this.isVi,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final availableSlots = doctor.slots.where((s) => !s.isBooked).length;
    final totalSlots = doctor.slots.length;
    final isFullyBooked = availableSlots == 0 && totalSlots > 0;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isPreferred
              ? AppColors.primary
              : (context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
          width: isPreferred ? 1.8 : 1,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(16),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                _DoctorAvatar(avatarUrl: doctor.avatarUrl, size: 56),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Flexible(
                            child: Text(
                              doctor.fullName,
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w800,
                                color: context.isDark ? Colors.white : AppColors.primary,
                              ),
                            ),
                          ),
                          if (isPreferred) ...[
                            const SizedBox(width: 6),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                              decoration: BoxDecoration(
                                color: AppColors.primary,
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: Text(
                                isVi ? 'Ưa thích' : 'Preferred',
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 10,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                          ],
                        ],
                      ),
                      const SizedBox(height: 3),
                      Text(
                        doctor.specialization,
                        style: TextStyle(
                          fontSize: 13,
                          color: context.textSecondary,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        runSpacing: 4,
                        children: [
                          if (doctor.experienceYears > 0)
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                              decoration: BoxDecoration(
                                color: const Color(0xFFFEF3C7),
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  const Icon(Iconsax.award, size: 12, color: Color(0xFFD97706)),
                                  const SizedBox(width: 4),
                                  Text(
                                    isVi ? '${doctor.experienceYears} năm KN' : '${doctor.experienceYears} yrs',
                                    style: const TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w700,
                                      color: Color(0xFF92400E),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                            decoration: BoxDecoration(
                              color: isFullyBooked
                                  ? const Color(0xFFFEE2E2)
                                  : (context.isDark ? const Color(0xFF1E293B) : const Color(0xFFECFDF5)),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(
                                  isFullyBooked ? Iconsax.close_circle : Iconsax.clock,
                                  size: 12,
                                  color: isFullyBooked ? const Color(0xFFDC2626) : const Color(0xFF059669),
                                ),
                                const SizedBox(width: 4),
                                Text(
                                  isFullyBooked
                                      ? (isVi ? 'Đã kín lịch' : 'Full')
                                      : (isVi ? '$availableSlots ca khám trống' : '$availableSlots slots open'),
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w700,
                                    color: isFullyBooked ? const Color(0xFFB91C1C) : const Color(0xFF047857),
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
                const SizedBox(width: 8),
                Container(
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    color: isFullyBooked
                        ? (context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9))
                        : AppColors.primary.withValues(alpha: context.isDark ? 0.2 : 0.1),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(
                    Iconsax.arrow_right_3,
                    size: 16,
                    color: isFullyBooked ? context.textMuted : AppColors.primary,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _DoctorAvatar extends StatelessWidget {
  final String? avatarUrl;
  final double size;
  const _DoctorAvatar({this.avatarUrl, this.size = 46});

  @override
  Widget build(BuildContext context) {
    if (avatarUrl != null && avatarUrl!.isNotEmpty) {
      if (avatarUrl!.startsWith('assets/')) {
        return ClipOval(
          child: Image.asset(avatarUrl!, width: size, height: size, fit: BoxFit.cover),
        );
      }
      final resolved = ApiConstants.resolveAssetUrl(avatarUrl);
      if (resolved != null) {
        return ClipOval(
          child: Image.network(
            resolved,
            width: size,
            height: size,
            fit: BoxFit.cover,
            errorBuilder: (_, __, ___) => _fallback(),
          ),
        );
      }
    }
    return _fallback();
  }

  Widget _fallback() => Container(
        width: size,
        height: size,
        decoration: const BoxDecoration(
          color: AppColors.primaryLight,
          shape: BoxShape.circle,
        ),
        child: Icon(Iconsax.user, color: AppColors.primary, size: size * 0.55),
      );
}

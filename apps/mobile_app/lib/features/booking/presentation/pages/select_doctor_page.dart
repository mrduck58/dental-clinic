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
        final selectedDocId = widget.draft.doctor?.id ?? widget.draft.preferredDentistId;
        List<ApiDoctorWithSlots> resultList;
        if (selectedDocId != null) {
          // Khi đặt lịch từ bác sĩ cụ thể: Chỉ hiển thị lịch của đúng bác sĩ đó
          resultList = list.where((d) => d.dentistId == selectedDocId).toList();
        } else {
          resultList = List.from(list);
        }
        setState(() { _doctors = resultList; _loading = false; });
      }
    } catch (e) {
      if (mounted) setState(() { _error = 'Không thể tải thông tin bác sĩ.'; _loading = false; });
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
    return '${_fmtDate(d)} - ${weekdays[d.weekday]}';
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Chọn khung giờ khám' : 'Select Time Slot'),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        isVi ? 'Không thể tải thông tin bác sĩ.' : 'Unable to load doctor info.',
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
                    // ── Header ────────────────────────────────────────────
                    SliverToBoxAdapter(
                      child: ColoredBox(
                        color: context.card,
                        child: Padding(
                          padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      isVi ? 'Chọn khung giờ khám' : 'Select Time Slot',
                                      style: TextStyle(
                                        fontSize: 15,
                                        fontWeight: FontWeight.w700,
                                        color: context.textPrimary,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    RichText(
                                      text: TextSpan(
                                        style: TextStyle(
                                          fontSize: 13,
                                          color: context.textSecondary,
                                          fontStyle: FontStyle.italic,
                                        ),
                                        children: [
                                          TextSpan(
                                            text: isVi 
                                                ? 'Bấm chọn ngày và giờ '
                                                : 'Select date & slot in ',
                                          ),
                                          const TextSpan(
                                            text: 'màu đỏ',
                                            style: TextStyle(
                                              color: AppColors.primary,
                                              fontWeight: FontWeight.w600,
                                              fontStyle: FontStyle.italic,
                                            ),
                                          ),
                                          TextSpan(
                                            text: isVi ? ' để đặt khám' : ' to book',
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              GestureDetector(
                                onTap: _pickCustomDate,
                                child: Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                                  decoration: BoxDecoration(
                                    color: AppColors.primary.withValues(alpha: context.isDark ? 0.2 : 0.1),
                                    borderRadius: BorderRadius.circular(8),
                                  ),
                                  child: Row(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      const Icon(Iconsax.calendar_edit, size: 14, color: AppColors.primary),
                                      const SizedBox(width: 4),
                                      Text(
                                        isVi ? 'Đổi ngày' : 'Change date',
                                        style: const TextStyle(
                                          fontSize: 12,
                                          fontWeight: FontWeight.w700,
                                          color: AppColors.primary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),

                    // ── Doctor cards ──────────────────────────────────────
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
                                          ? 'Bác sĩ ${widget.draft.doctor!.fullName.replaceAll(RegExp(r"^(Bác sĩ|BS|Bs|Dr)\.?\s*", caseSensitive: false), "").trim()} không có lịch khám trống vào ngày này'
                                          : 'Doctor ${widget.draft.doctor!.fullName.replaceAll(RegExp(r"^(Bác sĩ|BS|Bs|Dr)\.?\s*", caseSensitive: false), "").trim()} has no available slots on this date')
                                      : (isVi
                                          ? 'Không có bác sĩ nào có lịch khám trống vào ngày này'
                                          : 'No doctors available on this date'),
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
                                      ? 'Vui lòng chọn ngày khác ở thanh ngày bên trên hoặc chọn ngày trong lịch để tiếp tục.'
                                      : 'Please select another date from the bar above or open the calendar.',
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
                      SliverList(
                        delegate: SliverChildBuilderDelegate(
                          (_, i) => _DoctorSlotCard(
                            doctor: _doctors[i],
                            date: _currentDate,
                            dayLabel: _dayLabel(_currentDate, isVi),
                            isPreferred: widget.draft.preferredDentistId != null &&
                                _doctors[i].dentistId == widget.draft.preferredDentistId,
                            onDateChange: _onDateSelected,
                            onSlotSelected: (slot) {
                              final doctorInfo = _doctors[i].toDoctorInfo();
                              final draft2 = widget.draft.copyWith(
                                date: _currentDate,
                                doctor: doctorInfo,
                                timeSlot: slot.toTimeSlot(),
                              );
                              context.push(AppRoutes.bookingReview, extra: draft2);
                            },
                          ),
                          childCount: _doctors.length,
                        ),
                      ),
                    const SliverToBoxAdapter(child: SizedBox(height: 24)),
                  ],
                ),
    );
  }
}

// ─── Doctor + Slot Card ───────────────────────────────────────────────────────

class _DoctorSlotCard extends StatelessWidget {
  final ApiDoctorWithSlots doctor;
  final DateTime date;
  final String dayLabel;
  final bool isPreferred;
  final void Function(DateTime) onDateChange;
  final void Function(ApiTimeSlot) onSlotSelected;

  const _DoctorSlotCard({
    required this.doctor,
    required this.date,
    required this.dayLabel,
    this.isPreferred = false,
    required this.onDateChange,
    required this.onSlotSelected,
  });

  static const _periodOrder = ['Buổi sáng', 'Buổi chiều', 'Buổi tối'];

  static String _periodLabel(String period, bool isVi) {
    if (isVi) return period.isEmpty ? 'Khác' : period;
    return switch (period) {
      'Buổi sáng' => 'Morning',
      'Buổi chiều' => 'Afternoon',
      'Buổi tối' => 'Evening',
      _ => 'Other',
    };
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    final now = DateTime.now();
    final isDateToday = date.year == now.year && date.month == now.month && date.day == now.day;

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 12, 16, 0),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: isPreferred ? AppColors.primary : context.divider,
          width: isPreferred ? 1.6 : 1,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Doctor header ──────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                _DoctorAvatar(avatarUrl: doctor.avatarUrl),
                const SizedBox(width: 12),
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
                                fontSize: 14,
                                fontWeight: FontWeight.w800,
                                color: context.isDark ? Colors.white : AppColors.primary,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Text(
                        doctor.specialization,
                        style: TextStyle(
                          fontSize: 12,
                          color: context.textSecondary,
                        ),
                      ),
                      const SizedBox(height: 6),
                      GestureDetector(
                        onTap: () {},
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: context.isDark ? const Color(0xFF451A1A) : AppColors.primaryLight.withValues(alpha: 0.6),
                            borderRadius: BorderRadius.circular(6),
                            border: Border.all(color: AppColors.primary.withValues(alpha: 0.1)),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(
                                isVi ? 'Thông tin bác sĩ' : 'Dentist Info',
                                style: TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w500,
                                  color: context.isDark ? Colors.white : AppColors.primary,
                                ),
                              ),
                              const SizedBox(width: 3),
                              Icon(Iconsax.arrow_right_2, size: 10, color: context.isDark ? Colors.white : AppColors.primary),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(Iconsax.arrow_down_2, color: context.textSecondary, size: 22),
              ],
            ),
          ),

          Divider(color: context.divider, height: 1),

          // ── Date chip (các ngày trong thẻ của dentist) ─────────────────
          SizedBox(
            height: 66,
            child: ListView.builder(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              itemCount: 14,
              itemBuilder: (context, index) {
                final d = DateTime.now().add(Duration(days: index));
                final isSelected = d.year == date.year && d.month == date.month && d.day == date.day;
                final isToday = index == 0;
                final shortWeekday = _weekdayShort(d, isVi);

                return GestureDetector(
                  onTap: () => onDateChange(d),
                  child: Container(
                    width: 62,
                    margin: const EdgeInsets.only(right: 8),
                    decoration: BoxDecoration(
                      color: isSelected
                          ? AppColors.primary
                          : (context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9)),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: isSelected
                            ? AppColors.primary
                            : (isToday ? AppColors.primary.withValues(alpha: 0.5) : context.divider),
                        width: isSelected || isToday ? 1.5 : 1,
                      ),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          isToday ? (isVi ? 'Hôm nay' : 'Today') : shortWeekday,
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
                            color: isSelected
                                ? Colors.white
                                : (isToday ? AppColors.primary : context.textSecondary),
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}',
                          textAlign: TextAlign.center,
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

          // ── Date label ───────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 10, 14, 4),
            child: Text(
              '${_fmtDateInline(date)} (${_weekdayShort(date, isVi)})',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w800,
                color: context.isDark ? Colors.white : AppColors.primary,
              ),
            ),
          ),

          if (doctor.slots.isEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(14, 16, 14, 20),
              child: Center(
                child: Column(
                  children: [
                    Icon(Iconsax.calendar_remove, size: 28, color: context.textSecondary.withValues(alpha: 0.6)),
                    const SizedBox(height: 6),
                    Text(
                      isVi
                          ? 'Bác sĩ không có lịch khám trống vào ngày này.'
                          : 'No available slots on this date.',
                      style: TextStyle(fontSize: 12.5, color: context.textSecondary, fontWeight: FontWeight.w500),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      isVi
                          ? 'Vui lòng chọn ngày khác ở trên.'
                          : 'Please select another date above.',
                      style: const TextStyle(fontSize: 11.5, color: AppColors.primary, fontWeight: FontWeight.w600),
                    ),
                  ],
                ),
              ),
            ),

          // ── Time slot grid — nhóm theo buổi (sáng/chiều/tối) ──────────
          // Phòng khi có period lạ/không xác định (dữ liệu cũ) — vẫn hiện ra thay vì ẩn mất.
          ...[
            ..._periodOrder,
            ...doctor.slots.map((s) => s.period).toSet().where((p) => !_periodOrder.contains(p)),
          ].expand((period) {
            final periodSlots = doctor.slots.where((s) => s.period == period).toList();
            if (periodSlots.isEmpty) return const <Widget>[];

            return [
              Padding(
                padding: const EdgeInsets.fromLTRB(14, 10, 14, 6),
                child: Text(
                  _periodLabel(period, isVi),
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w800,
                    color: context.textSecondary,
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(14, 0, 14, 8),
                child: GridView.count(
                  crossAxisCount: 2,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  mainAxisSpacing: 8,
                  crossAxisSpacing: 8,
                  childAspectRatio: 4.2,
                  children: periodSlots.map((slot) {
                    final timePart = slot.range.split(' - ').first.trim();
                    final timeParts = timePart.split(':');
                    final slotHour = int.tryParse(timeParts[0]) ?? 0;
                    final slotMinute = int.tryParse(timeParts[1]) ?? 0;

                    final isPastSlot = isDateToday && (slotHour < now.hour || (slotHour == now.hour && slotMinute <= now.minute));
                    final booked = slot.isBooked || isPastSlot;

                    final cellBg = booked
                        ? (context.isDark ? const Color(0xFF1E293B) : AppColors.background)
                        : (context.isDark ? const Color(0xFF451A1A) : AppColors.primaryLight.withValues(alpha: 0.55));
                    final cellText = booked
                        ? context.textSecondary.withValues(alpha: 0.4)
                        : (context.isDark ? Colors.white : AppColors.primary);

                    return GestureDetector(
                      onTap: booked ? null : () => onSlotSelected(slot),
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 150),
                        decoration: BoxDecoration(
                          color: cellBg,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: context.divider),
                        ),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              slot.range,
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w700,
                                color: cellText,
                              ),
                            ),
                            if (booked)
                              Text(
                                isPastSlot
                                    ? (isVi ? 'Đã qua' : 'Passed')
                                    : (isVi ? 'Hết số' : 'Booked'),
                                style: TextStyle(fontSize: 9, color: context.textSecondary.withValues(alpha: 0.5)),
                              ),
                          ],
                        ),
                      ),
                    );
                  }).toList(),
                ),
              ),
            ];
          }),
        ],
      ),
    );
  }

  String _fmtDateInline(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  static const _wdShortVi = ['', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN'];
  static const _wdShortEn = ['', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  String _weekdayShort(DateTime d, bool isVi) =>
      isVi ? _wdShortVi[d.weekday] : _wdShortEn[d.weekday];
}

// ─── Doctor Avatar ────────────────────────────────────────────────────────────

class _DoctorAvatar extends StatelessWidget {
  final String? avatarUrl;
  const _DoctorAvatar({this.avatarUrl});

  @override
  Widget build(BuildContext context) {
    final placeholder = Container(
      width: 60,
      height: 60,
      decoration: BoxDecoration(
        color: context.isDark ? const Color(0xFF451A1A) : AppColors.primaryLight,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Icon(Iconsax.profile_circle, color: context.isDark ? Colors.white : AppColors.primary, size: 30),
    );

    final resolved = ApiConstants.resolveAssetUrl(avatarUrl);
    if (resolved == null) return placeholder;

    return ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: Image.network(
        resolved,
        width: 60,
        height: 60,
        fit: BoxFit.cover,
        errorBuilder: (_, _, _) => placeholder,
        loadingBuilder: (_, child, loadingProgress) {
          if (loadingProgress == null) return child;
          return Container(
            width: 60,
            height: 60,
            decoration: BoxDecoration(
              color: context.isDark ? const Color(0xFF1E293B) : AppColors.background,
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Center(
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
          );
        },
      ),
    );
  }
}

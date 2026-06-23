import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
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
  List<ApiDoctorWithSlots> _doctors = [];
  bool _loading = true;
  String? _error;

  static const _weekdays = [
    '', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'
  ];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final list = await _service.getDoctorsWithSlots(widget.draft.date!);
      if (mounted) setState(() { _doctors = list; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = 'Không thể tải thông tin bác sĩ.'; _loading = false; });
    }
  }

  String _fmtDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  String _dayLabel(DateTime d) => '${_fmtDate(d)} - ${_weekdays[d.weekday]}';

  @override
  Widget build(BuildContext context) {
    final date = widget.draft.date!;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: BookingAppBar(title: 'Chọn khung giờ khám', showHome: false),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!, style: const TextStyle(color: AppColors.textMuted)),
                      const SizedBox(height: 12),
                      TextButton(onPressed: _load, child: const Text('Thử lại')),
                    ],
                  ),
                )
              : CustomScrollView(
                  slivers: [
                    // ── Header ────────────────────────────────────────────
                    SliverToBoxAdapter(
                      child: ColoredBox(
                        color: AppColors.surface,
                        child: Padding(
                          padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Chọn khung giờ khám',
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 4),
                              RichText(
                                text: const TextSpan(
                                  style: TextStyle(
                                    fontSize: 13,
                                    color: AppColors.textSecondary,
                                    fontStyle: FontStyle.italic,
                                  ),
                                  children: [
                                    TextSpan(text: 'Vui lòng bấm chọn khung giờ '),
                                    TextSpan(
                                      text: 'màu đỏ',
                                      style: TextStyle(
                                        color: AppColors.primary,
                                        fontWeight: FontWeight.w600,
                                        fontStyle: FontStyle.italic,
                                      ),
                                    ),
                                    TextSpan(text: ' để đặt khám'),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),

                    // ── Doctor cards ──────────────────────────────────────
                    SliverList(
                      delegate: SliverChildBuilderDelegate(
                        (_, i) => _DoctorSlotCard(
                          doctor: _doctors[i],
                          date: date,
                          dayLabel: _dayLabel(date),
                          onSlotSelected: (slot) {
                            final doctorInfo = _doctors[i].toDoctorInfo();
                            final draft2 = widget.draft.copyWith(
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
  final void Function(ApiTimeSlot) onSlotSelected;

  const _DoctorSlotCard({
    required this.doctor,
    required this.date,
    required this.dayLabel,
    required this.onSlotSelected,
  });

  @override
  Widget build(BuildContext context) {
    final isMorning = doctor.shift == 'morning';
    final sessionLabel = isMorning ? 'Buổi sáng' : 'Buổi chiều';

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 12, 16, 0),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
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
                      Text(
                        doctor.fullName,
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w800,
                          color: AppColors.primary,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        doctor.specialization,
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                      const SizedBox(height: 6),
                      GestureDetector(
                        onTap: () {},
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: AppColors.primaryLight.withValues(alpha: 0.6),
                            borderRadius: BorderRadius.circular(6),
                            border: Border.all(color: AppColors.primary.withValues(alpha: 0.1)),
                          ),
                          child: const Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(
                                'Thông tin bác sĩ',
                                style: TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w500,
                                  color: AppColors.primary,
                                ),
                              ),
                              SizedBox(width: 3),
                              Icon(Iconsax.arrow_right_2, size: 10, color: AppColors.primary),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const Icon(Iconsax.arrow_down_2, color: AppColors.textMuted, size: 22),
              ],
            ),
          ),

          const Divider(color: AppColors.divider, height: 1),

          // ── Date chip ─────────────────────────────────────────────────
          SizedBox(
            height: 76,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              children: [date].map((d) {
                return Container(
                  margin: const EdgeInsets.only(right: 10),
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}',
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                          color: Colors.white,
                          height: 1.3,
                        ),
                      ),
                      Text(
                        '${d.year}',
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                          fontSize: 9,
                          fontWeight: FontWeight.w600,
                          color: Colors.white70,
                          height: 1.3,
                        ),
                      ),
                    ],
                  ),
                );
              }).toList(),
            ),
          ),

          // ── Session label ─────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.all(14),
            child: Text(
              '${_fmtDateInline(date)} - $sessionLabel (${_weekdayShort(date)})',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w800,
                color: isMorning ? const Color(0xFF16A34A) : const Color(0xFFEA580C),
              ),
            ),
          ),

          // ── Time slot grid ────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.all(14),
            child: GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 8,
              crossAxisSpacing: 8,
              childAspectRatio: 4.2,
              children: doctor.slots.map((slot) {
                final booked = slot.isBooked;
                return GestureDetector(
                  onTap: booked ? null : () => onSlotSelected(slot),
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 150),
                    decoration: BoxDecoration(
                      color: booked
                          ? AppColors.background
                          : AppColors.primaryLight.withValues(alpha: 0.55),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          slot.range,
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: booked
                                ? AppColors.textMuted.withValues(alpha: 0.5)
                                : AppColors.primary,
                          ),
                        ),
                        if (booked)
                          const Text(
                            'Hết số',
                            style: TextStyle(fontSize: 9, color: AppColors.textMuted),
                          ),
                      ],
                    ),
                  ),
                );
              }).toList(),
            ),
          ),
        ],
      ),
    );
  }

  String _fmtDateInline(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  static const _wdShort = ['', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN'];
  String _weekdayShort(DateTime d) => _wdShort[d.weekday];
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
        color: AppColors.primaryLight,
        borderRadius: BorderRadius.circular(12),
      ),
      child: const Icon(Iconsax.profile_circle, color: AppColors.primary, size: 30),
    );

    if (avatarUrl == null || avatarUrl!.isEmpty) return placeholder;

    return ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: Image.network(
        avatarUrl!,
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
              color: AppColors.background,
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

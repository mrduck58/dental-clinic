import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';

class BookingSuccessPage extends StatefulWidget {
  final BookingDraft draft;
  const BookingSuccessPage({super.key, required this.draft});

  @override
  State<BookingSuccessPage> createState() => _BookingSuccessPageState();
}

class _BookingSuccessPageState extends State<BookingSuccessPage>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;
  late final Animation<double> _scale;
  late final Animation<double> _fade;

  static const _weekdaysVi = [
    '', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'
  ];
  static const _weekdaysEn = [
    '', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'
  ];

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 600),
    )..forward();
    _scale = CurvedAnimation(parent: _ctrl, curve: Curves.elasticOut);
    _fade = CurvedAnimation(parent: _ctrl, curve: Curves.easeIn);
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  String _fmtDate(DateTime d, bool isVi) {
    final weekdays = isVi ? _weekdaysVi : _weekdaysEn;
    return '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}'
        ' - ${weekdays[d.weekday]}';
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final d = widget.draft;
    final bottomPad = MediaQuery.of(context).padding.bottom;

    return Scaffold(
      backgroundColor: context.bg,
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 20),
                child: Column(
                  children: [
                    const SizedBox(height: 48),

                    // ── Animated success icon ─────────────────────────────
                    FadeTransition(
                      opacity: _fade,
                      child: ScaleTransition(
                        scale: _scale,
                        child: Container(
                          width: 96,
                          height: 96,
                          decoration: BoxDecoration(
                            color: AppColors.successLight,
                            shape: BoxShape.circle,
                            boxShadow: [
                              BoxShadow(
                                color: AppColors.success.withValues(alpha: 0.25),
                                blurRadius: 24,
                                offset: const Offset(0, 8),
                              ),
                            ],
                          ),
                          child: const Icon(
                            Iconsax.tick_circle,
                            color: AppColors.success,
                            size: 50,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: 20),

                    Text(
                      widget.draft.isRescheduling
                          ? (isVi ? 'Dời lịch thành công!' : 'Reschedule Success!')
                          : (isVi ? 'Đặt lịch thành công!' : 'Booking Success!'),
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.w800,
                        color: context.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      isVi
                          ? 'Lịch khám của bạn đang chờ phòng khám xác nhận.\nChúng tôi sẽ thông báo cho bạn sớm nhất.'
                          : 'Your appointment is pending confirmation.\nWe will notify you soon.',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 14,
                        color: context.textSecondary,
                        height: 1.6,
                      ),
                    ),
                    const SizedBox(height: 28),

                    // ── Summary card ──────────────────────────────────────
                    Container(
                      width: double.infinity,
                      decoration: BoxDecoration(
                        color: context.card,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: 0.05),
                            blurRadius: 12,
                            offset: const Offset(0, 4),
                          ),
                        ],
                      ),
                      child: Column(
                        children: [
                          // Card header
                          Padding(
                            padding: const EdgeInsets.all(16),
                            child: Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(8),
                                  decoration: BoxDecoration(
                                    color: context.isDark ? const Color(0xFF451A03) : const Color(0xFFFEF3C7),
                                    borderRadius: BorderRadius.circular(10),
                                  ),
                                  child: const Icon(
                                    Iconsax.calendar_tick,
                                    color: Color(0xFFD97706),
                                    size: 20,
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        isVi ? 'Chi tiết lịch hẹn' : 'Appointment Details',
                                        style: TextStyle(
                                          fontSize: 15,
                                          fontWeight: FontWeight.w700,
                                          color: context.textPrimary,
                                        ),
                                      ),
                                      Text(
                                        isVi
                                            ? 'Mã lịch: #${widget.draft.appointmentCode ?? 'DK...'}'
                                            : 'Code: #${widget.draft.appointmentCode ?? 'DK...'}',
                                        style: TextStyle(fontSize: 12, color: context.textSecondary),
                                      ),
                                    ],
                                  ),
                                ),
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 10, vertical: 4),
                                  decoration: BoxDecoration(
                                    color: context.isDark ? const Color(0xFF451A03) : const Color(0xFFFEF3C7),
                                    borderRadius: BorderRadius.circular(999),
                                  ),
                                  child: Text(
                                    isVi ? 'Chờ xác nhận' : 'Pending',
                                    style: const TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w700,
                                      color: Color(0xFFD97706),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Divider(color: context.divider, height: 1),

                          // Rows
                          if (d.patient != null)
                            _SummaryRow(
                              icon: Iconsax.profile_circle,
                              label: isVi ? 'Bệnh nhân' : 'Patient',
                              value:
                                  '${d.patient!.name} (${d.patient!.relationship})',
                            ),
                          if (d.service != null)
                            _SummaryRow(
                              icon: Iconsax.health,
                              label: isVi ? 'Dịch vụ' : 'Service',
                              value: d.service!.durationText.isNotEmpty
                                  ? '${d.service!.name} (${d.service!.durationText})'
                                  : d.service!.name,
                            ),
                          if (d.doctor != null)
                            _SummaryRow(
                              icon: Iconsax.profile_circle,
                              label: isVi ? 'Bác sĩ' : 'Dentist',
                              value: d.doctor!.fullName,
                            ),
                          if (d.date != null)
                            _SummaryRow(
                              icon: Iconsax.calendar_1,
                              label: isVi ? 'Ngày khám' : 'Date',
                              value: _fmtDate(d.date!, isVi),
                            ),
                          if (d.timeSlot != null)
                            _SummaryRow(
                              icon: Iconsax.clock,
                              label: isVi ? 'Giờ khám' : 'Time Slot',
                              value: d.displayTimeRange.isNotEmpty ? d.displayTimeRange : d.timeSlot!.range,
                              isLast: true,
                            ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 12),

                    // ── Reminder notice ───────────────────────────────────
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: context.isDark ? const Color(0xFF1E293B) : AppColors.secondaryLight,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                          color: context.isDark ? context.divider : AppColors.secondary.withValues(alpha: 0.25),
                        ),
                      ),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(Iconsax.notification_1,
                              color: AppColors.secondary, size: 18),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              isVi
                                  ? 'Bạn sẽ nhận thông báo nhắc lịch vào ngày khám và trước giờ khám 1 tiếng.'
                                  : 'You will receive reminders on your appointment date and 1 hour before your appointment.',
                              style: const TextStyle(
                                fontSize: 12,
                                color: AppColors.secondary,
                                height: 1.5,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),

            // ── Action buttons ────────────────────────────────────────────
            Padding(
              padding: EdgeInsets.fromLTRB(20, 0, 20, 16 + bottomPad),
              child: Column(
                children: [
                  SizedBox(
                    width: double.infinity,
                    height: 50,
                    child: ElevatedButton(
                      onPressed: () {
                        final d = widget.draft;
                        final appId = d.appointmentId ?? d.reschedulingAppointmentId;
                        if (appId != null && appId.isNotEmpty) {
                          context.push(AppRoutes.appointmentDetails, extra: appId);
                        } else if (d.patient != null && d.patient!.id != 'self') {
                          context.go(
                            AppRoutes.appointments,
                            extra: {
                              'patientId': d.patient!.id,
                              'patientName': d.patient!.name,
                            },
                          );
                        } else {
                          context.go(AppRoutes.appointments);
                        }
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: Colors.white,
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: Text(
                        isVi ? 'Xem chi tiết lịch hẹn' : 'View appointment details',
                        style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
                      ),
                    ),
                  ),
                  const SizedBox(height: 10),
                  SizedBox(
                    width: double.infinity,
                    height: 50,
                    child: OutlinedButton(
                      onPressed: () => context.go(AppRoutes.home),
                      style: OutlinedButton.styleFrom(
                        side: BorderSide(color: context.divider, width: 1.5),
                        foregroundColor: context.textPrimary,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: Text(
                        isVi ? 'Về trang chủ' : 'Back to Home',
                        style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SummaryRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final bool isLast;

  const _SummaryRow({
    required this.icon,
    required this.label,
    required this.value,
    this.isLast = false,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            children: [
              Icon(icon, size: 16, color: context.textSecondary),
              const SizedBox(width: 10),
              SizedBox(
                width: 80,
                child: Text(
                  label,
                  style: TextStyle(
                      fontSize: 13, color: context.textSecondary),
                ),
              ),
              Expanded(
                child: Text(
                  value,
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: context.textPrimary,
                  ),
                ),
              ),
            ],
          ),
        ),
        if (!isLast)
          Divider(
              color: context.divider, height: 1, indent: 16, endIndent: 16),
      ],
    );
  }
}

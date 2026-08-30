import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

/// Lịch hẹn tái khám. Do bác sĩ đặt mốc ngày cần khám lại ở tab "Tái khám"
/// Bệnh nhân có thể xem chi tiết và bấm "Đặt lịch tái khám ngay" để chọn ca khám.
class FollowUpDetailPage extends StatelessWidget {
  final MedicalHistoryEvent event;
  const FollowUpDetailPage({super.key, required this.event});

  String _formatDate(DateTime d, bool isVi) {
    const weekdaysVi = ['Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'];
    const weekdaysEn = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    final weekday = isVi ? weekdaysVi[d.weekday - 1] : weekdaysEn[d.weekday - 1];
    final dd = d.day.toString().padLeft(2, '0');
    final mm = d.month.toString().padLeft(2, '0');
    return isVi ? '$weekday, $dd/$mm/${d.year}' : '$weekday, $mm/$dd/${d.year}';
  }

  void _bookFollowUp(BuildContext context, bool isVi) {
    final doctor = DoctorInfo(
      id: event.dentistId,
      name: event.dentistName,
      title: '',
      specialty: '',
      room: '',
      session: DoctorSession.morning,
      rating: 5.0,
      reviewCount: 0,
      avatarUrl: event.dentistAvatarUrl,
    );

    final service = event.serviceId != null
        ? ServiceInfo(
            id: event.serviceId!,
            name: event.serviceName,
            description: '',
            price: '',
            durationMinutes: 30,
          )
        : null;

    final draft = BookingDraft(
      preferredDentistId: event.dentistId,
      patient: PatientInfo(
        id: event.patientId,
        name: event.patientName,
        relationship: event.patientRelationship,
      ),
      doctor: doctor,
      service: service,
      date: event.followUpDate,
      isFollowUp: true,
      symptoms: isVi
          ? 'Tái khám theo hẹn: ${event.serviceName}${event.followUpNote != null ? ' - ${event.followUpNote}' : ''}'
          : 'Follow-up visit: ${event.serviceName}${event.followUpNote != null ? ' - ${event.followUpNote}' : ''}',
    );

    BookingService().setActiveDraft(draft);
    context.push(AppRoutes.bookingSelectTimeSlot, extra: draft);
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final followUpDate = event.followUpDate;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: Text(
          isVi ? 'Lịch tái khám' : 'Follow-up Appointment',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w800, fontSize: 18),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: followUpDate == null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 32),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Iconsax.calendar_2, size: 40, color: context.textMuted),
                    const SizedBox(height: 12),
                    Text(
                      isVi ? 'Bác sĩ chưa hẹn lịch tái khám cho buổi khám này.' : 'No follow-up scheduled for this visit.',
                      style: TextStyle(color: context.textMuted),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: context.isDark ? const Color(0xFF14532D).withValues(alpha: 0.25) : const Color(0xFFF0FDF4),
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: context.isDark ? Colors.green.withValues(alpha: 0.3) : const Color(0xFFBBF7D0)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(8),
                              decoration: const BoxDecoration(color: Color(0xFF16A34A), shape: BoxShape.circle),
                              child: const Icon(Icons.event_available_rounded, color: Colors.white, size: 18),
                            ),
                            const SizedBox(width: 10),
                            Text(
                              isVi ? 'NGÀY HẸN TÁI KHÁM' : 'FOLLOW-UP DATE',
                              style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w900, color: Color(0xFF16A34A), letterSpacing: 0.5),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
                        Text(
                          _formatDate(followUpDate, isVi),
                          style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900, color: Color(0xFF15803D)),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),
                  _CalendarCard(followUpDate: followUpDate, isVi: isVi),
                  if (event.followUpNote != null && event.followUpNote!.isNotEmpty) ...[
                    const SizedBox(height: 20),
                    Text(
                      isVi ? 'GHI CHÚ CỦA BÁC SĨ' : "DENTIST'S NOTE",
                      style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: context.textMuted, letterSpacing: 0.5),
                    ),
                    const SizedBox(height: 10),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: context.card,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                      ),
                      child: Text(
                        event.followUpNote!,
                        style: TextStyle(fontSize: 14, color: context.textSecondary, height: 1.5),
                      ),
                    ),
                  ],
                  const SizedBox(height: 20),
                  Text(
                    isVi
                        ? 'Đây là mốc ngày bác sĩ khuyên bạn quay lại khám. Bạn có thể bấm nút bên dưới để chọn khung giờ và đặt lịch khám ngay.'
                        : 'This is the recommended follow-up date. Tap the button below to pick a slot and book your appointment.',
                    style: TextStyle(fontSize: 12.5, color: context.textMuted, height: 1.4),
                  ),
                ],
              ),
            ),
      bottomNavigationBar: followUpDate == null
          ? null
          : Container(
              padding: EdgeInsets.fromLTRB(24, 16, 24, MediaQuery.of(context).padding.bottom + 16),
              decoration: BoxDecoration(
                color: context.card,
                border: Border(top: BorderSide(color: context.divider)),
              ),
              child: SizedBox(
                width: double.infinity,
                height: 50,
                child: ElevatedButton.icon(
                  onPressed: () => _bookFollowUp(context, isVi),
                  icon: const Icon(Iconsax.calendar_add, size: 20),
                  label: Text(
                    isVi ? 'ĐẶT LỊCH TÁI KHÁM NGAY' : 'BOOK FOLLOW-UP NOW',
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                    elevation: 2,
                  ),
                ),
              ),
            ),
    );
  }
}

/// Lịch tháng thu nhỏ, chỉ để xem — highlight đúng ngày hẹn tái khám cho trực quan,
/// có thể lật qua lại các tháng khác nhưng không chọn/sửa được ngày.
class _CalendarCard extends StatefulWidget {
  final DateTime followUpDate;
  final bool isVi;
  const _CalendarCard({required this.followUpDate, required this.isVi});

  @override
  State<_CalendarCard> createState() => _CalendarCardState();
}

class _CalendarCardState extends State<_CalendarCard> {
  late DateTime _visibleMonth;
  late final DateTime _today;

  static const _monthsVi = [
    'Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6',
    'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12',
  ];
  static const _monthsEn = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ];
  static const _weekdaysVi = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
  static const _weekdaysEn = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  @override
  void initState() {
    super.initState();
    _visibleMonth = DateTime(widget.followUpDate.year, widget.followUpDate.month);
    final now = DateTime.now();
    _today = DateTime(now.year, now.month, now.day);
  }

  void _shiftMonth(int delta) {
    setState(() => _visibleMonth = DateTime(_visibleMonth.year, _visibleMonth.month + delta));
  }

  void _goToToday() {
    setState(() => _visibleMonth = DateTime(_today.year, _today.month));
  }

  @override
  Widget build(BuildContext context) {
    final isVi = widget.isVi;
    final monthLabel = isVi ? _monthsVi[_visibleMonth.month - 1] : _monthsEn[_visibleMonth.month - 1];
    final weekdays = isVi ? _weekdaysVi : _weekdaysEn;

    final firstOfMonth = DateTime(_visibleMonth.year, _visibleMonth.month, 1);
    final daysInMonth = DateTime(_visibleMonth.year, _visibleMonth.month + 1, 0).day;
    final leadingBlanks = firstOfMonth.weekday - 1; // weekday: Mon=1..Sun=7

    final isFollowUpMonth = _visibleMonth.year == widget.followUpDate.year && _visibleMonth.month == widget.followUpDate.month;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        children: [
          Row(
            children: [
              IconButton(
                onPressed: () => _shiftMonth(-1),
                icon: Icon(Icons.chevron_left_rounded, color: context.textSecondary),
                visualDensity: VisualDensity.compact,
              ),
              Expanded(
                child: Center(
                  child: Text(
                    '$monthLabel ${_visibleMonth.year}',
                    style: TextStyle(fontSize: 14, fontWeight: FontWeight.w800, color: context.textPrimary),
                  ),
                ),
              ),
              IconButton(
                onPressed: () => _shiftMonth(1),
                icon: Icon(Icons.chevron_right_rounded, color: context.textSecondary),
                visualDensity: VisualDensity.compact,
              ),
              TextButton(
                onPressed: _goToToday,
                style: TextButton.styleFrom(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  minimumSize: Size.zero,
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  foregroundColor: AppColors.primary,
                  backgroundColor: AppColors.primary.withValues(alpha: context.isDark ? 0.18 : 0.08),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                ),
                child: Text(
                  isVi ? 'Hôm nay' : 'Today',
                  style: const TextStyle(fontSize: 11.5, fontWeight: FontWeight.w800),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: weekdays
                .map((w) => Expanded(
                      child: Center(
                        child: Text(
                          w,
                          style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: context.textMuted),
                        ),
                      ),
                    ))
                .toList(),
          ),
          const SizedBox(height: 6),
          GridView.builder(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 7),
            itemCount: leadingBlanks + daysInMonth,
            itemBuilder: (context, index) {
              if (index < leadingBlanks) return const SizedBox.shrink();
              final day = index - leadingBlanks + 1;
              final isFollowUpDay = isFollowUpMonth && day == widget.followUpDate.day;
              final isToday = _visibleMonth.year == _today.year && _visibleMonth.month == _today.month && day == _today.day;
              return Padding(
                padding: const EdgeInsets.all(2),
                child: Container(
                  decoration: BoxDecoration(
                    color: isFollowUpDay ? const Color(0xFF16A34A) : Colors.transparent,
                    shape: BoxShape.circle,
                    border: !isFollowUpDay && isToday ? Border.all(color: AppColors.primary, width: 1.5) : null,
                  ),
                  alignment: Alignment.center,
                  child: Text(
                    '$day',
                    style: TextStyle(
                      fontSize: 12.5,
                      fontWeight: (isFollowUpDay || isToday) ? FontWeight.w900 : FontWeight.w600,
                      color: isFollowUpDay
                          ? Colors.white
                          : (isToday ? AppColors.primary : context.textPrimary),
                    ),
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}

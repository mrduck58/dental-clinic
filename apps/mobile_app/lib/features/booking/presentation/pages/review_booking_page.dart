import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:dio/dio.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';

class ReviewBookingPage extends StatefulWidget {
  final BookingDraft draft;
  const ReviewBookingPage({super.key, required this.draft});

  @override
  State<ReviewBookingPage> createState() => _ReviewBookingPageState();
}

class _ReviewBookingPageState extends State<ReviewBookingPage> {
  final _symptomCtrl = TextEditingController();
  final _bookingService = BookingService();
  bool _isLoading = false;
  Timer? _holdTimer;
  int _remainingSeconds = 0;

  static const _weekdaysVi = [
    '', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'
  ];
  static const _weekdaysEn = [
    '', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'
  ];

  String _fmtDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  String? _submitError;

  @override
  void initState() {
    super.initState();
    _symptomCtrl.text = widget.draft.symptoms ?? '';

    // Khởi động đếm ngược hạn giữ chỗ nếu có
    if (widget.draft.holdExpiresAt != null) {
      final diff = widget.draft.holdExpiresAt!.difference(DateTime.now()).inSeconds;
      _remainingSeconds = diff > 0 ? diff : 0;
      if (_remainingSeconds > 0) {
        _holdTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
          if (!mounted) {
            timer.cancel();
            return;
          }
          if (_remainingSeconds <= 1) {
            timer.cancel();
            setState(() => _remainingSeconds = 0);
            _showExpiredAndRedirect();
          } else {
            setState(() => _remainingSeconds--);
          }
        });
      }
    }
  }

  @override
  void dispose() {
    _holdTimer?.cancel();
    _symptomCtrl.dispose();
    super.dispose();
  }

  void _showExpiredAndRedirect() {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Row(
          children: [
            const Icon(Iconsax.timer_pause, color: Color(0xFFD97706), size: 24),
            const SizedBox(width: 8),
            Text(
              isVi ? 'Hết giờ giữ chỗ' : 'Hold Expired',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Text(
          isVi
              ? 'Đã hết thời gian giữ chỗ 5 phút cho ca khám này. Ca khám đã được giải phóng để bệnh nhân khác có thể đặt.'
              : 'The 5-minute hold for this slot has expired and has been released.',
          style: const TextStyle(fontSize: 14, height: 1.4),
        ),
        actions: [
          ElevatedButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              context.pushReplacement(AppRoutes.bookingSelectTimeSlot, extra: widget.draft);
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            child: Text(isVi ? 'Chọn lại ca khám' : 'Select Slot Again'),
          ),
        ],
      ),
    );
  }

  Future<void> _confirm(bool isVi) async {
    setState(() {
      _isLoading = true;
      _submitError = null;
    });
    try {
      final d = widget.draft;
      if (d.patient != null && d.patient!.id.startsWith('member_')) {
        throw Exception(isVi 
            ? 'Lỗi đồng bộ: Vui lòng đăng xuất và đăng nhập lại để tải thông tin thành viên gia đình thực tế.'
            : 'Sync error: Please log out and log in again to load real family member profiles.');
      }
      if (d.isRescheduling) {
        final serviceId = (d.service != null && d.service!.id.isNotEmpty) ? d.service!.id : null;
        await _bookingService.rescheduleAppointment(
          d.reschedulingAppointmentId!,
          BookingService.combineDateAndSlot(d.date!, d.timeSlot!.range),
          dentistId: d.doctor?.id,
          serviceId: serviceId,
          reason: _symptomCtrl.text.trim().isEmpty ? null : _symptomCtrl.text.trim(),
        );
        if (!mounted) return;
        context.pushReplacement(
          AppRoutes.bookingSuccess,
          extra: d.copyWith(
            symptoms: _symptomCtrl.text.trim(),
            appointmentId: d.reschedulingAppointmentId,
          ),
        );
        return;
      }

      final result = await _bookingService.createAppointment(
        dentistId: d.doctor!.id,
        date: d.date!,
        timeSlotRange: d.timeSlot!.range,
        symptoms: _symptomCtrl.text.trim().isEmpty ? null : _symptomCtrl.text.trim(),
        serviceId: (d.service != null && d.service!.id.isNotEmpty) ? d.service!.id : null,
        patientId: (d.patient != null && d.patient!.id.isNotEmpty && d.patient!.id != 'self') ? d.patient!.id : null,
      );
      if (!mounted) return;
      final updatedDraft = d.copyWith(
        symptoms: _symptomCtrl.text.trim(),
        appointmentId: result.appointmentId,
        appointmentCode: result.appointmentCode,
      );
      context.pushReplacement(AppRoutes.bookingSuccess, extra: updatedDraft);
    } on DioException catch (e) {
      if (!mounted) return;
      final msg = ApiClient.errorMessage(e);
      setState(() => _submitError = msg);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(msg),
          behavior: SnackBarBehavior.floating,
          margin: const EdgeInsets.fromLTRB(16, 0, 16, 110),
          backgroundColor: const Color(0xFFEF4444),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      final msg = isVi ? 'Đặt lịch thất bại. Vui lòng thử lại.' : 'Booking failed. Please try again.';
      setState(() => _submitError = msg);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(msg),
          behavior: SnackBarBehavior.floating,
          margin: const EdgeInsets.fromLTRB(16, 0, 16, 110),
          backgroundColor: const Color(0xFFEF4444),
        ),
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Widget _buildHoldCountdownBanner(bool isVi) {
    final minutes = _remainingSeconds ~/ 60;
    final seconds = _remainingSeconds % 60;
    final timeStr = '${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
    final isUrgent = _remainingSeconds <= 60;

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 8, 16, 4),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: isUrgent
            ? (context.isDark ? const Color(0xFF3B1515) : const Color(0xFFFEF2F2))
            : (context.isDark ? const Color(0xFF38290D) : const Color(0xFFFFFBEB)),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: isUrgent
              ? (context.isDark ? const Color(0xFF7F1D1D) : const Color(0xFFFCA5A5))
              : (context.isDark ? const Color(0xFF78350F) : const Color(0xFFFDE68A)),
        ),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(6),
            decoration: BoxDecoration(
              color: isUrgent
                  ? (context.isDark ? const Color(0xFF5A1A1A) : const Color(0xFFFEE2E2))
                  : (context.isDark ? const Color(0xFF451A03) : const Color(0xFFFEF3C7)),
              shape: BoxShape.circle,
            ),
            child: Icon(
              Iconsax.timer_1,
              size: 18,
              color: isUrgent ? const Color(0xFFEF4444) : const Color(0xFFD97706),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              isVi
                  ? 'Thời gian giữ chỗ còn lại:'
                  : 'Hold time remaining:',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: isUrgent
                    ? (context.isDark ? const Color(0xFFFCA5A5) : const Color(0xFF991B1B))
                    : (context.isDark ? const Color(0xFFFDE68A) : const Color(0xFF92400E)),
              ),
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(
              color: isUrgent ? const Color(0xFFDC2626) : const Color(0xFFD97706),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Text(
              timeStr,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w800,
                color: Colors.white,
                fontFeatures: [FontFeature.tabularFigures()],
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final d = widget.draft;
    final bottomPad = MediaQuery.of(context).padding.bottom;
    final weekdays = isVi ? _weekdaysVi : _weekdaysEn;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(
        title: d.isRescheduling
            ? (isVi ? 'Xác nhận đổi lịch' : 'Confirm Reschedule')
            : (isVi ? 'Xác nhận đặt khám' : 'Confirm Booking'),
      ),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                children: [
                  if (_remainingSeconds > 0)
                    _buildHoldCountdownBanner(isVi),

                  const SizedBox(height: 12),

                  // ── Summary card ──────────────────────────────────────────
                  Container(
                    margin: const EdgeInsets.symmetric(horizontal: 16),
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: context.divider),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.04),
                          blurRadius: 8,
                          offset: const Offset(0, 3),
                        ),
                      ],
                    ),
                    child: Column(
                      children: [
                        if (d.patient != null)
                          _InfoRow(
                            icon: Iconsax.user,
                            label: isVi ? 'Bệnh nhân' : 'Patient',
                            value: '${d.patient!.name} (${d.patient!.relationship})',
                            onEdit: () => context.push(AppRoutes.bookingSelectPatient, extra: d),
                          ),
                        if (d.service != null)
                          _InfoRow(
                            icon: Iconsax.health,
                            label: isVi ? 'Dịch vụ' : 'Service',
                            value: d.service?.name ?? (isVi ? 'Khám tổng quát' : 'General check-up'),
                            onEdit: () => context.push(AppRoutes.bookingSelectService, extra: d),
                          ),
                        if (d.date != null)
                          _InfoRow(
                            icon: Iconsax.calendar,
                            label: isVi ? 'Ngày khám' : 'Date',
                            value: '${_fmtDate(d.date!)} - ${weekdays[d.date!.weekday]}',
                            onEdit: () => context.push(AppRoutes.bookingSelectDatetime, extra: d),
                          ),
                        if (d.timeSlot != null)
                          _InfoRow(
                            icon: Iconsax.clock,
                            label: isVi ? 'Giờ khám' : 'Time Slot',
                            value: '${d.timeSlot!.range}, ${d.doctor?.room ?? ''}',
                            onEdit: () => context.push(AppRoutes.bookingSelectTimeSlot, extra: d),
                          ),
                        if (d.doctor != null)
                          _InfoRow(
                            icon: Iconsax.profile_circle,
                            label: isVi ? 'Bác sĩ' : 'Dentist',
                            value: d.doctor!.fullName,
                            isLast: true,
                            onEdit: () => context.push(AppRoutes.bookingSelectDoctor, extra: d),
                          ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),

                  // ── Symptom input ─────────────────────────────────────────
                  Container(
                    margin: const EdgeInsets.symmetric(horizontal: 16),
                    padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: context.divider),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            const Icon(Iconsax.note_text, size: 16, color: AppColors.primary),
                            const SizedBox(width: 6),
                            Text(
                              isVi ? 'Triệu chứng / Ghi chú cho bác sĩ' : 'Symptoms / Notes',
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w700,
                                color: context.textPrimary,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        TextField(
                          controller: _symptomCtrl,
                          maxLines: 3,
                          maxLength: 300,
                          style: TextStyle(fontSize: 13, color: context.textPrimary),
                          decoration: InputDecoration(
                            hintText: isVi
                                ? 'Mô tả triệu chứng hoặc lưu ý thêm (ví dụ: đau răng hàm dưới, nhạy cảm khi uống nước lạnh...)'
                                : 'Describe symptoms or notes for the dentist...',
                            hintStyle: TextStyle(fontSize: 12, color: context.textMuted),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: BorderSide(color: context.divider),
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: BorderSide(color: context.divider),
                            ),
                            focusedBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
                            ),
                            contentPadding: const EdgeInsets.all(12),
                            filled: true,
                            fillColor: context.bg,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),

                  // ── Important Notice Box ──────────────────────────────────
                  Container(
                    margin: const EdgeInsets.symmetric(horizontal: 16),
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: const Color(0xFFEFF6FF),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFBFDBFE)),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(Iconsax.info_circle, color: Color(0xFF2563EB), size: 20),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            isVi
                                ? 'Vui lòng đến trước 10-15 phút để làm thủ tục check-in. Bạn có thể hủy hoặc đổi lịch khám miễn phí trong 24 giờ kể từ khi đặt.'
                                : 'Please arrive 10-15 minutes early for check-in. You can cancel or reschedule freely within 24 hours of booking.',
                            style: const TextStyle(
                              fontSize: 12,
                              color: Color(0xFF1E40AF),
                              height: 1.4,
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

          // ── Bottom Action Bar ─────────────────────────────────────────────
          Container(
            padding: EdgeInsets.fromLTRB(16, 12, 16, bottomPad > 0 ? bottomPad : 16),
            decoration: BoxDecoration(
              color: context.card,
              border: Border(top: BorderSide(color: context.divider)),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.05),
                  blurRadius: 8,
                  offset: const Offset(0, -2),
                ),
              ],
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (_submitError != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Text(
                      _submitError!,
                      style: const TextStyle(color: Color(0xFFEF4444), fontSize: 12),
                      textAlign: TextAlign.center,
                    ),
                  ),
                SizedBox(
                  width: double.infinity,
                  height: 50,
                  child: ElevatedButton(
                    onPressed: _isLoading ? null : () => _confirm(isVi),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.primary,
                      foregroundColor: Colors.white,
                      elevation: 0,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14),
                      ),
                    ),
                    child: _isLoading
                        ? const SizedBox(
                            width: 22,
                            height: 22,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.5,
                              valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                            ),
                          )
                        : Text(
                            d.isRescheduling
                                ? (isVi ? 'Xác nhận đổi lịch khám' : 'Confirm Reschedule')
                                : (isVi ? 'Xác nhận đặt khám' : 'Confirm Appointment'),
                            style: const TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final bool isLast;
  final VoidCallback? onEdit;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
    this.isLast = false,
    this.onEdit,
  });

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          child: Row(
            children: [
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: context.bg,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(icon, size: 20, color: AppColors.primary),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      label,
                      style: TextStyle(
                        fontSize: 12,
                        color: context.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      value,
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w800,
                        color: context.isDark ? Colors.white : AppColors.primary,
                      ),
                    ),
                  ],
                ),
              ),
              if (onEdit != null)
                GestureDetector(
                  onTap: onEdit,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Iconsax.edit_2, size: 15, color: context.textSecondary),
                        const SizedBox(width: 4),
                        Text(
                          isVi ? 'Sửa' : 'Edit',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: context.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                )
              else
                const Icon(Iconsax.tick_circle, color: AppColors.success, size: 20),
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

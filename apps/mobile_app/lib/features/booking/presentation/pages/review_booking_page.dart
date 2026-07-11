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

  static const _weekdaysVi = [
    '', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'
  ];
  static const _weekdaysEn = [
    '', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'
  ];

  String _fmtDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  @override
  void initState() {
    super.initState();
    // Điền sẵn triệu chứng/ghi chú nếu đã có từ trước (ví dụ do chatbot AI trích xuất).
    _symptomCtrl.text = widget.draft.symptoms ?? '';
  }

  @override
  void dispose() {
    _symptomCtrl.dispose();
    super.dispose();
  }

  Future<void> _confirm(bool isVi) async {
    setState(() => _isLoading = true);
    try {
      final d = widget.draft;
      if (d.patient != null && d.patient!.id.startsWith('member_')) {
        throw Exception(isVi 
            ? 'Lỗi đồng bộ: Vui lòng đăng xuất và đăng nhập lại để tải thông tin thành viên gia đình thực tế.'
            : 'Sync error: Please log out and log in again to load real family member profiles.');
      }
      final result = await _bookingService.createAppointment(
        dentistId: d.doctor!.id,
        date: d.date!,
        timeSlotRange: d.timeSlot!.range,
        symptoms: _symptomCtrl.text.trim().isEmpty ? null : _symptomCtrl.text.trim(),
        serviceId: d.service?.id,
        patientId: d.patient?.id == 'self' ? null : d.patient?.id,
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
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(ApiClient.errorMessage(e))),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(isVi ? 'Đặt lịch thất bại. Vui lòng thử lại.' : 'Booking failed. Please try again.')),
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final d = widget.draft;
    final bottomPad = MediaQuery.of(context).padding.bottom;
    final weekdays = isVi ? _weekdaysVi : _weekdaysEn;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Xác nhận đặt khám' : 'Confirm Booking'),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                children: [
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
                        if (d.service != null)
                          _InfoRow(
                            icon: Iconsax.health,
                            label: isVi ? 'Chuyên khoa' : 'Service',
                            value: d.service!.name,
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
                            onEdit: () => context.push(AppRoutes.bookingSelectDoctor, extra: d),
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
                            const SizedBox(width: 8),
                            Text(
                              isVi ? 'Triệu chứng' : 'Symptoms',
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                color: context.textPrimary,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _symptomCtrl,
                          maxLines: 4,
                          minLines: 3,
                          style: TextStyle(
                            fontSize: 14,
                            color: context.textPrimary,
                          ),
                          decoration: InputDecoration(
                            hintText: isVi 
                                ? 'Mô tả triệu chứng của bạn (đau răng, sâu răng, v.v.)'
                                : 'Describe your symptoms (toothache, decay, etc.)',
                            hintStyle: TextStyle(
                              fontSize: 13,
                              color: context.textSecondary.withValues(alpha: 0.6),
                            ),
                            filled: true,
                            fillColor: context.bg,
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 12,
                            ),
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
                              borderSide: const BorderSide(
                                color: AppColors.primary,
                                width: 1.5,
                              ),
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

          // ── Bottom confirm button ─────────────────────────────────────────
          Container(
            padding: EdgeInsets.fromLTRB(16, 12, 16, 12 + bottomPad),
            decoration: BoxDecoration(
              color: context.card,
              border: Border(top: BorderSide(color: context.divider)),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.06),
                  blurRadius: 12,
                  offset: const Offset(0, -4),
                ),
              ],
            ),
            child: SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton(
                onPressed: _isLoading ? null : () => _confirm(isVi),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(999),
                  ),
                ),
                child: _isLoading
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(
                            color: Colors.white, strokeWidth: 2),
                      )
                    : Text(
                        isVi ? 'Xác nhận đặt khám' : 'Confirm Booking',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ─── Info Row ─────────────────────────────────────────────────────────────────

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

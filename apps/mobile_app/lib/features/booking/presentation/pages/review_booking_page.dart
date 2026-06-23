import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
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

  static const _weekdays = [
    '', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'
  ];

  String _fmtDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  @override
  void dispose() {
    _symptomCtrl.dispose();
    super.dispose();
  }

  Future<void> _confirm() async {
    setState(() => _isLoading = true);
    try {
      final d = widget.draft;
      final result = await _bookingService.createAppointment(
        dentistId: d.doctor!.id,
        date: d.date!,
        timeSlotRange: d.timeSlot!.range,
        symptoms: _symptomCtrl.text.trim().isEmpty ? null : _symptomCtrl.text.trim(),
        serviceId: d.service?.id,
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
        const SnackBar(content: Text('Đặt lịch thất bại. Vui lòng thử lại.')),
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final d = widget.draft;
    final bottomPad = MediaQuery.of(context).padding.bottom;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: BookingAppBar(title: 'Xác nhận đặt khám'),
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
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: AppColors.divider),
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
                            label: 'Chuyên khoa',
                            value: d.service!.name,
                          ),
                        if (d.date != null)
                          _InfoRow(
                            icon: Iconsax.calendar,
                            label: 'Ngày khám',
                            value: '${_fmtDate(d.date!)} - ${_weekdays[d.date!.weekday]}',
                          ),
                        if (d.timeSlot != null)
                          _InfoRow(
                            icon: Iconsax.clock,
                            label: 'Giờ khám',
                            value: '${d.timeSlot!.range}, ${d.doctor?.room ?? ''}',
                          ),
                        if (d.doctor != null)
                          _InfoRow(
                            icon: Iconsax.profile_circle,
                            label: 'Bác sĩ',
                            value: d.doctor!.fullName,
                            isLast: true,
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
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: AppColors.divider),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: const [
                            Icon(Iconsax.note_text, size: 16, color: AppColors.primary),
                            SizedBox(width: 8),
                            Text(
                              'Triệu chứng',
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _symptomCtrl,
                          maxLines: 4,
                          minLines: 3,
                          style: const TextStyle(
                            fontSize: 14,
                            color: AppColors.textPrimary,
                          ),
                          decoration: InputDecoration(
                            hintText: 'Mô tả triệu chứng của bạn (đau răng, sâu răng, v.v.)',
                            hintStyle: const TextStyle(
                              fontSize: 13,
                              color: AppColors.textMuted,
                            ),
                            filled: true,
                            fillColor: AppColors.background,
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 12,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: const BorderSide(color: AppColors.divider),
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: const BorderSide(color: AppColors.divider),
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
              color: AppColors.surface,
              border: const Border(top: BorderSide(color: AppColors.divider)),
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
                onPressed: _isLoading ? null : _confirm,
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
                    : const Text(
                        'Xác nhận đặt khám',
                        style: TextStyle(
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

  const _InfoRow({
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
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          child: Row(
            children: [
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: AppColors.background,
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
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textMuted,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      value,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                        color: AppColors.primary,
                      ),
                    ),
                  ],
                ),
              ),
              const Icon(Iconsax.tick_circle, color: AppColors.success, size: 20),
            ],
          ),
        ),
        if (!isLast)
          const Divider(
              color: AppColors.divider, height: 1, indent: 16, endIndent: 16),
      ],
    );
  }
}

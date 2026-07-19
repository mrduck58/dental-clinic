import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

/// Trang trung tâm của 1 buổi khám — 4 lối vào tương ứng đúng 4 tab bác sĩ thấy khi khám
/// (apps/admin_website): Chẩn đoán / Liệu trình / Đơn thuốc / Tái khám.
class ExaminationDetailPage extends StatefulWidget {
  final MedicalHistoryEvent event;
  const ExaminationDetailPage({super.key, required this.event});

  @override
  State<ExaminationDetailPage> createState() => _ExaminationDetailPageState();
}

class _ExaminationDetailPageState extends State<ExaminationDetailPage> {
  MedicalHistoryEvent get event => widget.event;

  // null = đang tải. Đếm theo CẢ CHUỖI tái khám (không chỉ riêng buổi hẹn này) — một liệu trình
  // dài hạn có thể được tạo ở buổi trước/sau buổi đang xem, xem event.treatmentPlans (chỉ scope
  // theo đúng buổi hẹn này) sẽ đếm thiếu.
  List<RealTreatmentPlan>? _chainTreatmentPlans;

  @override
  void initState() {
    super.initState();
    _loadTreatmentPlans();
  }

  Future<void> _loadTreatmentPlans() async {
    try {
      final plans = await MedicalRecordService().getMyTreatmentPlans(patientId: event.patientId);
      final chainIds = event.treatmentChainIds;
      if (!mounted) return;
      setState(() {
        _chainTreatmentPlans = plans.where((p) => p.appointmentId != null && chainIds.contains(p.appointmentId)).toList();
      });
    } catch (_) {
      // Giữ null (đang tải) — thẻ sẽ tự thử lại khi mở lại trang; không chặn các mục khác.
    }
  }

  String _formatDate(DateTime d) => '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final treatmentCount = _chainTreatmentPlans?.length;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Chi tiết khám bệnh' : 'Examination Detail',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Visit summary card
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.divider),
              ),
              child: Row(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(color: AppColors.primaryLight, shape: BoxShape.circle),
                    child: const Icon(Iconsax.calendar_1, color: AppColors.primary, size: 22),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          event.serviceName,
                          style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800, color: context.textPrimary),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          '${_formatDate(event.appointmentDate)} · ${event.dentistName}',
                          style: TextStyle(fontSize: 12.5, color: context.textSecondary, fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            if (event.symptoms != null && event.symptoms!.isNotEmpty) ...[
              const SizedBox(height: 16),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: context.card,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: context.divider),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isVi ? 'TRIỆU CHỨNG' : 'SYMPTOMS',
                      style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: context.textMuted, letterSpacing: 0.5),
                    ),
                    const SizedBox(height: 6),
                    Text(event.symptoms!, style: TextStyle(fontSize: 13.5, color: context.textSecondary, height: 1.4)),
                  ],
                ),
              ),
            ],

            const SizedBox(height: 28),
            Text(
              isVi ? 'Xem chi tiết' : 'View details',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: context.textPrimary),
            ),
            const SizedBox(height: 14),

            // Lưới 2x2 — 4 lối vào cùng cấp độ quan trọng, không cần đọc tuần tự như 1 danh sách dọc.
            IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: _optionCard(
                      context,
                      icon: Iconsax.health,
                      color: AppColors.accent,
                      title: isVi ? 'Chẩn đoán' : 'Diagnosis',
                      subtitle: event.diagnoses.isEmpty
                          ? (isVi ? 'Chưa có phiếu khám' : 'No diagnosis yet')
                          : (isVi ? 'Xem phiếu khám' : 'View exam record'),
                      onTap: () => context.push(AppRoutes.diagnosisDetail, extra: event),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _optionCard(
                      context,
                      icon: Iconsax.health1,
                      color: AppColors.secondary,
                      title: isVi ? 'Liệu trình' : 'Treatment',
                      subtitle: treatmentCount == null
                          ? (isVi ? 'Đang tải...' : 'Loading...')
                          : treatmentCount == 0
                              ? (isVi ? 'Chưa có liệu trình' : 'None yet')
                              : (isVi ? '$treatmentCount dịch vụ' : '$treatmentCount items'),
                      onTap: () => context.push(AppRoutes.treatmentPlan, extra: event),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: _optionCard(
                      context,
                      icon: Iconsax.document_text,
                      color: AppColors.primary,
                      title: isVi ? 'Đơn thuốc' : 'Prescription',
                      subtitle: event.prescriptionItems.isEmpty
                          ? (isVi ? 'Chưa có đơn thuốc' : 'None yet')
                          : (isVi ? '${event.prescriptionItems.length} loại thuốc' : '${event.prescriptionItems.length} medicines'),
                      onTap: () => context.push(AppRoutes.prescriptionDetail, extra: event),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _optionCard(
                      context,
                      icon: Iconsax.calendar_2,
                      color: AppColors.success,
                      title: isVi ? 'Tái khám' : 'Follow-up',
                      subtitle: event.followUpDate == null
                          ? (isVi ? 'Chưa hẹn' : 'Not scheduled')
                          : _formatDate(event.followUpDate!),
                      onTap: () => context.push(AppRoutes.followUpDetail, extra: event),
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

  Widget _optionCard(
    BuildContext context, {
    required IconData icon,
    required Color color,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: context.isDark ? 0.1 : 0.03),
            blurRadius: 8,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: color.withValues(alpha: context.isDark ? 0.2 : 0.12),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(icon, color: color, size: 20),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    title,
                    style: TextStyle(fontSize: 14, fontWeight: FontWeight.w800, color: context.textPrimary),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    subtitle,
                    style: TextStyle(fontSize: 11.5, color: context.textSecondary, fontWeight: FontWeight.w600),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

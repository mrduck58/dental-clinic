import 'package:flutter/material.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/utils/money_formatter.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

class PhaseDetailPage extends StatelessWidget {
  final RealTreatmentPlan phase;
  const PhaseDetailPage({super.key, required this.phase});

  String _formatDate(DateTime d) => '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final title = phase.teeth == null || phase.teeth!.isEmpty
        ? phase.serviceName
        : '${phase.serviceName} - Răng ${phase.teeth}';
    final statusLabel = switch (phase.status) {
      'Completed' => isVi ? 'Hoàn thành' : 'Completed',
      'InProgress' => isVi ? 'Đang điều trị' : 'In progress',
      'Cancelled' => isVi ? 'Đã hủy' : 'Cancelled',
      _ => phase.status,
    };
    // Khớp đúng màu trạng thái với treatment_plan_page.dart / bảng PLAN_STATUS bên admin_website.
    final statusColor = switch (phase.status) {
      'Completed' => AppColors.success,
      'InProgress' => AppColors.secondary,
      'Cancelled' => AppColors.primary,
      _ => context.textMuted,
    };

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
          isVi ? 'Chi tiết dịch vụ điều trị' : 'Treatment Item Details',
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
            // Status Card
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: context.divider),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                    blurRadius: 10,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          title,
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: statusColor.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          statusLabel,
                          style: TextStyle(fontSize: 10, fontWeight: FontWeight.w900, color: statusColor),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),
                  Row(
                    children: [
                      Expanded(
                        child: _buildInfoCell(context, isVi ? 'BÁC SĨ THỰC HIỆN' : 'ATTENDING DOCTOR', phase.dentistName),
                      ),
                      Expanded(
                        child: _buildInfoCell(
                          context,
                          isVi ? 'BẢO HÀNH ĐẾN' : 'WARRANTY UNTIL',
                          phase.warrantyUntil == null ? '—' : _formatDate(phase.warrantyUntil!),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: _buildInfoCell(context, isVi ? 'CHI PHÍ' : 'COST', formatVnd(phase.totalCost)),
                      ),
                      Expanded(
                        child: _buildInfoCell(context, isVi ? 'ĐÃ THANH TOÁN' : 'PAID', formatVnd(phase.amountPaid)),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 28),

            if (phase.notes != null && phase.notes!.isNotEmpty) ...[
              Text(
                isVi ? 'Ghi chú' : 'Notes',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: context.textPrimary),
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
                  phase.notes!,
                  style: TextStyle(fontSize: 13.5, color: context.textSecondary, height: 1.5, fontWeight: FontWeight.w500),
                ),
              ),
              const SizedBox(height: 28),
            ],

            // Real step-progress log
            Text(
              isVi ? 'Nhật ký tiến độ điều trị' : 'Treatment Progress Log',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: context.textPrimary),
            ),
            const SizedBox(height: 10),
            if (phase.stepProgress.isEmpty)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: context.card,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: context.divider),
                ),
                child: Text(
                  isVi ? 'Chưa có mục nào trong nhật ký tiến độ.' : 'No progress entries logged yet.',
                  style: TextStyle(color: context.textMuted),
                ),
              )
            else
              Container(
                decoration: BoxDecoration(
                  color: context.card,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: context.divider),
                ),
                child: Column(
                  children: List.generate(phase.stepProgress.length, (i) {
                    final step = phase.stepProgress[i];
                    final displayNumber = step.stepNumber > 0 ? step.stepNumber : (i + 1);
                    return Column(
                      children: [
                        Padding(
                          padding: const EdgeInsets.all(16),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Container(
                                width: 32,
                                height: 32,
                                decoration: BoxDecoration(
                                  color: AppColors.success.withValues(alpha: context.isDark ? 0.22 : 0.12),
                                  shape: BoxShape.circle,
                                ),
                                child: Center(
                                  child: Text(
                                    '$displayNumber',
                                    style: const TextStyle(fontWeight: FontWeight.w900, color: AppColors.success, fontSize: 13),
                                  ),
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Expanded(
                                          child: Text(
                                            step.stepName,
                                            style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w800, color: context.textPrimary),
                                          ),
                                        ),
                                        Text(
                                          '${step.percent}%',
                                          style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w900, color: AppColors.success),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      '${_formatDate(step.date)} · ${step.dentistName}',
                                      style: TextStyle(fontSize: 11.5, color: context.textMuted, fontWeight: FontWeight.w600),
                                    ),
                                    if (step.note != null && step.note!.isNotEmpty) ...[
                                      const SizedBox(height: 6),
                                      Text(
                                        step.note!,
                                        style: TextStyle(fontSize: 12.5, color: context.textSecondary, height: 1.4),
                                      ),
                                    ],
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                        if (i != phase.stepProgress.length - 1) Divider(color: context.divider, height: 1),
                      ],
                    );
                  }),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoCell(BuildContext context, String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: 10,
            fontWeight: FontWeight.w800,
            color: context.textMuted,
            letterSpacing: 0.5,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w800,
            color: context.textPrimary,
          ),
        ),
      ],
    );
  }
}

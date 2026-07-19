import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

class TreatmentPlanPage extends StatefulWidget {
  final MedicalHistoryEvent event;
  const TreatmentPlanPage({super.key, required this.event});

  @override
  State<TreatmentPlanPage> createState() => _TreatmentPlanPageState();
}

class _TreatmentPlanPageState extends State<TreatmentPlanPage> {
  List<RealTreatmentPlan> _phases = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final plans = await MedicalRecordService().getMyTreatmentPlans(patientId: widget.event.patientId);
      // Gộp theo cả chuỗi tái khám (không chỉ đúng buổi hẹn đang xem) — liệu trình dài hạn
      // (niềng răng, cấy ghép...) được tạo ở buổi đầu tiên, các buổi tái khám sau chỉ ghi thêm
      // tiến độ vào CÙNG 1 liệu trình đó chứ không tạo liệu trình mới.
      final chainIds = widget.event.treatmentChainIds;
      setState(() {
        _phases = plans.where((p) => p.appointmentId != null && chainIds.contains(p.appointmentId)).toList();
        _isLoading = false;
      });
    } catch (_) {
      setState(() {
        _error = 'load_failed';
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    // Tính theo trạng thái THẬT của từng dịch vụ (không phải % của bước điều trị gần nhất — mỗi
    // bước chỉ ghi % hoàn thành CỦA RIÊNG bước đó, không phải % của cả liệu trình, nên không thể
    // suy ra tiến độ tổng thể từ đó). "Hoàn thành" = số dịch vụ đã Completed / tổng số dịch vụ còn
    // hiệu lực (bỏ qua dịch vụ đã hủy) — khớp đúng cách admin_website hiển thị trạng thái.
    final activePhases = _phases.where((p) => p.status != 'Cancelled').toList();
    final completedCount = activePhases.where((p) => p.status == 'Completed').length;
    final overallProgress = activePhases.isEmpty ? 0.0 : completedCount / activePhases.length;

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
          isVi ? 'Kế hoạch điều trị' : 'Active Treatment Plan',
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
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        isVi ? 'Không thể tải kế hoạch điều trị.' : 'Failed to load treatment plan.',
                        style: TextStyle(color: context.textMuted),
                      ),
                      const SizedBox(height: 12),
                      TextButton(onPressed: _load, child: Text(isVi ? 'Thử lại' : 'Retry')),
                    ],
                  ),
                )
              : SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Patient Header Card with Circular Chart
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
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isVi ? 'TIẾN TRÌNH ĐIỀU TRỊ' : 'ACTIVE TREATMENT PLAN',
                          style: const TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w900,
                            color: AppColors.primary,
                            letterSpacing: 0.5,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          widget.event.patientName,
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          widget.event.appointmentCode,
                          style: TextStyle(
                            fontSize: 13,
                            color: context.textSecondary,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Icon(Iconsax.user, size: 14, color: context.textMuted),
                            const SizedBox(width: 6),
                            Text(
                              widget.event.dentistName,
                              style: TextStyle(fontSize: 12, color: context.textSecondary, fontWeight: FontWeight.w600),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 16),
                  // Circular Progress Indicator — xanh (success) vì thể hiện phần ĐÃ hoàn thành,
                  // thu nhỏ lại so với trước để đỡ lấn át nội dung chính bên trái.
                  Stack(
                    alignment: Alignment.center,
                    children: [
                      SizedBox(
                        width: 64,
                        height: 64,
                        child: CircularProgressIndicator(
                          value: overallProgress,
                          strokeWidth: 6,
                          backgroundColor: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                          color: AppColors.success,
                        ),
                      ),
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            '${(overallProgress * 100).round()}%',
                            style: const TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w900,
                              color: AppColors.success,
                            ),
                          ),
                          Text(
                            '$completedCount/${activePhases.length}',
                            style: TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w700,
                              color: context.textMuted,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 28),

            // Phases Timeline Section Title
            Text(
              isVi ? 'Các dịch vụ điều trị' : 'Treatment Items',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
            ),
            const SizedBox(height: 16),

            if (_phases.isEmpty)
              Text(
                isVi ? 'Chưa có liệu trình nào cho buổi khám này.' : 'No treatment items for this visit yet.',
                style: TextStyle(color: context.textMuted),
              )
            else
              ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: _phases.length,
                itemBuilder: (context, index) {
                  final phase = _phases[index];
                  return _buildPhaseItem(context, phase, isVi, index == _phases.length - 1);
                },
              ),
          ],
        ),
      ),
    );
  }

  /// Màu + nhãn trạng thái — khớp đúng bảng PLAN_STATUS bên app quản lý (apps/admin_website)
  /// để bệnh nhân và bác sĩ nhìn thấy cùng một ngôn ngữ màu sắc cho cùng 1 trạng thái.
  (Color, String, String) _statusStyle(String status, bool isVi) => switch (status) {
        'Completed' => (AppColors.success, isVi ? 'HOÀN THÀNH' : 'COMPLETED', isVi ? 'Hoàn thành' : 'Completed'),
        'InProgress' => (AppColors.secondary, isVi ? 'ĐANG LÀM' : 'IN PROGRESS', isVi ? 'Đang làm' : 'In progress'),
        'Cancelled' => (AppColors.primary, isVi ? 'ĐÃ HỦY' : 'CANCELLED', isVi ? 'Đã hủy' : 'Cancelled'),
        _ => (context.textMuted, isVi ? 'CHỜ THỰC HIỆN' : 'PLANNED', isVi ? 'Chờ thực hiện' : 'Planned'),
      };

  Widget _buildPhaseItem(BuildContext context, RealTreatmentPlan phase, bool isVi, bool isLast) {
    final (statusColor, statusLabel, _) = _statusStyle(phase.status, isVi);
    final isCompleted = phase.status == 'Completed';
    final isInProgress = phase.status == 'InProgress';

    final title = phase.teeth == null || phase.teeth!.isEmpty
        ? phase.serviceName
        : '${phase.serviceName} - Răng ${phase.teeth}';

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Column(
            children: [
              Container(
                width: 14,
                height: 14,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: isCompleted || isInProgress ? statusColor : Colors.transparent,
                  border: Border.all(color: statusColor, width: 2.5),
                ),
              ),
              if (!isLast)
                Expanded(
                  child: Container(
                    width: 2,
                    color: context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                  ),
                ),
            ],
          ),
          const SizedBox(width: 16),

          Expanded(
            child: Container(
              margin: const EdgeInsets.only(bottom: 20),
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(18),
                border: Border.all(color: isInProgress ? statusColor.withValues(alpha: 0.35) : context.divider),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: context.isDark ? 0.1 : 0.03),
                    blurRadius: 8,
                    offset: const Offset(0, 3),
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
                            fontSize: 14,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: statusColor.withValues(alpha: context.isDark ? 0.22 : 0.1),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          statusLabel,
                          style: TextStyle(fontSize: 9, fontWeight: FontWeight.w900, color: statusColor),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),

                  // Step progress log (dữ liệu thật)
                  if (phase.stepProgress.isEmpty)
                    Text(
                      isVi ? 'Chưa có nhật ký tiến độ.' : 'No progress logged yet.',
                      style: TextStyle(fontSize: 12.5, color: context.textMuted, fontWeight: FontWeight.w500),
                    )
                  else
                    ...phase.stepProgress.map((step) {
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 8.0),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: [
                            Container(
                              width: 20,
                              height: 20,
                              alignment: Alignment.center,
                              decoration: BoxDecoration(
                                color: AppColors.success.withValues(alpha: context.isDark ? 0.22 : 0.12),
                                shape: BoxShape.circle,
                              ),
                              child: step.stepNumber > 0
                                  ? Text(
                                      '${step.stepNumber}',
                                      style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w900, color: AppColors.success),
                                    )
                                  : const Icon(Icons.add_rounded, size: 12, color: AppColors.success),
                            ),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                step.stepName,
                                style: TextStyle(
                                  fontSize: 12.5,
                                  color: context.textSecondary,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                            Text(
                              '${step.percent}%',
                              style: const TextStyle(fontSize: 11.5, fontWeight: FontWeight.w800, color: AppColors.success),
                            ),
                          ],
                        ),
                      );
                    }),
                  const SizedBox(height: 12),

                  Divider(color: context.divider, height: 1),
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          Icon(Iconsax.clock, size: 12, color: context.textMuted),
                          const SizedBox(width: 4),
                          Text(
                            isVi ? 'Bác sĩ: ${phase.dentistName}' : 'Dentist: ${phase.dentistName}',
                            style: TextStyle(
                              fontSize: 11,
                              color: context.textMuted,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                      if (phase.stepProgress.isNotEmpty)
                        GestureDetector(
                          onTap: () {
                            context.push(AppRoutes.phaseDetail, extra: phase);
                          },
                          child: Row(
                            children: [
                              Text(
                                isVi ? 'Xem chi tiết' : 'View Details',
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w800,
                                  color: AppColors.primary,
                                ),
                              ),
                              const SizedBox(width: 2),
                              const Icon(Iconsax.arrow_right_3, size: 12, color: AppColors.primary),
                            ],
                          ),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

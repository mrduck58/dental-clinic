import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_mock.dart';

class TreatmentPlanPage extends StatelessWidget {
  final MedicalRecordEvent event;
  const TreatmentPlanPage({super.key, required this.event});

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final phases = event.phases ?? [];

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
        actions: [
          IconButton(
            icon: Icon(Iconsax.more, color: context.textPrimary),
            onPressed: () {},
          ),
        ],
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
                          isVi ? 'Alex Johnson' : 'Alex Johnson',
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          isVi ? 'Mã BN: #8821' : 'Patient ID: #8821',
                          style: TextStyle(
                            fontSize: 13,
                            color: context.textSecondary,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Icon(Iconsax.calendar_1, size: 14, color: context.textMuted),
                            const SizedBox(width: 6),
                            Text(
                              isVi ? 'Dự kiến: Th11 2024' : 'Est. Completion: Nov 2024',
                              style: TextStyle(fontSize: 12, color: context.textSecondary, fontWeight: FontWeight.w600),
                            ),
                          ],
                        ),
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            Icon(Iconsax.user, size: 14, color: context.textMuted),
                            const SizedBox(width: 6),
                            Text(
                              event.doctorName,
                              style: TextStyle(fontSize: 12, color: context.textSecondary, fontWeight: FontWeight.w600),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 16),
                  // Circular Progress Indicator
                  Stack(
                    alignment: Alignment.center,
                    children: [
                      SizedBox(
                        width: 80,
                        height: 80,
                        child: CircularProgressIndicator(
                          value: 0.65,
                          strokeWidth: 8,
                          backgroundColor: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                          color: AppColors.primary,
                        ),
                      ),
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Text(
                            '65%',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w900,
                              color: AppColors.primary,
                            ),
                          ),
                          Text(
                            isVi ? 'Tổng thể' : 'Overall',
                            style: TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w800,
                              color: context.textSecondary,
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
              isVi ? 'Các giai đoạn điều trị' : 'Treatment Phases',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
            ),
            const SizedBox(height: 16),

            // Phases ListView
            ListView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: phases.length,
              itemBuilder: (context, index) {
                final phase = phases[index];
                return _buildPhaseItem(context, phase, isVi, index == phases.length - 1);
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPhaseItem(BuildContext context, TreatmentPlanPhase phase, bool isVi, bool isLast) {
    Color cardBorderColor = context.divider;
    Widget statusIcon = const Icon(Icons.circle_outlined, color: Colors.grey, size: 18);
    bool isCompleted = phase.status == 'Completed';
    bool isInProgress = phase.status == 'In Progress';

    if (isCompleted) {
      statusIcon = const Icon(Icons.check_circle, color: Color(0xFF16A34A), size: 18);
    } else if (isInProgress) {
      cardBorderColor = AppColors.primary.withValues(alpha: 0.3);
      statusIcon = Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: AppColors.primary.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(6),
          border: Border.all(color: AppColors.primary.withValues(alpha: 0.2)),
        ),
        child: Text(
          isVi ? 'ĐANG LÀM' : 'IN PROCESS',
          style: const TextStyle(fontSize: 8, fontWeight: FontWeight.w900, color: AppColors.primary),
        ),
      );
    }

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Timeline indicator column
          Column(
            children: [
              Container(
                width: 14,
                height: 14,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: isCompleted ? const Color(0xFF16A34A) : (isInProgress ? AppColors.primary : Colors.transparent),
                  border: Border.all(
                    color: isCompleted ? const Color(0xFF16A34A) : (isInProgress ? AppColors.primary : const Color(0xFF94A3B8)),
                    width: 2.5,
                  ),
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

          // Card content column
          Expanded(
            child: Container(
              margin: const EdgeInsets.only(bottom: 20),
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: phase.status == 'Upcoming'
                    ? (context.isDark ? const Color(0xFF0F172A).withValues(alpha: 0.2) : const Color(0xFFF8FAFC))
                    : context.card,
                borderRadius: BorderRadius.circular(18),
                border: Border.all(color: cardBorderColor),
                boxShadow: [
                  if (phase.status != 'Upcoming')
                    BoxShadow(
                      color: Colors.black.withValues(alpha: context.isDark ? 0.1 : 0.03),
                      blurRadius: 8,
                      offset: const Offset(0, 3),
                    ),
                ],
              ),
              child: Opacity(
                opacity: phase.status == 'Upcoming' ? 0.5 : 1.0,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Header title + Status
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            phase.title,
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w800,
                              color: context.textPrimary,
                            ),
                          ),
                        ),
                        statusIcon,
                      ],
                    ),
                    const SizedBox(height: 12),

                    // Tasks Checklist
                    ...phase.tasks.map((task) {
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 8.0),
                        child: Row(
                          children: [
                            Icon(
                              task.isCompleted ? Icons.check_box : Icons.check_box_outline_blank,
                              size: 16,
                              color: task.isCompleted ? const Color(0xFF16A34A) : context.textMuted,
                            ),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                task.title,
                                style: TextStyle(
                                  fontSize: 12.5,
                                  color: task.isCompleted ? context.textSecondary : context.textMuted,
                                  fontWeight: task.isCompleted ? FontWeight.w600 : FontWeight.w500,
                                ),
                              ),
                            ),
                          ],
                        ),
                      );
                    }),
                    const SizedBox(height: 12),

                    // Bottom Row: Timeline + Action Link
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
                              '${phase.durationText} · ${phase.finishedDate}',
                              style: TextStyle(
                                fontSize: 11,
                                color: context.textMuted,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                        if (isInProgress)
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
          ),
        ],
      ),
    );
  }
}

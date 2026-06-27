import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_mock.dart';

class PhaseDetailPage extends StatelessWidget {
  final TreatmentPlanPhase phase;
  const PhaseDetailPage({super.key, required this.phase});

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

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
          isVi ? 'Chi tiết giai đoạn' : 'Phase Details',
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
                      Text(
                        isVi ? 'Giai đoạn 2' : 'Phase 2',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w900,
                          color: AppColors.primary,
                          letterSpacing: 0.5,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF16A34A).withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          isVi ? 'Hoàn thành' : 'Completed',
                          style: const TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.w900,
                            color: Color(0xFF16A34A),
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    isVi ? 'Điều trị tủy răng lần 2' : 'Root Canal Treatment (Session 2)',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: context.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Info grid
                  Row(
                    children: [
                      Expanded(
                        child: _buildInfoCell(
                          context,
                          isVi ? 'NGÀY ĐIỀU TRỊ' : 'TREATMENT DATE',
                          '27/07/2026',
                        ),
                      ),
                      Expanded(
                        child: _buildInfoCell(
                          context,
                          isVi ? 'BÁC SĨ THỰC HIỆN' : 'ATTENDING DOCTOR',
                          isVi ? 'BS. Nguyễn Văn A' : 'Dr. Alan Nguyen',
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: _buildInfoCell(
                          context,
                          isVi ? 'THỜI GIAN KHÁM' : 'DURATION',
                          isVi ? '45 phút' : '45 minutes',
                        ),
                      ),
                      Expanded(
                        child: _buildInfoCell(
                          context,
                          isVi ? 'TÌNH TRẠNG' : 'STATUS',
                          isVi ? 'Đã hoàn thành' : 'Completed',
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 28),

            // Symptoms checklists
            Text(
              isVi ? 'Triệu chứng lâm sàng' : 'Clinical Symptoms Reported',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
            ),
            const SizedBox(height: 10),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.divider),
              ),
              child: Column(
                children: [
                  _buildSymptomRow(context, isVi ? 'Cảm giác đau nhức răng' : 'Toothache pain intensity', isVi ? 'Không còn đau' : 'No pain', true),
                  Divider(color: context.divider, height: 1),
                  _buildSymptomRow(context, isVi ? 'Nhạy cảm nhiệt độ (nóng/lạnh)' : 'Sensitivity to heat/cold', isVi ? 'Ê buốt nhẹ' : 'Mild sensitivity', false),
                  Divider(color: context.divider, height: 1),
                  _buildSymptomRow(context, isVi ? 'Sưng nướu quanh chân răng' : 'Swelling around target gum area', isVi ? 'Hết sưng' : 'Normal', true),
                ],
              ),
            ),
            const SizedBox(height: 28),

            // Results Section
            Text(
              isVi ? 'Kết quả điều trị' : 'Treatment Results',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
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
                isVi
                    ? 'Đã tiến hành làm sạch toàn bộ buồng tủy, bơm rửa sát khuẩn các ống tủy chân răng. Đặt thuốc sát trùng và trám tạm theo đúng quy trình.'
                    : 'Cleaned the entire pulp chamber, flushed and disinfected all root canal pathways. Placed medicaments and sealed temporarily according to standard guidelines.',
                style: TextStyle(
                  fontSize: 13.5,
                  color: context.textSecondary,
                  height: 1.5,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
            const SizedBox(height: 28),

            // Before / After Comparison
            Text(
              isVi ? 'Hình ảnh trước / sau' : 'Before / After Comparison',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: _buildComparisonImage(
                    context,
                    isVi ? 'Trước điều trị' : 'Before',
                    const Color(0xFFFCA5A5), // inflammed pinkish red background
                    isVi ? 'Tủy viêm xám' : 'Inflamed grey pulp',
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: _buildComparisonImage(
                    context,
                    isVi ? 'Sau điều trị' : 'After',
                    const Color(0xFF93C5FD), // clean blue background
                    isVi ? 'Ống tủy sạch' : 'Clean root canals',
                  ),
                ),
              ],
            ),
            const SizedBox(height: 28),

            // Actions Row / Cards
            Text(
              isVi ? 'Tác vụ liên quan' : 'Related Actions',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
            ),
            const SizedBox(height: 14),
            _buildActionCard(
              context,
              icon: Iconsax.health,
              title: isVi ? 'Chi tiết điều trị' : 'Treatment Details',
              subtitle: isVi ? 'Xem quy trình và vật liệu sử dụng' : 'Check procedures and materials used',
              onTap: () => context.push(AppRoutes.treatmentDetail),
            ),
            const SizedBox(height: 12),
            _buildActionCard(
              context,
              icon: Iconsax.receipt_2,
              title: isVi ? 'Đơn thuốc' : 'Prescription',
              subtitle: isVi ? 'Xem thông tin đơn thuốc được kê' : 'Check prescribed medicines',
              onTap: () => context.push(AppRoutes.prescriptionDetail),
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

  Widget _buildSymptomRow(BuildContext context, String symptom, String status, bool positive) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(symptom, style: TextStyle(fontSize: 13.5, color: context.textPrimary, fontWeight: FontWeight.w600)),
          Row(
            children: [
              Icon(
                positive ? Icons.check_circle_rounded : Icons.info_outline_rounded,
                color: positive ? const Color(0xFF16A34A) : Colors.amber,
                size: 16,
              ),
              const SizedBox(width: 6),
              Text(
                status,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: positive ? const Color(0xFF16A34A) : Colors.amber,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildComparisonImage(BuildContext context, String label, Color dentalColor, String desc) {
    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        children: [
          Container(
            height: 100,
            width: double.infinity,
            decoration: BoxDecoration(
              color: dentalColor.withValues(alpha: 0.2),
              borderRadius: const BorderRadius.vertical(top: Radius.circular(15)),
            ),
            child: Icon(Iconsax.flash_1, color: dentalColor, size: 36),
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              children: [
                Text(
                  label,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w800,
                    color: context.textPrimary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  desc,
                  style: TextStyle(
                    fontSize: 10,
                    color: context.textSecondary,
                    fontWeight: FontWeight.w500,
                  ),
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildActionCard(
    BuildContext context, {
    required IconData icon,
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
              child: Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: AppColors.primaryLight,
                      shape: BoxShape.circle,
                    ),
                    child: Icon(icon, color: AppColors.primary, size: 22),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          title,
                          style: TextStyle(
                            fontSize: 14.5,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          subtitle,
                          style: TextStyle(
                            fontSize: 12,
                            color: context.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Icon(Icons.arrow_forward_ios_rounded, size: 14, color: context.textMuted),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

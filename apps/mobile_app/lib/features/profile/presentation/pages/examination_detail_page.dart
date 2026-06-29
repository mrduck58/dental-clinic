import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_mock.dart';

class ExaminationDetailPage extends StatelessWidget {
  final MedicalRecordEvent event;
  const ExaminationDetailPage({super.key, required this.event});

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
          isVi ? 'Chi tiết khám bệnh' : 'Examination Detail',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: Icon(Iconsax.notification, color: context.textPrimary),
            onPressed: () {},
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Visit Date Header Box
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
                          width: 40,
                          height: 40,
                          decoration: BoxDecoration(
                            color: AppColors.primaryLight,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Iconsax.calendar_1, color: AppColors.primary, size: 20),
                        ),
                        const SizedBox(width: 12),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              isVi ? 'NGÀY KHÁM' : 'Visit Date',
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w800,
                                color: context.textMuted,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              event.dateStr,
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w800,
                                color: context.textPrimary,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Health metrics indicators row
                  Row(
                    children: [
                      Expanded(
                        child: _buildMetricTile(
                          context,
                          icon: Iconsax.weight,
                          title: isVi ? 'Cân nặng' : 'Weight',
                          value: '68 kg',
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: _buildMetricTile(
                          context,
                          icon: Iconsax.heart,
                          title: isVi ? 'Huyết áp' : 'Blood Pres.',
                          value: '120/80',
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: _buildMetricTile(
                          context,
                          icon: Iconsax.info_circle,
                          title: isVi ? 'Nhóm máu' : 'Blood Type',
                          value: 'O+',
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),

                  // Attending Dentist card
                  Text(
                    isVi ? 'Bác sĩ phụ trách' : 'Attending Dentist',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w800,
                      color: context.textMuted,
                      letterSpacing: 0.5,
                    ),
                  ),
                  const SizedBox(height: 10),
                  Container(
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
                          decoration: const BoxDecoration(
                            color: AppColors.primaryLight,
                            shape: BoxShape.circle,
                            image: DecorationImage(
                              image: AssetImage('assets/images/bac_si_1.png'),
                              fit: BoxFit.cover,
                            ),
                          ),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                event.doctorName,
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w800,
                                  color: context.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                event.doctorSpecialty,
                                style: TextStyle(
                                  fontSize: 12,
                                  color: context.textSecondary,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Diagnosis & Notes
                  Text(
                    isVi ? 'Chẩn đoán & Ghi chú lâm sàng' : 'Diagnosis & Clinical Notes',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w800,
                      color: context.textMuted,
                      letterSpacing: 0.5,
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
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.description_outlined, color: AppColors.primary, size: 18),
                            const SizedBox(width: 8),
                            Text(
                              isVi ? 'Kế hoạch chẩn trị' : 'Clinical Summary',
                              style: TextStyle(fontWeight: FontWeight.w800, fontSize: 13, color: context.textPrimary),
                            ),
                          ],
                        ),
                        const SizedBox(height: 10),
                        Text(
                          event.diagnosis ?? (isVi ? 'Không có ghi chú.' : 'No notes available.'),
                          style: TextStyle(
                            fontSize: 13.5,
                            color: context.textSecondary,
                            height: 1.5,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Diagnostic X-Rays
                  Text(
                    isVi ? 'Hình ảnh X-Quang chẩn đoán' : 'Diagnostic X-Rays',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w800,
                      color: context.textMuted,
                      letterSpacing: 0.5,
                    ),
                  ),
                  const SizedBox(height: 10),
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: (event.xRays ?? ['Dental X-Ray']).map((xray) {
                        return Container(
                          width: 110,
                          margin: const EdgeInsets.only(right: 12),
                          decoration: BoxDecoration(
                            color: context.card,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(color: context.divider),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // Dark x-ray negative simulation container
                              Container(
                                height: 80,
                                width: double.infinity,
                                decoration: const BoxDecoration(
                                  color: Color(0xFF0F172A),
                                  borderRadius: BorderRadius.vertical(top: Radius.circular(15)),
                                ),
                                child: Stack(
                                  alignment: Alignment.center,
                                  children: [
                                    Opacity(
                                      opacity: 0.15,
                                      child: Icon(Icons.grid_on, color: Colors.white, size: 60),
                                    ),
                                    const Icon(Icons.broken_image_outlined, color: Colors.white60, size: 24),
                                  ],
                                ),
                              ),
                              Padding(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
                                child: Text(
                                  xray,
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w800,
                                    color: context.textPrimary,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  textAlign: TextAlign.center,
                                ),
                              ),
                            ],
                          ),
                        );
                      }).toList(),
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Bottom buttons
          Container(
            padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
            decoration: BoxDecoration(
              color: context.card,
              border: Border(top: BorderSide(color: context.divider)),
            ),
            child: Column(
              children: [
                SizedBox(
                  width: double.infinity,
                  height: 48,
                  child: ElevatedButton.icon(
                    onPressed: () {
                      final activePlan = MedicalRecordMock.events.firstWhere((e) => e.isJourney);
                      context.push(AppRoutes.treatmentPlan, extra: activePlan);
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.primary,
                      elevation: 0,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                    icon: const Icon(Icons.assignment_outlined, color: Colors.white, size: 18),
                    label: Text(
                      isVi ? 'Xem kế hoạch điều trị' : 'View Treatment Plan',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 14),
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  height: 48,
                  child: OutlinedButton.icon(
                    onPressed: () {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(isVi ? 'Đang tải tóm tắt khám bệnh (PDF)...' : 'Downloading exam summary (PDF)...'),
                          behavior: SnackBarBehavior.floating,
                        ),
                      );
                    },
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppColors.primary,
                      side: const BorderSide(color: AppColors.primary),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                    icon: const Icon(Icons.download, size: 18),
                    label: Text(
                      isVi ? 'Tải tóm tắt khám bệnh (PDF)' : 'Download Summary (PDF)',
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
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

  Widget _buildMetricTile(BuildContext context, {required IconData icon, required String title, required String value}) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 10),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        children: [
          Icon(icon, color: AppColors.primary, size: 20),
          const SizedBox(height: 6),
          Text(
            title,
            style: TextStyle(fontSize: 10, color: context.textSecondary, fontWeight: FontWeight.w500),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 2),
          Text(
            value,
            style: TextStyle(fontSize: 13, color: context.textPrimary, fontWeight: FontWeight.w800),
          ),
        ],
      ),
    );
  }
}

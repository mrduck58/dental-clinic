import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

/// Thông tin chẩn đoán (phiếu khám răng miệng) — chỉ đọc, hiển thị đúng các trường bác sĩ đã ghi
/// nhận ở tab "Chuẩn đoán" trên app quản lý (apps/admin_website).
class DiagnosisDetailPage extends StatelessWidget {
  final MedicalHistoryEvent event;
  const DiagnosisDetailPage({super.key, required this.event});

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final diagnosis = event.diagnoses.isEmpty ? null : event.diagnoses.first;

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
          isVi ? 'Thông tin chẩn đoán' : 'Diagnosis',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w800, fontSize: 18),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: diagnosis == null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 32),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Iconsax.document, size: 40, color: context.textMuted),
                    const SizedBox(height: 12),
                    Text(
                      isVi ? 'Chưa có phiếu khám cho buổi hẹn này.' : 'No diagnosis recorded for this visit.',
                      style: TextStyle(color: context.textMuted),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Headline: main diagnosis description
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(18),
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: context.isDark
                            ? [AppColors.primary.withValues(alpha: 0.28), AppColors.primary.withValues(alpha: 0.12)]
                            : [AppColors.primary.withValues(alpha: 0.10), AppColors.primaryLight.withValues(alpha: 0.5)],
                      ),
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: AppColors.primary.withValues(alpha: 0.18)),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          padding: const EdgeInsets.all(10),
                          decoration: const BoxDecoration(color: AppColors.primary, shape: BoxShape.circle),
                          child: const Icon(Iconsax.search_normal_1, color: Colors.white, size: 20),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                isVi ? 'CHẨN ĐOÁN' : 'DIAGNOSIS',
                                style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w900, color: AppColors.primary, letterSpacing: 0.6),
                              ),
                              const SizedBox(height: 6),
                              Text(
                                diagnosis.description,
                                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: context.textPrimary, height: 1.35),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  _section(
                    context,
                    icon: Iconsax.heart,
                    color: AppColors.primary,
                    title: isVi ? 'Tình trạng lợi – niêm mạc' : 'Gum & Oral Mucosa',
                    fields: [
                      if (diagnosis.gumCondition != null) MapEntry(isVi ? 'Tình trạng lợi' : 'Gum condition', diagnosis.gumCondition!),
                      if (diagnosis.oralMucosaCondition != null) MapEntry(isVi ? 'Tình trạng niêm mạc miệng' : 'Oral mucosa', diagnosis.oralMucosaCondition!),
                      if (diagnosis.gumBleeding != null) MapEntry(isVi ? 'Chảy máu lợi' : 'Gum bleeding', diagnosis.gumBleeding!),
                      if (diagnosis.painOnChewing != null) MapEntry(isVi ? 'Đau khi chạm / ăn nhai' : 'Pain on chewing', diagnosis.painOnChewing!),
                    ],
                  ),
                  _section(
                    context,
                    icon: Iconsax.health,
                    color: AppColors.secondary,
                    title: isVi ? 'Tình trạng răng' : 'Teeth Condition',
                    fields: [
                      if (diagnosis.teethCount != null) MapEntry(isVi ? 'Số răng hiện có' : 'Teeth count', diagnosis.teethCount!),
                      if (diagnosis.decayedTeeth != null) MapEntry(isVi ? 'Răng sâu' : 'Decayed teeth', diagnosis.decayedTeeth!),
                      if (diagnosis.wornOrBrokenTeeth != null) MapEntry(isVi ? 'Răng mòn / nứt / vỡ' : 'Worn/broken teeth', diagnosis.wornOrBrokenTeeth!),
                      if (diagnosis.looseTeeth != null) MapEntry(isVi ? 'Răng lung lay' : 'Loose teeth', diagnosis.looseTeeth!),
                    ],
                  ),
                  _section(
                    context,
                    icon: Iconsax.magic_star,
                    color: AppColors.success,
                    title: isVi ? 'Vệ sinh răng miệng' : 'Oral Hygiene',
                    fields: [
                      if (diagnosis.tartar != null) MapEntry(isVi ? 'Cao răng' : 'Tartar', diagnosis.tartar!),
                      if (diagnosis.plaque != null) MapEntry(isVi ? 'Mảng bám' : 'Plaque', diagnosis.plaque!),
                      if (diagnosis.badBreath != null) MapEntry(isVi ? 'Mùi hôi miệng' : 'Bad breath', diagnosis.badBreath!),
                    ],
                  ),
                  _section(
                    context,
                    icon: Iconsax.activity,
                    color: AppColors.accent,
                    title: isVi ? 'Khớp thái dương hàm & khớp cắn' : 'TMJ & Occlusion',
                    fields: [
                      if (diagnosis.tmjSymptoms != null) MapEntry(isVi ? 'Triệu chứng khớp TDH' : 'TMJ symptoms', diagnosis.tmjSymptoms!),
                      if (diagnosis.occlusion != null) MapEntry(isVi ? 'Khớp cắn' : 'Occlusion', diagnosis.occlusion!),
                      if (diagnosis.occlusionDeviation != null) MapEntry(isVi ? 'Sai lệch khớp cắn' : 'Occlusion deviation', diagnosis.occlusionDeviation!),
                    ],
                  ),
                  if (diagnosis.conclusion != null && diagnosis.conclusion!.isNotEmpty)
                    _section(
                      context,
                      icon: Iconsax.document_text,
                      color: AppColors.primaryDark,
                      title: isVi ? 'Kết quả & kế hoạch điều trị' : 'Conclusion & Treatment Plan',
                      fields: [MapEntry(isVi ? 'Ghi chú' : 'Notes', diagnosis.conclusion!)],
                    ),
                ],
              ),
            ),
    );
  }

  Widget _section(
    BuildContext context, {
    required IconData icon,
    required Color color,
    required String title,
    required List<MapEntry<String, String>> fields,
  }) {
    if (fields.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Container(
        width: double.infinity,
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: context.divider),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: context.isDark ? 0.12 : 0.03),
              blurRadius: 8,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 10),
              child: Row(
                children: [
                  Container(
                    width: 34,
                    height: 34,
                    decoration: BoxDecoration(
                      color: context.isDark ? color.withValues(alpha: 0.2) : color.withValues(alpha: 0.12),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(icon, color: color, size: 18),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      title,
                      style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w800, color: context.textPrimary),
                    ),
                  ),
                ],
              ),
            ),
            Divider(color: context.divider, height: 1),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: fields.map((f) {
                  final isLast = f == fields.last;
                  return Padding(
                    padding: EdgeInsets.only(bottom: isLast ? 0 : 12),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          margin: const EdgeInsets.only(top: 6),
                          width: 5,
                          height: 5,
                          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                f.key,
                                style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w700, color: context.textSecondary),
                              ),
                              const SizedBox(height: 3),
                              Text(
                                f.value,
                                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: context.textPrimary, height: 1.4),
                              ),
                            ],
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
    );
  }
}

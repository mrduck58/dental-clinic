import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_mock.dart';

class MedicineDetailPage extends StatelessWidget {
  final MedicineModel medicine;
  const MedicineDetailPage({super.key, required this.medicine});

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
          isVi ? 'Thông tin chi tiết thuốc' : 'Medicine Details',
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
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Stylized Drug Header Box
                  Container(
                    width: double.infinity,
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
                      children: [
                        Container(
                          width: 64,
                          height: 64,
                          decoration: BoxDecoration(
                            color: AppColors.primaryLight,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Iconsax.box_1, color: AppColors.primary, size: 28),
                        ),
                        const SizedBox(height: 14),
                        Text(
                          medicine.name,
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w900,
                            color: context.textPrimary,
                          ),
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '${medicine.type} · ${medicine.form}',
                          style: TextStyle(
                            fontSize: 13,
                            color: context.textSecondary,
                            fontWeight: FontWeight.w600,
                          ),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 28),

                  // Drug specs checklist
                  _buildSpecificationItem(context, isVi ? 'Thành phần chính' : 'Active Components', medicine.components, Iconsax.info_circle),
                  const SizedBox(height: 16),
                  _buildSpecificationItem(context, isVi ? 'Chỉ định điều trị' : 'Therapeutic Uses', medicine.uses, Iconsax.activity),
                  const SizedBox(height: 16),
                  _buildSpecificationItem(context, isVi ? 'Liều lượng & Cách dùng' : 'Directions & Dosage', medicine.dosage, Iconsax.clock),
                  const SizedBox(height: 16),
                  _buildSpecificationItem(context, isVi ? 'Thời gian sử dụng' : 'Duration', medicine.duration, Iconsax.calendar_1),
                  const SizedBox(height: 16),
                  _buildSpecificationItem(context, isVi ? 'Tác dụng phụ có thể gặp' : 'Potential Side Effects', medicine.sideEffects, Iconsax.danger),
                  const SizedBox(height: 16),
                  _buildSpecificationItem(context, isVi ? 'Lưu ý đặc biệt' : 'Special Precautions', medicine.notes, Iconsax.edit_2),
                ],
              ),
            ),
          ),

          // Setup Pill Reminder notification button
          Container(
            padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
            decoration: BoxDecoration(
              color: context.card,
              border: Border(top: BorderSide(color: context.divider)),
            ),
            child: SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton.icon(
                onPressed: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(
                        isVi
                            ? 'Đã đặt nhắc nhở uống thuốc: ${medicine.name}!'
                            : 'Set pill reminder for ${medicine.name}!',
                      ),
                      behavior: SnackBarBehavior.floating,
                      backgroundColor: const Color(0xFF16A34A),
                    ),
                  );
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  elevation: 0,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                icon: const Icon(Iconsax.notification_status, color: Colors.white, size: 18),
                label: Text(
                  isVi ? 'Đặt nhắc nhở uống thuốc' : 'Set Pill Reminder',
                  style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 14),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSpecificationItem(BuildContext context, String title, String content, IconData icon) {
    return Container(
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
              Icon(icon, color: AppColors.primary, size: 18),
              const SizedBox(width: 8),
              Text(
                title,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w800,
                  color: context.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            content,
            style: TextStyle(
              fontSize: 13,
              color: context.textSecondary,
              height: 1.45,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}

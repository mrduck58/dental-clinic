import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class TreatmentDetailPage extends StatelessWidget {
  const TreatmentDetailPage({super.key});

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
          isVi ? 'Chi tiết điều trị' : 'Treatment Details',
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
            // Treatment overview box
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
                  Text(
                    isVi ? 'THỦ THUẬT' : 'PROCEDURE',
                    style: const TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w900,
                      color: AppColors.primary,
                      letterSpacing: 0.5,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    isVi ? 'Nội nha điều trị tủy chân răng' : 'Endodontic Root Canal Therapy',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: context.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    isVi ? 'Mã thủ thuật: #ENDO-402' : 'Procedure ID: #ENDO-402',
                    style: TextStyle(fontSize: 12, color: context.textSecondary),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Icon(Iconsax.activity, size: 14, color: context.textMuted),
                      const SizedBox(width: 6),
                      Text(
                        isVi ? 'Răng điều trị: #14 (Răng cối nhỏ hàm trên)' : 'Target Tooth: #14 (Upper Right Bicuspid)',
                        style: TextStyle(fontSize: 12.5, color: context.textSecondary, fontWeight: FontWeight.w600),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 28),

            // Timed Procedure Steps
            Text(
              isVi ? 'Quy trình thực hiện' : 'Clinical Steps Performed',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
            ),
            const SizedBox(height: 16),

            // Step timeline list
            _buildStepItem(context, '14:15', isVi ? 'Gây tê bề mặt & Gây tê vùng' : 'Surface & Infiltration Anesthesia', isVi ? 'Thuốc tê Lidocaine 2% giúp giảm đau nhức tuyệt đối.' : 'Lidocaine 2% administered for pain block.', true),
            _buildStepItem(context, '14:25', isVi ? 'Đặt đê cao su vô trùng' : 'Rubber Dam Placement', isVi ? 'Cô lập răng khỏi vi khuẩn tuyến nước bọt khoang miệng.' : 'Isolated tooth from oral fluids and bacteria.', true),
            _buildStepItem(context, '14:30', isVi ? 'Mở tủy & Định vị ống tủy' : 'Access Cavity & Canal Location', isVi ? 'Sử dụng mũi khoan siêu âm định vị 3 ống tủy chân răng.' : 'Located 3 root canals using micro-ultrasonics.', true),
            _buildStepItem(context, '14:45', isVi ? 'Tạo hình ống tủy WaveOne' : 'Canal Shaping & Disinfection', isVi ? 'Dàn ống tủy bằng trâm máy WaveOne Gold, bơm rửa sát khuẩn.' : 'Shaped canals using WaveOne Gold rotary files, flushed with NaOCl.', true),
            _buildStepItem(context, '14:55', isVi ? 'Trám bít tạm thời & Kiểm tra' : 'Temporary Sealing & Post-Op Review', isVi ? 'Đặt thuốc sát khuẩn Caviton và chụp X-Quang kiểm tra lại.' : 'Placed Caviton sealant, checked with digital radiographs.', false),
            
            const SizedBox(height: 16),

            // Materials used
            Text(
              isVi ? 'Vật liệu lâm sàng sử dụng' : 'Clinical Materials Used',
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
                  _buildMaterialRow(context, isVi ? 'Thuốc tê' : 'Anesthetics', 'Lidocaine 2% + Epinephrine'),
                  Divider(color: context.divider, height: 1),
                  _buildMaterialRow(context, isVi ? 'Hệ thống trâm' : 'Rotary File System', 'WaveOne Gold (Dentsply Sirona)'),
                  Divider(color: context.divider, height: 1),
                  _buildMaterialRow(context, isVi ? 'Vật liệu trám tạm' : 'Temporary Sealer', 'Caviton (GC Corporation)'),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStepItem(BuildContext context, String time, String title, String desc, bool isCompleted) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Time
          SizedBox(
            width: 50,
            child: Text(
              time,
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w900,
                color: AppColors.primary,
              ),
            ),
          ),
          // Timeline indicator
          Column(
            children: [
              Container(
                width: 12,
                height: 12,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: isCompleted ? const Color(0xFF16A34A) : Colors.amber,
                ),
              ),
              Expanded(
                child: Container(
                  width: 1.5,
                  color: context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                ),
              ),
            ],
          ),
          const SizedBox(width: 16),
          // Details card
          Expanded(
            child: Container(
              margin: const EdgeInsets.only(bottom: 16),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: context.divider),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w800,
                      color: context.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    desc,
                    style: TextStyle(
                      fontSize: 11.5,
                      color: context.textSecondary,
                      height: 1.35,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMaterialRow(BuildContext context, String label, String name) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: context.textSecondary,
            ),
          ),
          Text(
            name,
            style: TextStyle(
              fontSize: 13.5,
              fontWeight: FontWeight.w800,
              color: context.textPrimary,
            ),
          ),
        ],
      ),
    );
  }
}

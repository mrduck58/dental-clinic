import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';

class AppointmentDetailsPage extends StatelessWidget {
  final MyAppointmentItem item;
  const AppointmentDetailsPage({super.key, required this.item});

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    final date = item.parsedDate;
    final isCancelled = item.status.toLowerCase() == 'cancelled';
    final isCompleted = item.status.toLowerCase() == 'completed';

    // Format Date & Time
    final monthsEn = ['', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    final monthsVi = ['', 'Th01', 'Th02', 'Th03', 'Th04', 'Th05', 'Th06', 'Th07', 'Th08', 'Th09', 'Th10', 'Th11', 'Th12'];
    final dateStr = isVi
        ? '${date.day.toString().padLeft(2, '0')} ${monthsVi[date.month]}, ${date.year}'
        : '${monthsEn[date.month]} ${date.day.toString().padLeft(2, '0')}, ${date.year}';

    final hour = date.hour > 12 ? date.hour - 12 : (date.hour == 0 ? 12 : date.hour);
    final amPm = date.hour >= 12 ? 'PM' : 'AM';
    final timeStr = '${hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')} $amPm';

    // Status colors & messages
    Color statusColor = const Color(0xFF16A34A);
    Color statusBg = const Color(0xFFDCFCE7);
    String statusLabel = isVi ? 'Đã xác nhận' : 'Confirmed';
    String statusDesc = isVi
        ? 'Lịch hẹn của bạn đã được lên lịch và xác nhận.'
        : 'Your visit is scheduled and confirmed.';

    if (isCancelled) {
      statusColor = const Color(0xFFEF4444);
      statusBg = const Color(0xFFFEE2E2);
      statusLabel = isVi ? 'Đã hủy' : 'Cancelled';
      statusDesc = isVi
          ? 'Lịch hẹn này đã bị hủy.'
          : 'This appointment has been cancelled.';
    } else if (isCompleted) {
      statusColor = const Color(0xFF0284C7);
      statusBg = const Color(0xFFE0F2FE);
      statusLabel = isVi ? 'Hoàn thành' : 'Completed';
      statusDesc = isVi
          ? 'Lịch hẹn đã được thực hiện thành công.'
          : 'This appointment has been completed successfully.';
    } else if (item.status.toLowerCase() == 'pending') {
      statusColor = const Color(0xFFD97706);
      statusBg = const Color(0xFFFEF3C7);
      statusLabel = isVi ? 'Chờ xác nhận' : 'Pending';
      statusDesc = isVi
          ? 'Lịch hẹn đang được xử lý và chờ xác nhận.'
          : 'Your appointment is pending confirmation.';
    }

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
          isVi ? 'Chi tiết lịch hẹn' : 'Appointment Details',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: CircleAvatar(
              radius: 16,
              backgroundColor: context.divider,
              backgroundImage: item.dentistAvatarUrl != null
                  ? NetworkImage(item.dentistAvatarUrl!)
                  : null,
              child: item.dentistAvatarUrl == null
                  ? Icon(Icons.person, color: context.textSecondary, size: 18)
                  : null,
            ),
          )
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
                children: [
                  // Status badge and message
                  Center(
                    child: Column(
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                          decoration: BoxDecoration(
                            color: statusBg,
                            borderRadius: BorderRadius.circular(999),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(
                                isCancelled ? Icons.cancel : Icons.check_circle,
                                color: statusColor,
                                size: 16,
                              ),
                              const SizedBox(width: 6),
                              Text(
                                statusLabel,
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w800,
                                  color: statusColor,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 12),
                        Text(
                          statusDesc,
                          style: TextStyle(
                            fontSize: 14,
                            color: context.textSecondary,
                            fontWeight: FontWeight.w500,
                          ),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Dentist info card
                  Container(
                    width: double.infinity,
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: context.divider),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                          blurRadius: 16,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      children: [
                        Padding(
                          padding: const EdgeInsets.all(20),
                          child: Row(
                            children: [
                              ClipRRect(
                                borderRadius: BorderRadius.circular(16),
                                child: item.dentistAvatarUrl != null
                                    ? Image.network(
                                        item.dentistAvatarUrl!,
                                        width: 64,
                                        height: 64,
                                        fit: BoxFit.cover,
                                        errorBuilder: (context, error, stackTrace) => _placeholderAvatar(context),
                                      )
                                    : _placeholderAvatar(context),
                              ),
                              const SizedBox(width: 16),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      item.dentistName,
                                      style: TextStyle(
                                        fontSize: 16,
                                        fontWeight: FontWeight.w800,
                                        color: context.textPrimary,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      item.specialization,
                                      style: TextStyle(
                                        fontSize: 13,
                                        color: context.textSecondary,
                                        fontWeight: FontWeight.w500,
                                      ),
                                    ),
                                    const SizedBox(height: 6),
                                    Row(
                                      children: [
                                        const Icon(Icons.star_rounded, color: Colors.amber, size: 16),
                                        const SizedBox(width: 4),
                                        Text(
                                          isVi ? '4.9 (120+ Đánh giá)' : '4.9 (120+ Reviews)',
                                          style: TextStyle(
                                            fontSize: 12,
                                            fontWeight: FontWeight.w700,
                                            color: AppColors.primary,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                        Divider(color: context.divider, height: 1),
                        Padding(
                          padding: const EdgeInsets.all(20),
                          child: Row(
                            children: [
                              // Date Column
                              Expanded(
                                child: Row(
                                  children: [
                                    Container(
                                      width: 42,
                                      height: 42,
                                      decoration: BoxDecoration(
                                        color: AppColors.primaryLight,
                                        shape: BoxShape.circle,
                                      ),
                                      child: const Icon(Iconsax.calendar_1, color: AppColors.primary, size: 20),
                                    ),
                                    const SizedBox(width: 12),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            isVi ? 'NGÀY KHÁM' : 'DATE',
                                            style: TextStyle(
                                              fontSize: 11,
                                              fontWeight: FontWeight.w800,
                                              color: context.textMuted,
                                              letterSpacing: 0.5,
                                            ),
                                          ),
                                          const SizedBox(height: 2),
                                          Text(
                                            dateStr,
                                            style: TextStyle(
                                              fontSize: 13,
                                              fontWeight: FontWeight.w800,
                                              color: context.textPrimary,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              // Time Column
                              Expanded(
                                child: Row(
                                  children: [
                                    Container(
                                      width: 42,
                                      height: 42,
                                      decoration: BoxDecoration(
                                        color: AppColors.primaryLight,
                                        shape: BoxShape.circle,
                                      ),
                                      child: const Icon(Iconsax.clock, color: AppColors.primary, size: 20),
                                    ),
                                    const SizedBox(width: 12),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            isVi ? 'GIỜ KHÁM' : 'TIME',
                                            style: TextStyle(
                                              fontSize: 11,
                                              fontWeight: FontWeight.w800,
                                              color: context.textMuted,
                                              letterSpacing: 0.5,
                                            ),
                                          ),
                                          const SizedBox(height: 2),
                                          Text(
                                            timeStr,
                                            style: TextStyle(
                                              fontSize: 13,
                                              fontWeight: FontWeight.w800,
                                              color: context.textPrimary,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 28),

                  // Clinic location section
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        isVi ? 'Địa điểm phòng khám' : 'Clinic Location',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                          color: context.textPrimary,
                        ),
                      ),
                      GestureDetector(
                        onTap: () {},
                        child: Row(
                          children: [
                            Text(
                              isVi ? 'Chỉ đường' : 'Get Directions',
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w700,
                                color: AppColors.primary,
                              ),
                            ),
                            const SizedBox(width: 3),
                            const Icon(Iconsax.routing, color: AppColors.primary, size: 14),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  Container(
                    width: double.infinity,
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: context.divider),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                          blurRadius: 16,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Stylized vector map placeholder
                        Container(
                          height: 140,
                          width: double.infinity,
                          decoration: BoxDecoration(
                            color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                            borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
                          ),
                          child: Stack(
                            alignment: Alignment.center,
                            children: [
                              // Abstract lines representing roads
                              Positioned.fill(
                                child: CustomPaint(
                                  painter: _MapGridPainter(
                                    lineColor: context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                                  ),
                                ),
                              ),
                              // Location Pin
                              Container(
                                padding: const EdgeInsets.all(10),
                                decoration: BoxDecoration(
                                  color: AppColors.primary.withValues(alpha: 0.12),
                                  shape: BoxShape.circle,
                                ),
                                child: Container(
                                  padding: const EdgeInsets.all(8),
                                  decoration: const BoxDecoration(
                                    color: AppColors.primary,
                                    shape: BoxShape.circle,
                                  ),
                                  child: const Icon(Icons.location_on, color: Colors.white, size: 24),
                                ),
                              ),
                            ],
                          ),
                        ),
                        // Clinic Details
                        Padding(
                          padding: const EdgeInsets.all(20),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                isVi ? 'Phòng khám Nha khoa DentalCare' : 'DentalCare Premium Clinic',
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w800,
                                  color: context.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                isVi
                                    ? '452 Đường Medical Center, Tòa nhà Suite 300, Tp. Hồ Chí Minh'
                                    : '452 Medical Center Dr, Suite 300, San Francisco, CA 94107',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: context.textSecondary,
                                  height: 1.4,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 28),

                  // Pre-Visit Reminder Box
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: context.divider),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(Icons.info_outline_rounded, color: AppColors.primary, size: 22),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                isVi ? 'Lưu ý trước khi khám' : 'Pre-Visit Reminder',
                                style: TextStyle(
                                  fontSize: 14,
                                  fontWeight: FontWeight.w800,
                                  color: context.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                isVi
                                    ? 'Vui lòng đến sớm 10 phút để hoàn tất các thủ tục hồ sơ bệnh án điện tử cần thiết.'
                                    : 'Please arrive 10 minutes early to complete any pending digital paperwork.',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: context.textSecondary,
                                  height: 1.45,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Action Buttons at the bottom (disabled if completed/cancelled)
          if (!isCompleted && !isCancelled)
            Container(
              padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
              decoration: BoxDecoration(
                color: context.card,
                border: Border(top: BorderSide(color: context.divider)),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: SizedBox(
                      height: 50,
                      child: OutlinedButton.icon(
                        onPressed: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(isVi ? 'Đang chuyển đổi lịch hẹn...' : 'Rescheduling appointment...'),
                              behavior: SnackBarBehavior.floating,
                            ),
                          );
                        },
                        style: OutlinedButton.styleFrom(
                          foregroundColor: context.textPrimary,
                          side: BorderSide(color: context.divider),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                          backgroundColor: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                        ),
                        icon: const Icon(Iconsax.calendar_edit, size: 18),
                        label: Text(
                          isVi ? 'Đổi lịch khám' : 'Reschedule',
                          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: SizedBox(
                      height: 50,
                      child: ElevatedButton.icon(
                        onPressed: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(isVi ? 'Đã yêu cầu hủy lịch hẹn.' : 'Cancellation requested.'),
                              behavior: SnackBarBehavior.floating,
                            ),
                          );
                        },
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFFC2185B),
                          foregroundColor: Colors.white,
                          elevation: 0,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        icon: const Icon(Icons.cancel_outlined, size: 18),
                        label: Text(
                          isVi ? 'Hủy lịch khám' : 'Cancel visit',
                          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14),
                        ),
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

  Widget _placeholderAvatar(BuildContext context) {
    return Container(
      width: 64,
      height: 64,
      color: context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight,
      child: Icon(Iconsax.user, color: context.isDark ? Colors.white : AppColors.primary, size: 26),
    );
  }
}

class _MapGridPainter extends CustomPainter {
  final Color lineColor;
  _MapGridPainter({required this.lineColor});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = lineColor
      ..strokeWidth = 3
      ..style = PaintingStyle.stroke;

    // Draw some random roads intersecting
    canvas.drawLine(Offset(0, size.height * 0.3), Offset(size.width, size.height * 0.4), paint);
    canvas.drawLine(Offset(size.width * 0.25, 0), Offset(size.width * 0.3, size.height), paint);
    canvas.drawLine(Offset(size.width * 0.7, 0), Offset(size.width * 0.6, size.height), paint);
    canvas.drawLine(Offset(0, size.height * 0.7), Offset(size.width, size.height * 0.65), paint);
    
    // Draw minor streets
    paint.strokeWidth = 1.5;
    canvas.drawLine(Offset(0, size.height * 0.15), Offset(size.width * 0.3, size.height * 0.2), paint);
    canvas.drawLine(Offset(size.width * 0.3, size.height * 0.2), Offset(size.width * 0.6, size.height * 0.1), paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:dio/dio.dart';

class AppointmentDetailsPage extends StatefulWidget {
  final MyAppointmentItem item;
  const AppointmentDetailsPage({super.key, required this.item});

  @override
  State<AppointmentDetailsPage> createState() => _AppointmentDetailsPageState();
}

class _AppointmentDetailsPageState extends State<AppointmentDetailsPage> {
  late String _status;

  @override
  void initState() {
    super.initState();
    _status = widget.item.status;
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    final date = item.parsedDate;
    final isCancelled = _status.toLowerCase() == 'cancelled';
    final isCompleted = _status.toLowerCase() == 'completed';

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

    final normStatus = _status.toLowerCase();
    if (isCancelled) {
      statusColor = const Color(0xFFEF4444);
      statusBg = const Color(0xFFFEE2E2);
      statusLabel = isVi ? 'Đã hủy' : 'Cancelled';
      statusDesc = isVi
          ? 'Lịch hẹn này đã bị hủy.'
          : 'This appointment has been cancelled.';
    } else if (isCompleted) {
      statusColor = const Color(0xFF16A34A);
      statusBg = const Color(0xFFDCFCE7);
      statusLabel = isVi ? 'Hoàn thành' : 'Completed';
      statusDesc = isVi
          ? 'Lịch hẹn đã được thực hiện thành công.'
          : 'This appointment has been completed successfully.';
    } else if (normStatus == 'pending') {
      statusColor = const Color(0xFFD97706);
      statusBg = const Color(0xFFFEF3C7);
      statusLabel = isVi ? 'Chờ xác nhận' : 'Pending';
      statusDesc = isVi
          ? 'Lịch hẹn đang được xử lý và chờ xác nhận.'
          : 'Your appointment is pending confirmation.';
    } else if (normStatus == 'checkedin') {
      statusColor = const Color(0xFF4F46E5);
      statusBg = const Color(0xFFEEF2FF);
      statusLabel = isVi ? 'Đã check-in' : 'Checked In';
      statusDesc = isVi
          ? 'Bạn đã check-in thành công. Vui lòng chờ bác sĩ gọi vào phòng khám.'
          : 'You have checked in successfully. Please wait for the doctor.';
    } else if (normStatus == 'inprogress') {
      statusColor = const Color(0xFF0284C7);
      statusBg = const Color(0xFFE0F2FE);
      statusLabel = isVi ? 'Đang khám' : 'In Progress';
      statusDesc = isVi
          ? 'Lịch hẹn của bạn đang được tiến hành khám và điều trị.'
          : 'Your visit is currently in progress.';
    } else if (normStatus == 'pendingpayment') {
      statusColor = const Color(0xFFEA580C);
      statusBg = const Color(0xFFFFEDD5);
      statusLabel = isVi ? 'Chờ thanh toán' : 'Pending Payment';
      statusDesc = isVi
          ? 'Bạn đã khám xong. Vui lòng thanh toán phí tại quầy.'
          : 'Treatment is complete. Please proceed to the counter for payment.';
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
                  ? NetworkImage(ApiConstants.resolveAssetUrl(item.dentistAvatarUrl)!)
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
                                        ApiConstants.resolveAssetUrl(item.dentistAvatarUrl)!,
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
                  if (item.patientName != null && item.patientName!.isNotEmpty) ...[
                    const SizedBox(height: 16),
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
                            child: const Icon(Iconsax.user, color: AppColors.primary, size: 20),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  isVi ? 'BỆNH NHÂN' : 'PATIENT',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w800,
                                    color: context.textMuted,
                                    letterSpacing: 0.5,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  item.patientRelationship == null || item.patientRelationship!.isEmpty || item.patientRelationship == 'Tôi' || item.patientRelationship == 'Self'
                                      ? '${item.patientName} (${isVi ? 'Tôi' : 'Self'})'
                                      : '${item.patientName} (${item.patientRelationship})',
                                  style: TextStyle(
                                    fontSize: 14,
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

          // Action Buttons at the bottom (only for Pending or Confirmed status)
          if (normStatus == 'pending' || normStatus == 'confirmed')
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
                          showModalBottomSheet(
                            context: context,
                            isScrollControlled: true,
                            backgroundColor: Colors.transparent,
                            builder: (context) => _CancelReasonBottomSheet(
                              appointmentId: item.appointmentId,
                              isVi: isVi,
                              onCancelled: (reason) {
                                setState(() {
                                  _status = 'Cancelled';
                                });
                              },
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

          // Action Buttons at bottom for Completed appointments
          if (isCompleted)
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
                          context.push(AppRoutes.clinicFeedback);
                        },
                        style: OutlinedButton.styleFrom(
                          foregroundColor: AppColors.primary,
                          side: const BorderSide(color: AppColors.primary),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        icon: const Icon(Iconsax.hospital, size: 18),
                        label: Text(
                          isVi ? 'Đánh giá phòng khám' : 'Review Clinic',
                          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
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
                          final doctor = DoctorModel(
                            id: item.dentistId,
                            fullName: item.dentistName,
                            specialty: item.specialization,
                            profilePictureUrl: item.dentistAvatarUrl,
                          );
                          context.push(
                            AppRoutes.writeReview,
                            extra: {
                              'doctor': doctor,
                              'appointmentId': item.appointmentId,
                            },
                          );
                        },
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          foregroundColor: Colors.white,
                          elevation: 0,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        icon: const Icon(Iconsax.star_1, size: 18),
                        label: Text(
                          isVi ? 'Đánh giá nha sĩ' : 'Review Dentist',
                          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
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

class _CancelReasonBottomSheet extends StatefulWidget {
  final String appointmentId;
  final bool isVi;
  final ValueChanged<String> onCancelled;

  const _CancelReasonBottomSheet({
    required this.appointmentId,
    required this.isVi,
    required this.onCancelled,
  });

  @override
  State<_CancelReasonBottomSheet> createState() => _CancelReasonBottomSheetState();
}

class _CancelReasonBottomSheetState extends State<_CancelReasonBottomSheet> {
  final _bookingService = BookingService();
  String _selectedReason = 'Change of plans';
  final _textController = TextEditingController();
  bool _submitting = false;

  @override
  void dispose() {
    _textController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _submitting = true);
    try {
      String finalReason = widget.isVi 
          ? _translateToVi(_selectedReason) 
          : _selectedReason;
      
      if (_selectedReason == 'Other' && _textController.text.trim().isNotEmpty) {
        finalReason = _textController.text.trim();
      } else if (_textController.text.trim().isNotEmpty) {
        finalReason = '$finalReason: ${_textController.text.trim()}';
      }

      await _bookingService.cancelAppointment(widget.appointmentId, finalReason);
      
      if (mounted) {
        Navigator.pop(context); // close bottom sheet
        widget.onCancelled(finalReason);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(widget.isVi ? 'Đã hủy lịch khám thành công.' : 'Appointment cancelled successfully.'),
            behavior: SnackBarBehavior.floating,
            backgroundColor: const Color(0xFF16A34A),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        String msg = widget.isVi ? 'Hủy lịch khám thất bại. Vui lòng thử lại.' : 'Failed to cancel appointment. Please try again.';
        if (e is DioException) {
          msg = ApiClient.errorMessage(e);
        }
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(msg),
            behavior: SnackBarBehavior.floating,
            backgroundColor: const Color(0xFFEF4444),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  String _translateToVi(String englishReason) {
    switch (englishReason) {
      case 'Change of plans':
        return 'Thay đổi kế hoạch';
      case 'Found another clinic':
        return 'Tìm thấy phòng khám khác';
      case 'Health issues':
        return 'Vấn đề sức khỏe';
      case 'Other':
        return 'Lý do khác';
      default:
        return englishReason;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(28)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.1),
            blurRadius: 20,
            spreadRadius: 1,
          )
        ],
      ),
      padding: EdgeInsets.only(
        left: 24,
        right: 24,
        top: 14,
        bottom: 24 + MediaQuery.of(context).viewInsets.bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Center(
            child: Container(
              width: 48,
              height: 5,
              decoration: BoxDecoration(
                color: context.divider,
                borderRadius: BorderRadius.circular(999),
              ),
            ),
          ),
          const SizedBox(height: 24),
          Text(
            widget.isVi ? 'Lý do hủy lịch' : 'Reason for Cancellation',
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w800,
              color: context.textPrimary,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            widget.isVi
                ? 'Chúng tôi rất tiếc khi bạn muốn hủy lịch. Vui lòng cho biết lý do hủy.'
                : 'We are sorry to see you go. Please let us know why you are canceling.',
            style: TextStyle(
              fontSize: 14,
              color: context.textSecondary,
              height: 1.4,
            ),
          ),
          const SizedBox(height: 20),
          _buildRadioOption('Change of plans', widget.isVi ? 'Thay đổi kế hoạch' : 'Change of plans'),
          _buildRadioOption('Found another clinic', widget.isVi ? 'Tìm thấy phòng khám khác' : 'Found another clinic'),
          _buildRadioOption('Health issues', widget.isVi ? 'Vấn đề sức khỏe' : 'Health issues'),
          _buildRadioOption('Other', widget.isVi ? 'Lý do khác' : 'Other'),
          const SizedBox(height: 20),
          TextField(
            controller: _textController,
            maxLines: 4,
            style: TextStyle(color: context.textPrimary, fontSize: 14),
            decoration: InputDecoration(
              hintText: widget.isVi ? 'Mô tả thêm (Không bắt buộc)' : 'Tell us more (Optional)',
              hintStyle: TextStyle(color: context.textMuted, fontSize: 14),
              filled: true,
              fillColor: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: BorderSide(color: context.divider),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: BorderSide(color: context.divider),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
              ),
              contentPadding: const EdgeInsets.all(16),
            ),
          ),
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            height: 52,
            child: ElevatedButton(
              onPressed: _submitting ? null : _submit,
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFFC2185B),
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                elevation: 0,
              ),
              child: _submitting
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                    )
                  : Text(
                      widget.isVi ? 'Xác nhận hủy' : 'Confirm Cancellation',
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                    ),
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            height: 52,
            child: TextButton(
              onPressed: _submitting ? null : () => Navigator.pop(context),
              style: TextButton.styleFrom(
                backgroundColor: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                foregroundColor: context.textPrimary,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              ),
              child: Text(
                widget.isVi ? 'Giữ lịch hẹn' : 'Keep Appointment',
                style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRadioOption(String value, String label) {
    final isSelected = _selectedReason == value;
    return InkWell(
      onTap: _submitting ? null : () => setState(() => _selectedReason = value),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 10.0),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              label,
              style: TextStyle(
                fontSize: 15,
                fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                color: isSelected ? context.textPrimary : context.textSecondary,
              ),
            ),
            Container(
              width: 22,
              height: 22,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(
                  color: isSelected ? const Color(0xFFC2185B) : context.textMuted,
                  width: 2,
                ),
              ),
              padding: const EdgeInsets.all(3),
              child: isSelected
                  ? Container(
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        color: Color(0xFFC2185B),
                      ),
                    )
                  : null,
            ),
          ],
        ),
      ),
    );
  }
}

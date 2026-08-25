import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/core/utils/app_toast.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:dio/dio.dart';

class AppointmentDetailsPage extends StatefulWidget {
  final MyAppointmentItem? item;
  final String? appointmentId;
  const AppointmentDetailsPage({super.key, this.item, this.appointmentId});

  @override
  State<AppointmentDetailsPage> createState() => _AppointmentDetailsPageState();
}

class _AppointmentDetailsPageState extends State<AppointmentDetailsPage> {
  MyAppointmentItem? _item;
  String _status = '';
  bool _isLoading = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    if (widget.item != null) {
      _item = widget.item;
      _status = widget.item!.status;
    } else if (widget.appointmentId != null) {
      _loadItem(widget.appointmentId!);
    }
  }

  Future<void> _loadItem(String id) async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });
    try {
      final list = await BookingService().getMyAppointments();
      final match = list.firstWhere(
        (a) => a.appointmentId == id,
        orElse: () => list.isNotEmpty ? list.first : throw Exception('Không tìm thấy thông tin lịch hẹn.'),
      );
      if (mounted) {
        setState(() {
          _item = match;
          _status = match.status;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = e.toString();
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    if (_isLoading) {
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
            style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.bold, fontSize: 18),
          ),
        ),
        body: const Center(child: CircularProgressIndicator(color: AppColors.primary)),
      );
    }

    if (_errorMessage != null || _item == null) {
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
            style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.bold, fontSize: 18),
          ),
        ),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                _errorMessage ?? (isVi ? 'Không tìm thấy lịch hẹn' : 'Appointment not found'),
                style: TextStyle(color: context.textSecondary, fontSize: 14),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () => context.go(AppRoutes.appointments),
                child: Text(isVi ? 'Về danh sách lịch hẹn' : 'Back to appointments'),
              ),
            ],
          ),
        ),
      );
    }

    final item = _item!;
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
    } else if (normStatus == 'rebooking') {
      statusColor = const Color(0xFFD97706);
      statusBg = const Color(0xFFFEF3C7);
      statusLabel = isVi ? 'Tái đặt lịch (Chờ xác nhận)' : 'Rebooking';
      statusDesc = isVi
          ? 'Lịch hẹn đã được đổi ngày/giờ và đang chờ phòng khám xác nhận lại.'
          : 'Your rebooked visit is pending clinic confirmation.';
    } else if (normStatus == 'noshow') {
      statusColor = const Color(0xFFEF4444);
      statusBg = const Color(0xFFFEE2E2);
      statusLabel = isVi ? 'Vắng mặt' : 'No Show';
      statusDesc = isVi
          ? 'Lịch hẹn đã qua giờ và ghi nhận vắng mặt. Bạn có thể bấm "Đổi lịch khám" bên dưới để đặt lại ca mới.'
          : 'You missed this appointment. Tap "Reschedule" below to book a new slot.';
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

                  /*
                  // Clinic location section (Tạm thời ẩn do chưa tích hợp bản đồ)
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
                  */

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

          // Action Buttons at the bottom (for Pending, Confirmed, Rebooking, or NoShow status)
          if (normStatus == 'pending' || normStatus == 'confirmed' || normStatus == 'rebooking' || normStatus == 'noshow')
            Container(
              padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
              decoration: BoxDecoration(
                color: context.card,
                border: Border(top: BorderSide(color: context.divider)),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (item.isPastAppointmentDate)
                    Container(
                      width: double.infinity,
                      margin: const EdgeInsets.only(bottom: 12),
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFEF3C7),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: const Color(0xFFFDE68A)),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.info_outline_rounded, size: 18, color: Color(0xFFD97706)),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              isVi
                                  ? 'Lịch khám này đã qua ngày/giờ hẹn. Bạn có thể bấm "Đổi lịch khám" bên dưới để chọn ca khám mới.'
                                  : 'This appointment time has passed. Tap "Reschedule" below to pick a new slot.',
                              style: const TextStyle(fontSize: 12, color: Color(0xFF92400E), height: 1.3, fontWeight: FontWeight.w600),
                            ),
                          ),
                        ],
                      ),
                    ),
                  Row(
                    children: [
                      Expanded(
                          child: SizedBox(
                            height: 50,
                            child: OutlinedButton.icon(
                              onPressed: () async {
                                try {
                                  final eligibility = await BookingService().getBookingEligibility(patientId: item.patientId);
                                  if (eligibility.isInRescheduleCooldown) {
                                    if (!context.mounted) return;
                                    showDialog(
                                      context: context,
                                      builder: (ctx) => AlertDialog(
                                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                                        title: Row(
                                          children: [
                                            const Icon(Icons.timer_outlined, color: AppColors.primary),
                                            const SizedBox(width: 8),
                                            Text(isVi ? 'Thời gian chờ' : 'Cooldown Active', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                                          ],
                                        ),
                                        content: Text(
                                          isVi
                                              ? 'Bạn đã dời lịch từ 2 lần trở lên trong ngày hôm nay.\n\nVui lòng thử lại sau ${eligibility.rescheduleCooldownRemainingMinutes} phút nữa (thời gian chờ 30 phút giữa các lần dời).'
                                              : 'You have rescheduled 2 or more times today.\n\nPlease try again in ${eligibility.rescheduleCooldownRemainingMinutes} minutes (30-minute waiting period between reschedules).',
                                          style: const TextStyle(fontSize: 14, height: 1.4),
                                        ),
                                        actions: [
                                          TextButton(
                                            onPressed: () => Navigator.pop(ctx),
                                            child: Text(isVi ? 'Đã hiểu' : 'OK'),
                                          ),
                                        ],
                                      ),
                                    );
                                    return;
                                  }
                                } catch (_) {}

                                final parsedDate = item.parsedDate;
                                final date = DateTime(parsedDate.year, parsedDate.month, parsedDate.day);
                                final startHour = parsedDate.hour.toString().padLeft(2, '0');
                                final startMin = parsedDate.minute.toString().padLeft(2, '0');
                                final slotEndMinTotal = parsedDate.hour * 60 + parsedDate.minute + 30;
                                final endHour = (slotEndMinTotal ~/ 60 % 24).toString().padLeft(2, '0');
                                final endMin = (slotEndMinTotal % 60).toString().padLeft(2, '0');
                                final currentSlotRange = '$startHour:$startMin - $endHour:$endMin';

                                final doctor = DoctorInfo(
                                  id: item.dentistId,
                                  name: item.dentistName,
                                  title: '',
                                  specialty: item.specialization,
                                  room: '',
                                  session: parsedDate.hour < 12 ? DoctorSession.morning : DoctorSession.afternoon,
                                  rating: 5.0,
                                  reviewCount: 0,
                                  avatarUrl: item.dentistAvatarUrl,
                                );

                                final initialDate = item.isPastAppointmentDate ? null : date;
                                final draft = BookingDraft(
                                  reschedulingAppointmentId: item.appointmentId,
                                  appointmentCode: item.appointmentCode,
                                  preferredDentistId: item.dentistId,
                                  patient: PatientInfo(
                                    id: item.patientId ?? 'self',
                                    name: item.patientName ?? '',
                                    relationship: item.patientRelationship ?? '',
                                  ),
                                  service: item.serviceName == null
                                      ? null
                                      : ServiceInfo(
                                          id: item.serviceId ?? '',
                                          name: item.serviceName!,
                                          description: '',
                                          price: '',
                                          durationMinutes: item.serviceDurationMinutes ?? 30,
                                        ),
                                  date: initialDate,
                                  timeSlot: initialDate != null ? TimeSlot(range: currentSlotRange) : null,
                                  doctor: doctor,
                                  symptoms: item.symptoms,
                                );

                                if (context.mounted) {
                                  if (item.isPastAppointmentDate) {
                                    context.push(AppRoutes.bookingSelectDatetime, extra: draft);
                                  } else {
                                    context.push(AppRoutes.bookingSelectTimeSlot, extra: draft);
                                  }
                                }
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
                                if (item.isPastAppointmentDate) {
                                  showDialog(
                                    context: context,
                                    builder: (ctx) => AlertDialog(
                                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                                      title: Row(
                                        children: [
                                          const Icon(Icons.info_outline_rounded, color: AppColors.primary),
                                          const SizedBox(width: 8),
                                          Text(isVi ? 'Hỗ trợ hủy lịch' : 'Support Required', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                                        ],
                                      ),
                                      content: Text(
                                        isVi
                                            ? 'Lịch khám đã đến hoặc đã qua giờ hẹn. Bạn không thể tự hủy lịch trên ứng dụng.\n\nVui lòng liên hệ hotline phòng khám để được nhân viên hỗ trợ trực tiếp.'
                                            : 'Appointment time has arrived or passed. You cannot cancel in the app.\n\nPlease contact the clinic hotline for assistance.',
                                        style: const TextStyle(fontSize: 14, height: 1.4),
                                      ),
                                      actions: [
                                        TextButton(
                                          onPressed: () => Navigator.pop(ctx),
                                          child: Text(isVi ? 'Đóng' : 'Close'),
                                        ),
                                      ],
                                    ),
                                  );
                                  return;
                                }

                                showModalBottomSheet(
                                  context: context,
                                  isScrollControlled: true,
                                  backgroundColor: Colors.transparent,
                                  builder: (context) => _CancelReasonBottomSheet(
                                    appointmentId: item.appointmentId,
                                    patientId: item.patientId,
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



class _CancelReasonBottomSheet extends StatefulWidget {
  final String appointmentId;
  final String? patientId;
  final bool isVi;
  final ValueChanged<String> onCancelled;

  const _CancelReasonBottomSheet({
    required this.appointmentId,
    this.patientId,
    required this.isVi,
    required this.onCancelled,
  });

  @override
  State<_CancelReasonBottomSheet> createState() => _CancelReasonBottomSheetState();
}

class _CancelReasonBottomSheetState extends State<_CancelReasonBottomSheet> {
  final _bookingService = BookingService();
  List<CancellationReasonOption> _reasons = [];
  String? _selectedCode;
  bool _loadingReasons = true;
  String? _reasonsError;
  BookingEligibility? _eligibility;
  final _textController = TextEditingController();
  bool _submitting = false;
  String? _submitError;

  CancellationReasonOption? get _selectedReason =>
      _reasons.where((r) => r.code == _selectedCode).firstOrNull;

  bool get _canSubmit =>
      _selectedCode != null &&
      (!(_selectedReason?.requiresNote ?? false) || _textController.text.trim().isNotEmpty);

  @override
  void initState() {
    super.initState();
    _loadReasons();
  }

  Future<void> _loadReasons() async {
    try {
      final results = await Future.wait([
        _bookingService.getCancellationReasons(),
        _bookingService.getBookingEligibility(patientId: widget.patientId).catchError((_) => const BookingEligibility(
          activeBookingCount: 0,
          maxActiveBookings: 2,
          canBookNew: true,
          isInCooldown: false,
          cooldownRemainingSeconds: 0,
          cancellationCount: 0,
          rescheduleCount: 0,
        )),
      ]);
      if (!mounted) return;
      final reasons = results[0] as List<CancellationReasonOption>;
      final eligibility = results[1] as BookingEligibility;
      setState(() {
        _reasons = reasons;
        _eligibility = eligibility;
        _selectedCode = reasons.isNotEmpty ? reasons.first.code : null;
        _loadingReasons = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loadingReasons = false;
        _reasonsError = e is DioException
            ? ApiClient.errorMessage(e)
            : (widget.isVi
                ? 'Không tải được danh sách lý do hủy.'
                : 'Could not load cancellation reasons.');
      });
    }
  }

  @override
  void dispose() {
    _textController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final reason = _selectedReason;
    if (reason == null) return;

    setState(() {
      _submitting = true;
      _submitError = null;
    });
    try {
      final note = _textController.text.trim();
      await _bookingService.cancelAppointment(
        widget.appointmentId,
        reason.code,
        note: note,
      );

      if (mounted) {
        // Nhãn để hiển thị lại trên màn hình gọi; mã nhóm mới là thứ backend lưu.
        final displayed = note.isNotEmpty
            ? '${reason.label(widget.isVi)}: $note'
            : reason.label(widget.isVi);

        Navigator.pop(context); // close bottom sheet
        widget.onCancelled(displayed);
        final willHaveCooldown = _eligibility != null && _eligibility!.cancellationCount >= 1;
        final successMsg = widget.isVi
            ? (willHaveCooldown
                ? 'Đã hủy lịch khám. Bệnh nhân này sẽ tạm chờ 30 phút trước khi đặt lịch mới.'
                : 'Đã hủy lịch khám thành công.')
            : (willHaveCooldown
                ? 'Appointment cancelled. 30-minute cooldown active for this patient.'
                : 'Appointment cancelled successfully.');

        AppToast.showSuccess(context, successMsg);
      }
    } catch (e) {
      if (mounted) {
        String msg = widget.isVi ? 'Hủy lịch khám thất bại. Vui lòng thử lại.' : 'Failed to cancel appointment. Please try again.';
        if (e is DioException) {
          msg = ApiClient.errorMessage(e);
        }
        setState(() {
          _submitError = msg;
        });
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
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
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxHeight: MediaQuery.of(context).size.height * 0.85,
        ),
        child: SingleChildScrollView(
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
          if (_eligibility != null && _eligibility!.cancellationCount >= 1) ...[
            const SizedBox(height: 14),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFFFFBEB),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFF59E0B)),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.warning_amber_rounded, color: Color(0xFFD97706), size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      widget.isVi
                          ? 'Lưu ý: Đây là lần hủy lịch thứ ${_eligibility!.cancellationCount + 1} trong ngày hôm nay. Sau khi xác nhận hủy, bệnh nhân này sẽ phải chờ 30 phút mới có thể đặt lịch hẹn mới.'
                          : 'Warning: This is cancellation #${_eligibility!.cancellationCount + 1} today. After cancelling, this patient will have a 30-minute cooldown before booking a new appointment.',
                      style: const TextStyle(
                        color: Color(0xFF92400E),
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        height: 1.4,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
          const SizedBox(height: 20),
          if (_loadingReasons)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
            )
          else if (_reasonsError != null)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _reasonsError!,
                    style: const TextStyle(color: Color(0xFFEF4444), fontSize: 14, fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    onPressed: () {
                      setState(() {
                        _loadingReasons = true;
                        _reasonsError = null;
                      });
                      _loadReasons();
                    },
                    child: Text(widget.isVi ? 'Thử lại' : 'Retry'),
                  ),
                ],
              ),
            )
          else
            ..._reasons.map((r) => _buildRadioOption(r.code, r.label(widget.isVi))),
          const SizedBox(height: 20),
          TextField(
            controller: _textController,
            maxLines: 4,
            // Nút gửi bật/tắt theo nội dung ô này khi lý do đang chọn bắt buộc ghi chú.
            onChanged: (_) => setState(() {}),
            style: TextStyle(color: context.textPrimary, fontSize: 14),
            decoration: InputDecoration(
              hintText: (_selectedReason?.requiresNote ?? false)
                  ? (widget.isVi ? 'Vui lòng nêu rõ lý do (Bắt buộc)' : 'Please specify (Required)')
                  : (widget.isVi ? 'Mô tả thêm (Không bắt buộc)' : 'Tell us more (Optional)'),
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
          if (_submitError != null) ...[
            const SizedBox(height: 16),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              decoration: BoxDecoration(
                color: context.isDark ? const Color(0xFF451A1A) : const Color(0xFFFEF2F2),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: context.isDark ? const Color(0xFF7F1D1D) : const Color(0xFFFCA5A5),
                ),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.error_outline_rounded, color: Color(0xFFDC2626), size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      _submitError!,
                      style: TextStyle(
                        color: context.isDark ? const Color(0xFFFCA5A5) : const Color(0xFF991B1B),
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        height: 1.4,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            height: 52,
            child: ElevatedButton(
              onPressed: (_submitting || !_canSubmit) ? null : _submit,
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
    ),
  ),
);
  }

  Widget _buildRadioOption(String value, String label) {
    final isSelected = _selectedCode == value;
    return InkWell(
      onTap: _submitting ? null : () => setState(() => _selectedCode = value),
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



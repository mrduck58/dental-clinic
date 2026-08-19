import 'package:flutter/foundation.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';

class BookingService {
  static final BookingService _instance = BookingService._internal();
  factory BookingService() => _instance;
  BookingService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  /// Lấy danh sách dịch vụ đang hoạt động cho luồng đặt khám.
  Future<List<ServiceModel>> getActiveServices() async {
    final res = await _client.get(
      ApiConstants.services,
      queryParameters: {'status': 'Active'},
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => ServiceModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Lấy thông tin chi tiết của một dịch vụ kèm danh sách options.
  Future<ServiceModel> getServiceById(String serviceId) async {
    final res = await _client.get('${ApiConstants.services}/$serviceId');
    return ServiceModel.fromJson(res.data as Map<String, dynamic>);
  }

  /// Lấy danh sách nha sĩ kèm slot khả dụng cho một ngày cụ thể.
  Future<List<ApiDoctorWithSlots>> getDoctorsWithSlots(DateTime date) async {
    final dateStr =
        '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    final token = await _auth.getToken();
    final res = await _client.get(
      ApiConstants.dentistSlots,
      queryParameters: {'date': dateStr},
      token: token,
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => ApiDoctorWithSlots.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Lấy danh sách các ngày làm việc (có slot) của bác sĩ trong tháng.
  Future<Set<String>> getWorkingDatesForDentist(String dentistId, int year, int month) async {
    try {
      final res = await _client.get(
        ApiConstants.dentistWorkingDates(dentistId),
        queryParameters: {'year': year, 'month': month},
      );
      if (res.data is List) {
        return (res.data as List<dynamic>).map((e) => e.toString()).toSet();
      }
    } catch (e) {
      debugPrint('BookingService: getWorkingDatesForDentist error: $e');
    }
    return {};
  }

  /// Lấy thông tin điều kiện đặt lịch / hủy lịch / đổi lịch và trạng thái cooldown
  Future<BookingEligibility> getBookingEligibility({String? patientId}) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    try {
      final res = await _client.get(
        ApiConstants.bookingEligibility,
        queryParameters: patientId != null ? {'patientId': patientId} : null,
        token: token,
      );
      return BookingEligibility.fromJson(res.data as Map<String, dynamic>);
    } catch (e) {
      debugPrint('BookingService: getBookingEligibility error: $e');
      return const BookingEligibility(
        activeBookingCount: 0,
        maxActiveBookings: 2,
        canBookNew: true,
        isInCooldown: false,
        cooldownRemainingSeconds: 0,
        cancellationCount: 0,
        rescheduleCount: 0,
      );
    }
  }

  /// Giữ tạm thời một ca khám trong tối đa 5 phút
  Future<SlotHoldResult> holdSlot({
    required String patientId,
    required String dentistId,
    required DateTime date,
    required String timeSlot,
    String? serviceId,
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final dateStr =
        '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

    final body = <String, dynamic>{
      'dentistId': dentistId,
      'date': dateStr,
      'timeSlot': timeSlot,
    };
    if (patientId.isNotEmpty && patientId != 'self') {
      body['patientId'] = patientId;
    }
    if (serviceId != null && serviceId.isNotEmpty) {
      body['serviceId'] = serviceId;
    }

    final res = await _client.post(
      ApiConstants.holdSlot,
      body,
      token: token,
    );

    return SlotHoldResult.fromJson(res.data as Map<String, dynamic>);
  }

  /// Giải phóng ca khám đang giữ tạm
  Future<bool> releaseHold({
    required String patientId,
    required String dentistId,
    required DateTime date,
    required String timeSlot,
  }) async {
    final token = await _auth.getToken();
    if (token == null) return false;

    try {
      final dateStr =
          '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

      final body = <String, dynamic>{
        'dentistId': dentistId,
        'date': dateStr,
        'timeSlot': timeSlot,
      };
      if (patientId.isNotEmpty && patientId != 'self') {
        body['patientId'] = patientId;
      }

      final res = await _client.post(
        ApiConstants.releaseHold,
        body,
        token: token,
      );

      return (res.data as Map<String, dynamic>?)?['success'] as bool? ?? true;
    } catch (e) {
      debugPrint('BookingService: releaseHold error: $e');
      return false;
    }
  }

  /// Lấy ca khám đang giữ tạm hiện tại của bệnh nhân
  Future<SlotHoldResult?> getActiveHold({required String patientId}) async {
    final token = await _auth.getToken();
    if (token == null) return null;

    try {
      final query = <String, dynamic>{};
      if (patientId.isNotEmpty && patientId != 'self') {
        query['patientId'] = patientId;
      }

      final res = await _client.get(
        ApiConstants.activeHold,
        queryParameters: query.isNotEmpty ? query : null,
        token: token,
      );

      if (res.data == null) return null;
      return SlotHoldResult.fromJson(res.data as Map<String, dynamic>);
    } catch (e) {
      debugPrint('BookingService: getActiveHold error: $e');
      return null;
    }
  }

  /// Lấy danh sách lịch hẹn của bệnh nhân hiện tại.
  Future<List<MyAppointmentItem>> getMyAppointments() async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.get(
      ApiConstants.myAppointments,
      token: token,
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => MyAppointmentItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Ghép ngày đã chọn với giờ bắt đầu của khung giờ, ví dụ "07:30 - 08:30" → 07:30 của ngày đó.
  /// Dùng chung cho cả đặt lịch và dời lịch — hai bản sao chép tay của cùng phép ghép này sẽ lệch
  /// nhau ngay lần đầu ai đó đổi định dạng khung giờ.
  static DateTime combineDateAndSlot(DateTime date, String timeSlotRange) {
    final timePart = timeSlotRange.split(' - ').first.trim();
    final parts = timePart.split(':');
    return DateTime(date.year, date.month, date.day, int.parse(parts[0]), int.parse(parts[1]));
  }

  /// Đặt lịch khám — yêu cầu JWT của bệnh nhân.
  Future<ApiAppointmentResult> createAppointment({
    required String dentistId,
    required DateTime date,
    required String timeSlotRange,
    String? symptoms,
    String? serviceId,
    String? patientId,
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    // Format as ISO 8601 with UTC offset
    final isoDate = combineDateAndSlot(date, timeSlotRange).toUtc().toIso8601String();

    final effectiveSymptoms = (symptoms?.isEmpty ?? true) ? null : symptoms;
    final body = <String, dynamic>{
      'dentistId': dentistId,
      'appointmentDate': isoDate,
    };
    if (effectiveSymptoms != null) body['symptoms'] = effectiveSymptoms;
    if (serviceId != null && serviceId.isNotEmpty) body['serviceId'] = serviceId;
    if (patientId != null && patientId.isNotEmpty && patientId != 'self') {
      body['patientId'] = patientId;
    }

    // Diagnostics print
    debugPrint('BookingService: Sending POST request to ${ApiConstants.appointments}');
    debugPrint('BookingService: Request body: $body');

    final res = await _client.post(
      ApiConstants.appointments,
      body,
      token: token,
    );
    debugPrint('BookingService: Response status code: ${res.statusCode}');
    debugPrint('BookingService: Response data: ${res.data}');
    return ApiAppointmentResult.fromJson(res.data as Map<String, dynamic>);
  }

  /// Danh sách lý do hủy do server cung cấp — không hardcode trong app để thêm/sửa lý do
  /// không phải phát hành bản mới, và để app với web admin không lệch nhau.
  Future<List<CancellationReasonOption>> getCancellationReasons() async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final res = await _client.get(
      '${ApiConstants.appointments}/cancellation-reasons',
      token: token,
    );

    return (res.data as List<dynamic>)
        .map((e) => CancellationReasonOption.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Hủy lịch khám.
  /// [reasonCode] là mã nhóm lý do lấy từ [getCancellationReasons]; [note] là ghi chú tự do,
  /// bắt buộc với những lý do có requiresNote = true.
  Future<void> cancelAppointment(
    String appointmentId,
    String reasonCode, {
    String? note,
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    await _client.put(
      '${ApiConstants.appointments}/$appointmentId/cancel',
      {
        'reason': reasonCode,
        'note': (note != null && note.trim().isNotEmpty) ? note.trim() : null,
      },
      token: token,
    );
  }

  /// Dời lịch khám sang khung giờ (và có thể là bác sĩ) khác.
  ///
  /// Sửa TẠI CHỖ lịch hẹn hiện có nên mã lịch hẹn giữ nguyên — khác hẳn cách cũ là đặt lịch mới
  /// rồi hủy lịch cũ. Để [dentistId]/[serviceId] null nghĩa là giữ nguyên giá trị đang có.
  Future<void> rescheduleAppointment(
    String appointmentId,
    DateTime appointmentDate, {
    String? dentistId,
    String? serviceId,
    String? reason,
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    await _client.put(
      '${ApiConstants.appointments}/$appointmentId/reschedule',
      {
        'appointmentDate': appointmentDate.toUtc().toIso8601String(),
        'dentistId': dentistId,
        'serviceId': serviceId,
        'reason': (reason != null && reason.trim().isNotEmpty) ? reason.trim() : null,
      },
      token: token,
    );
  }
}

/// Một lựa chọn lý do hủy do backend cung cấp.
class CancellationReasonOption {
  const CancellationReasonOption({
    required this.code,
    required this.labelVi,
    required this.labelEn,
    required this.requiresNote,
  });

  final String code;
  final String labelVi;
  final String labelEn;

  /// App phải bắt người dùng nhập ghi chú trước khi cho gửi.
  final bool requiresNote;

  factory CancellationReasonOption.fromJson(Map<String, dynamic> json) =>
      CancellationReasonOption(
        code: json['code'] as String,
        labelVi: json['labelVi'] as String,
        labelEn: json['labelEn'] as String,
        requiresNote: json['requiresNote'] as bool? ?? false,
      );

  String label(bool isVi) => isVi ? labelVi : labelEn;
}

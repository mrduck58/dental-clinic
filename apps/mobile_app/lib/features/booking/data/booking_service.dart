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

  /// Lấy danh sách nha sĩ kèm slot khả dụng cho một ngày cụ thể.
  Future<List<ApiDoctorWithSlots>> getDoctorsWithSlots(DateTime date) async {
    final dateStr =
        '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    final res = await _client.get(
      ApiConstants.dentistSlots,
      queryParameters: {'date': dateStr},
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => ApiDoctorWithSlots.fromJson(e as Map<String, dynamic>))
        .toList();
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

  /// Đặt lịch khám — yêu cầu JWT của bệnh nhân.
  Future<ApiAppointmentResult> createAppointment({
    required String dentistId,
    required DateTime date,
    required String timeSlotRange,
    String? symptoms,
    String? serviceId,
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    // Parse time from slot range, e.g. "07:30 - 08:30" → 07:30
    final timePart = timeSlotRange.split(' - ').first.trim();
    final parts = timePart.split(':');
    final hour = int.parse(parts[0]);
    final minute = int.parse(parts[1]);

    final appointmentDate = DateTime(
      date.year,
      date.month,
      date.day,
      hour,
      minute,
    );

    // Format as ISO 8601 with UTC offset
    final isoDate = appointmentDate.toUtc().toIso8601String();

    final effectiveSymptoms = (symptoms?.isEmpty ?? true) ? null : symptoms;
    final body = <String, dynamic>{
      'dentistId': dentistId,
      'appointmentDate': isoDate,
      'symptoms': ?effectiveSymptoms,
      'serviceId': ?serviceId,
    };

    final res = await _client.post(
      ApiConstants.appointments,
      body,
      token: token,
    );
    return ApiAppointmentResult.fromJson(res.data as Map<String, dynamic>);
  }
}

import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/models/patient_queue_model.dart';

class QueueService {
  static final QueueService _instance = QueueService._internal();
  factory QueueService() => _instance;
  QueueService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  Future<PatientQueueResponse> getPatientQueue({String? patientId}) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final queryParameters = <String, dynamic>{};
    if (patientId != null && patientId.isNotEmpty) {
      queryParameters['patientId'] = patientId;
    }

    final res = await _client.get(
      ApiConstants.patientQueue,
      queryParameters: queryParameters,
      token: token,
    );
    
    return PatientQueueResponse.fromJson(res.data as Map<String, dynamic>);
  }
}

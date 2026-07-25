import 'package:shared_preferences/shared_preferences.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/models/medication_reminder_model.dart';

class MedicationReminderService {
  static final MedicationReminderService _instance = MedicationReminderService._internal();
  factory MedicationReminderService() => _instance;
  MedicationReminderService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  Future<List<MedicationReminder>> getReminders(DateTime date) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final dateStr =
        '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    final res = await _client.get(
      ApiConstants.myMedicationReminders,
      queryParameters: {'date': dateStr},
      token: token,
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => MedicationReminder.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

/// Đánh dấu "đã uống" được lưu cục bộ trên thiết bị (không đồng bộ server) — đây là dữ liệu
/// thật do người dùng tự ghi nhận, không phải giá trị mặc định giả lập.
class ReminderTakenStore {
  static String _key(String prescriptionItemId, DateTime date, String time) {
    final dateStr = '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    return 'reminder_taken_${prescriptionItemId}_${dateStr}_$time';
  }

  static Future<bool> isTaken(String prescriptionItemId, DateTime date, String time) async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getBool(_key(prescriptionItemId, date, time)) ?? false;
  }

  static Future<void> setTaken(String prescriptionItemId, DateTime date, String time, bool taken) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(_key(prescriptionItemId, date, time), taken);
  }
}

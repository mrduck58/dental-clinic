import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/models/notification_models.dart';

class NotificationService {
  static final NotificationService _instance = NotificationService._internal();
  factory NotificationService() => _instance;
  NotificationService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  /// Danh sách thông báo của người dùng hiện tại, có phân trang.
  Future<NotificationPageResult> getNotifications({
    int page = 1,
    int pageSize = 30,
    bool? isRead,
    String? type,
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final res = await _client.get(
      ApiConstants.notifications,
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
        if (isRead != null) 'isRead': isRead,
        if (type != null) 'type': type,
      },
      token: token,
    );
    return NotificationPageResult.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> markAsRead(String id) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    await _client.put(ApiConstants.notificationRead(id), <String, dynamic>{}, token: token);
  }

  Future<void> markAllAsRead() async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    await _client.put(ApiConstants.notificationsReadAll, <String, dynamic>{}, token: token);
  }

  Future<void> delete(String id) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    await _client.delete(ApiConstants.notificationDelete(id), token: token);
  }
}

import 'package:dio/dio.dart';
import 'package:mobile_app/core/constants/api_constants.dart';

class ApiClient {
  static final ApiClient _instance = ApiClient._internal();
  factory ApiClient() => _instance;

  final Dio _dio;

  ApiClient._internal()
      : _dio = Dio(
          BaseOptions(
            baseUrl: ApiConstants.baseUrl,
            connectTimeout: const Duration(seconds: 10),
            receiveTimeout: const Duration(seconds: 10),
            headers: {'Content-Type': 'application/json'},
          ),
        );

  Future<Response<dynamic>> post(
    String path,
    Map<String, dynamic> data, {
    String? token,
  }) {
    return _dio.post(
      path,
      data: data,
      options: _options(token),
    );
  }

  Future<Response<dynamic>> get(
    String path, {
    Map<String, dynamic>? queryParameters,
    String? token,
  }) {
    return _dio.get(
      path,
      queryParameters: queryParameters,
      options: _options(token),
    );
  }

  Future<Response<dynamic>> put(
    String path,
    Map<String, dynamic> data, {
    String? token,
  }) {
    return _dio.put(
      path,
      data: data,
      options: _options(token),
    );
  }

  Options? _options(String? token) => token != null
      ? Options(headers: {'Authorization': 'Bearer $token'})
      : null;

  /// Trích xuất thông báo lỗi từ phản hồi API (format: { title, status }).
  static String errorMessage(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['title'] is String) {
      return data['title'] as String;
    }
    return switch (e.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout =>
        'Kết nối quá thời gian. Vui lòng thử lại.',
      DioExceptionType.connectionError => 'Không thể kết nối máy chủ.',
      _ => 'Đã xảy ra lỗi. Vui lòng thử lại.',
    };
  }
}

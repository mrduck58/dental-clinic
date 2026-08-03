import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/models/review_model.dart';

class ReviewService {
  static final ReviewService _instance = ReviewService._internal();
  factory ReviewService() => _instance;
  ReviewService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  Future<DentistReviewsResult> getReviewsForDentist(String dentistId) async {
    final res = await _client.get(ApiConstants.dentistReviews(dentistId));
    return DentistReviewsResult.fromJson(res.data as Map<String, dynamic>);
  }

  /// Gửi (hoặc cập nhật) đánh giá của bệnh nhân hiện tại cho nha sĩ.
  Future<ReviewModel> submitReview({
    required String dentistId,
    required double rating,
    required String comment,
    required List<String> tags,
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.post(
      ApiConstants.dentistReviews(dentistId),
      {
        'rating': rating.round(),
        'comment': comment,
        'tags': tags,
      },
      token: token,
    );
    return ReviewModel.fromJson(res.data as Map<String, dynamic>);
  }

  /// Kiểm tra xem bệnh nhân hiện tại có đủ điều kiện đánh giá nha sĩ hay không.
  Future<ReviewEligibilityModel> checkEligibility(String dentistId) async {
    final token = await _auth.getToken();
    if (token == null) {
      return const ReviewEligibilityModel(
        canReview: false,
        reason: 'Vui lòng đăng nhập để thực hiện đánh giá.',
      );
    }
    try {
      final res = await _client.get(
        ApiConstants.dentistReviewEligibility(dentistId),
        token: token,
      );
      return ReviewEligibilityModel.fromJson(res.data as Map<String, dynamic>);
    } catch (e) {
      return const ReviewEligibilityModel(
        canReview: false,
        reason: 'Không thể kiểm tra điều kiện đánh giá.',
      );
    }
  }

  /// Gửi đánh giá / phản hồi phòng khám.
  Future<ClinicFeedbackModel> submitClinicFeedback({
    required double rating,
    required String comment,
    String? customerName,
  }) async {
    final token = await _auth.getToken();
    final name = customerName ?? await _auth.getUserName() ?? 'Khách hàng';
    final res = await _client.post(
      ApiConstants.feedbacks,
      {
        'customerName': name,
        'rating': rating.round(),
        'comment': comment,
      },
      token: token,
    );
    return ClinicFeedbackModel.fromJson(res.data as Map<String, dynamic>);
  }

  /// Lấy danh sách đánh giá nổi bật của phòng khám.
  Future<List<ClinicFeedbackModel>> getFeaturedClinicFeedbacks() async {
    final res = await _client.get(ApiConstants.featuredFeedbacks);
    final list = res.data as List<dynamic>? ?? [];
    return list.map((e) => ClinicFeedbackModel.fromJson(e as Map<String, dynamic>)).toList();
  }
}


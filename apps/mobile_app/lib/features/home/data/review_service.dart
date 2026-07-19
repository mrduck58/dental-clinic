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
}

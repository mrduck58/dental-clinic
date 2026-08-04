class ReviewModel {
  final String id;
  final String patientName;
  final double rating;
  final String comment;
  final List<String> tags;
  final DateTime createdAt;
  final String? serviceName;

  const ReviewModel({
    required this.id,
    required this.patientName,
    required this.rating,
    required this.comment,
    required this.tags,
    required this.createdAt,
    this.serviceName,
  });

  factory ReviewModel.fromJson(Map<String, dynamic> json) => ReviewModel(
        id: json['id'].toString(),
        patientName: json['patientName'] as String? ?? '',
        rating: (json['rating'] as num?)?.toDouble() ?? 0,
        comment: json['comment'] as String? ?? '',
        tags: (json['tags'] as List<dynamic>? ?? []).map((e) => e.toString()).toList(),
        createdAt: DateTime.parse(json['createdAt'] as String),
        serviceName: json['serviceName'] as String?,
      );
}

class DentistReviewsResult {
  final double averageRating;
  final int reviewCount;
  final List<ReviewModel> reviews;

  const DentistReviewsResult({
    required this.averageRating,
    required this.reviewCount,
    required this.reviews,
  });

  factory DentistReviewsResult.fromJson(Map<String, dynamic> json) => DentistReviewsResult(
        averageRating: (json['averageRating'] as num?)?.toDouble() ?? 0,
        reviewCount: (json['reviewCount'] as num?)?.toInt() ?? 0,
        reviews: (json['reviews'] as List<dynamic>? ?? [])
            .map((e) => ReviewModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ReviewEligibilityModel {
  final bool canReview;
  final String reason;
  final ReviewModel? myReview;

  const ReviewEligibilityModel({
    required this.canReview,
    required this.reason,
    this.myReview,
  });

  factory ReviewEligibilityModel.fromJson(Map<String, dynamic> json) => ReviewEligibilityModel(
        canReview: json['canReview'] as bool? ?? false,
        reason: json['reason'] as String? ?? '',
        myReview: json['myReview'] != null ? ReviewModel.fromJson(json['myReview'] as Map<String, dynamic>) : null,
      );
}

class ClinicFeedbackModel {
  final String id;
  final String customerName;
  final double rating;
  final String comment;
  final String? replyText;
  final DateTime createdAt;

  const ClinicFeedbackModel({
    required this.id,
    required this.customerName,
    required this.rating,
    required this.comment,
    this.replyText,
    required this.createdAt,
  });

  factory ClinicFeedbackModel.fromJson(Map<String, dynamic> json) => ClinicFeedbackModel(
        id: json['id'].toString(),
        customerName: json['customerName'] as String? ?? 'Khách hàng',
        rating: (json['rating'] as num?)?.toDouble() ?? 0,
        comment: json['comment'] as String? ?? '',
        replyText: json['replyText'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

class ClinicFeedbackEligibilityModel {
  final bool canReview;
  final String reason;
  final bool hasCompletedFirstVisit;

  const ClinicFeedbackEligibilityModel({
    required this.canReview,
    required this.reason,
    required this.hasCompletedFirstVisit,
  });

  factory ClinicFeedbackEligibilityModel.fromJson(Map<String, dynamic> json) {
    return ClinicFeedbackEligibilityModel(
      canReview: json['canReview'] as bool? ?? false,
      reason: json['reason'] as String? ?? '',
      hasCompletedFirstVisit: json['hasCompletedFirstVisit'] as bool? ?? false,
    );
  }
}


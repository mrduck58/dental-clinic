class ReviewModel {
  final String id;
  final String patientName;
  final double rating;
  final String comment;
  final List<String> tags;
  final DateTime createdAt;

  const ReviewModel({
    required this.id,
    required this.patientName,
    required this.rating,
    required this.comment,
    required this.tags,
    required this.createdAt,
  });

  factory ReviewModel.fromJson(Map<String, dynamic> json) => ReviewModel(
        id: json['id'].toString(),
        patientName: json['patientName'] as String? ?? '',
        rating: (json['rating'] as num?)?.toDouble() ?? 0,
        comment: json['comment'] as String? ?? '',
        tags: (json['tags'] as List<dynamic>? ?? []).map((e) => e.toString()).toList(),
        createdAt: DateTime.parse(json['createdAt'] as String),
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

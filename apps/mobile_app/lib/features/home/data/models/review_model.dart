class ReviewModel {
  final String id;
  final String dentistId;
  final String patientName;
  final double rating;
  final String comment;
  final String date;
  final List<String> tags;
  final String? patientAvatar;

  const ReviewModel({
    required this.id,
    required this.dentistId,
    required this.patientName,
    required this.rating,
    required this.comment,
    required this.date,
    required this.tags,
    this.patientAvatar,
  });
}

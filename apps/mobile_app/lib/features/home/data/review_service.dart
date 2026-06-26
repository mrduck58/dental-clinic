import 'package:mobile_app/features/home/data/models/review_model.dart';

class ReviewService {
  static final ReviewService _instance = ReviewService._internal();
  factory ReviewService() => _instance;
  ReviewService._internal();

  final List<ReviewModel> _reviews = [];

  void _seedDefaultReviews(String dentistId) {
    _reviews.addAll([
      ReviewModel(
        id: 'mock_1_$dentistId',
        dentistId: dentistId,
        patientName: 'Lê Minh Trí',
        rating: 5.0,
        comment: 'Bác sĩ thực sự tuyệt vời! Tôi đã rất lo lắng về việc nhổ răng khôn, nhưng bác sĩ đã giải thích mọi thứ rất rõ ràng. Quá trình thực hiện hoàn toàn không đau và hồi phục nhanh hơn tôi tưởng tượng rất nhiều.',
        date: '24 Th10, 2025',
        tags: const ['Nhổ răng', 'Không đau', 'Thân thiện'],
      ),
      ReviewModel(
        id: 'mock_2_$dentistId',
        dentistId: dentistId,
        patientName: 'Nguyễn Thị Mai',
        rating: 4.0,
        comment: 'Trải nghiệm tuyệt vời khi lấy cao răng định kỳ. Phòng khám cực kỳ sạch sẽ và hiện đại. Bác sĩ đã tư vấn thêm nhiều mẹo chăm sóc răng nhạy cảm rất hữu ích.',
        date: '12 Th10, 2025',
        tags: const ['Vệ sinh răng', 'Tận tâm', 'Sạch sẽ'],
      ),
      ReviewModel(
        id: 'mock_3_$dentistId',
        dentistId: dentistId,
        patientName: 'Trần Hoàng Nam',
        rating: 5.0,
        comment: 'Tôi đang điều trị niềng răng khay trong suốt Invisalign ở đây. Tiến triển rất tốt và sự tỉ mỉ của bác sĩ là không có gì phải bàn cãi. Bác sĩ thực sự quan tâm sát sao đến kết quả lâu dài của bệnh nhân.',
        date: '28 Th9, 2025',
        tags: const ['Niềng răng', 'Chuyên nghiệp'],
      ),
      ReviewModel(
        id: 'mock_4_$dentistId',
        dentistId: dentistId,
        patientName: 'Phan Thu Thảo',
        rating: 4.0,
        comment: 'Đội ngũ bác sĩ và nhân viên cực kỳ chuyên nghiệp. Thời gian chờ đợi tối thiểu và chi phí điều trị rất minh bạch, rõ ràng. Bác sĩ khám rất nhẹ nhàng và chu đáo.',
        date: '15 Th9, 2025',
        tags: const ['Chuyên nghiệp', 'Minh bạch', 'Nhiệt tình'],
      ),
    ]);
  }

  List<ReviewModel> getReviewsForDentist(String dentistId) {
    final dentistReviews = _reviews.where((r) => r.dentistId == dentistId).toList();
    if (dentistReviews.isEmpty) {
      _seedDefaultReviews(dentistId);
      // Re-fetch after seeding
      return _reviews.where((r) => r.dentistId == dentistId).toList();
    }
    // New reviews on top (ordered by creation/ID)
    dentistReviews.sort((a, b) {
      // Sort mock IDs, but we put custom added reviews first.
      // Mock IDs look like "mock_X_dentistId", custom look like "rev_time"
      if (a.id.startsWith('rev_') && !b.id.startsWith('rev_')) return -1;
      if (!a.id.startsWith('rev_') && b.id.startsWith('rev_')) return 1;
      return b.id.compareTo(a.id);
    });
    return dentistReviews;
  }

  double getAverageRating(String dentistId) {
    final list = getReviewsForDentist(dentistId);
    if (list.isEmpty) return 5.0;
    final total = list.map((e) => e.rating).reduce((a, b) => a + b);
    return double.parse((total / list.length).toStringAsFixed(1));
  }

  void addReview({
    required String dentistId,
    required double rating,
    required String comment,
    required List<String> tags,
    required String patientName,
  }) {
    final now = DateTime.now();
    final dateStr = '${now.day} Th${now.month}, ${now.year}';
    final newReview = ReviewModel(
      id: 'rev_${now.millisecondsSinceEpoch}',
      dentistId: dentistId,
      patientName: patientName,
      rating: rating,
      comment: comment,
      date: dateStr,
      tags: tags,
    );
    _reviews.add(newReview);
  }
}

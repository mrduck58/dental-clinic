class PostModel {
  final String id;
  final String title;
  final String category;
  final String author;
  final String content;
  final String? thumbnailUrl;
  final String? publishedAt;

  const PostModel({
    required this.id,
    required this.title,
    required this.category,
    required this.author,
    required this.content,
    this.thumbnailUrl,
    this.publishedAt,
  });

  factory PostModel.fromJson(Map<String, dynamic> json) {
    return PostModel(
      id: json['id'].toString(),
      title: json['title'] as String,
      category: json['category'] as String? ?? '',
      author: json['author'] as String? ?? '',
      content: json['content'] as String? ?? '',
      thumbnailUrl: json['thumbnailUrl'] as String?,
      publishedAt: (json['publishedAt'] ?? json['createdAt']) as String?,
    );
  }

  String get formattedDate {
    if (publishedAt == null) return '';
    try {
      final dt = DateTime.parse(publishedAt!).toLocal();
      const months = ['Th1','Th2','Th3','Th4','Th5','Th6','Th7','Th8','Th9','Th10','Th11','Th12'];
      return '${dt.day} ${months[dt.month - 1]} ${dt.year}';
    } catch (_) {
      return '';
    }
  }

  String get readTimeText {
    final words = content.trim().split(RegExp(r'\s+'));
    final minutes = (words.length / 200).ceil().clamp(1, 60);
    return '$minutes phút';
  }
}

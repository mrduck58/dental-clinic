class ServiceModel {
  final String id;
  final String name;
  final double price;
  final int durationMinutes;
  final String description;
  final String? imageUrl;
  final String? iconUrl;
  final int viewCount;

  const ServiceModel({
    required this.id,
    required this.name,
    required this.price,
    required this.durationMinutes,
    required this.description,
    this.imageUrl,
    this.iconUrl,
    this.viewCount = 0,
  });

  factory ServiceModel.fromJson(Map<String, dynamic> json) {
    return ServiceModel(
      id: json['id'].toString(),
      name: json['name'] as String,
      price: (json['price'] as num).toDouble(),
      durationMinutes: json['durationMinutes'] as int,
      description: json['description'] as String? ?? '',
      imageUrl: json['imageUrl'] as String?,
      iconUrl: json['iconUrl'] as String?,
      viewCount: json['viewCount'] as int? ?? json['ViewCount'] as int? ?? 0,
    );
  }

  String get formattedPrice {
    final p = price.toInt();
    final s = p.toString();
    final buf = StringBuffer();
    for (var i = 0; i < s.length; i++) {
      if (i > 0 && (s.length - i) % 3 == 0) buf.write('.');
      buf.write(s[i]);
    }
    return 'Từ ${buf.toString()}đ';
  }

  String get durationText {
    if (durationMinutes < 60) return '$durationMinutes phút';
    final h = durationMinutes ~/ 60;
    final m = durationMinutes % 60;
    return m == 0 ? '$h giờ' : '$h giờ $m phút';
  }
}

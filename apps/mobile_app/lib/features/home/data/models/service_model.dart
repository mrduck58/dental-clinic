class ServiceOptionModel {
  final String id;
  final String name;
  final double price;
  final String unit;
  final int sortOrder;

  const ServiceOptionModel({
    required this.id,
    required this.name,
    required this.price,
    required this.unit,
    this.sortOrder = 0,
  });

  factory ServiceOptionModel.fromJson(Map<String, dynamic> json) {
    return ServiceOptionModel(
      id: json['id']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
      unit: json['unit'] as String? ?? 'Răng',
      sortOrder: json['sortOrder'] as int? ?? 0,
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
    return '${buf.toString()}đ';
  }
}

class ServiceModel {
  final String id;
  final String name;
  final double price;
  final int durationMinutes;
  final String description;
  final String? imageUrl;
  final String? iconUrl;
  final int viewCount;
  final List<ServiceOptionModel> options;

  const ServiceModel({
    required this.id,
    required this.name,
    required this.price,
    required this.durationMinutes,
    required this.description,
    this.imageUrl,
    this.iconUrl,
    this.viewCount = 0,
    this.options = const [],
  });

  factory ServiceModel.fromJson(Map<String, dynamic> json) {
    final rawOptions = json['options'] as List<dynamic>?;
    final rawDuration = json['durationMinutes'] ?? json['DurationMinutes'];
    final parsedDuration = (rawDuration is num)
        ? rawDuration.toInt()
        : (int.tryParse(rawDuration?.toString() ?? '') ?? 0);

    return ServiceModel(
      id: json['id'].toString(),
      name: json['name'] as String? ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
      durationMinutes: parsedDuration > 0 ? parsedDuration : 30,
      description: json['description'] as String? ?? '',
      imageUrl: json['imageUrl'] as String?,
      iconUrl: json['iconUrl'] as String?,
      viewCount: json['viewCount'] as int? ?? json['ViewCount'] as int? ?? 0,
      options: rawOptions != null
          ? rawOptions
              .map((e) => ServiceOptionModel.fromJson(e as Map<String, dynamic>))
              .toList()
          : const [],
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
    return '${buf.toString()}đ';
  }

  String get durationText {
    final d = durationMinutes > 0 ? durationMinutes : 30;
    if (d < 60) return '~$d phút';
    final h = d ~/ 60;
    final m = d % 60;
    return m == 0 ? '~$h giờ' : '~$h giờ $m phút';
  }

  String durationTextLocalized(bool isVi) {
    final d = durationMinutes > 0 ? durationMinutes : 30;
    if (isVi) {
      if (d < 60) return '~$d phút';
      final h = d ~/ 60;
      final m = d % 60;
      return m == 0 ? '~$h giờ' : '~$h giờ $m phút';
    } else {
      if (d < 60) return '~$d mins';
      final h = d ~/ 60;
      final m = d % 60;
      return m == 0 ? '~$h hr' : '~$h hr $m mins';
    }
  }
}

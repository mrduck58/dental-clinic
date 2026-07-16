/// Một thông báo thật từ backend — Type khớp với DentalClinic.API.Domain.Constants.NotificationType
/// ("system", "account", "schedule", "service", "security", "reminder", "appointment", "invoice").
/// Title/Body luôn là tiếng Việt (backend sinh sẵn nội dung, không có bản dịch song ngữ như dữ liệu mẫu cũ).
class NotificationItem {
  final String id;
  final String type;
  final String priority;
  final String title;
  final String body;
  final bool isRead;
  final DateTime? readAt;
  final String? relatedEntityType;
  final String? relatedEntityId;
  final DateTime createdAt;

  const NotificationItem({
    required this.id,
    required this.type,
    required this.priority,
    required this.title,
    required this.body,
    required this.isRead,
    this.readAt,
    this.relatedEntityType,
    this.relatedEntityId,
    required this.createdAt,
  });

  factory NotificationItem.fromJson(Map<String, dynamic> json) => NotificationItem(
        id: json['id'] as String,
        type: (json['type'] as String).toLowerCase(),
        priority: (json['priority'] as String).toLowerCase(),
        title: json['title'] as String,
        body: json['body'] as String,
        isRead: json['isRead'] as bool,
        readAt: json['readAt'] != null ? DateTime.parse(json['readAt'] as String) : null,
        relatedEntityType: json['relatedEntityType'] as String?,
        relatedEntityId: json['relatedEntityId'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );

  NotificationItem copyWith({bool? isRead}) => NotificationItem(
        id: id,
        type: type,
        priority: priority,
        title: title,
        body: body,
        isRead: isRead ?? this.isRead,
        readAt: readAt,
        relatedEntityType: relatedEntityType,
        relatedEntityId: relatedEntityId,
        createdAt: createdAt,
      );
}

class NotificationPageResult {
  final List<NotificationItem> items;
  final int total;
  final int page;
  final int pageSize;
  final int totalPages;
  final int unreadCount;

  const NotificationPageResult({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
    required this.totalPages,
    required this.unreadCount,
  });

  factory NotificationPageResult.fromJson(Map<String, dynamic> json) => NotificationPageResult(
        items: (json['items'] as List<dynamic>)
            .map((e) => NotificationItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        total: json['total'] as int,
        page: json['page'] as int,
        pageSize: json['pageSize'] as int,
        totalPages: json['totalPages'] as int,
        unreadCount: json['unreadCount'] as int,
      );
}

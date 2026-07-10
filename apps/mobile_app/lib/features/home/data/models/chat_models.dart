class ChatConversationSummary {
  final String id;
  final String preview;
  final DateTime updatedAt;

  ChatConversationSummary({
    required this.id,
    required this.preview,
    required this.updatedAt,
  });

  factory ChatConversationSummary.fromJson(Map<String, dynamic> json) =>
      ChatConversationSummary(
        id: json['id'] as String,
        preview: json['preview'] as String,
        updatedAt: DateTime.parse(json['updatedAt'] as String),
      );
}

class ChatMessageItem {
  final String role;
  final String content;
  final DateTime createdAt;

  ChatMessageItem({
    required this.role,
    required this.content,
    required this.createdAt,
  });

  bool get isUser => role == 'user';

  factory ChatMessageItem.fromJson(Map<String, dynamic> json) =>
      ChatMessageItem(
        role: json['role'] as String,
        content: json['content'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Gợi ý điền sẵn cho luồng đặt lịch, rút ra từ hội thoại. serviceId/dentistId chỉ khác null khi
/// backend đã đối chiếu được với dữ liệu thật — chỉ nên dùng các trường có kèm id, không dùng tên
/// "trôi nổi" chưa đối chiếu được.
class ChatBookingHint {
  final String? serviceId;
  final String? serviceName;
  final String? dentistId;
  final String? dentistName;
  final DateTime? preferredDate;
  final String? notes;

  ChatBookingHint({
    this.serviceId,
    this.serviceName,
    this.dentistId,
    this.dentistName,
    this.preferredDate,
    this.notes,
  });

  factory ChatBookingHint.fromJson(Map<String, dynamic> json) => ChatBookingHint(
        serviceId: json['serviceId'] as String?,
        serviceName: json['serviceName'] as String?,
        dentistId: json['dentistId'] as String?,
        dentistName: json['dentistName'] as String?,
        preferredDate: json['preferredDate'] != null
            ? DateTime.parse(json['preferredDate'] as String)
            : null,
        notes: json['notes'] as String?,
      );
}

class ChatSendResult {
  final String reply;
  final bool suggestBooking;
  final ChatBookingHint bookingHint;

  ChatSendResult({
    required this.reply,
    required this.suggestBooking,
    required this.bookingHint,
  });

  factory ChatSendResult.fromJson(Map<String, dynamic> json) =>
      ChatSendResult(
        reply: json['reply'] as String,
        suggestBooking: json['suggestBooking'] as bool? ?? false,
        bookingHint: ChatBookingHint.fromJson(
          (json['bookingHint'] as Map<String, dynamic>?) ?? const {},
        ),
      );
}

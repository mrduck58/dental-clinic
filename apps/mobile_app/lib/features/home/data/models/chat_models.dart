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
  final String? patientId;
  final String? patientName;

  ChatBookingHint({
    this.serviceId,
    this.serviceName,
    this.dentistId,
    this.dentistName,
    this.preferredDate,
    this.notes,
    this.patientId,
    this.patientName,
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
        patientId: json['patientId'] as String?,
        patientName: json['patientName'] as String?,
      );
}

/// bookingCreated/bookingCancelled/bookingRescheduled = true khi bot đã thực sự đặt/hủy/dời lịch hẹn
/// ngay trong hội thoại (kèm appointmentCode) — UI hiển thị nút "Xem lịch hẹn" thay vì nút "Đặt lịch".
class ChatSendResult {
  final String reply;
  final bool suggestBooking;
  final ChatBookingHint bookingHint;
  final bool bookingCreated;
  final String? appointmentCode;
  final bool bookingCancelled;
  final bool bookingRescheduled;

  ChatSendResult({
    required this.reply,
    required this.suggestBooking,
    required this.bookingHint,
    this.bookingCreated = false,
    this.appointmentCode,
    this.bookingCancelled = false,
    this.bookingRescheduled = false,
  });

  factory ChatSendResult.fromJson(Map<String, dynamic> json) =>
      ChatSendResult(
        reply: json['reply'] as String,
        suggestBooking: json['suggestBooking'] as bool? ?? false,
        bookingHint: ChatBookingHint.fromJson(
          (json['bookingHint'] as Map<String, dynamic>?) ?? const {},
        ),
        bookingCreated: json['bookingCreated'] as bool? ?? false,
        appointmentCode: json['appointmentCode'] as String?,
        bookingCancelled: json['bookingCancelled'] as bool? ?? false,
        bookingRescheduled: json['bookingRescheduled'] as bool? ?? false,
      );
}

/// InitialMessage khác null khi bệnh nhân có lịch hẹn sắp tới — bot chủ động nhắc ngay
/// khi bắt đầu một cuộc trò chuyện MỚI.
class StartConversationResult {
  final String conversationId;
  final String? initialMessage;

  StartConversationResult({required this.conversationId, this.initialMessage});

  factory StartConversationResult.fromJson(Map<String, dynamic> json) =>
      StartConversationResult(
        conversationId: json['conversationId'] as String,
        initialMessage: json['initialMessage'] as String?,
      );
}

import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/models/chat_models.dart';

class ChatService {
  static final ChatService _instance = ChatService._internal();
  factory ChatService() => _instance;
  ChatService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  /// Bắt đầu một cuộc trò chuyện mới với chatbot AI. [language] quyết định bot chủ động nhắc lịch
  /// hẹn sắp tới (nếu có) bằng ngôn ngữ nào.
  Future<StartConversationResult> startConversation({String language = 'vi'}) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final res = await _client.post(
      '${ApiConstants.chatConversations}?language=$language',
      <String, dynamic>{},
      token: token,
    );
    return StartConversationResult.fromJson(res.data as Map<String, dynamic>);
  }

  /// Gửi tin nhắn cho chatbot AI, trả về phản hồi kèm cờ gợi ý đặt lịch.
  /// [language] là mã ngôn ngữ app đang dùng ('vi'/'en') để bot trả lời đúng ngôn ngữ.
  Future<ChatSendResult> sendMessage(
    String conversationId,
    String message, {
    String language = 'vi',
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final res = await _client.post(
      ApiConstants.chatMessages(conversationId),
      {'message': message, 'language': language},
      token: token,
    );
    return ChatSendResult.fromJson(res.data as Map<String, dynamic>);
  }

  /// Danh sách cuộc trò chuyện trước đây của bệnh nhân hiện tại (cho drawer lịch sử).
  Future<List<ChatConversationSummary>> listConversations() async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final res = await _client.get(ApiConstants.chatConversations, token: token);
    final list = res.data as List<dynamic>;
    return list
        .map((e) => ChatConversationSummary.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Toàn bộ tin nhắn của một cuộc trò chuyện cụ thể (khi mở lại từ lịch sử).
  Future<List<ChatMessageItem>> getConversation(String conversationId) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');

    final res = await _client.get(
      ApiConstants.chatConversation(conversationId),
      token: token,
    );
    final data = res.data as Map<String, dynamic>;
    final messages = data['messages'] as List<dynamic>;
    return messages
        .map((e) => ChatMessageItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

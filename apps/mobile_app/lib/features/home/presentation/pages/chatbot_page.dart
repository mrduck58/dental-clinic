import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class ChatMessage {
  final String text;
  final bool isUser;
  final DateTime timestamp;
  final Widget? customWidget;

  ChatMessage({
    required this.text,
    required this.isUser,
    required this.timestamp,
    this.customWidget,
  });
}

class ChatSession {
  final String id;
  final String title;
  final List<ChatMessage> messages;

  ChatSession({required this.id, required this.title, required this.messages});
}

class ChatbotPage extends StatefulWidget {
  const ChatbotPage({super.key});

  @override
  State<ChatbotPage> createState() => _ChatbotPageState();
}

class _ChatbotPageState extends State<ChatbotPage> {
  final GlobalKey<ScaffoldState> _scaffoldKey = GlobalKey<ScaffoldState>();
  final TextEditingController _messageCtrl = TextEditingController();
  final List<ChatMessage> _messages = [];
  bool _isTyping = false;

  late List<ChatSession> _historySessions;

  @override
  void initState() {
    super.initState();
    _historySessions = [
      ChatSession(
        id: 's1',
        title: 'Tư vấn sâu răng (24/06/2026)',
        messages: [
          ChatMessage(text: 'Chào bạn, tôi bị đau nhức răng hàm dưới.', isUser: true, timestamp: DateTime.now().subtract(const Duration(days: 3))),
          ChatMessage(text: 'Chào bạn! Đau nhức răng hàm dưới có thể do sâu răng tiến triển hoặc viêm nướu quanh răng khôn. Bạn nên chườm lạnh giảm sưng và súc miệng nước muối ấm. Hãy đặt lịch hẹn để bác sĩ chụp X-quang khám chi tiết nhé.', isUser: false, timestamp: DateTime.now().subtract(const Duration(days: 3))),
        ],
      ),
      ChatSession(
        id: 's2',
        title: 'Chăm sóc răng sứ (18/06/2026)',
        messages: [
          ChatMessage(text: 'Răng sứ sau khi bọc cần kiêng ăn đồ quá cứng đúng không?', isUser: true, timestamp: DateTime.now().subtract(const Duration(days: 9))),
          ChatMessage(text: 'Hoàn toàn chính xác! Tránh cắn đồ ăn quá dai cứng như xương, sụn, đá lạnh để bảo vệ lớp sứ không sứt mẻ. Chải răng nhẹ nhàng bằng bàn chải lông mềm ít nhất 2 lần/ngày nhé.', isUser: false, timestamp: DateTime.now().subtract(const Duration(days: 9))),
        ],
      ),
    ];

    // Welcome Message
    _messages.add(
      ChatMessage(
        text: 'Hello! I am your Dental AI Assistant. How can I help you today?',
        isUser: false,
        timestamp: DateTime.now(),
      ),
    );
  }

  @override
  void dispose() {
    _messageCtrl.dispose();
    super.dispose();
  }

  void _sendMessage(String text) {
    if (text.trim().isEmpty) return;

    setState(() {
      _messages.add(ChatMessage(text: text, isUser: true, timestamp: DateTime.now()));
      _isTyping = true;
    });
    _messageCtrl.clear();

    // Trigger AI response simulation
    Future.delayed(const Duration(seconds: 1, milliseconds: 200), () {
      if (!mounted) return;
      setState(() {
        _isTyping = false;
        _messages.add(_getAIResponse(text));
      });
    });
  }

  ChatMessage _getAIResponse(String userInput) {
    final lowerInput = userInput.toLowerCase();
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    if (lowerInput.contains('đau nhói') || lowerInput.contains('severe pain') || lowerInput.contains('đau nhức')) {
      return ChatMessage(
        text: isVi 
            ? 'Cảnh báo: Cơn đau nhói dữ dội có thể là dấu hiệu viêm tủy cấp tính. Vui lòng đặt khám khẩn cấp để điều trị tủy kịp thời.'
            : 'Warning: Severe throbbing pain may indicate acute pulpitis. Please book an urgent checkup for root canal therapy.',
        isUser: false,
        timestamp: DateTime.now(),
        customWidget: _buildUrgentBookingCard(isVi),
      );
    } else if (lowerInput.contains('ê buốt') || lowerInput.contains('sensitivity') || lowerInput.contains('sensitive')) {
      return ChatMessage(
        text: isVi
            ? 'Ê buốt răng thường do mòn men răng hoặc tụt nướu. Bạn nên sử dụng kem đánh răng cho răng nhạy cảm và tránh thức uống lạnh.'
            : 'Tooth sensitivity is often caused by enamel wear or receding gums. Consider using desensitizing toothpaste and avoid cold drinks.',
        isUser: false,
        timestamp: DateTime.now(),
      );
    } else if (lowerInput.contains('đặt lịch') || lowerInput.contains('booking') || lowerInput.contains('appointment')) {
      return ChatMessage(
        text: isVi ? 'Bạn có muốn đặt lịch hẹn ngay bây giờ?' : 'Would you like to book an appointment now?',
        isUser: false,
        timestamp: DateTime.now(),
        customWidget: _buildUrgentBookingCard(isVi),
      );
    } else {
      return ChatMessage(
        text: isVi
            ? 'Cảm ơn thông tin từ bạn. Để được chẩn đoán chính xác nhất, bạn nên sắp xếp một buổi khám lâm sàng với bác sĩ tại phòng khám.'
            : 'Thank you for the info. For a precise diagnosis, it is highly recommended to schedule a clinical exam with our dentists.',
        isUser: false,
        timestamp: DateTime.now(),
      );
    }
  }

  Widget _buildUrgentBookingCard(bool isVi) {
    return Container(
      margin: const EdgeInsets.only(top: 10),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: context.isDark ? const Color(0xFF334155) : const Color(0xFFEFF6FF),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.isDark ? Colors.transparent : const Color(0xFFBFDBFE)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Iconsax.calendar_tick5, color: Color(0xFF3B82F6), size: 20),
              const SizedBox(width: 8),
              Text(
                isVi ? 'Đặt khám ngay' : 'Book Checkup Now',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w800,
                  color: context.isDark ? Colors.white : const Color(0xFF1E3A8A),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            isVi 
                ? 'Hệ thống tự động ưu tiên khung giờ trống gần nhất cho bạn.'
                : 'System will auto-prioritize the earliest slot available for you.',
            style: TextStyle(
              fontSize: 12,
              color: context.isDark ? const Color(0xFF94A3B8) : const Color(0xFF1E40AF),
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            height: 36,
            child: ElevatedButton(
              onPressed: () => context.push(AppRoutes.bookingSelectPatient),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
              ),
              child: Text(
                isVi ? 'ĐẶT LỊCH HẸN' : 'BOOK APPOINTMENT',
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 11),
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _loadSession(ChatSession session) {
    setState(() {
      _messages.clear();
      _messages.add(
        ChatMessage(
          text: 'Hello! I am your Dental AI Assistant. How can I help you today?',
          isUser: false,
          timestamp: DateTime.now(),
        ),
      );
      _messages.addAll(session.messages);
    });
    Navigator.pop(context); // Close Drawer
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      key: _scaffoldKey,
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Row(
          children: [
            Container(
              width: 34,
              height: 34,
              decoration: const BoxDecoration(
                color: Color(0xFFDC2626),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.smart_toy_rounded, color: Colors.white, size: 18),
            ),
            const SizedBox(width: 10),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Dental AI',
                  style: TextStyle(
                    color: context.textPrimary,
                    fontWeight: FontWeight.bold,
                    fontSize: 16,
                  ),
                ),
                Text(
                  isVi ? 'Tư vấn thông minh' : 'Smart Assistant',
                  style: TextStyle(
                    color: context.textSecondary,
                    fontSize: 11,
                  ),
                ),
              ],
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.more_horiz_rounded, color: context.textPrimary, size: 26),
            onPressed: () => _scaffoldKey.currentState?.openEndDrawer(),
          ),
        ],
      ),
      endDrawer: _buildHistoryDrawer(isVi),
      body: Column(
        children: [
          // Message list
          Expanded(
            child: ListView.builder(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
              itemCount: _messages.length + (_isTyping ? 1 : 0),
              itemBuilder: (context, i) {
                if (i == _messages.length) {
                  return _buildTypingIndicator();
                }
                final msg = _messages[i];
                return _buildMessageBubble(msg);
              },
            ),
          ),

          // Quick Replies
          if (_messages.length == 1) _buildQuickReplies(isVi),

          // Input field
          _buildInputBar(isVi),
        ],
      ),
    );
  }

  Widget _buildHistoryDrawer(bool isVi) {
    return Drawer(
      backgroundColor: context.bg,
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.all(20.0),
              child: Row(
                children: [
                  const Icon(Iconsax.clock, color: AppColors.primary, size: 22),
                  const SizedBox(width: 10),
                  Text(
                    isVi ? 'Lịch sử tư vấn' : 'Consultation History',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w900,
                      color: context.textPrimary,
                    ),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            Expanded(
              child: ListView.builder(
                padding: const EdgeInsets.symmetric(vertical: 10),
                itemCount: _historySessions.length,
                itemBuilder: (context, i) {
                  final session = _historySessions[i];
                  return ListTile(
                    leading: const Icon(Iconsax.message, size: 20),
                    title: Text(
                      session.title,
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: context.textPrimary,
                      ),
                    ),
                    onTap: () => _loadSession(session),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMessageBubble(ChatMessage msg) {
    return Align(
      alignment: msg.isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 16),
        constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.78),
        child: Column(
          crossAxisAlignment: msg.isUser ? CrossAxisAlignment.end : CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: msg.isUser
                    ? AppColors.primary
                    : (context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9)),
                borderRadius: BorderRadius.only(
                  topLeft: const Radius.circular(16),
                  topRight: const Radius.circular(16),
                  bottomLeft: msg.isUser ? const Radius.circular(16) : Radius.zero,
                  bottomRight: msg.isUser ? Radius.zero : const Radius.circular(16),
                ),
                border: msg.isUser ? null : Border(left: BorderSide(color: AppColors.primary, width: 3)),
              ),
              child: Text(
                msg.text,
                style: TextStyle(
                  color: msg.isUser ? Colors.white : context.textPrimary,
                  fontSize: 14.5,
                  fontWeight: FontWeight.w600,
                  height: 1.45,
                ),
              ),
            ),
            if (msg.customWidget != null) msg.customWidget!,
          ],
        ),
      ),
    );
  }

  Widget _buildTypingIndicator() {
    return Align(
      alignment: Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 16),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
          borderRadius: const BorderRadius.only(
            topLeft: Radius.circular(16),
            topRight: Radius.circular(16),
            bottomRight: Radius.circular(16),
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            _buildDot(),
            const SizedBox(width: 4),
            _buildDot(),
            const SizedBox(width: 4),
            _buildDot(),
          ],
        ),
      ),
    );
  }

  Widget _buildDot() {
    return Container(
      width: 6,
      height: 6,
      decoration: BoxDecoration(
        color: context.textSecondary,
        shape: BoxShape.circle,
      ),
    );
  }

  Widget _buildQuickReplies(bool isVi) {
    final replies = [
      isVi ? 'Đau nhói dữ dội' : 'Severe Throbbing Pain',
      isVi ? 'Ê buốt răng lạnh' : 'Cold Sensitive Teeth',
      isVi ? 'Đặt lịch khám' : 'Book Appointment',
    ];

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 16),
      height: 52,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: replies.length,
        separatorBuilder: (_, __) => const SizedBox(width: 10),
        itemBuilder: (ctx, i) {
          return GestureDetector(
            onTap: () => _sendMessage(replies[i]),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(30),
                border: Border.all(color: AppColors.primary, width: 1.2),
              ),
              alignment: Alignment.center,
              child: Text(
                replies[i],
                style: const TextStyle(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w800,
                  color: AppColors.primary,
                ),
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildInputBar(bool isVi) {
    final bottomPad = MediaQuery.of(context).viewInsets.bottom + MediaQuery.of(context).padding.bottom + 12;

    return Container(
      color: context.card,
      padding: EdgeInsets.fromLTRB(16, 12, 16, bottomPad),
      child: Row(
        children: [
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                color: context.bg,
                borderRadius: BorderRadius.circular(24),
                border: Border.all(color: context.divider),
              ),
              padding: const EdgeInsets.symmetric(horizontal: 14),
              child: TextField(
                controller: _messageCtrl,
                style: TextStyle(color: context.textPrimary, fontSize: 14),
                decoration: InputDecoration(
                  border: InputBorder.none,
                  hintText: isVi ? 'Nhập tin nhắn...' : 'Type a message...',
                  hintStyle: TextStyle(color: context.textSecondary, fontSize: 14),
                ),
                onSubmitted: _sendMessage,
              ),
            ),
          ),
          const SizedBox(width: 10),
          GestureDetector(
            onTap: () => _sendMessage(_messageCtrl.text),
            child: Container(
              width: 44,
              height: 44,
              decoration: const BoxDecoration(
                color: AppColors.primary,
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.send_rounded,
                color: Colors.white,
                size: 20,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

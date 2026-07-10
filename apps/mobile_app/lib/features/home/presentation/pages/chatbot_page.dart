import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/home/data/chat_service.dart';
import 'package:mobile_app/features/home/data/models/chat_models.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';

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

  ChatSession({required this.id, required this.title});
}

class ChatbotPage extends StatefulWidget {
  const ChatbotPage({super.key});

  @override
  State<ChatbotPage> createState() => _ChatbotPageState();
}

class _ChatbotPageState extends State<ChatbotPage> {
  final GlobalKey<ScaffoldState> _scaffoldKey = GlobalKey<ScaffoldState>();
  final TextEditingController _messageCtrl = TextEditingController();
  final ChatService _chatService = ChatService();
  final List<ChatMessage> _messages = [];
  bool _isTyping = false;
  bool _isLoadingHistory = false;
  String? _conversationId;

  List<ChatSession> _historySessions = [];

  @override
  void initState() {
    super.initState();
    _addWelcomeMessage();
    _loadHistorySessions();
  }

  void _addWelcomeMessage() {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    _messages.add(
      ChatMessage(
        text: isVi
            ? 'Xin chào! Tôi là trợ lý AI của phòng khám. Tôi có thể giúp gì cho bạn?'
            : 'Hello! I am your Dental AI Assistant. How can I help you today?',
        isUser: false,
        timestamp: DateTime.now(),
      ),
    );
  }

  Future<void> _loadHistorySessions() async {
    setState(() => _isLoadingHistory = true);
    try {
      final conversations = await _chatService.listConversations();
      if (!mounted) return;
      setState(() {
        _historySessions = conversations
            .map((c) => ChatSession(id: c.id, title: c.preview))
            .toList();
      });
    } catch (_) {
      // Không tải được lịch sử — bỏ qua lặng lẽ, không chặn việc dùng chatbot.
    } finally {
      if (mounted) setState(() => _isLoadingHistory = false);
    }
  }

  @override
  void dispose() {
    _messageCtrl.dispose();
    super.dispose();
  }

  Future<void> _sendMessage(String text) async {
    if (text.trim().isEmpty || _isTyping) return;
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    setState(() {
      _messages.add(ChatMessage(text: text, isUser: true, timestamp: DateTime.now()));
      _isTyping = true;
    });
    _messageCtrl.clear();

    try {
      _conversationId ??= await _chatService.startConversation();
      final result = await _chatService.sendMessage(_conversationId!, text);

      if (!mounted) return;
      setState(() {
        _messages.add(ChatMessage(
          text: result.reply,
          isUser: false,
          timestamp: DateTime.now(),
          customWidget: result.suggestBooking
              ? _buildUrgentBookingCard(isVi, result.bookingHint)
              : null,
        ));
      });
      unawaited(_loadHistorySessions());
    } catch (e) {
      if (!mounted) return;
      final errorText = e is DioException
          ? ApiClient.errorMessage(e)
          : (isVi ? 'Đã xảy ra lỗi. Vui lòng thử lại.' : 'Something went wrong. Please try again.');
      setState(() {
        _messages.add(ChatMessage(text: errorText, isUser: false, timestamp: DateTime.now()));
      });
    } finally {
      if (mounted) setState(() => _isTyping = false);
    }
  }

  /// Điền sẵn màn hình đặt lịch từ những gì AI đã trích xuất được trong hội thoại
  /// (dịch vụ, ngày mong muốn, triệu chứng) thay vì mở một form trống — bệnh nhân vẫn
  /// tự chọn hồ sơ và xác nhận khung giờ với bác sĩ như bình thường.
  Future<void> _startBookingFromHint(ChatBookingHint hint) async {
    ServiceInfo? service;
    if (hint.serviceId != null) {
      try {
        final services = await BookingService().getActiveServices();
        ServiceModel? match;
        for (final s in services) {
          if (s.id == hint.serviceId) {
            match = s;
            break;
          }
        }
        if (match != null) {
          service = ServiceInfo(
            id: match.id,
            name: match.name,
            description: match.description,
            price: match.formattedPrice,
          );
        }
      } catch (_) {
        // Không lấy được thông tin dịch vụ — vẫn tiếp tục, người dùng tự chọn ở bước sau.
      }
    }

    DoctorInfo? doctor;
    TimeSlot? timeSlot;
    if (hint.dentistId != null && hint.preferredDate != null) {
      try {
        final doctors = await BookingService().getDoctorsWithSlots(hint.preferredDate!);
        ApiDoctorWithSlots? matchedDoctor;
        for (final d in doctors) {
          if (d.dentistId == hint.dentistId) {
            matchedDoctor = d;
            break;
          }
        }
        if (matchedDoctor != null) {
          final now = DateTime.now();
          final isToday = hint.preferredDate!.year == now.year &&
              hint.preferredDate!.month == now.month &&
              hint.preferredDate!.day == now.day;
          for (final slot in matchedDoctor.slots) {
            if (slot.isBooked) continue;
            if (isToday && _isSlotInPast(slot.range, now)) continue;
            doctor = matchedDoctor.toDoctorInfo();
            timeSlot = slot.toTimeSlot();
            break;
          }
        }
      } catch (_) {
        // Không lấy được lịch trống — vẫn tiếp tục, chỉ highlight bác sĩ gợi ý ở bước sau.
      }
    }

    final draft = BookingDraft(
      service: service,
      doctor: doctor,
      date: hint.preferredDate,
      timeSlot: timeSlot,
      symptoms: hint.notes,
      preferredDentistId: hint.dentistId,
    );

    if (!mounted) return;
    context.push(AppRoutes.bookingSelectPatient, extra: draft);
  }

  /// Slot có dạng "08:00 - 08:30" — coi là đã qua giờ nếu giờ bắt đầu sớm hơn hiện tại.
  bool _isSlotInPast(String range, DateTime now) {
    final startPart = range.split('-').first.trim();
    final parts = startPart.split(':');
    if (parts.length != 2) return false;
    final hour = int.tryParse(parts[0]);
    final minute = int.tryParse(parts[1]);
    if (hour == null || minute == null) return false;
    final slotTime = DateTime(now.year, now.month, now.day, hour, minute);
    return slotTime.isBefore(now);
  }

  Widget _buildUrgentBookingCard(bool isVi, ChatBookingHint hint) {
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
              onPressed: () => _startBookingFromHint(hint),
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

  Future<void> _loadSession(ChatSession session) async {
    Navigator.pop(context); // Close Drawer
    try {
      final messages = await _chatService.getConversation(session.id);
      if (!mounted) return;
      setState(() {
        _conversationId = session.id;
        _messages.clear();
        _addWelcomeMessage();
        _messages.addAll(messages.map((m) => ChatMessage(
              text: m.content,
              isUser: m.isUser,
              timestamp: m.createdAt,
            )));
      });
    } catch (_) {
      if (!mounted) return;
      final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isVi ? 'Không tải được cuộc trò chuyện này.' : 'Could not load this conversation.'),
      ));
    }
  }

  void _startNewConversation() {
    Navigator.pop(context); // Close Drawer
    setState(() {
      _conversationId = null;
      _messages.clear();
      _addWelcomeMessage();
    });
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
            ListTile(
              leading: const Icon(Icons.add_circle_outline_rounded, color: AppColors.primary, size: 22),
              title: Text(
                isVi ? 'Cuộc trò chuyện mới' : 'New conversation',
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: AppColors.primary,
                ),
              ),
              onTap: _startNewConversation,
            ),
            const Divider(height: 1),
            Expanded(
              child: _isLoadingHistory
                  ? const Center(child: CircularProgressIndicator())
                  : _historySessions.isEmpty
                      ? Center(
                          child: Text(
                            isVi ? 'Chưa có cuộc trò chuyện nào.' : 'No conversations yet.',
                            style: TextStyle(color: context.textSecondary, fontSize: 13),
                          ),
                        )
                      : ListView.builder(
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

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class ReminderItem {
  final String id;
  final String title;
  final String dosageVi;
  final String dosageEn;
  final String timeText;
  final String typeText; // MORNING, AFTERNOON, EVENING
  final IconData icon;
  final Color iconBg;
  String status; // 'completed', 'overdue', 'scheduled'

  ReminderItem({
    required this.id,
    required this.title,
    required this.dosageVi,
    required this.dosageEn,
    required this.timeText,
    required this.typeText,
    required this.icon,
    required this.iconBg,
    this.status = 'scheduled',
  });
}

class RemindersPage extends StatefulWidget {
  const RemindersPage({super.key});

  @override
  State<RemindersPage> createState() => _RemindersPageState();
}

class _RemindersPageState extends State<RemindersPage> {
  DateTime _selectedDate = DateTime(2024, 9, 13);

  final List<ReminderItem> _reminders = [
    ReminderItem(
      id: 'r1',
      title: 'Amoxicillin 500mg',
      dosageVi: 'Uống 1 viên - Sau bữa sáng',
      dosageEn: 'Take 1 pill - After breakfast',
      timeText: '08:30 AM',
      typeText: 'MORNING',
      icon: Icons.local_pharmacy_outlined,
      iconBg: const Color(0xFFFEE2E2),
      status: 'completed',
    ),
    ReminderItem(
      id: 'r2',
      title: 'Chlorhexidine Mouthwash',
      dosageVi: 'Súc miệng 10ml - Trong 30 giây',
      dosageEn: 'Rinse 10ml - 30 seconds',
      timeText: '01:30 PM',
      typeText: 'AFTERNOON',
      icon: Icons.local_activity_outlined,
      iconBg: const Color(0xFFFEF3C7),
      status: 'overdue',
    ),
    ReminderItem(
      id: 'r3',
      title: 'Ibuprofen 400mg',
      dosageVi: 'Uống 1 viên - Nếu đau nhức',
      dosageEn: 'Take 1 pill - If pain',
      timeText: '08:30 PM',
      typeText: 'EVENING',
      icon: Icons.local_pharmacy_outlined,
      iconBg: const Color(0xFFE0F2FE),
      status: 'scheduled',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Nhắc nhở y tế' : 'Medical Reminders',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.bold,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Month Header
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  isVi ? 'Tháng 9, 2024' : 'September 2024',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                    color: context.textPrimary,
                  ),
                ),
                Icon(Iconsax.calendar, color: context.textSecondary, size: 20),
              ],
            ),
            const SizedBox(height: 16),

            // Horizontal Day Selector
            _buildDaySelector(isVi),
            const SizedBox(height: 28),

            // Sections Morning, Afternoon, Evening
            _buildReminderSection('MORNING', isVi),
            const SizedBox(height: 20),
            _buildReminderSection('AFTERNOON', isVi),
            const SizedBox(height: 20),
            _buildReminderSection('EVENING', isVi),
            const SizedBox(height: 28),

            // Recovery Tip Card
            _buildRecoveryTipCard(isVi),
          ],
        ),
      ),
    );
  }

  Widget _buildDaySelector(bool isVi) {
    final days = [
      {'day': '11', 'weekVi': 'T2', 'weekEn': 'MON'},
      {'day': '12', 'weekVi': 'T3', 'weekEn': 'TUE'},
      {'day': '13', 'weekVi': 'T4', 'weekEn': 'WED'},
      {'day': '14', 'weekVi': 'T5', 'weekEn': 'THU'},
      {'day': '15', 'weekVi': 'T6', 'weekEn': 'FRI'},
      {'day': '16', 'weekVi': 'T7', 'weekEn': 'SAT'},
    ];

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: days.map((d) {
        final dayNum = int.parse(d['day']!);
        final isSelected = _selectedDate.day == dayNum;
        final weekText = isVi ? d['weekVi']! : d['weekEn']!;

        return GestureDetector(
          onTap: () {
            setState(() {
              _selectedDate = DateTime(2024, 9, dayNum);
            });
          },
          child: Container(
            width: 52,
            padding: const EdgeInsets.symmetric(vertical: 12),
            decoration: BoxDecoration(
              color: isSelected ? AppColors.primary : context.card,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: isSelected ? Colors.transparent : context.divider),
              boxShadow: isSelected
                  ? [
                      BoxShadow(
                        color: AppColors.primary.withValues(alpha: 0.25),
                        blurRadius: 10,
                        offset: const Offset(0, 4),
                      ),
                    ]
                  : null,
            ),
            child: Column(
              children: [
                Text(
                  weekText,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w900,
                    color: isSelected ? Colors.white.withValues(alpha: 0.9) : context.textSecondary,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  d['day']!,
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w900,
                    color: isSelected ? Colors.white : context.textPrimary,
                  ),
                ),
              ],
            ),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildReminderSection(String type, bool isVi) {
    final list = _reminders.where((r) => r.typeText == type).toList();
    if (list.isEmpty) return const SizedBox.shrink();

    String sectionTitle;
    if (type == 'MORNING') {
      sectionTitle = isVi ? 'BUỔI SÁNG - 08:30' : 'MORNING - 08:30 AM';
    } else if (type == 'AFTERNOON') {
      sectionTitle = isVi ? 'BUỔI CHIỀU - 13:30' : 'AFTERNOON - 01:30 PM';
    } else {
      sectionTitle = isVi ? 'BUỔI TỐI - 20:30' : 'EVENING - 08:30 PM';
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Container(
              width: 6,
              height: 6,
              decoration: const BoxDecoration(color: AppColors.primary, shape: BoxShape.circle),
            ),
            const SizedBox(width: 8),
            Text(
              sectionTitle.toUpperCase(),
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w900,
                color: context.textSecondary,
                letterSpacing: 0.5,
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        ...list.map((item) => _buildReminderCard(item, isVi)),
      ],
    );
  }

  Widget _buildReminderCard(ReminderItem item, bool isVi) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Icon Box
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: item.iconBg,
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(item.icon, color: AppColors.primary, size: 22),
              ),
              const SizedBox(width: 14),
              // Details
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            item.title,
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                              color: context.textPrimary,
                            ),
                          ),
                        ),
                        // Status Badge
                        _buildStatusBadge(item.status, isVi),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      isVi ? item.dosageVi : item.dosageEn,
                      style: TextStyle(
                        fontSize: 13,
                        color: context.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Divider(color: context.divider, height: 1),
          const SizedBox(height: 12),
          // Action buttons depending on status
          _buildActionButtons(item, isVi),
        ],
      ),
    );
  }

  Widget _buildStatusBadge(String status, bool isVi) {
    Color bg;
    Color text;
    String label;

    if (status == 'completed') {
      bg = const Color(0xFFDCFCE7);
      text = const Color(0xFF16A34A);
      label = isVi ? 'ĐÃ UỐNG' : 'TAKEN';
    } else if (status == 'overdue') {
      bg = const Color(0xFFFEF3C7);
      text = const Color(0xFFD97706);
      label = isVi ? 'QUÁ HẠN' : 'OVERDUE';
    } else {
      bg = context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9);
      text = context.textSecondary;
      label = isVi ? 'ĐÃ LÊN LỊCH' : 'SCHEDULED';
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 9,
          fontWeight: FontWeight.w900,
          color: text,
        ),
      ),
    );
  }

  Widget _buildActionButtons(ReminderItem item, bool isVi) {
    if (item.status == 'completed') {
      return Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.check_circle_rounded, color: Color(0xFF10B981), size: 16),
          const SizedBox(width: 6),
          Text(
            isVi ? 'Đã uống thuốc lúc ' : 'Taken at ',
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.bold,
              color: Color(0xFF10B981),
            ),
          ),
          Text(
            item.timeText,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w900,
              color: Color(0xFF10B981),
            ),
          ),
        ],
      );
    } else if (item.status == 'overdue') {
      return Row(
        children: [
          Expanded(
            child: OutlinedButton(
              onPressed: () {
                setState(() => item.status = 'completed');
              },
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: AppColors.primary, width: 1.5),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                foregroundColor: AppColors.primary,
              ),
              child: Text(
                isVi ? 'UỐNG LÚC 12:30' : 'TAKE AT 12:30 PM',
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 11),
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: TextButton(
              onPressed: () {
                setState(() => item.status = 'scheduled');
              },
              style: TextButton.styleFrom(
                foregroundColor: context.textSecondary,
              ),
              child: Text(
                isVi ? 'Bỏ qua' : 'Dismiss',
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
              ),
            ),
          ),
        ],
      );
    } else {
      return SizedBox(
        width: double.infinity,
        height: 38,
        child: ElevatedButton(
          onPressed: () {
            setState(() => item.status = 'completed');
          },
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: Colors.white,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
          child: Text(
            isVi ? 'ĐÁNH DẤU ĐÃ UỐNG' : 'MARK AS TAKEN',
            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 12),
          ),
        ),
      );
    }
  }

  Widget _buildRecoveryTipCard(bool isVi) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: context.isDark ? const Color(0xFF321E1E) : const Color(0xFFFFF1F2),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.isDark ? Colors.transparent : const Color(0xFFFECDD3)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: const BoxDecoration(
              color: AppColors.primary,
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.add_rounded,
              color: Colors.white,
              size: 18,
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  isVi ? 'Lời khuyên phục hồi' : 'Recovery Tip',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w800,
                    color: context.isDark ? Colors.white : const Color(0xFF9F1239),
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  isVi
                      ? 'Hãy cắn chặt miếng bông gòn trong miệng của bạn trong 30 phút đầu tiên để tạo cục máu đông và ngưng chảy máu hoàn toàn.'
                      : 'Keep the cotton pad in your mouth for the first 30 mins to facilitate blood clot and completely stop bleeding.',
                  style: TextStyle(
                    fontSize: 13,
                    height: 1.5,
                    color: context.isDark ? const Color(0xFFFDA4AF) : const Color(0xFFBE123C),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

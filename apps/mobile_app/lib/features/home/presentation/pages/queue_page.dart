import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class QueuePage extends StatefulWidget {
  const QueuePage({super.key});

  @override
  State<QueuePage> createState() => _QueuePageState();
}

class _QueuePageState extends State<QueuePage> {
  bool _isAway = false;
  final int _currentServing = 104;
  final int _userNumber = 108;

  void _toggleStatus() {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    setState(() {
      _isAway = !_isAway;
    });
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          _isAway 
              ? (isVi ? 'Đã chuyển trạng thái sang "Tạm vắng mặt".' : 'Status changed to "Absent".')
              : (isVi ? 'Đã sẵn sàng quay lại hàng chờ.' : 'Ready to return to queue.'),
        ),
        backgroundColor: _isAway ? Colors.orange : const Color(0xFF10B981),
        duration: const Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final brandRed = context.isDark ? const Color(0xFFDC2626) : const Color(0xFF8B1D2F);

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
          'DentalCare',
          style: TextStyle(
            color: brandRed,
            fontWeight: FontWeight.w900,
            fontSize: 22,
            letterSpacing: -0.5,
          ),
        ),
        centerTitle: false,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: CircleAvatar(
              radius: 18,
              backgroundColor: context.isDark ? Colors.grey[800] : const Color(0xFFF1F5F9),
              backgroundImage: const AssetImage('assets/images/bac_si_4.png'),
            ),
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.all(18.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Serving Panel Header
                  Center(
                    child: Column(
                      children: [
                        const SizedBox(height: 12),
                        Text(
                          (isVi ? 'SỐ THỨ TỰ ĐANG PHỤC VỤ' : 'CURRENT NUMBER BEING SERVED').toUpperCase(),
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w900,
                            color: context.textSecondary,
                            letterSpacing: 1.2,
                          ),
                        ),
                        const SizedBox(height: 16),
                        // Big Circular Badge
                        Container(
                          width: 140,
                          height: 140,
                          decoration: BoxDecoration(
                            color: context.isDark ? const Color(0xFF451A1A) : const Color(0xFFFFECEF),
                            shape: BoxShape.circle,
                            border: Border.all(
                              color: brandRed.withValues(alpha: 0.15),
                              width: 6,
                            ),
                          ),
                          child: Center(
                            child: Text(
                              '#$_currentServing',
                              style: TextStyle(
                                fontSize: 44,
                                fontWeight: FontWeight.w900,
                                color: brandRed,
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(height: 12),
                        Text(
                          isVi ? 'Phòng khám số 3 - BS. Sarah Williams' : 'Room 3 - Dr. Sarah Williams',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: context.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 28),

                  // Stats Info Cards (Side by side)
                  Row(
                    children: [
                      Expanded(
                        child: _buildStatCard(
                          icon: Iconsax.user_octagon,
                          label: isVi ? 'Số của bạn' : 'Your Number',
                          value: '#$_userNumber',
                          subtitle: isVi ? 'Lượt tiếp theo' : 'Average wait',
                          isHighlighted: true,
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: _buildStatCard(
                          icon: Iconsax.clock,
                          label: isVi ? 'Thời gian chờ' : 'Est. Wait Time',
                          value: '15 min',
                          subtitle: isVi ? 'Đang đúng tiến độ' : 'Status: on track',
                          isHighlighted: false,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 28),

                  // Live Queue Track Header
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        isVi ? 'Tiến trình Hàng chờ' : 'Live Queue Track',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w900,
                          color: context.textPrimary,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFEE2E2),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: const Text(
                          'LIVE',
                          style: TextStyle(
                            color: Color(0xFFDC2626),
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // Timeline Widget
                  _buildQueueTimeline(isVi),
                  const SizedBox(height: 24),

                  // Action Button
                  _buildActionRow(isVi),
                ],
              ),
            ),

            // Bottom Joint card matching the design mockup
            _buildJoinFooter(isVi),
          ],
        ),
      ),
    );
  }

  Widget _buildStatCard({
    required IconData icon,
    required String label,
    required String value,
    required String subtitle,
    required bool isHighlighted,
  }) {
    final cardBg = context.card;
    final brandRed = context.isDark ? const Color(0xFFDC2626) : const Color(0xFF8B1D2F);

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardBg,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isHighlighted ? brandRed.withValues(alpha: 0.3) : context.divider,
          width: 1.5,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            icon,
            color: isHighlighted ? brandRed : context.textSecondary,
            size: 20,
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.w900,
              color: isHighlighted ? brandRed : context.textPrimary,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w800,
              color: context.textPrimary,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            subtitle,
            style: TextStyle(
              fontSize: 11,
              color: context.textSecondary,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildQueueTimeline(bool isVi) {
    final List<Map<String, dynamic>> steps = [
      {'number': '103', 'status': 'completed', 'label': isVi ? 'Đã khám xong' : 'Completed'},
      {'number': '104', 'status': 'serving', 'label': isVi ? 'Đang khám - BS. Williams' : 'In room 3 - Dr. Williams'},
      {'number': '105', 'status': 'waiting', 'label': isVi ? 'Đang đợi' : 'Waiting'},
      {'number': '106', 'status': 'waiting', 'label': isVi ? 'Đang đợi' : 'Waiting'},
      {'number': '108', 'status': 'yours', 'label': _isAway ? (isVi ? 'Tạm vắng mặt' : 'Absent') : (isVi ? 'Lượt của bạn (Dự kiến 10:15)' : 'Estimated 10:15 AM')},
      {'number': '109', 'status': 'upcoming', 'label': isVi ? 'Đang đợi' : 'Upcoming'},
    ];

    final brandRed = context.isDark ? const Color(0xFFDC2626) : const Color(0xFF8B1D2F);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: context.divider),
      ),
      child: ListView.builder(
        shrinkWrap: true,
        physics: const NeverScrollableScrollPhysics(),
        itemCount: steps.length,
        itemBuilder: (context, i) {
          final step = steps[i];
          final isLast = i == steps.length - 1;

          Color dotColor;
          Widget dotIcon;
          bool isHighlighted = false;

          switch (step['status']) {
            case 'completed':
              dotColor = const Color(0xFFCBD5E1);
              dotIcon = const Icon(Icons.check, color: Colors.white, size: 10);
              break;
            case 'serving':
              dotColor = brandRed;
              dotIcon = Container(
                width: 6,
                height: 6,
                decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
              );
              break;
            case 'yours':
              dotColor = _isAway ? Colors.orange : brandRed;
              dotIcon = const Icon(Iconsax.user, color: Colors.white, size: 10);
              isHighlighted = true;
              break;
            case 'upcoming':
            case 'waiting':
            default:
              dotColor = const Color(0xFFE2E8F0);
              dotIcon = const SizedBox();
              break;
          }

          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Indicator Line
              Column(
                children: [
                  Container(
                    width: 20,
                    height: 20,
                    decoration: BoxDecoration(
                      color: dotColor,
                      shape: BoxShape.circle,
                    ),
                    child: Center(child: dotIcon),
                  ),
                  if (!isLast)
                    Container(
                      width: 2,
                      height: 38,
                      color: context.divider,
                    ),
                ],
              ),
              const SizedBox(width: 14),
              // Timeline details card
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                    decoration: BoxDecoration(
                      color: isHighlighted
                          ? (context.isDark ? const Color(0xFF451A1A) : const Color(0xFFFFECEF))
                          : Colors.transparent,
                      borderRadius: BorderRadius.circular(12),
                      border: isHighlighted
                          ? Border.all(color: _isAway ? Colors.orange : brandRed, width: 1.5)
                          : null,
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              isVi ? 'Hàng chờ #${step['number']}' : '#${step['number']} Queue',
                              style: TextStyle(
                                fontSize: 13.5,
                                fontWeight: isHighlighted ? FontWeight.w900 : FontWeight.bold,
                                color: isHighlighted ? brandRed : context.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              step['label'] as String,
                              style: TextStyle(
                                fontSize: 12,
                                color: isHighlighted ? brandRed.withValues(alpha: 0.8) : context.textSecondary,
                              ),
                            ),
                          ],
                        ),
                        if (isHighlighted)
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: _isAway ? Colors.orange : brandRed,
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              _isAway ? (isVi ? 'Tạm vắng' : 'ABSENT') : (isVi ? 'Lượt của bạn' : 'YOURS'),
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 8,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildActionRow(bool isVi) {
    return SizedBox(
      width: double.infinity,
      height: 48,
      child: OutlinedButton.icon(
        onPressed: _toggleStatus,
        style: OutlinedButton.styleFrom(
          foregroundColor: _isAway ? AppColors.primary : Colors.orange,
          side: BorderSide(color: _isAway ? AppColors.primary : Colors.orange, width: 1.5),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
        icon: Icon(_isAway ? Iconsax.play : Iconsax.pause),
        label: Text(
          _isAway 
              ? (isVi ? 'Quay lại hàng chờ' : 'Return to Queue')
              : (isVi ? 'Báo vắng mặt tạm thời' : 'Mark Absent Temporary'),
          style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13),
        ),
      ),
    );
  }

  Widget _buildJoinFooter(bool isVi) {
    final brandRed = context.isDark ? const Color(0xFFDC2626) : const Color(0xFF8B1D2F);

    return Container(
      color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
      child: SafeArea(
        top: false,
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    isVi ? 'Cấp thành viên: Vàng' : 'Member Level: Gold',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                      color: context.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    isVi ? 'Được tự động xếp lịch ưu tiên' : 'Auto-prioritized checking',
                    style: TextStyle(
                      fontSize: 12,
                      color: context.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
            ElevatedButton(
              onPressed: () {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(isVi ? 'Bạn đã tham gia hàng chờ!' : 'Joined queue successfully!'),
                    backgroundColor: brandRed,
                    behavior: SnackBarBehavior.floating,
                  ),
                );
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: brandRed,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              ),
              child: Text(
                isVi ? 'THAM GIA' : 'JOIN',
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 13),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

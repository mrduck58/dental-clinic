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
  int _currentServing = 104;

  void _toggleStatus() {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    setState(() {
      _isAway = !_isAway;
    });
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          _isAway 
              ? (isVi ? 'Đã chuyển trạng thái sang "Tạm vắng mặt". Số của bạn sẽ tạm thời được lùi lại.' : 'Status changed to "Absent". Your turn will be temporarily delayed.')
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

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Iconsax.arrow_left, color: AppColors.textPrimary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          context.l10n('queue_title'),
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
      ),
      body: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(18.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Serving Panel Card
              _buildServingPanel(isVi),
              SizedBox(height: 24),

              // Room info & Your status
              _buildInfoGrid(isVi),
              SizedBox(height: 28),

              // Timeline Header
              Text(
                isVi ? 'Tiến độ hàng chờ thực tế' : 'Live Queue Progress',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: context.textPrimary,
                ),
              ),
              SizedBox(height: 16),

              // Timeline Widget
              _buildQueueTimeline(isVi),
              SizedBox(height: 32),

              // Break Button
              _buildBreakButton(isVi),
              SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildServingPanel(bool isVi) {
    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
        border: Border.all(color: context.divider),
      ),
      padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 20),
      child: Column(
        children: [
          Text(
            isVi ? 'SỐ THỨ TỰ ĐANG PHỤC VỤ' : 'CURRENT SERVING NUMBER',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w900,
              color: context.textSecondary,
              letterSpacing: 1.2,
            ),
          ),
          SizedBox(height: 16),
          // Circular Badge
          Container(
            width: 110,
            height: 110,
            decoration: BoxDecoration(
              color: context.primaryLight,
              shape: BoxShape.circle,
              border: Border.all(color: AppColors.primary.withValues(alpha: 0.2), width: 3),
              boxShadow: [
                BoxShadow(
                  color: AppColors.primary.withValues(alpha: 0.12),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Center(
              child: Text(
                '#$_currentServing',
                style: TextStyle(
                  fontSize: 32,
                  fontWeight: FontWeight.w900,
                  color: AppColors.primary,
                ),
              ),
            ),
          ),
          SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 8,
                height: 8,
                decoration: const BoxDecoration(
                  color: Color(0xFF10B981),
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                isVi ? 'Đang cập nhật trực tiếp...' : 'Live updating...',
                style: const TextStyle(
                  fontSize: 13,
                  color: Color(0xFF10B981),
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildInfoGrid(bool isVi) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceAround,
        children: [
          _buildInfoItem(Iconsax.home_trend_up, isVi ? 'Phòng khám' : 'Clinic Room', isVi ? 'Phòng 03\n(Tầng 2)' : 'Room 03\n(2nd Floor)'),
          Container(width: 1, height: 40, color: context.divider),
          _buildInfoItem(Iconsax.user_octagon, context.l10n('your_number'), '#108', isPrimaryText: true),
          Container(width: 1, height: 40, color: context.divider),
          _buildInfoItem(Iconsax.clock, isVi ? 'Chờ dự kiến' : 'Est. Wait', isVi ? '~15 phút' : '~15 mins'),
        ],
      ),
    );
  }

  Widget _buildInfoItem(IconData icon, String label, String value, {bool isPrimaryText = false}) {
    return Column(
      children: [
        Icon(icon, color: isPrimaryText ? AppColors.primary : context.textSecondary, size: 22),
        SizedBox(height: 6),
        Text(
          label,
          style: TextStyle(fontSize: 11, color: context.textSecondary),
        ),
        SizedBox(height: 4),
        Text(
          value,
          textAlign: TextAlign.center,
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
            color: isPrimaryText ? AppColors.primary : context.textPrimary,
          ),
        ),
      ],
    );
  }

  Widget _buildQueueTimeline(bool isVi) {
    final List<Map<String, dynamic>> steps = [
      {'number': '103', 'status': 'completed', 'label': isVi ? 'Đã phục vụ xong' : 'Served'},
      {'number': '104', 'status': 'serving', 'label': isVi ? 'Đang trong phòng khám' : 'In Consultation'},
      {'number': '105', 'status': 'waiting', 'label': isVi ? 'Đang đợi ở sảnh' : 'Waiting in lobby'},
      {'number': '106', 'status': 'waiting', 'label': isVi ? 'Đang đợi ở sảnh' : 'Waiting in lobby'},
      {'number': '107', 'status': 'waiting', 'label': isVi ? 'Đang đợi ở sảnh' : 'Waiting in lobby'},
      {'number': '108', 'status': 'yours', 'label': _isAway ? (isVi ? 'Tạm vắng mặt' : 'Temporary Absent') : (isVi ? 'Vị trí của bạn (Kế tiếp)' : 'Your Position (Next)')},
      {'number': '109', 'status': 'upcoming', 'label': isVi ? 'Đang đợi ở sảnh' : 'Waiting in lobby'},
    ];

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

          Color indicatorColor;
          Widget indicatorIcon;
          bool highlight = false;

          switch (step['status']) {
            case 'completed':
              indicatorColor = const Color(0xFF10B981);
              indicatorIcon = const Icon(Icons.check, color: Colors.white, size: 14);
              break;
            case 'serving':
              indicatorColor = AppColors.primary;
              indicatorIcon = Container(
                width: 8,
                height: 8,
                decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
              );
              highlight = true;
              break;
            case 'yours':
              indicatorColor = _isAway ? Colors.orange : AppColors.primary;
              indicatorIcon = const Icon(Iconsax.notification, color: Colors.white, size: 13);
              highlight = true;
              break;
            case 'upcoming':
            case 'waiting':
            default:
              indicatorColor = const Color(0xFFCBD5E1);
              indicatorIcon = const SizedBox();
              break;
          }

          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Left Column: Dot & Line
              Column(
                children: [
                  Container(
                    width: 26,
                    height: 26,
                    decoration: BoxDecoration(
                      color: indicatorColor,
                      shape: BoxShape.circle,
                      boxShadow: highlight
                          ? [
                              BoxShadow(
                                color: indicatorColor.withValues(alpha: 0.3),
                                blurRadius: 6,
                                spreadRadius: 1,
                              )
                            ]
                          : null,
                    ),
                    child: Center(child: indicatorIcon),
                  ),
                  if (!isLast)
                    Container(
                      width: 2,
                      height: 40,
                      color: context.divider,
                    ),
                ],
              ),
              const SizedBox(width: 16),
              // Right Column: Details
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.only(top: 2),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                    decoration: BoxDecoration(
                      color: step['status'] == 'yours'
                          ? (_isAway ? Colors.orange.withValues(alpha: 0.08) : context.primaryLight)
                          : (step['status'] == 'serving' ? (context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9)) : Colors.transparent),
                      borderRadius: BorderRadius.circular(12),
                      border: step['status'] == 'yours'
                          ? Border.all(color: _isAway ? Colors.orange : AppColors.primary, width: 1.5)
                          : null,
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              isVi ? 'Số thứ tự #${step['number']}' : 'Queue No. #${step['number']}',
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: step['status'] == 'yours' || step['status'] == 'serving'
                                    ? FontWeight.bold
                                    : FontWeight.w600,
                                color: step['status'] == 'yours' && !_isAway
                                    ? AppColors.primary
                                    : context.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 3),
                            Text(
                              step['label'] as String,
                              style: TextStyle(
                                fontSize: 12,
                                color: step['status'] == 'yours' && !_isAway
                                    ? AppColors.primary.withValues(alpha: 0.8)
                                    : context.textSecondary,
                              ),
                            ),
                          ],
                        ),
                        if (step['status'] == 'yours')
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: _isAway ? Colors.orange : AppColors.primary,
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Text(
                              _isAway ? (isVi ? 'Tạm vắng' : 'Absent') : (isVi ? 'Số của bạn' : 'Your Turn'),
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10,
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

  Widget _buildBreakButton(bool isVi) {
    return SizedBox(
      width: double.infinity,
      height: 52,
      child: OutlinedButton.icon(
        onPressed: _toggleStatus,
        style: OutlinedButton.styleFrom(
          foregroundColor: _isAway ? AppColors.primary : Colors.orange,
          side: BorderSide(color: _isAway ? AppColors.primary : Colors.orange, width: 1.5),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        ),
        icon: Icon(_isAway ? Iconsax.play : Iconsax.pause),
        label: Text(
          _isAway 
              ? (isVi ? 'Quay lại hàng chờ (Sẵn sàng)' : 'Return to queue (Ready)')
              : (isVi ? 'Bạn cần tạm nghỉ? Báo vắng mặt tạm thời' : 'Need a break? Mark temporary absence'),
          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
        ),
      ),
    );
  }
}

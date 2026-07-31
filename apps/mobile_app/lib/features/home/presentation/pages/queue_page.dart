import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';
import 'package:mobile_app/features/home/data/queue_service.dart';
import 'package:mobile_app/features/home/data/models/patient_queue_model.dart';

class QueuePage extends StatefulWidget {
  const QueuePage({super.key});

  @override
  State<QueuePage> createState() => _QueuePageState();
}

class _QueuePageState extends State<QueuePage> {
  final _auth = AuthService();
  final _familyService = FamilyService();
  final _queueService = QueueService();

  bool _isLoading = true;
  bool _isLoadingQueue = false;
  UserProfile? _ownerProfile;
  List<FamilyMember> _familyMembers = [];
  String? _selectedMemberId; // null means 'Tôi' (owner profile)
  PatientQueueResponse? _queueResponse;
  bool _isAway = false;

  @override
  void initState() {
    super.initState();
    _initData();
  }

  Future<void> _initData() async {
    setState(() {
      _isLoading = true;
    });
    try {
      _ownerProfile = await _auth.getMyProfile();
    } catch (e) {
      debugPrint("QueuePage: Failed to load user profile: $e");
    }

    try {
      await _familyService.loadFromServer();
      _familyMembers = _familyService.getMembers();
    } catch (e) {
      debugPrint("QueuePage: Failed to load family members: $e");
      _familyMembers = _familyService.getMembers();
    }

    await _fetchQueue();

    setState(() {
      _isLoading = false;
    });
  }

  Future<void> _fetchQueue() async {
    setState(() {
      _isLoadingQueue = true;
    });
    try {
      final res = await _queueService.getPatientQueue(patientId: _selectedMemberId);
      setState(() {
        _queueResponse = res;
        _isAway = false; // Reset local status
      });
    } catch (e) {
      debugPrint("QueuePage: Failed to fetch queue: $e");
      setState(() {
        _queueResponse = null;
      });
    } finally {
      setState(() {
        _isLoadingQueue = false;
      });
    }
  }

  void _toggleStatus() {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    setState(() {
      _isAway = !_isAway;
    });
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          _isAway 
              ? (isVi ? 'Đã báo vắng mặt tạm thời với lễ tân. Số của bạn sẽ tạm thời được lùi lại.' : 'Status changed to "Absent". Your turn will be temporarily delayed.')
              : (isVi ? 'Đã báo sẵn sàng quay lại hàng chờ.' : 'Ready to return to queue.'),
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
          icon: Icon(Iconsax.arrow_left, color: context.textPrimary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          context.l10n('queue_title'),
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: context.textPrimary,
          ),
        ),
        actions: [
          if (!_isLoading)
            IconButton(
              icon: Icon(Iconsax.refresh, color: context.textPrimary),
              onPressed: _fetchQueue,
            ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _fetchQueue,
              child: SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                child: Padding(
                  padding: const EdgeInsets.all(18.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Member selector row
                      _buildMemberSelector(isVi),
                      const SizedBox(height: 20),

                      if (_isLoadingQueue)
                        const Padding(
                          padding: EdgeInsets.symmetric(vertical: 40),
                          child: Center(child: CircularProgressIndicator()),
                        )
                      else if (_queueResponse == null || !_queueResponse!.hasActiveQueue)
                        _buildEmptyState(isVi)
                      else ...[
                        // Serving Panel Card
                        _buildServingPanel(isVi),
                        const SizedBox(height: 24),

                        // Room info & Your status
                        _buildInfoGrid(isVi),
                        const SizedBox(height: 28),

                        // Timeline Header
                        Text(
                          isVi ? 'Tiến độ hàng chờ thực tế' : 'Live Queue Progress',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: context.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 16),

                        // Timeline Widget
                        _buildQueueTimeline(isVi),
                        const SizedBox(height: 32),

                        // Break Button (Hide if currently serving in consultation room)
                        if (_queueResponse!.status != 'InProgress') ...[
                          _buildBreakButton(isVi),
                          const SizedBox(height: 24),
                        ],
                      ],
                    ],
                  ),
                ),
              ),
            ),
    );
  }

  Widget _buildMemberSelector(bool isVi) {
    if (_ownerProfile == null) return const SizedBox();

    final List<Map<String, dynamic>> items = [
      {
        'id': null,
        'name': isVi ? 'Tôi' : 'Myself',
        'avatar': _ownerProfile?.profilePictureUrl,
        'relationship': isVi ? 'Tài khoản chính' : 'Primary Account',
      },
      ..._familyMembers.map((m) => {
            'id': m.id,
            'name': m.fullName,
            'avatar': m.profilePictureUrl,
            'relationship': m.relationship,
          }),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Text(
            isVi ? 'Chọn thành viên theo dõi:' : 'Select member to track:',
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.bold,
              color: context.textSecondary,
            ),
          ),
        ),
        const SizedBox(height: 12),
        SizedBox(
          height: 90,
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            itemCount: items.length,
            itemBuilder: (context, i) {
              final item = items[i];
              final isSelected = _selectedMemberId == item['id'];
              final String name = item['name'] as String;
              final String? avatar = item['avatar'] as String?;

              final nameParts = name.split(' ');
              final shortName = nameParts.length > 2
                  ? '${nameParts[nameParts.length - 2]} ${nameParts.last}'
                  : name;

              return Padding(
                padding: const EdgeInsets.only(right: 16),
                child: GestureDetector(
                  onTap: () {
                    if (_selectedMemberId != item['id']) {
                      setState(() {
                        _selectedMemberId = item['id'] as String?;
                      });
                      _fetchQueue();
                    }
                  },
                  child: Column(
                    children: [
                      Stack(
                        children: [
                          Container(
                            width: 54,
                            height: 54,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              border: Border.all(
                                color: isSelected ? AppColors.primary : context.divider,
                                width: isSelected ? 3 : 1.5,
                              ),
                              boxShadow: isSelected
                                  ? [
                                      BoxShadow(
                                        color: AppColors.primary.withValues(alpha: 0.25),
                                        blurRadius: 8,
                                        spreadRadius: 1,
                                      )
                                    ]
                                  : null,
                            ),
                            child: ClipOval(
                              child: avatar != null && avatar.isNotEmpty
                                  ? (avatar.startsWith('assets/')
                                      ? Image.asset(avatar, fit: BoxFit.cover)
                                      : Image.network(
                                          avatar.startsWith('http')
                                              ? avatar
                                              : '${ApiConstants.baseUrl.replaceAll('/api', '')}$avatar',
                                          fit: BoxFit.cover,
                                          errorBuilder: (_, __, ___) => _buildFallbackAvatar(isSelected),
                                        ))
                                  : _buildFallbackAvatar(isSelected),
                            ),
                          ),
                          if (isSelected)
                            Positioned(
                              right: 0,
                              bottom: 0,
                              child: Container(
                                padding: const EdgeInsets.all(2),
                                decoration: const BoxDecoration(
                                  color: AppColors.primary,
                                  shape: BoxShape.circle,
                                ),
                                child: const Icon(
                                  Icons.check,
                                  color: Colors.white,
                                  size: 10,
                                ),
                              ),
                            ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      SizedBox(
                        width: 70,
                        child: Text(
                          shortName,
                          textAlign: TextAlign.center,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                            color: isSelected ? AppColors.primary : context.textPrimary,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _buildFallbackAvatar(bool isSelected) {
    return Container(
      color: isSelected ? context.primaryLight : context.divider,
      child: Icon(
        Iconsax.user,
        color: isSelected ? AppColors.primary : context.textSecondary,
        size: 24,
      ),
    );
  }

  Widget _buildEmptyState(bool isVi) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 24),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 80,
            height: 80,
            decoration: BoxDecoration(
              color: context.divider.withValues(alpha: 0.5),
              shape: BoxShape.circle,
            ),
            child: Icon(
               Iconsax.calendar_tick,
               size: 36,
               color: context.textSecondary,
            ),
          ),
          const SizedBox(height: 24),
          Text(
            isVi ? 'Không có lượt khám hôm nay' : 'No active visits today',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: context.textPrimary,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            isVi
                ? 'Thành viên này hiện không có lịch khám đang chờ hoặc chưa được check-in tại quầy.'
                : 'This member does not have a waiting or checked-in appointment today.',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 13,
              color: context.textSecondary,
              height: 1.4,
            ),
          ),
          const SizedBox(height: 24),
          ElevatedButton.icon(
            onPressed: () {
              context.pop();
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            ),
            icon: const Icon(Iconsax.add, size: 18),
            label: Text(
              isVi ? 'Đặt lịch ngay' : 'Book Now',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildServingPanel(bool isVi) {
    final curServing = _queueResponse?.currentServingNumber;
    final displayServing = curServing != null ? '#$curServing' : (isVi ? 'Chưa bắt đầu' : 'Not started');

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
          const SizedBox(height: 16),
          Container(
            width: 130,
            height: 110,
            decoration: BoxDecoration(
              color: context.primaryLight,
              borderRadius: BorderRadius.circular(20),
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
                displayServing,
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: curServing != null ? 32 : 18,
                  fontWeight: FontWeight.w900,
                  color: AppColors.primary,
                ),
              ),
            ),
          ),
          const SizedBox(height: 16),
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
    final roomName = _queueResponse?.roomName ?? (isVi ? 'Phòng khám chung' : 'General Room');
    final yourNum = '#${_queueResponse?.queueNumber ?? 0}';
    
    String waitDisplay = '';
    if (_queueResponse?.status == 'InProgress') {
      waitDisplay = isVi ? 'Đang khám' : 'In Consultation';
    } else {
      final waitMins = _queueResponse?.estWaitMinutes ?? 0;
      waitDisplay = waitMins == 0
          ? (isVi ? 'Kế tiếp' : 'Next')
          : (isVi ? '~$waitMins phút' : '~$waitMins mins');
    }

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
          Expanded(
            child: _buildInfoItem(
              Iconsax.home_trend_up,
              isVi ? 'Phòng khám' : 'Clinic Room',
              roomName,
            ),
          ),
          Container(width: 1, height: 40, color: context.divider),
          Expanded(
            child: _buildInfoItem(
              Iconsax.user_octagon,
              context.l10n('your_number'),
              yourNum,
              isPrimaryText: true,
            ),
          ),
          Container(width: 1, height: 40, color: context.divider),
          Expanded(
            child: _buildInfoItem(
              Iconsax.clock,
              isVi ? 'Chờ dự kiến' : 'Est. Wait',
              waitDisplay,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoItem(IconData icon, String label, String value, {bool isPrimaryText = false}) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, color: isPrimaryText ? AppColors.primary : context.textSecondary, size: 22),
        const SizedBox(height: 6),
        Text(
          label,
          style: TextStyle(fontSize: 11, color: context.textSecondary),
        ),
        const SizedBox(height: 4),
        Text(
          value,
          textAlign: TextAlign.center,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.bold,
            color: isPrimaryText ? AppColors.primary : context.textPrimary,
          ),
        ),
      ],
    );
  }

  Widget _buildQueueTimeline(bool isVi) {
    final steps = _queueResponse?.steps ?? [];

    if (steps.isEmpty) {
      return Container(
        padding: const EdgeInsets.all(24),
        alignment: Alignment.center,
        child: Text(
          isVi ? 'Chưa có bệnh nhân nào trong phòng khám.' : 'No patients in the clinic yet.',
          style: TextStyle(color: context.textSecondary),
        ),
      );
    }

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

          final statusLower = step.status.toLowerCase();
          final isYours = step.isYours;

          if (statusLower == 'inprogress') {
            indicatorColor = AppColors.primary;
            indicatorIcon = Container(
              width: 8,
              height: 8,
              decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
            );
            highlight = true;
          } else if (isYours) {
            indicatorColor = _isAway ? Colors.orange : AppColors.primary;
            indicatorIcon = const Icon(Iconsax.notification, color: Colors.white, size: 13);
            highlight = true;
          } else {
            indicatorColor = const Color(0xFFCBD5E1);
            indicatorIcon = const SizedBox();
          }

          String statusLabel = '';
          if (statusLower == 'inprogress') {
            statusLabel = isVi ? 'Đang trong phòng khám' : 'In Consultation';
          } else if (isYours) {
            statusLabel = _isAway
                ? (isVi ? 'Tạm vắng mặt' : 'Temporary Absent')
                : (isVi ? 'Vị trí của bạn' : 'Your Position');
          } else {
            statusLabel = isVi ? 'Đang đợi ở sảnh' : 'Waiting in lobby';
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
                      color: isYours
                          ? (_isAway ? Colors.orange.withValues(alpha: 0.08) : context.primaryLight)
                          : (statusLower == 'inprogress' ? (context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9)) : Colors.transparent),
                      borderRadius: BorderRadius.circular(12),
                      border: isYours
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
                              isVi ? 'Số thứ tự #${step.queueNumber}' : 'Queue No. #${step.queueNumber}',
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: isYours || statusLower == 'inprogress'
                                    ? FontWeight.bold
                                    : FontWeight.w600,
                                color: isYours && !_isAway
                                    ? AppColors.primary
                                    : context.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 3),
                            Text(
                              statusLabel,
                              style: TextStyle(
                                fontSize: 12,
                                color: isYours && !_isAway
                                    ? AppColors.primary.withValues(alpha: 0.8)
                                    : context.textSecondary,
                              ),
                            ),
                          ],
                        ),
                        if (isYours)
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

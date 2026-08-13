import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_banner.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_header.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_search_bar.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_section_header.dart';
import 'package:mobile_app/features/home/presentation/widgets/news_card.dart';
import 'package:mobile_app/features/home/presentation/widgets/service_card.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  final _homeService = HomeService();
  final _auth = AuthService();

  List<ServiceModel> _services = [];
  List<DoctorModel> _dentists = [];
  List<PostModel> _posts = [];
  String _userName = '';
  String? _avatarUrl;
  MyAppointmentItem? _upcomingAppointment;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadAll();
  }

  Future<void> _loadAll() async {
    setState(() => _isLoading = true);
    final token = await _auth.getToken();
    final results = await Future.wait<dynamic>([
      _homeService.getServices().catchError((_) => <ServiceModel>[]),
      _homeService.getPosts().catchError((_) => <PostModel>[]),
      _auth.getUserName(),
      _auth.getUserAvatar(),
      token != null
          ? BookingService().getMyAppointments().catchError((_) => <MyAppointmentItem>[])
          : Future.value(<MyAppointmentItem>[]),
      _homeService.getDentists().catchError((_) => <DoctorModel>[]),
    ]);
    if (!mounted) return;

    final appointments = List<MyAppointmentItem>.from(results[4] as List);
    MyAppointmentItem? nearestUpcoming;
    if (appointments.isNotEmpty) {
      final upcoming = appointments.where((a) {
        final statusLower = a.status.toLowerCase();
        return statusLower == 'confirmed' ||
            statusLower == 'pending' ||
            statusLower == 'checkedin' ||
            statusLower == 'inprogress';
      }).toList();

      if (upcoming.isNotEmpty) {
        upcoming.sort((a, b) => a.parsedDate.compareTo(b.parsedDate));
        nearestUpcoming = upcoming.first;
      }
    }

    setState(() {
      _services = List<ServiceModel>.from(results[0] as List);
      _posts = List<PostModel>.from(results[1] as List);
      _userName = (results[2] as String?) ?? '';
      _avatarUrl = results[3] as String?;
      _upcomingAppointment = nearestUpcoming;
      _dentists = List<DoctorModel>.from(results[5] as List);
      _isLoading = false;
    });

    try {
      final p = await _auth.getMyProfile();
      if (mounted) {
        setState(() {
          _userName = p.fullName;
          _avatarUrl = p.profilePictureUrl;
        });
        _auth.saveUserName(p.fullName);
        if (p.profilePictureUrl != null) {
          _auth.saveUserAvatar(p.profilePictureUrl!);
        }
      }
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final bottomPad = MediaQuery.of(context).padding.bottom + 16;
    return ColoredBox(
      color: context.bg,
      child: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(child: HomeHeader(userName: _userName, avatarUrl: _avatarUrl)),
          const SliverToBoxAdapter(child: HomeSearchBar()),
          SliverPadding(
            padding: EdgeInsets.fromLTRB(18, 0, 18, bottomPad),
            sliver: SliverList(
              delegate: SliverChildListDelegate([
                const HomeBanner(),
                const SizedBox(height: 26),
                const _QuickAccessPanel(),
                const SizedBox(height: 26),

                // Lịch hẹn sắp tới
                HomeSectionHeader(
                  title: context.l10n('upcoming_appointment'),
                  onSeeAll: () => context.go(AppRoutes.appointments),
                ),
                const SizedBox(height: 12),
                _isLoading
                    ? const _LoadingRow()
                    : (_upcomingAppointment == null
                        ? const _NoAppointmentCard()
                        : _UpcomingAppointmentCard(
                            item: _upcomingAppointment!,
                            onRefresh: _loadAll,
                          )),
                const SizedBox(height: 26),

                // Đội ngũ nha sĩ (Hiện 5 nha sĩ)
                HomeSectionHeader(
                  title: isVi ? 'Đội ngũ nha sĩ' : 'Our Dentists',
                  onSeeAll: () => context.push(AppRoutes.dentistsList),
                ),
                const SizedBox(height: 14),
                _isLoading
                    ? const _LoadingColumn()
                    : _dentists.isEmpty
                        ? _EmptySection(message: isVi ? 'Chưa có thông tin nha sĩ.' : 'No dentists available.')
                        : SizedBox(
                            height: 135,
                            child: ListView.separated(
                              scrollDirection: Axis.horizontal,
                              clipBehavior: Clip.none,
                              itemCount: _dentists.take(5).length,
                              separatorBuilder: (_, __) => const SizedBox(width: 12),
                              itemBuilder: (context, i) {
                                final doc = _dentists[i];
                                final specialty = doc.specialty ?? (isVi ? 'Nha sĩ tổng quát' : 'General Dentist');
                                return GestureDetector(
                                  onTap: () => context.push(AppRoutes.dentistProfile, extra: doc),
                                  child: Container(
                                    width: 220,
                                    padding: const EdgeInsets.all(12),
                                    decoration: BoxDecoration(
                                      color: context.card,
                                      borderRadius: BorderRadius.circular(18),
                                      border: Border.all(color: context.divider),
                                      boxShadow: [
                                        BoxShadow(
                                          color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                                          blurRadius: 10,
                                          offset: const Offset(0, 4),
                                        ),
                                      ],
                                    ),
                                    child: Row(
                                      children: [
                                        ClipRRect(
                                          borderRadius: BorderRadius.circular(14),
                                          child: Container(
                                            width: 60,
                                            height: 80,
                                            color: context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight,
                                            child: doc.profilePictureUrl != null && doc.profilePictureUrl!.isNotEmpty
                                                ? Image.network(
                                                    ApiConstants.resolveAssetUrl(doc.profilePictureUrl)!,
                                                    fit: BoxFit.cover,
                                                    errorBuilder: (_, __, ___) => Icon(Iconsax.user, color: AppColors.primary, size: 28),
                                                  )
                                                : Icon(Iconsax.user, color: AppColors.primary, size: 28),
                                          ),
                                        ),
                                        const SizedBox(width: 12),
                                        Expanded(
                                          child: Column(
                                            mainAxisAlignment: MainAxisAlignment.center,
                                            crossAxisAlignment: CrossAxisAlignment.start,
                                            children: [
                                              Text(
                                                doc.fullName,
                                                style: TextStyle(
                                                  fontSize: 13.5,
                                                  fontWeight: FontWeight.w800,
                                                  color: context.textPrimary,
                                                ),
                                                maxLines: 2,
                                                overflow: TextOverflow.ellipsis,
                                              ),
                                              const SizedBox(height: 4),
                                              Text(
                                                specialty,
                                                style: const TextStyle(
                                                  fontSize: 11,
                                                  color: AppColors.primary,
                                                  fontWeight: FontWeight.w700,
                                                ),
                                                maxLines: 1,
                                                overflow: TextOverflow.ellipsis,
                                              ),
                                              const SizedBox(height: 6),
                                              if (doc.yearsOfExperience != null)
                                                Text(
                                                  '${doc.yearsOfExperience} ${isVi ? 'năm KN' : 'yrs exp'}',
                                                  style: TextStyle(
                                                    fontSize: 10.5,
                                                    color: context.textMuted,
                                                  ),
                                                ),
                                            ],
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                );
                              },
                            ),
                          ),
                const SizedBox(height: 26),

                // Dịch vụ nổi bật
                HomeSectionHeader(
                  title: context.l10n('our_services'),
                  onSeeAll: () => context.push(AppRoutes.servicesList),
                ),
                const SizedBox(height: 14),
                _isLoading
                    ? const _LoadingColumn()
                    : _services.isEmpty
                        ? _EmptySection(message: context.l10n('load_services_failed'))
                        : Column(
                            children: List.generate(
                              _services.take(5).length,
                              (i) => Padding(
                                padding: const EdgeInsets.only(bottom: 12),
                                child: ServiceCard(service: _services[i], index: i),
                              ),
                            ),
                          ),
                const SizedBox(height: 26),

                // Tin tức nổi bật
                HomeSectionHeader(
                  title: context.l10n('news'),
                  onSeeAll: () => context.push(AppRoutes.postsList),
                ),
                const SizedBox(height: 14),
                if (_isLoading)
                  const _LoadingColumn()
                else if (_posts.isEmpty)
                  const _EmptySection(message: 'No news.')
                else
                  ..._posts.take(5).map(
                    (post) => Padding(
                      padding: const EdgeInsets.only(bottom: 14),
                      child: NewsCard(post: post),
                    ),
                  ),
              ]),
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// No Appointment Placeholder
// ─────────────────────────────────────────────────
class _NoAppointmentCard extends StatelessWidget {
  const _NoAppointmentCard();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 56,
            height: 56,
            decoration: BoxDecoration(
              color: context.primaryLight,
              borderRadius: BorderRadius.circular(16),
            ),
            child: const Icon(Iconsax.calendar_tick, color: AppColors.primary, size: 28),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  context.l10n('no_appointments'),
                  style: TextStyle(
                    color: context.textPrimary,
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  context.l10n('book_now'),
                  style: TextStyle(color: context.textSecondary, fontSize: 13),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          GestureDetector(
            onTap: () => context.push(AppRoutes.bookingSelectPatient),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(
                context.l10n('book_button'),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Loading & Empty states
// ─────────────────────────────────────────────────
class _LoadingRow extends StatelessWidget {
  const _LoadingRow();

  @override
  Widget build(BuildContext context) {
    return const SizedBox(
      height: 80,
      child: Center(
        child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
      ),
    );
  }
}

class _LoadingColumn extends StatelessWidget {
  const _LoadingColumn();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(vertical: 24),
      child: Center(
        child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
      ),
    );
  }
}

class _EmptySection extends StatelessWidget {
  final String message;
  const _EmptySection({required this.message});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 16),
      child: Center(
        child: Text(
          message,
          style: const TextStyle(color: AppColors.textMuted, fontSize: 14),
        ),
      ),
    );
  }
}

class _QuickAccessOption {
  final String id;
  final String label;
  final String fullLabel;
  final IconData icon;
  final VoidCallback onTap;

  const _QuickAccessOption({
    required this.id,
    required this.label,
    required this.fullLabel,
    required this.icon,
    required this.onTap,
  });
}

class _QuickAccessPanel extends StatefulWidget {
  const _QuickAccessPanel();

  @override
  State<_QuickAccessPanel> createState() => _QuickAccessPanelState();
}

class _QuickAccessPanelState extends State<_QuickAccessPanel> {
  static const String _prefKey = 'quick_access_selected_ids';
  static const List<String> _defaultIds = [
    'dental_ai',
    'reminders',
    'records',
    'queue',
  ];

  List<String> _selectedIds = List.from(_defaultIds);

  @override
  void initState() {
    super.initState();
    _loadSelectedIds();
  }

  Future<void> _loadSelectedIds() async {
    final prefs = await SharedPreferences.getInstance();
    final saved = prefs.getStringList(_prefKey);
    if (saved != null && saved.length == 4) {
      if (mounted) {
        setState(() {
          _selectedIds = saved;
        });
      }
    }
  }

  Future<void> _saveSelectedIds(List<String> ids) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setStringList(_prefKey, ids);
    if (mounted) {
      setState(() {
        _selectedIds = List.from(ids);
      });
    }
  }

  /// Chatbot AI yêu cầu đăng nhập — kiểm tra token trước khi vào, vì router
  /// hiện không có redirect guard chung cho các route cần xác thực.
  Future<void> _openChatbot(BuildContext context) async {
    final token = await AuthService().getToken();
    if (!context.mounted) return;
    context.push(token == null ? AppRoutes.login : AppRoutes.chat);
  }

  Map<String, _QuickAccessOption> _getOptions(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    return {
      'dental_ai': _QuickAccessOption(
        id: 'dental_ai',
        label: 'DENTAL AI',
        fullLabel: isVi ? 'Dental AI (Chatbot tư vấn)' : 'Dental AI Assistant',
        icon: Icons.smart_toy_rounded,
        onTap: () => _openChatbot(context),
      ),
      'reminders': _QuickAccessOption(
        id: 'reminders',
        label: isVi ? 'NHẮC NHỞ' : 'REMINDER',
        fullLabel: isVi ? 'Nhắc nhở y tế & lịch khám' : 'Medication & Booking Reminders',
        icon: Iconsax.notification,
        onTap: () => context.push(AppRoutes.reminders),
      ),
      'records': _QuickAccessOption(
        id: 'records',
        label: isVi ? 'HỒ SƠ Y TẾ' : 'RECORDS',
        fullLabel: isVi ? 'Hồ sơ y tế' : 'Medical Records',
        icon: Iconsax.folder,
        onTap: () => context.push(AppRoutes.medicalHistory),
      ),
      'queue': _QuickAccessOption(
        id: 'queue',
        label: isVi ? 'HÀNG CHỜ' : 'QUEUE',
        fullLabel: isVi ? 'Hàng chờ trực tuyến' : 'Live Queue',
        icon: Iconsax.timer,
        onTap: () => context.push(AppRoutes.queue),
      ),
      'history': _QuickAccessOption(
        id: 'history',
        label: isVi ? 'LỊCH SỬ KHÁM' : 'HISTORY',
        fullLabel: isVi ? 'Lịch sử khám bệnh' : 'Examination History',
        icon: Iconsax.clock,
        onTap: () => context.push(AppRoutes.medicalHistory),
      ),
      'family': _QuickAccessOption(
        id: 'family',
        label: isVi ? 'THÀNH VIÊN' : 'FAMILY',
        fullLabel: isVi ? 'Thành viên gia đình' : 'Family Members',
        icon: Iconsax.profile_2user,
        onTap: () => context.push(AppRoutes.familyMembers),
      ),
      'payment': _QuickAccessOption(
        id: 'payment',
        label: isVi ? 'THANH TOÁN' : 'PAYMENTS',
        fullLabel: isVi ? 'Thanh toán & Công nợ' : 'Payments & Invoices',
        icon: Iconsax.card,
        onTap: () => context.push(AppRoutes.paymentHistory),
      ),
    };
  }

  void _showEditModal(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final optionsMap = _getOptions(context);
    final allOptions = optionsMap.values.toList();
    List<String> tempSelected = List.from(_selectedIds);

    showDialog(
      context: context,
      useRootNavigator: true,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (modalContext, setModalState) {
            return Dialog(
              backgroundColor: modalContext.card,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(24),
              ),
              insetPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
              child: Container(
                constraints: BoxConstraints(
                  maxHeight: MediaQuery.of(modalContext).size.height * 0.7,
                  maxWidth: 400,
                ),
                padding: const EdgeInsets.all(20),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          isVi ? 'Tùy chỉnh Truy cập nhanh' : 'Customize Quick Access',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: modalContext.textPrimary,
                          ),
                        ),
                        IconButton(
                          onPressed: () => Navigator.pop(ctx),
                          icon: const Icon(Icons.close),
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      isVi
                          ? 'Chọn 4 lối tắt hiển thị ở trang chủ (${tempSelected.length}/4)'
                          : 'Select 4 shortcuts to show on Home (${tempSelected.length}/4)',
                      style: TextStyle(
                        fontSize: 13,
                        color: modalContext.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 16),
                    Flexible(
                      child: ListView.separated(
                        shrinkWrap: true,
                        itemCount: allOptions.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 8),
                        itemBuilder: (context, index) {
                          final option = allOptions[index];
                          final isSelected = tempSelected.contains(option.id);
                          return InkWell(
                            onTap: () {
                              setModalState(() {
                                if (isSelected) {
                                  if (tempSelected.length > 1) {
                                    tempSelected.remove(option.id);
                                  }
                                } else {
                                  if (tempSelected.length < 4) {
                                    tempSelected.add(option.id);
                                  } else {
                                    ScaffoldMessenger.of(modalContext).showSnackBar(
                                      SnackBar(
                                        content: Text(
                                          isVi
                                              ? 'Tối đa 4 lối tắt. Vui lòng bỏ chọn 1 cái trước.'
                                              : 'Maximum 4 shortcuts. Uncheck one first.',
                                        ),
                                        duration: const Duration(seconds: 1),
                                      ),
                                    );
                                  }
                                }
                              });
                            },
                            borderRadius: BorderRadius.circular(12),
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                              decoration: BoxDecoration(
                                color: isSelected
                                    ? AppColors.primary.withValues(alpha: 0.08)
                                    : modalContext.bg,
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(
                                  color: isSelected
                                      ? AppColors.primary
                                      : modalContext.divider,
                                  width: isSelected ? 1.5 : 1.0,
                                ),
                              ),
                              child: Row(
                                children: [
                                  Icon(
                                    option.icon,
                                    color: isSelected ? AppColors.primary : modalContext.textSecondary,
                                    size: 22,
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: Text(
                                      option.fullLabel,
                                      style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                                        color: modalContext.textPrimary,
                                      ),
                                    ),
                                  ),
                                  Icon(
                                    isSelected
                                        ? Icons.check_circle_rounded
                                        : Icons.radio_button_unchecked,
                                    color: isSelected ? AppColors.primary : modalContext.textMuted,
                                    size: 20,
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                    const SizedBox(height: 16),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton(
                        onPressed: tempSelected.length == 4
                            ? () {
                                _saveSelectedIds(tempSelected);
                                Navigator.pop(ctx);
                              }
                            : null,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                        ),
                        child: Text(
                          isVi ? 'Lưu thay đổi' : 'Save Changes',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final optionsMap = _getOptions(context);

    final activeItems = _selectedIds
        .map((id) => optionsMap[id])
        .whereType<_QuickAccessOption>()
        .toList();

    while (activeItems.length < 4) {
      for (final opt in optionsMap.values) {
        if (!activeItems.contains(opt)) {
          activeItems.add(opt);
          if (activeItems.length == 4) break;
        }
      }
    }

    final item0 = activeItems[0];
    final item1 = activeItems[1];
    final item2 = activeItems[2];
    final item3 = activeItems[3];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              isVi ? 'Truy cập nhanh' : 'Quick Access',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
            ),
            InkWell(
              onTap: () => _showEditModal(context),
              borderRadius: BorderRadius.circular(8),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                child: Row(
                  children: [
                    Icon(
                      Iconsax.edit_2,
                      size: 14,
                      color: AppColors.primary,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      isVi ? 'Chỉnh sửa' : 'Edit',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: AppColors.primary,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Left - Book Appointment
            Expanded(
              flex: 4,
              child: GestureDetector(
                onTap: () => context.push(AppRoutes.bookingSelectPatient),
                child: Container(
                  height: 140,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [
                      BoxShadow(
                        color: AppColors.primary.withValues(alpha: 0.2),
                        blurRadius: 10,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Container(
                        padding: const EdgeInsets.all(8),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.2),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          Iconsax.calendar_add,
                          color: Colors.white,
                          size: 24,
                        ),
                      ),
                      Text(
                        isVi ? 'Đặt khám' : 'Book Appointment',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 14,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            // Right - Grid of 2x2
            Expanded(
              flex: 6,
              child: SizedBox(
                height: 140,
                child: Column(
                  children: [
                    Expanded(
                      child: Row(
                        children: [
                          _buildSmallCard(
                            context,
                            icon: item0.icon,
                            label: item0.label,
                            onTap: () => item0.onTap(),
                          ),
                          const SizedBox(width: 12),
                          _buildSmallCard(
                            context,
                            icon: item1.icon,
                            label: item1.label,
                            onTap: () => item1.onTap(),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 12),
                    Expanded(
                      child: Row(
                        children: [
                          _buildSmallCard(
                            context,
                            icon: item2.icon,
                            label: item2.label,
                            onTap: () => item2.onTap(),
                          ),
                          const SizedBox(width: 12),
                          _buildSmallCard(
                            context,
                            icon: item3.icon,
                            label: item3.label,
                            onTap: () => item3.onTap(),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildSmallCard(
    BuildContext context, {
    required IconData icon,
    required String label,
    required VoidCallback onTap,
  }) {
    final bgColor = context.card;
    final iconColor = context.isDark ? Colors.white : AppColors.primary;
    final textColor = context.textPrimary;

    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          decoration: BoxDecoration(
            color: bgColor,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: context.divider),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.03),
                blurRadius: 6,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: iconColor, size: 20),
              const SizedBox(height: 6),
              FittedBox(
                fit: BoxFit.scaleDown,
                child: Text(
                  label,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w900,
                    color: textColor,
                    letterSpacing: 0.5,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _UpcomingAppointmentCard extends StatelessWidget {
  final MyAppointmentItem item;
  final VoidCallback onRefresh;

  const _UpcomingAppointmentCard({
    required this.item,
    required this.onRefresh,
  });

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final weekdays = isVi
        ? ['', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật']
        : ['', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

    final date = item.parsedDate;
    final dateStr =
        '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
    final timeStr =
        '${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
    final dayLabel = weekdays[date.weekday];
    final (statusLabel, statusColor, statusBg) = _statusStyle(item.status, isVi);

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: context.divider,
                  shape: BoxShape.circle,
                ),
                child: ClipOval(
                  child: item.dentistAvatarUrl != null && item.dentistAvatarUrl!.isNotEmpty
                      ? (item.dentistAvatarUrl!.startsWith('assets/')
                          ? Image.asset(item.dentistAvatarUrl!, fit: BoxFit.cover)
                          : Image.network(
                              ApiConstants.resolveAssetUrl(item.dentistAvatarUrl)!,
                              fit: BoxFit.cover,
                              errorBuilder: (_, __, ___) => const Icon(Iconsax.user, size: 20),
                            ))
                      : const Icon(Iconsax.user, size: 20),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.dentistName,
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: context.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      item.specialization,
                      style: TextStyle(
                        fontSize: 12,
                        color: context.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: statusBg,
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  statusLabel,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: statusColor,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Divider(color: context.divider, height: 1),
          const SizedBox(height: 16),
          Row(
            children: [
              Icon(Iconsax.calendar_1, size: 16, color: AppColors.primary),
              const SizedBox(width: 8),
              Text(
                '$dateStr · $dayLabel',
                style: TextStyle(fontSize: 13, color: context.textPrimary, fontWeight: FontWeight.w600),
              ),
              const Spacer(),
              Icon(Iconsax.clock, size: 16, color: AppColors.primary),
              const SizedBox(width: 8),
              Text(
                timeStr,
                style: TextStyle(fontSize: 13, color: context.textPrimary, fontWeight: FontWeight.w600),
              ),
            ],
          ),
          if (item.patientName != null && item.patientName!.isNotEmpty) ...[
            const SizedBox(height: 10),
            Row(
              children: [
                Icon(Iconsax.user, size: 16, color: context.textSecondary),
                const SizedBox(width: 8),
                Text(
                  item.patientRelationship == null || item.patientRelationship!.isEmpty || item.patientRelationship == 'Tôi' || item.patientRelationship == 'Self'
                      ? '${isVi ? 'Bệnh nhân' : 'Patient'}: ${item.patientName} (${isVi ? 'Tôi' : 'Self'})'
                      : '${isVi ? 'Bệnh nhân' : 'Patient'}: ${item.patientName} (${item.patientRelationship})',
                  style: TextStyle(fontSize: 12, color: context.textSecondary),
                ),
              ],
            ),
          ],
          if (item.serviceName != null && item.serviceName!.isNotEmpty) ...[
            const SizedBox(height: 10),
            Row(
              children: [
                Icon(Iconsax.health, size: 16, color: context.textSecondary),
                const SizedBox(width: 8),
                Text(
                  item.serviceName!,
                  style: TextStyle(fontSize: 12, color: context.textSecondary),
                ),
              ],
            ),
          ],
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            height: 44,
            child: OutlinedButton(
              onPressed: () async {
                await context.push(AppRoutes.appointmentDetails, extra: item);
                onRefresh();
              },
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                side: const BorderSide(color: AppColors.primary, width: 1.5),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              child: Text(
                isVi ? 'Xem chi tiết' : 'View Details',
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
              ),
            ),
          ),
        ],
      ),
    );
  }

  (String, Color, Color) _statusStyle(String status, bool isVi) {
    switch (status.toLowerCase()) {
      case 'pending':
        return (
          isVi ? 'Chờ xác nhận' : 'Pending',
          const Color(0xFFF59E0B),
          const Color(0xFFFEF3C7),
        );
      case 'confirmed':
        return (
          isVi ? 'Đã xác nhận' : 'Confirmed',
          AppColors.primary,
          const Color(0xFFFEE2E2),
        );
      case 'checkedin':
        return (
          isVi ? 'Đã check-in' : 'Checked In',
          const Color(0xFF10B981),
          const Color(0xFFD1FAE5),
        );
      case 'inprogress':
        return (
          isVi ? 'Đang khám' : 'In Progress',
          const Color(0xFF3B82F6),
          const Color(0xFFDBEAFE),
        );
      default:
        return (
          status,
          const Color(0xFF64748B),
          const Color(0xFFF1F5F9),
        );
    }
  }
}

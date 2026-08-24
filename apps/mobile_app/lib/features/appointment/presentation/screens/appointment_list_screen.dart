import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';
import 'package:mobile_app/core/constants/api_constants.dart';

class AppointmentListScreen extends StatefulWidget {
  final String? filterPatientId;
  final String? filterPatientName;
  final int? initialTab;
  const AppointmentListScreen({
    super.key,
    this.filterPatientId,
    this.filterPatientName,
    this.initialTab,
  });

  @override
  State<AppointmentListScreen> createState() => _AppointmentListScreenState();
}

class _AppointmentListScreenState extends State<AppointmentListScreen> {
  final _service = BookingService();
  List<MyAppointmentItem> _items = [];
  bool _loading = true;
  String? _error;
  String? _filterPatientId;
  String? _filterPatientName;
  List<PatientInfo> _filterOptions = [];
  bool _isDropdownOpen = false;

  @override
  void initState() {
    super.initState();
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    _filterPatientId = widget.filterPatientId ?? 'self';
    _filterPatientName = widget.filterPatientName ?? (isVi ? 'Tôi' : 'Self');

    // Initialize with default values first so we don't have an empty list
    _filterOptions = [
      PatientInfo(
        id: _filterPatientId ?? 'self',
        name: _filterPatientName ?? (isVi ? 'Tôi' : 'Self'),
        relationship: _filterPatientId == 'self' ? (isVi ? 'Tôi' : 'Self') : (isVi ? 'Thành viên gia đình' : 'Family Member'),
      ),
    ];

    _load();
    _loadFilterOptions();
  }

  @override
  void didUpdateWidget(covariant AppointmentListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.filterPatientId != oldWidget.filterPatientId ||
        widget.filterPatientName != oldWidget.filterPatientName) {
      final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
      setState(() {
        _filterPatientId = widget.filterPatientId ?? 'self';
        _filterPatientName = widget.filterPatientName ?? (isVi ? 'Tôi' : 'Self');
      });
      _load();
      _loadFilterOptions();
    }
  }

  Future<void> _loadFilterOptions() async {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    try {
      final auth = AuthService();
      String myName = isVi ? 'Tôi' : 'Self';
      String? myAvatar;
      try {
        final profile = await auth.getMyProfile();
        final cachedAvatar = await auth.getUserAvatar();
        myAvatar = (profile.profilePictureUrl != null && profile.profilePictureUrl!.isNotEmpty)
            ? profile.profilePictureUrl
            : cachedAvatar;
        if (profile.fullName.isNotEmpty) myName = profile.fullName;
      } catch (_) {
        myAvatar = await auth.getUserAvatar();
        final localName = await auth.getUserName();
        if (localName != null && localName.isNotEmpty) myName = localName;
      }

      await FamilyService().loadFromServer();
      final family = FamilyService().getMembers();

      final list = [
        PatientInfo(id: 'self', name: myName, relationship: isVi ? 'Tôi' : 'Self', avatarUrl: myAvatar),
        ...family.map((m) => PatientInfo(
              id: m.id,
              name: m.fullName,
              relationship: m.relationship,
              avatarUrl: m.profilePictureUrl,
            )),
      ];

      if (mounted) {
        setState(() {
          _filterOptions = list;
          if (_filterPatientId == 'self') {
            _filterPatientName = myName;
          } else {
            final match = list.firstWhere(
              (opt) => opt.id == _filterPatientId,
              orElse: () => PatientInfo(id: '', name: '', relationship: ''),
            );
            if (match.name.isNotEmpty) {
              _filterPatientName = match.name;
            }
          }
        });
      }
    } catch (_) {}
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      var list = await _service.getMyAppointments();
      if (_filterPatientId != null) {
        if (_filterPatientId == 'self') {
          list = list.where((item) => 
            item.patientId == null ||
            item.patientId == 'self' ||
            item.patientRelationship == null || 
            item.patientRelationship!.isEmpty || 
            item.patientRelationship == 'Tôi' || 
            item.patientRelationship == 'Self'
          ).toList();
        } else {
          list = list.where((item) => 
            item.patientId == _filterPatientId ||
            (_filterPatientName != null && _filterPatientName!.isNotEmpty && item.patientName == _filterPatientName)
          ).toList();
        }
      }
      if (mounted) setState(() { _items = list; _loading = false; });
    } catch (e) {
      if (mounted) {
        setState(() { 
          _error = context.l10n('load_appointments_failed') == 'load_appointments_failed'
              ? 'Không thể tải lịch hẹn.'
              : context.l10n('load_appointments_failed'); 
          _loading = false; 
        });
      }
    }
  }

  String _getInitials(String name) {
    final parts = name.trim().split(' ');
    if (parts.isEmpty || parts[0].isEmpty) return 'T';
    if (parts.length == 1) return parts[0][0].toUpperCase();
    return '${parts[0][0]}${parts.last[0]}'.toUpperCase();
  }

  Widget _buildAvatarCircle(String? avatarUrl, {required double size, required bool isSelected, String? name}) {
    final initials = _getInitials(name ?? 'Tôi');
    if (avatarUrl != null && avatarUrl.isNotEmpty) {
      if (avatarUrl.startsWith('assets/')) {
        return Container(
          width: size,
          height: size,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            border: isSelected ? Border.all(color: AppColors.primary, width: 2) : null,
          ),
          child: ClipOval(
            child: Image.asset(
              avatarUrl,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => _buildInitialsFallback(size, isSelected, initials),
            ),
          ),
        );
      }
      final url = ApiConstants.resolveAssetUrl(avatarUrl)!;
      return Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          border: isSelected ? Border.all(color: AppColors.primary, width: 2) : null,
        ),
        child: ClipOval(
          child: Image.network(
            url,
            fit: BoxFit.cover,
            errorBuilder: (_, __, ___) => _buildInitialsFallback(size, isSelected, initials),
          ),
        ),
      );
    }
    return _buildInitialsFallback(size, isSelected, initials);
  }

  Widget _buildInitialsFallback(double size, bool isSelected, String initials) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: isSelected ? AppColors.primary : AppColors.primaryLight,
        shape: BoxShape.circle,
      ),
      child: Center(
        child: Text(
          initials,
          style: TextStyle(
            color: isSelected ? Colors.white : AppColors.primary,
            fontWeight: FontWeight.bold,
            fontSize: size * 0.4,
          ),
        ),
      ),
    );
  }

  Widget _buildMemberDropdown(bool isVi) {
    final selectedOption = _filterOptions.firstWhere(
      (opt) => _filterPatientId == opt.id,
      orElse: () => PatientInfo(
        id: _filterPatientId ?? 'self',
        name: _filterPatientName ?? (_filterPatientId == 'self' ? (isVi ? 'Tôi' : 'Self') : 'Thành viên'),
        relationship: isVi ? 'Thành viên gia đình' : 'Family Member',
      ),
    );

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Column(
        children: [
          GestureDetector(
            onTap: () {
              setState(() {
                _isDropdownOpen = !_isDropdownOpen;
              });
            },
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.divider),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                    blurRadius: 8,
                    offset: const Offset(0, 3),
                  ),
                ],
              ),
              child: Row(
                children: [
                  _buildAvatarCircle(
                    selectedOption.avatarUrl,
                    size: 44,
                    isSelected: true,
                    name: selectedOption.name,
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          selectedOption.name,
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          selectedOption.relationship,
                          style: TextStyle(
                            fontSize: 12,
                            color: context.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  AnimatedRotation(
                    turns: _isDropdownOpen ? 0.5 : 0.0,
                    duration: const Duration(milliseconds: 200),
                    child: Icon(Icons.keyboard_arrow_down_rounded, color: context.textSecondary, size: 24),
                  ),
                ],
              ),
            ),
          ),
          if (_isDropdownOpen) ...[
            const SizedBox(height: 8),
            Container(
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.divider),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: context.isDark ? 0.12 : 0.03),
                    blurRadius: 8,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                children: _filterOptions.map((opt) {
                  final isSelected = _filterPatientId == opt.id;
                  return Column(
                    children: [
                      ListTile(
                        leading: _buildAvatarCircle(
                          opt.avatarUrl,
                          size: 36,
                          isSelected: isSelected,
                          name: opt.name,
                        ),
                        title: Text(
                          opt.name,
                          style: TextStyle(
                            fontSize: 14.5,
                            fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
                            color: context.textPrimary,
                          ),
                        ),
                        subtitle: Text(
                          opt.relationship,
                          style: TextStyle(
                            fontSize: 12,
                            color: context.textSecondary,
                          ),
                        ),
                        trailing: isSelected
                            ? const Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 20)
                            : null,
                        onTap: () {
                          setState(() {
                            _filterPatientId = opt.id;
                            _filterPatientName = opt.name;
                            _isDropdownOpen = false;
                          });
                          _load();
                        },
                      ),
                      if (opt != _filterOptions.last)
                        Divider(color: context.divider, height: 1),
                    ],
                  );
                }).toList(),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildTabContent(List<MyAppointmentItem> items, bool isVi) {
    return RefreshIndicator(
      onRefresh: _load,
      color: AppColors.primary,
      child: items.isEmpty
          ? _EmptyView(
              onBook: () => context.push(AppRoutes.bookingSelectPatient),
              isVi: isVi,
            )
          : ListView.builder(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 100),
              itemCount: items.length,
              itemBuilder: (_, i) => _AppointmentCard(
                item: items[i],
                isVi: isVi,
                onRefresh: _load,
              ),
            ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    return DefaultTabController(
      key: ValueKey(widget.initialTab ?? 0),
      length: 3,
      initialIndex: (widget.initialTab ?? 0).clamp(0, 2),
      child: Scaffold(
        backgroundColor: context.bg,
        appBar: AppBar(
          backgroundColor: context.card,
          elevation: 0,
          automaticallyImplyLeading: false,
          title: Text(
            context.l10n('my_appointments'),
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w800,
              color: context.textPrimary,
            ),
          ),
          centerTitle: true,
          actions: [
            IconButton(
              icon: Icon(Iconsax.refresh, color: context.textMuted, size: 20),
              onPressed: _load,
            ),
          ],
        ),
        body: _loading
            ? Center(child: CircularProgressIndicator(color: AppColors.primary))
            : _error != null
                 ? _ErrorView(message: _error!, onRetry: _load, isVi: isVi)
                 : Column(
                     children: [
                       _buildMemberDropdown(isVi),
                       Container(
                         color: context.card,
                         child: TabBar(
                           labelColor: AppColors.primary,
                           unselectedLabelColor: context.textSecondary,
                           indicatorColor: AppColors.primary,
                           indicatorSize: TabBarIndicatorSize.tab,
                           indicatorWeight: 3,
                           labelStyle: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
                           unselectedLabelStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
                           tabs: [
                             Tab(text: isVi ? 'Sắp tới' : 'Upcoming'),
                             Tab(text: isVi ? 'Hoàn thành' : 'Completed'),
                             Tab(text: isVi ? 'Đã hủy' : 'Cancelled'),
                           ],
                         ),
                       ),
                       Expanded(
                         child: TabBarView(
                           children: [
                             _buildTabContent(
                               _items.where((item) =>
                                   item.status.toLowerCase() != 'completed' &&
                                   item.status.toLowerCase() != 'cancelled').toList(),
                               isVi,
                             ),
                             _buildTabContent(
                               _items.where((item) => item.status.toLowerCase() == 'completed').toList(),
                               isVi,
                             ),
                             _buildTabContent(
                               _items.where((item) => item.status.toLowerCase() == 'cancelled').toList(),
                               isVi,
                             ),
                           ],
                         ),
                       ),
                     ],
                   ),
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => context.push(AppRoutes.bookingSelectPatient),
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          elevation: 2,
          icon: Icon(Iconsax.calendar_add, size: 20),
          label: Text(
            context.l10n('book_new'),
            style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14),
          ),
        ),
      ),
    );
  }
}

// â”€â”€ Appointment Card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _AppointmentCard extends StatelessWidget {
  final MyAppointmentItem item;
  final bool isVi;
  final VoidCallback onRefresh;
  const _AppointmentCard({
    required this.item,
    required this.isVi,
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
      margin: EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        children: [
          // Header
          Padding(
            padding: EdgeInsets.fromLTRB(16, 14, 16, 12),
            child: Row(
              children: [
                _DoctorAvatar(avatarUrl: item.dentistAvatarUrl),
                SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.dentistName,
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w800,
                          color: AppColors.primary,
                        ),
                      ),
                      SizedBox(height: 2),
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
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: statusBg,
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Text(
                    statusLabel,
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: statusColor,
                    ),
                  ),
                ),
              ],
            ),
          ),

          Divider(color: context.divider, height: 1),

          // Detail rows
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Column(
              children: [
                _DetailRow(icon: Iconsax.tag, text: '#${item.appointmentCode}', bold: true),
                SizedBox(height: 8),
                _DetailRow(icon: Iconsax.calendar_1, text: '$dateStr · $dayLabel'),
                SizedBox(height: 8),
                _DetailRow(icon: Iconsax.clock, text: item.displayTimeRange),
                if (item.serviceName != null) ...[
                  SizedBox(height: 8),
                  _DetailRow(icon: Iconsax.health, text: item.serviceName!),
                ],
                if (item.patientName != null && item.patientName!.isNotEmpty) ...[
                  SizedBox(height: 8),
                  _DetailRow(
                    icon: Iconsax.user,
                    text: item.patientRelationship == null || item.patientRelationship!.isEmpty || item.patientRelationship == 'Tôi' || item.patientRelationship == 'Self'
                        ? '${isVi ? 'Bệnh nhân' : 'Patient'}: ${item.patientName} (${isVi ? 'Tôi' : 'Self'})'
                        : '${isVi ? 'Bệnh nhân' : 'Patient'}: ${item.patientName} (${item.patientRelationship})',
                  ),
                ],
                if (item.symptoms != null && item.symptoms!.isNotEmpty) ...[
                  SizedBox(height: 8),
                  _DetailRow(icon: Iconsax.note_text, text: item.symptoms!, muted: true),
                ],
              ],
            ),
          ),
          Divider(color: context.divider, height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: SizedBox(
              width: double.infinity,
              height: 38,
              child: OutlinedButton(
                onPressed: () async {
                  await context.push(AppRoutes.appointmentDetails, extra: item);
                  onRefresh();
                },
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppColors.primary,
                  side: const BorderSide(color: AppColors.primary),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
                child: Text(
                  isVi ? 'Xem chi tiết' : 'View Details',
                  style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  static (String, Color, Color) _statusStyle(String status, bool isVi) {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return (isVi ? 'Đã xác nhận' : 'Confirmed', const Color(0xFF16A34A), const Color(0xFFDCFCE7));
      case 'checkedin':
        return (isVi ? 'Đã check-in' : 'Checked In', const Color(0xFF4F46E5), const Color(0xFFEEF2FF));
      case 'inprogress':
        return (isVi ? 'Đang khám' : 'In Progress', const Color(0xFF0284C7), const Color(0xFFE0F2FE));
      case 'pendingpayment':
        return (isVi ? 'Chờ thanh toán' : 'Pending Payment', const Color(0xFFEA580C), const Color(0xFFFFEDD5));
      case 'completed':
        return (isVi ? 'Hoàn thành' : 'Completed', const Color(0xFF16A34A), const Color(0xFFDCFCE7));
      case 'cancelled':
        return (isVi ? 'Đã hủy' : 'Cancelled', const Color(0xFFEF4444), const Color(0xFFFEE2E2));
      case 'rebooking':
        return (isVi ? 'Tái đặt lịch' : 'Rebooked', const Color(0xFFD97706), const Color(0xFFFEF3C7));
      case 'noshow':
        return (isVi ? 'Vắng mặt' : 'No Show', const Color(0xFFEF4444), const Color(0xFFFEE2E2));
      default:
        return (isVi ? 'Chờ xác nhận' : 'Pending', const Color(0xFFD97706), const Color(0xFFFEF3C7));
    }
  }
}

// â”€â”€ Detail Row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _DetailRow extends StatelessWidget {
  final IconData icon;
  final String text;
  final bool bold;
  final bool muted;
  const _DetailRow({required this.icon, required this.text, this.bold = false, this.muted = false});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 14, color: context.textMuted),
        SizedBox(width: 8),
        Expanded(
          child: Text(
            text,
            style: TextStyle(
              fontSize: 13,
              fontWeight: bold ? FontWeight.w700 : FontWeight.w500,
              color: muted ? context.textMuted : context.textPrimary,
            ),
          ),
        ),
      ],
    );
  }
}

// â”€â”€ Doctor Avatar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _DoctorAvatar extends StatelessWidget {
  final String? avatarUrl;
  const _DoctorAvatar({this.avatarUrl});

  @override
  Widget build(BuildContext context) {
    final placeholder = Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        color: context.primaryLight,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Icon(Iconsax.profile_circle, color: AppColors.primary, size: 26),
    );
    final resolved = ApiConstants.resolveAssetUrl(avatarUrl);
    if (resolved == null) return placeholder;
    return ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: Image.network(
        resolved,
        width: 48,
        height: 48,
        fit: BoxFit.cover,
        errorBuilder: (_, _, _) => placeholder,
        loadingBuilder: (_, child, prog) => prog == null
            ? child
            : Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: context.bg,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Center(
                  child: SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.primary),
                  ),
                ),
              ),
      ),
    );
  }
}

// â”€â”€ Empty View â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _EmptyView extends StatelessWidget {
  final VoidCallback onBook;
  final bool isVi;
  const _EmptyView({required this.onBook, required this.isVi});

  @override
  Widget build(BuildContext context) {
    return ListView(
      children: [
        SizedBox(height: 80),
        Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 80,
                height: 80,
                decoration: BoxDecoration(
                  color: context.primaryLight,
                  shape: BoxShape.circle,
                ),
                child: Icon(Iconsax.calendar_1, color: AppColors.primary, size: 36),
              ),
              const SizedBox(height: 20),
              Text(
                context.l10n('no_appointments_yet'),
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                  color: context.textPrimary,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                context.l10n('book_to_start_tracking'),
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: context.textSecondary,
                  height: 1.6,
                ),
              ),
              SizedBox(height: 28),
              ElevatedButton.icon(
                onPressed: onBook,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.symmetric(horizontal: 24, vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                icon: const Icon(Iconsax.calendar_add, size: 18),
                label: Text(
                  context.l10n('book_now_btn'),
                  style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// â”€â”€ Error View â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  final bool isVi;
  const _ErrorView({required this.message, required this.onRetry, required this.isVi});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, style: TextStyle(color: context.textMuted)),
          const SizedBox(height: 12),
          TextButton(
            onPressed: onRetry,
            child: Text(
              context.l10n('retry') == 'retry' ? 'Thử lại' : context.l10n('retry'),
              style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }
}


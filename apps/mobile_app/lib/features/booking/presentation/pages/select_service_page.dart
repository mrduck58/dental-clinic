import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';

class SelectServicePage extends StatefulWidget {
  final BookingDraft draft;
  const SelectServicePage({super.key, required this.draft});

  @override
  State<SelectServicePage> createState() => _SelectServicePageState();
}

class _SelectServicePageState extends State<SelectServicePage> {
  final _searchCtrl = TextEditingController();
  final _bookingService = BookingService();
  String _query = '';
  List<ServiceModel> _services = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _searchCtrl.addListener(() => setState(() => _query = _searchCtrl.text.toLowerCase()));
    _load();
  }

  Future<void> _load() async {
    try {
      final list = await _bookingService.getActiveServices();
      if (mounted) setState(() { _services = list; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = 'Không thể tải dịch vụ.'; _loading = false; });
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  List<ServiceModel> get _filtered {
    if (_query.isEmpty) return _services;
    return _services
        .where((s) =>
            s.name.toLowerCase().contains(_query) ||
            s.description.toLowerCase().contains(_query))
        .toList();
  }

  Future<void> _onSelectService(ServiceModel s) async {
    final service = ServiceInfo(
      id: s.id,
      name: s.name,
      description: s.description,
      price: s.formattedPrice,
      imageUrl: s.imageUrl,
      iconUrl: s.iconUrl,
      durationMinutes: s.durationMinutes,
      options: s.options,
    );
    var updatedDraft = widget.draft.copyWith(service: service);

    if (updatedDraft.date != null && updatedDraft.timeSlot != null && updatedDraft.doctor != null) {
      try {
        final patientId = updatedDraft.patient?.id ?? 'self';
        final res = await _bookingService.holdSlot(
          patientId: patientId == 'self' ? '' : patientId,
          dentistId: updatedDraft.doctor!.id,
          date: updatedDraft.date!,
          timeSlot: updatedDraft.timeSlot!.range,
          serviceId: s.id,
        );
        if (res.isSuccess && res.expiresAt != null) {
          updatedDraft = updatedDraft.copyWith(holdExpiresAt: res.expiresAt);
        }
      } catch (e) {
        if (!mounted) return;
        final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
        await showDialog(
          context: context,
          builder: (ctx) => AlertDialog(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            title: Row(
              children: [
                const Icon(Iconsax.warning_2, color: Color(0xFFEF4444), size: 24),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    isVi ? 'Khung giờ không đủ thời lượng' : 'Slot Overlap',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                ),
              ],
            ),
            content: Text(
              isVi
                  ? 'Dịch vụ "${s.name}" có thời lượng ước tính ${s.durationMinutes} phút và bị trùng với lịch hẹn/ca giữ khác ở khung giờ đã chọn. Vui lòng chọn lại giờ khám.'
                  : 'Service "${s.name}" takes ${s.durationMinutes} minutes and overlaps with an existing appointment. Please select another slot.',
              style: const TextStyle(fontSize: 14, height: 1.4),
            ),
            actions: [
              ElevatedButton(
                onPressed: () {
                  Navigator.of(ctx).pop();
                  context.push(AppRoutes.bookingSelectTimeSlot, extra: updatedDraft);
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
                child: Text(isVi ? 'Chọn lại giờ khám' : 'Select Time Slot'),
              ),
            ],
          ),
        );
        return;
      }
      if (!mounted) return;
      context.push(AppRoutes.bookingReview, extra: updatedDraft);
    } else {
      context.push(AppRoutes.bookingSelectDatetime, extra: updatedDraft);
    }
  }

  ServiceInfo _toServiceInfo(ServiceModel s) => ServiceInfo(
        id: s.id,
        name: s.name,
        description: s.description,
        price: s.formattedPrice,
        imageUrl: s.imageUrl,
        iconUrl: s.iconUrl,
        durationMinutes: s.durationMinutes,
        options: s.options,
      );

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Chọn dịch vụ' : 'Select Service'),
      body: Column(
        children: [
          if (widget.draft.holdExpiresAt != null)
            HoldCountdownBanner(holdExpiresAt: widget.draft.holdExpiresAt),
          // ── Search bar ────────────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
            child: TextField(
              controller: _searchCtrl,
              style: TextStyle(fontSize: 14, color: context.textPrimary),
              decoration: InputDecoration(
                hintText: isVi ? 'Tìm nhanh dịch vụ' : 'Search services...',
                hintStyle: TextStyle(color: context.textSecondary, fontSize: 14),
                prefixIcon: Icon(Iconsax.search_normal, color: context.textSecondary, size: 20),
                filled: true,
                fillColor: context.card,
                contentPadding: const EdgeInsets.symmetric(vertical: 10),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: context.divider),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: context.divider),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
                ),
              ),
            ),
          ),

          // ── List ──────────────────────────────────────────────────────────
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _error != null
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(isVi ? 'Không thể tải dịch vụ.' : 'Unable to load services.', style: TextStyle(color: context.textSecondary)),
                            const SizedBox(height: 12),
                            TextButton(onPressed: _load, child: Text(isVi ? 'Thử lại' : 'Retry')),
                          ],
                        ),
                      )
                    : _filtered.isEmpty
                        ? Center(
                            child: Text(isVi ? 'Không tìm thấy dịch vụ phù hợp.' : 'No matching services found.',
                                style: TextStyle(color: context.textSecondary)),
                          )
                        : ListView.builder(
                            padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
                            itemCount: _filtered.length,
                            itemBuilder: (_, i) {
                              final s = _filtered[i];
                              return _ServiceItem(
                                service: s,
                                onTap: () => _onSelectService(s),
                                onViewDetail: () => context.push(
                                  AppRoutes.bookingServiceDetail,
                                  extra: _toServiceInfo(s),
                                ),
                              );
                            },
                          ),
          ),
        ],
      ),
    );
  }
}

// ─── Service List Item ────────────────────────────────────────────────────────

class _ServiceItem extends StatelessWidget {
  final ServiceModel service;
  final VoidCallback onTap;
  final VoidCallback onViewDetail;

  const _ServiceItem({
    required this.service,
    required this.onTap,
    required this.onViewDetail,
  });

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                GestureDetector(
                  onTap: onTap,
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 14, 12, 0),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: Wrap(
                            crossAxisAlignment: WrapCrossAlignment.center,
                            spacing: 6,
                            runSpacing: 4,
                            children: [
                              Text(
                                service.name,
                                style: TextStyle(
                                  fontSize: 14,
                                  fontWeight: FontWeight.w800,
                                  color: context.textPrimary,
                                ),
                              ),
                              if (service.durationMinutes > 0)
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                  decoration: BoxDecoration(
                                    color: AppColors.primary.withValues(alpha: context.isDark ? 0.2 : 0.1),
                                    borderRadius: BorderRadius.circular(6),
                                  ),
                                  child: Row(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      const Icon(Iconsax.clock, size: 10, color: AppColors.primary),
                                      const SizedBox(width: 3),
                                      Text(
                                        service.durationText,
                                        style: const TextStyle(
                                          fontSize: 11,
                                          fontWeight: FontWeight.w700,
                                          color: AppColors.primary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),

                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 6, 0, 0),
                  child: Text(
                    service.description,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 13,
                      color: context.textSecondary,
                      height: 1.5,
                    ),
                  ),
                ),

                GestureDetector(
                  onTap: onViewDetail,
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 4, 0, 14),
                    child: Text(
                      isVi ? 'Xem thêm  ›' : 'View detail  ›',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppColors.primary,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          GestureDetector(
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: Icon(Icons.arrow_forward_ios, color: context.textSecondary, size: 20),
            ),
          ),
        ],
      ),
    );
  }
}

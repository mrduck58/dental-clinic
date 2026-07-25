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

  void _onSelectService(ServiceModel s) {
    final service = ServiceInfo(
      id: s.id,
      name: s.name,
      description: s.description,
      price: s.formattedPrice,
    );
    context.push(AppRoutes.bookingSelectDatetime, extra: widget.draft.copyWith(service: service));
  }

  ServiceInfo _toServiceInfo(ServiceModel s) => ServiceInfo(
        id: s.id,
        name: s.name,
        description: s.description,
        price: s.formattedPrice,
      );

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Chọn dịch vụ' : 'Select Service', showHome: false),
      body: Column(
        children: [
          // ── Search bar ────────────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
            child: TextField(
              controller: _searchCtrl,
              style: TextStyle(fontSize: 14, color: context.textPrimary),
              decoration: InputDecoration(
                hintText: isVi ? 'Tìm nhanh chuyên khoa' : 'Search services...',
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
                    padding: const EdgeInsets.fromLTRB(16, 14, 0, 0),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: Text(
                            service.name,
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w800,
                              color: context.textPrimary,
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Text(
                          service.formattedPrice,
                          style: const TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w700,
                            color: AppColors.primary,
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

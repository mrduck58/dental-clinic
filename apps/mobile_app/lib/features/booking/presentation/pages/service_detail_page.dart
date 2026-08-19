import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';

class ServiceDetailPage extends StatefulWidget {
  final ServiceInfo service;
  const ServiceDetailPage({super.key, required this.service});

  @override
  State<ServiceDetailPage> createState() => _ServiceDetailPageState();
}

class _ServiceDetailPageState extends State<ServiceDetailPage> {
  late List<ServiceOptionModel> _options;
  bool _isLoadingOptions = false;

  @override
  void initState() {
    super.initState();
    _options = List<ServiceOptionModel>.from(widget.service.options);
    if (_options.isEmpty && widget.service.id.isNotEmpty) {
      _loadOptions();
    }
  }

  Future<void> _loadOptions() async {
    setState(() => _isLoadingOptions = true);
    try {
      final fullService = await HomeService().getServiceById(widget.service.id);
      if (mounted && fullService.options.isNotEmpty) {
        setState(() {
          _options = fullService.options;
          _isLoadingOptions = false;
        });
        return;
      }
    } catch (_) {}
    if (mounted) {
      setState(() => _isLoadingOptions = false);
    }
  }

  Widget _buildServiceIcon(BuildContext context) {
    final iconUrl = widget.service.iconUrl;
    if (iconUrl != null && iconUrl.isNotEmpty) {
      final resolved = ApiConstants.resolveAssetUrl(iconUrl);
      if (resolved != null && resolved.isNotEmpty) {
        if (resolved.toLowerCase().contains('.svg')) {
          return Container(
            width: 52,
            height: 52,
            padding: const EdgeInsets.all(11),
            decoration: BoxDecoration(
              color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
              borderRadius: BorderRadius.circular(14),
            ),
            child: SvgPicture.network(
              resolved,
              fit: BoxFit.contain,
              placeholderBuilder: (_) => const SizedBox(),
            ),
          );
        }
        return Container(
          width: 52,
          height: 52,
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
            borderRadius: BorderRadius.circular(14),
          ),
          child: Image.network(
            resolved,
            fit: BoxFit.contain,
            errorBuilder: (_, _, _) => Icon(
              Iconsax.health,
              color: context.isDark ? Colors.white : AppColors.primary,
              size: 26,
            ),
          ),
        );
      }
    }

    return Container(
      width: 52,
      height: 52,
      decoration: BoxDecoration(
        color: context.isDark ? const Color(0xFF451A1A) : AppColors.primaryLight,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Icon(
        Iconsax.health,
        color: context.isDark ? Colors.white : AppColors.primary,
        size: 26,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final bottomPad = MediaQuery.of(context).padding.bottom;
    final hasImageUrl = widget.service.imageUrl != null && widget.service.imageUrl!.isNotEmpty;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Thông tin dịch vụ' : 'Service Info'),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── Hero Image Banner (nếu có ảnh dịch vụ) ──
                  if (hasImageUrl) ...[
                    Container(
                      width: double.infinity,
                      height: 190,
                      margin: const EdgeInsets.only(bottom: 16),
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(20),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: context.isDark ? 0.25 : 0.08),
                            blurRadius: 12,
                            offset: const Offset(0, 4),
                          ),
                        ],
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(20),
                        child: Stack(
                          fit: StackFit.expand,
                          children: [
                            Image.network(
                              ApiConstants.resolveAssetUrl(widget.service.imageUrl)!,
                              fit: BoxFit.cover,
                              errorBuilder: (_, _, _) => Container(
                                color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                                child: Center(
                                  child: Icon(Iconsax.health, size: 48, color: AppColors.primary.withValues(alpha: 0.4)),
                                ),
                              ),
                            ),
                            Container(
                              decoration: BoxDecoration(
                                gradient: LinearGradient(
                                  begin: Alignment.topCenter,
                                  end: Alignment.bottomCenter,
                                  colors: [
                                    Colors.transparent,
                                    Colors.black.withValues(alpha: 0.35),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],

                  // ── Header card ─────────────────────────────────────────
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: context.divider),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.04),
                          blurRadius: 8,
                          offset: const Offset(0, 3),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            _buildServiceIcon(context),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    widget.service.name,
                                    style: TextStyle(
                                      fontSize: 17,
                                      fontWeight: FontWeight.w800,
                                      color: context.textPrimary,
                                      height: 1.3,
                                    ),
                                  ),
                                  if (widget.service.note != null && widget.service.note!.isNotEmpty) ...[
                                    const SizedBox(height: 4),
                                    Text(
                                      widget.service.note!,
                                      style: TextStyle(
                                        fontSize: 12,
                                        color: context.textSecondary,
                                        fontStyle: FontStyle.italic,
                                      ),
                                    ),
                                  ],
                                  if (widget.service.durationMinutes > 0) ...[
                                    const SizedBox(height: 8),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                      decoration: BoxDecoration(
                                        color: AppColors.primary.withValues(alpha: context.isDark ? 0.2 : 0.1),
                                        borderRadius: BorderRadius.circular(6),
                                      ),
                                      child: Row(
                                        mainAxisSize: MainAxisSize.min,
                                        children: [
                                          const Icon(Iconsax.clock, size: 12, color: AppColors.primary),
                                          const SizedBox(width: 4),
                                          Text(
                                            '${widget.service.durationMinutes} phút',
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
                                ],
                              ),
                            ),
                          ],
                        ),
                        if (widget.service.price.isNotEmpty && widget.service.price != '0đ') ...[
                          const SizedBox(height: 16),
                          Divider(color: context.divider, height: 1),
                          const SizedBox(height: 14),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                _options.isNotEmpty
                                    ? (isVi ? 'Giá khởi điểm' : 'Starting From')
                                    : (isVi ? 'Chi phí khám tham khảo' : 'Consultation Cost'),
                                style: TextStyle(
                                  fontSize: 13.5,
                                  color: context.textSecondary,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              Text(
                                _options.isNotEmpty
                                    ? 'Từ ${widget.service.price}'
                                    : widget.service.price,
                                style: const TextStyle(
                                  fontSize: 17,
                                  fontWeight: FontWeight.w800,
                                  color: AppColors.primary,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── Tùy chọn & Bảng giá chi tiết (Service Options) ────────
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(18),
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: context.divider),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.03),
                          blurRadius: 8,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(7),
                              decoration: BoxDecoration(
                                color: AppColors.primary.withValues(alpha: context.isDark ? 0.2 : 0.1),
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: const Icon(Iconsax.receipt_2, size: 18, color: AppColors.primary),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    isVi ? 'Tùy chọn & Bảng giá chi tiết' : 'Options & Pricing Details',
                                    style: TextStyle(
                                      fontSize: 14.5,
                                      fontWeight: FontWeight.w800,
                                      color: context.textPrimary,
                                    ),
                                  ),
                                  Text(
                                    isVi ? 'Các phân loại / vật liệu điều trị' : 'Treatment types & materials',
                                    style: TextStyle(
                                      fontSize: 11.5,
                                      color: context.textSecondary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            if (_options.isNotEmpty)
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                decoration: BoxDecoration(
                                  color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Text(
                                  '${_options.length} ${isVi ? 'gói' : 'types'}',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w700,
                                    color: context.textSecondary,
                                  ),
                                ),
                              ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        Divider(color: context.divider, height: 1),
                        const SizedBox(height: 10),
                        if (_isLoadingOptions)
                          const Padding(
                            padding: EdgeInsets.symmetric(vertical: 20),
                            child: Center(
                              child: SizedBox(
                                width: 24,
                                height: 24,
                                child: CircularProgressIndicator(strokeWidth: 2.5, color: AppColors.primary),
                              ),
                            ),
                          )
                        else if (_options.isEmpty)
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 12),
                            child: Row(
                              children: [
                                Icon(Iconsax.info_circle, size: 16, color: context.textSecondary),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    isVi
                                        ? 'Dịch vụ này áp dụng theo chỉ định trực tiếp từ nha sĩ khi thăm khám.'
                                        : 'Specific treatment cost will be advised directly by your dentist.',
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: context.textSecondary,
                                      height: 1.4,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          )
                        else
                          ListView.separated(
                            shrinkWrap: true,
                            physics: const NeverScrollableScrollPhysics(),
                            itemCount: _options.length,
                            separatorBuilder: (_, _) => Divider(color: context.divider.withValues(alpha: 0.6), height: 16),
                            itemBuilder: (context, index) {
                              final opt = _options[index];
                              final unitText = opt.unit.isNotEmpty ? ' / ${opt.unit}' : '';
                              return Padding(
                                padding: const EdgeInsets.symmetric(vertical: 4),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.center,
                                  children: [
                                    Container(
                                      width: 26,
                                      height: 26,
                                      decoration: BoxDecoration(
                                        color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                                        shape: BoxShape.circle,
                                      ),
                                      child: Center(
                                        child: Text(
                                          '${index + 1}',
                                          style: TextStyle(
                                            fontSize: 11,
                                            fontWeight: FontWeight.w800,
                                            color: context.textPrimary,
                                          ),
                                        ),
                                      ),
                                    ),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            opt.name,
                                            style: TextStyle(
                                              fontSize: 13.5,
                                              fontWeight: FontWeight.w700,
                                              color: context.textPrimary,
                                              height: 1.3,
                                            ),
                                          ),
                                          if (opt.unit.isNotEmpty)
                                            Text(
                                              '${isVi ? 'Đơn vị tính' : 'Unit'}: ${opt.unit}',
                                              style: TextStyle(
                                                fontSize: 11.5,
                                                color: context.textSecondary,
                                              ),
                                            ),
                                        ],
                                      ),
                                    ),
                                    const SizedBox(width: 8),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                                      decoration: BoxDecoration(
                                        color: AppColors.primary.withValues(alpha: context.isDark ? 0.15 : 0.08),
                                        borderRadius: BorderRadius.circular(8),
                                      ),
                                      child: Text(
                                        '${opt.formattedPrice}$unitText',
                                        style: const TextStyle(
                                          fontSize: 13,
                                          fontWeight: FontWeight.w800,
                                          color: AppColors.primary,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              );
                            },
                          ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── Description ─────────────────────────────────────────
                  if (widget.service.description.isNotEmpty) ...[
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(18),
                      decoration: BoxDecoration(
                        color: context.card,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              const Icon(Iconsax.document_text, size: 18, color: AppColors.primary),
                              const SizedBox(width: 8),
                              Text(
                                isVi ? 'Mô tả dịch vụ' : 'Service Description',
                                style: TextStyle(
                                  fontSize: 14.5,
                                  fontWeight: FontWeight.w800,
                                  color: context.textPrimary,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          Text(
                            widget.service.description,
                            style: TextStyle(
                              fontSize: 13.5,
                              color: context.textSecondary,
                              height: 1.6,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // ── Notice ──────────────────────────────────────────────
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: context.isDark ? const Color(0xFF451A1A) : AppColors.primaryLight,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: context.isDark ? Colors.transparent : AppColors.primary.withValues(alpha: 0.2),
                      ),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Iconsax.info_circle,
                            color: context.isDark ? Colors.white : AppColors.primary, size: 18),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            isVi
                                ? 'Vui lòng đến trước giờ hẹn 10-15 phút. Đội ngũ bác sĩ sẽ tiến hành khám lâm sàng và tư vấn chi tiết phương án phù hợp nhất cho bạn.'
                                : 'Please arrive 10-15 minutes prior to your appointment. Our dental team will examine and advise the most suitable treatment for you.',
                            style: TextStyle(
                              fontSize: 12,
                              color: context.isDark ? Colors.white : AppColors.primary,
                              height: 1.5,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),

          // ── Bottom: Quay lại để chọn ────────────────────────────────────
          Container(
            padding: EdgeInsets.fromLTRB(16, 12, 16, 12 + bottomPad),
            decoration: BoxDecoration(
              color: context.card,
              border: Border(top: BorderSide(color: context.divider)),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.06),
                  blurRadius: 12,
                  offset: const Offset(0, -4),
                ),
              ],
            ),
            child: SizedBox(
              width: double.infinity,
              height: 52,
              child: ElevatedButton(
                onPressed: () => Navigator.of(context).pop(),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                child: Text(
                  isVi ? 'Quay lại để chọn' : 'Back to select',
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

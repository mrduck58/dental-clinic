import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';

class ServiceCard extends StatelessWidget {
  final ServiceModel service;
  final int index;

  const ServiceCard({super.key, required this.service, required this.index});

  static const _styles = [
    ([Color(0xFFDC2626), Color(0xFFF87171)], Iconsax.health),
    ([Color(0xFFF59E0B), Color(0xFFFCD34D)], Iconsax.flash_1),
    ([Color(0xFF0284C7), Color(0xFF38BDF8)], Iconsax.element_4),
    ([Color(0xFF16A34A), Color(0xFF4ADE80)], Iconsax.shield_tick),
    ([Color(0xFF7C3AED), Color(0xFFA78BFA)], Iconsax.scissor),
    ([Color(0xFFEA580C), Color(0xFFFB923C)], Iconsax.medal_star),
  ];

  @override
  Widget build(BuildContext context) {
    final style = _styles[index % _styles.length];
    final gradientColors = style.$1;
    final icon = style.$2;
    final quickInfo = '${service.durationText} • Tại phòng khám';

    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(18),
      shadowColor: Colors.black.withValues(alpha: 0.05),
      elevation: 2,
      child: InkWell(
        onTap: () {
          context.push(
            AppRoutes.bookingServiceDetail,
            extra: ServiceInfo(
              id: service.id,
              name: service.name,
              description: service.description,
              price: service.formattedPrice,
              note: '${service.durationText} • Khám sơ bộ miễn phí',
            ),
          );
        },
        borderRadius: BorderRadius.circular(18),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: gradientColors,
                  ),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(icon, color: Colors.white, size: 22),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      service.name,
                      style: const TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 15,
                        fontWeight: FontWeight.w800,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      quickInfo,
                      style: const TextStyle(
                        color: AppColors.textMuted,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    service.formattedPrice,
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontSize: 14,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Icon(
                    Iconsax.arrow_right_3,
                    color: AppColors.textMuted,
                    size: 14,
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class ServicesGrid extends StatelessWidget {
  final List<ServiceModel> services;

  const ServicesGrid({super.key, required this.services});

  @override
  Widget build(BuildContext context) {
    final rows = <Widget>[];
    for (var i = 0; i < services.length; i += 2) {
      if (i > 0) rows.add(const SizedBox(height: 12));
      rows.add(
        IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(child: ServiceCard(service: services[i], index: i)),
              const SizedBox(width: 12),
              Expanded(
                child: i + 1 < services.length
                    ? ServiceCard(service: services[i + 1], index: i + 1)
                    : const SizedBox(),
              ),
            ],
          ),
        ),
      );
    }
    return Column(children: rows);
  }
}

import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';

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
      borderRadius: BorderRadius.circular(20),
      shadowColor: Colors.black.withValues(alpha: 0.07),
      elevation: 3,
      child: InkWell(
        onTap: () {},
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 14, 14, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: gradientColors,
                      ),
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: Icon(icon, color: Colors.white, size: 20),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          service.name,
                          style: const TextStyle(
                            color: AppColors.textPrimary,
                            fontSize: 13,
                            fontWeight: FontWeight.w800,
                            height: 1.25,
                          ),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 3),
                        Text(
                          quickInfo,
                          style: const TextStyle(
                            color: AppColors.textMuted,
                            fontSize: 11,
                            height: 1.3,
                          ),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      service.formattedPrice,
                      style: const TextStyle(
                        color: AppColors.primary,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Container(
                    width: 28,
                    height: 28,
                    decoration: BoxDecoration(
                      color: AppColors.primaryLight,
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: const Icon(
                      Iconsax.arrow_right_3,
                      color: AppColors.primary,
                      size: 15,
                    ),
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

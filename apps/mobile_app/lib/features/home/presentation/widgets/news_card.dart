import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';

class NewsCard extends StatelessWidget {
  final PostModel post;

  const NewsCard({super.key, required this.post});

  Color _tagColor() => switch (post.category.toLowerCase()) {
        'nha khoa' || 'dental' => AppColors.primary,
        'khuyến mãi' || 'promotion' => AppColors.accent,
        'sức khoẻ' || 'health' || 'sức khỏe' => AppColors.secondary,
        _ => AppColors.textSecondary,
      };

  Color _tagBg() => switch (post.category.toLowerCase()) {
        'nha khoa' || 'dental' => AppColors.primaryLight,
        'khuyến mãi' || 'promotion' => AppColors.accentLight,
        'sức khoẻ' || 'health' || 'sức khỏe' => AppColors.secondaryLight,
        _ => const Color(0xFFF1F5F9),
      };

  @override
  Widget build(BuildContext context) {
    final tagColor = _tagColor();
    final tagBg = _tagBg();

    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(20),
      shadowColor: Colors.black.withValues(alpha: 0.07),
      elevation: 4,
      child: InkWell(
        onTap: () {},
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(14),
                child: post.thumbnailUrl != null
                    ? Image.network(
                        post.thumbnailUrl!,
                        width: 90,
                        height: 90,
                        fit: BoxFit.cover,
                        errorBuilder: (_, _, _) => _imagePlaceholder(),
                      )
                    : _imagePlaceholder(),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                      decoration: BoxDecoration(
                        color: tagBg,
                        borderRadius: BorderRadius.circular(999),
                      ),
                      child: Text(
                        post.category,
                        style: TextStyle(
                          color: tagColor,
                          fontSize: 13,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(height: 7),
                    Text(
                      post.title,
                      style: const TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 15,
                        fontWeight: FontWeight.w700,
                        height: 1.35,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        const Icon(Iconsax.clock, size: 13, color: AppColors.textMuted),
                        const SizedBox(width: 4),
                        Expanded(
                          child: Text(
                            '${post.formattedDate} · ${post.readTimeText} đọc',
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 13,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _imagePlaceholder() {
    return Container(
      width: 90,
      height: 90,
      color: AppColors.primaryLight,
      child: const Icon(Iconsax.image, color: AppColors.primary, size: 32),
    );
  }
}

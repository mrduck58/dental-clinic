import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class HomeSearchBar extends StatelessWidget {
  const HomeSearchBar({super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(18, 4, 18, 14),
      child: Container(
        height: 54,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(999),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.07),
              blurRadius: 12,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: Row(
          children: [
            const SizedBox(width: 20),
            const Expanded(
              child: Text(
                'Tìm kiếm dịch vụ, bác sĩ...',
                style: TextStyle(color: AppColors.textMuted, fontSize: 16),
              ),
            ),
            Container(
              width: 40,
              height: 40,
              margin: const EdgeInsets.only(right: 7),
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(999),
              ),
              child: const Icon(Iconsax.search_normal, size: 20, color: Colors.white),
            ),
          ],
        ),
      ),
    );
  }
}

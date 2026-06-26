import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class HomeSearchBar extends StatelessWidget {
  const HomeSearchBar({super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(18, 4, 18, 14),
      child: Container(
        height: 54,
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: BorderRadius.circular(999),
          border: Border.all(color: context.divider, width: 1),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.07),
              blurRadius: 12,
              offset: Offset(0, 3),
            ),
          ],
        ),
        child: Row(
          children: [
            SizedBox(width: 20),
            Expanded(
              child: Text(
                context.l10n('search_hint_home'),
                style: TextStyle(color: context.textMuted, fontSize: 15),
              ),
            ),
            Container(
              width: 40,
              height: 40,
              margin: EdgeInsets.only(right: 7),
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(999),
              ),
              child: Icon(Iconsax.search_normal, size: 20, color: Colors.white),
            ),
          ],
        ),
      ),
    );
  }
}

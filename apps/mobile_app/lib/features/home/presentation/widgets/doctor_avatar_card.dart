import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';

class DoctorAvatarCard extends StatelessWidget {
  final DoctorModel doctor;

  const DoctorAvatarCard({super.key, required this.doctor});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 84,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ClipOval(
            child: doctor.profilePictureUrl != null
                ? Image.network(
                    doctor.profilePictureUrl!,
                    width: 76,
                    height: 76,
                    fit: BoxFit.cover,
                    errorBuilder: (_, _, _) => _placeholder(),
                  )
                : _placeholder(),
          ),
          const SizedBox(height: 8),
          Text(
            doctor.fullName,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 13,
              fontWeight: FontWeight.w700,
              height: 1.3,
            ),
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
          if (doctor.specialty != null) ...[
            const SizedBox(height: 2),
            Text(
              doctor.specialty!,
              style: const TextStyle(
                color: AppColors.textMuted,
                fontSize: 11,
              ),
              textAlign: TextAlign.center,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ],
      ),
    );
  }

  Widget _placeholder() {
    return Container(
      width: 76,
      height: 76,
      color: AppColors.primaryLight,
      child: const Icon(Iconsax.user, color: AppColors.primary, size: 34),
    );
  }
}

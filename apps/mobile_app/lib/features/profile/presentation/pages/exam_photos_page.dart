import 'package:flutter/material.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

/// Ảnh chụp chiếu lúc khám (X-quang...) — chỉ đọc, đúng những ảnh bác sĩ đã tải lên ở tab "Khám"
/// trên app quản lý (apps/admin_website). Không có máy tích hợp nên chỉ là ảnh chụp tay kèm ghi chú.
class ExamPhotosPage extends StatelessWidget {
  final MedicalHistoryEvent event;
  const ExamPhotosPage({super.key, required this.event});

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final photos = event.photos;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: Text(
          isVi ? 'Ảnh chụp chiếu' : 'Exam Photos',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w800, fontSize: 18),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: photos.isEmpty
          ? Center(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 32),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Iconsax.gallery, size: 40, color: context.textMuted),
                    const SizedBox(height: 12),
                    Text(
                      isVi ? 'Chưa có ảnh chụp chiếu cho buổi khám này.' : 'No exam photos for this visit.',
                      style: TextStyle(color: context.textMuted),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
            )
          : GridView.builder(
              padding: const EdgeInsets.all(20),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                crossAxisSpacing: 14,
                mainAxisSpacing: 18,
                childAspectRatio: 0.82,
              ),
              itemCount: photos.length,
              itemBuilder: (context, index) {
                final photo = photos[index];
                final url = ApiConstants.resolveAssetUrl(photo.url);
                return GestureDetector(
                  onTap: () => _openFullScreen(context, url, photo.note),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(16),
                          child: Container(
                            decoration: BoxDecoration(
                              border: Border.all(color: context.divider),
                            ),
                            child: url == null
                                ? _brokenImage(context)
                                : Image.network(
                                    url,
                                    fit: BoxFit.cover,
                                    width: double.infinity,
                                    height: double.infinity,
                                    errorBuilder: (_, _, _) => _brokenImage(context),
                                  ),
                          ),
                        ),
                      ),
                      if (photo.note != null && photo.note!.isNotEmpty) ...[
                        const SizedBox(height: 6),
                        Text(
                          photo.note!,
                          style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w600, color: context.textSecondary),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ],
                  ),
                );
              },
            ),
    );
  }

  Widget _brokenImage(BuildContext context) => Container(
        color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
        child: Center(
          child: Icon(Iconsax.gallery_slash, size: 32, color: AppColors.primary.withValues(alpha: 0.4)),
        ),
      );

  void _openFullScreen(BuildContext context, String? url, String? note) {
    if (url == null) return;
    Navigator.of(context).push(
      PageRouteBuilder(
        opaque: false,
        barrierColor: Colors.black.withValues(alpha: 0.92),
        pageBuilder: (context, _, _) => _FullScreenPhotoViewer(url: url, note: note),
      ),
    );
  }
}

class _FullScreenPhotoViewer extends StatelessWidget {
  final String url;
  final String? note;
  const _FullScreenPhotoViewer({required this.url, this.note});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: SafeArea(
        child: Stack(
          children: [
            Center(
              child: InteractiveViewer(
                minScale: 1,
                maxScale: 4,
                child: Image.network(
                  url,
                  errorBuilder: (_, _, _) => const Icon(Iconsax.gallery_slash, color: Colors.white54, size: 48),
                ),
              ),
            ),
            if (note != null && note!.isNotEmpty)
              Positioned(
                left: 20,
                right: 20,
                bottom: 24,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.55),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    note!,
                    style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w600),
                    textAlign: TextAlign.center,
                  ),
                ),
              ),
            Positioned(
              top: 8,
              right: 8,
              child: IconButton(
                icon: const Icon(Icons.close_rounded, color: Colors.white, size: 28),
                onPressed: () => Navigator.of(context).pop(),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';

class PostDetailPage extends StatefulWidget {
  final PostModel post;

  const PostDetailPage({super.key, required this.post});

  @override
  State<PostDetailPage> createState() => _PostDetailPageState();
}

class _PostDetailPageState extends State<PostDetailPage> {
  final _homeService = HomeService();
  List<PostModel> _relatedPosts = [];
  bool _isLoadingRelated = true;

  @override
  void initState() {
    super.initState();
    _loadRelatedPosts();
  }

  Future<void> _loadRelatedPosts() async {
    setState(() => _isLoadingRelated = true);
    try {
      final all = await _homeService.getPosts();
      // Filter out the current post
      final list = all.where((p) => p.id != widget.post.id).toList();
      setState(() {
        _relatedPosts = list.take(3).toList();
        _isLoadingRelated = false;
      });
    } catch (_) {
      setState(() => _isLoadingRelated = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final post = widget.post;
    
    // Split content by paragraphs
    final paragraphs = post.content.split(RegExp(r'\n+')).where((p) => p.trim().isNotEmpty).toList();

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: const Text(
          'Chi tiết bài viết',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Iconsax.share, color: AppColors.textPrimary),
            onPressed: () {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: const Text('Đã sao chép liên kết bài viết!'),
                  backgroundColor: AppColors.success,
                  behavior: SnackBarBehavior.floating,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
              );
            },
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(
            color: AppColors.divider,
            height: 1,
          ),
        ),
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Thumbnail Image
            if (post.thumbnailUrl != null)
              Image.network(
                post.thumbnailUrl!,
                width: double.infinity,
                height: 230,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => _placeholderImage(),
              )
            else
              _placeholderImage(),
            
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Category tag & Date
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.primaryLight,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          post.category.toUpperCase(),
                          style: const TextStyle(
                            color: AppColors.primary,
                            fontSize: 10,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      const Icon(Iconsax.calendar_1, size: 14, color: AppColors.textMuted),
                      const SizedBox(width: 4),
                      Text(
                        post.formattedDate,
                        style: const TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // Title
                  Text(
                    post.title,
                    style: const TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                      height: 1.35,
                    ),
                  ),
                  const SizedBox(height: 8),

                  // Author
                  Row(
                    children: [
                      const Icon(Iconsax.user, size: 14, color: AppColors.textMuted),
                      const SizedBox(width: 6),
                      Text(
                        'Tác giả: ${post.author}',
                        style: const TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),
                  const Divider(color: AppColors.divider, height: 1),
                  const SizedBox(height: 24),

                  // Paragraphs Content
                  ...paragraphs.map((p) => Padding(
                        padding: const EdgeInsets.only(bottom: 16.0),
                        child: Text(
                          p.trim(),
                          style: const TextStyle(
                            fontSize: 14,
                            color: AppColors.textSecondary,
                            height: 1.6,
                          ),
                        ),
                      )),

                  const SizedBox(height: 8),

                  // Premium Blockquote / Callout Card
                  Container(
                    margin: const EdgeInsets.symmetric(vertical: 16),
                    padding: const EdgeInsets.all(20),
                    decoration: const BoxDecoration(
                      color: Color(0xFFFEF2F2),
                      border: Border(
                        left: BorderSide(color: AppColors.primary, width: 4),
                      ),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Icon(
                          Iconsax.quote_up5,
                          color: AppColors.primary,
                          size: 24,
                        ),
                        SizedBox(height: 10),
                        Text(
                          '"Chăm sóc sức khỏe răng miệng chủ động từ sớm không chỉ mang lại nụ cười rạng rỡ tự tin mà còn là chìa khóa vàng để bảo vệ sức khỏe tổng thể của bạn và gia đình."',
                          style: TextStyle(
                            fontSize: 14,
                            color: AppColors.textPrimary,
                            fontWeight: FontWeight.w700,
                            fontStyle: FontStyle.italic,
                            height: 1.5,
                          ),
                        ),
                        SizedBox(height: 8),
                        Text(
                          '— BAN CHUYÊN MÔN NHA KHOA DENTALCARE',
                          style: TextStyle(
                            fontSize: 11,
                            color: AppColors.textSecondary,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 32),
                  const Divider(color: AppColors.divider, height: 1),
                  const SizedBox(height: 32),

                  // Related Articles Title
                  const Text(
                    'Bài viết liên quan',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Related Articles List
                  _isLoadingRelated
                      ? const Center(
                          child: CircularProgressIndicator(color: AppColors.primary),
                        )
                      : _relatedPosts.isEmpty
                          ? const Text(
                              'Không có bài viết liên quan.',
                              style: TextStyle(color: AppColors.textMuted, fontSize: 13),
                            )
                          : SizedBox(
                              height: 220,
                              child: ListView.separated(
                                scrollDirection: Axis.horizontal,
                                itemCount: _relatedPosts.length,
                                separatorBuilder: (_, _) => const SizedBox(width: 16),
                                itemBuilder: (context, index) {
                                  final relPost = _relatedPosts[index];
                                  return _buildRelatedPostCard(relPost);
                                },
                              ),
                            ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRelatedPostCard(PostModel post) {
    return GestureDetector(
      onTap: () => context.pushReplacement(AppRoutes.postDetail, extra: post),
      child: SizedBox(
        width: 180,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: post.thumbnailUrl != null
                  ? Image.network(
                      post.thumbnailUrl!,
                      height: 110,
                      width: 180,
                      fit: BoxFit.cover,
                      errorBuilder: (_, _, _) => _placeholderRelatedImage(),
                    )
                  : _placeholderRelatedImage(),
            ),
            const SizedBox(height: 8),
            // Category
            Text(
              post.category.toUpperCase(),
              style: const TextStyle(
                color: AppColors.primary,
                fontSize: 10,
                fontWeight: FontWeight.w800,
                letterSpacing: 0.5,
              ),
            ),
            const SizedBox(height: 4),
            // Title
            Text(
              post.title,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
                height: 1.35,
              ),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }

  Widget _placeholderImage() {
    return Container(
      height: 230,
      width: double.infinity,
      color: AppColors.primaryLight,
      child: const Icon(Iconsax.image, color: AppColors.primary, size: 64),
    );
  }

  Widget _placeholderRelatedImage() {
    return Container(
      height: 110,
      width: 180,
      color: AppColors.primaryLight,
      child: const Icon(Iconsax.image, color: AppColors.primary, size: 32),
    );
  }
}

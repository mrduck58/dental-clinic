import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';
import 'package:mobile_app/features/home/presentation/widgets/news_card.dart';

class PostsListPage extends StatefulWidget {
  const PostsListPage({super.key});

  @override
  State<PostsListPage> createState() => _PostsListPageState();
}

class _PostsListPageState extends State<PostsListPage> {
  final _homeService = HomeService();
  List<PostModel> _allPosts = [];
  List<PostModel> _filteredPosts = [];
  bool _isLoading = true;
  String _selectedCategory = 'Tất cả';

  final List<String> _categories = [
    'Tất cả',
    'Khuyến mãi',
    'Chăm sóc nha khoa',
    'Tin tức',
  ];

  @override
  void initState() {
    super.initState();
    _loadPosts();
  }

  Future<void> _loadPosts() async {
    setState(() => _isLoading = true);
    try {
      final list = await _homeService.getPosts();
      setState(() {
        _allPosts = list;
        _filterPosts();
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  void _filterPosts() {
    if (_selectedCategory == 'Tất cả') {
      _filteredPosts = _allPosts;
    } else {
      _filteredPosts = _allPosts.where((post) {
        // Map category tags to user selected tabs
        final cat = post.category.toLowerCase();
        if (_selectedCategory == 'Khuyến mãi') {
          return cat.contains('khuyến mãi') || cat.contains('promotion');
        }
        if (_selectedCategory == 'Chăm sóc nha khoa') {
          return cat.contains('nha khoa') || cat.contains('dental');
        }
        if (_selectedCategory == 'Tin tức') {
          return cat.contains('tin tức') || cat.contains('news');
        }
        return false;
      }).toList();
    }
  }

  void _onCategorySelected(String category) {
    setState(() {
      _selectedCategory = category;
      _filterPosts();
    });
  }

  String _getLocalizedCategoryName(String categoryKey, bool isVi) {
    if (categoryKey == 'Tất cả') return isVi ? 'Tất cả' : 'All';
    if (categoryKey == 'Khuyến mãi') return isVi ? 'Khuyến mãi' : 'Promotions';
    if (categoryKey == 'Chăm sóc nha khoa') return isVi ? 'Chăm sóc nha khoa' : 'Dental Care';
    if (categoryKey == 'Tin tức') return isVi ? 'Tin tức' : 'News';
    return categoryKey;
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Tin tức nổi bật' : 'Featured News',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(
            color: context.divider,
            height: 1,
          ),
        ),
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator(color: AppColors.primary))
          : Column(
              children: [
                // Category tabs row
                Container(
                  color: context.card,
                  height: 64,
                  child: ListView.builder(
                    scrollDirection: Axis.horizontal,
                    padding: EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                    itemCount: _categories.length,
                    itemBuilder: (context, index) {
                      final cat = _categories[index];
                      final active = cat == _selectedCategory;
                      return GestureDetector(
                        onTap: () => _onCategorySelected(cat),
                        child: AnimatedContainer(
                          duration: Duration(milliseconds: 180),
                          margin: EdgeInsets.only(right: 10),
                          padding: EdgeInsets.symmetric(horizontal: 18, vertical: 8),
                          decoration: BoxDecoration(
                            color: active ? AppColors.primary : (context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9)),
                            borderRadius: BorderRadius.circular(999),
                          ),
                          child: Center(
                            child: Text(
                              _getLocalizedCategoryName(cat, isVi),
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w800,
                                color: active ? Colors.white : context.textSecondary,
                              ),
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
                Divider(height: 1, color: context.divider),
                
                // Content area
                Expanded(
                  child: _filteredPosts.isEmpty
                      ? Center(
                          child: Text(
                            isVi ? 'Chưa có bài viết nào trong mục này.' : 'No articles found in this category.',
                            style: TextStyle(color: context.textMuted),
                          ),
                        )
                      : CustomScrollView(
                          slivers: [
                            // Featured Banner (First Post in list)
                            SliverToBoxAdapter(
                              child: _buildFeaturedBanner(_filteredPosts.first),
                            ),
                            
                            // Other Posts Label
                            if (_filteredPosts.length > 1)
                              SliverToBoxAdapter(
                                child: Padding(
                                  padding: EdgeInsets.fromLTRB(24, 24, 24, 12),
                                  child: Text(
                                    isVi ? 'Bài viết mới nhất' : 'Latest Articles',
                                    style: TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.w800,
                                      color: context.textPrimary,
                                    ),
                                  ),
                                ),
                              ),
                              
                            // Remaining list
                            SliverPadding(
                              padding: EdgeInsets.fromLTRB(24, 0, 24, 40),
                              sliver: SliverList(
                                delegate: SliverChildBuilderDelegate(
                                  (context, index) {
                                    // Skip first one as it is in the banner
                                    return Padding(
                                      padding: EdgeInsets.only(bottom: 14),
                                      child: NewsCard(post: _filteredPosts[index + 1]),
                                    );
                                  },
                                  childCount: _filteredPosts.length - 1,
                                ),
                              ),
                            ),
                          ],
                        ),
                ),
              ],
            ),
    );
  }

  Widget _buildFeaturedBanner(PostModel post) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final categoryLabel = _getLocalizedCategoryName(post.category, isVi);
    final rawMin = post.readTimeText.replaceAll(RegExp(r'\D'), '');
    final readTimeLabel = isVi ? '$rawMin phút đọc' : '$rawMin min read';

    return GestureDetector(
      onTap: () => context.push(AppRoutes.postDetail, extra: post),
      child: Container(
        margin: EdgeInsets.fromLTRB(24, 24, 24, 0),
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: context.divider),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
              blurRadius: 16,
              offset: Offset(0, 6),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top Image
            ClipRRect(
              borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
              child: post.thumbnailUrl != null
                  ? Image.network(
                      post.thumbnailUrl!,
                      height: 180,
                      width: double.infinity,
                      fit: BoxFit.cover,
                      errorBuilder: (_, _, _) => _placeholderBannerImage(),
                    )
                  : _placeholderBannerImage(),
            ),
            
            // Post Info
            Padding(
              padding: EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: context.primaryLight,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          categoryLabel.toUpperCase(),
                          style: TextStyle(
                            color: AppColors.primary,
                            fontSize: 10,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                      SizedBox(width: 8),
                      Text(
                        readTimeLabel,
                        style: TextStyle(
                          color: context.textMuted,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 12),
                  Text(
                    post.title,
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: context.textPrimary,
                      height: 1.35,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  SizedBox(height: 8),
                  Text(
                    post.content.length > 120 
                        ? '${post.content.substring(0, 120).trim()}...' 
                        : post.content,
                    style: TextStyle(
                      color: context.textSecondary,
                      fontSize: 13,
                      height: 1.45,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _placeholderBannerImage() {
    return Container(
      height: 180,
      width: double.infinity,
      color: context.primaryLight,
      child: Icon(Iconsax.image, color: AppColors.primary, size: 48),
    );
  }
}

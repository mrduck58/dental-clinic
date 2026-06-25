import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';
import 'package:mobile_app/features/home/presentation/widgets/service_card.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';

class ServicesListPage extends StatefulWidget {
  const ServicesListPage({super.key});

  @override
  State<ServicesListPage> createState() => _ServicesListPageState();
}

class _ServicesListPageState extends State<ServicesListPage> {
  final _homeService = HomeService();
  final _searchCtrl = TextEditingController();

  List<ServiceModel> _allServices = [];
  List<ServiceModel> _filteredServices = [];
  bool _isLoading = true;
  String _selectedCategory = 'Tất cả';
  String _searchQuery = '';

  final List<String> _categories = const [
    'Tất cả',
    'Chỉnh nha',
    'Tổng quát',
    'Thẩm mỹ',
  ];

  @override
  void initState() {
    super.initState();
    _loadServices();
    _searchCtrl.addListener(() {
      setState(() {
        _searchQuery = _searchCtrl.text;
        _filterServices();
      });
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadServices() async {
    setState(() => _isLoading = true);
    try {
      final list = await _homeService.getServices();
      setState(() {
        _allServices = list;
        _filterServices();
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  String _getServiceCategory(ServiceModel service) {
    final name = service.name.toLowerCase();
    if (name.contains('niềng') || name.contains('invisalign') || name.contains('chỉnh nha')) {
      return 'Chỉnh nha';
    }
    if (name.contains('tẩy trắng') || name.contains('sứ') || name.contains('thẩm mỹ') || name.contains('tạo hình')) {
      return 'Thẩm mỹ';
    }
    return 'Tổng quát';
  }

  void _filterServices() {
    List<ServiceModel> temp = _allServices;

    // Filter by category
    if (_selectedCategory != 'Tất cả') {
      temp = temp.where((s) => _getServiceCategory(s) == _selectedCategory).toList();
    }

    // Filter by search query
    if (_searchQuery.trim().isNotEmpty) {
      final q = _searchQuery.toLowerCase();
      temp = temp.where((s) => s.name.toLowerCase().contains(q) || s.description.toLowerCase().contains(q)).toList();
    }

    setState(() {
      _filteredServices = temp;
    });
  }

  void _onCategorySelected(String category) {
    setState(() {
      _selectedCategory = category;
      _filterServices();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: const Text(
          'Dịch vụ của chúng tôi',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(
            color: AppColors.divider,
            height: 1,
          ),
        ),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : Column(
              children: [
                // Search Bar at the top
                Container(
                  color: Colors.white,
                  padding: const EdgeInsets.fromLTRB(24, 16, 24, 8),
                  child: Container(
                    decoration: BoxDecoration(
                      color: const Color(0xFFF1F5F9),
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: TextField(
                      controller: _searchCtrl,
                      style: const TextStyle(fontSize: 14, color: AppColors.textPrimary),
                      decoration: const InputDecoration(
                        hintText: 'Tìm kiếm dịch vụ nha khoa...',
                        hintStyle: TextStyle(color: AppColors.textMuted, fontSize: 13),
                        prefixIcon: Icon(Iconsax.search_normal, color: AppColors.textMuted, size: 18),
                        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                        border: InputBorder.none,
                      ),
                    ),
                  ),
                ),

                // Category selector tabs row
                Container(
                  color: Colors.white,
                  height: 64,
                  child: ListView.builder(
                    scrollDirection: Axis.horizontal,
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                    itemCount: _categories.length,
                    itemBuilder: (context, index) {
                      final cat = _categories[index];
                      final active = cat == _selectedCategory;
                      return GestureDetector(
                        onTap: () => _onCategorySelected(cat),
                        child: AnimatedContainer(
                          duration: const Duration(milliseconds: 180),
                          margin: const EdgeInsets.only(right: 10),
                          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 8),
                          decoration: BoxDecoration(
                            color: active ? AppColors.primary : const Color(0xFFF1F5F9),
                            borderRadius: BorderRadius.circular(999),
                          ),
                          child: Center(
                            child: Text(
                              cat,
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w800,
                                color: active ? Colors.white : AppColors.textSecondary,
                              ),
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
                const Divider(height: 1, color: AppColors.divider),

                // Services List
                Expanded(
                  child: _filteredServices.isEmpty
                      ? const Center(
                          child: Text(
                            'Chưa có dịch vụ nào phù hợp.',
                            style: TextStyle(color: AppColors.textMuted),
                          ),
                        )
                      : CustomScrollView(
                          slivers: [
                            // Featured Banner (First Service in list)
                            SliverToBoxAdapter(
                              child: _buildFeaturedServiceBanner(_filteredServices.first),
                            ),

                            // Other Services Header Label
                            if (_filteredServices.length > 1)
                              const SliverToBoxAdapter(
                                child: Padding(
                                  padding: EdgeInsets.fromLTRB(24, 24, 24, 12),
                                  child: Text(
                                    'Dịch vụ khác',
                                    style: TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.w800,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                ),
                              ),

                            // Remaining list below
                            SliverPadding(
                              padding: const EdgeInsets.fromLTRB(24, 0, 24, 40),
                              sliver: SliverList(
                                delegate: SliverChildBuilderDelegate(
                                  (context, index) {
                                    // Skip first one as it is in the banner
                                    return Padding(
                                      padding: const EdgeInsets.only(bottom: 14),
                                      child: ServiceCard(
                                        service: _filteredServices[index + 1],
                                        index: index + 1,
                                      ),
                                    );
                                  },
                                  childCount: _filteredServices.length - 1,
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

  Widget _buildFeaturedServiceBanner(ServiceModel service) {
    final cat = _getServiceCategory(service).toUpperCase();
    final quickInfo = '${service.durationText} • Tại phòng khám';

    return GestureDetector(
      onTap: () {
        context.push(
          AppRoutes.bookingServiceDetail,
          extra: ServiceInfo(
            id: service.id,
            name: service.name,
            description: service.description,
            price: service.formattedPrice,
            note: '$quickInfo • Khám sơ bộ miễn phí',
          ),
        );
      },
      child: Container(
        margin: const EdgeInsets.fromLTRB(24, 24, 24, 0),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: AppColors.divider),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
              blurRadius: 16,
              offset: const Offset(0, 6),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top Feature Header (Gradient banner with a dental shield icon)
            Container(
              height: 140,
              width: double.infinity,
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  colors: [Color(0xFFDC2626), Color(0xFFF87171)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
              ),
              child: Stack(
                alignment: Alignment.center,
                children: [
                  Positioned(
                    right: -20,
                    top: -20,
                    child: Icon(
                      Iconsax.health,
                      size: 180,
                      color: Colors.white.withValues(alpha: 0.08),
                    ),
                  ),
                  const Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        Iconsax.shield_tick,
                        color: Colors.white,
                        size: 44,
                      ),
                      SizedBox(height: 8),
                      Text(
                        'DỊCH VỤ NỔI BẬT NHẤT',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 12,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 1.5,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),

            // Service details
            Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: AppColors.primaryLight,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          cat,
                          style: const TextStyle(
                            color: AppColors.primary,
                            fontSize: 10,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                      Text(
                        service.formattedPrice,
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text(
                    service.name,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                      height: 1.35,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    service.description,
                    style: const TextStyle(
                      color: AppColors.textSecondary,
                      fontSize: 13,
                      height: 1.45,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 12),
                  const Divider(color: AppColors.divider, height: 1),
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          const Icon(Iconsax.clock, size: 14, color: AppColors.textMuted),
                          const SizedBox(width: 4),
                          Text(
                            quickInfo,
                            style: const TextStyle(
                              color: AppColors.textSecondary,
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                      const Icon(
                        Iconsax.arrow_right_3,
                        color: AppColors.primary,
                        size: 16,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
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

  final List<String> _categories = [
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

  String _getLocalizedCategoryName(String categoryKey, bool isVi) {
    if (categoryKey == 'Tất cả') return isVi ? 'Tất cả' : 'All';
    if (categoryKey == 'Chỉnh nha') return isVi ? 'Chỉnh nha' : 'Orthodontics';
    if (categoryKey == 'Tổng quát') return isVi ? 'Tổng quát' : 'General';
    if (categoryKey == 'Thẩm mỹ') return isVi ? 'Thẩm mỹ' : 'Cosmetic';
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
          context.l10n('our_services'),
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
                // Search Bar at the top
                Container(
                  color: context.card,
                  padding: EdgeInsets.fromLTRB(24, 16, 24, 8),
                  child: Container(
                    decoration: BoxDecoration(
                      color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: TextField(
                      controller: _searchCtrl,
                      style: TextStyle(fontSize: 14, color: context.textPrimary),
                      decoration: InputDecoration(
                        hintText: context.l10n('search_services'),
                        hintStyle: TextStyle(color: context.textMuted, fontSize: 13),
                        prefixIcon: Icon(Iconsax.search_normal, color: context.textMuted, size: 18),
                        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                        border: InputBorder.none,
                      ),
                    ),
                  ),
                ),

                // Category selector tabs row
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

                // Services List
                Expanded(
                  child: _filteredServices.isEmpty
                      ? Center(
                          child: Text(
                            context.l10n('no_matching_services'),
                            style: TextStyle(color: context.textMuted),
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
                              SliverToBoxAdapter(
                                child: Padding(
                                  padding: EdgeInsets.fromLTRB(24, 24, 24, 12),
                                  child: Text(
                                    context.l10n('other_services'),
                                    style: TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.w800,
                                      color: context.textPrimary,
                                    ),
                                  ),
                                ),
                              ),

                            // Remaining list below
                            SliverPadding(
                              padding: EdgeInsets.fromLTRB(24, 0, 24, 40),
                              sliver: SliverList(
                                delegate: SliverChildBuilderDelegate(
                                  (context, index) {
                                    // Skip first one as it is in the banner
                                    return Padding(
                                      padding: EdgeInsets.only(bottom: 14),
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
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final cat = _getLocalizedCategoryName(_getServiceCategory(service), isVi).toUpperCase();
    final hasCustomIcon = service.iconUrl != null && service.iconUrl!.isNotEmpty;
    final quickInfo = '${service.durationText} • ${isVi ? 'Tại phòng khám' : 'At Clinic'}';

    return GestureDetector(
      onTap: () {
        context.push(
          AppRoutes.bookingServiceDetail,
          extra: ServiceInfo(
            id: service.id,
            name: service.name,
            description: service.description,
            price: service.formattedPrice,
            note: '$quickInfo • ${isVi ? 'Khám sơ bộ miễn phí' : 'Free preliminary exam'}',
            imageUrl: service.imageUrl,
            iconUrl: service.iconUrl,
          ),
        );
      },
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
            // Top Feature Header (Gradient banner with a dental shield icon)
            Container(
              height: 140,
              width: double.infinity,
              decoration: BoxDecoration(
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
                  Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      if (hasCustomIcon)
                        Container(
                          width: 64,
                          height: 64,
                          padding: EdgeInsets.all(12),
                          decoration: BoxDecoration(color: Colors.white, shape: BoxShape.circle),
                          child: ClipOval(
                            child: Builder(
                              builder: (_) {
                                final resolved = ApiConstants.resolveAssetUrl(service.iconUrl)!;
                                if (resolved.toLowerCase().contains('.svg')) {
                                  return SvgPicture.network(
                                    resolved,
                                    fit: BoxFit.contain,
                                    placeholderBuilder: (_) => const SizedBox(),
                                  );
                                }
                                return Image.network(
                                  resolved,
                                  fit: BoxFit.contain,
                                  errorBuilder: (_, __, ___) => const Icon(Iconsax.shield_tick, color: AppColors.primary, size: 28),
                                );
                              },
                            ),
                          ),
                        )
                      else
                        Icon(
                          Iconsax.shield_tick,
                          color: Colors.white,
                          size: 44,
                        ),
                      SizedBox(height: 8),
                      Text(
                        context.l10n('featured_service_title'),
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
              padding: EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: context.primaryLight,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          cat,
                          style: TextStyle(
                            color: AppColors.primary,
                            fontSize: 10,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                      Text(
                        service.formattedPrice,
                        style: TextStyle(
                          color: AppColors.primary,
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 12),
                  Text(
                    service.name,
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: context.textPrimary,
                      height: 1.35,
                    ),
                  ),
                  SizedBox(height: 8),
                  Text(
                    service.description,
                    style: TextStyle(
                      color: context.textSecondary,
                      fontSize: 13,
                      height: 1.45,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  SizedBox(height: 12),
                  Divider(color: context.divider, height: 1),
                  SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          Icon(Iconsax.clock, size: 14, color: context.textMuted),
                          SizedBox(width: 4),
                          Text(
                            quickInfo,
                            style: TextStyle(
                              color: context.textSecondary,
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                      Icon(
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

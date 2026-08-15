import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/utils/money_formatter.dart';
import 'package:mobile_app/features/payment/data/payment_service.dart';

class PaymentHistoryPage extends StatefulWidget {
  final int initialTab;
  const PaymentHistoryPage({super.key, this.initialTab = 0});

  @override
  State<PaymentHistoryPage> createState() => _PaymentHistoryPageState();
}

class _PaymentHistoryPageState extends State<PaymentHistoryPage> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final _service = PaymentService();

  List<MyInvoiceDto> _invoices = [];
  bool _loading = true;
  String? _error;

  List<MyInvoiceDto> _history = [];
  bool _historyLoading = true;
  String? _historyError;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this, initialIndex: widget.initialTab.clamp(0, 1));
    _load();
    _loadHistory();
  }

  @override
  void didUpdateWidget(covariant PaymentHistoryPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.initialTab != oldWidget.initialTab) {
      _tabController.animateTo(widget.initialTab.clamp(0, 1));
    }
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final invoices = await _service.getMyInvoices();
      setState(() {
        _invoices = invoices;
        _loading = false;
      });
    } catch (e) {
      setState(() {
        _error = 'Không thể tải hóa đơn. Vui lòng thử lại.';
        _loading = false;
      });
    }
  }

  Future<void> _loadHistory() async {
    setState(() {
      _historyLoading = true;
      _historyError = null;
    });
    try {
      final history = await _service.getPaymentHistory();
      setState(() {
        _history = history;
        _historyLoading = false;
      });
    } catch (e) {
      setState(() {
        _historyError = 'Không thể tải lịch sử giao dịch. Vui lòng thử lại.';
        _historyLoading = false;
      });
    }
  }

  double _getOutstandingTotal() {
    return _invoices.fold(0.0, (sum, i) => sum + i.depositAmount);
  }

  String _formatMonth(int m, bool isVi) {
    if (isVi) return 'THG $m';
    final months = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];
    return months[m - 1];
  }

  Future<void> _payNow(MyInvoiceDto item) async {
    final paid = await context.push<bool>(AppRoutes.paymentGatewaySelect, extra: item);
    if (paid == true) {
      _load();
      _loadHistory();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final debtTotal = _getOutstandingTotal();

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Thanh toán & Công nợ' : 'Payments & Debt',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.bold,
            fontSize: 18,
          ),
        ),
        bottom: TabBar(
          controller: _tabController,
          labelColor: AppColors.primary,
          unselectedLabelColor: context.textSecondary,
          indicatorColor: AppColors.primary,
          indicatorWeight: 3,
          labelStyle: const TextStyle(fontWeight: FontWeight.w900, fontSize: 13, letterSpacing: 0.5),
          unselectedLabelStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
          tabs: [
            Tab(text: isVi ? 'CÔNG NỢ CHỜ' : 'OUTSTANDING DEBT'),
            Tab(text: isVi ? 'LỊCH SỬ GD' : 'PAYMENTS'),
          ],
        ),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : _error != null
              ? _buildErrorState(isVi)
              : TabBarView(
                  controller: _tabController,
                  children: [
                    _buildDebtTab(_invoices, debtTotal, isVi),
                    _buildPaymentsTab(isVi),
                  ],
                ),
    );
  }

  Widget _buildErrorState(bool isVi) {
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        children: [
          const SizedBox(height: 80),
          Icon(Icons.error_outline_rounded, color: Colors.orange[700], size: 48),
          const SizedBox(height: 12),
          Center(child: Text(_error ?? '', style: TextStyle(color: context.textSecondary))),
          const SizedBox(height: 12),
          Center(
            child: OutlinedButton(
              onPressed: _load,
              style: OutlinedButton.styleFrom(side: const BorderSide(color: AppColors.primary)),
              child: Text(isVi ? 'Thử lại' : 'Retry', style: const TextStyle(color: AppColors.primary)),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPaymentsTab(bool isVi) {
    if (_historyLoading) {
      return const Center(child: CircularProgressIndicator(color: AppColors.primary));
    }
    if (_historyError != null) {
      return RefreshIndicator(
        onRefresh: _loadHistory,
        child: ListView(
          children: [
            const SizedBox(height: 80),
            Icon(Icons.error_outline_rounded, color: Colors.orange[700], size: 48),
            const SizedBox(height: 12),
            Center(child: Text(_historyError!, style: TextStyle(color: context.textSecondary))),
            const SizedBox(height: 12),
            Center(
              child: OutlinedButton(
                onPressed: _loadHistory,
                style: OutlinedButton.styleFrom(side: const BorderSide(color: AppColors.primary)),
                child: Text(isVi ? 'Thử lại' : 'Retry', style: const TextStyle(color: AppColors.primary)),
              ),
            ),
          ],
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: _loadHistory,
      child: ListView(
        padding: const EdgeInsets.all(18),
        children: [
          if (_history.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 60),
              child: Center(
                child: Text(
                  isVi ? 'Chưa có lịch sử giao dịch.' : 'No transactions found.',
                  style: TextStyle(color: context.textSecondary),
                ),
              ),
            )
          else
            ..._history.map((item) => _buildHistoryCard(item, isVi)),
        ],
      ),
    );
  }

  Widget _buildHistoryCard(MyInvoiceDto item, bool isVi) {
    final date = item.paymentDate ?? item.createdAt;
    final dateText = '${_formatMonth(date.month, isVi)} ${date.day}, ${date.year}';
    final title = item.items.isNotEmpty
        ? item.items.map((i) => i.name).join(', ')
        : (isVi ? 'Hóa đơn điều trị' : 'Treatment invoice');

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 56,
            height: 56,
            decoration: BoxDecoration(
              color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  _formatMonth(date.month, isVi),
                  style: TextStyle(fontSize: 10, fontWeight: FontWeight.w900, color: context.textSecondary),
                ),
                const SizedBox(height: 2),
                Text(
                  '${date.day}',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w900, color: context.textPrimary),
                ),
              ],
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        title,
                        style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800, color: context.textPrimary),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: context.isDark ? const Color(0xFF14532D) : const Color(0xFFDCFCE7),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Text(
                        isVi ? 'ĐÃ THANH TOÁN' : 'PAID',
                        style: TextStyle(fontSize: 9, fontWeight: FontWeight.w900, color: Colors.green[800]),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Icon(Iconsax.receipt_1, size: 14, color: context.textSecondary),
                        const SizedBox(width: 4),
                        Text(
                          item.invoiceNumber,
                          style: TextStyle(fontSize: 13, color: context.textSecondary, fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                    Text(
                      formatVnd(item.totalAmount),
                      style: TextStyle(fontSize: 17, fontWeight: FontWeight.w900, color: context.textPrimary),
                    ),
                  ],
                ),
                const SizedBox(height: 2),
                Text(dateText, style: TextStyle(fontSize: 11, color: context.textSecondary)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDebtTab(List<MyInvoiceDto> list, double total, bool isVi) {
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(18),
        children: [
          // Total Debt Banner
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: context.isDark ? const Color(0xFF451A1A) : const Color(0xFFFEE2E2),
              borderRadius: BorderRadius.circular(20),
              border: Border.all(
                color: context.isDark ? Colors.transparent : const Color(0xFFFCA5A5),
              ),
            ),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: const BoxDecoration(
                    color: AppColors.primary,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Iconsax.receipt_item,
                    color: Colors.white,
                    size: 24,
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        isVi ? 'TỔNG CÔNG NỢ CHỜ' : 'TOTAL OUTSTANDING DEBT',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w900,
                          color: context.isDark ? const Color(0xFFFCA5A5) : const Color(0xFF991B1B),
                          letterSpacing: 0.5,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        formatVnd(total),
                        style: TextStyle(
                          fontSize: 26,
                          fontWeight: FontWeight.w900,
                          color: context.isDark ? Colors.white : const Color(0xFF991B1B),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          Text(
            isVi ? 'Hóa đơn chưa thanh toán' : 'Unpaid Invoices',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: context.textPrimary,
            ),
          ),
          const SizedBox(height: 16),
          if (list.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 40),
              child: Column(
                children: [
                  const Icon(Icons.check_circle_outline_rounded, color: Color(0xFF10B981), size: 48),
                  const SizedBox(height: 12),
                  Text(
                    isVi ? 'Không có công nợ nào cần thanh toán!' : 'No outstanding debts!',
                    style: const TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF10B981)),
                  ),
                ],
              ),
            )
          else
            ...list.map((item) => _buildInvoiceCard(item, isVi)),
        ],
      ),
    );
  }

  Widget _buildInvoiceCard(MyInvoiceDto item, bool isVi) {
    final date = item.createdAt;
    final dateText = '${_formatMonth(date.month, isVi)} ${date.day}, ${date.year}';
    final title = item.items.isNotEmpty
        ? item.items.map((i) => i.name).join(', ')
        : (isVi ? 'Hóa đơn điều trị' : 'Treatment invoice');

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Date box
              Container(
                width: 56,
                height: 56,
                decoration: BoxDecoration(
                  color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      _formatMonth(date.month, isVi),
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w900,
                        color: context.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${date.day}',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w900,
                        color: context.textPrimary,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 14),
              // Mid
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            title,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w800,
                              color: context.textPrimary,
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                          decoration: BoxDecoration(
                            color: context.isDark ? const Color(0xFF3C2F1F) : const Color(0xFFFEF3C7),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            isVi ? 'CHƯA TRẢ' : 'UNPAID',
                            style: TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w900,
                              color: Colors.orange[800],
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            Icon(Iconsax.receipt_1, size: 14, color: context.textSecondary),
                            const SizedBox(width: 4),
                            Text(
                              item.invoiceNumber,
                              style: TextStyle(
                                fontSize: 13,
                                color: context.textSecondary,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                        Text(
                          formatVnd(item.depositAmount),
                          style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w900,
                            color: context.textPrimary,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 2),
                    Text(
                      dateText,
                      style: TextStyle(fontSize: 11, color: context.textSecondary),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Divider(color: context.divider, height: 1),
          const SizedBox(height: 10),
          SizedBox(
            width: double.infinity,
            height: 40,
            child: OutlinedButton(
              onPressed: () => _payNow(item),
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: AppColors.primary, width: 1.5),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                foregroundColor: AppColors.primary,
              ),
              child: Text(
                isVi ? 'THANH TOÁN NGAY' : 'PAY NOW',
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 12),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

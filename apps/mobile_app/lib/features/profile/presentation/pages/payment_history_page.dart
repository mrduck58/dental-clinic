import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class InvoiceItem {
  final String id;
  final String titleVi;
  final String titleEn;
  final DateTime date;
  final double amount;
  bool isPaid;

  InvoiceItem({
    required this.id,
    required this.titleVi,
    required this.titleEn,
    required this.date,
    required this.amount,
    this.isPaid = false,
  });
}

class PaymentHistoryPage extends StatefulWidget {
  const PaymentHistoryPage({super.key});

  @override
  State<PaymentHistoryPage> createState() => _PaymentHistoryPageState();
}

class _PaymentHistoryPageState extends State<PaymentHistoryPage> with SingleTickerProviderStateMixin {
  late TabController _tabController;

  final List<InvoiceItem> _invoices = [
    InvoiceItem(
      id: 'INV-88210',
      titleVi: 'Lấy cao răng định kỳ',
      titleEn: 'Professional Cleaning',
      date: DateTime(2023, 10, 5),
      amount: 120.0,
      isPaid: true,
    ),
    InvoiceItem(
      id: 'INV-77196',
      titleVi: 'Chụp X-quang toàn hàm',
      titleEn: 'Dental X-Ray (Full)',
      date: DateTime(2023, 9, 23),
      amount: 85.0,
      isPaid: true,
    ),
    InvoiceItem(
      id: 'INV-57272',
      titleVi: 'Trám răng thẩm mỹ',
      titleEn: 'Composite Filling',
      date: DateTime(2023, 8, 18),
      amount: 210.0,
      isPaid: true,
    ),
    InvoiceItem(
      id: 'INV-99014',
      titleVi: 'Điều trị tủy răng',
      titleEn: 'Root Canal Therapy',
      date: DateTime(2023, 10, 12),
      amount: 350.0,
      isPaid: false,
    ),
    InvoiceItem(
      id: 'INV-32114',
      titleVi: 'Nhổ răng khôn',
      titleEn: 'Wisdom Tooth Extraction',
      date: DateTime(2023, 11, 2),
      amount: 180.0,
      isPaid: false,
    ),
  ];

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  double _getOutstandingTotal() {
    return _invoices.where((i) => !i.isPaid).fold(0.0, (sum, i) => sum + i.amount);
  }

  String _formatMonth(int m, bool isVi) {
    if (isVi) return 'THG $m';
    final months = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];
    return months[m - 1];
  }

  void _showPaymentBottomSheet(InvoiceItem item, bool isVi) {
    String selectedMethod = 'card';

    showModalBottomSheet(
      context: context,
      backgroundColor: context.card,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) {
        return StatefulBuilder(
          builder: (context, setModalState) {
            return Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        isVi ? 'Thanh toán hóa đơn' : 'Pay Invoice',
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: context.textPrimary,
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.close),
                        onPressed: () => Navigator.pop(ctx),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  // Bill info
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: context.bg,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              isVi ? item.titleVi : item.titleEn,
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.bold,
                                color: context.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              item.id,
                              style: TextStyle(fontSize: 12, color: context.textSecondary),
                            ),
                          ],
                        ),
                        Text(
                          '\$${item.amount.toStringAsFixed(2)}',
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w900,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),
                  Text(
                    isVi ? 'Chọn phương thức' : 'Select Payment Method',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w800,
                      color: context.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 12),
                  // Option Visa/Mastercard
                  _buildPaymentOption(
                    id: 'card',
                    icon: Icons.credit_card_rounded,
                    title: isVi ? 'Thẻ tín dụng / Ghi nợ' : 'Credit / Debit Card',
                    selected: selectedMethod == 'card',
                    onTap: () => setModalState(() => selectedMethod = 'card'),
                  ),
                  const SizedBox(height: 10),
                  // Option Momo
                  _buildPaymentOption(
                    id: 'momo',
                    icon: Icons.wallet_rounded,
                    title: isVi ? 'Ví điện tử MoMo' : 'MoMo Wallet',
                    selected: selectedMethod == 'momo',
                    onTap: () => setModalState(() => selectedMethod = 'momo'),
                  ),
                  const SizedBox(height: 20),
                  // Confirm button
                  SizedBox(
                    width: double.infinity,
                    height: 52,
                    child: ElevatedButton(
                      onPressed: () {
                        Navigator.pop(ctx);
                        _executePayment(item, isVi);
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                      ),
                      child: Text(
                        isVi ? 'XÁC NHẬN THANH TOÁN' : 'CONFIRM PAYMENT',
                        style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 14),
                      ),
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildPaymentOption({
    required String id,
    required IconData icon,
    required String title,
    required bool selected,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: selected
              ? (context.isDark ? AppColors.primary.withValues(alpha: 0.15) : const Color(0xFFFEE2E2))
              : Colors.transparent,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: selected ? AppColors.primary : context.divider,
            width: 1.5,
          ),
        ),
        child: Row(
          children: [
            Icon(
              icon,
              color: selected ? AppColors.primary : context.textSecondary,
              size: 22,
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Text(
                title,
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: selected ? FontWeight.bold : FontWeight.w600,
                  color: selected ? AppColors.primary : context.textPrimary,
                ),
              ),
            ),
            if (selected)
              const Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 20)
            else
              Container(
                width: 20,
                height: 20,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: context.textSecondary, width: 2),
                ),
              ),
          ],
        ),
      ),
    );
  }

  void _executePayment(InvoiceItem item, bool isVi) {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => const Center(child: CircularProgressIndicator(color: AppColors.primary)),
    );

    Future.delayed(const Duration(seconds: 1, milliseconds: 500), () {
      Navigator.pop(context); // Pop loading

      setState(() {
        item.isPaid = true;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isVi
                ? 'Thanh toán thành công hóa đơn ${item.id}!'
                : 'Payment successful for ${item.id}!',
          ),
          backgroundColor: const Color(0xFF10B981),
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final paidInvoices = _invoices.where((i) => i.isPaid).toList();
    final unpaidInvoices = _invoices.where((i) => !i.isPaid).toList();
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
            Tab(text: isVi ? 'LỊCH SỬ GD' : 'PAYMENTS'),
            Tab(text: isVi ? 'CÔNG NỢ CHỜ' : 'OUTSTANDING DEBT'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          // Payments Tab
          _buildPaymentsTab(paidInvoices, isVi),
          // Outstanding Debt Tab
          _buildDebtTab(unpaidInvoices, debtTotal, isVi),
        ],
      ),
    );
  }

  Widget _buildPaymentsTab(List<InvoiceItem> list, bool isVi) {
    if (list.isEmpty) {
      return Center(
        child: Text(
          isVi ? 'Chưa có lịch sử giao dịch.' : 'No transactions found.',
          style: TextStyle(color: context.textSecondary),
        ),
      );
    }

    return ListView(
      padding: const EdgeInsets.all(18),
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              isVi ? 'Giao dịch gần đây' : 'Recent Transactions',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: context.textPrimary,
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                borderRadius: BorderRadius.circular(99),
              ),
              child: Text(
                isVi ? 'Lịch sử' : 'History',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: context.textSecondary,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        ...list.map((item) => _buildInvoiceCard(item, isVi)),
      ],
    );
  }

  Widget _buildDebtTab(List<InvoiceItem> list, double total, bool isVi) {
    return ListView(
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
                      '\$${total.toStringAsFixed(2)}',
                      style: TextStyle(
                        fontSize: 28,
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
    );
  }

  Widget _buildInvoiceCard(InvoiceItem item, bool isVi) {
    final dateText = '${_formatMonth(item.date.month, isVi)} ${item.date.day}, ${item.date.year}';
    final cardBg = context.card;

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardBg,
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
                      _formatMonth(item.date.month, isVi),
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w900,
                        color: context.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${item.date.day}',
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
                            isVi ? item.titleVi : item.titleEn,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w800,
                              color: context.textPrimary,
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        // Badge
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                          decoration: BoxDecoration(
                            color: item.isPaid
                                ? (context.isDark ? const Color(0xFF451A1A) : const Color(0xFFFEE2E2))
                                : (context.isDark ? const Color(0xFF3C2F1F) : const Color(0xFFFEF3C7)),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            item.isPaid
                                ? (isVi ? 'ĐÃ TRẢ' : 'PAID')
                                : (isVi ? 'CHƯA TRẢ' : 'UNPAID'),
                            style: TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w900,
                              color: item.isPaid ? AppColors.primary : Colors.orange[800],
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
                              item.id,
                              style: TextStyle(
                                fontSize: 13,
                                color: context.textSecondary,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                        Text(
                          '\$${item.amount.toStringAsFixed(2)}',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w900,
                            color: context.textPrimary,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (!item.isPaid) ...[
            const SizedBox(height: 14),
            Divider(color: context.divider, height: 1),
            const SizedBox(height: 10),
            // Pay Now Button
            SizedBox(
              width: double.infinity,
              height: 40,
              child: OutlinedButton(
                onPressed: () => _showPaymentBottomSheet(item, isVi),
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
        ],
      ),
    );
  }
}

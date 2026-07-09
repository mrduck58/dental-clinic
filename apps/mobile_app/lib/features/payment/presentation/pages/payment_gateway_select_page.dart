import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/utils/money_formatter.dart';
import 'package:mobile_app/features/payment/data/payment_service.dart';
import 'package:mobile_app/features/payment/presentation/pages/payment_checkout_page.dart';

/// Màn hình chọn cổng thanh toán trước khi vào checkout.
/// Chỉ PayOS hoạt động thật hiện tại — Momo/VNPay hiển thị "Sắp có" cho tới khi
/// có credentials thật và một [IPaymentGatewayService] tương ứng được thêm ở backend.
class PaymentGatewaySelectPage extends StatelessWidget {
  final MyInvoiceDto invoice;

  const PaymentGatewaySelectPage({super.key, required this.invoice});

  Future<void> _select(BuildContext context, String gateway) async {
    final paid = await context.push<bool>(
      AppRoutes.payment,
      extra: PaymentCheckoutArgs(invoice: invoice, gateway: gateway),
    );
    if (context.mounted) context.pop(paid);
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

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
          isVi ? 'Chọn cổng thanh toán' : 'Choose Payment Gateway',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.bold, fontSize: 18),
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.divider),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          invoice.invoiceNumber,
                          style: TextStyle(fontWeight: FontWeight.bold, color: context.textPrimary, fontSize: 15),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          invoice.dentistName,
                          style: TextStyle(color: context.textSecondary, fontSize: 12),
                        ),
                      ],
                    ),
                  ),
                  Text(
                    formatVnd(invoice.depositAmount),
                    style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 18, color: AppColors.primary),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            Text(
              isVi ? 'Chọn một cổng để tiếp tục' : 'Pick a gateway to continue',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: context.textSecondary),
            ),
            const SizedBox(height: 12),
            _GatewayTile(
              title: 'PayOS',
              subtitle: isVi
                  ? 'VietQR, thẻ ATM/Visa/Mastercard, ví điện tử'
                  : 'VietQR, ATM/Visa/Mastercard cards, e-wallets',
              logoAsset: 'assets/images/payos.png',
              enabled: true,
              onTap: () => _select(context, 'PayOS'),
            ),
            const SizedBox(height: 12),
            _GatewayTile(
              title: 'MoMo',
              subtitle: isVi ? 'Sắp hỗ trợ' : 'Coming soon',
              logoAsset: 'assets/images/momo.png',
              enabled: false,
            ),
            const SizedBox(height: 12),
            _GatewayTile(
              title: 'ZaloPay',
              subtitle: isVi ? 'Sắp hỗ trợ' : 'Coming soon',
              logoAsset: 'assets/images/zalopay.png',
              enabled: false,
            ),
            const SizedBox(height: 12),
            _GatewayTile(
              title: 'VNPay',
              subtitle: isVi ? 'Sắp hỗ trợ' : 'Coming soon',
              logoAsset: 'assets/images/vnpay.png',
              enabled: false,
            ),
          ],
        ),
      ),
    );
  }
}

class _GatewayTile extends StatelessWidget {
  final String title;
  final String subtitle;
  final IconData? icon;
  final String? logoAsset;
  final bool enabled;
  final VoidCallback? onTap;

  const _GatewayTile({
    required this.title,
    required this.subtitle,
    this.icon,
    this.logoAsset,
    required this.enabled,
    this.onTap,
  }) : assert(icon != null || logoAsset != null, 'Cần icon hoặc logoAsset');

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Opacity(
      opacity: enabled ? 1 : 0.5,
      child: Material(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(16),
        child: InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: onTap,
          child: Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: context.card,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: context.divider),
            ),
            child: Row(
              children: [
                Container(
                  width: 44,
                  height: 44,
                  padding: logoAsset != null ? const EdgeInsets.all(8) : null,
                  decoration: BoxDecoration(
                    color: logoAsset != null ? Colors.white : AppColors.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(12),
                    border: logoAsset != null ? Border.all(color: context.divider) : null,
                  ),
                  child: logoAsset != null
                      ? Image.asset(logoAsset!, fit: BoxFit.contain)
                      : Icon(icon, color: AppColors.primary),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(title, style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15, color: context.textPrimary)),
                      const SizedBox(height: 2),
                      Text(subtitle, style: TextStyle(fontSize: 12, color: context.textSecondary)),
                    ],
                  ),
                ),
                if (enabled)
                  Icon(Icons.chevron_right_rounded, color: context.textSecondary)
                else
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(color: context.divider, borderRadius: BorderRadius.circular(6)),
                    child: Text(
                      isVi ? 'SẮP CÓ' : 'SOON',
                      style: TextStyle(fontSize: 9, fontWeight: FontWeight.w900, color: context.textSecondary),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

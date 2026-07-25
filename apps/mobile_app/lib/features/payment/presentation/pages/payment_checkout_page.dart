import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/utils/money_formatter.dart';
import 'package:mobile_app/features/payment/data/payment_service.dart';
import 'package:url_launcher/url_launcher.dart';

/// Tham số điều hướng tới [PaymentCheckoutPage] — hóa đơn cần thanh toán + cổng đã chọn
/// ở màn "Chọn cổng thanh toán" (PaymentGatewaySelectPage).
class PaymentCheckoutArgs {
  final MyInvoiceDto invoice;
  final String gateway;

  const PaymentCheckoutArgs({required this.invoice, this.gateway = 'PayOS'});
}

/// Màn hình thanh toán một hóa đơn qua cổng thanh toán đã chọn (hiện chỉ PayOS hoạt động thật).
/// Tạo yêu cầu thanh toán, mở trang checkout (đã có sẵn VietQR/thẻ/ví do cổng render),
/// và poll trạng thái để tự động phát hiện khi thanh toán thành công.
class PaymentCheckoutPage extends StatefulWidget {
  final MyInvoiceDto invoice;
  final String gateway;

  const PaymentCheckoutPage({
    super.key,
    required this.invoice,
    this.gateway = 'PayOS',
  });

  @override
  State<PaymentCheckoutPage> createState() => _PaymentCheckoutPageState();
}

class _PaymentCheckoutPageState extends State<PaymentCheckoutPage> {
  final _service = PaymentService();

  PaymentTransactionDto? _txn;
  bool _loading = true;
  bool _paid = false;
  String? _error;
  Timer? _pollTimer;

  @override
  void initState() {
    super.initState();
    _createRequest();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    super.dispose();
  }

  Future<void> _createRequest() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final txn = await _service.createPaymentRequest(
        widget.invoice.id,
        gateway: widget.gateway,
      );
      setState(() {
        _txn = txn;
        _loading = false;
      });
      _startPolling();
    } catch (e) {
      setState(() {
        _error = 'Không thể tạo yêu cầu thanh toán. Vui lòng thử lại.';
        _loading = false;
      });
    }
  }

  void _startPolling() {
    _pollTimer?.cancel();
    _pollTimer = Timer.periodic(const Duration(seconds: 4), (_) async {
      try {
        final status = await _service.getStatus(widget.invoice.id);
        if (status.invoiceStatus == 'Paid' && mounted) {
          _pollTimer?.cancel();
          setState(() => _paid = true);
        }
      } catch (_) {
        // Bỏ qua lỗi polling — thử lại ở lượt kế tiếp.
      }
    });
  }

  Future<void> _openCheckout() async {
    final url = _txn?.checkoutUrl;
    if (url == null) return;
    final uri = Uri.tryParse(url);
    if (uri == null) return;
    await launchUrl(uri, mode: LaunchMode.externalApplication);
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
          onPressed: () => context.pop(_paid),
        ),
        title: Text(
          isVi ? 'Thanh toán' : 'Payment',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.bold, fontSize: 18),
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: _paid
            ? _buildSuccess(isVi)
            : _loading
                ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
                : _error != null
                    ? _buildError(isVi)
                    : _buildCheckout(isVi),
      ),
    );
  }

  Widget _buildCheckout(bool isVi) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(color: context.card, borderRadius: BorderRadius.circular(16), border: Border.all(color: context.divider)),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(widget.invoice.invoiceNumber, style: TextStyle(fontWeight: FontWeight.bold, color: context.textPrimary, fontSize: 15)),
                    const SizedBox(height: 4),
                    Text(widget.invoice.dentistName, style: TextStyle(color: context.textSecondary, fontSize: 12)),
                  ],
                ),
              ),
              Text(
                formatVnd(widget.invoice.depositAmount),
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 18, color: AppColors.primary),
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
        Icon(Icons.qr_code_2_rounded, size: 64, color: context.textSecondary),
        const SizedBox(height: 12),
        Text(
          isVi
              ? 'Mở trang thanh toán để quét mã VietQR hoặc chọn ví/thẻ. Trạng thái sẽ tự động cập nhật khi thanh toán thành công.'
              : 'Open the checkout page to scan the VietQR code or pick a wallet/card. Status updates automatically once payment succeeds.',
          textAlign: TextAlign.center,
          style: TextStyle(color: context.textSecondary, fontSize: 13),
        ),
        const SizedBox(height: 20),
        SizedBox(
          height: 52,
          child: ElevatedButton(
            onPressed: _txn?.checkoutUrl == null ? null : _openCheckout,
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            ),
            child: Text(
              isVi ? 'MỞ TRANG THANH TOÁN' : 'OPEN CHECKOUT',
              style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 14),
            ),
          ),
        ),
        const SizedBox(height: 16),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const SizedBox(
              width: 16, height: 16,
              child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.primary),
            ),
            const SizedBox(width: 10),
            Text(
              isVi ? 'Đang chờ thanh toán...' : 'Waiting for payment...',
              style: TextStyle(color: context.textSecondary, fontSize: 12.5, fontWeight: FontWeight.w600),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildSuccess(bool isVi) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.check_circle_rounded, color: Color(0xFF10B981), size: 72),
          const SizedBox(height: 16),
          Text(
            isVi ? 'Thanh toán thành công!' : 'Payment successful!',
            style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w900, fontSize: 18),
          ),
          const SizedBox(height: 20),
          SizedBox(
            height: 48,
            child: ElevatedButton(
              onPressed: () => context.pop(true),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
              ),
              child: Text(isVi ? 'ĐÓNG' : 'CLOSE', style: const TextStyle(fontWeight: FontWeight.w900)),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildError(bool isVi) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.error_outline_rounded, color: Colors.orange[700], size: 56),
          const SizedBox(height: 12),
          Text(_error ?? '', textAlign: TextAlign.center, style: TextStyle(color: context.textSecondary)),
          const SizedBox(height: 16),
          OutlinedButton(
            onPressed: _createRequest,
            style: OutlinedButton.styleFrom(side: const BorderSide(color: AppColors.primary)),
            child: Text(isVi ? 'Thử lại' : 'Retry', style: const TextStyle(color: AppColors.primary)),
          ),
        ],
      ),
    );
  }
}

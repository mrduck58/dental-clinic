import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';

class MyInvoiceItemDto {
  final String name;
  final int quantity;
  final double unitPrice;

  const MyInvoiceItemDto({
    required this.name,
    required this.quantity,
    required this.unitPrice,
  });

  factory MyInvoiceItemDto.fromJson(Map<String, dynamic> json) =>
      MyInvoiceItemDto(
        name: json['name'] as String? ?? '',
        quantity: (json['quantity'] as num?)?.toInt() ?? 1,
        unitPrice: (json['unitPrice'] as num?)?.toDouble() ?? 0,
      );
}

/// Hóa đơn chưa thanh toán của bệnh nhân hiện tại (mobile app).
class MyInvoiceDto {
  final String id;
  final String invoiceNumber;
  final String dentistName;
  final DateTime createdAt;
  final List<MyInvoiceItemDto> items;
  final double totalAmount;
  final double depositAmount; // Số tiền cần thanh toán cho hóa đơn này
  final double remainingAmount;
  final String paymentType; // "Full" | "Deposit"
  final String paymentMethod; // "Cash" | "BankTransfer" | "OnlinePayment"
  final String status; // "Unpaid" | "Paid" | "Refunded"
  final DateTime? paymentDate;

  const MyInvoiceDto({
    required this.id,
    required this.invoiceNumber,
    required this.dentistName,
    required this.createdAt,
    required this.items,
    required this.totalAmount,
    required this.depositAmount,
    required this.remainingAmount,
    required this.paymentType,
    required this.paymentMethod,
    required this.status,
    this.paymentDate,
  });

  factory MyInvoiceDto.fromJson(Map<String, dynamic> json) => MyInvoiceDto(
        id: json['id'] as String,
        invoiceNumber: json['invoiceNumber'] as String? ?? '',
        dentistName: json['dentistName'] as String? ?? '',
        createdAt: DateTime.parse(json['createdAt'] as String),
        items: (json['items'] as List<dynamic>? ?? [])
            .map((e) => MyInvoiceItemDto.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0,
        depositAmount: (json['depositAmount'] as num?)?.toDouble() ?? 0,
        remainingAmount: (json['remainingAmount'] as num?)?.toDouble() ?? 0,
        paymentType: json['paymentType'] as String? ?? 'Full',
        paymentMethod: json['paymentMethod'] as String? ?? 'Cash',
        status: json['status'] as String? ?? 'Unpaid',
        paymentDate: json['paymentDate'] == null ? null : DateTime.parse(json['paymentDate'] as String),
      );
}

/// Một lần thử thanh toán qua cổng (PayOS) cho một hóa đơn.
class PaymentTransactionDto {
  final String id;
  final String invoiceId;
  final String gateway;
  final String status; // Pending | Success | Failed | Cancelled | Expired
  final double amount;
  final String? checkoutUrl;
  final String? qrCode;

  const PaymentTransactionDto({
    required this.id,
    required this.invoiceId,
    required this.gateway,
    required this.status,
    required this.amount,
    this.checkoutUrl,
    this.qrCode,
  });

  factory PaymentTransactionDto.fromJson(Map<String, dynamic> json) =>
      PaymentTransactionDto(
        id: json['id'] as String,
        invoiceId: json['invoiceId'] as String,
        gateway: json['gateway'] as String? ?? 'PayOS',
        status: json['status'] as String? ?? 'Pending',
        amount: (json['amount'] as num?)?.toDouble() ?? 0,
        checkoutUrl: json['checkoutUrl'] as String?,
        qrCode: json['qrCode'] as String?,
      );
}

class PaymentStatusDto {
  final String invoiceStatus; // Unpaid | Paid | Refunded
  final PaymentTransactionDto? latestTransaction;

  const PaymentStatusDto({required this.invoiceStatus, this.latestTransaction});

  factory PaymentStatusDto.fromJson(Map<String, dynamic> json) =>
      PaymentStatusDto(
        invoiceStatus: json['invoiceStatus'] as String? ?? 'Unpaid',
        latestTransaction: json['latestTransaction'] == null
            ? null
            : PaymentTransactionDto.fromJson(
                json['latestTransaction'] as Map<String, dynamic>),
      );
}

class PaymentService {
  static final PaymentService _instance = PaymentService._internal();
  factory PaymentService() => _instance;
  PaymentService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  /// Hóa đơn chưa thanh toán của bệnh nhân hiện tại.
  Future<List<MyInvoiceDto>> getMyInvoices() async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.get(ApiConstants.myInvoices, token: token);
    final list = res.data as List<dynamic>;
    return list
        .map((e) => MyInvoiceDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Hóa đơn đã thanh toán của bệnh nhân hiện tại (tab "Lịch sử giao dịch").
  Future<List<MyInvoiceDto>> getPaymentHistory() async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.get(ApiConstants.myPaymentHistory, token: token);
    final list = res.data as List<dynamic>;
    return list
        .map((e) => MyInvoiceDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Tạo (hoặc lấy lại) yêu cầu thanh toán qua PayOS cho một hóa đơn.
  Future<PaymentTransactionDto> createPaymentRequest(
    String invoiceId, {
    String gateway = 'PayOS',
  }) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.post(
      ApiConstants.paymentRequest(invoiceId),
      {'gateway': gateway},
      token: token,
    );
    return PaymentTransactionDto.fromJson(res.data as Map<String, dynamic>);
  }

  /// Kiểm tra trạng thái thanh toán hiện tại của hóa đơn (dùng để polling).
  Future<PaymentStatusDto> getStatus(String invoiceId) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res =
        await _client.get(ApiConstants.paymentStatus(invoiceId), token: token);
    return PaymentStatusDto.fromJson(res.data as Map<String, dynamic>);
  }
}

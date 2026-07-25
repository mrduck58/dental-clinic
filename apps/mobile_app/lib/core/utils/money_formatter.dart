/// Định dạng số tiền VND kiểu Việt Nam (dấu chấm phân cách hàng nghìn), ví dụ: 5000000 → "5.000.000₫".
String formatVnd(double amount) {
  final s = amount.toStringAsFixed(0);
  final buf = StringBuffer();
  for (int i = 0; i < s.length; i++) {
    if (i > 0 && (s.length - i) % 3 == 0) buf.write('.');
    buf.write(s[i]);
  }
  return '${buf.toString()}₫';
}

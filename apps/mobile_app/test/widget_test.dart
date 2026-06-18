import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/app/app.dart';

void main() {
  testWidgets('App khởi động và hiển thị màn hình Home', (WidgetTester tester) async {
    await tester.pumpWidget(const DentalClinicApp());
    await tester.pumpAndSettle();

    // Tên phòng khám trên header
    expect(find.text('DentalCare Plus'), findsOneWidget);

    // Tên người dùng
    expect(find.text('Nguyễn Văn An'), findsOneWidget);

    // Các chức năng chính
    expect(find.text('Đặt lịch hẹn'), findsOneWidget);
    expect(find.text('Hồ sơ bệnh án'), findsOneWidget);
    expect(find.text('Thanh toán'), findsOneWidget);
    expect(find.text('Hỏi đáp AI'), findsOneWidget);

    // Tiêu đề section
    expect(find.text('Chức năng chính'), findsOneWidget);
    expect(find.text('Tin tức mới nhất'), findsOneWidget);
  });
}

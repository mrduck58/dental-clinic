# 🔌 Tài liệu API Endpoints (API Specification)

Tất cả các API được thiết kế theo chuẩn RESTful. Format dữ liệu trao đổi mặc định là `application/json`. Base URL của API khi phát triển local là `http://localhost/api` hoặc thông qua proxy `http://localhost/api`.

---

## 🔑 1. Xác thực & Tài khoản (Authentication)

### Đăng nhập (Login)
* **Endpoint:** `POST /auth/login`
* **Quyền truy cập:** Công khai (Public)
* **Request Body:**
  ```json
  {
    "username": "dentist_alice",
    "password": "SecurePassword123"
  }
  ```
* **Response (200 OK):**
  ```json
  {
    "accessToken": "eyJhbGciOi...",
    "expiresIn": 900,
    "refreshToken": "xYz123...",
    "user": {
      "id": "e2a22f7a-80bb-432d-9477-d6e4dfdfb28e",
      "username": "dentist_alice",
      "email": "alice@dentalclinic.com",
      "role": "Dentist"
    }
  }
  ```

### Làm mới Access Token (Refresh Token)
* **Endpoint:** `POST /auth/refresh`
* **Quyền truy cập:** Công khai
* **Request Body:**
  ```json
  {
    "refreshToken": "xYz123..."
  }
  ```

---

## 📅 2. Quản lý Lịch hẹn (Appointments)

### Đăng ký lịch hẹn (Dành cho Patient / Lễ tân)
* **Endpoint:** `POST /appointments`
* **Quyền truy cập:** Đã đăng nhập (`Patient`, `Receptionist`, `Admin`)
* **Request Body:**
  ```json
  {
    "dentistId": "a823bcfd-91b3-4f9e-aef3-d6c11a01ef12",
    "appointmentDate": "2026-06-15T09:30:00+07:00",
    "notes": "Khám răng định kỳ và cạo vôi răng"
  }
  ```
* **Response (201 Created):** Trả về thông tin chi tiết lịch hẹn vừa tạo cùng `Id`.

### Lấy danh sách lịch hẹn (Phân trang và bộ lọc)
* **Endpoint:** `GET /appointments`
* **Quyền truy cập:** Nhân viên (`Admin`, `Dentist`, `Receptionist`) hoặc chính `Patient` sở hữu.
* **Query Parameters:**
  * `page`: Trang hiện tại (Mặc định: 1)
  * `pageSize`: Số lượng bản ghi/trang (Mặc định: 10)
  * `status`: Lọc theo trạng thái (`Pending`, `Confirmed`, `Completed`, `Cancelled`)
  * `dentistId`: Lọc theo nha sĩ
  * `patientId`: Lọc theo bệnh nhân

### Cập nhật trạng thái lịch hẹn
* **Endpoint:** `PATCH /appointments/{id}/status`
* **Quyền truy cập:** Nhân viên (`Admin`, `Dentist`, `Receptionist`)
* **Request Body:**
  ```json
  {
    "status": "Confirmed"
  }
  ```

---

## 🩺 3. Hồ sơ Bệnh án & Kết quả khám (Medical Records)

### Thêm hồ sơ bệnh án mới (Chỉ dành cho Nha sĩ)
* **Endpoint:** `POST /medical-records`
* **Quyền truy cập:** `Dentist`, `Admin`
* **Request Body:**
  ```json
  {
    "patientId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "appointmentId": "f7d7fb3b-c2e8-466d-8bc4-9d5ffcd4f18a",
    "diagnosis": "Sâu răng số 36 và viêm nướu nhẹ",
    "treatmentPlan": "Hàn răng số 36 bằng Composite. Chỉ định dùng nước súc miệng chứa Chlorhexidine.",
    "notes": "Hẹn tái khám sau 6 tháng để kiểm tra khớp cắn."
  }
  ```

### Lấy lịch sử bệnh án của một Bệnh nhân
* **Endpoint:** `GET /patients/{patientId}/medical-records`
* **Quyền truy cập:** `Dentist`, `Admin`, hoặc chính `Patient` sở hữu hồ sơ.

---

## 💳 4. Hóa đơn & Thanh toán (Invoices & Payments)

### Tạo hóa đơn (Khi hoàn thành khám)
* **Endpoint:** `POST /invoices`
* **Quyền truy cập:** `Receptionist`, `Admin`
* **Request Body:**
  ```json
  {
    "appointmentId": "f7d7fb3b-c2e8-466d-8bc4-9d5ffcd4f18a",
    "totalAmount": 550000.00,
    "paymentMethod": "OnlinePayment"
  }
  ```

### Khởi tạo thanh toán Online (Momo / Stripe / VNPay)
* **Endpoint:** `POST /payments/checkout`
* **Quyền truy cập:** `Patient`, `Admin`
* **Request Body:**
  ```json
  {
    "invoiceId": "d3b07384-d113-4c9f-802b-a81d0f52d0a3",
    "callbackUrl": "https://dentalclinic.com/payment-result"
  }
  ```
* **Response (200 OK):** Trả về cổng liên kết thanh toán (`paymentUrl`) để redirect người dùng.

---

## 🤖 5. Hỏi đáp AI (AI Assistant Chat)

Chatbot đọc dữ liệu thật của phòng khám (dịch vụ, ưu đãi, bác sĩ, **lịch trống 7 ngày tới**, lịch
hẹn sắp tới của bệnh nhân và người thân) và có thể **đặt/hủy/dời lịch hẹn trực tiếp trong hội
thoại** — cho chính chủ tài khoản hoặc cho người thân đã đăng ký — sau khi bệnh nhân xác nhận rõ
ràng. Dời lịch giữ nguyên bác sĩ/dịch vụ của lịch gốc, chỉ đổi ngày/giờ.

### Bắt đầu / xem hội thoại
* `POST /chat/conversations?language=vi` — tạo hội thoại mới → `{ "conversationId": "<guid>", "initialMessage": "<string|null>" }`.
  `initialMessage` khác `null` khi bệnh nhân (hoặc người thân) có lịch hẹn trong 48h tới — bot chủ
  động nhắc ngay khi bắt đầu hội thoại mới, thay vì chỉ bị động chờ được hỏi.
* `GET /chat/conversations` — danh sách hội thoại của bệnh nhân hiện tại
* `GET /chat/conversations/{id}` — toàn bộ tin nhắn của một hội thoại
* **Quyền truy cập:** `Patient`

### Gửi tin nhắn cho chatbot
* **Endpoint:** `POST /chat/conversations/{id}/messages`
* **Quyền truy cập:** `Patient`
* **Request Body:**
  ```json
  {
    "message": "Đồng ý, đặt lịch với BS Nguyễn Văn A lúc 9h sáng mai cho con tôi giúp",
    "language": "vi"
  }
  ```
* **Response (200 OK):**
  ```json
  {
    "reply": "✅ Đặt lịch thành công!\n- Mã lịch hẹn: DK20260715ABC123\n- Đặt cho: Bé Bún\n- Bác sĩ: BS Nguyễn Văn A\n- Thời gian: 09:00, Thứ Tư 15/07/2026\nPhòng khám sẽ sớm xác nhận lịch hẹn.",
    "suggestBooking": false,
    "bookingHint": {
      "serviceId": null,
      "serviceName": null,
      "dentistId": "d3b07384-d113-4c9f-802b-a81d0f52d0a3",
      "dentistName": "BS Nguyễn Văn A",
      "preferredDate": "2026-07-15",
      "notes": null,
      "patientId": "a1b2c3d4-...",
      "patientName": "Bé Bún"
    },
    "bookingCreated": true,
    "appointmentCode": "DK20260715ABC123",
    "bookingCancelled": false,
    "bookingRescheduled": false
  }
  ```
* **Giải thích các trường:**
  * `suggestBooking` — bot gợi ý mở luồng đặt lịch thủ công (chỉ bật khi bệnh nhân mô tả
    triệu chứng cần thăm khám, hoặc bot không tự đặt/hủy/dời lịch được).
  * `bookingHint` — dữ liệu điền sẵn cho màn hình đặt lịch; `serviceId`/`dentistId`/`patientId`
    chỉ khác `null` khi backend đối chiếu được với dữ liệu thật trong DB (`patientId` là người
    thân đã đăng ký được chọn để đặt lịch hộ).
  * `bookingCreated` + `appointmentCode` — bot đã tạo lịch hẹn thành công ngay trong hội thoại
    (trạng thái `Pending`, chờ phòng khám xác nhận); app hiển thị nút "Xem lịch hẹn".
  * `bookingCancelled` + `appointmentCode` — bot đã hủy đúng lịch hẹn được xác nhận (khớp mã lịch
    hẹn trong danh sách lịch sắp tới của bệnh nhân/người thân); app hiển thị nút "Xem lịch hẹn".
  * `bookingRescheduled` + `appointmentCode` — bot đã dời lịch hẹn thành công: tạo lịch hẹn MỚI
    (`appointmentCode` là mã lịch MỚI) rồi hủy lịch gốc, giữ nguyên bác sĩ/dịch vụ/người khám.
    `bookingCreated`, `bookingCancelled`, `bookingRescheduled` loại trừ nhau trong cùng một tin nhắn.

### Tóm tắt lịch sử khám bằng AI (cho bác sĩ)
* **Endpoint:** `GET /appointments/{id}/ai-summary?force=false`
* **Quyền truy cập:** `Staff`, `Admin`, `Dentist`
* Tóm tắt lịch sử khám TRƯỚC ĐÂY của bệnh nhân (chẩn đoán/liệu trình/đơn thuốc), gắn với lịch hẹn
  `{id}` đang xem. Kết quả được **cache** trên chính lịch hẹn đó — mặc định (`force=false`) chỉ gọi
  lại Gemini khi có thêm lịch khám mới kể từ lần tóm tắt trước; `force=true` bắt tạo lại dù chưa có
  gì mới (nút "Làm mới" trên UI).
* **Response (200 OK):** `{ "summary": "...", "disclaimer": "⚠️ Đây là tóm tắt do AI tạo tự động...", "fromCache": true }`

---

## 📦 6. Quản lý Kho vật tư (Inventory)

### Lấy danh sách tồn kho vật tư
* **Endpoint:** `GET /inventory`
* **Quyền truy cập:** `Admin`, `Receptionist`

### Cập nhật số lượng vật tư (Nhập/Xuất kho)
* **Endpoint:** `PUT /inventory/{id}/stock`
* **Quyền truy cập:** `Admin`
* **Request Body:**
  ```json
  {
    "quantityChange": -10, // Số âm là xuất kho, số dương là nhập kho
    "reason": "Sử dụng cho điều trị tuần này"
  }
  ```

---

## 📊 7. Thống kê & Trợ lý AI cho vận hành (AI Analytics & Marketing Assistant)

### Thống kê vận hành các tính năng AI
* **Endpoint:** `GET /ai-analytics?rangeDays=14`
* **Quyền truy cập:** `Admin`
* Tổng hợp số liệu vận hành cho toàn bộ tính năng AI (chatbot, tóm tắt bệnh án, soạn nội dung
  marketing, soạn phản hồi đánh giá) trong `rangeDays` ngày gần nhất (1–90, mặc định 14) — số lượt
  gọi/tỷ lệ lỗi/thời gian phản hồi theo từng tính năng, số hội thoại, tỷ lệ gợi ý đặt lịch, tỷ lệ
  đặt/hủy/dời lịch thành công qua chat. Bỏ trống `rangeDays` để lấy TẤT CẢ dữ liệu từ trước tới nay.
  Không trả về nội dung hội thoại/prompt — chỉ số liệu tổng hợp.
* **Response (200 OK):**
  ```json
  {
    "rangeDays": 14,
    "totalConversations": 42,
    "totalMessages": 210,
    "totalUserMessages": 105,
    "suggestBookingCount": 18,
    "bookingActionCount": 12,
    "usageByFeature": [
      { "feature": "ChatBot", "totalCalls": 105, "successCount": 103, "failureCount": 2, "avgDurationMs": 850 }
    ],
    "dailyUsage": [
      { "date": "2026-07-01", "calls": 12, "failures": 0 }
    ]
  }
  ```

### Soạn nháp nội dung marketing bằng AI
* **Endpoint:** `POST /posts/generate-ai-draft`
* **Quyền truy cập:** `Staff`
* Soạn NHÁP bài viết (tiêu đề + nội dung + danh mục gợi ý) từ dữ liệu dịch vụ/ưu đãi có sẵn trong
  hệ thống — nhân viên xem lại, chỉnh sửa và tự lưu/xuất bản như quy trình tạo bài viết thông
  thường; AI không tự đăng bài. Cần cung cấp ít nhất một trong `serviceId`/`promotionId`/`topic`.
* **Request Body:**
  ```json
  {
    "serviceId": "a823bcfd-91b3-4f9e-aef3-d6c11a01ef12",
    "promotionId": null,
    "topic": null,
    "tone": "hào hứng, thu hút"
  }
  ```
* **Response (200 OK):**
  ```json
  {
    "title": "Nụ cười rạng rỡ với dịch vụ tẩy trắng răng chuyên nghiệp",
    "content": "...",
    "suggestedCategory": "Chăm sóc răng miệng"
  }
  ```

### Soạn nháp phản hồi đánh giá khách hàng bằng AI
* **Endpoint:** `POST /feedbacks/{id}/ai-draft-reply`
* **Quyền truy cập:** `Staff`, `Owner`
* Soạn NHÁP một câu trả lời (2–4 câu, lịch sự/chuyên nghiệp) cho đánh giá `{id}` của khách hàng —
  đánh giá thấp (≤ 3 sao) được soạn theo hướng xin lỗi/cầu thị, không hứa hẹn bồi thường cụ thể;
  đánh giá cao được soạn theo hướng cảm ơn. Chỉ điền sẵn vào ô trả lời trên UI — nhân viên xem lại,
  chỉnh sửa rồi tự gửi qua `POST /feedbacks/{id}/reply` như bình thường; AI không tự gửi phản hồi.
* **Response (200 OK):** `{ "replyText": "Cảm ơn bạn đã tin tưởng phòng khám..." }`

---

## 📱 8. API dành riêng cho Mobile App (Patient self-service)

Nhóm endpoint này được thêm để mobile app (Flutter) thay thế toàn bộ dữ liệu mock/hardcode bằng dữ
liệu thật, có ownership check (bệnh nhân chỉ xem được dữ liệu của chính mình và người thân đã đăng
ký dưới tài khoản — so khớp qua `Patient.PrimaryPatientId`).

### Lịch sử khám bệnh của chính bệnh nhân
* **Endpoint:** `GET /appointments/my/medical-history?patientId={optional}`
* **Quyền truy cập:** `Patient`
* Trả về tối đa 50 buổi khám đã hoàn tất (`Completed`/`PendingPayment`) của bệnh nhân hiện tại **và**
  thành viên gia đình, mỗi buổi gồm chẩn đoán + tóm tắt liệu trình + đơn thuốc trong cùng một lần gọi.
  Bỏ `patientId` để lấy tất cả; truyền vào để chỉ xem 1 thành viên cụ thể.
* **Response (200 OK):** danh sách `MyMedicalHistoryDto` — mỗi phần tử gồm `appointmentId`,
  `appointmentCode`, `appointmentDate`, `dentistName`, `serviceName`, `symptoms`, `patientId`,
  `patientName`, `patientRelationship` (`"Tôi"` nếu là chính chủ), `diagnoses[]`, `treatmentPlans[]`,
  `prescriptionItems[]` (gồm cả `usage`, `notes`).

### Liệu trình điều trị kèm nhật ký tiến độ thật
* **Endpoint:** `GET /appointments/my/treatment-plans?patientId={optional}`
* **Quyền truy cập:** `Patient`
* Trả về từng dòng dịch vụ trong kế hoạch điều trị (`TreatmentPlanDto`) kèm `stepProgress[]` —
  nhật ký các bước đã thực hiện thật (số thứ tự, tên bước, %, ngày, bác sĩ, ghi chú), dùng để mobile
  hiển thị tiến độ điều trị thay vì phần trăm/checklist bịa.

### Lịch sử thanh toán (hóa đơn đã trả)
* **Endpoint:** `GET /payments/invoices/my/history`
* **Quyền truy cập:** `Patient`
* Cặp với `GET /payments/invoices/my` (hóa đơn **chưa** trả) đã có — endpoint này trả hóa đơn
  **đã** thanh toán (`Status == Paid`) của bệnh nhân hiện tại, sắp xếp theo `PaymentDate` giảm dần.

### Chi tiết hồ sơ nha sĩ
* **Endpoint:** `GET /dentists/{id}`
* **Quyền truy cập:** Công khai
* Trả về `DentistDetailDto`: `bio`, `education`, `certificateIssuedBy`, `yearsOfExperience`, và
  **`patientCount`** — số bệnh nhân duy nhất tính thật từ các buổi khám hoàn tất với nha sĩ này
  (không phải số liệu marketing cố định).

### Đánh giá nha sĩ (thay hệ thống review mock trong bộ nhớ)
* **Endpoint:** `GET /dentists/{id}/reviews` — công khai, trả `{ averageRating, reviewCount, reviews[] }`
  (mỗi review gồm `patientName`, `rating`, `comment`, `tags[]`, `createdAt`).
* **Endpoint:** `POST /dentists/{id}/reviews` — `Patient`. Body: `{ rating: 1-5, comment, tags?: string[] }`.
  Mỗi bệnh nhân chỉ có **1 đánh giá / nha sĩ** (gửi lại sẽ ghi đè, không tạo bản ghi mới). Yêu cầu
  bệnh nhân đã có ít nhất 1 buổi khám hoàn tất với nha sĩ đó — nếu chưa, trả lỗi 422.

### Lịch nhắc uống thuốc thật
* **Endpoint:** `GET /appointments/my/medication-reminders?date=yyyy-MM-dd`
* **Quyền truy cập:** `Patient`
* Sinh danh sách nhắc thuốc cho một ngày cụ thể, chỉ từ các dòng đơn thuốc đã được bác sĩ nhập đủ
  `TimesPerDay` + `DurationDays` (xem mục "Kê đơn thuốc" trong `docs/database.md`). Đơn thuốc không
  có 2 field này (VD: dùng "khi đau") sẽ **không** sinh nhắc nhở — không suy đoán lịch khi thiếu dữ
  liệu. Giờ nhắc được dàn đều trong khung 07:00–21:00 theo số lần/ngày (giờ gợi ý, không phải giờ
  bác sĩ chỉ định chính xác vì đơn thuốc chỉ ghi tần suất, không ghi giờ cụ thể).
* **Response (200 OK):** danh sách `MedicationReminderDto` — `prescriptionItemId`, `medicineName`,
  `dosage`, `usage`, `time` ("HH:mm"), `patientId`, `patientName`, `patientRelationship`.
* Trạng thái "đã uống" **không lưu ở backend** — mobile lưu cục bộ trên máy (SharedPreferences),
  vì đây là dữ liệu người dùng tự ghi nhận, không cần đồng bộ nhiều thiết bị ở giai đoạn này.

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

### Gửi câu hỏi tư vấn nha khoa tự động
* **Endpoint:** `POST /ai/chat`
* **Quyền truy cập:** Đã đăng nhập (`Patient`, `Guest` nếu cấu hình cho phép)
* **Request Body:**
  ```json
  {
    "message": "Tôi bị ê buốt răng khi uống nước đá thì có phải bị sâu răng không bác sĩ?",
    "history": [
      {
        "role": "user",
        "content": "Chào bác sĩ"
      },
      {
        "role": "model",
        "content": "Chào bạn, tôi là trợ lý ảo nha khoa. Tôi có thể giúp gì cho bạn?"
      }
    ]
  }
  ```
* **Response (200 OK):**
  ```json
  {
    "reply": "Ê buốt răng khi uống nước lạnh có thể do nhiều nguyên nhân khác nhau như: mòn men răng, lộ cổ chân răng, viêm nướu hoặc sâu răng. Bạn nên hạn chế uống nước đá lạnh và đến trực tiếp phòng khám để nha sĩ kiểm tra cụ thể nhé."
  }
  ```

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

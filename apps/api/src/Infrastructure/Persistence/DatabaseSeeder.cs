using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedUsersAsync(db);
        await SeedRoomsAsync(db);
    }

    /// <summary>
    /// Seed dữ liệu mẫu cho dashboard bác sĩ — idempotent, chạy mỗi lần restart
    /// để bổ sung dữ liệu còn thiếu (lịch tuần, lịch hẹn hôm nay).
    /// </summary>
    public static async Task SeedDentistDashboardAsync(AppDbContext db)
    {
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vnNow     = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);
        var today     = DateOnly.FromDateTime(vnNow);
        var vnOffset  = vietnamTz.BaseUtcOffset;

        const string dentistEmail = "thao.nguyen@dentalclinic.com";
        const string dentistPwd   = "Dentist@123456";
        const string dentistName  = "Nguyễn Thị Thảo";

        // ── 1. Find or create dentist user ─────────────────────────────────
        var dentistUser = await db.Users.FirstOrDefaultAsync(u => u.Email == dentistEmail);
        if (dentistUser == null)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(dentistPwd, workFactor: 10);
            dentistUser = User.Create(
                username:     "thao.nguyen",
                email:        dentistEmail,
                passwordHash: hash,
                role:         "Dentist",
                phoneNumber:  "0912345678",
                fullName:     dentistName);

            dentistUser.SetStaffProfile(new StaffProfileData(
                EmployeeId:            "BS001",
                Department:            "Bác sĩ",
                EmploymentStatus:      "full-time",
                ProfilePictureUrl:     null,
                ProfessionalNotes:     null,
                Specialty:             "Nha khoa tổng quát & Implant",
                LicenseNumber:         "NHA-2018-001",
                YearsOfExperience:     8,
                Gender:                "Nữ",
                DateOfBirth:           new DateOnly(1990, 3, 15),
                Address:               "123 Nguyễn Huệ, Q.1, TP.HCM",
                StartDate:             new DateOnly(2018, 6, 1),
                ServicesHandled:       null,
                CertificateIssuedDate: null,
                CertificateIssuedBy:   null,
                Education:             "Đại học Y Dược TP.HCM - Bác sĩ Răng Hàm Mặt",
                Bio:                   "Bác sĩ chuyên khoa Răng Hàm Mặt với 8 năm kinh nghiệm.",
                Position:              "Bác sĩ điều trị",
                EmploymentType:        "Full-time",
                BaseSalary:            30000000,
                SalaryUnit:            "Theo tháng",
                LeaveAccrued:          12));

            db.Users.Add(dentistUser);
            await db.SaveChangesAsync();
            Console.WriteLine($"  [SEED] Đã tạo tài khoản bác sĩ: {dentistEmail} / {dentistPwd}");
        }

        // ── 2. Find or create Dentist record ───────────────────────────────
        var dentist = await db.Dentists.FirstOrDefaultAsync(d => d.UserId == dentistUser.Id);
        if (dentist == null)
        {
            dentist = Dentist.Create(dentistUser.Id, dentistName, "Nha khoa tổng quát & Implant", 8);
            db.Dentists.Add(dentist);
            await db.SaveChangesAsync();
            Console.WriteLine($"  [SEED] Đã tạo Dentist record cho {dentistName}");
        }

        // ── 3. Work schedules tuần này — chỉ thêm ca chưa có ──────────────
        var dow = (int)today.DayOfWeek;
        var daysFromMon = dow == 0 ? 6 : dow - 1;
        var weekMon = today.AddDays(-daysFromMon);
        var weekEnd = weekMon.AddDays(7);

        var existingSchedules = await db.WorkSchedules
            .Where(s => s.StaffName == dentistName && s.Date >= weekMon && s.Date < weekEnd)
            .Select(s => new { s.Date, s.Shift })
            .ToListAsync();

        var existingKeys = existingSchedules.Select(s => (s.Date, s.Shift)).ToHashSet();

        var scheduleSpecs = new (DateOnly Date, string Shift, string Room)[]
        {
            (weekMon,              "morning",   "Phòng 1"),  // Thứ 2
            (weekMon.AddDays(1),   "morning",   "Phòng 1"),  // Thứ 3 (hôm nay)
            (weekMon.AddDays(3),   "afternoon", "Phòng 2"),  // Thứ 5
            (weekMon.AddDays(4),   "morning",   "Phòng 1"),  // Thứ 6
            (weekMon.AddDays(5),   "afternoon", "Phòng 3"),  // Thứ 7
        };

        var addedSchedules = 0;
        foreach (var (date, shift, room) in scheduleSpecs)
        {
            if (existingKeys.Contains((date, shift))) continue;
            var color = shift == "morning" ? "border-primary" : "border-secondary";
            db.WorkSchedules.Add(WorkSchedule.Create(date, shift, "dentist", "dentist", dentistName, room, color, false));
            addedSchedules++;
        }
        if (addedSchedules > 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"  [SEED] Đã thêm {addedSchedules} ca làm việc tuần này");
        }

        // ── 4. Lịch hẹn hôm nay — chỉ tạo nếu chưa có ────────────────────
        var todayVnStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, vnOffset);
        var utcStart = todayVnStart.ToUniversalTime();
        var utcEnd   = utcStart.AddDays(1);

        var hasToday = await db.Appointments
            .AnyAsync(a => a.DentistId == dentist.Id &&
                           a.AppointmentDate >= utcStart &&
                           a.AppointmentDate < utcEnd);

        if (!hasToday)
        {
            // Tạo bệnh nhân
            var patients = new[]
            {
                Patient.Create("Trần Thị Bích",   new DateOnly(1990, 5, 20), "Nữ"),
                Patient.Create("Phạm Minh Cường",  new DateOnly(1985, 8, 14), "Nam"),
                Patient.Create("Lê Thu Hà",        new DateOnly(1995, 3, 8),  "Nữ"),
                Patient.Create("Hoàng Văn Đức",    new DateOnly(1978, 11, 2), "Nam"),
                Patient.Create("Nguyễn Thị Mai",   new DateOnly(2000, 1, 25), "Nữ"),
                Patient.Create("Lê Văn Nam",        new DateOnly(1992, 7, 17), "Nam"),
                Patient.Create("Trần Minh Kha",    new DateOnly(1988, 4, 30), "Nam"),
                Patient.Create("Đỗ Thị Lan",       new DateOnly(1975, 9, 11), "Nữ"),
            };
            db.Patients.AddRange(patients);
            await db.SaveChangesAsync();

            DateTimeOffset VnTime(int h, int m) =>
                new DateTimeOffset(today.Year, today.Month, today.Day, h, m, 0, vnOffset).ToUniversalTime();

            var appts = new[]
            {
                (patients[7], VnTime(7, 30),  "Kiểm tra định kỳ",             AppointmentStatus.Completed),
                (patients[0], VnTime(8,  0),  "Trám răng số 6",               AppointmentStatus.PendingPayment),
                (patients[1], VnTime(8, 30),  "Lấy cao răng định kỳ",         AppointmentStatus.InProgress),
                (patients[2], VnTime(9,  0),  "Tẩy trắng răng Zoom Advanced", AppointmentStatus.CheckedIn),
                (patients[3], VnTime(9, 30),  "Cấy ghép Implant răng số 4",   AppointmentStatus.CheckedIn),
                (patients[4], VnTime(10, 30), "Nhổ răng khôn số 8",           AppointmentStatus.Confirmed),
                (patients[5], VnTime(11,  0), "Chụp X-Quang toàn cảnh",       AppointmentStatus.Confirmed),
                (patients[6], VnTime(13,  0), "Niềng răng - tái khám",        AppointmentStatus.Confirmed),
            };

            foreach (var (patient, apptDate, symptoms, finalStatus) in appts)
            {
                var a = Appointment.Create(patient.Id, dentist.Id, apptDate, symptoms);
                a.Confirm();
                if (finalStatus == AppointmentStatus.CheckedIn)      { a.CheckIn(); }
                if (finalStatus == AppointmentStatus.InProgress)     { a.CheckIn(); a.StartTreatment(); }
                if (finalStatus == AppointmentStatus.PendingPayment) { a.CheckIn(); a.StartTreatment(); a.EndTreatment(); }
                if (finalStatus == AppointmentStatus.Completed)      { a.CheckIn(); a.StartTreatment(); a.EndTreatment(); a.Complete(); }
                db.Appointments.Add(a);
            }
            await db.SaveChangesAsync();

            Console.WriteLine($"  [SEED] Đã tạo {appts.Length} lịch hẹn cho ngày {today:dd/MM/yyyy}");
        }
    }

    private static async Task SeedUsersAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        const string defaultPassword = "Admin@123456";
        var hash = BCrypt.Net.BCrypt.HashPassword(defaultPassword, workFactor: 12);

        var admin = User.Create(
            username:     "admin",
            email:        "admin@dentalclinic.com",
            passwordHash: hash,
            role:         "Admin",
            phoneNumber:  null);

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("  [SEED] Tài khoản Admin mặc định đã tạo:");
        Console.WriteLine($"  Email:    admin@dentalclinic.com");
        Console.WriteLine($"  Password: {defaultPassword}");
        Console.WriteLine("  ⚠️  Đổi mật khẩu ngay sau khi đăng nhập!");
        Console.WriteLine("─────────────────────────────────────────────");
    }

    private static async Task SeedRoomsAsync(AppDbContext db)
    {
        if (await db.Rooms.AnyAsync())
            return;

        var rooms = new[]
        {
            Room.Create("P001", "Phòng 1", "1", "Khám tổng quát",
                "Phòng khám tổng quát đầy đủ trang thiết bị nha khoa, ghế khám cơ học thế hệ mới, đèn LED chiếu sáng chuyên dụng."),

            Room.Create("P002", "Phòng 2", "1", "Khám tổng quát",
                "Phòng khám nha khoa tổng quát cơ bản, trang bị dụng cụ chẩn đoán hình ảnh di động và hệ thống hút nước."),

            Room.Create("P003", "Phòng 3", "2", "Phẫu thuật",
                "Phòng phẫu thuật nha khoa chuyên sâu, hệ thống vô khuẩn cấp độ cao, dụng cụ phẫu thuật implant và nhổ răng khôn."),

            Room.Create("P004", "Phòng 4", "2", "X-Quang",
                "Phòng X-Quang kỹ thuật số hiện đại (Digital Radiography), hỗ trợ chụp phim toàn cảnh (Panorama) và CT-Cone Beam."),

            Room.Create("P005", "Phòng 5", "3", "Cấp cứu",
                "Phòng cấp cứu nha khoa, trang bị đầy đủ thiết bị xử lý các trường hợp khẩn cấp: đau tủy cấp, chấn thương răng, áp-xe."),
        };

        // Đặt trạng thái thực tế cho từng phòng
        rooms[0].ChangeStatus(RoomStatus.DangKham);    // P001 đang có bệnh nhân
        rooms[2].ChangeStatus(RoomStatus.BaoTri);      // P003 đang bảo trì
        // P002, P004, P005 mặc định là Trống

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();

        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("  [SEED] Đã tạo 5 phòng khám mẫu:");
        foreach (var r in rooms)
            Console.WriteLine($"  {r.Code} - {r.Name} ({r.Type}) | {r.Status}");
        Console.WriteLine("─────────────────────────────────────────────");
    }
  
    /// <summary>
    /// Tạo dữ liệu mẫu đơn xin nghỉ cho nhân viên chưa có đơn nào.
    /// </summary>
    public static async Task SeedLeaveRequestsAsync(AppDbContext db)
    {
        // Chỉ seed nếu chưa có leave request nào của nhân viên (không phải Admin)
        var hasStaffLeaves = await db.LeaveRequests
            .AnyAsync(lr => db.Users.Any(u => u.Id == lr.UserId && u.Role != "Admin"));
        if (hasStaffLeaves) return;

        // Lấy danh sách nhân viên (không phải Admin)
        var staffUsers = await db.Users
            .Where(u => u.Role != "Admin")
            .ToListAsync();

        // Tạo nhân viên demo nếu chưa đủ
        if (staffUsers.Count < 7)
        {
            const string pwd = "Staff@123456";
            var hash = BCrypt.Net.BCrypt.HashPassword(pwd, workFactor: 10);

            var demos = new[]
            {
                ("Nguyễn Thị Lan",   "lan.nguyen@dentalclinic.com",  "Dentist", "Lễ tân",  "0901234567"),
                ("Trần Văn Hùng",    "hung.tran@dentalclinic.com",   "Dentist", "Bác sĩ",  "0902345678"),
                ("Lê Thị Mai",       "mai.le@dentalclinic.com",      "Staff",   "Kế toán", "0903456789"),
                ("Phạm Đức Anh",     "anh.pham@dentalclinic.com",    "Dentist", "Bác sĩ",  "0904567890"),
                ("Hoàng Thị Hương",  "huong.hoang@dentalclinic.com", "Staff",   "Lễ tân",  "0905678901"),
                ("Vũ Minh Tuấn",     "tuan.vu@dentalclinic.com",     "Dentist", "Bác sĩ",  "0906789012"),
                ("Đặng Thị Ngọc",    "ngoc.dang@dentalclinic.com",   "Staff",   "Kế toán", "0907890123"),
            };

            foreach (var (name, email, role, dept, phone) in demos)
            {
                if (staffUsers.Any(u => u.Email == email)) continue;

                var user = User.Create(
                    username:     email.Split('@')[0],
                    email:        email,
                    passwordHash: hash,
                    role:         role,
                    phoneNumber:  phone,
                    fullName:     name);

                user.SetStaffProfile(new StaffProfileData(
                    EmployeeId:            null,
                    Department:            dept,
                    EmploymentStatus:      User.DefaultEmploymentStatus,
                    ProfilePictureUrl:     null,
                    ProfessionalNotes:     null,
                    Specialty:             role == "Dentist" ? "Nha khoa tổng quát" : null,
                    LicenseNumber:         null,
                    YearsOfExperience:     null,
                    Gender:                null,
                    DateOfBirth:           null,
                    Address:               null,
                    StartDate:             null,
                    ServicesHandled:       null,
                    CertificateIssuedDate: null,
                    CertificateIssuedBy:   null,
                    Education:             null,
                    Bio:                   null,
                    Position:              null,
                    EmploymentType:        "Full-time",
                    BaseSalary:            null,
                    SalaryUnit:            "Theo tháng",
                    LeaveAccrued:          null));

                db.Users.Add(user);
                staffUsers.Add(user);
            }

            await db.SaveChangesAsync();
        }

        // Dữ liệu mẫu đơn xin nghỉ
        var requests = new (int userIdx, LeaveType type, DateOnly start, DateOnly end, string reason, bool approve, bool reject, string? note)[]
        {
            (0, LeaveType.Annual,    new DateOnly(2026,6,20), new DateOnly(2026,6,22),
             "Có việc gia đình cần giải quyết, xin nghỉ 3 ngày để về quê.",
             false, false, null),

            (1, LeaveType.Sick,      new DateOnly(2026,6,15), new DateOnly(2026,6,15),
             "Bị cảm sốt, sức khỏe không đảm bảo để làm việc.",
             true, false, "Đã duyệt. Nghỉ dưỡng sức, hẹn gặp lại tuần sau."),

            (2, LeaveType.Training,  new DateOnly(2026,6,25), new DateOnly(2026,6,26),
             "Tham gia khóa đào tạo kế toán mới tại TP.HCM do công ty tổ chức.",
             false, false, null),

            (3, LeaveType.Unpaid,    new DateOnly(2026,7,1),  new DateOnly(2026,7,5),
             "Có việc riêng quan trọng cần xin nghỉ không lương 5 ngày.",
             false, true,  "Từ chối do lịch làm việc cao điểm, thiếu nhân sự thay thế. Vui lòng chọn thời gian khác."),

            (4, LeaveType.Maternity, new DateOnly(2026,6,30), new DateOnly(2026,10,30),
             "Nghỉ thai sản theo quy định của pháp luật.",
             true, false, null),

            (5, LeaveType.Annual,    new DateOnly(2026,7,10), new DateOnly(2026,7,12),
             "Kế hoạch đi du lịch gia đình đã sắp xếp từ trước.",
             false, false, null),

            (6, LeaveType.Sick,      new DateOnly(2026,6,18), new DateOnly(2026,6,19),
             "Khám và điều trị răng theo chỉ định bác sĩ.",
             true, false, null),
        };

        foreach (var (userIdx, type, start, end, reason, doApprove, doReject, note) in requests)
        {
            var userId = staffUsers[userIdx % staffUsers.Count].Id;
            var req = LeaveRequest.Create(userId, type, start, end, reason);
            if (doApprove) req.Approve();
            if (doReject)  req.Reject(note);
            db.LeaveRequests.Add(req);
        }

        await db.SaveChangesAsync();
        Console.WriteLine("[SEED] Đã tạo dữ liệu mẫu đơn xin nghỉ phép.");
    }
}

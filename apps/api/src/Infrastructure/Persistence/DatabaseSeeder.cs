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

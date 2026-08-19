using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence;

/// <summary>
/// Đây là DbContext chính của ứng dụng.
/// Cấu hình Entity được đặt trong thư mục Configurations/ theo chuẩn IEntityTypeConfiguration&lt;T&gt;.
/// Implement <see cref="IUnitOfWork"/> trực tiếp (chữ ký <c>SaveChangesAsync</c> đã khớp sẵn với DbContext)
/// để các luồng xuyên nhiều entity (xem PaymentConfirmationService) có 1 điểm chốt lưu duy nhất.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<DentistProfile> DentistProfiles => Set<DentistProfile>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<SupplyItem> SupplyItems => Set<SupplyItem>();
    public DbSet<SupplyTransaction> SupplyTransactions => Set<SupplyTransaction>();
    public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>();
    public DbSet<MaterialRequestItem> MaterialRequestItems => Set<MaterialRequestItem>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentProcedure> TreatmentProcedures => Set<TreatmentProcedure>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<ClinicInfo> ClinicInfos => Set<ClinicInfo>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();
    public DbSet<DentistReview> DentistReviews => Set<DentistReview>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();
    public DbSet<AppointmentSlotHold> AppointmentSlotHolds => Set<AppointmentSlotHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tự động đọc và áp dụng tất cả các cấu hình trong Assembly này
        // (Các class implement IEntityTypeConfiguration<T> trong Configurations/)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<UserDeviceToken>(b =>
        {
            b.ToTable("UserDeviceTokens");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.UserId);
            b.HasIndex(e => e.Token);
        });

        modelBuilder.Entity<AppointmentSlotHold>(b =>
        {
            b.ToTable("AppointmentSlotHolds");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.DentistId, e.AppointmentDate });
            b.HasIndex(e => new { e.PatientId, e.CreatedAt });
        });

        base.OnModelCreating(modelBuilder);
    }
}

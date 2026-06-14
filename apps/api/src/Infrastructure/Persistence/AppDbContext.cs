using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence;

/// <summary>
/// Đây là DbContext chính của ứng dụng.
/// Cấu hình Entity được đặt trong thư mục Configurations/ theo chuẩn IEntityTypeConfiguration&lt;T&gt;.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Dentist> Dentists => Set<Dentist>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tự động đọc và áp dụng tất cả các cấu hình trong Assembly này
        // (Các class implement IEntityTypeConfiguration<T> trong Configurations/)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

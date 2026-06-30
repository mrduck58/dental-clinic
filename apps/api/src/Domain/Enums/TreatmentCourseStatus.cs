namespace DentalClinic.API.Domain.Enums;

public enum TreatmentCourseStatus
{
    Active = 1,     // Đang điều trị (còn công nợ)
    Completed = 2,  // Đã hoàn tất & thu đủ
    Cancelled = 3   // Đã hủy
}

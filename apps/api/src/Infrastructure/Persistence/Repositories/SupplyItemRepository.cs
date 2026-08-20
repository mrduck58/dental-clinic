using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class SupplyItemRepository(AppDbContext db) : ISupplyItemRepository
{
    public async Task<IEnumerable<SupplyItem>> GetAllAsync(CancellationToken ct = default)
        => await db.SupplyItems
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<SupplyItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.SupplyItems.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(SupplyItem item, CancellationToken ct = default)
    {
        await db.SupplyItems.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SupplyItem item, CancellationToken ct = default)
    {
        db.SupplyItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(SupplyItem item, CancellationToken ct = default)
    {
        try
        {
            // Quan hệ Restrict + khóa ngoại bắt buộc (không nullable) trên SupplyTransaction/ServiceSupplyItem/
            // TreatmentSupplyUsage khiến EF Core ném InvalidOperationException NGAY TỪ Remove() (kiểm tra phía
            // client, trước khi chạm DB) nếu vật tư đã có bản ghi liên quan — DbUpdateException chỉ xảy ra nếu
            // ràng buộc còn lại được DB tự kiểm tra. Bắt cả hai, dịch thành lỗi nghiệp vụ dễ hiểu thay vì lộ 500.
            db.SupplyItems.Remove(item);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
        {
            throw new ValidationException(
                $"Không thể xóa \"{item.Name}\" vì đã có giao dịch nhập/xuất hoặc được dùng trong định mức vật tư của dịch vụ.");
        }
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
        => await db.SupplyItems.AnyAsync(s => s.Code == code, ct);

    public async Task<SupplyItem?> GetByNameAsync(string name, CancellationToken ct = default)
        => await db.SupplyItems.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower(), ct);
}

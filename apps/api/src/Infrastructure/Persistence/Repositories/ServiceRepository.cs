using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ServiceRepository(AppDbContext db) : IServiceRepository
{
    public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken ct = default)
        => await db.Services
            .Include(s => s.Options)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Services
            .Include(s => s.Options)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Service service, CancellationToken ct = default)
    {
        await db.Services.AddAsync(service, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Service service, CancellationToken ct = default)
    {
        // KHÔNG gọi db.Services.Update() với entity đang được theo dõi.
        //
        // DbSet.Update duyệt cả đồ thị và đánh dấu Modified cho MỌI entity có khoá đã set. Các
        // ServiceOption vừa được ReplaceOptions tạo ra đều tự sinh Guid nên rơi vào diện đó — EF phát
        // câu UPDATE cho những hàng chưa hề tồn tại, 0 dòng bị ảnh hưởng, và ném
        // DbUpdateConcurrencyException. Người dùng thấy "cập nhật dịch vụ" lỗi 500.
        //
        // Entity lấy từ GetByIdAsync vốn đã được change tracker theo dõi: nó tự biết cột nào đổi và
        // tuỳ chọn nào là mới. Chỉ cần lưu. Nhánh Detached giữ lại cho ai đó truyền vào entity rời.
        if (db.Entry(service).State == EntityState.Detached)
            db.Services.Update(service);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Service service, CancellationToken ct = default)
    {
        db.Services.Remove(service);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Service>> GetActiveAsync(CancellationToken ct = default)
        => await db.Services
            .Include(s => s.Options)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
}

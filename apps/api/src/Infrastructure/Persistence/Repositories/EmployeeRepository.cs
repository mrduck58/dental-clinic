using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class EmployeeRepository(AppDbContext db) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.Employees.FirstOrDefaultAsync(e => e.UserId == userId, ct);

    public async Task<Employee?> GetByUserIdWithDentistProfileAsync(Guid userId, CancellationToken ct = default)
        => await db.Employees
            .Include(e => e.DentistProfile)
            .FirstOrDefaultAsync(e => e.UserId == userId, ct);

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
    {
        await db.Employees.AddAsync(employee, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken ct = default)
    {
        db.Employees.Update(employee);
        await db.SaveChangesAsync(ct);
    }
}

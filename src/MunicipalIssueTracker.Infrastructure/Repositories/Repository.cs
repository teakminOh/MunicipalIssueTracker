using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Interfaces;
using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Db;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext db)
    {
        Db = db;
        DbSet = db.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);

    public virtual async Task<List<T>> GetAllAsync() => await DbSet.ToListAsync();

    public virtual async Task AddAsync(T entity)
    {
        DbSet.Add(entity);
        await Db.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Db.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync() => await Db.SaveChangesAsync();
}

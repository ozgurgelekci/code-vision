using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using CodeVision.Core.Interfaces;
using CodeVision.Infrastructure.Data;

namespace CodeVision.Infrastructure.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly CodeVisionDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(CodeVisionDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        var entry = await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate == null)
            return await _dbSet.CountAsync();
        
        return await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<List<TEntity>> GetPagedAsync(int skip, int take, Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (predicate != null)
            query = query.Where(predicate);
            
        return await query.Skip(skip).Take(take).ToListAsync();
    }
}

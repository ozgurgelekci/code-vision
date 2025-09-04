using System.Linq.Expressions;

namespace CodeVision.Core.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(string id);
    Task<List<TEntity>> GetAllAsync();
    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
    Task<List<TEntity>> GetPagedAsync(int skip, int take, Expression<Func<TEntity, bool>>? predicate = null);
}

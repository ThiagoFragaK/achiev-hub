using System.Linq.Expressions;
using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace achiev_hub.Server.Repositories;

public class Repository<T> : IRepository<T> where T : class, IEntity
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _set;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _set.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _set.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _set.AnyAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
    }

    public void Update(T entity)
    {
        _set.Update(entity);
    }

    public void Remove(T entity)
    {
        _set.Remove(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("A record with the same unique values already exists.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}

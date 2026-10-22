using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IItemboxRepository
{
    /// <summary>Entries in box order (oldest first), which is the order the client lists and drains them.</summary>
    Task<IReadOnlyList<ItemboxEntry>> ListAsync(int characterId, CancellationToken ct = default);
    Task<int> CountAsync(int characterId, CancellationToken ct = default);
    Task<ItemboxEntry?> FindAsync(int characterId, long entryId, CancellationToken ct = default);
}

public sealed class ItemboxRepository(MainContext db) : IItemboxRepository
{
    public async Task<IReadOnlyList<ItemboxEntry>> ListAsync(
        int characterId,
        CancellationToken ct = default
    ) =>
        await db
            .ItemboxEntries.AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

    public Task<int> CountAsync(int characterId, CancellationToken ct = default) =>
        db.ItemboxEntries.CountAsync(x => x.CharacterId == characterId, ct);

    public Task<ItemboxEntry?> FindAsync(
        int characterId,
        long entryId,
        CancellationToken ct = default
    ) =>
        db.ItemboxEntries.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.Id == entryId,
            ct
        );
}

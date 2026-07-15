using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class FeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    private readonly IDbConnectionFactory _factory;

    public FeaturedPromoItemRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    // PromoCode is joined in from Promotion2 (the FK target) for display.
    private const string SelectColumns = @"
        SELECT f.pkid                 AS Pkid,
               f.ScheduleOn           AS ScheduleOn,
               f.TrainingCenter_pkid  AS TrainingCenterPkid,
               f.Slot                 AS Slot,
               f.Promotion_pkid       AS PromotionPkid,
               p2.PromoCode           AS PromoCode,
               f.Topic                AS Topic,
               f.Description          AS Description
        FROM FeaturedPromoItem f
        JOIN Promotion2 p2 ON p2.pkid = f.Promotion_pkid";

    public async Task<IReadOnlyList<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (query.TrainingCenterPkid is { } tc)
        {
            where.Add("f.TrainingCenter_pkid = @TrainingCenterPkid");
            p.Add("@TrainingCenterPkid", tc);
        }

        if (query.WeekStart is { } weekStart)
        {
            where.Add("f.ScheduleOn BETWEEN @WeekStart AND @WeekEnd");
            p.Add("@WeekStart", weekStart);
            p.Add("@WeekEnd", weekStart.AddDays(6));
        }

        var whereClause = where.Count > 0 ? $" WHERE {string.Join(" AND ", where)}" : "";
        var sql = $"{SelectColumns}{whereClause} ORDER BY f.ScheduleOn ASC, f.Slot ASC";

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<FeaturedPromoItem>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<FeaturedPromoItem?> GetByIdAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} WHERE f.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<FeaturedPromoItem>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<bool> SlotTakenAsync(DateOnly scheduleOn, short trainingCenterPkid, byte slot, int? excludePkid = null, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1)
              FROM FeaturedPromoItem
              WHERE ScheduleOn = @ScheduleOn
                AND TrainingCenter_pkid = @TrainingCenterPkid
                AND Slot = @Slot
                AND (@ExcludePkid IS NULL OR pkid <> @ExcludePkid)",
            new { ScheduleOn = scheduleOn, TrainingCenterPkid = trainingCenterPkid, Slot = slot, ExcludePkid = excludePkid },
            cancellationToken: ct));
        return count > 0;
    }

    public async Task<FeaturedPromoItem> CreateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var newId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"INSERT INTO FeaturedPromoItem (ScheduleOn, TrainingCenter_pkid, Slot, Promotion_pkid, Topic, Description)
              VALUES (@ScheduleOn, @TrainingCenterPkid, @Slot, @PromotionPkid, @Topic, @Description);
              SELECT CAST(SCOPE_IDENTITY() AS int);",
            request, cancellationToken: ct));

        return (await GetByIdAsync(newId, ct))!;
    }

    public async Task<bool> UpdateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE FeaturedPromoItem
                 SET ScheduleOn          = @ScheduleOn,
                     TrainingCenter_pkid = @TrainingCenterPkid,
                     Slot                = @Slot,
                     Promotion_pkid      = @PromotionPkid,
                     Topic               = @Topic,
                     Description         = @Description
               WHERE pkid = @Pkid;",
            request, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM FeaturedPromoItem WHERE pkid = @Pkid",
            new { Pkid = pkid }, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> MoveSlotAsync(int pkid, int direction, CancellationToken ct = default)
    {
        if (direction != 1 && direction != -1) return false;

        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        var row = await conn.QuerySingleOrDefaultAsync<(DateOnly ScheduleOn, short TrainingCenterPkid, byte Slot)?>(
            new CommandDefinition(
                @"SELECT ScheduleOn, TrainingCenter_pkid AS TrainingCenterPkid, Slot
                  FROM FeaturedPromoItem WHERE pkid = @Pkid",
                new { Pkid = pkid }, cancellationToken: ct));
        if (row is not { } cur) return false;

        var target = cur.Slot + direction;
        if (target is < 1 or > 3) return false;
        var targetSlot = (byte)target;

        using var tx = conn.BeginTransaction();

        // Swap with whatever currently sits in the target slot on the same day/center.
        // Use temp slot 0 first so the unique (date, center, slot) index is never violated mid-swap.
        var neighbourId = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(
            @"SELECT pkid FROM FeaturedPromoItem
              WHERE ScheduleOn = @ScheduleOn AND TrainingCenter_pkid = @TrainingCenterPkid AND Slot = @Slot",
            new { cur.ScheduleOn, cur.TrainingCenterPkid, Slot = targetSlot }, tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE FeaturedPromoItem SET Slot = 0 WHERE pkid = @Pkid",
            new { Pkid = pkid }, tx, cancellationToken: ct));

        if (neighbourId is { } otherId)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE FeaturedPromoItem SET Slot = @Slot WHERE pkid = @Pkid",
                new { Slot = cur.Slot, Pkid = otherId }, tx, cancellationToken: ct));
        }

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE FeaturedPromoItem SET Slot = @Slot WHERE pkid = @Pkid",
            new { Slot = targetSlot, Pkid = pkid }, tx, cancellationToken: ct));

        tx.Commit();
        return true;
    }
}

using System.Data;
using Dapper;

namespace CMS.API.Infrastructure;

/// <summary>
/// Dapper 2.1.66 can <b>read</b> a <c>date</c> column but cannot bind a <see cref="DateOnly"/> as a
/// command <b>parameter</b> (it throws "cannot be used as a parameter value"). This handler bridges
/// both directions against SQL Server <c>date</c>. Register with <c>SqlMapper.AddTypeHandler</c>.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value)),
    };
}

/// <summary>Same bridge for SQL Server <c>time</c> ↔ <see cref="TimeOnly"/> (no feature uses it yet).</summary>
public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    public override TimeOnly Parse(object value) => value switch
    {
        TimeOnly t => t,
        TimeSpan ts => TimeOnly.FromTimeSpan(ts),
        DateTime dt => TimeOnly.FromDateTime(dt),
        _ => TimeOnly.FromTimeSpan((TimeSpan)value),
    };
}

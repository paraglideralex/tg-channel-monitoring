namespace MonitoringBot.Domain.Abstractions;

public interface ISearchableEntity
{
    string IdProjection();
    string NameProjection();
    string AggregateProjection();
}

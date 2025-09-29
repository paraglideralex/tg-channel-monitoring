namespace MonitoringBot.Domain.Projections;

public sealed class EntitiesChanges<TIdentity>
{
    public required IReadOnlyCollection<TIdentity> EntitiesJoined { get; init; }
    public required IReadOnlyCollection<TIdentity> EntitiesLeft { get; init; }
}

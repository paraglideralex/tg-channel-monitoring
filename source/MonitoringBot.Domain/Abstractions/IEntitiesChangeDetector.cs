using MonitoringBot.Domain.Projections;

namespace MonitoringBot.Domain.Abstractions;

public interface IEntitiesChangeDetector<TIdentity>
{
    public EntitiesChanges<TIdentity> FindChanges(
        IEnumerable<TIdentity> usersCollectionFromApi,
        IEnumerable<TIdentity> usersCollectionFromRepository,
        string aggregateName);
}

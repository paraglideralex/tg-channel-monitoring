using MonitoringBot.Domain.Abstractions;
using MonitoringBot.Domain.Events;
using MonitoringBot.Domain.Projections;
using System.Security.Principal;

namespace MonitoringBot.Domain.Services;

public class EntitiesChangeDetector<TIdentity>()
    : IEntitiesChangeDetector<TIdentity>
{
    private List<TIdentity> ExistInFirstAbsentInSecond(
        IEnumerable<TIdentity> firstCollection,
        IEnumerable<TIdentity> secondCollection) =>

        firstCollection.Except(secondCollection).ToList();

    public EntitiesChanges<TIdentity> FindChanges(
        IEnumerable<TIdentity> usersCollectionFromApi,
        IEnumerable<TIdentity> usersCollectionFromRepository,
        string aggregateName)
    {
        var left = ExistInFirstAbsentInSecond(usersCollectionFromRepository, usersCollectionFromApi);
        var joined = ExistInFirstAbsentInSecond(usersCollectionFromApi, usersCollectionFromRepository);

        var changes = new EntitiesChanges<TIdentity>
        { 
            EntitiesJoined = joined, 
            EntitiesLeft = left 
        };

        return changes;
    }
}

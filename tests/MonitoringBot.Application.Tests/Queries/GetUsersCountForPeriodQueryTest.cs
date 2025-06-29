using MonitoringBot.Application.Queries.Events;
using MonitoringBot.Domain.Events.ChannelMembers;
using MonitoringBot.Domain.Projections;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MonitoringBot.Application.Tests.Queries;

public class GetUsersCountForPeriodQueryTest
{
    UsersCountForPeriodCore usersCountForPeriodCore;

    List<EventTypeWithDate> repoOutput =
    [
        new(new DateTime(2025,1,1,10,0,0),          nameof(SubscriberJoinedEvent)), // 1

        new(new DateTime(2025, 1, 2, 2, 0, 0),      nameof(SubscriberJoinedEvent)), // 2
        new(new DateTime(2025, 1, 3, 9, 0, 0),      nameof(SubscriberJoinedEvent)), // 3
                                                                                      
        new(new DateTime(2025, 1, 4, 6, 0, 0),      nameof(SubscriberJoinedEvent)), // 4
        new(new DateTime(2025, 1, 4, 9, 0, 0),      nameof(SubscriberLeftEvent)),   // 3
                                                                                      
        new(new DateTime(2025, 1, 5, 11, 0, 0),     nameof(SubscriberLeftEvent)),   // 2
        new(new DateTime(2025, 1, 5, 19, 0, 0),     nameof(SubscriberJoinedEvent)), // 3
                                                                                      
        new(new DateTime(2025, 1, 6, 8, 0, 0),      nameof(SubscriberLeftEvent)),   // 2
        new(new DateTime(2025, 1, 6, 12, 0, 0),     nameof(SubscriberJoinedEvent)), // 3
        new(new DateTime(2025, 1, 6, 14, 0, 0),     nameof(SubscriberJoinedEvent)), // 4
                                                                                      
        new(new DateTime(2025, 1, 7, 2, 0, 0),      nameof(SubscriberLeftEvent)),   // 3
        new(new DateTime(2025, 1, 7, 15, 0, 0),     nameof(SubscriberJoinedEvent)), // 4
        new(new DateTime(2025, 1, 7, 19, 0, 0),     nameof(SubscriberJoinedEvent)),  // 5
          
                                                                                // 08 -- 5 
                                                                                // 09 -- 5 

        new(new DateTime(2025, 1, 10, 19, 0, 0),     nameof(SubscriberLeftEvent)),   // 4
        new(new DateTime(2025, 1, 10, 21, 0, 0),     nameof(SubscriberJoinedEvent))   // 5
    ];

    [SetUp]
    public void SetUp()
    {
        usersCountForPeriodCore = new();
        repoOutput.Reverse();
    }

    [Test]
    public void Basic_OneDayTimeSpan()
    {
        var finalCount = 5;

        var result = usersCountForPeriodCore.Execute(
            finalCount,
            repoOutput,
            new DateTime(2025, 1, 1, 10, 0, 0),
            new DateTime(2025, 1, 11, 12, 0, 0),
            TimeSpan.FromDays(1));

        var resultCheck = new List<DateWithUsersCount>
        {
            new(new DateTime(2025,1,11,0,0,0), finalCount),
            new(new DateTime(2025,1,10,0,0,0), finalCount),
            new(new DateTime(2025,1,9,0,0,0), finalCount),
            new(new DateTime(2025,1,8,0,0,0), finalCount),
            new(new DateTime(2025,1,7,0,0,0), finalCount),
            new(new DateTime(2025,1,6,0,0,0),  4),
            new(new DateTime(2025,1,5,0,0,0),  3),
            new(new DateTime(2025,1,4,0,0,0),  3),
            new(new DateTime(2025,1,3,0,0,0),  3),
            new(new DateTime(2025,1,2,0,0,0),  2),
            new(new DateTime(2025,1,1,0,0,0),  1),
        };

        CollectionAssert.AreEqual(result, resultCheck);
    }

    [Test]
    public void Basic_6HoursTimeSpan()
    {
        var finalCount = 5;

        var result = usersCountForPeriodCore.Execute(
            finalCount,
            repoOutput,
            new DateTime(2025, 1, 1, 10, 0, 0),
            new DateTime(2025, 1, 7, 20, 0, 0),
            TimeSpan.FromHours(6));

        var resultCheck = new List<DateWithUsersCount>
        {
            new(new DateTime(2025,1,7,20,0,0), finalCount),
            new(new DateTime(2025,1,6,12,0,0),  4),
            new(new DateTime(2025,1,6,6,0,0),  3),
            new(new DateTime(2025,1,5,18,0,0),  3),
            new(new DateTime(2025,1,5,6,0,0),  2),
            new(new DateTime(2025,1,4,6,0,0),  3),
            new(new DateTime(2025,1,4,0,0,0),  4),
            new(new DateTime(2025,1,3,6,0,0),  3),
            new(new DateTime(2025,1,2,0,0,0),  2),
            new(new DateTime(2025,1,1,6,0,0),  1),
        };

        CollectionAssert.AreEqual(result, resultCheck);
    }

    [Test]
    public void Empty()
    {
        var finalCount = 0;

        var result = usersCountForPeriodCore.Execute(
            finalCount,
            [],
            new DateTime(2025, 1, 1, 10, 0, 0),
            new DateTime(2025, 1, 7, 20, 0, 0),
            TimeSpan.FromDays(1));

        var resultCheck = new List<DateWithUsersCount>
        {
            new(new DateTime(2025,1,7,0,0,0), finalCount),
            new(new DateTime(2025,1,6,0,0,0),  finalCount),
            new(new DateTime(2025,1,5,0,0,0),  finalCount),
            new(new DateTime(2025,1,4,0,0,0),  finalCount),
            new(new DateTime(2025,1,3,0,0,0),  finalCount),
            new(new DateTime(2025,1,2,0,0,0),  finalCount),
            new(new DateTime(2025,1,1,0,0,0),  finalCount),
        };

        CollectionAssert.AreEqual(result, resultCheck);
    }

    [Test]
    public void OnlyOne()
    {
        var repoOutput = new List<EventTypeWithDate>
        {
            new(new DateTime(2025, 1, 5, 19, 0, 0), nameof(SubscriberJoinedEvent)),
        };

        repoOutput.Reverse();

        var finalCount = 1;

        var result = usersCountForPeriodCore.Execute(
            finalCount,
            repoOutput,
            new DateTime(2025, 1, 1, 10, 0, 0),
            new DateTime(2025, 1, 7, 20, 0, 0), TimeSpan.FromDays(1));

        var resultCheck = new List<DateWithUsersCount>
        { 
            new(new DateTime(2025, 1, 7, 20, 0, 0), finalCount)
        };

        CollectionAssert.AreEqual(result, resultCheck);
    }
}

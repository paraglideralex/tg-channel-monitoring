using MonitoringBot.Domain.Entities;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot.Infrastructure.Tests;
public class EventsConverionsTests
{
    [Test]
    public void Base()
    {
        var entity = new ChannelMember(1, "1", false, "1", "1", "1", new DateTime(2025, 1, 1, 3, 3, 3), new DateTime(2025, 1, 1, 3, 3, 3), "test-channel");
    }
}

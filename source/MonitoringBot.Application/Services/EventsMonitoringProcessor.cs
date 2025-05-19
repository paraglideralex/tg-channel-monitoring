using MonitoringBot.Application.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot.Application.Services;
public class EventsMonitoringProcessor<TEntity>(
    GetEventsInPeriodQueryExecution<TEntity> getEventsInPeriodQueryExecution)
{
    protected DateTime LastCheckTimeStamp;

    //public 

}

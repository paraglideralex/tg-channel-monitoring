using MonitoringBot.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot.Application.BackgroundJobs;
public sealed class ServiceContextDto
{
    public Guid? CorrelationId { get; init; }

    public ServiceContextDto() { }

    public ServiceContextDto(ServiceContext context)
    {
        CorrelationId = context.CorrelationId;
    }
}

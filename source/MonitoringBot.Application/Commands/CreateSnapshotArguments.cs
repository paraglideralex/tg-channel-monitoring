using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot.Application.Commands;
public sealed class CreateSnapshotArguments
{
    public DateTime TimeStamp { get; set; }
    public string AggregateName { get; set; }
}

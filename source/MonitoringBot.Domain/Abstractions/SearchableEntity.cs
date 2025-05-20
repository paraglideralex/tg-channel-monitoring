using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot.Domain.Abstractions;
public abstract class SearchableEntity
{
    public abstract string IdProjection();
    public abstract string NameProjection();
}

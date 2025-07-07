using Quartz;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringBot;
public class Biba : IJob
{
    private readonly Boba _boba;

    // Внедрение зависимостей через конструктор
    public Biba(Boba boba)
    {
        _boba = boba;
    }

    public Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"Hello from Biba! Message: {_boba.Message}");
        return Task.CompletedTask;
    }
}


public class Boba
{
    public string Message = "KEK KEK KEK";

    public Boba()
    {
    }
}
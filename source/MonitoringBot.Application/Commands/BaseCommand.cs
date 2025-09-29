using MonitoringBot.Infrastructure;

using Serilog;

using System.Text.Json;

namespace MonitoringBot.Application.Commands;

public abstract class BaseCommand<TArguments>
{
    protected abstract Task ExecuteCoreAsync(TArguments arguments, ServiceContext serviceContext);

    protected virtual bool Validate(TArguments arguments)
    {
        if(arguments is null)
            return false;
        return true;
    }

    public async Task<bool> ExecuteAsync(TArguments arguments, ServiceContext serviceContext)
    {
        if (arguments is TArguments typedArguments)
        {
            var validationResult = Validate(typedArguments);
            if (!validationResult)
            {
                Log.Error($"Аргументы {JsonSerializer.Serialize(typedArguments)} не прошли валидацию");
                return false;
            }
            try
            {
                await ExecuteCoreAsync(typedArguments, serviceContext);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Операция c аргументами {JsonSerializer.Serialize(typedArguments)} завершилась с ошибкой:" +
                    $"{ex.Message}");
                return false;
            }
        }
        Log.Error($"Неверный тип аргументов для данной команды.");
        return false;
    }
}

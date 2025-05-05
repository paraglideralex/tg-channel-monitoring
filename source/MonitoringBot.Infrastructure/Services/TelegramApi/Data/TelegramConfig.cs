namespace MonitoringBot.Infrastructure.Services.TelegramApi.Data;

public record TelegramConfig(
    string ApiId,
    string ApiHash,
    string PhoneNumber
);

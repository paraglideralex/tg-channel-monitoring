namespace MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

public record TelegramConfig(
    string ApiId,
    string ApiHash,
    string PhoneNumber
);

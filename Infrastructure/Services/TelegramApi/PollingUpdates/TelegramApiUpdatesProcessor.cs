using MonitoringBot.Infrastructure.Services.TelegramApi.FetchUsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TL;
using WTelegram;

namespace MonitoringBot.Infrastructure.Services.TelegramApi.PollingUpdates;
public sealed class TelegramApiUpdatesProcessor : ConfigurableTelegramApiService
{
    public TelegramApiUpdatesProcessor(TelegramConfig config, string? channelReference) : base(config, channelReference)
    {
    }

    public async Task RunAsync()
    {
 //       _self = await _client.LoginUserIfNeeded();

        client.OnUpdates += HandleUpdate;

 //       _logger.LogInformation("Бот запущен под пользователем: {Username}", _self.username);

        await Task.Delay(-1); // Бесконечная работа
    }

    // Handle incoming updates
    private async Task HandleUpdate(UpdatesBase updates)
    {



        try
        {
            //var dialogs = await client.Messages_GetAllDialogs();
            //var channel = dialogs.chats.Values
            //    .OfType<Channel>()
            //    .FirstOrDefault(c => c.title == "channel-for-api-testing");

            foreach (var update in updates.UpdateList)
            {
                if (update is UpdateNewMessage unm && unm.message is MessageService serviceMessage)
                {
                    if (serviceMessage.action is MessageActionChatAddUser addUserAction)
                    {
                        Console.WriteLine();
                    }
                }


                        if (update is UpdateNewChannelMessage updateNewChannelMessage)
                {
                    var message = updateNewChannelMessage.message;
                    if (message is MessageService messageService &&
                        messageService.action is MessageActionChatAddUser action)
                    {
                        // Check if this is our target channel
                        //if (messageService.peer_id is PeerChannel peerChannel &&
                        //    peerChannel.channel_id == AccessHashToChannelId(channel.ID))
                        //{
                        //    foreach (var userId in action.users)
                        //    {
                        //        await SendWelcomeMessage(userId, channel.id);
                        //    }
                        //}
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling update: {ex.Message}");
        }
    }

    // Send welcome message with inline buttons
    private async Task SendWelcomeMessage(long userId, long channelId)
    {
        try
        {
            var replyKeyboard = new ReplyKeyboardMarkup();
            replyKeyboard.rows = new[]
            {
                new KeyboardButtonRow
                {
                    buttons = new[]
                    {
                        new KeyboardButtonUrl { text = "📖 Read Rules", url = "https://t.me/yourchannel/rules" },
                        new KeyboardButtonUrl { text = "👋 Say Hi", url = "https://t.me/yourchannel/intro" }
                    }
                }
            };


            var userPeer = new InputPeerUser(userId, long.Parse(config.ApiHash));
            await client.SendMessageAsync(
                userPeer,
                "Welcome to our channel! 🎉\nPlease read our rules and introduce yourself:"//,//,
                //replyKeyboard
            );


            Console.WriteLine($"Sent welcome message to user {userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending welcome message to user {userId}: {ex.Message}");
        }
    }

    // Convert access hash to channel ID
    private long AccessHashToChannelId(long accessHash)
    {
        return accessHash & 0xFFFFFFFF; // Extract channel ID from access hash
    }

    //private async Task OnUpdateHandler(UpdatesBase update)
    //{
    //    //if (update is not UpdateNewMessage unm) return;
    //    if (update is not MessageAction serviceMsg) return;
    //    if (serviceMsg is not MessageActionChatAddUser addUserAction) return;

    //    var chatId = unm.message.peer.ID;
    //    foreach (var userId in addUserAction.users)
    //    {
    //        await SendWelcomeMessageAsync(chatId, userId);
    //    }
    //}

    private async Task SendWelcomeMessageAsync(long chatId, long userId)
    {
        //try
        //{
        //    var buttons = new[]
        //    {
        //        new[] { new KeyboardButtonUrl("🌐 Наш сайт", "https://example.com") },
        //        new[] { new KeyboardButtonUrl("📢 Канал", "https://t.me/example_channel") }
        //    };

        //    var replyMarkup = new ReplyInlineMarkup
        //    {
        //        rows = buttons.Select(row => row.Select(button => new KeyboardButtonUrl(button.text, button.url)).ToArray()).ToArray()
        //    };

        //    var text = "Добро пожаловать в канал! Ознакомьтесь с полезными ресурсами 👇";

        //    await _client.SendMessageAsync(
        //        new InputPeerUser { user_id = userId },
        //        text,
        //        reply_markup: replyMarkup
        //    );

        //    _logger.LogInformation("Отправлено приветственное сообщение пользователю {UserId}", userId);
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "Ошибка при отправке приветствия пользователю {UserId}", userId);
        //}
    }
}

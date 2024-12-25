using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System.IO.Compression;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LocalDownloaderBot;

internal class BotService(ILogger<BotService> logger) : BackgroundService
{
    private const string BotToken = "BOT-API-KEY";
    private static readonly string BaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DownloadedFiles");
    private static readonly string ArchivesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Archives");
    private TelegramBotClient? _botClient;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Utils.InitializeDirectories(BaseDirectory, ArchivesDirectory);

        _botClient = new TelegramBotClient(BotToken);
        _botClient.SendStartupMessage();
        _botClient.SetCustomInfo();

        _botClient.OnMessage += OnMessage;

        logger.LogInformation("Bot is running...");

        while (!stoppingToken.IsCancellationRequested) Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        return Task.CompletedTask;
    }

    private async Task OnMessage(Message message, UpdateType type) => _ = Task.Run(async () => await HandleMessage(message, type));

    private async Task HandleMessage(Message message, UpdateType type)
    {
        var fromId = (long)message.From?.Id!;
        var messageId = message.Id;

        try
        {
            if (fromId != 116969885) return;

            if (message.Text is not null && message.Text!.Trim().Equals("/tree", StringComparison.OrdinalIgnoreCase))
            {
                await SendTree(fromId, messageId);
                return;
            }

            if (message.Text is not null && message.Text!.Trim().Equals("/archives", StringComparison.OrdinalIgnoreCase))
            {
                await SendArchives(fromId, messageId);
                return;
            }

            if (message.Text is not null && message.Text!.Trim().Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                await SendHere(fromId, messageId);
                return;
            }

            if (message.Text is not null && message.Text!.Trim().Equals("/ping", StringComparison.OrdinalIgnoreCase))
            {
                await SendPing(fromId, messageId);
                return;
            }

            if (message.Text is not null && message.Text!.Trim().Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                await Clear(fromId, messageId);
                return;
            }

            if (message.Text is not null && message.Text!.Trim().Split(" ")[0].Equals("/zip", StringComparison.OrdinalIgnoreCase))
            {
                var archiveName = message.Text!.Trim().Split(" ")[1].Trim();

                if (string.IsNullOrWhiteSpace(archiveName))
                    throw new Exception("archiveName is NullOrWhiteSpace");

                await Zip(fromId, messageId, archiveName ?? DateTime.Now.ToString("s"));
                return;
            }

            switch (message?.Type)
            {
                case MessageType.Document:
                    await DownloadFile(message.Document!, FileType.Document, DateTime.Now.ToString("s"), message.Document!.FileName, fromId, messageId);
                    break;

                case MessageType.Photo:
                    await DownloadFile(message.Photo!.Last(), FileType.Photo, DateTime.Now.ToString("s"), $"{message.Photo!.Last().FileId}.jpg", fromId, messageId);
                    break;

                case MessageType.Audio:
                    await DownloadFile(message.Audio!, FileType.Audio, DateTime.Now.ToString("s"), message.Audio!.FileName, fromId, messageId);
                    break;

                case MessageType.Video:
                    await DownloadFile(message.Video!, FileType.Video, DateTime.Now.ToString("s"), message.Video!.FileName, fromId, messageId);
                    break;

                case MessageType.VideoNote:
                    await DownloadFile(message.VideoNote!, FileType.VideoNote, DateTime.Now.ToString("s"), $"{message.VideoNote!.FileId}.mp4", fromId, messageId);
                    break;

                case MessageType.Voice:
                    await DownloadFile(message.Voice!, FileType.Voice, DateTime.Now.ToString("s"), $"{message.Voice!.FileId}.mp3", fromId, messageId);
                    break;

                case MessageType.Sticker:
                    await DownloadFile(message.Sticker!, FileType.Sticker, DateTime.Now.ToString("s"), $"{message.Sticker!.FileId}.mp4", fromId, messageId);
                    break;

                default:
                    await _botClient!.SendSticker(fromId, sticker: new InputFileId("CAACAgQAAxkBAAOIZ2nB0v3aWqCHfyN6lWXI38dBdLUAAhAWAAJJQHFQspQ8MHPZbaM2BA"), replyParameters: messageId);
                    break;
            }
        }
        catch (Exception ex)
        {
            await _botClient!.SendMessage(fromId, replyParameters: messageId, text: $"❌ Received an Exception: {ex.Message}.");
            await _botClient!.SetMessageReaction(fromId, messageId, [new ReactionTypeEmoji { Emoji = "💩" }]);
        }
    }

    private async Task SendTree(ChatId fromId, ReplyParameters? messageId)
    {
        await _botClient!.SendMessage(fromId, replyParameters: messageId, text: Utils.GetDirectoryTree(Path.Combine(BaseDirectory)));
    }

    private async Task SendArchives(ChatId fromId, ReplyParameters? messageId)
    {
        await _botClient!.SendMessage(fromId, replyParameters: messageId, text: Utils.GetDirectoryTree(Path.Combine(ArchivesDirectory)));
    }

    private async Task SendHere(ChatId fromId, ReplyParameters? messageId)
    {
        await _botClient!.SendMessage(fromId, replyParameters: messageId, text: "I'm here.");
    }

    private async Task SendPing(ChatId fromId, ReplyParameters? messageId)
    {
        await _botClient!.SendMessage(fromId, replyParameters: messageId, text: "pong");
    }

    private async Task Clear(ChatId fromId, ReplyParameters? messageId)
    {
        var directoryInfo = new DirectoryInfo(Path.Combine(BaseDirectory));

        foreach (var file in directoryInfo.GetFiles())
            file.Delete();

        foreach (var dir in directoryInfo.GetDirectories())
            dir.Delete(true);

        await _botClient!.SendMessage(fromId, replyParameters: messageId, text: "Cleared.");
    }

    private async Task Zip(ChatId fromId, ReplyParameters? messageId, string archiveName)
    {
        var msg = await _botClient!.SendMessage(fromId, replyParameters: messageId, text: $"Processing...");

        ZipFile.CreateFromDirectory(BaseDirectory, Path.Combine(ArchivesDirectory, $"{archiveName}.zip"));

        await _botClient!.EditMessageText(fromId, msg.MessageId, text: $"Success -> {archiveName}.zip");
    }

    private async Task DownloadFile(FileBase file, FileType fileType, string datetime, string? documentFileName, ChatId fromId, int messageId)
    {
        var policy = Policy.Handle<Exception>()
                           .RetryAsync(20, async (e, i) =>
                               await _botClient!.EditMessageText(fromId, messageId, text: $"❌ Received an Exception: {e.Message}., RetryNumber: {i}")
                           );

        await policy.ExecuteAsync(async () => await DownloadAsync(file, fileType, datetime, documentFileName, fromId, messageId));
    }

    private async Task DownloadAsync(FileBase file, FileType fileType, string datetime, string? documentFileName, ChatId fromId, int messageId)
    {
        var fileName = (string?)file.GetType().GetProperty("FileName")?.GetValue(file)!;

        var fileInfo = await _botClient!.GetInfoAndDownloadFile(file.FileId, Stream.Null);
        var filePath = Path.Combine(BaseDirectory, fileInfo.FilePath.ToWindowsSupportedFileName() ?? $"{fileType:G}/{documentFileName ?? file.FileId}");

        if (!string.IsNullOrEmpty(fileName?.Trim()))
            filePath = filePath.Replace(filePath.Split("/")[^1], fileName);

        var msg = await _botClient!.SendMessage(fromId, replyParameters: messageId, text: $"⏳ Downloading file of type {fileType:G} to {fileName ?? fileInfo.FilePath}...");

        if (!Directory.Exists(filePath.Replace(filePath.Split("/")[^1], "")))
            Directory.CreateDirectory(filePath.Replace(filePath.Split("/")[^1], "")!);

        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            await _botClient?.DownloadFile(fileInfo.FilePath!, fileStream)!;
        }

        await _botClient!.EditMessageText(fromId, messageId: msg.MessageId, text: $"✅ File downloaded successfully: {fileName ?? fileInfo.FilePath}");
        await _botClient!.SetMessageReaction(fromId, messageId, [new ReactionTypeEmoji { Emoji = "💯" }]);
    }
}
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LocalDownloaderBot;

internal static class Utils
{
    internal static void SetCustomInfo(this ITelegramBotClient bot)
    {
        bot.SetMyName("Local Downloader Bot", "en");
        bot.SetMyDescription("""
                             This Telegram bot is your personal download assistant. 
                             Simply share any files with me, and I'll efficiently retrieve the file and deliver it directly to your chat. 
                             Supports a wide range of file types and sources, making downloads effortless and convenient.
                             """, "en");
        bot.SetMyShortDescription("""
                                  Effortlessly download files on Telegram.
                                  Share any files with me, and I'll deliver it directly to you.
                                  """, "en");
        bot.SetMyCommands(
        [
            new BotCommand() { Command = "start", Description = "start" },
            new BotCommand() { Command = "tree", Description = "tree" },
            new BotCommand() { Command = "archives", Description = "archives" },
            new BotCommand() { Command = "ping", Description = "ping" },
            new BotCommand() { Command = "clear", Description = "clear" },
            new BotCommand() { Command = "zip", Description = "zip" },
        ]);
    }

    internal static void SendStartupMessage(this ITelegramBotClient bot)
    {
        var msg = bot.SendMessage(116969885, "🍓 Local downloader bot is ready!");
    }

    public static string GetDirectoryTree(string rootPath, string indent = "")
    {
        StringBuilder treeBuilder = new();

        treeBuilder.AppendLine($"{indent}{Path.GetFileName(rootPath)}");

        string[] subdirectories = Directory.GetDirectories(rootPath);
        foreach (string subdirectory in subdirectories)
        {
            treeBuilder.Append(GetDirectoryTree(subdirectory, indent + "  ")); // Increase indentation for subdirectories
        }

        string[] files = Directory.GetFiles(rootPath);
        foreach (string file in files)
        {
            treeBuilder.AppendLine($"{indent}  - {Path.GetFileName(file)}"); // Indent files
        }

        return treeBuilder.ToString();
    }

    internal static void InitializeDirectories(string baseDirectory, string archiveDirectory)
    {
        if (!Directory.Exists(baseDirectory))
            Directory.CreateDirectory(baseDirectory);
        if (!Directory.Exists(archiveDirectory))
            Directory.CreateDirectory(archiveDirectory);
    }
}
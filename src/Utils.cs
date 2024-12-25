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

    private static readonly char[] InvalidChars = new char[]
    {
        '\\', '/', ':', '*', '?', '"', '<', '>', '|',
        (char)0, (char)1, (char)2, (char)3, (char)4, (char)5,
        (char)6, (char)7, (char)8, (char)9, (char)10, (char)11,
        (char)12, (char)13, (char)14, (char)15, (char)16, (char)17,
        (char)18, (char)19, (char)20, (char)21, (char)22, (char)23,
        (char)24, (char)25, (char)26, (char)27, (char)28, (char)29,
        (char)30, (char)31
    };

    internal static string? ToWindowsSupportedFileName(this string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return filePath;

        var result = new StringBuilder();

        foreach (var c in filePath)
            result.Append(Array.Exists(InvalidChars, invalidChar => invalidChar == c) ? '_' : c);

        return result.ToString();
    }
}
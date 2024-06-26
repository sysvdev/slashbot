﻿/*
 *      This file is part of SlashBot distribution (https://github.com/sysvdev/slashbot).
 *      Copyright (c) 2023 contributors
 *
 *      SlashBot is free software: you can redistribute it and/or modify
 *      it under the terms of the GNU General Public License as published by
 *      the Free Software Foundation, either version 3 of the License, or
 *      (at your option) any later version.
 *
 *      SlashBot is distributed in the hope that it will be useful,
 *      but WITHOUT ANY WARRANTY; without even the implied warranty of
 *      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *      GNU General Public License for more details.
 *
 *      You should have received a copy of the GNU General Public License
 *      along with SlashBot.  If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.Extensions.DependencyInjection;

namespace SlashBot;

internal class Program
{
    private static Logger? logger;

    public static Logger Logger
    { get { return logger ?? throw new Exception("Logger Is Null"); } set => logger = value; }

    public static string CurrentDir { get => Environment.CurrentDirectory; }
    public static string SettingPath { get; set; } = Path.Combine(CurrentDir, "config.json");

    public static Settings Config { get; set; } = new();

    public readonly EventId BotEventId = new(42, "SlashBot");

    public static DiscordClient? Client { get; set; }

    public static DiscordChannel? lastdiscordChannel = null;

    public static string[] TableFlips = [
        "┳━┳ ヽ(ಠل͜ಠ)ﾉ",
        "┬─┬ノ( º _ ºノ)",
        "(˚Õ˚)ر ~~~~╚╩╩╝",
        "ヽ(ຈل͜ຈ)ﾉ︵ ┻━┻",
        "(ノಠ益ಠ)ノ彡┻━┻",
        "(╯°□°)╯︵ ┻━┻",
        "(┛◉Д◉)┛彡┻━┻",
        "(☞ﾟヮﾟ)☞ ┻━┻",
        "(┛ಠ_ಠ)┛彡┻━┻"
        ];

    private static void Main(string[] args)
    {
        Thread.CurrentThread.Name = "MainThread";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Config.LoadSettings();
        }
        catch (Exception ex) { Logger.Error(ex, "Error loading {SettingPath}", SettingPath); }
        finally { logger.Information("Settings loaded"); }

        var prog = new Program();

        try { prog.RunBotAsync().GetAwaiter().GetResult(); }
        catch (Exception ex) { Logger.Error(ex, ex.Message); }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Program()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Config.Discord.Token = Environment.GetEnvironmentVariable("Discord_Token") ?? Config.Discord.Token;
        Config.Discord.DefaultActivity = Environment.GetEnvironmentVariable("Discord_DefaultActivity") ?? Config.Discord.DefaultActivity;

        if (Enum.TryParse<DiscordActivityType>(Environment.GetEnvironmentVariable("Discord_DefaultActivityType"), out DiscordActivityType at))
        {
            Config.Discord.DefaultActivityType = at;
        }
        else
        {
            Config.Discord.DefaultActivityType = DiscordActivityType.ListeningTo;
        }

        Config.SaveSetting();
    }

    public async Task RunBotAsync()
    {
        ILoggerFactory logFactory = new LoggerFactory().AddSerilog(logger);

        ServiceProvider serviceProvider = new ServiceCollection()
        .AddSingleton(logFactory)
        .BuildServiceProvider();

        var cfg = new DiscordConfiguration
        {
            Token = Config.Discord.Token,
            TokenType = TokenType.Bot,

            AutoReconnect = true,
            LoggerFactory = logFactory,

            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
        };

        Client = new DiscordClient(cfg);

        Client.SessionCreated += Client_SessionCreated;

        Client.GuildAvailable += this.Client_GuildAvailable;
        Client.ClientErrored += this.Client_ClientError;

        var cmds = Client.UseCommands(new CommandsConfiguration
        {
            ServiceProvider = serviceProvider
        });

        cmds.CommandExecuted += CommandExecuted;
        cmds.CommandErrored += CommandErrored;

        //cmds.RegisterCommands<BotSlashCommands>();
        cmds.AddCommands(typeof(BotCommands).Assembly);

        await Client.ConnectAsync();

        await Task.Delay(-1);
    }

    //private async Task Slash_SlashCommandInvoked(SlashCommandsExtension sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandInvokedEventArgs e)
    //{
    //    e.Context.Client.Logger.LogInformation(BotEventId, "{Username} successfully invoked '{CommandName}'", e.Context.User.Username, e.Context.CommandName);

    //    await e.Context.Client.UpdateStatusAsync(new DiscordActivity()
    //    {
    //        ActivityType = ActivityType.Playing,
    //        Name = "Running commands..."
    //    }, UserStatus.Idle);
    //}

#nullable disable

    private async Task CommandErrored(CommandsExtension sender, DSharpPlus.Commands.EventArgs.CommandErroredEventArgs e)
    {
        e.Context.Client.Logger.LogError(BotEventId, "{Username} tried executing '{CommandName}' but it errored: {Type}: {Message}", e.Context.User.Username, e.Context?.Command.Name ?? "<unknown command>", e.Exception.GetType(), e.Exception.Message ?? "<no message>");

        if (e.Exception is ChecksFailedException)
        {
            var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

            var embed = new DiscordEmbedBuilder
            {
                Title = "Access denied",
                Description = $"{emoji} You do not have the permissions required to execute this command.",
                Color = new DiscordColor(0xFF0000)
            };
            await e.Context.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"{embed}"));
            //await e.Context.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{embed}"));
        }

        await e.Context.Client.UpdateStatusAsync(new DiscordActivity()
        {
            ActivityType = DiscordActivityType.Watching,
            Name = "Errors!"
        }, DiscordUserStatus.DoNotDisturb);
    }

#nullable enable

    private async Task CommandExecuted(CommandsExtension sender, DSharpPlus.Commands.EventArgs.CommandExecutedEventArgs e)
    {
        e.Context.Client.Logger.LogInformation(BotEventId, "{Username} successfully executed '{CommandName}'", e.Context.User.Username, e.Context.Command.Name);

        await e.Context.Client.UpdateStatusAsync(new DiscordActivity()
        {
            ActivityType = Config.Discord.DefaultActivityType,
            Name = Config.Discord.DefaultActivity
        }, DiscordUserStatus.Online);
    }

    private async Task Client_SessionCreated(DiscordClient sender, SessionReadyEventArgs args)
    {
        sender.Logger.LogInformation(BotEventId, "Client is ready to process events.");

        await sender.UpdateStatusAsync(new DiscordActivity()
        {
            ActivityType = Config.Discord.DefaultActivityType,
            Name = Config.Discord.DefaultActivity
        }, DiscordUserStatus.Online);
    }

    private Task Client_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        Thread.CurrentThread.Name = "MainThread";

        sender.Logger.LogInformation(BotEventId, "Guild available: {GuildName}", e.Guild.Name);

        return Task.CompletedTask;
    }

    private Task Client_ClientError(DiscordClient sender, ClientErrorEventArgs e)
    {
        Thread.CurrentThread.Name = "MainThread";

        sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

        return Task.CompletedTask;
    }
}
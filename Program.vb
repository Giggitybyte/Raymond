Imports System.IO
Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.Entities
Imports DSharpPlus.VoiceNext
Imports LiteDB
Imports Microsoft.Extensions.DependencyInjection
Imports Newtonsoft.Json
Imports Raymond.Commands
Imports Raymond.Database
Imports Raymond.Services

Module Program
    Private _services As IServiceProvider

    Public Sub Main(args As String())
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Private Async Function MainAsync() As Task
        Dim config = GetRaymondConfig()
        Dim logger As New LogService

        Dim discord As New DiscordClient(New DiscordConfiguration With {
            .LogLevel = LogLevel.Debug,
            .Token = config("token.discord"),
            .TokenType = TokenType.Bot
        })

        discord.UseVoiceNext()

        AddHandler discord.Ready, Function(e) logger.PrintAsync(LogLevel.Info, "DSharpPlus", "Ready Fired.")
        AddHandler discord.ClientErrored, Function(e) logger.PrintAsync(LogLevel.Error, e.EventName, "", e.Exception)
        AddHandler discord.DebugLogger.LogMessageReceived, Function(s, e) logger.PrintAsync(e.Level, e.Application, e.Message, e.Exception)

        Dim db As New LiteDatabase("Raymond.db")
        db.GetCollection(Of GuildBlacklist).EnsureIndex(Function(b) b.GuildId)

        With New ServiceCollection
            .AddSingleton(db)
            .AddSingleton(discord)
            .AddSingleton(logger)
            .AddSingleton(Of NumberService)
            .AddSingleton(Of GoogleTtsService)
            .AddSingleton(Of PhraseService)
            _services = .BuildServiceProvider
        End With

        _services.GetRequiredService(Of PhraseService)

        Dim cmds = discord.UseCommandsNext(New CommandsNextConfiguration With {
            .IgnoreExtraArguments = True,
            .Services = _services
        })

        cmds.SetHelpFormatter(Of HelpFormatter)()

        Await discord.ConnectAsync(status:=UserStatus.DoNotDisturb)
        Await Task.Delay(-1)
    End Function

    Private Function GetRaymondConfig() As Dictionary(Of String, String)
        Dim cfg As Dictionary(Of String, String)

        Using fileStream As FileStream = File.OpenRead("config.json")
            Using textReader As New StreamReader(fileStream, New UTF8Encoding(False))
                cfg = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(textReader.ReadToEnd)
            End Using
        End Using

        Return cfg
    End Function
End Module


Imports System.IO
Imports System.Reflection
Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.CommandsNext.Exceptions
Imports DSharpPlus.Entities
Imports DSharpPlus.VoiceNext
Imports LiteDB
Imports Microsoft.Extensions.DependencyInjection
Imports Newtonsoft.Json
Imports Raymond.Commands
Imports Raymond.Database
Imports Raymond.Services

Module Raymond
    Private _services As IServiceProvider

    Public Sub Main(args As String())
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Private Async Function MainAsync() As Task
        Dim config = GetRaymondConfig()
        Dim logger As New LogService

        Dim discord As New DiscordClient(New DiscordConfiguration With {
            .LogLevel = LogLevel.Debug,
            .Token = config("token.discord.pub"),
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

        cmds.RegisterCommands(Assembly.GetExecutingAssembly)
        cmds.SetHelpFormatter(Of HelpFormatter)()

        AddHandler cmds.CommandErrored, AddressOf CommandErroredHandler

        Await discord.ConnectAsync(status:=UserStatus.DoNotDisturb)
        Await Task.Delay(-1)
    End Function

    Private Async Function CommandErroredHandler(e As CommandErrorEventArgs) As Task
        Dim msg = $"{e.Command.QualifiedName} errored in {e.Context.Guild.Id}"
        Await _services.GetRequiredService(Of LogService).PrintAsync(LogLevel.Error, "CNext", msg, e.Exception)

        If Not TypeOf e.Exception Is ChecksFailedException Then
            msg = $"Something went wrong while running `{e.Command.QualifiedName}`"
            msg &= $"{vbCrLf}{Formatter.BlockCode($"{e.Exception.Message}{vbCrLf}{e.Exception.StackTrace}", "less")}"
            Await e.Context.RespondAsync(msg)
            Return
        End If

        Dim strBuilder As New StringBuilder
        Dim exception = DirectCast(e.Exception, ChecksFailedException)

        For Each failedCheck In exception.FailedChecks
            If TypeOf failedCheck Is RequireGuildAttribute Then
                strBuilder.AppendLine("This command can only be used on a server.")

            ElseIf TypeOf failedCheck Is RequireDirectMessageAttribute Then
                strBuilder.AppendLine("This command can only be used in a direct message.")

            ElseIf TypeOf failedCheck Is RequireOwnerAttribute Then
                strBuilder.AppendLine("This command can only be used by my creator.")

            ElseIf TypeOf failedCheck Is RequireBotPermissionsAttribute Then
                Dim check = DirectCast(failedCheck, RequireBotPermissionsAttribute)
                strBuilder.AppendLine($"I'm missing the following permissions:{vbCrLf}{check.Permissions.ToPermissionString}")

            ElseIf TypeOf failedCheck Is RequireUserPermissionsAttribute Then
                Dim check = DirectCast(failedCheck, RequireUserPermissionsAttribute)
                strBuilder.AppendLine($"You're missing the following permissions:{vbCrLf}{check.Permissions.ToPermissionString}")
            End If
        Next

        Await e.Context.RespondAsync(strBuilder.ToString)
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


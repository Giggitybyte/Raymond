Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Threading
Imports Discord
Imports Discord.Commands
Imports Discord.WebSocket
Imports Microsoft.Extensions.DependencyInjection
Imports Newtonsoft.Json
Imports Raymond.Services
Imports Serilog

Module Program
    Sub Main(args As String())
        Dim bot As New Bot
        bot.RunAsync().GetAwaiter().GetResult()
    End Sub
End Module

Public Class Bot
    Public Shared ReadOnly Property Configuration As Dictionary(Of String, String)
    Private ReadOnly _semaphore As SemaphoreSlim
    Private _services As IServiceProvider

    Sub New()
        Dim fileStream As FileStream = File.OpenRead("config.json")
        Dim fileReader As New StreamReader(fileStream, New UTF8Encoding(False))

        _Configuration = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(fileReader.ReadToEnd)
        _semaphore = New SemaphoreSlim(1)

        fileStream.Close()
        fileReader.Close()
    End Sub

    Public Async Function RunAsync() As Task
        Dim logFormat = "[{Timestamp:MMM dd yyyy hh:mm:ss tt}] [{Level:u4}] {Message:lj}{NewLine}{Exception}"
        Log.Logger = New LoggerConfiguration().MinimumLevel.Verbose _
            .WriteTo.Console(outputTemplate:=logFormat) _
            .CreateLogger

        Dim cmdConfig = New CommandServiceConfig With {
            .CaseSensitiveCommands = False,
            .DefaultRunMode = RunMode.Async,
            .IgnoreExtraArgs = True,
            .LogLevel = LogSeverity.Verbose
        }

        Dim clientConfig = New DiscordSocketConfig With {
            .DefaultRetryMode = RetryMode.AlwaysRetry,
            .RateLimitPrecision = RateLimitPrecision.Millisecond,
            .MessageCacheSize = 0,
            .LogLevel = LogSeverity.Verbose
        }

        _services = New ServiceCollection() _
            .AddSingleton(clientConfig) _
            .AddSingleton(cmdConfig) _
            .AddSingleton(Of DiscordSocketClient) _
            .AddSingleton(Of CommandService) _
            .AddSingleton(Of AzureSpeechService) _
            .AddSingleton(Of MutteringService) _
            .BuildServiceProvider

        Dim cmdService = _services.GetRequiredService(Of CommandService)
        Dim client = _services.GetRequiredService(Of DiscordSocketClient)

        AddHandler cmdService.Log, AddressOf DiscordLogger
        AddHandler client.Log, AddressOf DiscordLogger
        AddHandler client.MessageReceived, AddressOf CommandHandler
        AddHandler client.Ready, AddressOf InitializeServices

        Await cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), _services)
        Await client.SetStatusAsync(UserStatus.Invisible)
        Await client.LoginAsync(TokenType.Bot, Configuration("token.discord"))
        Await client.StartAsync

        Await Task.Delay(-1)
    End Function

    Private Async Function DiscordLogger(arg As LogMessage) As Task
        Await _semaphore.WaitAsync()
        Dim logMsg = $"[{arg.Source}] {arg.Message}"

        Try
            Select Case arg.Severity
                Case LogSeverity.Verbose
                    Log.Verbose(logMsg)
                Case LogSeverity.Debug
                    Log.Debug(logMsg)
                Case LogSeverity.Info
                    Log.Information(logMsg)
                Case LogSeverity.Warning
                    Log.Warning(arg.Exception, logMsg)
                Case LogSeverity.Error
                    Log.Error(arg.Exception, logMsg)
                Case LogSeverity.Critical
                    Log.Error(arg.Exception, logMsg)
            End Select
        Finally
            _semaphore.Release()
        End Try
    End Function

    Private Async Function CommandHandler(msg As SocketMessage) As Task
        Dim argPos As Integer
        Dim message = TryCast(msg, SocketUserMessage)
        Dim client = _services.GetRequiredService(Of DiscordSocketClient)

        If message Is Nothing OrElse
            Not message.HasMentionPrefix(client.CurrentUser, argPos) OrElse
            message.Author.IsBot Then Return

        Dim context As New SocketCommandContext(client, message)
        Dim cmdService = _services.GetRequiredService(Of CommandService)
        Await cmdService.ExecuteAsync(context, argPos, Nothing, MultiMatchHandling.Best)
    End Function

    Private Function InitializeServices() As Task
        _services.GetRequiredService(Of AzureSpeechService)
        _services.GetRequiredService(Of MutteringService)
        Return Task.CompletedTask
    End Function
End Class

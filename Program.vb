Imports System.IO
Imports System.Text
Imports System.Threading
Imports Discord
Imports Discord.WebSocket
Imports Newtonsoft.Json
Imports Raymond.Services
Imports Serilog

Module Program
    Sub Main(args As String())
        Dim bot As New Bot
        bot.RunAsync().GetAwaiter().GetResult()
    End Sub
End Module

Public Class Bot ' lmao fuck DI
    Public Shared ReadOnly Property Configuration As Dictionary(Of String, String)
    Public Shared ReadOnly Property DiscordClient As DiscordSocketClient
    Public Shared ReadOnly Property Muttering As MutteringService
    Public Shared ReadOnly Property Azure As AzureSpeechService
    Private _semaphore As SemaphoreSlim

    Sub New()
        Dim fileStream As FileStream = File.OpenRead("config.json")
        Dim fileReader As New StreamReader(fileStream, New UTF8Encoding(False))

        _Configuration = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(fileReader.ReadToEnd)
        _semaphore = New SemaphoreSlim(1)

        fileStream.Close()
        fileReader.Close()
    End Sub

    Public Async Function RunAsync() As Task
        Dim logFormat = "[{Timestamp:MMM dd yyyy hh:mm:ss tt}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        Log.Logger = New LoggerConfiguration().MinimumLevel.Verbose _
            .WriteTo.Console(outputTemplate:=logFormat) _
            .CreateLogger

        Dim clientConfig = New DiscordSocketConfig With {
            .DefaultRetryMode = RetryMode.AlwaysRetry,
            .RateLimitPrecision = RateLimitPrecision.Millisecond,
            .MessageCacheSize = 0,
            .LogLevel = LogSeverity.Verbose
        }

        _DiscordClient = New DiscordSocketClient(clientConfig)
        AddHandler _DiscordClient.Log, AddressOf DiscordLogger

        _Azure = New AzureSpeechService
        _Muttering = New MutteringService

        Await _DiscordClient.SetStatusAsync(UserStatus.Invisible)
        Await _DiscordClient.LoginAsync(TokenType.Bot, Configuration("token.discord"))
        Await _DiscordClient.StartAsync

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
End Class

Imports System.Threading
Imports DSharpPlus
Imports Serilog
Imports Serilog.Context
Imports Serilog.Events
Imports Serilog.Sinks.SystemConsole.Themes

Namespace Services
    Public Class LogService
        Private _semaphore As SemaphoreSlim

        Public Sub New()
            Directory.CreateDirectory("Logs")
            Dim logFormat = "[{Timestamp:MMM dd yyyy hh:mm:ss tt}] [{Level:u3}] [{Source}] {Message:lj}{NewLine}{Exception}"
            Log.Logger = New LoggerConfiguration() _
                .MinimumLevel.Verbose _
                .WriteTo.File("Logs/raymond-.log", outputTemplate:=logFormat, rollingInterval:=RollingInterval.Day) _
                .WriteTo.Console(LogEventLevel.Information, outputTemplate:=logFormat, theme:=SystemConsoleTheme.Colored) _
                .Enrich.FromLogContext _
                .CreateLogger

            _semaphore = New SemaphoreSlim(1)
        End Sub

        Public Async Function PrintAsync(level As LogLevel, source As String, message As String, Optional exception As Exception = Nothing) As Task
            Await _semaphore.WaitAsync()
            Dim ctx = LogContext.PushProperty("Source", source)

            Try
                Select Case level
                    Case LogLevel.Debug
                        Log.Debug(exception, message)
                    Case LogLevel.Info
                        Log.Information(exception, message)
                    Case LogLevel.Warning
                        Log.Warning(exception, message)
                    Case LogLevel.Error
                        Log.Error(exception, message)
                    Case LogLevel.Critical
                        Log.Fatal(exception, message)
                End Select
            Finally
                ctx.Dispose()
                _semaphore.Release()
            End Try
        End Function

        Public Sub Print(level As LogLevel, source As String, message As String, Optional exception As Exception = Nothing)
            PrintAsync(level, source, message, exception).GetAwaiter().GetResult()
        End Sub
    End Class
End Namespace
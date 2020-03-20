Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes

Namespace Commands.Modules
    Public Class PingModule
        Inherits BaseCommandModule

        <Command("ping")>
        <Description("Displays Discord websocket latency and calculates round-trip message time.")>
        Public Async Function PingCommand(ctx As CommandContext) As Task
            Dim rtt = Stopwatch.StartNew
            Dim msg = Await ctx.RespondAsync("Calculating round-trip time...")
            rtt.Stop()

            With New StringBuilder
                .Append(If(ctx.Client.Ping > 250, "- ", "+ "))
                .AppendLine($"WS: {ctx.Client.Ping}ms")
                .Append(If(rtt.ElapsedMilliseconds > 500, "- ", "+ "))
                .AppendLine($"RTT: {CInt(rtt.ElapsedMilliseconds)}ms")

                Await msg.ModifyAsync(Formatter.BlockCode(.ToString, "diff"))
            End With
        End Function
    End Class
End Namespace
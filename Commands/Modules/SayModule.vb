Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports Raymond.Services

Namespace Commands.Modules
    Public Class SayModule
        Inherits BaseCommandModule

        Private _sentence As SentenceService

        Public Sub New(sentence As SentenceService)
            _sentence = sentence
        End Sub

        <Command("say"), Aliases("speak", "echo")>
        <Description("Joins the voice channel you're currently in and says the specified text." + vbCrLf +
                     "To keep costs down, this command is limited to 256 characters and with a maximum of 5 uses a day.")>
        <Cooldown(5, 86400, CooldownBucketType.Guild)>
        Public Async Function SayCommand(ctx As CommandContext, <RemainingText> text As String) As Task
            If ctx.Member?.VoiceState?.Channel Is Nothing Then
                Await ctx.RespondAsync("You must be in a voice channel to use this command.")
                Return
            End If

            If text.Length > 256 Then
                Await ctx.RespondAsync($"Text length exceedes maximum ({Formatter.Bold(text.Length.ToString)}/256)")
                Return
            End If

            Await _sentence.SendSentenceAsync(ctx.Member.VoiceState.Channel, text, "en-US-Standard-B")
        End Function
    End Class
End Namespace

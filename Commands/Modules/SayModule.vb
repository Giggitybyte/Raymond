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
        <Description("Joins the voice channel you're currently in and says the specified text.")>
        <Cooldown(50, 86400, CooldownBucketType.Guild)>
        Public Async Function SayCommand(ctx As CommandContext, <RemainingText> text As String) As Task
            If ctx.Member?.VoiceState?.Channel Is Nothing Then
                Await ctx.RespondAsync("You must be in a voice channel to use this command.")
                Return
            End If

            Await _sentence.SendSentenceAsync(ctx.Member.VoiceState.Channel, text, "en-US-Standard-B")
        End Function
    End Class
End Namespace

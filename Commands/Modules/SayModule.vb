Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports Raymond.Services

Namespace Commands.Modules
    Public Class SayModule
        Inherits BaseCommandModule

        Private _phrase As PhraseService

        Public Sub New(phrase As PhraseService)
            _phrase = phrase
        End Sub

        <Command("say"), Aliases("speak", "echo")>
        <Description("Joins the voice channel you're currently in and says the specified text.")>
        Public Async Function SayCommand(ctx As CommandContext, <RemainingText> text As String) As Task
            If ctx.Member?.VoiceState?.Channel Is Nothing Then Await ctx.RespondAsync("You must be in a voice channel to use this command.")
            Await _phrase.SendPhraseAsync(text, ctx.Member.VoiceState.Channel)
        End Function
    End Class
End Namespace

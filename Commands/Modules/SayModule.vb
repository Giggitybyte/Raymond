Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports Raymond.Services

Namespace Commands.Modules
    Public Class SayModule
        Inherits BaseCommandModule

        Private _tts As GoogleTtsService

        Public Sub New(tts As GoogleTtsService)
            _tts = tts
        End Sub

        <Command("say"), Aliases("speak", "echo")>
        <Description("Joins the voice channel you're currently in and says the specified text.")>
        Public Async Function SayCommand(ctx As CommandContext, <RemainingText> text As String) As Task
            Throw New NotImplementedException
        End Function
    End Class
End Namespace

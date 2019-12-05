Imports Discord
Imports Discord.Commands

Namespace Modules
    Public Class SpeechModule


        <Command("say")>
        <RequireUserPermission(GuildPermission.ManageChannels)>
        Public Async Function SayCommand(text As String) As Task

        End Function

        <Command("trigger"), RequireOwner>
        Public Function TriggerCommand() As Task
            Throw New NotImplementedException
        End Function

    End Class
End Namespace
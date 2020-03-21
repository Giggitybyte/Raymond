Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes

Namespace Commands.Modules
    Public Class ModeModule
        Inherits BaseCommandModule

        <Command("mode"), Aliases("m")>
        <Description("Sets which mode Raymond will use for your server." + vbCrLf + vbCrLf + "Valid modes are: `buzzword`, `markov`, `random`." + vbCrLf +
                     "`buzzword` is the default. Raymond will say a randomly selected set of buzzwords for a specific topic." + vbCrLf +
                     "`markov` will have Raymond feed text messages from all non-blacklisted channels into a markov chain generator then say the result in voice channel." + vbCrLf +
                     "`random` will randomly choose between `buzzword` and `markov`")>
        <RequireUserPermissions(Permissions.ManageGuild)>
        Public Async Function ModeCommand(ctx As CommandContext, mode As String) As Task
            Throw New NotImplementedException
        End Function
    End Class
End Namespace
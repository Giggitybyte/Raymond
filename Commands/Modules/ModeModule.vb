Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports LiteDB
Imports Raymond.Database

Namespace Commands.Modules
    Public Class ModeModule
        Inherits BaseCommandModule

        Public _db As LiteDatabase

        Public Sub New(db As LiteDatabase)
            _db = db
        End Sub

        <Command("mode"), Aliases("m")>
        <Description("Sets which mode Raymond will use for your server." + vbCrLf + vbCrLf + "Valid modes are: `buzzword`, `markov`, `random`." + vbCrLf +
                     "`buzzword` is the default. Raymond will say a randomly selected set of buzzwords from a specific topic." + vbCrLf +
                     "`markov` will have Raymond feed text messages from all non-blacklisted channels into a markov chain generator then say the result in voice channel." + vbCrLf +
                     "`random` will randomly choose between `buzzword` and `markov`")>
        <RequireUserPermissions(Permissions.ManageGuild)>
        Public Async Function ModeCommand(ctx As CommandContext, mode As RaymondMode) As Task
            Dim collection = _db.GetCollection(Of GuildData)("guilds")
            Dim guild = collection.FindOne(Function(g) g.GuildId = ctx.Guild.Id)

            If guild Is Nothing Then
                guild = New GuildData With {
                    .GuildId = ctx.Guild.Id
                }
            End If

            guild.Mode = mode
            collection.Upsert(guild)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function
    End Class
End Namespace
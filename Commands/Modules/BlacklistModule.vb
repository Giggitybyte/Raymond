Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports LiteDB
Imports Raymond.Database

Namespace Commands.Modules
    Public Class BlacklistModule
        Inherits BaseCommandModule

        Private _database As LiteDatabase

        Public Sub New(db As LiteDatabase)
            _database = db
        End Sub

        <Command("blacklist")>
        <Description("Blacklisting a voice channel will prevent Raymond from joining it.")>
        <RequireUserPermissions(Permissions.ManageGuild)>
        Public Async Function BlacklistCommand(ctx As CommandContext, channel As DiscordChannel) As Task
            Dim collection = _database.GetCollection(Of GuildBlacklist)("blacklists")
            Dim blacklist = collection.FindOne(Function(g) g.GuildId = ctx.Guild.Id)

            If blacklist Is Nothing Then
                blacklist = New GuildBlacklist With {
                    .GuildId = ctx.Guild.Id,
                    .ChannelIds = New List(Of ULong)
                }
            End If

            If blacklist.ChannelIds.Contains(channel.Id) Then
                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"))
            End If

            blacklist.ChannelIds.Add(channel.Id)
            collection.Upsert(blacklist)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("unblacklist")>
        <Description("Unblacklisting a voice channel will allow Raymond to grace it with his presence.")>
        <RequireUserPermissions(Permissions.ManageGuild)>
        Public Async Function UnblacklistCommand(ctx As CommandContext, channel As DiscordChannel) As Task
            Dim collection = _database.GetCollection(Of GuildBlacklist)("blacklists")
            Dim blacklist = collection.FindOne(Function(g) ctx.Guild.Id = g.GuildId)

            If blacklist Is Nothing Then
                blacklist = New GuildBlacklist With {
                    .GuildId = ctx.Guild.Id,
                    .ChannelIds = New List(Of ULong)
                }

                blacklist.Id = collection.Insert(blacklist)
            End If

            If Not blacklist.ChannelIds.Contains(channel.Id) Then
                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"))
            End If

            blacklist.ChannelIds.Remove(channel.Id)
            collection.Update(blacklist)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function
    End Class
End Namespace
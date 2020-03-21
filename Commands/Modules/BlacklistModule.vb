Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports LiteDB
Imports Raymond.Database

Namespace Commands.Modules
    <Group("blacklist")>
    <Description("Adding a voice channel to the blacklist will prevent Raymond from joining it, while " +
                 "blacklisting a text channel will prevent Raymond from using it for Markov chain generation." + vbCrLf +
                 "Removing a channel from the blacklist will allow Raymond to do the above for voice channels and text channels respectively.")>
    <RequireUserPermissions(Permissions.ManageGuild)>
    Public Class BlacklistModule
        Inherits BaseCommandModule

        Private _database As LiteDatabase

        Public Sub New(db As LiteDatabase)
            _database = db
        End Sub

        <Command("display")>
        <Description("Displays a list of all blacklisted channels.")>
        Public Async Function DisplayCommand(ctx As CommandContext) As Task
            Dim guild As GuildData = GetGuild(_database.GetCollection(Of GuildData)("guilds"), ctx.Guild.Id)

            If guild Is Nothing _
               OrElse (Not guild.ProhibitedTextIds.Any _
                       And Not guild.ProhibitedVoiceIds.Any) Then
                Await ctx.RespondAsync("There aren't any channels blacklisted.")
                Return
            End If

            Dim channels = Await ctx.Guild.GetChannelsAsync
            Dim voice = channels.Where(Function(c) guild.ProhibitedVoiceIds.Contains(c.Id))
            Dim text = channels.Where(Function(c) guild.ProhibitedTextIds.Contains(c.Id))

            If Not voice.Any And Not text.Any Then
                Await ctx.RespondAsync("No blacklisted channels could be found.")
                Return
            End If

            With New StringBuilder
                If voice.Any Then
                    .AppendLine(Formatter.Bold("Voice Channels:"))

                    For Each channel In voice
                        .AppendLine($"{channel.Name} ({channel.Id})")
                    Next
                End If

                .AppendLine()

                If text.Any Then
                    .AppendLine(Formatter.Bold("Text Channels:"))

                    For Each channel In text
                        .AppendLine($"{channel.Mention} ({channel.Id})")
                    Next
                End If

                Await ctx.RespondAsync(.ToString.Trim)
            End With
        End Function

        <Command("add")>
        <Description("Adds a channel to the blacklist, preventing Raymond from interacting with that channel.")>
        Public Async Function AddCommand(ctx As CommandContext, channel As DiscordChannel) As Task
            Dim collection = _database.GetCollection(Of GuildData)("guilds")
            Dim guild = GetGuild(collection, ctx.Guild.Id)

            If guild.ProhibitedTextIds.Contains(channel.Id) Or guild.ProhibitedVoiceIds.Contains(channel.Id) Then
                Await ctx.RespondAsync("That channel is already on the blacklist.")
                Return
            End If

            Select Case channel.Type
                Case ChannelType.Voice
                    guild.ProhibitedVoiceIds.Add(channel.Id)
                Case ChannelType.Text
                    guild.ProhibitedTextIds.Add(channel.Id)
                Case Else
                    Await ctx.RespondAsync($"That channel type cannot be blacklisted.")
                    Return
            End Select

            collection.Upsert(guild)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("except")>
        <Description("Adds all voice and text channels to the blacklist with the exception the provided channels.")>
        Public Async Function ExceptCommand(ctx As CommandContext, ParamArray exemptChannels As DiscordChannel()) As Task
            If Not exemptChannels.Any Then
                Await ctx.RespondAsync("At least one exempt channel must be provided.")
                Return
            End If

            Dim channels = (Await ctx.Guild.GetChannelsAsync).ToList
            exemptChannels = exemptChannels.Where(Function(c) channels.Contains(c)).ToArray

            If Not exemptChannels.Any Then
                Await ctx.RespondAsync("All provided channels were invalid.")
                Return
            End If

            Dim collection = _database.GetCollection(Of GuildData)("guilds")
            Dim guild = GetGuild(collection, ctx.Guild.Id)

            Dim exemptIds = exemptChannels.Select(Function(c) c.Id)
            Dim blacklistedChannels = channels.Where(Function(c) Not (exemptIds.Contains(c.Id) _
                                                                 OrElse guild.ProhibitedTextIds.Contains(c.Id) _
                                                                 Or guild.ProhibitedVoiceIds.Contains(c.Id)))

            If blacklistedChannels.Count = 0 Then
                Await ctx.RespondAsync("Exempt channels withheld, there are no channels to blacklist.")
                Return
            End If

            For Each channel In blacklistedChannels
                Select Case channel.Type
                    Case ChannelType.Voice
                        guild.ProhibitedVoiceIds.Add(channel.Id)
                    Case ChannelType.Text
                        guild.ProhibitedTextIds.Add(channel.Id)
                End Select
            Next

            collection.Upsert(guild)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("remove")>
        <Description("Unblacklisting a voice channel will allow Raymond to grace it with his presence.")>
        Public Async Function RemoveCommand(ctx As CommandContext, channel As DiscordChannel) As Task
            Dim collection = _database.GetCollection(Of GuildData)("guilds")
            Dim guild = GetGuild(collection, ctx.Guild.Id)

            If Not guild.ProhibitedTextIds.Contains(channel.Id) Or Not guild.ProhibitedVoiceIds.Contains(channel.Id) Then
                Await ctx.RespondAsync("That channel is not on the blacklist.")
                Return
            End If

            Select Case channel.Type
                Case ChannelType.Voice
                    guild.ProhibitedVoiceIds.Remove(channel.Id)
                Case ChannelType.Text
                    guild.ProhibitedTextIds.Remove(channel.Id)
                Case Else
                    Await ctx.RespondAsync($"That channel type cannot be blacklisted, therefore it cannot be unblacklisted.")
                    Return
            End Select

            collection.Upsert(guild)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("clear")>
        <Description("Clears the blacklist, permitting Raymond to interact with previously blacklisted channels.")>
        Public Async Function ClearCommand(ctx As CommandContext) As Task
            Dim collection = _database.GetCollection(Of GuildData)("guilds")
            Dim guild = GetGuild(collection, ctx.Guild.Id)

            guild.ProhibitedTextIds.Clear()
            guild.ProhibitedVoiceIds.Clear()
            collection.Upsert(guild)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        Protected Function GetGuild(collection As ILiteCollection(Of GuildData), guildId As ULong) As GuildData
            Dim guild = collection.FindOne(Function(g) g.GuildId = guildId)

            If guild Is Nothing Then
                guild = New GuildData With {
                    .GuildId = guildId
                }
            End If

            Return guild
        End Function
    End Class
End Namespace
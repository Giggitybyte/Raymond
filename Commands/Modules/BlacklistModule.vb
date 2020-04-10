Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports DSharpPlus.Entities.DiscordEmbedBuilder
Imports DSharpPlus.Interactivity
Imports LiteDB
Imports Raymond.Database

Namespace Commands.Modules
    <Group("blacklist")>
    <Description("The various subcommands below will allow you to display and modify the blacklist for your server.")>
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
            Dim data As GuildData = _database.GetCollection(Of GuildData).GetGuildData(ctx.Guild.Id)

            If Not data.ProhibitedChannelIds.Any Then
                Await ctx.RespondAsync("There aren't any channels blacklisted.")
                Return
            End If

            Dim channels = (Await ctx.Guild.GetChannelsAsync).Where(Function(c) data.ProhibitedChannelIds.Contains(c.Id)) _
                                                             .OrderBy(Function(c) c.Type)
            If Not channels.Any Then
                Await ctx.RespondAsync("No blacklisted channels could be found.")
                Return
            End If

            Dim strBuilder As New StringBuilder
            For Each channel In channels
                strBuilder.AppendLine($"{If(channel.Type = ChannelType.Text, "[T]", "[V]")} {channel.Name} ({channel.Id})")
            Next

            Dim embed As New DiscordEmbedBuilder With {
                .Color = DiscordColor.CornflowerBlue,
                .Footer = New EmbedFooter With {
                    .Text = $"Total channels: {channels.Count}"
                }
            }

            Dim interactivity = ctx.Client.GetInteractivity
            Dim pages = interactivity.GeneratePagesInEmbed(strBuilder.ToString, SplitType.Line, embed)

            Await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages, New PaginationEmojis)
        End Function

        <Command("add")>
        <Description("Adds channels to the blacklist, preventing Raymond from interacting with those channels.")>
        Public Async Function AddCommand(ctx As CommandContext, ParamArray channels As DiscordChannel()) As Task
            If Not channels.Any Then
                Await ctx.RespondAsync("You must provide as least one channel to blacklist.")
                Return
            End If

            Dim blacklisted As New List(Of ULong)
            Dim collection = _database.GetCollection(Of GuildData)
            Dim data = collection.GetGuildData(ctx.Guild.Id)

            For Each channel In channels
                If Not data.ProhibitedChannelIds.Contains(channel.Id) AndAlso
                        channel.Type = ChannelType.Text Or channel.Type = ChannelType.Voice Then
                    blacklisted.Add(channel.Id)
                End If
            Next

            data.ProhibitedChannelIds.AddRange(blacklisted)
            collection.Update(data)

            If blacklisted.Count > 0 Then Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))

            If blacklisted.Count < channels.Count Then
                Dim message = $"Some channels you provided were either already in the blacklist or were of an invalid type.{vbCrLf}"
                message &= $"{blacklisted.Count} out of {channels.Count} channels provided were added to the blacklist."
                Await ctx.RespondAsync(message)
            End If
        End Function

        <Command("except")>
        <Description("Adds all voice and text channels to the blacklist, with the exception the specified channels.")>
        Public Async Function ExceptCommand(ctx As CommandContext, ParamArray exemptChannels As DiscordChannel()) As Task
            If Not exemptChannels.Any Then
                Await ctx.RespondAsync("At least one exempt channel must be provided.")
                Return
            End If

            ' Verify exempt channels are from the guild in context.
            Dim channels = Await ctx.Guild.GetChannelsAsync
            exemptChannels = exemptChannels.Where(Function(c) channels.Contains(c)).ToArray

            If Not exemptChannels.Any Then
                Await ctx.RespondAsync("All provided channels were invalid.")
                Return
            End If

            ' Get all channels that will be blacklisted.
            Dim collection = _database.GetCollection(Of GuildData)
            Dim data = collection.GetGuildData(ctx.Guild.Id)

            Dim exemptIds = exemptChannels.Select(Function(c) c.Id)
            Dim blacklistedChannels = channels.Where(Function(c) Not (exemptIds.Contains(c.Id) _
                                                                 OrElse data.ProhibitedChannelIds.Contains(c.Id)))
            If blacklistedChannels.Count = 0 Then
                Await ctx.RespondAsync("All channels, sans the provided ones, have already been blacklisted.")
                Return
            End If

            ' Blacklist channels.
            For Each channel In blacklistedChannels
                If channel.Type = ChannelType.Voice Or channel.Type = ChannelType.Text Then
                    data.ProhibitedChannelIds.Add(channel.Id)
                End If
            Next

            collection.Update(data)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))

            If blacklistedChannels.Count < exemptChannels.Count Then
                Dim message = $"Some channels you provided were invalid.{vbCrLf}"
                message &= $"{blacklistedChannels.Count} out of {exemptChannels.Count} channels provided were added to the blacklist."
                Await ctx.RespondAsync(message)
            End If
        End Function

        <Command("remove")>
        <Description("Removing a channel from the blacklist will allow Raymond to interact with it and grace it with his presence.")>
        Public Async Function RemoveCommand(ctx As CommandContext, channel As DiscordChannel) As Task
            Dim collection = _database.GetCollection(Of GuildData)
            Dim data = collection.GetGuildData(ctx.Guild.Id)

            If Not data.ProhibitedChannelIds.Contains(channel.Id) Then
                Await ctx.RespondAsync("That channel is not on the blacklist.")
                Return
            End If

            Select Case channel.Type
                Case ChannelType.Voice, ChannelType.Text
                    data.ProhibitedChannelIds.Remove(channel.Id)
                Case Else
                    Await ctx.RespondAsync($"That channel type cannot be blacklisted, therefore it cannot be unblacklisted.")
                    Return
            End Select

            collection.Update(data)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("clear")>
        <Description("Clears the blacklist, permitting Raymond to interact with previously blacklisted channels.")>
        Public Async Function ClearCommand(ctx As CommandContext) As Task
            Dim collection = _database.GetCollection(Of GuildData)
            Dim data = collection.GetGuildData(ctx.Guild.Id)

            data.ProhibitedChannelIds.Clear()
            collection.Update(data)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function
    End Class
End Namespace
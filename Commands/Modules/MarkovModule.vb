Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Raymond.Generators
Imports Raymond.Generators.MarkovChain

Namespace Commands.Modules
    <Group("markov")>
    <Description("Contains subcommands relating to the markov generator. See help for each subcommand.")>
    <RequireUserPermissions(Permissions.ManageGuild)>
    Public Class MarkovModule
        Inherits BaseCommandModule

        Private _generator As MarkovGenerator

        Public Sub New(generators As IEnumerable(Of IGenerator))
            Dim gen = generators.First(Function(g) TypeOf g Is MarkovGenerator)
            _generator = CType(gen, MarkovGenerator)
        End Sub

        <Command("train")>
        <Description("Retrieves the last 500 messages from the specified text channels and trains the markov generator using the content of those messages." + vbCrLf +
                     "Ignores blacklisted channels and messages from bots.")>
        Public Async Function TrainCommand(ctx As CommandContext, ParamArray channels As DiscordChannel()) As Task
            channels = channels.Where(Function(c) c.Type = ChannelType.Text).ToArray

            If Not channels.Any Then
                Await ctx.RespondAsync("You must specify at least one text channel.")
                Return
            End If

            For Each channel In channels
                For Each message In Await channel.GetMessagesAsync(500)
                    _generator.TrainMarkov(ctx.Guild, message)
                Next
            Next

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("reset")>
        <Description("Deletes the markov chain for this server then reinitializes it using 20 messages from each non-blacklisted text channel.")>
        Public Async Function ResetCommand(ctx As CommandContext) As Task
            Dim loading = DiscordEmoji.FromGuildEmote(ctx.Client, 534230967574069249)
            Dim okay = DiscordEmoji.FromName(ctx.Client, ":ok_hand:")

            Await ctx.Message.CreateReactionAsync(loading)
            Await _generator.ReinitializeMarkovAsync(ctx.Guild)
            Await ctx.Message.CreateReactionAsync(okay)
            Await ctx.Message.DeleteOwnReactionAsync(loading)
        End Function

        <Command("generate")>
        <Description("Creates a 'sentence' using the markov chain for your server. This can be used to get an idea of what Raymond " +
                     "would say when the markov generator is enabled so you can retrain it if desired.")>
        Public Async Function GenerateCommand(ctx As CommandContext) As Task
            Dim sentence = _generator.CreateSentence(ctx.Guild).Sentence
            Await ctx.RespondAsync(Formatter.BlockCode(sentence))
        End Function
    End Class
End Namespace
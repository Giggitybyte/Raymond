Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports LiteDB
Imports Raymond.Database
Imports Raymond.Extensions
Imports Raymond.Services

Namespace Commands.Modules
    <Group("generator"), Aliases("gen", "g")>
    <Description("The 'set' sub command will allow you to change which generator will be used for your server, " + vbCrLf +
                 "while the 'list' sub command will display all available generators for you to choose from.")>
    <RequireUserPermissions(Permissions.ManageGuild)>
    Public Class GeneratorModule
        Inherits BaseCommandModule

        Private _db As LiteDatabase
        Private _sentence As SentenceService

        Public Sub New(db As LiteDatabase, sentence As SentenceService)
            _db = db
            _sentence = sentence
        End Sub

        <Command("set")>
        <Description("Sets the generator for your server.")>
        Public Async Function SetCommand(ctx As CommandContext, generator As String) As Task
            If Not _sentence.Generators.Any(Function(g) g.Name = generator.ToLower) Then
                Await ctx.RespondAsync("Invalid generator.")
                Return
            End If

            Dim collection = _db.GetCollection(Of GuildData)
            Dim data = collection.GetGuildData(ctx.Guild.Id)

            data.Generator = generator.ToLower
            collection.Update(data)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("list")>
        <Description("Displays all available generators.")>
        Public Async Function ListCommand(ctx As CommandContext) As Task
            Dim strBuilder As New StringBuilder
            strBuilder.AppendLine("Available Generators:").AppendLine()

            For Each generator In _sentence.Generators
                strBuilder.AppendLine(generator.Name)
            Next

            strBuilder.Append("random")
            Await ctx.RespondAsync(Formatter.BlockCode(strBuilder.ToString))
        End Function
    End Class
End Namespace
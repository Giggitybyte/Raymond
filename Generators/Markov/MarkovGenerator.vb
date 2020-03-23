Imports System.IO
Imports System.Text.RegularExpressions
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports LiteDB
Imports MarkovNextGen
Imports Raymond.Database
Imports Raymond.Extensions

Namespace Generators.MarkovChain
    ''' <summary>
    ''' A set of markov generators trained from Discord user messages.
    ''' </summary>
    Public Class MarkovGenerator
        Implements IGenerator

        Private _db As LiteDatabase
        Private _markovs As Dictionary(Of ULong, Markov)
        Private _discordRegex As New Regex("<(?:[^\d>]+|:[A-Za-z0-9]+:)\w+>", RegexOptions.Compiled)
        Private _puncuationRegex As New Regex("\w(?:[\w'-]*\w)?", RegexOptions.Compiled)

        Public Sub New(discord As DiscordClient, database As LiteDatabase)
            Directory.CreateDirectory("Markov")

            _db = database
            _markovs = New Dictionary(Of ULong, Markov)

            AddHandler discord.GuildAvailable, AddressOf GuildAvailableHandler
            AddHandler discord.MessageCreated, AddressOf MessageCreatedHandler
        End Sub

        Public ReadOnly Property Name As String Implements IGenerator.Name
            Get
                Return "markov"
            End Get
        End Property

        Public Function CreateSentence(guild As DiscordGuild) As GeneratorResult Implements IGenerator.CreateSentence
            Dim length = Random.NextNumber(15, 20)
            Return New GeneratorResult With {
                .Sentence = _markovs(guild.Id).Generate(length),
                .TtsVoice = "en-AU-Wavenet-B"
            }
        End Function

        ''' <summary>
        ''' Adds existing markov generators to the dictionary and creates new generators as needed.
        ''' </summary>
        Private Function GuildAvailableHandler(e As GuildCreateEventArgs) As Task
            If _markovs.ContainsKey(e.Guild.Id) Then Return Task.CompletedTask

            If File.Exists($"Markov/{e.Guild.Id}.pdo") Then
                _markovs.Add(e.Guild.Id, New Markov($"Markov/{e.Guild.Id}.pdo"))
            Else
                Task.Run(Function() CreateMarkovAsync(e.Guild))
            End If

            Return Task.CompletedTask
        End Function

        ''' <summary>
        ''' Adds the content of a message to its guild markov generator.
        ''' </summary>
        Private Function MessageCreatedHandler(e As MessageCreateEventArgs) As Task
            Dim data = _db.GetCollection(Of GuildData).GetGuildData(e.Guild.Id)

            If e.Message.Author.IsBot _
               OrElse Not data.Generator = "markov" _
               Or data.ProhibitedChannelIds.Contains(e.Channel.Id) _
               Then Return Task.CompletedTask

            _markovs(e.Guild.Id).AddToChain(_discordRegex.Replace(e.Message.Content, ""))
            Return Task.CompletedTask
        End Function

        ''' <summary>
        ''' Creates and trains a markov generator, then adds it to the dictionary.
        ''' </summary>
        Private Async Function CreateMarkovAsync(guild As DiscordGuild) As Task
            Dim data = _db.GetCollection(Of GuildData).GetGuildData(guild.Id)
            Dim channels = (Await guild.GetChannelsAsync).Where(Function(c) Not data.ProhibitedChannelIds.Contains(c.Id))
            Dim markov As New Markov($"Markov/{guild.Id}.pdo")

            For Each channel In channels
                Dim messages = (Await channel.GetMessagesAsync(300)).Where(Function(m) Not m.Author.IsBot)

                For Each message In messages
                    Dim text = _discordRegex.Replace(message.Content, "")
                    text = String.Join(" ", _puncuationRegex.Matches(text))
                    markov.AddToChain(text)
                Next
            Next

            _markovs.Add(guild.Id, markov)
        End Function
    End Class
End Namespace
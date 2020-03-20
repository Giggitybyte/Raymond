Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.VoiceNext
Imports LiteDB
Imports Raymond.Database
Imports Raymond.Generators

Namespace Services
    Public Class PhraseService
        Private _database As LiteDatabase
        Private _discord As DiscordClient
        Private _generators As WeightedGeneratorBag
        Private _logger As LogService
        Private _numbers As NumberService
        Private _timers As Dictionary(Of ULong, Timer)
        Private _tts As GoogleTtsService

        Public Sub New(db As LiteDatabase, discord As DiscordClient, tts As GoogleTtsService, numbers As NumberService, logger As LogService)
            _database = db
            _discord = discord
            _logger = logger
            _timers = New Dictionary(Of ULong, Timer)
            _tts = tts
            _numbers = numbers

            _generators = New WeightedGeneratorBag
            InitializeGenerators()

            AddHandler _discord.GuildAvailable, AddressOf GuildAvailableHandler
        End Sub

        Private Async Function GuildAvailableHandler(e As GuildCreateEventArgs) As Task
            If _timers.ContainsKey(e.Guild.Id) Then Return

            _timers.Add(e.Guild.Id, New Timer(AddressOf PhraseTrigger, e.Guild.Id, 5000, -1))
            Await _logger.PrintAsync(LogLevel.Info, "Phrase Service", $"Added a new timer for {e.Guild.Id}")
        End Function

        Private Async Sub PhraseTrigger(state As Object)
            ' Get guild.
            Dim guild As DiscordGuild = Nothing
            _discord.Guilds.TryGetValue(CType(state, ULong), guild)

            If guild Is Nothing Then
                Dim timer As Timer = Nothing
                _timers.Remove(CType(state, ULong), timer)
                timer.Dispose()
                Return
            End If

            ' Pick the most populated, non-blacklisted voice channel.
            Dim blacklist = GetChannelBlacklist(guild.Id)
            Dim channels = (Await guild.GetChannelsAsync()).Where(Function(c) Not blacklist.Contains(c.Id)) _
                                                           .Where(Function(c) c.Type = ChannelType.Voice AndAlso c.Users.Any) _
                                                           .OrderByDescending(Function(c) c.Users.Count) _
                                                           .ToList

            If guild.AfkChannel IsNot Nothing AndAlso channels.Contains(guild.AfkChannel) Then channels.Remove(guild.AfkChannel)

            ' Send phrase if there are any valid channels.
            If channels.Any Then
                Await SendPhraseAsync(channels.First)
            Else
                Await _logger.PrintAsync(LogLevel.Debug, "Phrase Service", $"No valid channels could be found for {guild.Id}; skipping.")
            End If

            ' Set timer to fire again anywhere between 2 and 5 days.
            Dim time = TimeSpan.FromMilliseconds(_numbers.RandomNumber(172800000, 432000000))
            _timers(guild.Id).Change(time, Timeout.InfiniteTimeSpan)

            Await _logger.PrintAsync(LogLevel.Info, "Phrase Service", $"Next appearance for {guild.Id}: {time.Days} days, {time.Hours} hours, {time.Minutes} minutes, {time.Seconds} seconds.")
        End Sub

        Private Async Function SendPhraseAsync(channel As DiscordChannel) As Task
            ' Generator selection.
            Dim generator = _generators.GetGenerator(_numbers)

            ' Generate phrase.
            Await _logger.PrintAsync(LogLevel.Info, "Phrase Service", $"{generator.GetType.Name} will be generating a phrase.")
            Dim phrase = generator.GeneratePhrase(_numbers)

            ' Send phrase to provided voice channel.
            Await SendPhraseAsync(phrase, channel)
        End Function

        Public Async Function SendPhraseAsync(text As String, channel As DiscordChannel) As Task
            ' Send phrase to provided voice channel.
            Dim speech = Await _tts.SynthesizeAsync(text)
            Dim voiceChn = Await channel.ConnectAsync()

            Await _logger.PrintAsync(LogLevel.Debug, "Phrase Service", $"Sending phrase to {channel.Id}.")
            Await voiceChn.SendSpeakingAsync()

            Dim transmit = voiceChn.GetTransmitStream()

            Using ffmpeg = CreateFfmpeg()
                Dim input = ffmpeg.StandardInput.BaseStream
                speech.WriteTo(input)
                Await input.DisposeAsync

                Dim output = ffmpeg.StandardOutput.BaseStream
                Await output.CopyToAsync(transmit)
                Await transmit.FlushAsync()
            End Using

            Await _logger.PrintAsync(LogLevel.Debug, "Phrase Service", $"Waiting for phrase to finish... ({channel.Id})")
            Await voiceChn.WaitForPlaybackFinishAsync

            Await voiceChn.SendSpeakingAsync(False)
            Await transmit.DisposeAsync
            voiceChn.Disconnect()

            Await _logger.PrintAsync(LogLevel.Debug, "Phrase Service", $"Phrase playback completed in {channel.Id}.")
        End Function

        Protected Function GetChannelBlacklist(guildId As ULong) As IReadOnlyList(Of ULong)
            Dim collection = _database.GetCollection(Of GuildBlacklist)("blacklists")
            Dim blacklist = collection.FindOne(Function(g) g.GuildId = guildId)

            If blacklist Is Nothing Then
                blacklist = New GuildBlacklist With {
                    .GuildId = guildId,
                    .ChannelIds = New List(Of ULong)
                }

                collection.Insert(blacklist)
            End If

            Return blacklist.ChannelIds.AsReadOnly
        End Function

        Protected Sub InitializeGenerators()
            Dim interfaceType = GetType(IPhraseGenerator)
            Dim types = interfaceType.Assembly.GetTypes.Where(Function(t) t IsNot interfaceType AndAlso
                                                                  interfaceType.IsAssignableFrom(t)).ToList

            Dim instances As New List(Of IPhraseGenerator)
            types.ForEach(Sub(t) instances.Add(CType(Activator.CreateInstance(t), IPhraseGenerator)))
            instances.OrderBy(Function(i) i.Chance).ToList.ForEach(Sub(i) _generators.AddEntry(i))
        End Sub

        Protected Function CreateFfmpeg() As Process
            Return Process.Start(New ProcessStartInfo With {
                .FileName = "ffmpeg",
                .Arguments = $"-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardInput = True
            })
        End Function

        Protected Class WeightedGeneratorBag ' https://gamedev.stackexchange.com/a/162987
            Private _entries As New List(Of Entry)
            Private _accumulatedWeight As Double

            Public Sub AddEntry(item As IPhraseGenerator)
                _accumulatedWeight += item.Chance
                _entries.Add(New Entry With {
                    .Generator = item,
                    .AccumulatedWeight = _accumulatedWeight
                })
            End Sub

            Public Function GetGenerator(numberService As NumberService) As IPhraseGenerator
                Dim r As Double = numberService.RandomDouble * _accumulatedWeight

                For Each entry As Entry In _entries
                    If entry.AccumulatedWeight >= r Then
                        Return entry.Generator
                    End If
                Next

                Return Nothing
            End Function

            Private Structure Entry
                Public Property AccumulatedWeight As Double
                Public Property Generator As IPhraseGenerator
            End Structure
        End Class
    End Class
End Namespace
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
        Private _generators As List(Of IPhraseGenerator)
        Private _logger As LogService
        Private _numbers As NumberService
        Private _timers As Dictionary(Of ULong, Timer)
        Private _tts As GoogleTtsService

        Public Sub New(db As LiteDatabase, discord As DiscordClient, tts As GoogleTtsService, numbers As NumberService, logger As LogService)
            _database = db
            _discord = discord
            _generators = GetGenerators().OrderBy(Function(g) g.Chance).ToList
            _logger = logger
            _timers = New Dictionary(Of ULong, Timer)
            _tts = tts
            _numbers = numbers

            AddHandler _discord.GuildAvailable, AddressOf GuildAvailableHandler
        End Sub

        Private Function GuildAvailableHandler(e As GuildCreateEventArgs) As Task
            If Not _timers.ContainsKey(e.Guild.Id) Then _timers.Add(e.Guild.Id, New Timer(AddressOf PhraseTrigger, e.Guild.Id, 5000, -1))
            Return Task.CompletedTask
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

            ' Pick a random populated, non-blacklisted voice channel.
            Dim blacklist = GetChannelBlacklist(guild.Id)
            Dim channels = (Await guild.GetChannelsAsync()).Where(Function(c) Not blacklist.Contains(c.Id)) _
                                                           .Where(Function(c) c.Type = ChannelType.Voice AndAlso c.Users.Any) _
                                                           .OrderByDescending(Function(c) c.Users.Count) _
                                                           .ToList

            If guild.AfkChannel IsNot Nothing AndAlso channels.Contains(guild.AfkChannel) Then channels.Remove(guild.AfkChannel)

            If Not channels.Any Then
                Await _logger.PrintAsync(LogLevel.Debug, "Phrase Service", $"No valid channels could be found for {guild.Id}; skipping.")
                Return
            End If

            ' I bet you can't guess what happens next...
            Await SendPhraseAsync(channels.First)

            ' Set timer to fire again anywhere between 2 and 5 days.
            ' Dim time = TimeSpan.FromMilliseconds(_numbers.RandomNumber(172800000, 432000000))
            Dim time = TimeSpan.FromMilliseconds(_numbers.RandomNumber(5000, 10000))
            _timers(guild.Id).Change(time, Timeout.InfiniteTimeSpan)

            Await _logger.PrintAsync(LogLevel.Info, "Phrase Service", $"Next appearance for {guild.Id}: {time.Days} days, {time.Hours} hours, {time.Minutes} minutes, {time.Seconds} seconds.")
        End Sub

        Public Async Function SendPhraseAsync(channel As DiscordChannel) As Task
            ' Generate phrase.
            Dim dbl = _numbers.RandomDouble
            Dim generator = _generators.Where(Function(g) _numbers.ProbabilityCheck(g.Chance)).First

            Await _logger.PrintAsync(LogLevel.Info, "Phrase Service", $"{generator.GetType.Name} will be generating a phrase.")
            Dim phrase = generator.GeneratePhrase(_numbers)

            ' Send phrase to provided voice channel.
            Dim speech = Await _tts.SynthesizeAsync(phrase)
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

            Await _logger.PrintAsync(LogLevel.Debug, "Phrase Service", $"Phrase playback completed in {channel.Id}")
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

        Protected Function GetGenerators() As List(Of IPhraseGenerator)
            Dim interfaceType = GetType(IPhraseGenerator)
            Dim types = interfaceType.Assembly.GetTypes.Where(Function(t) t IsNot interfaceType AndAlso interfaceType.IsAssignableFrom(t))
            Dim generators As New List(Of IPhraseGenerator)

            For Each generatorType In types
                generators.Add(CType(Activator.CreateInstance(generatorType), IPhraseGenerator))
            Next

            Return generators
        End Function

        Protected Function CreateFfmpeg() As Process
            Return Process.Start(New ProcessStartInfo With {
                .FileName = "ffmpeg",
                .Arguments = $"-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardInput = True
            })
        End Function
    End Class
End Namespace
Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.VoiceNext
Imports LiteDB
Imports Raymond.Database
Imports Raymond.Extensions
Imports Raymond.Generators

Namespace Services
    ''' <summary>
    ''' Synthesizes 'sentences' and other text to be sent Discord voice channels.
    ''' </summary>
    Public Class SentenceService
        Private _database As LiteDatabase
        Private _discord As DiscordClient
        Private _logger As LogService
        Private _timers As Dictionary(Of ULong, Timer)
        Private _tts As GoogleTtsService

        Public ReadOnly Property Generators As List(Of IGenerator)

        Public Sub New(db As LiteDatabase, discord As DiscordClient, tts As GoogleTtsService, logger As LogService, generators As IEnumerable(Of IGenerator))
            _database = db
            _discord = discord
            _Generators = generators.ToList
            _logger = logger
            _timers = New Dictionary(Of ULong, Timer)
            _tts = tts

            AddHandler _discord.GuildAvailable, AddressOf GuildAvailableHandler
            AddHandler _discord.GuildUnavailable, AddressOf GuildUnavailableHandler
        End Sub

        Private Async Function GuildAvailableHandler(e As GuildCreateEventArgs) As Task
            If _timers.ContainsKey(e.Guild.Id) Then Return

            _timers.Add(e.Guild.Id, New Timer(AddressOf GuildTrigger, e.Guild.Id, 5000, -1))
            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Added a new timer for {e.Guild.Id}")
        End Function

        Private Async Function GuildUnavailableHandler(e As GuildDeleteEventArgs) As Task
            If _timers.ContainsKey(e.Guild.Id) Then
                Dim timer As Timer
                _timers.Remove(e.Guild.Id, timer)
                Await timer.DisposeAsync()
            End If
        End Function

        Private Async Sub GuildTrigger(state As Object)
            ' Get guild.
            Dim guild As DiscordGuild = Nothing
            _discord.Guilds.TryGetValue(CULng(state), guild)

            If guild Is Nothing Then
                Dim timer As Timer
                _timers.Remove(CULng(state), timer)
                Await timer.DisposeAsync()
                Return
            End If

            ' Send sentence to the most populated, non-blacklisted voice channel.
            Dim channels = (Await guild.GetChannelsAsync).ToList
            If guild.AfkChannel IsNot Nothing Then channels.Remove(guild.AfkChannel)

            Dim blacklist = _database.GetCollection(Of GuildData).GetGuildData(guild.Id).ProhibitedChannelIds
            Dim validChannels = channels.Where(Function(c) c.Type = ChannelType.Voice _
                                                           AndAlso Not blacklist.Contains(c.Id) _
                                                           AndAlso c.Users.Any).ToList
            If validChannels.Any Then
                Await SendSentenceAsync(validChannels.OrderByDescending(Function(c) c.Users.Count).First)
            Else
                Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Skipping {guild.Id}.")
            End If

            ' Set timer to fire again anywhere between 2 and 5 days.
            Dim time = TimeSpan.FromMilliseconds(Random.NextNumber(172800000, 432000000))
            _timers(guild.Id).Change(time, Timeout.InfiniteTimeSpan)

            Await _logger.PrintAsync(LogLevel.Info, "Sentence Service", $"Next appearance for {guild.Id}: {Date.Now.Add(time).ToString}")
        End Sub

        Private Async Function SendSentenceAsync(channel As DiscordChannel) As Task
            ' Get generator
            Dim data = _database.GetCollection(Of GuildData).GetGuildData(channel.Guild.Id)
            Dim generator = _Generators.FirstOrDefault(Function(g) g.Name = data.Generator)
            If generator Is Nothing Then Throw New NotImplementedException($"Generator not implemented: {data.Generator}")

            ' Send sentence.
            Dim result = generator.CreateSentence(channel.Guild)
            Await SendSentenceAsync(channel, result.Sentence, result.TtsVoice)
        End Function

        Public Async Function SendSentenceAsync(channel As DiscordChannel, text As String, voice As String) As Task
            Dim speech = Await _tts.SynthesizeAsync(text, voice)
            Dim voiceChn As VoiceNextConnection
            Dim transmit As VoiceTransmitStream

            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Speaking in channel {channel.Id}.")

            Using ffmpeg = CreateFfmpeg()
                Dim input = ffmpeg.StandardInput.BaseStream
                speech.WriteTo(input)
                Await input.DisposeAsync

                voiceChn = Await channel.ConnectAsync()
                transmit = voiceChn.GetTransmitStream()

                Dim output = ffmpeg.StandardOutput.BaseStream
                Await output.CopyToAsync(transmit)
                Await transmit.FlushAsync()
            End Using

            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Waiting for speaking to finish... ({channel.Id})")
            Await voiceChn.WaitForPlaybackFinishAsync

            Await transmit.DisposeAsync
            voiceChn.Disconnect()

            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Finished speaking in channel {channel.Id}.")
        End Function

        Private Function CreateFfmpeg() As Process
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
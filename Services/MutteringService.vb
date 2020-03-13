Imports System.IO
Imports System.Timers
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.VoiceNext

Namespace Services
    Public Class MutteringService
        Private _discord As DiscordClient
        Private _logger As LogService
        Private _timer As Timer
        Private _tts As GoogleTtsService
        Private _random As Random
        Private _voiceChannelId As ULong
        Private _verbs, _adjectives, _nouns, _quirkyPhrases As List(Of String)

        Sub New(config As Dictionary(Of String, String), discord As DiscordClient, tts As GoogleTtsService, logger As LogService)
            _discord = discord
            _logger = logger
            _timer = New Timer(5000)
            _tts = tts
            _random = New Random()
            _voiceChannelId = CULng(config("discord.voicechannel.prod"))

            For Each wordType In {"verbs", "adjectives", "nouns", "woke"}
                Using fileReader As New StreamReader($"Words/{wordType}.txt")
                    Dim words = fileReader.ReadToEnd.Split(vbCrLf).ToList

                    Select Case wordType
                        Case "verbs"
                            _verbs = words
                        Case "adjectives"
                            _adjectives = words
                        Case "nouns"
                            _nouns = words
                        Case "woke"
                            _quirkyPhrases = words
                    End Select
                End Using
            Next

            AddHandler _timer.Elapsed, AddressOf TimerHandler

            _timer.Start()
        End Sub

        Public Async Function SendPhraseAsync(Optional phrase As String = Nothing) As Task
            Dim speech = Await _tts.SynthesizeAsync(If(phrase, GeneratePhrase()))
            Dim channel = Await _discord.GetChannelAsync(_voiceChannelId)
            Dim voiceChn = Await channel.ConnectAsync()

            Await _discord.UpdateStatusAsync(userStatus:=UserStatus.DoNotDisturb)
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

            Await voiceChn.WaitForPlaybackFinishAsync

            Await voiceChn.SendSpeakingAsync(False)
            Await _discord.UpdateStatusAsync(userStatus:=UserStatus.Invisible)

            voiceChn.Disconnect()
        End Function

        Private Function GeneratePhrase() As String
            ' Roll for rare phrases.
            Dim quirkyRoll = _random.NextDouble
            Dim percentage = Math.Round(quirkyRoll * 100, 2, MidpointRounding.AwayFromZero)
            _logger.Print(LogLevel.Info, "Mutter", $"Rolled {percentage}% for quirky lines.")

            ' If probability hit, return a rare phrase. 
            If quirkyRoll <= 0.05 Then
                _logger.Print(LogLevel.Info, "Mutter", "Probability hit!")
                Dim index = _random.Next(_quirkyPhrases.Count)
                Return _quirkyPhrases(index)
            End If

            ' Pick random words.
            Dim verb = _verbs(_random.Next(_verbs.Count))
            Dim adjective = _adjectives(_random.Next(_adjectives.Count))
            Dim noun = _nouns(_random.Next(_nouns.Count))

            ' You'll never guess.
            Return $"{verb} {adjective} {noun}"
        End Function

        Private Async Sub TimerHandler(sender As Object, e As ElapsedEventArgs)
            ' Pick random time between 2 and 5 days for the next event.
            Dim time = _random.Next(172800000, 432000000)
            Dim ts = TimeSpan.FromMilliseconds(time)

            ' Set timer.
            _timer.Interval = time

            ' Play phrase if voice channel is populated.
            Dim channel = Await _discord.GetChannelAsync(_voiceChannelId)
            If channel.Users.Any Then
                Await _logger.PrintAsync(LogLevel.Info, "Mutter", "Voice channel populated.")
                Await SendPhraseAsync()
            Else
                Await _logger.PrintAsync(LogLevel.Info, "Mutter", "Voice channel empty.")
            End If

            Await _logger.PrintAsync(LogLevel.Info, "Mutter", $"Event will fire again in {ts.Days}d:{ts.Hours}h:{ts.Minutes}m")
        End Sub

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
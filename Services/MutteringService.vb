Imports System.IO
Imports System.Timers
Imports Discord
Imports Discord.Audio
Imports Discord.Commands
Imports Discord.WebSocket
Imports Serilog

Namespace Services
    Public Class MutteringService
        Private _verbs, _adjectives, _nouns, _quirkyPhrases As List(Of String)
        Private _random As Random
        Private _timer As Timer

        Sub New()
            For Each wordType In {"verbs", "adjectives", "nouns", "woke"}
                Using fileReader As New StreamReader($"Words/{wordType}.txt")
                    Dim words = fileReader.ReadToEnd.Split(",").ToList

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

            _random = New Random()
            _timer = New Timer(3000)
            _timer.Start()

            AddHandler _timer.Elapsed, AddressOf TimerHandler
            AddHandler Bot.DiscordClient.MessageReceived, AddressOf MentionHandler
        End Sub

        Public Async Function SendPhraseAsync(Optional phrase As String = Nothing) As Task
            Await Bot.Azure.SynthesizeAsync(If(phrase, GeneratePhrase()))

            Dim audioClient = Await GetChannel().ConnectAsync
            Await audioClient.SetSpeakingAsync(True)
            Await Bot.DiscordClient.SetStatusAsync(UserStatus.DoNotDisturb)

            Using ffmpeg = CreateFfmpeg("synth.mp3")
                Using output = ffmpeg.StandardOutput.BaseStream
                    Using voiceChannel = audioClient.CreatePCMStream(AudioApplication.Mixed)
                        Try
                            output.CopyTo(voiceChannel)
                        Finally
                            voiceChannel.Flush()
                        End Try
                    End Using
                End Using
            End Using

            Await audioClient.SetSpeakingAsync(False)
            Await audioClient.StopAsync
            Await Bot.DiscordClient.SetStatusAsync(UserStatus.Invisible)
        End Function

        Private Function GeneratePhrase() As String
            Dim quirkyRoll = _random.Next(0, 101)
            Log.Verbose($"Rolled {quirkyRoll} for quirky lines.")

            If quirkyRoll = 50 Then
                Log.Verbose("Thank you, RNGesus.")
                Dim index = _random.Next(_quirkyPhrases.Count)
                Return _quirkyPhrases(index)
            End If

            Dim verb = _verbs(_random.Next(_verbs.Count))
            Dim adjective = _adjectives(_random.Next(_adjectives.Count))
            Dim noun = _nouns(_random.Next(_nouns.Count))

            Return $"{verb} {adjective} {noun}"
        End Function

        Private Async Sub TimerHandler(sender As Object, e As ElapsedEventArgs)
            Dim time = _random.Next(172800000, 432000000)
            Dim ts = TimeSpan.FromMilliseconds(time)
            _timer.Interval = time

            If Not GetChannel.Users.Count = 0 Then
                Log.Information("Voice channel populated; playing phrase.")
                Await SendPhraseAsync()
            Else
                Log.Information("Voice channel empty; nothing to do.")
            End If

            Log.Verbose($"Timer will fire again in {ts.Days}d:{ts.Hours}h:{ts.Minutes}m")
        End Sub

        Private Function MentionHandler(msg As SocketMessage) As Task
            Dim message = TryCast(msg, SocketUserMessage)
            Dim argPos As Integer

            If message Is Nothing OrElse
                Not message.HasMentionPrefix(Bot.DiscordClient.CurrentUser, argPos) OrElse
                message.Author.IsBot Then Return Task.CompletedTask

            Return Task.Run(Async Sub() Await SendPhraseAsync(message.Content.Substring(argPos)))
        End Function

        Private Function CreateFfmpeg(ByVal path As String) As Process
            Return Process.Start(New ProcessStartInfo With {
                .FileName = "ffmpeg",
                .Arguments = $"-hide_banner -loglevel panic -i ""{path}"" -ac 2 -f s16le -ar 48000 pipe:1",
                .UseShellExecute = False,
                .RedirectStandardOutput = True
            })
        End Function

        Private Function GetChannel() As SocketVoiceChannel
            Dim chnId = ULong.Parse(Bot.Configuration("discord.voicechannel"))
            Return CType(Bot.DiscordClient.GetChannel(chnId), SocketVoiceChannel)
        End Function
    End Class
End Namespace
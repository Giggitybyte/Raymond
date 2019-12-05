Imports System.IO
Imports System.Timers
Imports Discord
Imports Discord.Audio
Imports Discord.WebSocket

Namespace Services
    Public Class MutteringService
        Private _timer As Timer
        Private _verbs, _adjectives, _nouns As List(Of String)
        Private _discord As DiscordSocketClient
        Private _azure As AzureSpeechService
        Private _random As Random

        Sub New(client As DiscordSocketClient, azure As AzureSpeechService)
            For Each wordType In {"verbs", "adjectives", "nouns"}
                Using fileReader As New StreamReader($"Words/{wordType}.txt")
                    Dim words = fileReader.ReadToEnd.Split(",").ToList

                    Select Case wordType
                        Case "verbs"
                            _verbs = words
                        Case "adjectives"
                            _adjectives = words
                        Case "nouns"
                            _nouns = words
                    End Select
                End Using
            Next

            _azure = azure
            _discord = client
            _random = New Random()
            _timer = New Timer(1000)

            AddHandler _timer.Elapsed, AddressOf TimerHandler
            _timer.Start()
        End Sub

        Public Async Function SendPhraseAsync(Optional phrase As String = Nothing) As Task
            Await _azure.SynthesizeAsync(If(phrase, GeneratePhrase()))

            Dim audioClient = Await GetChannel().ConnectAsync
            Await audioClient.SetSpeakingAsync(True)
            Await _discord.SetStatusAsync(UserStatus.DoNotDisturb)

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
            Await _discord.SetStatusAsync(UserStatus.Invisible)
        End Function

        Private Function GeneratePhrase() As String
            Dim verb = _verbs(_random.Next(_verbs.Count))
            Dim adjective = _adjectives(_random.Next(_adjectives.Count))
            Dim noun = _nouns(_random.Next(_nouns.Count))

            Return $"{verb} {adjective} {noun}"
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
            Return CType(_discord.GetChannel(chnId), SocketVoiceChannel)
        End Function

        Private Async Sub TimerHandler(sender As Object, e As ElapsedEventArgs)
            '_timer.Interval = _random.Next(5000, 15000)

            _timer.Interval = _random.Next(21600000, 43200000)
            If GetChannel.Users.Count > 0 Then Await SendPhraseAsync()
        End Sub
    End Class
End Namespace
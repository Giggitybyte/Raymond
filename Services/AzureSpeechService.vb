Imports Microsoft.CognitiveServices.Speech
Imports Microsoft.CognitiveServices.Speech.Audio
Imports Serilog

Namespace Services
    Public Class AzureSpeechService
        Private _random As New Random
        Private _azureConfig As SpeechConfig
        Private _voices As New List(Of String) From {
            "en-US-ZiraRUS",
            "en-US-JessaRUS",
            "en-US-BenjaminRUS",
            "en-US-Jessa24kRUS",
            "en-US-Guy24kRUS"
        }

        Sub New()
            _azureConfig = SpeechConfig.FromSubscription(Bot.Configuration("token.azure"), "westus")
            _azureConfig.SetProfanity(ProfanityOption.Raw)
            _azureConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3)
        End Sub

        Public Async Function SynthesizeAsync(text As String) As Task(Of String) ' RETURN FILE PATH
            Dim stream = AudioOutputStream.CreatePullStream
            Dim rawAudio = New Byte() {}

            _azureConfig.SpeechSynthesisVoiceName = _voices(_random.Next(0, _voices.Count))

            Using output = AudioConfig.FromWavFileOutput($"synth.mp3")
                Using synthesizer As New SpeechSynthesizer(_azureConfig, output)
                    Dim result = Await synthesizer.SpeakTextAsync(text).ConfigureAwait(False)

                    If result.Reason = ResultReason.SynthesizingAudioCompleted Then
                        rawAudio = result.AudioData
                        Log.Information($"Azure speech synthesis successfully completed. Text: {text}")
                    Else
                        Log.Warning($"Azure speech synthesis completed with non-success reason: {result.Reason}. Text: {text}")
                    End If
                End Using
            End Using


        End Function
    End Class
End Namespace
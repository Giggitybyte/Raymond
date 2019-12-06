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

        Public Async Function SynthesizeAsync(text As String) As Task
            Dim stream = AudioOutputStream.CreatePullStream

            _azureConfig.SpeechSynthesisVoiceName = _voices(_random.Next(0, _voices.Count))

            Using output = AudioConfig.FromWavFileOutput($"synth.mp3")
                Using synthesizer As New SpeechSynthesizer(_azureConfig, output)
                    Dim result = Await synthesizer.SpeakTextAsync(text).ConfigureAwait(False)

                    If result.Reason = ResultReason.SynthesizingAudioCompleted Then
                        Log.Verbose("Azure speech synthesis successfully completed.")
                        Log.Verbose($"Text used: {text}")
                    Else
                        Log.Warning($"Azure speech synthesis completed with non-success reason: {result.Reason}")
                        If result.Reason = ResultReason.Canceled Then
                            Dim reason = SpeechSynthesisCancellationDetails.FromResult(result)
                            Log.Warning(reason.ErrorDetails)
                        End If
                        Log.Warning($"Text used: {text}")
                    End If
                End Using
            End Using
        End Function
    End Class
End Namespace
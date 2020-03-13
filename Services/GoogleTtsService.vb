Imports DSharpPlus
Imports Google.Cloud.TextToSpeech.V1
Imports Google.Protobuf

Namespace Services
    Public Class GoogleTtsService
        Private _client As TextToSpeechClient
        Private _logger As LogService

        Public Sub New(logger As LogService)
            _client = TextToSpeechClient.Create()
            _logger = logger
        End Sub

        Public Async Function SynthesizeAsync(phrase As String) As Task(Of ByteString)
            Await _logger.PrintAsync(LogLevel.Info, "Google TTS", $"Synthsizing phrase: {phrase}")

            Dim config As New AudioConfig With {
                .AudioEncoding = AudioEncoding.Mp3
            }

            Dim voice As New VoiceSelectionParams With {
                .LanguageCode = "en-US",
                .SsmlGender = SsmlVoiceGender.Male
            }

            Dim input As New SynthesisInput With {
                .Text = phrase
            }

            Dim response = Await _client.SynthesizeSpeechAsync(New SynthesizeSpeechRequest With {
                .AudioConfig = config,
                .Input = input,
                .Voice = voice
            })

            Return response.AudioContent
        End Function
    End Class
End Namespace
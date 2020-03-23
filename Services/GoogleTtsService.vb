Imports DSharpPlus
Imports Google.Cloud.TextToSpeech.V1
Imports Google.Protobuf

Namespace Services
    Public Class GoogleTtsService
        Private _client As TextToSpeechClient
        Private _logger As LogService
        Private _validVoices As List(Of String)

        Public Sub New(logger As LogService)
            _client = TextToSpeechClient.Create()
            _logger = logger

            Dim req As New ListVoicesRequest With {.LanguageCode = "en-US"}
            _validVoices = _client.ListVoices(req).Voices _
                                  .Where(Function(v) v.SsmlGender = SsmlVoiceGender.Male) _
                                  .Select(Function(v) v.Name) _
                                  .ToList
        End Sub

        Public Async Function SynthesizeAsync(sentence As String, Optional voice As String = Nothing) As Task(Of ByteString)
            If voice Is Nothing Then voice = "en-US-Wavenet-D"
            If Not _validVoices.Contains(voice) Then Throw New ArgumentException($"{voice} is an invalid TTS voice.")
            Await _logger.PrintAsync(LogLevel.Info, "Google TTS", $"Synthesizing sentence: {sentence}")

            Dim config As New AudioConfig With {
                .AudioEncoding = AudioEncoding.Mp3
            }

            Dim voiceParams As New VoiceSelectionParams With {
                .LanguageCode = "en-US",
                .SsmlGender = SsmlVoiceGender.Male,
                .Name = voice
            }

            Dim input As New SynthesisInput With {
                .Text = sentence
            }

            Dim response = Await _client.SynthesizeSpeechAsync(New SynthesizeSpeechRequest With {
                .AudioConfig = config,
                .Input = input,
                .Voice = voiceParams
            })

            Return response.AudioContent
        End Function
    End Class
End Namespace
Imports System.IO
Imports System.Text
Imports Newtonsoft.Json

Namespace Generators.Buzzwords
    Public Class TechnologyBuzzwords
        Implements IBuzzwords

        Private _adjectives, _nouns, _verbs As List(Of String)

        Public Sub New()
            Dim words As Dictionary(Of String, List(Of String))

            Using fileStream = File.OpenRead("Words/technology.json")
                Using textReader As New StreamReader(fileStream, New UTF8Encoding(False))
                    words = JsonConvert.DeserializeObject(Of Dictionary(Of String, List(Of String)))(textReader.ReadToEnd)
                End Using
            End Using

            _adjectives = words("adjectives")
            _nouns = words("nouns")
            _verbs = words("verbs")
        End Sub

        Public ReadOnly Property Chance As Double Implements IBuzzwords.Chance
            Get
                Return 0.8
            End Get
        End Property

        Public ReadOnly Property TtsVoice As String Implements IBuzzwords.TtsVoice
            Get
                Return "en-IN-Wavenet-C"
            End Get
        End Property

        Public Function GenerateSentence() As String Implements IBuzzwords.GenerateSentence
            Dim verb = _verbs(Random.NextNumber(_verbs.Count))
            Dim adjective = _adjectives(Random.NextNumber(_adjectives.Count))
            Dim noun = _nouns(Random.NextNumber(_nouns.Count))

            Return $"{verb} {adjective} {noun}"
        End Function
    End Class
End Namespace
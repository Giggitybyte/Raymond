Imports System.IO
Imports System.Text
Imports Newtonsoft.Json
Imports Raymond.Services

Namespace Generators
    Public Class TechnologyPhraseGenerator
        Implements IPhraseGenerator

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

        Public ReadOnly Property Chance As Double Implements IPhraseGenerator.Chance
            Get
                Return 0.9
            End Get
        End Property

        Public Function GeneratePhrase(numberGenerator As NumberService) As String Implements IPhraseGenerator.GeneratePhrase
            Dim verb = _verbs(numberGenerator.RandomNumber(_verbs.Count))
            Dim adjective = _adjectives(numberGenerator.RandomNumber(_adjectives.Count))
            Dim noun = _nouns(numberGenerator.RandomNumber(_nouns.Count))

            Return $"{verb} {adjective} {noun}"
        End Function
    End Class
End Namespace
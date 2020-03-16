Imports System.IO
Imports System.Text
Imports Newtonsoft.Json
Imports Raymond.Services

Namespace Generators
    Public Class RarePhraseGenerator
        Implements IPhraseGenerator

        Private _sentences As List(Of String)

        Public Sub New()
            Using fileStream = File.OpenRead("Words/rare.json")
                Using textReader As New StreamReader(fileStream, New UTF8Encoding(False))
                    _sentences = JsonConvert.DeserializeObject(Of List(Of String))(textReader.ReadToEnd)
                End Using
            End Using
        End Sub

        Public ReadOnly Property Chance As Double Implements IPhraseGenerator.Chance
            Get
                Return 0.05
            End Get
        End Property

        Public Function GeneratePhrase(numberGenerator As NumberService) As String Implements IPhraseGenerator.GeneratePhrase
            Return _sentences(numberGenerator.RandomNumber(_sentences.Count))
        End Function
    End Class
End Namespace
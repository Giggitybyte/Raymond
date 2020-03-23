Imports System.IO
Imports System.Text
Imports Newtonsoft.Json

Namespace Generators.Buzzwords
    Public Class NewAgeBuzzwords
        Implements IBuzzwords

        Private _prefixes, _adjectives, _buzzwords, _products, _beginningHalves, _endHalves As List(Of String)

        Public Sub New()
            Dim words As Dictionary(Of String, List(Of String))

            Using fileStream = File.OpenRead("Words/newage.json")
                Using textReader As New StreamReader(fileStream, New UTF8Encoding(False))
                    words = JsonConvert.DeserializeObject(Of Dictionary(Of String, List(Of String)))(textReader.ReadToEnd)
                End Using
            End Using

            _prefixes = words("prefixes")
            _adjectives = words("adjectives")
            _buzzwords = words("buzzwords")
            _products = words("products")

            _beginningHalves = words("beginninghalves")
            _endHalves = words("endhalves")
        End Sub

        Public ReadOnly Property Chance As Double Implements IBuzzwords.Chance
            Get
                Return 0.75
            End Get
        End Property

        Public ReadOnly Property TtsVoice As String Implements IBuzzwords.TtsVoice
            Get
                Return "en-US-Wavenet-A"
            End Get
        End Property

        Public Function GenerateSentence() As String Implements IBuzzwords.GenerateSentence
            If Random.ProbabilityCheck(0.3) Then
                Dim firstHalf = _beginningHalves(Random.NextNumber(_beginningHalves.Count))
                Dim secondHalf = _endHalves(Random.NextNumber(_endHalves.Count))
                Return $"{firstHalf} {secondHalf}"
            End If

            With New StringBuilder
                If Random.ProbabilityCheck(0.4) Then
                    .Append(_prefixes(Random.NextNumber(_prefixes.Count)))
                End If

                .Append($"{_adjectives(Random.NextNumber(_adjectives.Count))} ")

                If Random.ProbabilityCheck(0.25) Then
                    .Append($"{_adjectives(Random.NextNumber(_adjectives.Count))} ")
                End If

                .Append($"{_buzzwords(Random.NextNumber(_buzzwords.Count))} ")
                .Append(_products(Random.NextNumber(_products.Count)))

                Return .ToString
            End With
        End Function
    End Class
End Namespace
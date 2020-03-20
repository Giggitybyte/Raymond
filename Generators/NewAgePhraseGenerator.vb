Imports System.IO
Imports System.Text
Imports Newtonsoft.Json
Imports Raymond.Services

Namespace Generators
    Public Class NewAgePhraseGenerator
        Implements IPhraseGenerator

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

        Public ReadOnly Property Chance As Double Implements IPhraseGenerator.Chance
            Get
                Return 0.75
            End Get
        End Property

        Public Function GeneratePhrase(numberGenerator As NumberService) As String Implements IPhraseGenerator.GeneratePhrase
            If numberGenerator.ProbabilityCheck(0.3) Then
                Dim firstHalf = _beginningHalves(numberGenerator.RandomNumber(_beginningHalves.Count))
                Dim secondHalf = _endHalves(numberGenerator.RandomNumber(_endHalves.Count))
                Return $"{firstHalf} {secondHalf}"
            End If

            With New StringBuilder
                If numberGenerator.ProbabilityCheck(0.4) Then
                    .Append(_prefixes(numberGenerator.RandomNumber(_prefixes.Count)))
                End If

                .Append($"{_adjectives(numberGenerator.RandomNumber(_adjectives.Count))} ")

                If numberGenerator.ProbabilityCheck(0.25) Then
                    .Append($"{_adjectives(numberGenerator.RandomNumber(_adjectives.Count))} ")
                End If

                .Append($"{_buzzwords(numberGenerator.RandomNumber(_buzzwords.Count))} ")
                .Append(_products(numberGenerator.RandomNumber(_products.Count)))

                Return .ToString
            End With
        End Function
    End Class
End Namespace
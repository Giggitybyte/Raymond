Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json

Namespace Generators.Buzzwords
    Public MustInherit Class BuzzwordBase
        Private _constructs As List(Of String)
        Private _words As Dictionary(Of String, List(Of String))

        Public Sub New(name As String)
            Dim words As Dictionary(Of String, List(Of String))

            Using fileStream = File.OpenRead($"Words/{name}.json")
                Using textReader As New StreamReader(fileStream, New UTF8Encoding(False))
                    words = JsonConvert.DeserializeObject(Of Dictionary(Of String, List(Of String)))(textReader.ReadToEnd)
                    _constructs = words("constructs")
                    words.Remove("constructs")
                End Using
            End Using

            _words = words
        End Sub

        ''' <summary>
        ''' How likely this set of buzzwords will be used.
        ''' </summary>
        Public MustOverride ReadOnly Property Chance As Double

        ''' <summary>
        ''' The TTS voice to be used when synthesizing the generated sentence.
        ''' </summary>
        Public MustOverride ReadOnly Property TtsVoice As String

        ''' <summary>
        ''' Returns a randomly chosen sentence of buzzwords.
        ''' </summary>
        Public Function GenerateSentence() As String
            If Not _words?.Any Or Not _constructs?.Any Then
                Throw New InvalidOperationException("This buzzword generator was not properly initialized.")
            End If

            Dim sentence = _constructs(Random.NextNumber(_constructs.Count))

            For Each token In _words.Keys
                Dim tokenRegex As New Regex($"(?<!\w){token}(?!\w)")
                Dim wordPool = _words(token)

                Do
                    Dim match = tokenRegex.Match(sentence)
                    If Not match.Success Then Exit Do

                    Dim word = wordPool(Random.NextNumber(wordPool.Count))
                    sentence = sentence.Remove(match.Index, match.Length)
                    sentence = sentence.Insert(match.Index, word)
                Loop
            Next

            Return sentence
        End Function
    End Class
End Namespace
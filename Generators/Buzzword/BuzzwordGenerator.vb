Imports DSharpPlus.Entities

Namespace Generators.Buzzwords
    Public Class BuzzwordGenerator
        Implements IGenerator

        Private _bag As BuzzwordBag

        Public Sub New()
            _bag = New BuzzwordBag

            Dim baseType = GetType(BuzzwordBase)
            Dim types = baseType.Assembly.GetTypes.Where(Function(t) t IsNot baseType _
                                                                          AndAlso baseType.IsAssignableFrom(t)).ToList
            Dim instances As New List(Of BuzzwordBase)
            types.ForEach(Sub(t) instances.Add(CType(Activator.CreateInstance(t), BuzzwordBase)))
            instances.OrderBy(Function(i) i.Chance).ToList.ForEach(Sub(i) _bag.AddEntry(i))
        End Sub

        Public ReadOnly Property Name As String Implements IGenerator.Name
            Get
                Return "buzzword"
            End Get
        End Property

        Public Function CreateSentence(guild As DiscordGuild) As GeneratorResult Implements IGenerator.CreateSentence
            Dim buzzwords = _bag.GetRandomEntry()
            Return New GeneratorResult With {
                .Sentence = buzzwords.GenerateSentence,
                .TtsVoice = buzzwords.TtsVoice
            }
        End Function

        Protected Class BuzzwordBag ' https://gamedev.stackexchange.com/a/162987
            Private _entries As New List(Of Entry)
            Private _accumulatedWeight As Double

            Public Sub AddEntry(entry As BuzzwordBase)
                _accumulatedWeight += entry.Chance
                _entries.Add(New Entry With {
                    .Buzzwords = entry,
                    .AccumulatedWeight = _accumulatedWeight
                })
            End Sub

            Public Function GetRandomEntry() As BuzzwordBase
                Dim r As Double = Random.NextDouble * _accumulatedWeight

                For Each entry As Entry In _entries
                    If entry.AccumulatedWeight >= r Then
                        Return entry.Buzzwords
                    End If
                Next

                Return Nothing
            End Function

            Private Structure Entry
                Public Property AccumulatedWeight As Double
                Public Property Buzzwords As BuzzwordBase
            End Structure
        End Class
    End Class
End Namespace
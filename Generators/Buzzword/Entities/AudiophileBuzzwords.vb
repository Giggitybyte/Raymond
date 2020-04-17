Namespace Generators.Buzzwords
    Public Class AudiophileBuzzwords
        Inherits BuzzwordBase

        Public Sub New()
            MyBase.New("audiophile")
        End Sub

        Public Overrides ReadOnly Property Chance As Double
            Get
                Return 0.5
            End Get
        End Property

        Public Overrides ReadOnly Property TtsVoice As String
            Get
                Return "en-GB-Wavenet-B"
            End Get
        End Property
    End Class
End Namespace
Namespace Generators.Buzzwords
    Public Class TechnologyBuzzwords
        Inherits BuzzwordBase

        Public Sub New()
            MyBase.New("technology")
        End Sub

        Public Overrides ReadOnly Property Chance As Double
            Get
                Return 0.8
            End Get
        End Property

        Public Overrides ReadOnly Property TtsVoice As String
            Get
                Return "en-IN-Wavenet-C"
            End Get
        End Property
    End Class
End Namespace
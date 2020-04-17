﻿Namespace Generators.Buzzwords
    Public Class NewAgeBuzzwords
        Inherits BuzzwordBase

        Public Sub New()
            MyBase.New("newage")
        End Sub

        Public Overrides ReadOnly Property Chance As Double
            Get
                Return 0.65
            End Get
        End Property

        Public Overrides ReadOnly Property TtsVoice As String
            Get
                Return "en-US-Wavenet-A"
            End Get
        End Property
    End Class
End Namespace
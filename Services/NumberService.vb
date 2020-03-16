Namespace Services
    ''' <summary>
    ''' A thread-safe random number and probablity service.
    ''' </summary>
    Public Class NumberService ' Thanks to nitro#0001 (233648473390448641) for the snippet this is all based off of.
        Private Shared ReadOnly _global As Random = New Random()
        <ThreadStatic> Private Shared _local As Random

        Private Shared Property LocalRandom As Random
            Get
                If _local Is Nothing Then
                    SyncLock _global
                        If _local Is Nothing Then _local = New Random(_global.Next())
                    End SyncLock
                End If

                Return _local
            End Get

            Set(value As Random)
                _local = value
            End Set
        End Property

        ''' <summary>
        ''' Returns a random number that is less than the specified maximum.
        ''' </summary>
        Public Function RandomNumber(max As Integer) As Integer
            Return LocalRandom.Next(max)
        End Function

        ''' <summary>
        ''' Returns a random number within a specified range.
        ''' </summary>
        Public Function RandomNumber(min As Integer, max As Integer) As Integer
            Return LocalRandom.Next(min, max + 1)
        End Function

        ''' <summary>
        ''' Returns a number between 0.0 and 1.0.
        ''' </summary>
        Public Function RandomDouble() As Double
            Return LocalRandom.NextDouble
        End Function

        ''' <summary>
        ''' Returns whether the specified probability was hit.
        ''' </summary>
        Public Function ProbabilityCheck(probability As Double) As Boolean
            If probability >= 1.0 Then Return True
            If probability <= 0.0 Then probability = 0.01

            Dim result = LocalRandom.NextDouble
            Return result <= probability
        End Function
    End Class
End Namespace
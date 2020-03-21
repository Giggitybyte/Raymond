Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.Entities
Imports Raymond.Database

Namespace Commands
    Public Class RaymondModeConverter
        Implements IArgumentConverter(Of RaymondMode)

        Public Async Function ConvertAsync(value As String, ctx As CommandContext) As Task(Of [Optional](Of RaymondMode)) Implements IArgumentConverter(Of RaymondMode).ConvertAsync
            Dim mode As RaymondMode

            If Not [Enum].TryParse(value, True, mode) Then
                Dim modes = String.Join(", ", [Enum].GetNames(GetType(RaymondMode)))
                Await ctx.RespondAsync($"Invalid mode provided.{vbCrLf}Valid modes: {modes}")
                Return [Optional].FromNoValue(Of RaymondMode)
            End If

            Return [Optional].FromValue(mode)
        End Function
    End Class
End Namespace
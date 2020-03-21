Namespace Database
    Public Class GuildData
        ''' <summary>
        ''' Database ID.
        ''' </summary>
        Public Property Id As Integer

        ''' <summary>
        ''' Discord Guild ID.
        ''' </summary>
        Public Property GuildId As ULong

        ''' <summary>
        ''' Collection of Discord voice channel IDs that Raymond is not permitted to join.
        ''' </summary>
        Public Property ProhibitedVoiceIds As New List(Of ULong)

        ''' <summary>
        ''' Collection of Discord text channel IDs that Raymond is not allowed to generate Markov chains from.
        ''' </summary>
        Public Property ProhibitedTextIds As New List(Of ULong)

        ''' <summary>
        ''' Determines what sort of things Raymond will say when he enters a voice channel.
        ''' </summary>
        Public Property Mode As RaymondMode
    End Class
End Namespace
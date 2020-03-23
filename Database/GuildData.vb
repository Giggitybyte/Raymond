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
        ''' Collection of Discord channel IDs that Raymond is not permitted interact with.
        ''' </summary>
        Public Property ProhibitedChannelIds As New List(Of ULong)

        ''' <summary>
        ''' Determines what sort of things Raymond will say when he enters a voice channel.
        ''' </summary>
        Public Property Generator As String
    End Class
End Namespace
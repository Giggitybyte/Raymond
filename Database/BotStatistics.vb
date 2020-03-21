Namespace Database
    Public Class BotStatistics
        ''' <summary>
        ''' Database ID (should always be 1)
        ''' </summary>
        Public Property Id As Integer

        ''' <summary>
        ''' The total number of buzzword 'sentences' generated.
        ''' </summary>
        Public Property BuzzsentencesGenerated As ULong

        ''' <summary>
        ''' The total number of 'sentences' generated from Markov chains.
        ''' </summary>
        Public Property MarkovSentencesGenerated As ULong

        ''' <summary>
        ''' Total number of times in a voice channel.
        ''' </summary>
        ''' <returns></returns>
        Public Property GlobalNumberOfApperances As ULong
    End Class
End Namespace

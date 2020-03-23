Namespace Generators.Buzzwords
    Public Interface IBuzzwords
        ''' <summary>
        ''' How likely this set of buzzwords will be used.
        ''' </summary>
        ReadOnly Property Chance As Double

        ''' <summary>
        ''' The TTS voice to be used when synthesizing the generated sentence.
        ''' </summary>
        ReadOnly Property TtsVoice As String

        ''' <summary>
        ''' Returns a randomly chosen sentence of buzzwords.
        ''' </summary>
        Function GenerateSentence() As String
    End Interface
End Namespace
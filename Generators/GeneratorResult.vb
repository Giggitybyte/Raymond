Namespace Generators
    Public Structure GeneratorResult
        ''' <summary>
        ''' The generated sentence to be spoken.
        ''' </summary>
        Public Property Sentence As String

        ''' <summary>
        ''' The TTS voice to be used when synthesizing the generated sentence.
        ''' </summary>
        Public Property TtsVoice As String
        ' List of voices: https://cloud.google.com/text-to-speech/docs/voices
        ' Only en-US compatible voices should be used; preferably WaveNet. 
        ' Set this to Nothing to use the default voice.
    End Structure
End Namespace
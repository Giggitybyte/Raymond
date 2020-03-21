Namespace Database
    Public Enum RaymondMode
        ''' <summary>
        ''' Generates a set of randomly chosen buzzwords from a topic.
        ''' </summary>
        Buzzword

        ''' <summary>
        ''' Generates a 'sentence' from a Markov chain trained using text all non-blacklisted channels.
        ''' </summary>
        Markov

        ''' <summary>
        ''' Chooses randomly between each mode.
        ''' </summary>
        Random
    End Enum
End Namespace
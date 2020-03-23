Imports DSharpPlus.Entities

Namespace Generators
    Public Interface IGenerator
        ''' <summary>
        ''' The name of this generator.
        ''' </summary>
        ReadOnly Property Name As String

        ''' <summary>
        ''' Returns data to be synthesized into speech.
        ''' </summary>
        Function CreateSentence(guild As DiscordGuild) As GeneratorResult
    End Interface
End Namespace
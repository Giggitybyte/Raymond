Imports Raymond.Services

Namespace Generators
    Public Interface IPhraseGenerator
        ReadOnly Property Chance As Double
        Function GeneratePhrase(numberGenerator As NumberService) As String
    End Interface
End Namespace
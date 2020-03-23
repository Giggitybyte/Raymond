Imports System.Runtime.CompilerServices
Imports LiteDB
Imports Raymond.Database

Namespace Extensions
    Public Module DatabaseExtensions
        ''' <summary>
        ''' Retrieves a <see cref="GuildData"/> object from the database.
        ''' If a guild does not already have an entry, a new one will be created, inserted, then returned.
        ''' </summary>
        <Extension>
        Public Function GetGuildData(ByRef collection As ILiteCollection(Of GuildData), guildId As ULong) As GuildData
            Dim data = collection.FindOne(Function(g) g.GuildId = guildId)

            If data Is Nothing Then
                data = New GuildData With {
                    .GuildId = guildId,
                    .Generator = "buzzword"
                }

                data.Id = collection.Insert(data)
            End If

            Return data
        End Function
    End Module
End Namespace
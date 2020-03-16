Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.CommandsNext.Entities

Namespace Commands
    Public Class HelpFormatter
        Inherits BaseHelpFormatter

        Private Property MsgBuilder As StringBuilder
        Private Property DisplayAllCommands As Boolean

        Public Sub New(ctx As CommandContext)
            MyBase.New(ctx)
            _MsgBuilder = New StringBuilder
            _DisplayAllCommands = True
        End Sub

        Public Overrides Function WithCommand(cmd As Command) As BaseHelpFormatter
            DisplayAllCommands = False

            With MsgBuilder
                ' Header
                .AppendLine(cmd.QualifiedName)
                .AppendLine(New String("-"c, cmd.QualifiedName.Count))

                ' Description
                .AppendLine(If(cmd.Description, "Users should not see this message."))

                ' Aliases
                Dim aliases = GetCommandAliases(cmd)
                If aliases?.Any Then
                    .AppendLine()
                    .AppendLine("aliases::")
                    .AppendLine(String.Join(", ", aliases))
                End If

                ' Subcommands
                If TypeOf cmd Is CommandGroup Then
                    Dim group = DirectCast(cmd, CommandGroup)
                    Dim subCmds = group.Children.Select(Function(c) c.Name)
                    If subCmds.Any Then
                        .AppendLine()
                        .AppendLine("subcommands::")
                        .AppendLine(String.Join(", ", subCmds))
                    End If
                End If

                ' Usage
                Dim usages = GetCommandUsage(cmd)
                If usages IsNot Nothing Then
                    .AppendLine()
                    .AppendLine("usage::")
                    .Append(usages)
                End If

                MsgBuilder.AppendLine.AppendLine("// [arg] = required • (arg) = optional")
            End With

            Return Me
        End Function

        Public Overrides Function WithSubcommands(commands As IEnumerable(Of Command)) As BaseHelpFormatter
            If DisplayAllCommands Then
                Dim cmds = commands.Where(Function(s) s.Name.ToLower <> "help")
                Dim cmdNames = cmds.Select(Function(cmd) $"{cmd.Name}")
                MsgBuilder.Append(String.Join($", ", cmdNames))
            End If

            Return Me
        End Function

        Public Overrides Function Build() As CommandHelpMessage
            If Not DisplayAllCommands Then Return New CommandHelpMessage(Formatter.BlockCode(MsgBuilder.ToString, "asciidoc"))

            With New StringBuilder
                .AppendLine("available commands")
                .AppendLine(New String("-"c, 18))
                .AppendLine(MsgBuilder.ToString)
                .AppendLine()
                .AppendLine("// specify a command to see its usage")

                Return New CommandHelpMessage(Formatter.BlockCode(.ToString, "asciidoc"))
            End With
        End Function

        Private Function GetCommandAliases(cmd As Command) As List(Of String)
            If Not cmd.Aliases.Any Then Return Nothing

            If cmd.Parent IsNot Nothing Then
                Return cmd.Aliases _
                    .Select(Function(a) $"{cmd.Parent.QualifiedName} {a}") _
                    .ToList
            End If

            Return cmd.Aliases.ToList
        End Function

        Private Function GetCommandUsage(cmd As Command) As String
            Dim usageBuilder As New StringBuilder

            For Each overload In cmd.Overloads.OrderBy(Function(x) x.Priority)
                ' Display full name of overload.
                usageBuilder.Append($"{Context.Prefix}{cmd.QualifiedName} ")

                ' Append arguments, if any.
                For Each arg In overload.Arguments
                    usageBuilder.Append(If(arg.IsOptional, $"({arg.Name}) ", $"[{arg.Name}] "))
                Next

                usageBuilder.AppendLine()
            Next

            Return usageBuilder.ToString
        End Function
    End Class
End Namespace
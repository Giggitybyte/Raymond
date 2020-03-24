﻿Imports System.IO
Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.VoiceNext
Imports LiteDB
Imports Raymond.Database
Imports Raymond.Extensions
Imports Raymond.Generators

Namespace Services
    ''' <summary>
    ''' Synthesizes 'sentences' and other text to be sent Discord voice channels.
    ''' </summary>
    Public Class SentenceService
        Private _database As LiteDatabase
        Private _discord As DiscordClient
        Private _generators As List(Of IGenerator)
        Private _logger As LogService
        Private _timers As Dictionary(Of ULong, Timer)
        Private _tts As GoogleTtsService

        Public Sub New(db As LiteDatabase, discord As DiscordClient, tts As GoogleTtsService, logger As LogService, generators As IEnumerable(Of IGenerator))
            _database = db
            _discord = discord
            _generators = generators.ToList
            _logger = logger
            _timers = New Dictionary(Of ULong, Timer)
            _tts = tts

            AddHandler _discord.GuildAvailable, AddressOf GuildAvailableHandler
            AddHandler _discord.GuildDeleted, AddressOf GuildRemovedHandler
        End Sub

        ''' <summary>
        ''' Sets up timers for guilds as they come available. These initial timers are set for 5 seconds.
        ''' </summary>
        Private Async Function GuildAvailableHandler(e As GuildCreateEventArgs) As Task
            If _timers.ContainsKey(e.Guild.Id) Then Return

            _timers.Add(e.Guild.Id, New Timer(AddressOf GuildTrigger, e.Guild.Id, 5000, -1)) ' In prod, change to 2 days.
            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Added a new timer for guild {e.Guild.Id}")
        End Function

        ''' <summary>
        ''' Removes timers for guilds that have kicked us. Makes no sense in keeping those around.
        ''' </summary>
        Private Async Function GuildRemovedHandler(e As GuildDeleteEventArgs) As Task
            If _timers.ContainsKey(e.Guild.Id) Then
                Dim timer As Timer
                _timers.Remove(e.Guild.Id, timer)
                Await timer.DisposeAsync()
            End If
        End Function

        ''' <summary>
        ''' The callback method for all timers. This triggers Raymond's appearances.
        ''' </summary>
        Private Async Sub GuildTrigger(guildId As Object)
            ' Get guild.
            Dim guild = _discord.Guilds(CULng(guildId))

            ' Send sentence to the most populated, non-blacklisted voice channel.
            Dim channels = (Await guild.GetChannelsAsync).ToList
            If guild.AfkChannel IsNot Nothing Then channels.Remove(guild.AfkChannel)

            Dim blacklist = _database.GetCollection(Of GuildData).GetGuildData(guild.Id).ProhibitedChannelIds
            Dim validChannels = channels.Where(Function(c) c.Type = ChannelType.Voice _
                                                           AndAlso Not blacklist.Contains(c.Id) _
                                                           AndAlso c.Users.Any).ToList
            If validChannels.Any Then
                Dim channel = validChannels.OrderByDescending(Function(c) c.Users.Count).First
                Dim success As Boolean

                Do ' vnext occasionally throws, so we'll retry until it doesn't fucking throw.
                    success = Await SendSentenceAsync(channel)
                Loop Until success
            Else
                Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Skipping guild {guild.Id}.")
            End If

            ' Set timer to fire again anywhere between 2 and 5 days.
            Dim time = TimeSpan.FromMilliseconds(Random.NextNumber(172800000, 432000000))
            _timers(guild.Id).Change(time, Timeout.InfiniteTimeSpan)

            Await _logger.PrintAsync(LogLevel.Info, "Sentence Service", $"Next appearance for guild {guild.Id}: {Date.Now.Add(time).ToString}")
        End Sub

        ''' <summary>
        ''' Selects a generator based on user preference then sends the guild a sentence.
        ''' Returns <see langword="False"/> if something went wrong as an indication to try again.
        ''' </summary>
        Private Async Function SendSentenceAsync(channel As DiscordChannel) As Task(Of Boolean)
            ' Get generator
            Dim collection = _database.GetCollection(Of GuildData)
            Dim data = collection.GetGuildData(channel.Guild.Id)
            Dim generator As IGenerator

            If data.Generator = "random" Then
                generator = _generators(Random.NextNumber(_generators.Count))
            Else
                generator = _generators.First(Function(g) g.Name = data.Generator)
            End If

            ' Send sentence.
            Dim result = generator.CreateSentence(channel.Guild)
            Return Await SendSentenceAsync(channel, result.Sentence, result.TtsVoice)
        End Function

        ''' <summary>
        ''' Joins the specified voice channel and says the provided text.
        ''' Returns <see langword="False"/> if something went wrong as an indication to try again.
        ''' </summary>
        Public Async Function SendSentenceAsync(channel As DiscordChannel, text As String, voice As String) As Task(Of Boolean)
            Dim guild = channel.Guild
            Dim speech = Await _tts.SynthesizeAsync(text, voice)
            Dim voiceChn = Await channel.ConnectAsync()
            Dim transmit = voiceChn.GetTransmitStream()

            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Speaking in guild {guild.Id}")

            Try
                Await speech.CopyToAsync(transmit)
                Await transmit.FlushAsync()
            Catch ex As Exception
                _logger.Print(LogLevel.Warning, "Sentence Service", $"Speaking failed in guild {guild.Id}.", ex)
                speech.Dispose()
                Return False
            End Try

            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Waiting for speaking to finish... ({guild.Id})")
            Await voiceChn.WaitForPlaybackFinishAsync

            Await speech.DisposeAsync
            Await transmit.DisposeAsync
            voiceChn.Disconnect()

            Await _logger.PrintAsync(LogLevel.Debug, "Sentence Service", $"Finished speaking in guild {guild.Id}.")
            Return True
        End Function
    End Class
End Namespace
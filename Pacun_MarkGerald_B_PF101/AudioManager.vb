Imports System.Windows.Media
Imports System.Linq

''' <summary>
''' Shared audio playback for the game. Background music plays through one looping
''' MediaPlayer; each sound effect gets its own short-lived MediaPlayer so effects
''' can overlap. Volumes come from GameSettings and apply immediately.
''' </summary>
Public Module AudioManager

    Private musicPlayer As MediaPlayer = Nothing
    Private currentMusicPath As String = ""
    Private ReadOnly activeSfxPlayers As New List(Of MediaPlayer)

    Public ReadOnly AssetsRoot As String = IO.Path.Combine(Application.StartupPath, "Assets")

    ' ===== Background / ambient music =====
    Public Sub PlayMusic(relativePath As String, loopPlayback As Boolean)
        Dim fullPath As String = IO.Path.Combine(AssetsRoot, relativePath)

        If Not IO.File.Exists(fullPath) Then
            Debug.WriteLine("AudioManager: music file not found: " & fullPath)
            Return
        End If

        ' Already playing this exact track -- don't restart it.
        If musicPlayer IsNot Nothing AndAlso currentMusicPath = fullPath Then
            Return
        End If

        StopMusic()

        musicPlayer = New MediaPlayer()
        currentMusicPath = fullPath
        musicPlayer.Volume = GameSettings.GetInstance().GetMusicVolumeAsDecimal()

        If loopPlayback Then
            AddHandler musicPlayer.MediaEnded, AddressOf MusicPlayer_MediaEnded
        End If

        Try
            musicPlayer.Open(New Uri(fullPath))
            musicPlayer.Play()
        Catch ex As Exception
            Debug.WriteLine("AudioManager: failed to play music - " & ex.Message)
        End Try
    End Sub

    Private Sub MusicPlayer_MediaEnded(sender As Object, e As EventArgs)
        Try
            musicPlayer.Position = TimeSpan.Zero
            musicPlayer.Play()
        Catch
        End Try
    End Sub

    Public Sub StopMusic()
        If musicPlayer IsNot Nothing Then
            Try
                RemoveHandler musicPlayer.MediaEnded, AddressOf MusicPlayer_MediaEnded
                musicPlayer.Stop()
                musicPlayer.Close()
            Catch
            End Try
            musicPlayer = Nothing
            currentMusicPath = ""
        End If
    End Sub

    ' Call this after changing GameSettings.MusicVolume so currently-playing music updates live.
    Public Sub ApplyMusicVolume()
        If musicPlayer IsNot Nothing Then
            musicPlayer.Volume = GameSettings.GetInstance().GetMusicVolumeAsDecimal()
        End If
    End Sub

    ' ===== Sound effects (can overlap) =====
    Public Sub PlaySfx(relativePath As String)
        Dim fullPath As String = IO.Path.Combine(AssetsRoot, relativePath)

        If Not IO.File.Exists(fullPath) Then
            Debug.WriteLine("AudioManager: SFX file not found: " & fullPath)
            Return
        End If

        Dim volume As Double = GameSettings.GetInstance().GetSfxVolumeAsDecimal()
        If volume <= 0.0 Then Return ' muted -- don't even spin up a player

        Dim player As New MediaPlayer()
        player.Volume = volume

        AddHandler player.MediaEnded, Sub(s, e) CleanupSfxPlayer(player)
        AddHandler player.MediaFailed, Sub(s, e) CleanupSfxPlayer(player)

        activeSfxPlayers.Add(player)

        Try
            player.Open(New Uri(fullPath))
            player.Play()
        Catch ex As Exception
            Debug.WriteLine("AudioManager: failed to play SFX - " & ex.Message)
            CleanupSfxPlayer(player)
        End Try
    End Sub

    Private Sub CleanupSfxPlayer(player As MediaPlayer)
        Try
            player.Stop()
            player.Close()
        Catch
        End Try
        activeSfxPlayers.Remove(player)
    End Sub

    ' Call once when the whole game flow (Level1MenuForm chain) is closing.
    Public Sub ShutdownAll()
        StopMusic()
        For Each p In activeSfxPlayers.ToList()
            CleanupSfxPlayer(p)
        Next
    End Sub

End Module
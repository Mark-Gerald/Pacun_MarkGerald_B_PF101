Public Class GameSettings
    ' Singleton instance
    Private Shared _instance As GameSettings

    ' Volume values (0-100)
    Private _musicVolume As Integer = 100
    Private _sfxVolume As Integer = 100

    Public Shared Function GetInstance() As GameSettings
        If _instance Is Nothing Then
            _instance = New GameSettings()
        End If
        Return _instance
    End Function

    Public Property MusicVolume As Integer
        Get
            Return _musicVolume
        End Get
        Set(value As Integer)
            ' Clamp to 0-100
            _musicVolume = Math.Max(0, Math.Min(100, value))
        End Set
    End Property

    Public Property SfxVolume As Integer
        Get
            Return _sfxVolume
        End Get
        Set(value As Integer)
            ' Clamp to 0-100
            _sfxVolume = Math.Max(0, Math.Min(100, value))
        End Set
    End Property

    ' Convert 0-100 to 0.0-1.0 for audio playback (if needed)
    Public Function GetMusicVolumeAsDecimal() As Single
        Return _musicVolume / 100.0F
    End Function

    Public Function GetSfxVolumeAsDecimal() As Single
        Return _sfxVolume / 100.0F
    End Function
End Class
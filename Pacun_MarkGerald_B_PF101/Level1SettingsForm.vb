Public Class Level1SettingsForm
    Inherits Form

    Private musicSlider As TrackBar
    Private sfxSlider As TrackBar
    Private musicLabel As Label
    Private sfxLabel As Label

    Public Sub New()
        Me.Text = "Level 1 Settings"
        Me.Size = New Size(520, 420)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.FromArgb(30, 30, 36)

        Dim titleLabel As New Label() With {
            .Text = "SETTINGS",
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Top,
            .Height = 70,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(titleLabel)

        Dim mainPanel As New Panel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(40, 20, 40, 20),
            .BackColor = Color.FromArgb(245, 245, 245)
        }
        Me.Controls.Add(mainPanel)
        mainPanel.BringToFront()
        titleLabel.SendToBack()

        Dim musicCaption As New Label() With {
            .Text = "BACKGROUND MUSIC VOLUME",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 40, 40),
            .AutoSize = True,
            .Location = New Point(0, 10)
        }
        mainPanel.Controls.Add(musicCaption)

        musicSlider = New TrackBar() With {
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().MusicVolume,
            .Location = New Point(0, 40),
            .Width = 400,
            .TickFrequency = 10
        }
        mainPanel.Controls.Add(musicSlider)

        musicLabel = New Label() With {
            .Text = GameSettings.GetInstance().MusicVolume & "%",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .AutoSize = True,
            .Location = New Point(0, 90)
        }
        mainPanel.Controls.Add(musicLabel)

        AddHandler musicSlider.ValueChanged, Sub(s, e)
                                                 GameSettings.GetInstance().MusicVolume = musicSlider.Value
                                                 musicLabel.Text = musicSlider.Value & "%"
                                                 AudioManager.ApplyMusicVolume()
                                             End Sub

        Dim sfxCaption As New Label() With {
            .Text = "SOUND EFFECTS VOLUME",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 40, 40),
            .AutoSize = True,
            .Location = New Point(0, 150)
        }
        mainPanel.Controls.Add(sfxCaption)

        sfxSlider = New TrackBar() With {
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().SfxVolume,
            .Location = New Point(0, 180),
            .Width = 400,
            .TickFrequency = 10
        }
        mainPanel.Controls.Add(sfxSlider)

        sfxLabel = New Label() With {
            .Text = GameSettings.GetInstance().SfxVolume & "%",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .AutoSize = True,
            .Location = New Point(0, 230)
        }
        mainPanel.Controls.Add(sfxLabel)

        AddHandler sfxSlider.ValueChanged, Sub(s, e)
                                               GameSettings.GetInstance().SfxVolume = sfxSlider.Value
                                               sfxLabel.Text = sfxSlider.Value & "%"
                                           End Sub

        Dim backButton As New Button() With {
            .Text = "BACK TO MENU",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Size = New Size(180, 42),
            .Location = New Point(110, 290)
        }
        backButton.FlatAppearance.BorderSize = 0
        AddHandler backButton.Click, Sub(s, e)
                                         AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
                                         Me.DialogResult = DialogResult.OK
                                         Me.Close()
                                     End Sub
        mainPanel.Controls.Add(backButton)
    End Sub

End Class
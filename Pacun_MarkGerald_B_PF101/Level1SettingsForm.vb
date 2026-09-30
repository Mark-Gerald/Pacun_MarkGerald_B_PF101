Public Class Level1SettingsForm
    Inherits Form

    Private musicSlider As TrackBar
    Private sfxSlider As TrackBar
    Private musicLabel As Label
    Private sfxLabel As Label
    Private backButton As Button

    Public Sub New()
        Me.Text = "Level 1 Settings"
        Me.Size = New Size(500, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(245, 245, 245)

        ' Title
        Dim titleLabel As New Label() With {
            .Text = "SETTINGS",
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .Dock = DockStyle.Top,
            .Height = 60,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(titleLabel)

        ' Main panel for sliders
        Dim mainPanel As New Panel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(40, 20, 40, 60),
            .BackColor = Color.White
        }
        Me.Controls.Add(mainPanel)

        ' Music Volume Section
        Dim musicCaptionLabel As New Label() With {
            .Text = "Background Music Volume",
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(0, 0)
        }
        mainPanel.Controls.Add(musicCaptionLabel)

        musicSlider = New TrackBar() With {
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().MusicVolume,
            .Location = New Point(0, 30),
            .Width = mainPanel.ClientSize.Width - 40,
            .Height = 40
        }
        mainPanel.Controls.Add(musicSlider)

        musicLabel = New Label() With {
            .Text = GameSettings.GetInstance().MusicVolume & "%",
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(0, 75)
        }
        mainPanel.Controls.Add(musicLabel)

        AddHandler musicSlider.ValueChanged, Sub(s, e)
                                                 GameSettings.GetInstance().MusicVolume = musicSlider.Value
                                                 musicLabel.Text = musicSlider.Value & "%"
                                             End Sub

        ' SFX Volume Section
        Dim sfxCaptionLabel As New Label() With {
            .Text = "Sound Effects Volume",
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(0, 130)
        }
        mainPanel.Controls.Add(sfxCaptionLabel)

        sfxSlider = New TrackBar() With {
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().SfxVolume,
            .Location = New Point(0, 160),
            .Width = mainPanel.ClientSize.Width - 40,
            .Height = 40
        }
        mainPanel.Controls.Add(sfxSlider)

        sfxLabel = New Label() With {
            .Text = GameSettings.GetInstance().SfxVolume & "%",
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(0, 205)
        }
        mainPanel.Controls.Add(sfxLabel)

        AddHandler sfxSlider.ValueChanged, Sub(s, e)
                                               GameSettings.GetInstance().SfxVolume = sfxSlider.Value
                                               sfxLabel.Text = sfxSlider.Value & "%"
                                           End Sub

        ' Back Button (in bottom panel)
        Dim bottomPanel As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 50,
            .BackColor = Color.FromArgb(230, 230, 230)
        }
        Me.Controls.Add(bottomPanel)

        backButton = New Button() With {
            .Text = "Back to Menu",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Size = New Size(150, 40),
            .Location = New Point((bottomPanel.Width - 150) \ 2, 5)
        }
        backButton.FlatAppearance.BorderSize = 0
        AddHandler backButton.Click, AddressOf BackButton_Click
        bottomPanel.Controls.Add(backButton)
    End Sub

    Private Sub BackButton_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
End Class
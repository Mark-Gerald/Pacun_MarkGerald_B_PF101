Public Class Level1MenuForm
    Inherits Form

    Private startButton As Button
    Private settingsButton As Button
    Private nextGameButton As Button
    Private exitButton As Button
    Private menuPanel As Panel
    Private settingsPanel As Panel

    Public Sub New()
        Me.Text = "Level 1 — River Crossing"
        Me.Size = New Size(800, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(135, 206, 235)

        ' Title
        Dim titleLabel As New Label() With {
            .Text = "LEVEL 1" & vbCrLf & "River Crossing Puzzle",
            .Font = New Font("Segoe UI", 28.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Location = New Point(0, 0),
            .Width = Me.ClientSize.Width,
            .Height = 120
        }
        Me.Controls.Add(titleLabel)

        ' Menu Panel
        menuPanel = New Panel() With {
            .BackColor = Me.BackColor,
            .Location = New Point(0, 120),
            .Width = Me.ClientSize.Width,
            .Height = Me.ClientSize.Height - 120
        }
        Me.Controls.Add(menuPanel)

        ' Settings Panel (hidden initially)
        settingsPanel = New Panel() With {
            .BackColor = Me.BackColor,
            .Location = New Point(0, 120),
            .Width = Me.ClientSize.Width,
            .Height = Me.ClientSize.Height - 120,
            .Visible = False
        }
        Me.Controls.Add(settingsPanel)

        ' Build menu buttons
        BuildMenuButtons()

        ' Build settings controls
        BuildSettingsPanel()
    End Sub

    Private Sub BuildMenuButtons()
        Dim buttonWidth As Integer = 200
        Dim buttonHeight As Integer = 50
        Dim centerX As Integer = (Me.ClientSize.Width - buttonWidth) \ 2

        ' START button
        startButton = New Button() With {
            .Text = "START",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(34, 177, 76),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(centerX, 20),
            .Size = New Size(buttonWidth, buttonHeight)
        }
        startButton.FlatAppearance.BorderSize = 0
        AddHandler startButton.Click, AddressOf StartButton_Click
        menuPanel.Controls.Add(startButton)

        ' SETTINGS button
        settingsButton = New Button() With {
            .Text = "SETTINGS",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(52, 152, 219),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(centerX, 80),
            .Size = New Size(buttonWidth, buttonHeight)
        }
        settingsButton.FlatAppearance.BorderSize = 0
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        menuPanel.Controls.Add(settingsButton)

        ' NEXT GAME button
        nextGameButton = New Button() With {
            .Text = "NEXT GAME",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(155, 89, 182),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(centerX, 140),
            .Size = New Size(buttonWidth, buttonHeight)
        }
        nextGameButton.FlatAppearance.BorderSize = 0
        AddHandler nextGameButton.Click, AddressOf NextGameButton_Click
        menuPanel.Controls.Add(nextGameButton)

        ' EXIT button
        exitButton = New Button() With {
            .Text = "EXIT",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(192, 57, 43),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(centerX, 200),
            .Size = New Size(buttonWidth, buttonHeight)
        }
        exitButton.FlatAppearance.BorderSize = 0
        AddHandler exitButton.Click, AddressOf ExitButton_Click
        menuPanel.Controls.Add(exitButton)
    End Sub

    Private Sub BuildSettingsPanel()
        ' Music Volume Label
        Dim musicLabel As New Label() With {
            .Text = "Background Music Volume: 100%",
            .Font = New Font("Segoe UI", 11.0F),
            .ForeColor = Color.Black,
            .Location = New Point(40, 30),
            .AutoSize = True
        }
        settingsPanel.Controls.Add(musicLabel)

        ' Music Slider
        Dim musicSlider As New TrackBar() With {
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().MusicVolume,
            .Location = New Point(40, 60),
            .Width = 300
        }
        settingsPanel.Controls.Add(musicSlider)

        AddHandler musicSlider.ValueChanged, Sub(s, e)
                                                 GameSettings.GetInstance().MusicVolume = musicSlider.Value
                                                 musicLabel.Text = "Background Music Volume: " & musicSlider.Value & "%"
                                             End Sub

        ' SFX Volume Label
        Dim sfxLabel As New Label() With {
            .Text = "Sound Effects Volume: 100%",
            .Font = New Font("Segoe UI", 11.0F),
            .ForeColor = Color.Black,
            .Location = New Point(40, 120),
            .AutoSize = True
        }
        settingsPanel.Controls.Add(sfxLabel)

        ' SFX Slider
        Dim sfxSlider As New TrackBar() With {
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().SfxVolume,
            .Location = New Point(40, 150),
            .Width = 300
        }
        settingsPanel.Controls.Add(sfxSlider)

        AddHandler sfxSlider.ValueChanged, Sub(s, e)
                                               GameSettings.GetInstance().SfxVolume = sfxSlider.Value
                                               sfxLabel.Text = "Sound Effects Volume: " & sfxSlider.Value & "%"
                                           End Sub

        ' Back Button
        Dim backButton As New Button() With {
            .Text = "Back to Menu",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(300, 220),
            .Size = New Size(150, 40)
        }
        backButton.FlatAppearance.BorderSize = 0
        AddHandler backButton.Click, AddressOf BackButton_Click
        settingsPanel.Controls.Add(backButton)
    End Sub

    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        Try
            Dim gameplayForm As New Level1GameplayForm()
            gameplayForm.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        menuPanel.Visible = False
        settingsPanel.Visible = True
    End Sub

    Private Sub BackButton_Click(sender As Object, e As EventArgs)
        settingsPanel.Visible = False
        menuPanel.Visible = True
    End Sub

    Private Sub NextGameButton_Click(sender As Object, e As EventArgs)
        Try
            Dim level2Form As New Level2PlaceholderForm()
            level2Form.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub
End Class
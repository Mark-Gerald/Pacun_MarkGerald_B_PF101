Public Class Level1MenuForm
    Inherits Form

    Private startButton As Button
    Private settingsButton As Button
    Private nextGameButton As Button
    Private exitButton As Button

    Public Sub New()
        Me.Text = "Level 1 — River Crossing"
        Me.Size = New Size(800, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(135, 206, 235) ' Light sky blue as placeholder

        ' Simple menu title
        Dim titleLabel As New Label() With {
            .Text = "LEVEL 1" & vbCrLf & "River Crossing Puzzle",
            .Font = New Font("Segoe UI", 28.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Top,
            .Height = 120
        }
        Me.Controls.Add(titleLabel)

        ' Center panel for buttons
        Dim centerPanel As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Me.BackColor
        }
        Me.Controls.Add(centerPanel)

        ' Button size and layout
        Dim buttonWidth As Integer = 200
        Dim buttonHeight As Integer = 50
        Dim centerX As Integer = (centerPanel.Width - buttonWidth) \ 2

        ' START button
        startButton = New Button() With {
            .Text = "START",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(34, 177, 76),
            .ForeColor = Color.White,
            .Size = New Size(buttonWidth, buttonHeight),
            .Location = New Point(centerX, 80)
        }
        startButton.FlatAppearance.BorderSize = 0
        AddHandler startButton.Click, AddressOf StartButton_Click
        centerPanel.Controls.Add(startButton)

        ' SETTINGS button
        settingsButton = New Button() With {
            .Text = "SETTINGS",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(52, 152, 219),
            .ForeColor = Color.White,
            .Size = New Size(buttonWidth, buttonHeight),
            .Location = New Point(centerX, 150)
        }
        settingsButton.FlatAppearance.BorderSize = 0
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        centerPanel.Controls.Add(settingsButton)

        ' NEXT GAME button
        nextGameButton = New Button() With {
            .Text = "NEXT GAME",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(155, 89, 182),
            .ForeColor = Color.White,
            .Size = New Size(buttonWidth, buttonHeight),
            .Location = New Point(centerX, 220)
        }
        nextGameButton.FlatAppearance.BorderSize = 0
        AddHandler nextGameButton.Click, AddressOf NextGameButton_Click
        centerPanel.Controls.Add(nextGameButton)

        ' EXIT button
        exitButton = New Button() With {
            .Text = "EXIT",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(192, 57, 43),
            .ForeColor = Color.White,
            .Size = New Size(buttonWidth, buttonHeight),
            .Location = New Point(centerX, 290)
        }
        exitButton.FlatAppearance.BorderSize = 0
        AddHandler exitButton.Click, AddressOf ExitButton_Click
        centerPanel.Controls.Add(exitButton)
    End Sub

    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        ' TODO: Play button click sound (will implement in Stage 6)
        ' TODO: Open Level1GameplayForm and hide this menu
        MessageBox.Show("Gameplay will open here.", "Start Game", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        ' TODO: Play button click sound
        Dim settingsForm As New Level1SettingsForm()
        settingsForm.ShowDialog(Me)
    End Sub

    Private Sub NextGameButton_Click(sender As Object, e As EventArgs)
        ' TODO: Play button click sound
        Dim level2Form As New Level2PlaceholderForm()
        level2Form.ShowDialog(Me)
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        ' TODO: Play button click sound
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
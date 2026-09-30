Public Class Level1MenuForm
    Inherits Form

    Private startButton As Button
    Private settingsButton As Button
    Private nextGameButton As Button
    Private exitButton As Button

    Public Sub New()
        Me.Text = "Level 1 — River Crossing"
        Me.Size = New Size(800, 600)
        Me.MinimumSize = New Size(600, 500)
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

        ' START button
        startButton = New Button() With {
            .Text = "START",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(34, 177, 76),
            .ForeColor = Color.White,
            .Size = New Size(buttonWidth, buttonHeight)
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
            .Size = New Size(buttonWidth, buttonHeight)
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
            .Size = New Size(buttonWidth, buttonHeight)
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
            .Size = New Size(buttonWidth, buttonHeight)
        }
        exitButton.FlatAppearance.BorderSize = 0
        AddHandler exitButton.Click, AddressOf ExitButton_Click
        centerPanel.Controls.Add(exitButton)

        ' Position buttons dynamically on Resize
        AddHandler centerPanel.Resize, Sub(s, e)
                                           Dim centerX As Integer = (centerPanel.Width - buttonWidth) \ 2
                                           startButton.Location = New Point(centerX, 60)
                                           settingsButton.Location = New Point(centerX, 130)
                                           nextGameButton.Location = New Point(centerX, 200)
                                           exitButton.Location = New Point(centerX, 270)
                                       End Sub
    End Sub

    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        Try
            Dim gameplayForm As New Level1GameplayForm()
            gameplayForm.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show("Gameplay form could not be opened." & vbCrLf & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        Dim settingsForm As New Level1SettingsForm()
        settingsForm.ShowDialog(Me)
    End Sub

    Private Sub NextGameButton_Click(sender As Object, e As EventArgs)
        Dim level2Form As New Level2PlaceholderForm()
        level2Form.ShowDialog(Me)
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
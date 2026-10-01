Public Class Level3PlaceholderForm
    Inherits Form

    Public Sub New()
        Me.Text = "Level 3 " & ChrW(8212) & " Coming Soon"
        Me.Size = New Size(800, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(245, 245, 245)

        Dim messageLabel As New Label() With {
            .Text = "Level 3 is not yet implemented." & vbCrLf & vbCrLf & "This is a placeholder.",
            .Font = New Font("Segoe UI", 14.0F),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        Me.Controls.Add(messageLabel)

        Dim closeButton As New Button() With {
            .Text = "Back",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Size = New Size(100, 40),
            .Dock = DockStyle.Bottom
        }
        closeButton.FlatAppearance.BorderSize = 0
        AddHandler closeButton.Click, Sub(s, e)
                                          Me.DialogResult = DialogResult.OK
                                          Me.Close()
                                      End Sub
        Me.Controls.Add(closeButton)
    End Sub
End Class
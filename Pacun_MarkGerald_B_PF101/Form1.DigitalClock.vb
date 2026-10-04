Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>
''' Digital Clock for the Welcome page (PF101 Form1).
'''   * A "Digital Clock" button at the far right of the navigation bar.
'''   * Clicking it drops down a live clock showing Philippine Standard Time (UTC+8, no daylight saving).
'''   * It uses the Welcome page colours: the dark-to-indigo header gradient, the faint dot pattern,
'''     white text and the purple (138, 100, 255) accent used by the lesson badges.
'''
''' This is a PARTIAL class, so it lives in its own file. Form1.vb only needs ONE extra line:
'''     BuildDigitalClockButton()
''' placed right after   navPanel.Controls.Add(navLeftFlow)   in Form1_Load.
''' </summary>
Partial Public Class Form1

    Private clockHost As Panel
    Private clockButton As Button
    Private clockDropdownPanel As Panel
    Private clockDropdownOpen As Boolean = False
    Private clockTimer As System.Windows.Forms.Timer
    Private clockOpenScrollY As Integer = 0

    Private Const ClockDropdownWidth As Integer = 320
    Private Const ClockDropdownHeight As Integer = 236
    Private Const PhilippineUtcOffsetHours As Double = 8.0     ' the Philippines never uses daylight saving time

    ' ===================== Setup =====================

    Private Sub BuildDigitalClockButton()
        ' Host panel gives the same 6 px spacing the other nav buttons have, docked to the right edge.
        clockHost = New Panel() With {
            .Dock = DockStyle.Right,
            .Width = 160,
            .Padding = New Padding(6),
            .BackColor = Color.Transparent
        }

        clockButton = New Button() With {
            .Text = "Digital Clock  " & ChrW(9662),
            .Dock = DockStyle.Fill,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.0F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Cursor = Cursors.Hand
        }
        clockButton.FlatAppearance.BorderSize = 0
        AddHandler clockButton.Click, AddressOf ClockButton_Click
        clockHost.Controls.Add(clockButton)
        navPanel.Controls.Add(clockHost)

        ' The dropdown itself (a plain panel painted by ClockDropdown_Paint), added to the form like the Lessons dropdown.
        clockDropdownPanel = New Panel() With {
            .Size = New Size(ClockDropdownWidth, ClockDropdownHeight),
            .BackColor = Color.FromArgb(26, 26, 28),
            .Visible = False
        }
        AddHandler clockDropdownPanel.Paint, AddressOf ClockDropdown_Paint
        EnableDoubleBuffering(clockDropdownPanel)
        Me.Controls.Add(clockDropdownPanel)

        ' One timer, running only while the dropdown is open. It repaints the clock and closes the dropdown
        ' if the page is scrolled or the Lessons dropdown is opened.
        clockTimer = New System.Windows.Forms.Timer() With {.Interval = 100}
        AddHandler clockTimer.Tick, AddressOf ClockTimer_Tick

        AddHandler Me.Resize, Sub(s, ev) If clockDropdownOpen Then PositionClockDropdown()
        AddHandler Me.FormClosed, Sub(s, ev)
                                      clockTimer.Stop()
                                      clockTimer.Dispose()
                                  End Sub
    End Sub

    ' ===================== Open / close =====================

    Private Sub ClockButton_Click(sender As Object, e As EventArgs)
        If clockDropdownOpen Then
            CloseClockDropdown()
        Else
            OpenClockDropdown()
        End If
    End Sub

    Private Sub OpenClockDropdown()
        If lessonsDropdownOpen Then CloseLessonsDropdown()
        PositionClockDropdown()
        clockOpenScrollY = scrollContainer.AutoScrollPosition.Y
        clockDropdownPanel.BringToFront()
        clockDropdownPanel.Visible = True
        clockDropdownOpen = True
        clockButton.Text = "Digital Clock  " & ChrW(9652)
        clockTimer.Start()
        clockDropdownPanel.Invalidate()
    End Sub

    Private Sub CloseClockDropdown()
        clockTimer.Stop()
        clockDropdownPanel.Visible = False
        clockDropdownOpen = False
        clockButton.Text = "Digital Clock  " & ChrW(9662)
    End Sub

    Private Sub ClockTimer_Tick(sender As Object, e As EventArgs)
        If Not clockDropdownOpen Then Return
        If lessonsDropdownOpen OrElse scrollContainer.AutoScrollPosition.Y <> clockOpenScrollY Then
            CloseClockDropdown()
            Return
        End If
        clockDropdownPanel.Invalidate()
    End Sub

    ''' <summary>Opens right under the button, right edges aligned, and always stays inside the window.</summary>
    Private Sub PositionClockDropdown()
        If clockDropdownPanel Is Nothing OrElse clockButton Is Nothing Then Return
        Dim screenPt As Point = clockButton.PointToScreen(New Point(clockButton.Width, clockButton.Height + 4))
        Dim clientPt As Point = Me.PointToClient(screenPt)
        Dim x As Integer = clientPt.X - clockDropdownPanel.Width
        x = Math.Max(4, Math.Min(x, Me.ClientSize.Width - clockDropdownPanel.Width - 4))
        clockDropdownPanel.Location = New Point(x, clientPt.Y)
    End Sub

    ' ===================== Time =====================

    ''' <summary>Philippine Standard Time = UTC + 8. Computed from UTC so it is correct on any PC time zone.</summary>
    Private Shared Function GetPhilippineTime() As DateTime
        Return DateTime.UtcNow.AddHours(PhilippineUtcOffsetHours)
    End Function

    ' ===================== Painting =====================

    Private Shared Function RoundedPath(r As RectangleF, radius As Single) As GraphicsPath
        Dim d As Single = radius * 2.0F
        Dim path As New GraphicsPath()
        path.AddArc(r.Left, r.Top, d, d, 180, 90)
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90)
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Private Sub ClockDropdown_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim r As Rectangle = clockDropdownPanel.ClientRectangle
        If r.Width <= 0 OrElse r.Height <= 0 Then Return

        Dim ph As DateTime = GetPhilippineTime()

        ' ---- background: the same gradient + dot pattern as the Welcome header ----
        Using bg As New LinearGradientBrush(r, Color.FromArgb(26, 26, 28), Color.FromArgb(64, 43, 220), LinearGradientMode.Vertical)
            g.FillRectangle(bg, r)
        End Using
        Using dotBrush As New SolidBrush(Color.FromArgb(18, 255, 255, 255))
            For x As Integer = 0 To r.Width Step 24
                For y As Integer = 0 To r.Height Step 24
                    g.FillEllipse(dotBrush, x, y, 2, 2)
                Next
            Next
        End Using

        Using centerFmt As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

            ' ---- caption ----
            Using f As New Font("Segoe UI", 8.5F, FontStyle.Bold)
                Using b As New SolidBrush(Color.FromArgb(200, 190, 255))
                    g.DrawString("PHILIPPINE STANDARD TIME", f, b, New RectangleF(0, 14, r.Width, 18), centerFmt)
                End Using
            End Using

            ' ---- time hh:mm:ss ----
            Dim timeText As String = ph.ToString("hh:mm:ss", CultureInfo.InvariantCulture)
            Using f As New Font("Segoe UI", 36.0F, FontStyle.Bold)
                Using shadow As New SolidBrush(Color.FromArgb(90, 0, 0, 0))
                    g.DrawString(timeText, f, shadow, New RectangleF(2, 42, r.Width, 62), centerFmt)
                End Using
                g.DrawString(timeText, f, Brushes.White, New RectangleF(0, 40, r.Width, 62), centerFmt)
            End Using

            ' ---- AM / PM pill (the purple used by the lesson badges) ----
            Dim pill As New RectangleF((r.Width - 60) / 2.0F, 108, 60, 22)
            Using path As GraphicsPath = RoundedPath(pill, 10.0F)
                Using b As New SolidBrush(Color.FromArgb(138, 100, 255))
                    g.FillPath(b, path)
                End Using
            End Using
            Using f As New Font("Segoe UI", 9.0F, FontStyle.Bold)
                g.DrawString(If(ph.Hour < 12, "AM", "PM"), f, Brushes.White, pill, centerFmt)
            End Using

            ' ---- date ----
            Using f As New Font("Segoe UI", 11.0F, FontStyle.Regular)
                Using b As New SolidBrush(Color.FromArgb(230, 230, 240))
                    g.DrawString(ph.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture), f, b, New RectangleF(0, 138, r.Width, 24), centerFmt)
                End Using
            End Using

            ' ---- seconds progress bar ----
            Dim track As New RectangleF(24, 176, r.Width - 48, 6)
            Dim frac As Single = CSng((ph.Second + ph.Millisecond / 1000.0) / 60.0)
            Using path As GraphicsPath = RoundedPath(track, 3.0F)
                Using b As New SolidBrush(Color.FromArgb(50, 255, 255, 255))
                    g.FillPath(b, path)
                End Using
            End Using
            Dim fillW As Single = Math.Max(6.0F, track.Width * frac)
            Dim fillRect As New RectangleF(track.X, track.Y, fillW, track.Height)
            Using path As GraphicsPath = RoundedPath(fillRect, 3.0F)
                Using b As New SolidBrush(Color.FromArgb(138, 100, 255))
                    g.FillPath(b, path)
                End Using
            End Using

            ' ---- footer ----
            Using f As New Font("Segoe UI", 9.0F, FontStyle.Regular)
                Using b As New SolidBrush(Color.FromArgb(190, 185, 225))
                    g.DrawString("Manila, Philippines  " & ChrW(8226) & "  UTC+8", f, b, New RectangleF(0, 196, r.Width, 22), centerFmt)
                End Using
            End Using
        End Using

        ' ---- border (same thin outline style as the Lessons dropdown) ----
        Using pen As New Pen(Color.FromArgb(90, 90, 130), 1)
            g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1)
        End Using
    End Sub

End Class
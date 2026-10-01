''' <summary>
''' Level 2 menu: JUMP KNIGHT. Same four PixelButtons, layout and fade-out as the Level 1 menu.
'''   START      -> opens the Jump Knight game (fresh run)
'''   SETTINGS   -> volume settings (shared GameSettings)
'''   NEXT GAME  -> Level 3 placeholder
'''   EXIT       -> closes this form (the form that opened it appears again)
''' Both entry points (Form1 dropdown and Level 1 "Next Game") use the same helper methods
''' IsOpen / BringExistingToFront so only ONE Level 2 menu can exist at a time.
''' </summary>
Public Class Level2MenuForm
    Inherits Form

    Private scenePanel As Level2ScenePanel
    Private startButton As PixelButton
    Private settingsButton As PixelButton
    Private nextGameButton As PixelButton
    Private exitButton As PixelButton

    Private isTransitioning As Boolean = False

    ' ===================== Shared entry-point helpers =====================

    ''' <summary>True if a Level 2 menu (or its game window) is already open.</summary>
    Public Shared Function IsOpen() As Boolean
        Return Application.OpenForms.OfType(Of Level2MenuForm)().Any()
    End Function

    ''' <summary>Brings the already-open Level 2 window (game if running, otherwise menu) to the front.</summary>
    Public Shared Sub BringExistingToFront()
        Dim game As JumpKnightGameForm = Application.OpenForms.OfType(Of JumpKnightGameForm)().FirstOrDefault()
        If game IsNot Nothing Then
            If game.WindowState = FormWindowState.Minimized Then game.WindowState = FormWindowState.Normal
            game.Activate()
            Return
        End If

        Dim menu As Level2MenuForm = Application.OpenForms.OfType(Of Level2MenuForm)().FirstOrDefault()
        If menu IsNot Nothing Then
            menu.Opacity = 1.0
            If menu.WindowState = FormWindowState.Minimized Then menu.WindowState = FormWindowState.Normal
            menu.Show()
            menu.Activate()
        End If
    End Sub

    ' ===================== Construction =====================

    Public Sub New()
        Me.Text = "Level 2 " & ChrW(8212) & " Jump Knight"
        Me.Size = New Size(900, 650)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable

        scenePanel = New Level2ScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(scenePanel)

        BuildButtons()
        PositionButtons()
        AddHandler scenePanel.Resize, Sub(s, ev) PositionButtons()
        AddHandler Me.FormClosed, AddressOf Level2MenuForm_FormClosed
    End Sub

    Private Sub BuildButtons()
        startButton = New PixelButton("START", Color.FromArgb(76, 175, 80))
        settingsButton = New PixelButton("SETTINGS", Color.FromArgb(66, 133, 200))
        nextGameButton = New PixelButton("NEXT GAME", Color.FromArgb(150, 90, 190))
        exitButton = New PixelButton("EXIT", Color.FromArgb(200, 70, 60))

        AddHandler startButton.Click, AddressOf StartButton_Click
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        AddHandler nextGameButton.Click, AddressOf NextGameButton_Click
        AddHandler exitButton.Click, AddressOf ExitButton_Click

        For Each b As PixelButton In New PixelButton() {startButton, settingsButton, nextGameButton, exitButton}
            ' Same click sound the Level 1 menu buttons use.
            AddHandler b.Click, Sub(s, ev) AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
            scenePanel.Controls.Add(b)
        Next
    End Sub

    Private Sub PositionButtons()
        Dim btnWidth As Integer = 220
        Dim btnHeight As Integer = 52
        Dim gap As Integer = 16
        Dim totalHeight As Integer = btnHeight * 4 + gap * 3
        Dim startY As Integer = (scenePanel.ClientSize.Height - totalHeight) \ 2 + 40
        Dim x As Integer = (scenePanel.ClientSize.Width - btnWidth) \ 2

        Dim buttons() As PixelButton = {startButton, settingsButton, nextGameButton, exitButton}
        For i As Integer = 0 To 3
            buttons(i).Size = New Size(btnWidth, btnHeight)
            buttons(i).Location = New Point(x, startY + i * (btnHeight + gap))
        Next
    End Sub

    ' ===================== Buttons =====================

    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        If isTransitioning Then Return
        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim gameForm As New JumpKnightGameForm()
                                  AddHandler gameForm.FormClosed, Sub(s2, e2)
                                                                      isTransitioning = False
                                                                      If Not Me.IsDisposed Then
                                                                          Me.Opacity = 1.0
                                                                          Me.Show()
                                                                      End If
                                                                  End Sub
                                  Me.Hide()
                                  gameForm.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open Jump Knight: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        Using settingsForm As New Level2SettingsForm()
            settingsForm.ShowDialog(Me)
        End Using
        ' Volume changes apply live through GameSettings, same as Level 1.
    End Sub

    Private Sub NextGameButton_Click(sender As Object, e As EventArgs)
        If isTransitioning Then Return
        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim level3Form As New Level3PlaceholderForm()
                                  AddHandler level3Form.FormClosed, Sub(s2, e2)
                                                                        isTransitioning = False
                                                                        If Not Me.IsDisposed Then
                                                                            Me.Opacity = 1.0
                                                                            Me.Show()
                                                                        End If
                                                                    End Sub
                                  Me.Hide()
                                  level3Form.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open Level 3: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    ' Non-blocking fade using a Timer (no Thread.Sleep), same idea as Level 1.
    Private Sub FadeOutThenAction(onFadeComplete As Action)
        Dim fadeTimer As New System.Windows.Forms.Timer() With {.Interval = 15}
        AddHandler fadeTimer.Tick, Sub(s, e)
                                       Me.Opacity = Math.Max(0.0, Me.Opacity - 0.08)
                                       If Me.Opacity <= 0.0 Then
                                           fadeTimer.Stop()
                                           fadeTimer.Dispose()
                                           onFadeComplete()
                                       End If
                                   End Sub
        fadeTimer.Start()
    End Sub

    Private Sub Level2MenuForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        scenePanel.StopAnimation()
        ' Audio is intentionally left alone: Level 1's music (if it is behind this window)
        ' keeps working, and Level 1 shuts audio down itself when it closes.
    End Sub

End Class

''' <summary>
''' Castle-tower menu scene: slowly scrolling stone wall, side pillars, an idle knight, and the
''' JUMP KNIGHT title. Images are loaded once (JumpKnightAssets) and disposed in StopAnimation.
''' </summary>
Public Class Level2ScenePanel
    Inherits Panel

    Private ReadOnly animTimer As System.Windows.Forms.Timer
    Private ReadOnly art As JumpKnightAssets
    Private scrollOffset As Double = 0.0
    Private clockSeconds As Single = 0.0F
    Private stopped As Boolean = False

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        art = New JumpKnightAssets()

        animTimer = New System.Windows.Forms.Timer() With {.Interval = 50}
        AddHandler animTimer.Tick, Sub(s, e)
                                       scrollOffset += 6.0
                                       clockSeconds += 0.05F
                                       Me.Invalidate()
                                   End Sub
        animTimer.Start()
    End Sub

    Public Sub StopAnimation()
        If stopped Then Return
        stopped = True
        animTimer.Stop()
        animTimer.Dispose()
        art.Dispose()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        If stopped Then Return
        Dim g As Graphics = e.Graphics
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        If w <= 0 OrElse h <= 0 Then Return

        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

        ' --- Wall: repeat the seamless period across the whole width ---
        If art.WallFar IsNot Nothing Then
            Dim periodWidth As Integer = JumpKnightAssets.WallPeriodW * JumpKnightAssets.ArtScale
            Dim x As Integer = 0
            Do While x < w
                art.DrawWallColumn(g, art.WallFar, x, 0, JumpKnightAssets.WallPeriodW, 0, h, scrollOffset)
                x += periodWidth
            Loop
        Else
            g.Clear(Color.FromArgb(25, 18, 14))
        End If

        ' --- Dark overlay so title and buttons stay readable ---
        Using overlay As New SolidBrush(Color.FromArgb(120, 8, 6, 14))
            g.FillRectangle(overlay, 0, 0, w, h)
        End Using

        ' --- Idle knight above the buttons (only if the window is tall enough) ---
        If art.KnightIdle IsNot Nothing AndAlso h >= 560 Then
            Dim bigScale As Integer = 4
            Dim frame As Integer = CInt(Math.Floor(clockSeconds * 8.0F)) Mod JumpKnightAssets.IdleFrames
            Dim src As New Rectangle(frame * JumpKnightAssets.KnightFrameW, 0, JumpKnightAssets.KnightFrameW, JumpKnightAssets.KnightFrameH)
            Dim dest As New Rectangle((w - src.Width * bigScale) \ 2, 100, src.Width * bigScale, src.Height * bigScale)
            g.DrawImage(art.KnightIdle, dest, src, GraphicsUnit.Pixel)
        End If

        ' --- Title ---
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Using titleFont As New Font("Segoe UI", 34.0F, FontStyle.Bold)
            Dim titleText As String = "JUMP KNIGHT"
            Dim titleSize As SizeF = g.MeasureString(titleText, titleFont)
            Dim titleX As Single = (w - titleSize.Width) / 2.0F
            Dim titleY As Single = 30
            Using shadowBrush As New SolidBrush(Color.FromArgb(160, 0, 0, 0))
                g.DrawString(titleText, titleFont, shadowBrush, titleX + 3, titleY + 3)
            End Using
            Using textBrush As New SolidBrush(Color.White)
                g.DrawString(titleText, titleFont, textBrush, titleX, titleY)
            End Using
        End Using

        MyBase.OnPaint(e)
    End Sub

End Class
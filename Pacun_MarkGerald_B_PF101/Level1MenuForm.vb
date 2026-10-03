Imports System.Drawing.Drawing2D
Imports System.Linq

Public Class Level1MenuForm
    Inherits Form

    Private scenePanel As ScenePanel
    Private startButton As PixelButton
    Private settingsButton As PixelButton
    Private nextGameButton As PixelButton
    Private exitButton As PixelButton

    ' Client-area fade overlay (never touches the window frame)
    Private fadeOverlay As FadeOverlayPanel

    ' Settings UI (Level 2 style)
    Private settingsPanel As Panel
    Private musicSlider As PixelSlider
    Private sfxSlider As PixelSlider
    Private musicLabel As Label
    Private sfxLabel As Label
    Private backSettingsButton As Button

    ' State
    Private isTransitioning As Boolean = False
    Private settingsOpen As Boolean = False

    Public Sub New()
        Me.Text = "Level 1 " & ChrW(8212) & " River Crossing"
        Me.Size = New Size(900, 650)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.FromArgb(20, 20, 26)

        scenePanel = New ScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(scenePanel)

        BuildButtons()
        BuildSettingsPanel()

        ' Fade overlay must be added AFTER the scene panel to sit on top.
        ' WS_EX_TRANSPARENT makes it a click-through, alpha-fading layer
        ' that only covers the client area, never the title bar.
        fadeOverlay = New FadeOverlayPanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(fadeOverlay)
        fadeOverlay.BringToFront()
        AddHandler fadeOverlay.FadeComplete, AddressOf FadeOverlay_FadeComplete

        PositionButtons()
        AddHandler scenePanel.Resize, Sub(s, ev) PositionButtons()
        AddHandler Me.Resize, Sub(s, ev)
                                  If settingsPanel IsNot Nothing AndAlso settingsPanel.Visible Then
                                      CenterSettingsPanel()
                                  End If
                              End Sub

        AddHandler Me.Shown, Sub(s, ev) AudioManager.PlayMusic("Audio\Music\Wet Hands.mp3", True)
        AddHandler Me.FormClosed, AddressOf Level1MenuForm_FormClosed
    End Sub

    ' ==================================================
    ' MAIN MENU BUTTONS
    ' ==================================================
    Private Sub BuildButtons()
        startButton = New PixelButton("START", Color.FromArgb(76, 175, 80))
        settingsButton = New PixelButton("SETTINGS", Color.FromArgb(66, 133, 200))
        nextGameButton = New PixelButton("NEXT GAME", Color.FromArgb(150, 90, 190))
        exitButton = New PixelButton("EXIT", Color.FromArgb(200, 70, 60))

        AddHandler startButton.Click, AddressOf StartButton_Click
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        AddHandler nextGameButton.Click, AddressOf NextGameButton_Click
        AddHandler exitButton.Click, AddressOf ExitButton_Click

        For Each b In New PixelButton() {startButton, settingsButton, nextGameButton, exitButton}
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

        For i As Integer = 0 To 3
            Dim btn As PixelButton = {startButton, settingsButton, nextGameButton, exitButton}(i)
            btn.Size = New Size(btnWidth, btnHeight)
            btn.Location = New Point(x, startY + i * (btnHeight + gap))
        Next
    End Sub

    Private Sub SetMenuButtonsVisible(visible As Boolean)
        startButton.Visible = visible
        settingsButton.Visible = visible
        nextGameButton.Visible = visible
        exitButton.Visible = visible
    End Sub

    ' ==================================================
    ' SETTINGS PANEL  (Level 2 visual design)
    ' ==================================================
    Private Sub BuildSettingsPanel()
        settingsPanel = New Panel() With {
            .Size = New Size(500, 460),
            .BackColor = Color.FromArgb(26, 26, 46),
            .Visible = False
        }
        Me.Controls.Add(settingsPanel)
        settingsPanel.BringToFront()

        ' Gold frame border
        AddHandler settingsPanel.Paint, Sub(s, pe)
                                            Dim r = settingsPanel.ClientRectangle
                                            r = New Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1)
                                            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                            Using pen As New Pen(Color.FromArgb(218, 165, 32), 2)
                                                pe.Graphics.DrawRectangle(pen, r)
                                            End Using
                                            ' Gold underline under the title
                                            Using pen As New Pen(Color.FromArgb(218, 165, 32), 1)
                                                pe.Graphics.DrawLine(pen, 60, 78, settingsPanel.Width - 60, 78)
                                            End Using
                                        End Sub

        Dim titleLabel As New Label() With {
            .Text = "SETTINGS",
            .Font = New Font("Segoe UI", 24.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.Transparent,
            .Size = New Size(settingsPanel.Width, 66),
            .Location = New Point(0, 6),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        settingsPanel.Controls.Add(titleLabel)

        ' --- Background Music ---
        Dim musicCaption As New Label() With {
            .Text = "BACKGROUND MUSIC",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(50, 100)
        }
        settingsPanel.Controls.Add(musicCaption)

        musicLabel = New Label() With {
            .Text = GameSettings.GetInstance().MusicVolume & "%",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(218, 165, 32),
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(340, 100)
        }
        settingsPanel.Controls.Add(musicLabel)

        musicSlider = New PixelSlider() With {
            .Location = New Point(50, 130),
            .Size = New Size(400, 22),
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().MusicVolume,
            .BarColor = Color.FromArgb(120, 200, 80)   ' green
        }
        AddHandler musicSlider.ValueChanged, Sub(s, e)
                                                 GameSettings.GetInstance().MusicVolume = musicSlider.Value
                                                 musicLabel.Text = musicSlider.Value & "%"
                                                 AudioManager.ApplyMusicVolume()
                                             End Sub
        settingsPanel.Controls.Add(musicSlider)

        ' --- Sound Effects ---
        Dim sfxCaption As New Label() With {
            .Text = "SOUND EFFECTS",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(50, 190)
        }
        settingsPanel.Controls.Add(sfxCaption)

        sfxLabel = New Label() With {
            .Text = GameSettings.GetInstance().SfxVolume & "%",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(218, 165, 32),
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(340, 190)
        }
        settingsPanel.Controls.Add(sfxLabel)

        sfxSlider = New PixelSlider() With {
            .Location = New Point(50, 220),
            .Size = New Size(400, 22),
            .Minimum = 0,
            .Maximum = 100,
            .Value = GameSettings.GetInstance().SfxVolume,
            .BarColor = Color.FromArgb(80, 160, 230)   ' blue
        }
        AddHandler sfxSlider.ValueChanged, Sub(s, e)
                                               GameSettings.GetInstance().SfxVolume = sfxSlider.Value
                                               sfxLabel.Text = sfxSlider.Value & "%"
                                           End Sub
        settingsPanel.Controls.Add(sfxSlider)

        ' --- BACK button (Level 2 red) ---
        backSettingsButton = New Button() With {
            .Text = "BACK",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(180, 50, 50),
            .ForeColor = Color.White,
            .Size = New Size(230, 48),
            .Location = New Point(135, 320)
        }
        backSettingsButton.FlatAppearance.BorderSize = 0
        AddHandler backSettingsButton.Click, Sub(s, e)
                                                 AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
                                                 BeginSettingsTransition(False)
                                             End Sub
        settingsPanel.Controls.Add(backSettingsButton)

        ' --- "Esc also goes back" hint ---
        Dim hintLabel As New Label() With {
            .Text = "Esc also goes back",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(150, 150, 160),
            .BackColor = Color.Transparent,
            .Size = New Size(settingsPanel.Width, 20),
            .Location = New Point(0, 380),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        settingsPanel.Controls.Add(hintLabel)
    End Sub

    Private Sub CenterSettingsPanel()
        settingsPanel.Location = New Point(
            Math.Max(0, (Me.ClientSize.Width - settingsPanel.Width) \ 2),
            Math.Max(0, (Me.ClientSize.Height - settingsPanel.Height) \ 2))
    End Sub

    ' ==================================================
    ' TRANSITION (client-area-only fade via overlay panel)
    ' ==================================================
    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        BeginSettingsTransition(True)
    End Sub

    Private Sub BeginSettingsTransition(toSettings As Boolean)
        If isTransitioning Then Return
        isTransitioning = True
        scenePanel.PauseAnimation()          ' freeze the wave so it doesn't fight the fade
        fadeOverlay.PendingTarget = toSettings
        fadeOverlay.FadeIn(Me)               ' pass the form for snapshot capture
    End Sub

    Private Sub FadeOverlay_FadeComplete(isFadeIn As Boolean)
        If isFadeIn Then
            ' Fully dark: swap content underneath.
            ' IMPORTANT: do NOT call BringToFront on the settings panel.
            ' The overlay is already the topmost control, so the settings
            ' panel appears *under* it and is revealed by the fade-out.
            If fadeOverlay.PendingTarget Then
                SetMenuButtonsVisible(False)
                CenterSettingsPanel()
                settingsPanel.Visible = True
                settingsOpen = True
            Else
                settingsPanel.Visible = False
                SetMenuButtonsVisible(True)
                settingsOpen = False
            End If
            ' Fade back to reveal the new content (captures a fresh snapshot).
            fadeOverlay.FadeOut(Me)
        Else
            ' Fully visible again — transition done.
            scenePanel.ResumeAnimation()
            isTransitioning = False
        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape AndAlso settingsOpen AndAlso Not isTransitioning Then
            BeginSettingsTransition(False)
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' ==================================================
    ' START / NEXT / EXIT
    ' ==================================================
    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        If isTransitioning OrElse settingsOpen Then Return
        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim gameplayForm As New Level1GameplayForm()
                                  AddHandler gameplayForm.FormClosed, Sub(s2, e2)
                                                                          isTransitioning = False
                                                                          Me.Opacity = 1.0
                                                                          Me.Show()
                                                                      End Sub
                                  Me.Hide()
                                  gameplayForm.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open gameplay: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    Private Sub NextGameButton_Click(sender As Object, e As EventArgs)
        If isTransitioning OrElse settingsOpen Then Return

        If Level2MenuForm.IsOpen() Then
            Level2MenuForm.BringExistingToFront()
            Return
        End If

        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim level2Menu As New Level2MenuForm()
                                  AddHandler level2Menu.FormClosed, Sub(s2, e2)
                                                                        isTransitioning = False
                                                                        Me.Opacity = 1.0
                                                                        Me.Show()
                                                                    End Sub
                                  Me.Hide()
                                  level2Menu.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open Level 2: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub FadeOutThenAction(onFadeComplete As Action)
        Dim fadeTimer As New Timer() With {.Interval = 15}
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

    Private Sub Level1MenuForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        scenePanel.StopAnimation()
        If Application.OpenForms.OfType(Of Level1GameplayForm)().Count() = 0 AndAlso
           Application.OpenForms.OfType(Of Level2MenuForm)().Count() = 0 Then
            AudioManager.ShutdownAll()
        End If
    End Sub

End Class

' =========================================================
' CLIENT-AREA FADE OVERLAY
' Paints a bitmap snapshot of the client area with an increasing
' or decreasing dark tint. Because it paints a bitmap rather than
' compositing with siblings, the transition is flicker-free and
' never touches the window frame / title bar.
' =========================================================
Public Class FadeOverlayPanel
    Inherits Panel

    Public Event FadeComplete(isFadeIn As Boolean)

    Public Property PendingTarget As Boolean = False

    Private _alpha As Integer = 0
    Private _timer As Timer
    Private _snapshot As Bitmap = Nothing

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or
                    ControlStyles.Opaque, True)
        Me.Visible = False
        Me.TabStop = False
    End Sub

    ''' <summary>Captures the current client area of the given form into a bitmap.</summary>
    Private Sub CaptureSnapshot(sourceForm As Form)
        If _snapshot IsNot Nothing Then
            _snapshot.Dispose()
            _snapshot = Nothing
        End If
        Dim w As Integer = Math.Max(1, sourceForm.ClientSize.Width)
        Dim h As Integer = Math.Max(1, sourceForm.ClientSize.Height)
        _snapshot = New Bitmap(w, h)
        Try
            sourceForm.DrawToBitmap(_snapshot, New Rectangle(0, 0, w, h))
        Catch
            ' DrawToBitmap can occasionally fail on certain GPU drivers;
            ' fall back to a flat dark fill so the transition still works.
            Using g As Graphics = Graphics.FromImage(_snapshot)
                g.Clear(Color.FromArgb(20, 20, 26))
            End Using
        End Try
    End Sub

    Public Sub FadeIn(sourceForm As Form)
        CaptureSnapshot(sourceForm)
        _alpha = 0
        Me.Visible = True
        Me.BringToFront()
        StartFade(255)
    End Sub

    Public Sub FadeOut(sourceForm As Form)
        CaptureSnapshot(sourceForm)
        _alpha = 255
        Me.Visible = True
        Me.BringToFront()
        StartFade(0)
    End Sub

    Private Sub StartFade(target As Integer)
        If _timer IsNot Nothing Then
            _timer.Stop()
            _timer.Dispose()
            _timer = Nothing
        End If
        Dim goingIn As Boolean = (target = 255)
        _timer = New Timer() With {.Interval = 15}
        AddHandler _timer.Tick, Sub(s, e)
                                    If _alpha < target Then
                                        _alpha = Math.Min(target, _alpha + 15)
                                    ElseIf _alpha > target Then
                                        _alpha = Math.Max(target, _alpha - 15)
                                    End If
                                    Me.Invalidate()

                                    If _alpha = target Then
                                        _timer.Stop()
                                        _timer.Dispose()
                                        _timer = Nothing
                                        If Not goingIn Then Me.Visible = False
                                        RaiseEvent FadeComplete(goingIn)
                                    End If
                                End Sub
        _timer.Start()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        ' Paint the frozen snapshot first...
        If _snapshot IsNot Nothing Then
            e.Graphics.DrawImageUnscaled(_snapshot, 0, 0)
        Else
            Using b As New SolidBrush(Color.FromArgb(20, 20, 26))
                e.Graphics.FillRectangle(b, Me.ClientRectangle)
            End Using
        End If

        ' ...then layer the dark tint on top. alpha 0 = fully visible snapshot,
        ' alpha 255 = fully dark.
        If _alpha > 0 Then
            Using b As New SolidBrush(Color.FromArgb(_alpha, 20, 20, 26))
                e.Graphics.FillRectangle(b, Me.ClientRectangle)
            End Using
        End If
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If _timer IsNot Nothing Then
                _timer.Stop()
                _timer.Dispose()
                _timer = Nothing
            End If
            If _snapshot IsNot Nothing Then
                _snapshot.Dispose()
                _snapshot = Nothing
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class


' =========================================================
' PIXEL SLIDER
' Custom horizontal slider matching the Level 2 reference:
' dark track + colored fill + white thumb.
' =========================================================
Public Class PixelSlider
    Inherits Control

    Public Event ValueChanged(sender As Object, e As EventArgs)

    Private _value As Integer = 100
    Private _minimum As Integer = 0
    Private _maximum As Integer = 100
    Private _barColor As Color = Color.FromArgb(120, 200, 80)
    Private _dragging As Boolean = False

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or
                    ControlStyles.ResizeRedraw, True)
        Me.Cursor = Cursors.Hand
        Me.Height = 22
    End Sub

    Public Property Value As Integer
        Get
            Return _value
        End Get
        Set(v As Integer)
            Dim clamped As Integer = Math.Max(_minimum, Math.Min(_maximum, v))
            If clamped <> _value Then
                _value = clamped
                Me.Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public Property Minimum As Integer
        Get
            Return _minimum
        End Get
        Set(v As Integer)
            _minimum = v
            If _value < _minimum Then Value = _minimum
            Me.Invalidate()
        End Set
    End Property

    Public Property Maximum As Integer
        Get
            Return _maximum
        End Get
        Set(v As Integer)
            _maximum = v
            If _value > _maximum Then Value = _maximum
            Me.Invalidate()
        End Set
    End Property

    Public Property BarColor As Color
        Get
            Return _barColor
        End Get
        Set(v As Color)
            _barColor = v
            Me.Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.None

        Dim trackH As Integer = 6
        Dim trackY As Integer = (Height - trackH) \ 2
        Dim trackRect As New Rectangle(0, trackY, Width - 1, trackH)

        ' Track background
        Using bg As New SolidBrush(Color.FromArgb(40, 40, 60))
            g.FillRectangle(bg, trackRect)
        End Using

        ' Filled portion
        Dim range As Integer = Math.Max(1, _maximum - _minimum)
        Dim pct As Single = CSng(_value - _minimum) / range
        Dim fillW As Integer = CInt(pct * (Width - 1))
        If fillW > 0 Then
            Using fill As New SolidBrush(_barColor)
                g.FillRectangle(fill, New Rectangle(0, trackY, fillW, trackH))
            End Using
        End If

        ' Thumb
        Dim thumbW As Integer = 10
        Dim thumbH As Integer = 18
        Dim thumbX As Integer = Math.Max(0, Math.Min(Width - thumbW, fillW - thumbW \ 2))
        Dim thumbY As Integer = (Height - thumbH) \ 2
        Using thumbFill As New SolidBrush(Color.White)
            g.FillRectangle(thumbFill, thumbX, thumbY, thumbW, thumbH)
        End Using
        Using thumbPen As New Pen(Color.FromArgb(80, 80, 100))
            g.DrawRectangle(thumbPen, thumbX, thumbY, thumbW - 1, thumbH - 1)
        End Using
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        _dragging = True
        UpdateFromX(e.X)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If _dragging Then UpdateFromX(e.X)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        _dragging = False
    End Sub

    Private Sub UpdateFromX(x As Integer)
        Dim pct As Single = Math.Max(0.0F, Math.Min(1.0F, x / CSng(Math.Max(1, Width - 1))))
        Dim newVal As Integer = CInt(Math.Round(_minimum + pct * (_maximum - _minimum)))
        Value = newVal
    End Sub
End Class


' =========================================================
' SCENE PANEL (background art)
' =========================================================
Public Class ScenePanel
    Inherits Panel

    Private animTimer As Timer
    Private waveOffset As Integer = 0

    Private skyTile As Image
    Private skyDecor As Image
    Private bankTile As Image
    Private waterTile As Image

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        LoadAsset("enviroment\sky.png", skyTile)
        LoadAsset("enviroment\sky_decor.png", skyDecor)
        LoadAsset("tiles\Grass_rocks.png", bankTile)
        LoadAsset("tiles\water-Sheet.png", waterTile)

        animTimer = New Timer() With {.Interval = 90}
        AddHandler animTimer.Tick, Sub(s, e)
                                       waveOffset = (waveOffset + 2) Mod 64
                                       Me.Invalidate()
                                   End Sub
        animTimer.Start()
    End Sub

    Private Sub LoadAsset(relativePath As String, ByRef target As Image)
        Dim fullPath As String = IO.Path.Combine(AudioManager.AssetsRoot, relativePath)
        If IO.File.Exists(fullPath) Then
            Try
                target = Image.FromFile(fullPath)
            Catch ex As Exception
                Debug.WriteLine("ScenePanel: failed to load " & fullPath & " - " & ex.Message)
                target = Nothing
            End Try
        Else
            Debug.WriteLine("ScenePanel: asset not found: " & fullPath)
            target = Nothing
        End If
    End Sub

    ''' <summary>Temporarily stops the wave animation (does not dispose images).</summary>
    Public Sub PauseAnimation()
        If animTimer IsNot Nothing Then animTimer.Stop()
    End Sub

    ''' <summary>Resumes the wave animation after PauseAnimation.</summary>
    Public Sub ResumeAnimation()
        If animTimer IsNot Nothing AndAlso Not animTimer.Enabled Then animTimer.Start()
    End Sub

    Public Sub StopAnimation()
        animTimer.Stop()
        DisposeIfNotNothing(skyTile)
        DisposeIfNotNothing(skyDecor)
        DisposeIfNotNothing(bankTile)
        DisposeIfNotNothing(waterTile)
    End Sub

    Private Sub DisposeIfNotNothing(img As Image)
        If img IsNot Nothing Then
            Try
                img.Dispose()
            Catch
            End Try
        End If
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        If w <= 0 OrElse h <= 0 Then Return

        g.SmoothingMode = SmoothingMode.None
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = PixelOffsetMode.Half

        If skyTile IsNot Nothing Then
            TileImage(g, skyTile, New Rectangle(0, 0, w, h))
        Else
            Using skyBrush As New LinearGradientBrush(New Rectangle(0, 0, w, h), Color.FromArgb(120, 190, 235), Color.FromArgb(200, 230, 245), LinearGradientMode.Vertical)
                g.FillRectangle(skyBrush, 0, 0, w, h)
            End Using
        End If

        If skyDecor IsNot Nothing Then
            g.DrawImage(skyDecor, New Rectangle(w - skyDecor.Width - 16, 16, skyDecor.Width, skyDecor.Height))
        End If

        Dim riverTop As Integer = CInt(h * 0.62)
        Dim riverHeight As Integer = CInt(h * 0.2)
        Dim riverRect As New Rectangle(0, riverTop, w, riverHeight)

        If waterTile IsNot Nothing Then
            TileImageScrolling(g, waterTile, riverRect, waveOffset)
        Else
            Using riverBrush As New SolidBrush(Color.FromArgb(70, 140, 200))
                g.FillRectangle(riverBrush, riverRect)
            End Using
        End If

        Dim bankTop As Integer = CInt(h * 0.5)
        If bankTile IsNot Nothing Then
            TileImage(g, bankTile, New Rectangle(0, bankTop, w, riverTop - bankTop))
            TileImage(g, bankTile, New Rectangle(0, riverTop + riverHeight, w, h - (riverTop + riverHeight)))
        Else
            Using grassBrush As New SolidBrush(Color.FromArgb(90, 170, 80))
                g.FillRectangle(grassBrush, 0, bankTop, w, riverTop - bankTop)
            End Using
            Using dirtBrush As New SolidBrush(Color.FromArgb(120, 90, 60))
                g.FillRectangle(dirtBrush, 0, riverTop + riverHeight, w, h - (riverTop + riverHeight))
            End Using
        End If

        Using overlay As New SolidBrush(Color.FromArgb(110, 10, 10, 20))
            g.FillRectangle(overlay, 0, 0, w, h)
        End Using

        Using titleFont As New Font("Segoe UI", 34.0F, FontStyle.Bold)
            Dim titleText As String = "RIVER CROSSING"
            Dim titleSize = g.MeasureString(titleText, titleFont)
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

    Private Sub TileImage(g As Graphics, img As Image, area As Rectangle)
        If area.Width <= 0 OrElse area.Height <= 0 Then Return
        Dim oldClip = g.Clip
        g.SetClip(area)
        Dim y As Integer = area.Top
        While y < area.Bottom
            Dim x As Integer = area.Left
            While x < area.Right
                g.DrawImage(img, x, y, img.Width, img.Height)
                x += img.Width
            End While
            y += img.Height
        End While
        g.Clip = oldClip
    End Sub

    Private Sub TileImageScrolling(g As Graphics, img As Image, area As Rectangle, offsetX As Integer)
        If area.Width <= 0 OrElse area.Height <= 0 OrElse img.Width <= 0 Then Return
        Dim oldClip = g.Clip
        g.SetClip(area)
        Dim startX As Integer = area.Left - (offsetX Mod img.Width)
        Dim y As Integer = area.Top
        While y < area.Bottom
            Dim x As Integer = startX
            While x < area.Right
                g.DrawImage(img, x, y, img.Width, img.Height)
                x += img.Width
            End While
            y += img.Height
        End While
        g.Clip = oldClip
    End Sub

End Class


' =========================================================
' PIXEL BUTTON (menu buttons)
' =========================================================
Public Class PixelButton
    Inherits Panel

    Private ReadOnly baseColor As Color
    Private ReadOnly captionText As String
    Private isHovering As Boolean = False
    Private isPressed As Boolean = False

    Public Sub New(caption As String, color As Color)
        captionText = caption
        baseColor = color
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.Cursor = Cursors.Hand
        Me.BackColor = Color.Transparent

        AddHandler Me.MouseEnter, Sub(s, e)
                                      isHovering = True
                                      Invalidate()
                                  End Sub
        AddHandler Me.MouseLeave, Sub(s, e)
                                      isHovering = False
                                      isPressed = False
                                      Invalidate()
                                  End Sub
        AddHandler Me.MouseDown, Sub(s, e)
                                     isPressed = True
                                     Invalidate()
                                 End Sub
        AddHandler Me.MouseUp, Sub(s, e)
                                   isPressed = False
                                   Invalidate()
                               End Sub
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.None

        Dim fillColor As Color = baseColor
        If isPressed Then
            fillColor = ControlPaint.Dark(baseColor, 0.2F)
        ElseIf isHovering Then
            fillColor = ControlPaint.Light(baseColor, 0.15F)
        End If

        Dim bodyRect As New Rectangle(0, 0, Width - 1, Height - 5)
        Dim shadowRect As New Rectangle(0, Height - 5, Width - 1, 4)

        Using shadowBrush As New SolidBrush(ControlPaint.Dark(baseColor, 0.4F))
            g.FillRectangle(shadowBrush, shadowRect)
        End Using
        Using fillBrush As New SolidBrush(fillColor)
            g.FillRectangle(fillBrush, bodyRect)
        End Using
        Using outlinePen As New Pen(Color.FromArgb(30, 20, 10), 2)
            g.DrawRectangle(outlinePen, 1, 1, bodyRect.Width - 2, bodyRect.Height - 2)
        End Using

        Using font As New Font("Segoe UI", 11.0F, FontStyle.Bold)
            Dim sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            Dim textRect As New RectangleF(0, 0, Width, bodyRect.Height)
            Using shadowBrush As New SolidBrush(Color.FromArgb(140, 0, 0, 0))
                g.DrawString(captionText, font, shadowBrush, New RectangleF(textRect.X + 1, textRect.Y + 1, textRect.Width, textRect.Height), sf)
            End Using
            Using textBrush As New SolidBrush(Color.White)
                g.DrawString(captionText, font, textBrush, textRect, sf)
            End Using
        End Using

        MyBase.OnPaint(e)
    End Sub

End Class
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Drawing.Text
Imports System.Linq

Public Enum MenuLayer
    None
    Main
    Settings
    HowTo
End Enum

Friend Enum FadePhase
    Idle
    FadingOut
    FadingIn
End Enum

Public Class Level1MenuForm
    Inherits Form

    Private scenePanel As MenuScenePanel
    Private isTransitioning As Boolean = False
    Private Const MenuMusic As String = "Audio\Music\Wet_Hands.mp3"

    Public Sub New()
        Me.Text = "Level 1 " & ChrW(8212) & " River Crossing"

        ' Taller window so the How to Play panel always has room. Clamped for short screens.
        Dim workArea As Rectangle = Screen.PrimaryScreen.WorkingArea
        Me.Size = New Size(900, Math.Min(780, workArea.Height - 40))
        Me.MinimumSize = New Size(700, Math.Min(640, workArea.Height - 40))

        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.FromArgb(120, 190, 110)

        scenePanel = New MenuScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(scenePanel)

        AddHandler scenePanel.StartClicked, AddressOf OnStartClicked
        AddHandler scenePanel.NextGameClicked, AddressOf OnNextGameClicked
        AddHandler scenePanel.ExitClicked, AddressOf OnExitClicked

        AddHandler Me.Shown, Sub(s, ev)
                                 AudioManager.PlayMusic(MenuMusic, True)
                                 scenePanel.GoToLayer(MenuLayer.Main)
                             End Sub
        ' Don't spend CPU animating water while this form is hidden behind a game.
        AddHandler Me.VisibleChanged, Sub(s, ev) scenePanel.SetRunning(Me.Visible)
        AddHandler Me.FormClosed, AddressOf Level1MenuForm_FormClosed
    End Sub

    Private Sub OnStartClicked()
        If isTransitioning Then Return
        isTransitioning = True
        ' Only the menu objects fade out; the background stays put.
        scenePanel.GoToLayer(MenuLayer.None, Sub() OpenChildForm(New Level1GameplayForm()))
    End Sub

    Private Sub OnNextGameClicked()
        If isTransitioning Then Return
        isTransitioning = True
        ' <-- If your Jump Knight menu class has a different name, change it on the next line.
        scenePanel.GoToLayer(MenuLayer.None, Sub() OpenChildForm(New Level2MenuForm(), True))
    End Sub

    Private Sub OnExitClicked()
        Me.Close()
    End Sub

    Private Sub OpenChildForm(childForm As Form, Optional stopMenuMusic As Boolean = False)
        Try
            AddHandler childForm.FormClosed, Sub(s2, e2)
                                                 isTransitioning = False
                                                 If Not Me.IsDisposed Then
                                                     Me.Show()
                                                     ' Brings the menu music back after a game (stops whatever the game was playing).
                                                     AudioManager.PlayMusic(MenuMusic, True)
                                                     scenePanel.GoToLayer(MenuLayer.Main)
                                                 End If
                                             End Sub
            If stopMenuMusic Then AudioManager.StopMusic()
            Me.Hide()
            childForm.Show()
        Catch ex As Exception
            isTransitioning = False
            Me.Show()
            AudioManager.PlayMusic(MenuMusic, True)
            scenePanel.GoToLayer(MenuLayer.Main)
            MessageBox.Show("Could not open the next screen: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Level1MenuForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        scenePanel.StopAnimation()
        If Application.OpenForms.OfType(Of Level1GameplayForm)().Count() = 0 Then
            AudioManager.ShutdownAll()
        End If
    End Sub

End Class

''' <summary>
''' The whole menu screen on ONE surface: the (never-fading) river background, plus three layers --
''' Main, Settings, How To Play. Only the active layer's objects fade. Settings and How To Play share
''' one green/blue nature theme. The How To Play panel is measured at draw time, so its button can
''' never overlap its text.
''' </summary>
Public Class MenuScenePanel
    Inherits Panel

    Public Event StartClicked()
    Public Event NextGameClicked()
    Public Event ExitClicked()

    Private Const FadeSeconds As Double = 0.22
    Private Const HowToSidePad As Integer = 36
    Private Const HowToChrome As Integer = 168    ' header band + gaps + button + bottom margin

    Private Class HowToSection
        Public ReadOnly Heading As String
        Public ReadOnly Body As String
        Public ReadOnly Accent As Color

        Public Sub New(headingValue As String, bodyValue As String, accentValue As Color)
            Heading = headingValue
            Body = bodyValue
            Accent = accentValue
        End Sub
    End Class

    Private ReadOnly _backdrop As New RiverBackdrop()
    Private _timer As Timer
    Private ReadOnly _clock As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
    Private _lastMs As Long = 0

    Private _layer As MenuLayer = MenuLayer.None
    Private _pending As MenuLayer = MenuLayer.None
    Private _alpha As Single = 0.0F
    Private _phase As FadePhase = FadePhase.Idle
    Private _onSwitched As Action = Nothing

    Private ReadOnly _mainButtons As New List(Of PixelButtonDef)
    Private ReadOnly _settingsButtons As New List(Of PixelButtonDef)
    Private ReadOnly _howToButtons As New List(Of PixelButtonDef)
    Private ReadOnly _howToSections As New List(Of HowToSection)
    Private _hover As PixelButtonDef = Nothing
    Private _pressed As PixelButtonDef = Nothing

    Private _dragSlider As Integer = 0   ' 0 = none, 1 = music, 2 = sound effects
    Private _musicTrack As Rectangle
    Private _sfxTrack As Rectangle
    Private _settingsPanel As Rectangle
    Private _howToPanel As Rectangle
    Private _handShown As Boolean = False

    Private ReadOnly _titleFont As New Font("Segoe UI", 34.0F, FontStyle.Bold)
    Private ReadOnly _buttonFont As New Font("Segoe UI", 11.0F, FontStyle.Bold)
    Private ReadOnly _headerFont As New Font("Segoe UI", 18.0F, FontStyle.Bold)
    Private ReadOnly _captionFont As New Font("Segoe UI", 10.0F, FontStyle.Bold)
    Private ReadOnly _bodyFont As New Font("Segoe UI", 10.5F)
    Private ReadOnly _bodySmallFont As New Font("Segoe UI", 9.0F)
    Private ReadOnly _centerFormat As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

    Private _buffer As Bitmap = Nothing

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        _mainButtons.Add(New PixelButtonDef("start", "START", Color.FromArgb(76, 175, 80)))
        _mainButtons.Add(New PixelButtonDef("settings", "SETTINGS", Color.FromArgb(66, 133, 200)))
        _mainButtons.Add(New PixelButtonDef("next", "NEXT GAME", Color.FromArgb(150, 90, 190)))
        _mainButtons.Add(New PixelButtonDef("exit", "EXIT", Color.FromArgb(200, 70, 60)))
        _mainButtons.Add(New PixelButtonDef("howto", "HOW TO PLAY", Color.FromArgb(230, 150, 40)))
        _settingsButtons.Add(New PixelButtonDef("back", "BACK TO MENU", Color.FromArgb(46, 139, 87)))
        _howToButtons.Add(New PixelButtonDef("back", "BACK", Color.FromArgb(46, 139, 87)))

        BuildHowToSections()
        LayoutControls()

        _lastMs = _clock.ElapsedMilliseconds
        _timer = New Timer() With {.Interval = 30}
        AddHandler _timer.Tick, AddressOf OnTick
        _timer.Start()
    End Sub

    Private Sub BuildHowToSections()
        Dim bullet As String = ChrW(8226) & " "
        _howToSections.Add(New HowToSection("GOAL",
            "Get all 3 farmers and all 3 goblins from the right bank to the left bank.",
            Color.FromArgb(36, 110, 60)))
        _howToSections.Add(New HowToSection("HOW TO PLAY",
            bullet & "Click a character to put them on the boat. Click someone on the boat to take them off." & vbLf &
            bullet & "The boat carries 1 or 2 passengers and can't sail empty." & vbLf &
            bullet & "Press CROSS RIVER to sail.",
            Color.FromArgb(35, 105, 165)))
        _howToSections.Add(New HowToSection("HOW YOU LOSE",
            "If goblins outnumber the farmers on a bank that has at least one farmer, the goblins attack and you lose.",
            Color.FromArgb(150, 88, 38)))
        _howToSections.Add(New HowToSection("MOVES",
            "Every character you pick, every unloading, and every boat trip counts as one move.",
            Color.FromArgb(30, 128, 120)))
    End Sub

    ' ==================================================
    ' PUBLIC API
    ' ==================================================

    ''' <summary>Fades the current layer out, switches to target, then fades it in. onSwitched runs at the switch.</summary>
    Public Sub GoToLayer(target As MenuLayer, Optional onSwitched As Action = Nothing)
        If _phase <> FadePhase.Idle Then Return
        If target = _layer AndAlso onSwitched Is Nothing Then Return
        _pending = target
        _onSwitched = onSwitched
        _phase = FadePhase.FadingOut
    End Sub

    Public Sub SetRunning(isRunning As Boolean)
        If _timer Is Nothing Then Return
        If isRunning Then
            _lastMs = _clock.ElapsedMilliseconds
            _timer.Start()
        Else
            _timer.Stop()
        End If
    End Sub

    Public Sub StopAnimation()
        If _timer IsNot Nothing Then _timer.Stop()
    End Sub

    ' ==================================================
    ' LAYOUT
    ' ==================================================
    Private Sub LayoutControls()
        Dim w As Integer = Math.Max(1, Me.ClientSize.Width)
        Dim h As Integer = Math.Max(1, Me.ClientSize.Height)
        _backdrop.Layout(w, h)

        ' Main layer: four centred buttons plus How To Play at the upper left.
        Dim bw As Integer = 250
        Dim bh As Integer = 54
        Dim gap As Integer = 16
        Dim total As Integer = 4 * bh + 3 * gap
        Dim startY As Integer = Math.Max(150, (h - total) \ 2 + 50)
        Dim x As Integer = (w - bw) \ 2
        For i As Integer = 0 To 3
            _mainButtons(i).Rect = New Rectangle(x, startY + i * (bh + gap), bw, bh)
        Next
        _mainButtons(4).Rect = New Rectangle(16, 16, 190, 44)

        ' Settings panel
        Dim pw As Integer = Math.Min(540, w - 40)
        Dim ph As Integer = Math.Min(400, h - 40)
        _settingsPanel = New Rectangle((w - pw) \ 2, (h - ph) \ 2, pw, ph)
        _musicTrack = New Rectangle(_settingsPanel.X + 44, _settingsPanel.Y + 120, Math.Max(10, pw - 88), 18)
        _sfxTrack = New Rectangle(_settingsPanel.X + 44, _settingsPanel.Y + 226, Math.Max(10, pw - 88), 18)
        _settingsButtons(0).Rect = New Rectangle(_settingsPanel.X + (pw - 220) \ 2, _settingsPanel.Bottom - 70, 220, 46)

        ' How-to panel: left edge and width are set here; its height is measured at draw time.
        Dim hw As Integer = Math.Min(600, w - 40)
        _howToPanel = New Rectangle((w - hw) \ 2, 20, hw, 400)
        _howToButtons(0).Rect = New Rectangle(_howToPanel.X + (hw - 200) \ 2, _howToPanel.Bottom - 70, 200, 46)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        LayoutControls()
        Me.Invalidate()
    End Sub

    ' ==================================================
    ' ANIMATION / FADE
    ' ==================================================
    Private Sub OnTick(sender As Object, e As EventArgs)
        If Me.IsDisposed Then Return
        Dim nowMs As Long = _clock.ElapsedMilliseconds
        Dim dt As Double = Math.Min(0.1, (nowMs - _lastMs) / 1000.0)
        _lastMs = nowMs

        _backdrop.Advance(dt)
        UpdateFade(dt)
        Me.Invalidate()
    End Sub

    Private Sub UpdateFade(dt As Double)
        Select Case _phase
            Case FadePhase.FadingOut
                _alpha -= CSng(dt / FadeSeconds)
                If _alpha <= 0.0F Then
                    _alpha = 0.0F
                    _layer = _pending
                    _hover = Nothing
                    _pressed = Nothing
                    _dragSlider = 0
                    Dim callback As Action = _onSwitched
                    _onSwitched = Nothing
                    _phase = If(_layer = MenuLayer.None, FadePhase.Idle, FadePhase.FadingIn)
                    If callback IsNot Nothing Then callback.Invoke()
                End If
            Case FadePhase.FadingIn
                _alpha += CSng(dt / FadeSeconds)
                If _alpha >= 1.0F Then
                    _alpha = 1.0F
                    _phase = FadePhase.Idle
                End If
        End Select
    End Sub

    Private ReadOnly Property IsInteractive As Boolean
        Get
            Return _phase = FadePhase.Idle AndAlso _alpha >= 1.0F AndAlso _layer <> MenuLayer.None
        End Get
    End Property

    Private Function CurrentButtons() As List(Of PixelButtonDef)
        Select Case _layer
            Case MenuLayer.Main
                Return _mainButtons
            Case MenuLayer.Settings
                Return _settingsButtons
            Case MenuLayer.HowTo
                Return _howToButtons
            Case Else
                Return Nothing
        End Select
    End Function

    ' ==================================================
    ' MOUSE
    ' ==================================================
    Private Function ButtonAt(p As Point) As PixelButtonDef
        Dim list As List(Of PixelButtonDef) = CurrentButtons()
        If list Is Nothing Then Return Nothing
        Return list.FirstOrDefault(Function(b) b.Rect.Contains(p))
    End Function

    Private Function SliderAt(p As Point) As Integer
        If _layer <> MenuLayer.Settings Then Return 0
        If Rectangle.Inflate(_musicTrack, 8, 14).Contains(p) Then Return 1
        If Rectangle.Inflate(_sfxTrack, 8, 14).Contains(p) Then Return 2
        Return 0
    End Function

    Private Sub UpdateCursor(p As Point)
        Dim show As Boolean = False
        If IsInteractive Then
            show = (ButtonAt(p) IsNot Nothing) OrElse (SliderAt(p) > 0) OrElse (_dragSlider > 0)
        End If
        If show <> _handShown Then
            _handShown = show
            Me.Cursor = If(show, Cursors.Hand, Cursors.Default)
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If _dragSlider > 0 Then
            SetSliderFromMouse(e.X)
        End If
        _hover = If(IsInteractive, ButtonAt(e.Location), Nothing)
        UpdateCursor(e.Location)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If e.Button <> MouseButtons.Left OrElse Not IsInteractive Then Return

        Dim slider As Integer = SliderAt(e.Location)
        If slider > 0 Then
            _dragSlider = slider
            SetSliderFromMouse(e.X)
            Return
        End If
        _pressed = ButtonAt(e.Location)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        _dragSlider = 0
        Dim pressedButton As PixelButtonDef = _pressed
        _pressed = Nothing
        If pressedButton IsNot Nothing AndAlso IsInteractive AndAlso ButtonAt(e.Location) Is pressedButton Then
            HandleButton(pressedButton.Id)
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hover = Nothing
        _pressed = Nothing
    End Sub

    Private Sub SetSliderFromMouse(mouseX As Integer)
        Dim track As Rectangle = If(_dragSlider = 1, _musicTrack, _sfxTrack)
        If track.Width <= 0 Then Return
        Dim value As Integer = CInt(Math.Round((mouseX - track.X) / CDbl(track.Width) * 100.0))
        value = Math.Max(0, Math.Min(100, value))

        If _dragSlider = 1 Then
            GameSettings.GetInstance().MusicVolume = value
            AudioManager.ApplyMusicVolume()      ' music volume changes live
        Else
            GameSettings.GetInstance().SfxVolume = value
        End If
    End Sub

    Private Sub HandleButton(id As String)
        AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
        Select Case id
            Case "start"
                RaiseEvent StartClicked()
            Case "settings"
                GoToLayer(MenuLayer.Settings)
            Case "howto"
                GoToLayer(MenuLayer.HowTo)
            Case "next"
                RaiseEvent NextGameClicked()
            Case "exit"
                RaiseEvent ExitClicked()
            Case "back"
                GoToLayer(MenuLayer.Main)
        End Select
    End Sub

    ' ==================================================
    ' DRAWING
    ' ==================================================
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim w As Integer = Me.ClientSize.Width
        Dim h As Integer = Me.ClientSize.Height
        If w <= 0 OrElse h <= 0 Then Return

        g.SmoothingMode = SmoothingMode.None
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = PixelOffsetMode.Half

        ' Background: never fades.
        _backdrop.Draw(g)
        Using shade As New SolidBrush(Color.FromArgb(90, 10, 10, 20))
            g.FillRectangle(shade, 0, 0, w, h)
        End Using

        If _layer = MenuLayer.None OrElse _alpha <= 0.0F Then Return

        If _alpha >= 0.999F Then
            DrawLayer(g)
            Return
        End If

        ' Mid-fade: draw the whole layer once at full opacity, then blend it as one group.
        If _buffer Is Nothing OrElse _buffer.Width <> w OrElse _buffer.Height <> h Then
            If _buffer IsNot Nothing Then _buffer.Dispose()
            _buffer = New Bitmap(w, h, PixelFormat.Format32bppArgb)
            ' Match the screen's DPI so text measures and sizes the same during a fade as at rest.
            _buffer.SetResolution(g.DpiX, g.DpiY)
        End If
        Using bg As Graphics = Graphics.FromImage(_buffer)
            bg.Clear(Color.Transparent)
            bg.SmoothingMode = SmoothingMode.None
            DrawLayer(bg)
        End Using

        Dim matrix As New ColorMatrix()
        matrix.Matrix33 = _alpha
        Using attributes As New ImageAttributes()
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap)
            g.DrawImage(_buffer, New Rectangle(0, 0, w, h), 0, 0, w, h, GraphicsUnit.Pixel, attributes)
        End Using
    End Sub

    Private Sub DrawLayer(g As Graphics)
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit
        Select Case _layer
            Case MenuLayer.Main
                DrawMainLayer(g)
            Case MenuLayer.Settings
                DrawSettingsLayer(g)
            Case MenuLayer.HowTo
                DrawHowToLayer(g)
        End Select
    End Sub

    Private Sub DrawMainLayer(g As Graphics)
        Dim titleRect As New RectangleF(0, Math.Max(24, _mainButtons(0).Rect.Y - 120), Me.ClientSize.Width, 70)
        DrawOutlinedText(g, "RIVER CROSSING", _titleFont, Color.White, Color.FromArgb(20, 20, 30), titleRect, _centerFormat, 3)

        For Each btn In _mainButtons
            DrawPixelButton(g, btn, If(btn.Id = "howto", _captionFont, _buttonFont), btn Is _hover, btn Is _pressed)
        Next
    End Sub

    ' Shared green/blue frame for Settings and How To Play.
    Private Sub DrawNaturePanelFrame(g As Graphics, area As Rectangle, title As String)
        Using bodyBrush As New SolidBrush(Color.FromArgb(234, 245, 224))
            g.FillRectangle(bodyBrush, area)
        End Using
        Using headerBrush As New SolidBrush(Color.FromArgb(42, 104, 62))
            g.FillRectangle(headerBrush, New Rectangle(area.X, area.Y, area.Width, 70))
        End Using
        Using waterBrush As New SolidBrush(Color.FromArgb(70, 150, 205))
            g.FillRectangle(waterBrush, New Rectangle(area.X, area.Y + 70, area.Width, 6))
        End Using
        Using titleBrush As New SolidBrush(Color.FromArgb(240, 250, 225))
            g.DrawString(title, _headerFont, titleBrush, New RectangleF(area.X, area.Y, area.Width, 70), _centerFormat)
        End Using
        Using outlinePen As New Pen(Color.FromArgb(24, 66, 40), 3)
            g.DrawRectangle(outlinePen, area.X + 1, area.Y + 1, area.Width - 3, area.Height - 3)
        End Using
    End Sub

    ' ---------- Settings ----------
    Private Sub DrawSettingsLayer(g As Graphics)
        Dim p As Rectangle = _settingsPanel
        DrawNaturePanelFrame(g, p, "SETTINGS")

        Dim musicAccent As Color = Color.FromArgb(70, 150, 205)   ' river blue
        Dim sfxAccent As Color = Color.FromArgb(76, 160, 90)      ' leaf green

        Using musicBrush As New SolidBrush(Color.FromArgb(35, 105, 165))
            g.DrawString("BACKGROUND MUSIC VOLUME", _captionFont, musicBrush, p.X + 44, p.Y + 92)
        End Using
        Using sfxBrush As New SolidBrush(Color.FromArgb(36, 110, 60))
            g.DrawString("SOUND EFFECTS VOLUME", _captionFont, sfxBrush, p.X + 44, p.Y + 198)
        End Using

        Dim musicValue As Integer = GameSettings.GetInstance().MusicVolume
        Dim sfxValue As Integer = GameSettings.GetInstance().SfxVolume
        DrawNatureSlider(g, _musicTrack, musicValue, musicAccent)
        DrawNatureSlider(g, _sfxTrack, sfxValue, sfxAccent)

        Using valueBrush As New SolidBrush(Color.FromArgb(38, 62, 46))
            g.DrawString(musicValue & "%", _captionFont, valueBrush, p.X + 44, _musicTrack.Bottom + 10)
            g.DrawString(sfxValue & "%", _captionFont, valueBrush, p.X + 44, _sfxTrack.Bottom + 10)
        End Using

        DrawPixelButton(g, _settingsButtons(0), _buttonFont, _settingsButtons(0) Is _hover, _settingsButtons(0) Is _pressed)
    End Sub

    Private Sub DrawNatureSlider(g As Graphics, track As Rectangle, value As Integer, fillColor As Color)
        Dim fillWidth As Integer = CInt(track.Width * value / 100.0)

        Using trackBrush As New SolidBrush(Color.FromArgb(200, 222, 200))
            g.FillRectangle(trackBrush, track)
        End Using
        If fillWidth > 0 Then
            Using fillBrush As New SolidBrush(fillColor)
                g.FillRectangle(fillBrush, track.X, track.Y, fillWidth, track.Height)
            End Using
        End If
        Using outlinePen As New Pen(Color.FromArgb(24, 66, 40), 2)
            g.DrawRectangle(outlinePen, track.X, track.Y, track.Width, track.Height)
        End Using

        Dim thumb As New Rectangle(track.X + fillWidth - 7, track.Y - 6, 14, track.Height + 12)
        Using thumbBrush As New SolidBrush(Color.FromArgb(24, 66, 40))
            g.FillRectangle(thumbBrush, thumb)
        End Using
        Using lightBrush As New SolidBrush(Color.FromArgb(234, 245, 224))
            g.FillRectangle(lightBrush, thumb.X + 3, thumb.Y + 3, thumb.Width - 6, thumb.Height - 6)
        End Using
    End Sub

    ' ---------- How to play ----------
    ' Total height of every section's heading + wrapped text at the given font and width.
    Private Function MeasureHowToContent(g As Graphics, font As Font, width As Single) As Single
        Dim total As Single = 0
        For Each section In _howToSections
            Dim size As SizeF = g.MeasureString(section.Body, font, CInt(width))
            total += 22 + size.Height + 12
        Next
        Return total
    End Function

    Private Sub DrawHowToLayer(g As Graphics)
        Dim panelW As Integer = _howToPanel.Width
        Dim bodyWidth As Single = panelW - HowToSidePad * 2
        Dim available As Integer = Me.ClientSize.Height - 16

        ' Measure with the real Graphics so the panel is exactly tall enough on any display scaling.
        ' If the window is too short for the normal text size, fall back to the smaller font.
        Dim bodyFont As Font = _bodyFont
        Dim contentH As Single = MeasureHowToContent(g, bodyFont, bodyWidth)
        If contentH + HowToChrome > available Then
            bodyFont = _bodySmallFont
            contentH = MeasureHowToContent(g, bodyFont, bodyWidth)
        End If

        Dim panelH As Integer = Math.Min(available, CInt(Math.Ceiling(contentH)) + HowToChrome)
        Dim panelTop As Integer = Math.Max(8, (Me.ClientSize.Height - panelH) \ 2)
        _howToPanel = New Rectangle(_howToPanel.X, panelTop, panelW, panelH)
        Dim p As Rectangle = _howToPanel

        DrawNaturePanelFrame(g, p, "HOW TO PLAY")

        Dim x As Single = p.X + HowToSidePad
        Dim y As Single = p.Y + 94
        For Each section In _howToSections
            DrawNatureSection(g, section, bodyFont, x, y, bodyWidth)
        Next

        ' The button always sits below the measured text, never over it.
        _howToButtons(0).Rect = New Rectangle(p.X + (panelW - 200) \ 2, p.Bottom - 70, 200, 46)
        DrawPixelButton(g, _howToButtons(0), _buttonFont, _howToButtons(0) Is _hover, _howToButtons(0) Is _pressed)
    End Sub

    Private Sub DrawNatureSection(g As Graphics, section As HowToSection, font As Font,
                                  x As Single, ByRef y As Single, width As Single)
        Using accentBrush As New SolidBrush(section.Accent)
            g.FillRectangle(accentBrush, x, y + 4, 12, 12)
            g.DrawString(section.Heading, _captionFont, accentBrush, x + 20, y)
        End Using
        y += 22
        Dim size As SizeF = g.MeasureString(section.Body, font, CInt(width))
        Using bodyBrush As New SolidBrush(Color.FromArgb(38, 62, 46))
            g.DrawString(section.Body, font, bodyBrush, New RectangleF(x, y, width, size.Height + 4))
        End Using
        y += size.Height + 12
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If _timer IsNot Nothing Then
                _timer.Stop()
                _timer.Dispose()
                _timer = Nothing
            End If
            If _buffer IsNot Nothing Then _buffer.Dispose()
            _backdrop.Dispose()
            _titleFont.Dispose()
            _buttonFont.Dispose()
            _headerFont.Dispose()
            _captionFont.Dispose()
            _bodyFont.Dispose()
            _bodySmallFont.Dispose()
            _centerFormat.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
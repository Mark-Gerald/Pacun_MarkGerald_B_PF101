Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Windows.Forms

''' <summary>Describes one main-menu button so the scene can draw a fading copy of it during transitions.</summary>
Public Class MenuButtonSpec
    Public Bounds As Rectangle
    Public ReadOnly Caption As String
    Public ReadOnly Fill As Color

    Public Sub New(captionText As String, fillColor As Color)
        Caption = captionText
        Fill = fillColor
    End Sub
End Class

''' <summary>The full-screen pages that can fade in over the menu.</summary>
Public Enum MenuPage
    None
    Settings
    Tutorial
End Enum

''' <summary>
''' The whole Level 2 menu scene, drawn in ONE panel with ONE timer:
'''   * scrolling castle wall, decorative platforms and flying bats (background, no collisions)
'''   * main menu look (knight + title; the real PixelButtons are child controls of this panel)
'''   * themed in-window SETTINGS and HOW TO PLAY pages (custom drawn) with a BACK button
'''   * fade transitions between the two (menu buttons are replaced by a fading copy while it runs)
''' </summary>
Public Class Level2ScenePanel
    Inherits System.Windows.Forms.Panel

    Private Enum SceneMode
        Menu
        ToPage
        Page
        ToMenu
    End Enum

    Private Class MenuBat
        Public X As Single
        Public BaseY As Single
        Public Speed As Single
        Public Dir As Single
        Public SizePx As Integer
        Public Phase As Single
        Public Wobble As Single
    End Class

    Private Class MenuPlatform
        Public Kind As Integer          ' 0 stone, 1 moving (tinted), 2 ice, 3 wood
        Public XFrac As Single
        Public YFrac As Single
        Public LeftLane As Boolean
    End Class

    Private Class SettingsLayout
        Public Card As Rectangle
        Public HeadingY As Integer
        Public DividerY As Integer
        Public MusicLabelY As Integer
        Public MusicTrack As Rectangle
        Public SfxLabelY As Integer
        Public SfxTrack As Rectangle
        Public Back As Rectangle
    End Class

    ''' <summary>Raised when a fade has completely finished (True = a page is now showing, False = the menu).</summary>
    Public Event TransitionFinished(toPage As Boolean)

    ''' <summary>Raised when the player clicks BACK on a page.</summary>
    Public Event BackRequested()

    Private Const ScrollSpeed As Double = 120.0
    Private Const TransitionSeconds As Single = 0.3F

    Private ReadOnly animTimer As System.Windows.Forms.Timer
    Private ReadOnly art As JumpKnightAssets
    Private ReadOnly watch As New System.Diagnostics.Stopwatch()
    Private lastTime As Double = 0.0
    Private scrollOffset As Double = 0.0
    Private clockSeconds As Single = 0.0F
    Private stopped As Boolean = False

    Private mode As SceneMode = SceneMode.Menu
    Private page As MenuPage = MenuPage.None
    Private transitionT As Single = 0.0F
    Private menuLayer As Bitmap = Nothing

    Private dragging As Integer = -1
    Private hoverBack As Boolean = False

    Private decorReady As Boolean = False
    Private ReadOnly bats As New List(Of MenuBat)
    Private ReadOnly decorPlatforms As New List(Of MenuPlatform)
    Private ReadOnly rng As New Random()

    Private ReadOnly titleFont As New Font("Segoe UI", 34.0F, FontStyle.Bold)
    Private ReadOnly headingFont As New Font("Segoe UI", 30.0F, FontStyle.Bold)
    Private ReadOnly labelFont As New Font("Segoe UI", 11.0F, FontStyle.Bold)
    Private ReadOnly pctFont As New Font("Segoe UI", 14.0F, FontStyle.Bold)
    Private ReadOnly buttonFont As New Font("Segoe UI", 11.0F, FontStyle.Bold)
    Private ReadOnly hintFont As New Font("Segoe UI", 9.0F, FontStyle.Regular)
    Private ReadOnly bodyFont As New Font("Segoe UI", 10.0F, FontStyle.Regular)
    Private ReadOnly keyFont As New Font("Segoe UI", 10.0F, FontStyle.Bold)
    Private ReadOnly overlayBrush As New SolidBrush(Color.FromArgb(120, 8, 6, 14))
    Private ReadOnly centerFormat As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
    Private ReadOnly centerTopFormat As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near}
    Private ReadOnly rightFormat As New StringFormat() With {.Alignment = StringAlignment.Far, .LineAlignment = StringAlignment.Near}

    Private Shared ReadOnly MusicAccent As Color = Color.FromArgb(76, 175, 80)     ' same green as START
    Private Shared ReadOnly SfxAccent As Color = Color.FromArgb(66, 133, 200)      ' same blue as SETTINGS
    Private Shared ReadOnly BackColorRed As Color = Color.FromArgb(200, 70, 60)    ' same red as EXIT

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        art = New JumpKnightAssets()

        watch.Start()
        animTimer = New System.Windows.Forms.Timer() With {.Interval = 30}
        AddHandler animTimer.Tick, AddressOf AnimTimer_Tick
        animTimer.Start()
    End Sub

    ' ===================== Public API (used by Level2MenuForm) =====================

    ''' <summary>Fades the menu out and a page (Settings / Tutorial) in. The caller hides the real buttons first.</summary>
    Public Sub BeginPageTransition(target As MenuPage, specs As List(Of MenuButtonSpec))
        If stopped OrElse mode <> SceneMode.Menu OrElse target = MenuPage.None Then Return
        BuildMenuLayer(specs)
        dragging = -1
        hoverBack = False
        transitionT = 0.0F
        page = target
        mode = SceneMode.ToPage
    End Sub

    ''' <summary>Fades the current page out and the menu back in.</summary>
    Public Sub BeginMenuTransition(specs As List(Of MenuButtonSpec))
        If stopped OrElse mode <> SceneMode.Page Then Return
        BuildMenuLayer(specs)
        dragging = -1
        hoverBack = False
        transitionT = 0.0F
        mode = SceneMode.ToMenu
    End Sub

    Public Sub StopAnimation()
        If stopped Then Return
        stopped = True
        animTimer.Stop()
        RemoveHandler animTimer.Tick, AddressOf AnimTimer_Tick
        animTimer.Dispose()
        FreeMenuLayer()
        art.Dispose()
        titleFont.Dispose() : headingFont.Dispose() : labelFont.Dispose()
        pctFont.Dispose() : buttonFont.Dispose() : hintFont.Dispose()
        bodyFont.Dispose() : keyFont.Dispose()
        overlayBrush.Dispose()
        centerFormat.Dispose() : centerTopFormat.Dispose() : rightFormat.Dispose()
    End Sub

    ' ===================== Timer =====================

    Private Sub AnimTimer_Tick(sender As Object, e As EventArgs)
        If stopped Then Return
        Dim nowSeconds As Double = watch.Elapsed.TotalSeconds
        Dim dt As Single = CSng(Math.Min(0.1, nowSeconds - lastTime))
        lastTime = nowSeconds

        scrollOffset += ScrollSpeed * dt
        clockSeconds += dt
        UpdateDecor(dt)

        If mode = SceneMode.ToPage OrElse mode = SceneMode.ToMenu Then
            transitionT = Math.Min(1.0F, transitionT + dt / TransitionSeconds)
            If transitionT >= 1.0F Then
                Dim wentToPage As Boolean = (mode = SceneMode.ToPage)
                mode = If(wentToPage, SceneMode.Page, SceneMode.Menu)
                If Not wentToPage Then
                    FreeMenuLayer()
                    page = MenuPage.None
                End If
                RaiseEvent TransitionFinished(wentToPage)
            End If
        End If

        Me.Invalidate()
    End Sub

    ''' <summary>1 = menu fully visible, 0 = menu fully hidden (eased).</summary>
    Private Function MenuAlpha() As Single
        Dim e As Single = transitionT * transitionT * (3.0F - 2.0F * transitionT)
        Select Case mode
            Case SceneMode.Menu
                Return 1.0F
            Case SceneMode.ToPage
                Return 1.0F - e
            Case SceneMode.ToMenu
                Return e
            Case Else
                Return 0.0F
        End Select
    End Function

    ' ===================== Painting =====================

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        If stopped Then Return
        Dim g As Graphics = e.Graphics
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        If w <= 0 OrElse h <= 0 Then Return
        If Not decorReady Then InitDecor()

        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

        ' --- Castle wall ---
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
        g.FillRectangle(overlayBrush, 0, 0, w, h)

        ' --- Decorations (behind everything else) ---
        DrawDecorPlatforms(g, w, h)
        DrawBats(g)

        ' --- Menu look (the real buttons are child controls drawn on top of this) ---
        Dim menuA As Single = MenuAlpha()
        If menuA > 0.003F Then
            DrawKnight(g, w, h, menuA)
            If mode <> SceneMode.Menu AndAlso menuLayer IsNot Nothing Then DrawLayer(g, menuLayer, menuA)
            DrawTitle(g, w, menuA)
        End If

        ' --- Settings look ---
        Dim setA As Single = 1.0F - menuA
        If setA > 0.003F Then
            If page = MenuPage.Tutorial Then
                DrawTutorial(g, w, h, setA)
            Else
                DrawSettings(g, w, h, setA)
            End If
        End If

        MyBase.OnPaint(e)
    End Sub

    Private Shared Function Al(c As Color, a As Single) As Color
        Dim v As Single = Math.Max(0.0F, Math.Min(1.0F, a))
        Return Color.FromArgb(CInt(c.A * v), c.R, c.G, c.B)
    End Function

    Private Shared Function AlphaAttributes(alpha As Single) As ImageAttributes
        Dim cm As New ColorMatrix()
        cm.Matrix33 = Math.Max(0.0F, Math.Min(1.0F, alpha))
        Dim ia As New ImageAttributes()
        ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap)
        ia.SetWrapMode(Drawing2D.WrapMode.TileFlipXY)
        Return ia
    End Function

    Private Sub DrawKnight(g As Graphics, w As Integer, h As Integer, a As Single)
        If art.KnightIdle Is Nothing OrElse h < 560 Then Return
        Dim bigScale As Integer = 4
        Dim frame As Integer = CInt(Math.Floor(clockSeconds * 8.0F)) Mod JumpKnightAssets.IdleFrames
        Dim src As New Rectangle(frame * JumpKnightAssets.KnightFrameW, 0, JumpKnightAssets.KnightFrameW, JumpKnightAssets.KnightFrameH)
        Dim dest As New Rectangle((w - src.Width * bigScale) \ 2, 100, src.Width * bigScale, src.Height * bigScale)
        Using ia As ImageAttributes = AlphaAttributes(a)
            g.DrawImage(art.KnightIdle, dest, src.X, src.Y, src.Width, src.Height, GraphicsUnit.Pixel, ia)
        End Using
    End Sub

    Private Sub DrawTitle(g As Graphics, w As Integer, a As Single)
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Dim titleText As String = "JUMP KNIGHT"
        Dim titleSize As SizeF = g.MeasureString(titleText, titleFont)
        Dim titleX As Single = (w - titleSize.Width) / 2.0F
        Dim titleY As Single = 30
        Using shadowBrush As New SolidBrush(Al(Color.FromArgb(160, 0, 0, 0), a))
            g.DrawString(titleText, titleFont, shadowBrush, titleX + 3, titleY + 3)
        End Using
        Using textBrush As New SolidBrush(Al(Color.White, a))
            g.DrawString(titleText, titleFont, textBrush, titleX, titleY)
        End Using
        g.SmoothingMode = Drawing2D.SmoothingMode.None
    End Sub

    Private Sub DrawLayer(g As Graphics, bmp As Bitmap, a As Single)
        Using ia As ImageAttributes = AlphaAttributes(a)
            g.DrawImage(bmp, New Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia)
        End Using
    End Sub

    ''' <summary>Same look as PixelButton (flat fill, dark outline, bottom shadow edge), with an alpha.</summary>
    Private Sub DrawPixelButton(g As Graphics, r As Rectangle, caption As String, fill As Color, hover As Boolean, a As Single)
        Dim body As Color = If(hover, ControlPaint.Light(fill, 0.15F), fill)
        Dim bodyH As Integer = r.Height - 5

        Using sb As New SolidBrush(Al(ControlPaint.Dark(fill, 0.4F), a))
            g.FillRectangle(sb, r.X, r.Y + r.Height - 5, r.Width - 1, 4)
        End Using
        Using fb As New SolidBrush(Al(body, a))
            g.FillRectangle(fb, r.X, r.Y, r.Width - 1, bodyH)
        End Using
        Using pen As New Pen(Al(Color.FromArgb(30, 20, 10), a), 2.0F)
            g.DrawRectangle(pen, r.X + 1, r.Y + 1, r.Width - 3, bodyH - 2)
        End Using

        Dim textRect As New RectangleF(r.X, r.Y, r.Width, bodyH)
        Using tsb As New SolidBrush(Al(Color.FromArgb(140, 0, 0, 0), a))
            g.DrawString(caption, buttonFont, tsb, New RectangleF(textRect.X + 1, textRect.Y + 1, textRect.Width, textRect.Height), centerFormat)
        End Using
        Using tb As New SolidBrush(Al(Color.White, a))
            g.DrawString(caption, buttonFont, tb, textRect, centerFormat)
        End Using
    End Sub

    ' ===================== Menu layer (fading copy of the real buttons) =====================

    Private Sub BuildMenuLayer(specs As List(Of MenuButtonSpec))
        FreeMenuLayer()
        If Width <= 0 OrElse Height <= 0 OrElse specs Is Nothing Then Return
        menuLayer = New Bitmap(Width, Height, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(menuLayer)
            g.SmoothingMode = Drawing2D.SmoothingMode.None
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit
            For Each s As MenuButtonSpec In specs
                DrawPixelButton(g, s.Bounds, s.Caption, s.Fill, False, 1.0F)
            Next
        End Using
    End Sub

    Private Sub FreeMenuLayer()
        If menuLayer IsNot Nothing Then
            menuLayer.Dispose()
            menuLayer = Nothing
        End If
    End Sub

    ' ===================== Settings screen =====================

    Private Function GetLayout() As SettingsLayout
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        Dim cw As Integer = Math.Max(320, Math.Min(560, w - 60))
        Dim ch As Integer = 380
        Dim cx As Integer = (w - cw) \ 2
        Dim cy As Integer = Math.Max(10, (h - ch) \ 2)
        Dim pad As Integer = 40

        Dim L As New SettingsLayout()
        L.Card = New Rectangle(cx, cy, cw, ch)
        L.HeadingY = cy + 22
        L.DividerY = cy + 84
        L.MusicLabelY = cy + 108
        L.MusicTrack = New Rectangle(cx + pad, cy + 140, cw - pad * 2, 18)
        L.SfxLabelY = cy + 198
        L.SfxTrack = New Rectangle(cx + pad, cy + 230, cw - pad * 2, 18)
        L.Back = New Rectangle(cx + (cw - 220) \ 2, cy + ch - 52 - 30, 220, 52)
        Return L
    End Function

    Private Sub DrawSettings(g As Graphics, w As Integer, h As Integer, a As Single)
        Dim L As SettingsLayout = GetLayout()
        Dim c As Rectangle = L.Card
        Dim gs As GameSettings = GameSettings.GetInstance()

        ' --- Card: shadow, dark fill, thick castle outline, inner line, gold corner studs ---
        Using sb As New SolidBrush(Al(Color.FromArgb(120, 0, 0, 0), a))
            g.FillRectangle(sb, c.X + 6, c.Y + 7, c.Width, c.Height)
        End Using
        Using fb As New SolidBrush(Al(Color.FromArgb(228, 18, 13, 28), a))
            g.FillRectangle(fb, c)
        End Using
        Using pen As New Pen(Al(Color.FromArgb(30, 20, 10), a), 4.0F)
            g.DrawRectangle(pen, c.X + 2, c.Y + 2, c.Width - 4, c.Height - 4)
        End Using
        Using pen As New Pen(Al(Color.FromArgb(110, 86, 150), a), 1.0F)
            g.DrawRectangle(pen, c.X + 7, c.Y + 7, c.Width - 15, c.Height - 15)
        End Using
        Using gold As New SolidBrush(Al(Color.FromArgb(235, 195, 95), a))
            g.FillRectangle(gold, c.X + 3, c.Y + 3, 6, 6)
            g.FillRectangle(gold, c.Right - 9, c.Y + 3, 6, 6)
            g.FillRectangle(gold, c.X + 3, c.Bottom - 9, 6, 6)
            g.FillRectangle(gold, c.Right - 9, c.Bottom - 9, 6, 6)
        End Using

        ' --- Heading (same style as the main title) ---
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Dim headRect As New RectangleF(c.X, L.HeadingY, c.Width, 52)
        Using sh As New SolidBrush(Al(Color.FromArgb(160, 0, 0, 0), a))
            g.DrawString("SETTINGS", headingFont, sh, New RectangleF(headRect.X + 3, headRect.Y + 3, headRect.Width, headRect.Height), centerTopFormat)
        End Using
        Using tb As New SolidBrush(Al(Color.White, a))
            g.DrawString("SETTINGS", headingFont, tb, headRect, centerTopFormat)
        End Using
        g.SmoothingMode = Drawing2D.SmoothingMode.None

        ' --- Divider ---
        Using dv As New SolidBrush(Al(Color.FromArgb(90, 70, 120), a))
            g.FillRectangle(dv, c.X + 40, L.DividerY, c.Width - 80, 2)
        End Using
        Using gold As New SolidBrush(Al(Color.FromArgb(235, 195, 95), a))
            g.FillRectangle(gold, c.X + c.Width \ 2 - 4, L.DividerY - 3, 8, 8)
        End Using

        ' --- Sliders ---
        DrawSlider(g, "BACKGROUND MUSIC", L.MusicTrack, L.MusicLabelY, gs.MusicVolume, MusicAccent, dragging = 0, a)
        DrawSlider(g, "SOUND EFFECTS", L.SfxTrack, L.SfxLabelY, gs.SfxVolume, SfxAccent, dragging = 1, a)

        ' --- Back button (same pixel button look as the menu) ---
        DrawPixelButton(g, L.Back, "BACK", BackColorRed, hoverBack, a)

        Using hb As New SolidBrush(Al(Color.FromArgb(150, 140, 170), a))
            g.DrawString("Esc also goes back", hintFont, hb, New RectangleF(c.X, L.Back.Bottom + 6, c.Width, 16), centerTopFormat)
        End Using
    End Sub

    Private Sub DrawSlider(g As Graphics, caption As String, track As Rectangle, labelY As Integer,
                           value As Integer, accent As Color, active As Boolean, a As Single)
        ' Caption (left) and percentage (right)
        Using lb As New SolidBrush(Al(Color.FromArgb(225, 225, 235), a))
            g.DrawString(caption, labelFont, lb, track.X, labelY)
        End Using
        Using pb As New SolidBrush(Al(Color.FromArgb(255, 220, 110), a))
            g.DrawString(value.ToString() & "%", pctFont, pb, New RectangleF(track.X, labelY - 4, track.Width, 28), rightFormat)
        End Using

        Dim innerW As Integer = Math.Max(1, track.Width - 6)
        Dim fillW As Integer = CInt(innerW * value / 100.0)

        ' Track: dark frame, inner groove, coloured fill with a lighter top edge
        Using fb As New SolidBrush(Al(Color.FromArgb(12, 8, 18), a))
            g.FillRectangle(fb, track)
        End Using
        Using pen As New Pen(Al(Color.FromArgb(30, 20, 10), a), 2.0F)
            g.DrawRectangle(pen, track.X + 1, track.Y + 1, track.Width - 3, track.Height - 3)
        End Using
        Using gb As New SolidBrush(Al(Color.FromArgb(42, 36, 56), a))
            g.FillRectangle(gb, track.X + 3, track.Y + 3, innerW, track.Height - 6)
        End Using
        If fillW > 0 Then
            Using ab As New SolidBrush(Al(accent, a))
                g.FillRectangle(ab, track.X + 3, track.Y + 3, fillW, track.Height - 6)
            End Using
            Using hb As New SolidBrush(Al(ControlPaint.Light(accent, 0.45F), a))
                g.FillRectangle(hb, track.X + 3, track.Y + 3, fillW, 3)
            End Using
        End If

        ' Tick marks every 10%
        Using tk As New SolidBrush(Al(Color.FromArgb(130, 118, 160), a))
            For i As Integer = 0 To 10
                Dim tx As Integer = track.X + CInt(track.Width * i / 10.0)
                g.FillRectangle(tk, tx - 1, track.Bottom + 5, 2, If(i Mod 5 = 0, 8, 5))
            Next
        End Using

        ' Knob
        Dim kx As Integer = track.X + 3 + fillW
        Dim ky As Integer = track.Y + track.Height \ 2 - 15
        Using ks As New SolidBrush(Al(Color.FromArgb(0, 0, 0), a * 0.5F))
            g.FillRectangle(ks, kx - 8, ky + 4, 16, 30)
        End Using
        Using kb As New SolidBrush(Al(If(active, Color.White, Color.FromArgb(228, 228, 238)), a))
            g.FillRectangle(kb, kx - 8, ky, 16, 30)
        End Using
        Using kp As New Pen(Al(Color.FromArgb(30, 20, 10), a), 2.0F)
            g.DrawRectangle(kp, kx - 7, ky + 1, 14, 28)
        End Using
        Using st As New SolidBrush(Al(accent, a))
            g.FillRectangle(st, kx - 2, ky + 6, 4, 18)
        End Using
    End Sub

    ' ===================== How to play (tutorial) page =====================

    Private Class TutorialLayout
        Public Card As Rectangle
        Public HeadingY As Integer
        Public DividerY As Integer
        Public RowsTop As Integer
        Public Back As Rectangle
    End Class

    Private Function GetTutorialLayout() As TutorialLayout
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        Dim cw As Integer = Math.Max(440, Math.Min(740, w - 60))
        Dim ch As Integer = 550
        Dim cx As Integer = (w - cw) \ 2
        Dim cy As Integer = Math.Max(10, (h - ch) \ 2)

        Dim L As New TutorialLayout()
        L.Card = New Rectangle(cx, cy, cw, ch)
        L.HeadingY = cy + 18
        L.DividerY = cy + 76
        L.RowsTop = cy + 92
        L.Back = New Rectangle(cx + (cw - 220) \ 2, cy + ch - 52 - 18, 220, 52)
        Return L
    End Function

    Private Shared ReadOnly TutorialKeys() As String = {"GOAL", "MOVE", "ATTACK", "DOUBLE JUMP", "SUPER JUMP", "PLATFORMS", "PAUSE"}
    Private Shared ReadOnly TutorialText() As String = {
        "Climb the castle tower as high as you can. The knight jumps by himself. Do not fall, and avoid the bats!",
        "LEFT / RIGHT arrows or A / D steer the knight. Leave one side of the screen to appear on the other.",
        "SPACE swings your sword in front of you. Hit a bat for +25 points. Touching a bat ends the run.",
        "Grab the MEAT for 5 seconds of double jump. Press UP or W while in the air.",
        "Touch the HAMMER for a giant spring launch. Each power-up can be used only once.",
        "Stone is safe. Blue-tinted platforms move. Wood breaks after one landing. Ice is slippery.",
        "P, Esc or the pause button pauses the game. RESUME gives you a 3-2-1 countdown."}

    Private Sub DrawTutorial(g As Graphics, w As Integer, h As Integer, a As Single)
        Dim L As TutorialLayout = GetTutorialLayout()
        Dim c As Rectangle = L.Card

        DrawCard(g, c, a)

        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Dim headRect As New RectangleF(c.X, L.HeadingY, c.Width, 52)
        Using sh As New SolidBrush(Al(Color.FromArgb(160, 0, 0, 0), a))
            g.DrawString("HOW TO PLAY", headingFont, sh, New RectangleF(headRect.X + 3, headRect.Y + 3, headRect.Width, headRect.Height), centerTopFormat)
        End Using
        Using tb As New SolidBrush(Al(Color.White, a))
            g.DrawString("HOW TO PLAY", headingFont, tb, headRect, centerTopFormat)
        End Using
        g.SmoothingMode = Drawing2D.SmoothingMode.None

        Using dv As New SolidBrush(Al(Color.FromArgb(90, 70, 120), a))
            g.FillRectangle(dv, c.X + 40, L.DividerY, c.Width - 80, 2)
        End Using
        Using gold As New SolidBrush(Al(Color.FromArgb(235, 195, 95), a))
            g.FillRectangle(gold, c.X + c.Width \ 2 - 4, L.DividerY - 3, 8, 8)
        End Using

        Dim labelW As Integer = 150
        Dim textX As Integer = c.X + 36 + labelW
        Dim textW As Integer = c.Width - 72 - labelW
        Dim rowH As Integer = 50
        Using kb As New SolidBrush(Al(Color.FromArgb(255, 220, 110), a))
            Using bb As New SolidBrush(Al(Color.FromArgb(225, 225, 235), a))
                For i As Integer = 0 To TutorialKeys.Length - 1
                    Dim y As Integer = L.RowsTop + i * rowH
                    g.DrawString(TutorialKeys(i), keyFont, kb, c.X + 36, y)
                    g.DrawString(TutorialText(i), bodyFont, bb, New RectangleF(textX, y, textW, rowH))
                Next
            End Using
        End Using

        DrawPixelButton(g, L.Back, "BACK", BackColorRed, hoverBack, a)
    End Sub

    ''' <summary>Shared card look: shadow, dark fill, castle outline, inner line, gold corner studs.</summary>
    Private Sub DrawCard(g As Graphics, c As Rectangle, a As Single)
        Using sb As New SolidBrush(Al(Color.FromArgb(120, 0, 0, 0), a))
            g.FillRectangle(sb, c.X + 6, c.Y + 7, c.Width, c.Height)
        End Using
        Using fb As New SolidBrush(Al(Color.FromArgb(228, 18, 13, 28), a))
            g.FillRectangle(fb, c)
        End Using
        Using pen As New Pen(Al(Color.FromArgb(30, 20, 10), a), 4.0F)
            g.DrawRectangle(pen, c.X + 2, c.Y + 2, c.Width - 4, c.Height - 4)
        End Using
        Using pen As New Pen(Al(Color.FromArgb(110, 86, 150), a), 1.0F)
            g.DrawRectangle(pen, c.X + 7, c.Y + 7, c.Width - 15, c.Height - 15)
        End Using
        Using gold As New SolidBrush(Al(Color.FromArgb(235, 195, 95), a))
            g.FillRectangle(gold, c.X + 3, c.Y + 3, 6, 6)
            g.FillRectangle(gold, c.Right - 9, c.Y + 3, 6, 6)
            g.FillRectangle(gold, c.X + 3, c.Bottom - 9, 6, 6)
            g.FillRectangle(gold, c.Right - 9, c.Bottom - 9, 6, 6)
        End Using
    End Sub

    ' ===================== Settings mouse input =====================

    Private Shared Function SliderHit(track As Rectangle, p As Point) As Boolean
        Return New Rectangle(track.X - 12, track.Y - 14, track.Width + 24, track.Height + 28).Contains(p)
    End Function

    Private Sub SetSliderFromX(index As Integer, x As Integer, L As SettingsLayout)
        Dim track As Rectangle = If(index = 0, L.MusicTrack, L.SfxTrack)
        Dim innerW As Integer = Math.Max(1, track.Width - 6)
        Dim v As Integer = CInt(Math.Round((x - (track.X + 3)) * 100.0 / innerW))
        v = Math.Max(0, Math.Min(100, v))
        If index = 0 Then
            GameSettings.GetInstance().MusicVolume = v
            AudioManager.ApplyMusicVolume()
        Else
            GameSettings.GetInstance().SfxVolume = v
        End If
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If mode = SceneMode.Page AndAlso e.Button = MouseButtons.Left Then
            If page = MenuPage.Tutorial Then
                If GetTutorialLayout().Back.Contains(e.Location) Then RaiseEvent BackRequested()
                MyBase.OnMouseDown(e)
                Return
            End If
            Dim L As SettingsLayout = GetLayout()
            If L.Back.Contains(e.Location) Then
                RaiseEvent BackRequested()
            ElseIf SliderHit(L.MusicTrack, e.Location) Then
                dragging = 0
                SetSliderFromX(0, e.X, L)
            ElseIf SliderHit(L.SfxTrack, e.Location) Then
                dragging = 1
                SetSliderFromX(1, e.X, L)
            End If
        End If
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        If mode = SceneMode.Page AndAlso page = MenuPage.Tutorial Then
            hoverBack = GetTutorialLayout().Back.Contains(e.Location)
            Me.Cursor = If(hoverBack, Cursors.Hand, Cursors.Default)
        ElseIf mode = SceneMode.Page Then
            Dim L As SettingsLayout = GetLayout()
            If dragging >= 0 Then SetSliderFromX(dragging, e.X, L)
            hoverBack = L.Back.Contains(e.Location)
            Dim overSomething As Boolean = hoverBack OrElse dragging >= 0 OrElse
                SliderHit(L.MusicTrack, e.Location) OrElse SliderHit(L.SfxTrack, e.Location)
            Me.Cursor = If(overSomething, Cursors.Hand, Cursors.Default)
        End If
        MyBase.OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        dragging = -1
        MyBase.OnMouseUp(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        hoverBack = False
        Me.Cursor = Cursors.Default
        MyBase.OnMouseLeave(e)
    End Sub

    ' ===================== Decorations: platforms and bats =====================

    Private Sub InitDecor()
        If Width <= 0 OrElse Height <= 0 Then Return
        decorPlatforms.Clear()
        bats.Clear()

        ' Side lanes only (never behind the title, knight or buttons). They scroll down with the wall.
        Dim lY() As Single = {0.06F, 0.29F, 0.53F, 0.77F}
        Dim lX() As Single = {0.35F, 0.9F, 0.1F, 0.6F}
        Dim lK() As Integer = {0, 2, 0, 3}
        Dim rY() As Single = {0.17F, 0.4F, 0.63F, 0.88F}
        Dim rX() As Single = {0.75F, 0.2F, 0.85F, 0.4F}
        Dim rK() As Integer = {1, 0, 3, 0}
        For i As Integer = 0 To 3
            decorPlatforms.Add(New MenuPlatform With {.Kind = lK(i), .XFrac = lX(i), .YFrac = lY(i) + CSng(rng.NextDouble()) * 0.04F - 0.02F, .LeftLane = True})
            decorPlatforms.Add(New MenuPlatform With {.Kind = rK(i), .XFrac = rX(i), .YFrac = rY(i) + CSng(rng.NextDouble()) * 0.04F - 0.02F, .LeftLane = False})
        Next

        Dim sizes() As Integer = {24, 32, 40, 28, 36, 24}
        For i As Integer = 0 To sizes.Length - 1
            bats.Add(New MenuBat With {
                .SizePx = sizes(i),
                .Dir = If(i Mod 2 = 0, 1.0F, -1.0F),
                .Speed = 50.0F + (sizes(i) - 24) * 3.0F + CSng(rng.NextDouble()) * 40.0F,
                .X = CSng(rng.NextDouble()) * Width,
                .BaseY = Height * (0.08F + CSng(rng.NextDouble()) * 0.8F),
                .Phase = CSng(rng.NextDouble()) * 6.28F,
                .Wobble = 6.0F + CSng(rng.NextDouble()) * 12.0F})
        Next
        decorReady = True
    End Sub

    Private Sub UpdateDecor(dt As Single)
        If Not decorReady OrElse Width <= 0 Then Return
        For Each b As MenuBat In bats
            b.X += b.Dir * b.Speed * dt
            If b.Dir > 0.0F AndAlso b.X > Width + 50 Then
                b.X = -50
                b.BaseY = Height * (0.08F + CSng(rng.NextDouble()) * 0.8F)
                b.Speed = 50.0F + (b.SizePx - 24) * 3.0F + CSng(rng.NextDouble()) * 40.0F
            ElseIf b.Dir < 0.0F AndAlso b.X < -50 Then
                b.X = Width + 50
                b.BaseY = Height * (0.08F + CSng(rng.NextDouble()) * 0.8F)
                b.Speed = 50.0F + (b.SizePx - 24) * 3.0F + CSng(rng.NextDouble()) * 40.0F
            End If
        Next
    End Sub

    Private Function DecorSprite(kind As Integer) As Bitmap
        Select Case kind
            Case 0
                Return art.StoneTile
            Case 1
                Return art.MovingTile
            Case 2
                Return art.IceTile
            Case Else
                Return art.WoodLog
        End Select
    End Function

    Private Sub DrawDecorPlatforms(g As Graphics, w As Integer, h As Integer)
        Dim span As Double = h + 150
        Dim leftMax As Integer = Math.Max(12, w \ 2 - 215 - 64)
        Dim rightMin As Integer = Math.Min(w - 80, w \ 2 + 215)

        For Each p As MenuPlatform In decorPlatforms
            Dim bmp As Bitmap = DecorSprite(p.Kind)
            If bmp Is Nothing Then Continue For
            Dim sc As Integer = If(p.Kind = 3, 1, 2)
            Dim dw As Integer = bmp.Width * sc
            Dim dh As Integer = bmp.Height * sc

            Dim x As Integer
            If p.LeftLane Then
                x = 12 + CInt(p.XFrac * Math.Max(0, leftMax - 12))
            Else
                x = rightMin + CInt(p.XFrac * Math.Max(0, (w - dw - 12) - rightMin))
            End If

            Dim yv As Double = p.YFrac * span + scrollOffset
            yv = yv - Math.Floor(yv / span) * span
            g.DrawImage(bmp, x, CInt(yv) - 90, dw, dh)
        Next
    End Sub

    Private Sub DrawBats(g As Graphics)
        For Each b As MenuBat In bats
            Dim frame As Integer = CInt(Math.Floor(clockSeconds * 9.0F + b.Phase)) And 1
            Dim bmp As Bitmap = art.BatFrames(frame)
            If bmp Is Nothing Then Continue For
            Dim y As Single = b.BaseY + CSng(Math.Sin(clockSeconds * 2.0F + b.Phase)) * b.Wobble
            Dim alpha As Single = 0.55F + 0.45F * (b.SizePx - 24) / 16.0F
            Using ia As ImageAttributes = AlphaAttributes(alpha)
                g.DrawImage(bmp, New Rectangle(CInt(b.X - b.SizePx / 2), CInt(y - b.SizePx / 2), b.SizePx, b.SizePx),
                            0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia)
            End Using
        Next
    End Sub

End Class
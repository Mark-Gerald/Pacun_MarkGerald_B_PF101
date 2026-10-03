Option Strict On
Imports System
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Windows.Forms

''' <summary>
''' Draws Cannon Ball. It only READS the engine; it never changes game state (except the volume sliders,
''' which write to GameSettings). The fixed 400 x 640 logical view (40 HUD + 600 playfield) is scaled with
''' nearest-neighbour to fit the panel and centred, exactly like Jump Knight.
''' </summary>
Public Class CannonBallPanel
    Inherits System.Windows.Forms.Panel

    Private ReadOnly engine As CannonBallEngine
    Private ReadOnly art As CannonBallAssets

    ' ---- Set by the game form ----
    Public Property Paused As Boolean = False
    Public Property SettingsOpen As Boolean = False
    Public Property CountdownText As String = ""
    Public Property FadeAmount As Single = 0.0F          ' 0 = clear, 1 = fully black

    ' ---- Layout (view space: y includes the 40 unit HUD band) ----
    Private Shared ReadOnly MusicTrackRect As New Rectangle(60, 250, 280, 16)
    Private Shared ReadOnly SfxTrackRect As New Rectangle(60, 340, 280, 16)
    Private Const CartSpriteBottom As Single = CannonBallEngine.PaddleY + 18.0F
    Private dragSlider As Integer = -1

    ' ---- HUD pause button (upper-left, just before the CANNON BALLS info) ----
    Public Event PauseClicked()
    Private Shared ReadOnly PauseRect As New Rectangle(20, 6, 28, 28)
    Private pauseHover As Boolean = False
    Private ReadOnly pauseBody As New SolidBrush(Color.FromArgb(58, 58, 82))
    Private ReadOnly pauseBodyHover As New SolidBrush(Color.FromArgb(96, 96, 130))
    Private ReadOnly pauseShadow As New SolidBrush(Color.FromArgb(22, 22, 34))
    Private ReadOnly pauseOutline As New Pen(Color.FromArgb(30, 20, 10), 2.0F)

    ' ---- Cached GDI+ objects ----
    Private ReadOnly hudFont As New Font("Segoe UI", 18.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly smallFont As New Font("Segoe UI", 13.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly bigFont As New Font("Segoe UI", 40.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly countFont As New Font("Segoe UI", 120.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly capFont As New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly valFont As New Font("Segoe UI", 15.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly debugFont As New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Pixel)

    Private ReadOnly shadowBrush As New SolidBrush(Color.FromArgb(170, 0, 0, 0))
    Private ReadOnly whiteBrush As New SolidBrush(Color.White)
    Private ReadOnly goldBrush As New SolidBrush(Color.FromArgb(255, 230, 120))
    Private ReadOnly redBrush As New SolidBrush(Color.FromArgb(240, 96, 84))
    Private ReadOnly greenBrush As New SolidBrush(Color.FromArgb(120, 235, 110))
    Private ReadOnly captionBrush As New SolidBrush(Color.FromArgb(200, 168, 104))
    Private ReadOnly dimBrush As New SolidBrush(Color.FromArgb(165, 0, 0, 0))
    Private ReadOnly countDimBrush As New SolidBrush(Color.FromArgb(95, 0, 0, 0))
    Private ReadOnly flashBrush As New SolidBrush(Color.FromArgb(110, 255, 255, 255))
    Private ReadOnly keyBrush As New SolidBrush(Color.FromArgb(235, 40, 44, 50))
    Private ReadOnly keyPen As New Pen(Color.FromArgb(210, 210, 222), 1.0F)
    Private ReadOnly arrowBrush As New SolidBrush(Color.FromArgb(235, 214, 86, 60))
    Private ReadOnly debugBrush As New SolidBrush(Color.FromArgb(255, 80, 255, 120))
    Private ReadOnly debugPen As New Pen(Color.FromArgb(200, 255, 60, 60), 1.0F)
    ' brushes whose colour is changed before use (no allocation per frame)
    Private ReadOnly mutableBrush As New SolidBrush(Color.White)
    Private ReadOnly textBrush As New SolidBrush(Color.White)

    Private ReadOnly cardBrush As New SolidBrush(Color.FromArgb(235, 18, 13, 28))
    Private ReadOnly cardPen As New Pen(Color.FromArgb(30, 20, 10), 4.0F)
    Private ReadOnly cardInnerPen As New Pen(Color.FromArgb(110, 86, 150), 1.0F)
    Private ReadOnly trackBack As New SolidBrush(Color.FromArgb(12, 8, 18))
    Private ReadOnly trackFrame As New Pen(Color.FromArgb(30, 20, 10), 2.0F)
    Private ReadOnly grooveBrush As New SolidBrush(Color.FromArgb(42, 36, 56))
    Private ReadOnly musicBrush As New SolidBrush(Color.FromArgb(76, 175, 80))
    Private ReadOnly sfxBrush As New SolidBrush(Color.FromArgb(66, 133, 200))
    Private ReadOnly knobBrush As New SolidBrush(Color.FromArgb(228, 228, 238))
    Private ReadOnly knobBrushActive As New SolidBrush(Color.White)
    Private ReadOnly tickBrush As New SolidBrush(Color.FromArgb(130, 118, 160))

    Private ReadOnly spriteAttr As New ImageAttributes()
    Private ReadOnly centerFormat As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
    Private ReadOnly topCenterFormat As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near}
    Private ReadOnly topRightFormat As New StringFormat() With {.Alignment = StringAlignment.Far, .LineAlignment = StringAlignment.Near}

    ' Colour families for particles / popups, indexed by CBBlockType
    Private Shared ReadOnly TintColors() As Color = {
        Color.FromArgb(236, 120, 70),     ' Brick
        Color.FromArgb(170, 186, 204),    ' Stone
        Color.FromArgb(255, 226, 90),     ' Gold
        Color.FromArgb(90, 240, 170)}     ' Emerald

    ' A faint colour wash per level so the five levels feel different (alpha 0 = none)
    Private Shared ReadOnly LevelTints() As Color = {
        Color.FromArgb(0, 0, 0, 0),
        Color.FromArgb(34, 40, 90, 200),
        Color.FromArgb(34, 20, 200, 130),
        Color.FromArgb(38, 230, 140, 30),
        Color.FromArgb(44, 200, 40, 110)}

    Private ReadOnly leftArrow(2) As PointF
    Private ReadOnly rightArrow(2) As PointF

    Public Sub New(gameEngine As CannonBallEngine, assets As CannonBallAssets)
        engine = gameEngine
        art = assets
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True
        Me.BackColor = Color.Black
        spriteAttr.SetWrapMode(Drawing2D.WrapMode.TileFlipXY)
    End Sub

    ' ===================== Coordinate helpers =====================

    Private Function ViewScale() As Single
        Return Math.Max(0.01F, Math.Min(Me.Width / CannonBallEngine.WorldW, Me.Height / CannonBallEngine.ViewH))
    End Function

    ''' <summary>View-space (0..400, 0..640) to control pixels. Used by the form to place PixelButtons.</summary>
    Public Function LogicalToScreen(x As Single, y As Single) As Point
        Dim s As Single = ViewScale()
        Dim offX As Single = (Me.Width - CannonBallEngine.WorldW * s) / 2.0F
        Dim offY As Single = (Me.Height - CannonBallEngine.ViewH * s) / 2.0F
        Return New Point(CInt(offX + x * s), CInt(offY + y * s))
    End Function

    Private Function ScreenToLogical(p As Point) As PointF
        Dim s As Single = ViewScale()
        Dim offX As Single = (Me.Width - CannonBallEngine.WorldW * s) / 2.0F
        Dim offY As Single = (Me.Height - CannonBallEngine.ViewH * s) / 2.0F
        Return New PointF((p.X - offX) / s, (p.Y - offY) / s)
    End Function

    ' ===================== Pause-settings sliders (mouse) =====================

    Private Function PauseButtonVisible() As Boolean
        Return Not Paused AndAlso CountdownText = "" AndAlso engine.CanPause
    End Function

    Private Function OverPauseButton(p As Point) As Boolean
        If Not PauseButtonVisible() Then Return False
        Dim lp As PointF = ScreenToLogical(p)
        Return PauseRect.Contains(CInt(Math.Floor(lp.X)), CInt(Math.Floor(lp.Y)))
    End Function

    Private Function PauseSettingsActive() As Boolean
        Return Paused AndAlso SettingsOpen
    End Function

    Private Shared Function SliderHit(track As Rectangle, lp As PointF) As Boolean
        Return lp.X >= track.X - 10 AndAlso lp.X <= track.Right + 10 AndAlso
               lp.Y >= track.Y - 14 AndAlso lp.Y <= track.Bottom + 14
    End Function

    Private Sub SetSliderValue(index As Integer, logicalX As Single)
        Dim track As Rectangle = If(index = 0, MusicTrackRect, SfxTrackRect)
        Dim innerW As Integer = Math.Max(1, track.Width - 6)
        Dim v As Integer = CInt(Math.Round((logicalX - (track.X + 3)) * 100.0 / innerW))
        v = Math.Max(0, Math.Min(100, v))
        If index = 0 Then
            GameSettings.GetInstance().MusicVolume = v
            AudioManager.ApplyMusicVolume()
        Else
            GameSettings.GetInstance().SfxVolume = v
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        If PauseSettingsActive() Then
            Dim lp As PointF = ScreenToLogical(e.Location)
            If dragSlider >= 0 Then SetSliderValue(dragSlider, lp.X)
            Dim over As Boolean = dragSlider >= 0 OrElse SliderHit(MusicTrackRect, lp) OrElse SliderHit(SfxTrackRect, lp)
            Me.Cursor = If(over, Cursors.Hand, Cursors.Default)
        End If
        If Not PauseSettingsActive() Then
            Dim overBtn As Boolean = OverPauseButton(e.Location)
            If overBtn <> pauseHover Then
                pauseHover = overBtn
                Me.Cursor = If(overBtn, Cursors.Hand, Cursors.Default)
            End If
        End If
        MyBase.OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left AndAlso PauseSettingsActive() Then
            Dim lp As PointF = ScreenToLogical(e.Location)
            If SliderHit(MusicTrackRect, lp) Then
                dragSlider = 0
                SetSliderValue(0, lp.X)
            ElseIf SliderHit(SfxTrackRect, lp) Then
                dragSlider = 1
                SetSliderValue(1, lp.X)
            End If
        End If
        If e.Button = MouseButtons.Left AndAlso OverPauseButton(e.Location) Then
            pauseHover = False
            Me.Cursor = Cursors.Default
            RaiseEvent PauseClicked()
        End If
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        pauseHover = False
        Me.Cursor = Cursors.Default
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        dragSlider = -1
        MyBase.OnMouseUp(e)
    End Sub

    ' ===================== Painting =====================

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.Clear(Color.Black)
        If Me.Width <= 0 OrElse Me.Height <= 0 Then Return

        Dim s As Single = ViewScale()
        Dim offX As Single = (Me.Width - CannonBallEngine.WorldW * s) / 2.0F
        Dim offY As Single = (Me.Height - CannonBallEngine.ViewH * s) / 2.0F

        Dim saved As Drawing2D.GraphicsState = g.Save()
        g.TranslateTransform(offX, offY)
        g.ScaleTransform(s, s)
        g.SetClip(New RectangleF(0, 0, CannonBallEngine.WorldW, CannonBallEngine.ViewH))
        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

        Dim viewState As Drawing2D.GraphicsState = g.Save()

        ' ---- background (view space) ----
        DrawBackdrop(g)

        ' ---- playfield (play space = view space shifted down by the HUD band) ----
        g.TranslateTransform(0, CannonBallEngine.HudH)
        g.SetClip(New RectangleF(0, 0, CannonBallEngine.WorldW, CannonBallEngine.PlayH))
        If engine.ShakeTime > 0.0F Then
            Dim k As Single = engine.ShakeTime * 14.0F
            g.TranslateTransform(CSng(Math.Sin(engine.AnimClock * 90.0F)) * k, CSng(Math.Cos(engine.AnimClock * 70.0F)) * k * 0.6F)
        End If
        DrawBlocks(g)
        DrawParticles(g)
        DrawTrail(g)
        DrawAimPreview(g)
        DrawCart(g)
        DrawBall(g)
        DrawPopups(g)
        DrawPrompts(g)
        DrawStatusText(g)
        If CannonBallEngine.DEBUG_MODE Then DrawDebugPlay(g)
        g.Restore(viewState)

        ' ---- HUD and overlays (view space) ----
        DrawHud(g)
        DrawOverlay(g)
        DrawCountdown(g)
        If CannonBallEngine.DEBUG_MODE Then DrawDebugText(g)

        g.Restore(saved)

        ' ---- fade to black covers the whole control ----
        If FadeAmount > 0.0F Then
            mutableBrush.Color = Color.FromArgb(CInt(Math.Min(1.0F, FadeAmount) * 255.0F), 0, 0, 0)
            g.FillRectangle(mutableBrush, 0, 0, Me.Width, Me.Height)
        End If

        MyBase.OnPaint(e)
    End Sub

    Private Sub DrawBackdrop(g As Graphics)
        g.DrawImage(art.Background, New Rectangle(0, 0, CInt(CannonBallEngine.WorldW), CInt(CannonBallEngine.ViewH)),
                    0, 0, art.Background.Width, art.Background.Height, GraphicsUnit.Pixel, spriteAttr)
        Dim tint As Color = LevelTints(Math.Max(0, Math.Min(LevelTints.Length - 1, engine.Level - 1)))
        If tint.A > 0 Then
            mutableBrush.Color = tint
            g.FillRectangle(mutableBrush, 0, 0, CannonBallEngine.WorldW, CannonBallEngine.ViewH)
        End If
    End Sub

    ' ---------- Blocks ----------

    Private Function BlockSprite(b As CBBlock) As Bitmap
        Select Case b.Kind
            Case CBBlockType.Stone : Return If(b.IsDamaged, art.StoneDamaged, art.Stone)
            Case CBBlockType.Gold : Return art.Gold
            Case CBBlockType.Emerald : Return art.Emerald
        End Select
        Return art.Brick
    End Function

    Private Sub DrawBlocks(g As Graphics)
        For Each b As CBBlock In engine.Blocks
            If Not b.Alive Then Continue For
            Dim bmp As Bitmap = BlockSprite(b)
            Dim x As Integer = CInt(Math.Round(b.X))
            Dim y As Integer = CInt(Math.Round(b.Y))
            Dim w As Integer = CInt(Math.Round(b.Width))
            Dim h As Integer = CInt(Math.Round(b.Height))
            g.DrawImage(bmp, New Rectangle(x, y, w, h), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, spriteAttr)
            If b.FlashTime > 0.0F Then g.FillRectangle(flashBrush, x, y, w, h)
        Next
    End Sub

    ' ---------- Cart / ball ----------

    Private Sub DrawCart(g As Graphics)
        Dim bmp As Bitmap = art.Cart
        Dim w As Integer = CInt(CannonBallEngine.PaddleW)
        Dim h As Integer = Math.Max(1, CInt(Math.Round(bmp.Height * CannonBallEngine.PaddleW / bmp.Width)))
        Dim x As Integer = CInt(Math.Round(engine.CartX - CannonBallEngine.PaddleW / 2.0F))
        Dim y As Integer = CInt(Math.Round(CartSpriteBottom - h))
        g.DrawImage(bmp, New Rectangle(x, y, w, h), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, spriteAttr)
    End Sub

    Private Function BallVisible() As Boolean
        Return engine.State = CBState.Ready OrElse engine.State = CBState.Locked OrElse engine.State = CBState.Playing
    End Function

    Private Sub DrawBall(g As Graphics)
        If Not BallVisible() Then Return
        Dim d As Integer = CInt(CannonBallEngine.BallRadius * 2.0F)
        Dim x As Integer = CInt(Math.Round(engine.BallX - CannonBallEngine.BallRadius))
        Dim y As Integer = CInt(Math.Round(engine.BallY - CannonBallEngine.BallRadius))
        g.DrawImage(art.Ball, New Rectangle(x, y, d, d), 0, 0, art.Ball.Width, art.Ball.Height, GraphicsUnit.Pixel, spriteAttr)
    End Sub

    Private Sub DrawTrail(g As Graphics)
        If engine.State <> CBState.Playing Then Return
        For i As Integer = 1 To engine.TrailCount - 1
            mutableBrush.Color = Color.FromArgb(Math.Max(8, 120 - i * 12), 255, 190, 110)
            Dim sz As Integer = If(i < 4, 4, 3)
            g.FillRectangle(mutableBrush, CInt(engine.TrailX(i)) - sz \ 2, CInt(engine.TrailY(i)) - sz \ 2, sz, sz)
        Next
    End Sub

    Private Sub DrawAimPreview(g As Graphics)
        If engine.State <> CBState.Locked Then Return
        For i As Integer = 0 To engine.PreviewCount - 1
            mutableBrush.Color = Color.FromArgb(240 - i * 22, 255, 226, 120)
            g.FillRectangle(mutableBrush, CInt(engine.PreviewX(i)) - 1, CInt(engine.PreviewY(i)) - 1, 3, 3)
        Next
    End Sub

    ' ---------- Effects ----------

    Private Sub DrawParticles(g As Graphics)
        For Each p As CBParticle In engine.Particles
            Dim a As Single = 1.0F - p.Age / p.Life
            Dim c As Color = TintColors(CInt(p.Tint))
            mutableBrush.Color = Color.FromArgb(CInt(255.0F * a), c.R, c.G, c.B)
            g.FillRectangle(mutableBrush, CInt(p.X), CInt(p.Y), CInt(p.Size), CInt(p.Size))
        Next
    End Sub

    Private Sub DrawPopups(g As Graphics)
        For Each p As CBPopup In engine.Popups
            Dim a As Single = 1.0F - p.Age / 0.8F
            Dim c As Color = TintColors(CInt(p.Tint))
            textBrush.Color = Color.FromArgb(CInt(255.0F * a), c.R, c.G, c.B)
            g.DrawString(p.Text, smallFont, shadowBrush, p.X - 14.0F + 1.0F, p.Y - 10.0F - p.Age * 36.0F + 1.0F)
            g.DrawString(p.Text, smallFont, textBrush, p.X - 14.0F, p.Y - 10.0F - p.Age * 36.0F)
        Next
    End Sub

    ' ---------- Prompts ----------

    Private Sub DrawKeyPrompt(g As Graphics, keyText As String, label As String, y As Single)
        Dim textSize As SizeF = g.MeasureString(label, smallFont)
        Dim keyW As Single = 46.0F
        Dim total As Single = keyW + 8.0F + textSize.Width
        Dim x As Single = (CannonBallEngine.WorldW - total) / 2.0F
        g.FillRectangle(keyBrush, x, y, keyW, 20.0F)
        g.DrawRectangle(keyPen, x, y, keyW, 20.0F)
        g.DrawString(keyText, capFont, whiteBrush, New RectangleF(x, y, keyW, 20.0F), centerFormat)
        g.DrawString(label, smallFont, shadowBrush, x + keyW + 8.0F + 1.0F, y + 2.0F + 1.0F)
        g.DrawString(label, smallFont, goldBrush, x + keyW + 8.0F, y + 2.0F)
    End Sub

    Private Sub DrawPrompts(g As Graphics)
        If engine.State = CBState.Ready AndAlso engine.IntroTime <= 0.0F Then
            DrawKeyPrompt(g, "SPACE", "LOCK CANNON POSITION", 340.0F)
            Dim pulse As Single = CSng(Math.Sin(engine.AnimClock * 6.0F)) * 3.0F
            Dim cy As Single = CannonBallEngine.PaddleY + 8.0F
            Dim lx As Single = engine.CartX - CannonBallEngine.PaddleW / 2.0F - 16.0F - pulse
            Dim rx As Single = engine.CartX + CannonBallEngine.PaddleW / 2.0F + 16.0F + pulse
            leftArrow(0) = New PointF(lx - 8.0F, cy) : leftArrow(1) = New PointF(lx + 4.0F, cy - 8.0F) : leftArrow(2) = New PointF(lx + 4.0F, cy + 8.0F)
            rightArrow(0) = New PointF(rx + 8.0F, cy) : rightArrow(1) = New PointF(rx - 4.0F, cy - 8.0F) : rightArrow(2) = New PointF(rx - 4.0F, cy + 8.0F)
            g.FillPolygon(arrowBrush, leftArrow)
            g.FillPolygon(arrowBrush, rightArrow)
        ElseIf engine.State = CBState.Locked Then
            DrawKeyPrompt(g, "SPACE", "FIRE CANNON", 340.0F)
        End If
    End Sub

    Private Sub DrawCenteredPlay(g As Graphics, text As String, font As Font, brush As Brush, centreY As Single)
        Dim area As New RectangleF(0, centreY - 40, CannonBallEngine.WorldW, 80)
        g.DrawString(text, font, shadowBrush, New RectangleF(area.X + 2, area.Y + 2, area.Width, area.Height), centerFormat)
        g.DrawString(text, font, brush, area, centerFormat)
    End Sub

    Private Sub DrawStatusText(g As Graphics)
        ' LEVEL title at the start of a level
        If engine.State = CBState.Ready AndAlso engine.IntroTime > 0.0F Then
            Dim t As Single = Math.Min(1.0F, engine.IntroTime / 0.6F)
            mutableBrush.Color = Color.FromArgb(CInt(150.0F * t), 0, 0, 0)
            g.FillRectangle(mutableBrush, 0, 0, CannonBallEngine.WorldW, CannonBallEngine.PlayH)
            textBrush.Color = Color.FromArgb(CInt(255.0F * t), 255, 230, 120)
            DrawCenteredPlay(g, "LEVEL " & engine.Level.ToString(), bigFont, textBrush, 250.0F)
            textBrush.Color = Color.FromArgb(CInt(255.0F * t), 235, 235, 235)
            DrawCenteredPlay(g, engine.LevelName, smallFont, textBrush, 292.0F)
        End If

        If engine.State = CBState.BallLost Then
            DrawCenteredPlay(g, "BALL LOST", bigFont, redBrush, 300.0F)
        End If
        If engine.LifeLostFlash > 0.0F Then
            mutableBrush.Color = Color.FromArgb(CInt(110.0F * engine.LifeLostFlash / 0.6F), 220, 30, 20)
            g.FillRectangle(mutableBrush, 0, 0, CannonBallEngine.WorldW, CannonBallEngine.PlayH)
        End If

        If engine.State = CBState.LevelComplete Then
            Dim a As Single = Math.Min(1.0F, engine.StateTime / 0.25F)
            textBrush.Color = Color.FromArgb(CInt(255.0F * a), 255, 230, 120)
            DrawCenteredPlay(g, If(engine.Level >= CannonBallLevelData.LevelCount, "FINAL CHAMBER CLEARED!", "LEVEL " & engine.Level.ToString() & " COMPLETE!"),
                             hudFont, textBrush, 300.0F)
        End If
    End Sub

    ' ===================== HUD =====================

    Private Sub DrawHud(g As Graphics)
        g.DrawImage(art.HudBar, New Rectangle(0, 0, CInt(CannonBallEngine.WorldW), CInt(CannonBallEngine.HudH)),
                    0, 0, art.HudBar.Width, art.HudBar.Height, GraphicsUnit.Pixel, spriteAttr)

        DrawPauseButton(g)

        ' lives
        g.DrawString("CANNON BALLS", capFont, If(engine.LifeFlash > 0.0F, goldBrush, captionBrush), 58.0F, 6.0F)
        For i As Integer = 0 To engine.Lives - 1
            g.DrawImage(art.LifeIcon, New Rectangle(58 + i * 13, 20, 10, 10), 0, 0, art.LifeIcon.Width, art.LifeIcon.Height, GraphicsUnit.Pixel, spriteAttr)
        Next

        ' level / time / score
        DrawStat(g, "LEVEL", engine.Level.ToString() & " / " & CannonBallLevelData.LevelCount.ToString(), 150.0F, topCenterFormat)
        DrawStat(g, "TIME", CannonBallEngine.FormatTime(engine.ElapsedTime), 238.0F, topCenterFormat)
        DrawStat(g, "SCORE", engine.Score.ToString("0000"), 378.0F, topRightFormat)
    End Sub

    Private Sub DrawPauseButton(g As Graphics)
        Dim r As Rectangle = PauseRect
        g.FillRectangle(pauseShadow, r.X, r.Bottom - 3, r.Width, 3)
        g.FillRectangle(If(pauseHover, pauseBodyHover, pauseBody), r.X, r.Y, r.Width, r.Height - 3)
        g.DrawRectangle(pauseOutline, r.X + 1, r.Y + 1, r.Width - 3, r.Height - 5)
        g.FillRectangle(whiteBrush, r.X + 8, r.Y + 6, 4, 13)
        g.FillRectangle(whiteBrush, r.X + 16, r.Y + 6, 4, 13)
    End Sub

    Private Sub DrawStat(g As Graphics, caption As String, value As String, x As Single, fmt As StringFormat)
        ' x is the centre for centred stats, or the RIGHT edge for right-aligned stats
        Dim left As Single = If(fmt Is topRightFormat, x - 140.0F, x - 70.0F)
        Dim capArea As New RectangleF(left, 5.0F, 140.0F, 12.0F)
        Dim valArea As New RectangleF(left, 17.0F, 140.0F, 20.0F)
        g.DrawString(caption, capFont, captionBrush, capArea, fmt)
        g.DrawString(value, valFont, shadowBrush, New RectangleF(valArea.X + 1, valArea.Y + 1, valArea.Width, valArea.Height), fmt)
        g.DrawString(value, valFont, whiteBrush, valArea, fmt)
    End Sub

    ' ===================== Overlays (view space) =====================

    Private Sub DrawShadowText(g As Graphics, text As String, font As Font, brush As Brush, x As Single, y As Single)
        g.DrawString(text, font, shadowBrush, x + 2, y + 2)
        g.DrawString(text, font, brush, x, y)
    End Sub

    Private Sub DrawCenteredText(g As Graphics, text As String, font As Font, brush As Brush, centreY As Single)
        Dim area As New RectangleF(0, centreY - 30, CannonBallEngine.WorldW, 60)
        g.DrawString(text, font, shadowBrush, New RectangleF(area.X + 2, area.Y + 2, area.Width, area.Height), centerFormat)
        g.DrawString(text, font, brush, area, centerFormat)
    End Sub

    Private Sub DrawOverlay(g As Graphics)
        If Paused Then
            g.FillRectangle(dimBrush, 0, 0, CannonBallEngine.WorldW, CannonBallEngine.ViewH)
            If SettingsOpen Then
                DrawPauseSettings(g)
            Else
                DrawCenteredText(g, "PAUSED", bigFont, whiteBrush, 190)
            End If
        ElseIf engine.State = CBState.GameOver Then
            g.FillRectangle(dimBrush, 0, 0, CannonBallEngine.WorldW, CannonBallEngine.ViewH)
            DrawCenteredText(g, "GAME OVER", bigFont, redBrush, 150)
            DrawCenteredText(g, "SCORE", smallFont, captionBrush, 205)
            DrawCenteredText(g, engine.Score.ToString(), hudFont, whiteBrush, 228)
            DrawCenteredText(g, "LEVEL", smallFont, captionBrush, 265)
            DrawCenteredText(g, engine.Level.ToString() & " / " & CannonBallLevelData.LevelCount.ToString(), hudFont, whiteBrush, 288)
            If engine.NewBestScore Then DrawCenteredText(g, "NEW BEST SCORE!", smallFont, goldBrush, 322)
        ElseIf engine.State = CBState.Victory Then
            g.FillRectangle(dimBrush, 0, 0, CannonBallEngine.WorldW, CannonBallEngine.ViewH)
            DrawCenteredText(g, "YOU WIN!", bigFont, goldBrush, 130)
            DrawCenteredText(g, "SCORE", smallFont, captionBrush, 190)
            DrawCenteredText(g, engine.Score.ToString(), hudFont, whiteBrush, 213)
            DrawCenteredText(g, "TIME", smallFont, captionBrush, 253)
            DrawCenteredText(g, CannonBallEngine.FormatTime(engine.ElapsedTime), hudFont, whiteBrush, 276)
            If engine.NewBestTime Then
                DrawCenteredText(g, "NEW BEST TIME!", smallFont, goldBrush, 312)
            ElseIf engine.BestTime > 0.0 Then
                DrawCenteredText(g, "BEST TIME  " & CannonBallEngine.FormatTime(engine.BestTime), smallFont, captionBrush, 312)
            End If
        End If
    End Sub

    Private Sub DrawPauseSettings(g As Graphics)
        Dim card As New Rectangle(30, 100, 340, 400)
        g.FillRectangle(cardBrush, card)
        g.DrawRectangle(cardPen, card.X + 2, card.Y + 2, card.Width - 4, card.Height - 4)
        g.DrawRectangle(cardInnerPen, card.X + 7, card.Y + 7, card.Width - 15, card.Height - 15)
        DrawCenteredText(g, "SETTINGS", bigFont, whiteBrush, 145)
        g.FillRectangle(tickBrush, card.X + 30, 180, card.Width - 60, 2)
        g.FillRectangle(goldBrush, card.X + card.Width \ 2 - 4, 177, 8, 8)

        DrawSliderLogical(g, "BACKGROUND MUSIC", MusicTrackRect, GameSettings.GetInstance().MusicVolume, musicBrush, dragSlider = 0)
        DrawSliderLogical(g, "SOUND EFFECTS", SfxTrackRect, GameSettings.GetInstance().SfxVolume, sfxBrush, dragSlider = 1)
    End Sub

    Private Sub DrawSliderLogical(g As Graphics, caption As String, track As Rectangle, value As Integer, accent As Brush, active As Boolean)
        DrawShadowText(g, caption, smallFont, whiteBrush, track.X, track.Y - 30)
        Dim pct As String = value.ToString() & "%"
        Dim pctSize As SizeF = g.MeasureString(pct, hudFont)
        DrawShadowText(g, pct, hudFont, goldBrush, track.Right - pctSize.Width, track.Y - 34)

        Dim innerW As Integer = Math.Max(1, track.Width - 6)
        Dim fillW As Integer = CInt(innerW * value / 100.0)

        g.FillRectangle(trackBack, track)
        g.DrawRectangle(trackFrame, track.X + 1, track.Y + 1, track.Width - 3, track.Height - 3)
        g.FillRectangle(grooveBrush, track.X + 3, track.Y + 3, innerW, track.Height - 6)
        If fillW > 0 Then g.FillRectangle(accent, track.X + 3, track.Y + 3, fillW, track.Height - 6)

        For i As Integer = 0 To 10
            Dim tx As Integer = track.X + CInt(track.Width * i / 10.0)
            g.FillRectangle(tickBrush, tx - 1, track.Bottom + 5, 2, If(i Mod 5 = 0, 8, 5))
        Next

        Dim kx As Integer = track.X + 3 + fillW
        Dim ky As Integer = track.Y + track.Height \ 2 - 15
        g.FillRectangle(shadowBrush, kx - 8, ky + 4, 16, 30)
        g.FillRectangle(If(active, knobBrushActive, knobBrush), kx - 8, ky, 16, 30)
        g.DrawRectangle(trackFrame, kx - 7, ky + 1, 14, 28)
        g.FillRectangle(accent, kx - 2, ky + 6, 4, 18)
    End Sub

    Private Sub DrawCountdown(g As Graphics)
        If CountdownText = "" Then Return
        If CountdownText <> "GO!" Then
            g.FillRectangle(countDimBrush, 0, 0, CannonBallEngine.WorldW, CannonBallEngine.ViewH)
        End If
        Dim brush As Brush = If(CountdownText = "GO!", greenBrush, goldBrush)
        Dim area As New RectangleF(0, 200, CannonBallEngine.WorldW, 200)
        g.DrawString(CountdownText, countFont, shadowBrush, New RectangleF(area.X + 4, area.Y + 4, area.Width, area.Height), centerFormat)
        g.DrawString(CountdownText, countFont, brush, area, centerFormat)
    End Sub

    ' ===================== Debug helpers (only when DEBUG_MODE = True) =====================

    Private Sub DrawDebugPlay(g As Graphics)
        For Each b As CBBlock In engine.Blocks
            If b.Alive Then g.DrawRectangle(debugPen, b.X, b.Y, b.Width, b.Height)
        Next
        g.DrawRectangle(debugPen, engine.CartX - CannonBallEngine.PaddleW / 2.0F, CannonBallEngine.PaddleY, CannonBallEngine.PaddleW, CannonBallEngine.PaddleH)
        g.DrawEllipse(debugPen, engine.BallX - CannonBallEngine.BallRadius, engine.BallY - CannonBallEngine.BallRadius,
                      CannonBallEngine.BallRadius * 2.0F, CannonBallEngine.BallRadius * 2.0F)
    End Sub

    Private Sub DrawDebugText(g As Graphics)
        Dim t As String = "state=" & engine.State.ToString() & "  vel=(" & engine.VelX.ToString("0") & "," & engine.VelY.ToString("0") &
                          ")  speed=" & CSng(Math.Sqrt(engine.VelX * engine.VelX + engine.VelY * engine.VelY)).ToString("0")
        g.DrawString(t, debugFont, debugBrush, 20.0F, 44.0F)
    End Sub

    ' ===================== Cleanup =====================

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            hudFont.Dispose() : smallFont.Dispose() : bigFont.Dispose() : countFont.Dispose()
            capFont.Dispose() : valFont.Dispose() : debugFont.Dispose()
            shadowBrush.Dispose() : whiteBrush.Dispose() : goldBrush.Dispose() : redBrush.Dispose() : greenBrush.Dispose()
            captionBrush.Dispose() : dimBrush.Dispose() : countDimBrush.Dispose() : flashBrush.Dispose()
            keyBrush.Dispose() : keyPen.Dispose() : arrowBrush.Dispose() : debugBrush.Dispose() : debugPen.Dispose()
            mutableBrush.Dispose() : textBrush.Dispose()
            cardBrush.Dispose() : cardPen.Dispose() : cardInnerPen.Dispose()
            trackBack.Dispose() : trackFrame.Dispose() : grooveBrush.Dispose()
            musicBrush.Dispose() : sfxBrush.Dispose() : knobBrush.Dispose() : knobBrushActive.Dispose() : tickBrush.Dispose()
            spriteAttr.Dispose()
            pauseBody.Dispose() : pauseBodyHover.Dispose() : pauseShadow.Dispose() : pauseOutline.Dispose()
            centerFormat.Dispose() : topCenterFormat.Dispose() : topRightFormat.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
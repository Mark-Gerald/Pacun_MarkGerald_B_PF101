Option Strict On
Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Windows.Forms

Public Enum CBMenuPage
    Main
    Settings
    Tutorial
End Enum

''' <summary>
''' Draws the Cannon Ball menu window: animated jungle scene, title, and the SETTINGS and HOW TO PLAY pages
''' (all inside the same window). The buttons themselves are PixelButtons owned by CannonBallMenuForm.
''' </summary>
Public Class CannonBallMenuPanel
    Inherits System.Windows.Forms.Panel

    Private ReadOnly art As CannonBallAssets
    Public Property Page As CBMenuPage = CBMenuPage.Main
    Public Property FadeAmount As Single = 0.0F          ' 0..1 black overlay used by page transitions

    Private scene As Bitmap
    Private sceneSize As Size = Size.Empty
    Private ReadOnly animTimer As System.Windows.Forms.Timer
    Private ReadOnly animClock As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
    Private dragSlider As Integer = -1

    Private ReadOnly titleFont As New Font("Segoe UI", 58.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly pageTitleFont As New Font("Segoe UI", 30.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly headFont As New Font("Segoe UI", 13.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly bodyFont As New Font("Segoe UI", 12.0F, FontStyle.Regular, GraphicsUnit.Pixel)
    Private ReadOnly smallFont As New Font("Segoe UI", 13.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly valueFont As New Font("Segoe UI", 18.0F, FontStyle.Bold, GraphicsUnit.Pixel)

    Private ReadOnly shadowBrush As New SolidBrush(Color.FromArgb(190, 0, 0, 0))
    Private ReadOnly outlineBrush As New SolidBrush(Color.FromArgb(40, 24, 8))
    Private ReadOnly goldBrush As New SolidBrush(Color.FromArgb(255, 214, 90))
    Private ReadOnly whiteBrush As New SolidBrush(Color.White)
    Private ReadOnly softBrush As New SolidBrush(Color.FromArgb(214, 224, 214))
    Private ReadOnly cardBrush As New SolidBrush(Color.FromArgb(232, 14, 20, 18))
    Private ReadOnly cardPen As New Pen(Color.FromArgb(30, 20, 10), 4.0F)
    Private ReadOnly cardInnerPen As New Pen(Color.FromArgb(80, 140, 96), 1.0F)
    Private ReadOnly mutableBrush As New SolidBrush(Color.White)
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

    Private Shared ReadOnly TutorialEntries()() As String = {
        New String() {"MOVE", "LEFT / RIGHT arrows or A / D move the cannon cart."},
        New String() {"LAUNCH", "SPACE locks your position, SPACE again fires the cannon ball."},
        New String() {"BREAK BLOCKS", "Bounce the ball into blocks to destroy them and earn points."},
        New String() {"SURVIVE", "Catch the ball with your cart. Where it lands changes its angle."},
        New String() {"LOSE A BALL", "If the ball falls below the cart you lose one cannon ball."},
        New String() {"REACH THE TOP", "Get the ball through the top of the screen to clear the level."}}

    Public Sub New(assets As CannonBallAssets)
        art = assets
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True
        Me.BackColor = Color.Black
        spriteAttr.SetWrapMode(WrapMode.TileFlipXY)

        animTimer = New System.Windows.Forms.Timer() With {.Interval = 33}
        AddHandler animTimer.Tick, Sub(s, e) Me.Invalidate()
        animTimer.Start()
    End Sub

    Public Sub StopAnimation()
        animTimer.Stop()
    End Sub

    ' ===================== Page layout (shared with the form) =====================

    Public Function CardRect() As Rectangle
        Dim w As Integer = Math.Min(640, Math.Max(300, Me.ClientSize.Width - 40))
        Dim h As Integer = If(Page = CBMenuPage.Tutorial, 330, 270)
        Return New Rectangle((Me.ClientSize.Width - w) \ 2, 24, w, h)
    End Function

    Private Function MusicTrack() As Rectangle
        Dim c As Rectangle = CardRect()
        Return New Rectangle(c.X + 40, c.Y + 110, c.Width - 80, 18)
    End Function

    Private Function SfxTrack() As Rectangle
        Dim c As Rectangle = CardRect()
        Return New Rectangle(c.X + 40, c.Y + 190, c.Width - 80, 18)
    End Function

    ' ===================== Slider mouse =====================

    Private Shared Function SliderHit(track As Rectangle, p As Point) As Boolean
        Return p.X >= track.X - 10 AndAlso p.X <= track.Right + 10 AndAlso p.Y >= track.Y - 14 AndAlso p.Y <= track.Bottom + 14
    End Function

    Private Sub SetSlider(index As Integer, x As Integer)
        Dim track As Rectangle = If(index = 0, MusicTrack(), SfxTrack())
        Dim innerW As Integer = Math.Max(1, track.Width - 6)
        Dim v As Integer = Math.Max(0, Math.Min(100, CInt(Math.Round((x - (track.X + 3)) * 100.0 / innerW))))
        If index = 0 Then
            GameSettings.GetInstance().MusicVolume = v
            AudioManager.ApplyMusicVolume()
        Else
            GameSettings.GetInstance().SfxVolume = v
        End If
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left AndAlso Page = CBMenuPage.Settings Then
            If SliderHit(MusicTrack(), e.Location) Then
                dragSlider = 0 : SetSlider(0, e.X)
            ElseIf SliderHit(SfxTrack(), e.Location) Then
                dragSlider = 1 : SetSlider(1, e.X)
            End If
        End If
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        If Page = CBMenuPage.Settings Then
            If dragSlider >= 0 Then SetSlider(dragSlider, e.X)
            Dim over As Boolean = dragSlider >= 0 OrElse SliderHit(MusicTrack(), e.Location) OrElse SliderHit(SfxTrack(), e.Location)
            Me.Cursor = If(over, Cursors.Hand, Cursors.Default)
        End If
        MyBase.OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        If dragSlider = 1 Then AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")   ' let the player hear the new SFX volume
        dragSlider = -1
        MyBase.OnMouseUp(e)
    End Sub

    ' ===================== Painting =====================

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim w As Integer = Me.ClientSize.Width
        Dim h As Integer = Me.ClientSize.Height
        If w <= 0 OrElse h <= 0 Then Return

        If scene Is Nothing OrElse sceneSize.Width <> w OrElse sceneSize.Height <> h Then BuildScene(w, h)
        g.DrawImageUnscaled(scene, 0, 0)
        DrawFireflies(g, w, h)

        Select Case Page
            Case CBMenuPage.Main : DrawMain(g, w, h)
            Case CBMenuPage.Settings : DrawSettings(g)
            Case CBMenuPage.Tutorial : DrawTutorial(g)
        End Select

        If FadeAmount > 0.0F Then
            mutableBrush.Color = Color.FromArgb(CInt(Math.Min(1.0F, FadeAmount) * 255.0F), 0, 0, 0)
            g.FillRectangle(mutableBrush, 0, 0, w, h)
        End If
        MyBase.OnPaint(e)
    End Sub

    ' ---------- Cached scene (drawn at 1/3 size, then enlarged with nearest-neighbour for a pixel look) ----------

    Private Sub BuildScene(w As Integer, h As Integer)
        If scene IsNot Nothing Then scene.Dispose()
        Dim rnd As New Random(21)
        Dim lw As Integer = Math.Max(8, w \ 3)
        Dim lh As Integer = Math.Max(8, h \ 3)

        Using low As New Bitmap(lw, lh, PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(low)
                g.SmoothingMode = SmoothingMode.None
                Using br As New LinearGradientBrush(New Rectangle(0, 0, lw, lh), Color.FromArgb(4, 14, 20), Color.FromArgb(10, 42, 30), LinearGradientMode.Vertical)
                    g.FillRectangle(br, 0, 0, lw, lh)
                End Using
                Using glow As New SolidBrush(Color.FromArgb(26, 70, 210, 140))
                    g.FillEllipse(glow, lw \ 2 - 70, lh \ 2 - 40, 140, 120)
                End Using

                ' stepped temple
                Dim cx As Integer = lw \ 2
                For i As Integer = 0 To 7
                    Using b As New SolidBrush(Color.FromArgb(12, 32, 32))
                        g.FillRectangle(b, cx - (14 + 10 * i), lh - 120 + 12 * i, 28 + 20 * i, 12)
                    End Using
                Next
                Using b As New SolidBrush(Color.FromArgb(3, 10, 12))
                    g.FillRectangle(b, cx - 9, lh - 50, 18, 26)
                End Using

                ' trunks
                For i As Integer = 0 To CInt(lw / 22)
                    Using b As New SolidBrush(Color.FromArgb(8, 22, 16))
                        g.FillRectangle(b, rnd.Next(0, lw), 0, rnd.Next(4, 9), lh)
                    End Using
                Next
                ' canopy
                Dim greens() As Color = {Color.FromArgb(10, 46, 28), Color.FromArgb(14, 62, 36), Color.FromArgb(8, 34, 22)}
                For i As Integer = 0 To CInt(lw / 5)
                    Using b As New SolidBrush(greens(rnd.Next(0, 3)))
                        g.FillEllipse(b, rnd.Next(-10, lw), rnd.Next(-16, 20), rnd.Next(24, 56), rnd.Next(14, 34))
                    End Using
                Next
                ' vines
                For i As Integer = 0 To CInt(lw / 12)
                    Dim vx As Integer = rnd.Next(0, lw)
                    For y As Integer = 8 To rnd.Next(30, Math.Max(31, lh \ 2))
                        If y Mod 9 = 0 Then vx += rnd.Next(-1, 2)
                        Using b As New SolidBrush(Color.FromArgb(34, 98, 52))
                            g.FillRectangle(b, vx, y, 1, 1)
                        End Using
                        If y Mod 14 = 0 Then
                            Using b As New SolidBrush(Color.FromArgb(62, 152, 74))
                                g.FillRectangle(b, vx + 1, y, 2, 2)
                            End Using
                        End If
                    Next
                Next
                ' ground
                Using b As New SolidBrush(Color.FromArgb(8, 22, 14))
                    g.FillRectangle(b, 0, lh - 18, lw, 18)
                End Using
                For x As Integer = 0 To lw Step 5
                    Using b As New SolidBrush(Color.FromArgb(18, 56, 30))
                        g.FillRectangle(b, x, lh - 20 - rnd.Next(0, 4), 5, 6)
                    End Using
                Next
            End Using

            scene = New Bitmap(w, h, PixelFormat.Format32bppArgb)
            Using g2 As Graphics = Graphics.FromImage(scene)
                g2.InterpolationMode = InterpolationMode.NearestNeighbor
                g2.PixelOffsetMode = PixelOffsetMode.Half
                g2.DrawImage(low, New Rectangle(0, 0, w, h), 0, 0, lw, lh, GraphicsUnit.Pixel)
            End Using
        End Using
        sceneSize = New Size(w, h)
    End Sub

    Private Sub DrawFireflies(g As Graphics, w As Integer, h As Integer)
        Dim t As Double = animClock.Elapsed.TotalSeconds
        For i As Integer = 0 To 17
            Dim fx As Single = CSng((Math.Sin(t * 0.25 + i * 1.7) * 0.5 + 0.5) * w)
            Dim fy As Single = CSng((Math.Sin(t * 0.18 + i * 2.3) * 0.5 + 0.5) * h * 0.8 + h * 0.05)
            Dim a As Integer = CInt((Math.Sin(t * 2.0 + i) * 0.5 + 0.5) * 200.0) + 30
            mutableBrush.Color = Color.FromArgb(a, 190, 255, 130)
            g.FillRectangle(mutableBrush, CInt(fx), CInt(fy), 3, 3)
        Next
    End Sub

    ' ---------- Main page ----------

    Private Sub DrawMain(g As Graphics, w As Integer, h As Integer)
        Dim rect As New RectangleF(0, 38, w, 80)
        For Each o As Point In New Point() {New Point(-3, 0), New Point(3, 0), New Point(0, -3), New Point(0, 3), New Point(0, 5)}
            g.DrawString("CANNON BALL", titleFont, outlineBrush, New RectangleF(rect.X + o.X, rect.Y + o.Y, rect.Width, rect.Height), centerFormat)
        Next
        g.DrawString("CANNON BALL", titleFont, goldBrush, rect, centerFormat)

        ' decorative row of the four block types
        Dim sprites() As Bitmap = {art.Brick, art.Stone, art.Gold, art.Emerald}
        Dim bw As Integer = CInt(CannonBallLevelData.BlockW) * 3
        Dim bh As Integer = CInt(CannonBallLevelData.BlockH) * 3
        Dim total As Integer = sprites.Length * bw + (sprites.Length - 1) * 6
        Dim x As Integer = (w - total) \ 2
        For Each spr As Bitmap In sprites
            g.InterpolationMode = InterpolationMode.NearestNeighbor
            g.PixelOffsetMode = PixelOffsetMode.Half
            g.DrawImage(spr, New Rectangle(x, 128, bw, bh), 0, 0, spr.Width, spr.Height, GraphicsUnit.Pixel, spriteAttr)
            x += bw + 6
        Next

        ' bouncing cannon ball
        Dim t As Double = animClock.Elapsed.TotalSeconds
        Dim by As Integer = 128 + bh + 12 + CInt(Math.Abs(Math.Sin(t * 3.0)) * 24.0)
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        g.DrawImage(art.Ball, New Rectangle(w \ 2 - 15, by, 30, 30), 0, 0, art.Ball.Width, art.Ball.Height, GraphicsUnit.Pixel, spriteAttr)
    End Sub

    ' ---------- Card helpers ----------

    Private Sub DrawCard(g As Graphics, card As Rectangle, title As String)
        g.FillRectangle(cardBrush, card)
        g.DrawRectangle(cardPen, card.X + 2, card.Y + 2, card.Width - 4, card.Height - 4)
        g.DrawRectangle(cardInnerPen, card.X + 7, card.Y + 7, card.Width - 15, card.Height - 15)
        Dim area As New RectangleF(card.X, card.Y + 14, card.Width, 40)
        g.DrawString(title, pageTitleFont, shadowBrush, New RectangleF(area.X + 2, area.Y + 2, area.Width, area.Height), centerFormat)
        g.DrawString(title, pageTitleFont, goldBrush, area, centerFormat)
        g.FillRectangle(tickBrush, card.X + 30, card.Y + 60, card.Width - 60, 2)
    End Sub

    ' ---------- Settings page ----------

    Private Sub DrawSettings(g As Graphics)
        Dim card As Rectangle = CardRect()
        DrawCard(g, card, "SETTINGS")
        DrawSlider(g, "BACKGROUND MUSIC", MusicTrack(), GameSettings.GetInstance().MusicVolume, musicBrush, dragSlider = 0)
        DrawSlider(g, "SOUND EFFECTS", SfxTrack(), GameSettings.GetInstance().SfxVolume, sfxBrush, dragSlider = 1)
    End Sub

    Private Sub DrawSlider(g As Graphics, caption As String, track As Rectangle, value As Integer, accent As Brush, active As Boolean)
        g.DrawString(caption, smallFont, whiteBrush, track.X, track.Y - 30)
        Dim pct As String = value.ToString() & "%"
        Dim sz As SizeF = g.MeasureString(pct, valueFont)
        g.DrawString(pct, valueFont, goldBrush, track.Right - sz.Width, track.Y - 34)

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

    ' ---------- How to play page ----------

    Private Sub DrawTutorial(g As Graphics)
        Dim card As Rectangle = CardRect()
        DrawCard(g, card, "HOW TO PLAY")
        Dim y As Integer = card.Y + 72
        Dim x As Integer = card.X + 28
        For Each entry As String() In TutorialEntries
            g.DrawString(entry(0), headFont, goldBrush, x, y)
            g.DrawString(entry(1), bodyFont, softBrush, x, y + 16)
            y += 36
        Next
        g.DrawString("There are 5 levels. Clear them all to win!", headFont, whiteBrush, x, y + 2)
    End Sub

    ' ===================== Cleanup =====================

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            animTimer.Stop()
            animTimer.Dispose()
            If scene IsNot Nothing Then scene.Dispose()
            titleFont.Dispose() : pageTitleFont.Dispose() : headFont.Dispose() : bodyFont.Dispose()
            smallFont.Dispose() : valueFont.Dispose()
            shadowBrush.Dispose() : outlineBrush.Dispose() : goldBrush.Dispose() : whiteBrush.Dispose() : softBrush.Dispose()
            cardBrush.Dispose() : cardPen.Dispose() : cardInnerPen.Dispose() : mutableBrush.Dispose()
            trackBack.Dispose() : trackFrame.Dispose() : grooveBrush.Dispose()
            musicBrush.Dispose() : sfxBrush.Dispose() : knobBrush.Dispose() : knobBrushActive.Dispose() : tickBrush.Dispose()
            spriteAttr.Dispose() : centerFormat.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
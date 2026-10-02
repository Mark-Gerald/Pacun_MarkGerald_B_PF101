Imports System.Drawing.Imaging

Public Enum JKOverlay
    None
    Paused
    GameOver
End Enum

''' <summary>
''' Draws the Jump Knight world. It only READS the engine; it never changes game state.
''' The 400 x 600 logical play area is scaled (nearest-neighbour) to fit the panel and centred.
''' It also owns the small PAUSE button drawn in the top-left corner (raises PauseClicked).
''' </summary>
Public Class JumpKnightPanel
    Inherits System.Windows.Forms.Panel

    Private ReadOnly engine As JumpKnightEngine
    Private ReadOnly art As JumpKnightAssets

    Public Property Overlay As JKOverlay = JKOverlay.None

    ''' <summary>"3", "2", "1", "GO!" or "" (nothing). Set by the game form during the resume countdown.</summary>
    Public Property CountdownText As String = ""

    ''' <summary>Raised when the player clicks the pause button.</summary>
    Public Event PauseClicked()

    ' Pause button (logical play-area coordinates). It sits on the left stone pillar, so it never covers the score.
    Private Shared ReadOnly PauseRect As New Rectangle(4, 6, 32, 28)
    Private pauseHover As Boolean = False

    ' Cached GDI+ objects (created once, disposed in Dispose)
    Private ReadOnly hudFont As New Font("Segoe UI", 18.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly smallFont As New Font("Segoe UI", 13.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly bigFont As New Font("Segoe UI", 40.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly countFont As New Font("Segoe UI", 120.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly shadowBrush As New SolidBrush(Color.FromArgb(170, 0, 0, 0))
    Private ReadOnly whiteBrush As New SolidBrush(Color.White)
    Private ReadOnly goldBrush As New SolidBrush(Color.FromArgb(255, 230, 120))
    Private ReadOnly greenBrush As New SolidBrush(Color.FromArgb(120, 235, 110))
    Private ReadOnly glowBrush As New SolidBrush(Color.FromArgb(70, 255, 230, 120))
    Private ReadOnly darkOverlay As New SolidBrush(Color.FromArgb(70, 0, 0, 12))
    Private ReadOnly dimBrush As New SolidBrush(Color.FromArgb(165, 0, 0, 0))
    Private ReadOnly countDimBrush As New SolidBrush(Color.FromArgb(95, 0, 0, 0))
    Private ReadOnly barBack As New SolidBrush(Color.FromArgb(150, 0, 0, 0))
    Private ReadOnly barFill As New SolidBrush(Color.FromArgb(235, 120, 80))
    Private ReadOnly fallbackStone As New SolidBrush(Color.FromArgb(120, 130, 120))
    Private ReadOnly fallbackMoving As New SolidBrush(Color.FromArgb(110, 130, 220))
    Private ReadOnly fallbackWood As New SolidBrush(Color.FromArgb(120, 80, 50))
    Private ReadOnly fallbackIce As New SolidBrush(Color.FromArgb(180, 230, 250))
    Private ReadOnly fallbackBat As New SolidBrush(Color.FromArgb(120, 70, 150))
    Private ReadOnly fallbackKnight As New SolidBrush(Color.FromArgb(190, 190, 205))
    Private ReadOnly pauseBody As New SolidBrush(Color.FromArgb(58, 58, 82))
    Private ReadOnly pauseBodyHover As New SolidBrush(Color.FromArgb(88, 88, 120))
    Private ReadOnly pauseShadow As New SolidBrush(Color.FromArgb(22, 22, 34))
    Private ReadOnly pauseOutline As New Pen(Color.FromArgb(30, 20, 10), 2.0F)
    Private ReadOnly slashPenOuter As New Pen(Color.FromArgb(255, 255, 255), 5.0F)
    Private ReadOnly slashPenMid As New Pen(Color.FromArgb(190, 232, 255), 3.0F)
    Private ReadOnly slashPenInner As New Pen(Color.FromArgb(120, 180, 255), 2.0F)
    Private ReadOnly sparkPen As New Pen(Color.FromArgb(255, 240, 150), 2.0F)
    Private ReadOnly spriteAttr As New ImageAttributes()
    Private ReadOnly leftShade As Drawing2D.LinearGradientBrush
    Private ReadOnly rightShade As Drawing2D.LinearGradientBrush
    Private ReadOnly centerFormat As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

    Private Const PillarArtW As Integer = 18
    Private Const PillarW As Single = PillarArtW * JumpKnightAssets.ArtScale

    Public Sub New(gameEngine As JumpKnightEngine, assets As JumpKnightAssets)
        engine = gameEngine
        art = assets
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True
        Me.BackColor = Color.Black

        spriteAttr.SetWrapMode(Drawing2D.WrapMode.TileFlipXY)   ' no blurry edges when sprites are scaled

        leftShade = New Drawing2D.LinearGradientBrush(
            New RectangleF(PillarW, 0, 26, JumpKnightEngine.ViewH),
            Color.FromArgb(150, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), Drawing2D.LinearGradientMode.Horizontal)
        rightShade = New Drawing2D.LinearGradientBrush(
            New RectangleF(JumpKnightEngine.WorldW - PillarW - 26, 0, 26, JumpKnightEngine.ViewH),
            Color.FromArgb(0, 0, 0, 0), Color.FromArgb(150, 0, 0, 0), Drawing2D.LinearGradientMode.Horizontal)
    End Sub

    ' ===================== Coordinate helpers =====================

    Private Function ViewScale() As Single
        Return Math.Max(0.01F, Math.Min(Me.Width / JumpKnightEngine.WorldW, Me.Height / JumpKnightEngine.ViewH))
    End Function

    Public Function LogicalToScreen(x As Single, y As Single) As Point
        Dim s As Single = ViewScale()
        Dim offX As Single = (Me.Width - JumpKnightEngine.WorldW * s) / 2.0F
        Dim offY As Single = (Me.Height - JumpKnightEngine.ViewH * s) / 2.0F
        Return New Point(CInt(offX + x * s), CInt(offY + y * s))
    End Function

    Private Function ScreenToLogical(p As Point) As PointF
        Dim s As Single = ViewScale()
        Dim offX As Single = (Me.Width - JumpKnightEngine.WorldW * s) / 2.0F
        Dim offY As Single = (Me.Height - JumpKnightEngine.ViewH * s) / 2.0F
        Return New PointF((p.X - offX) / s, (p.Y - offY) / s)
    End Function

    Private Function SY(worldY As Single) As Single
        Return JumpKnightEngine.ViewH - (worldY - engine.CamBottom)
    End Function

    ' ===================== Pause button (mouse) =====================

    Private Function PauseButtonVisible() As Boolean
        Return engine.State = JKState.Playing AndAlso Overlay = JKOverlay.None AndAlso
               (CountdownText = "" OrElse CountdownText = "GO!")
    End Function

    Private Function OverPauseButton(p As Point) As Boolean
        If Not PauseButtonVisible() Then Return False
        Dim lp As PointF = ScreenToLogical(p)
        Return PauseRect.Contains(CInt(Math.Floor(lp.X)), CInt(Math.Floor(lp.Y)))
    End Function

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        Dim over As Boolean = OverPauseButton(e.Location)
        If over <> pauseHover Then
            pauseHover = over
            Me.Cursor = If(over, Cursors.Hand, Cursors.Default)
        End If
        MyBase.OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        pauseHover = False
        Me.Cursor = Cursors.Default
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left AndAlso OverPauseButton(e.Location) Then
            pauseHover = False
            Me.Cursor = Cursors.Default
            RaiseEvent PauseClicked()
        End If
        MyBase.OnMouseDown(e)
    End Sub

    ' ===================== Painting =====================

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.Clear(Color.Black)
        If Me.Width <= 0 OrElse Me.Height <= 0 Then Return

        Dim s As Single = ViewScale()
        Dim offX As Single = (Me.Width - JumpKnightEngine.WorldW * s) / 2.0F
        Dim offY As Single = (Me.Height - JumpKnightEngine.ViewH * s) / 2.0F

        Dim saved As Drawing2D.GraphicsState = g.Save()
        g.TranslateTransform(offX, offY)
        g.ScaleTransform(s, s)
        g.SetClip(New RectangleF(0, 0, JumpKnightEngine.WorldW, JumpKnightEngine.ViewH))
        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

        DrawBackground(g)
        DrawPlatforms(g)
        DrawPowerUps(g)
        DrawEnemies(g)
        DrawKnight(g)
        DrawAttack(g)
        DrawImpacts(g)
        DrawHud(g)
        DrawPauseButton(g)
        DrawOverlay(g)
        DrawCountdown(g)

        g.Restore(saved)
        MyBase.OnPaint(e)
    End Sub

    Private Sub DrawBackground(g As Graphics)
        Dim w As Integer = CInt(JumpKnightEngine.WorldW)
        Dim h As Integer = CInt(JumpKnightEngine.ViewH)

        If art.WallFar IsNot Nothing Then
            art.DrawWallColumn(g, art.WallFar, 0, 4, w \ JumpKnightAssets.ArtScale, 0, h, engine.CamBottom * 0.4)
        Else
            g.FillRectangle(Brushes.Black, 0, 0, w, h)
        End If
        g.FillRectangle(darkOverlay, 0, 0, w, h)

        If art.WallNear IsNot Nothing Then
            art.DrawWallColumn(g, art.WallNear, 0, 0, PillarArtW, 0, h, engine.CamBottom)
            art.DrawWallColumn(g, art.WallNear, JumpKnightEngine.WorldW - PillarW, 0, PillarArtW, 0, h, engine.CamBottom)
        End If
        g.FillRectangle(leftShade, PillarW, 0, 26, h)
        g.FillRectangle(rightShade, JumpKnightEngine.WorldW - PillarW - 26, 0, 26, h)
    End Sub

    ' ---------- Platforms: the picture is always scaled to the platform's real (collision) width ----------

    Private Sub DrawPlatforms(g As Graphics)
        For Each p As JKPlatform In engine.Platforms
            Dim top As Single = SY(p.Y)
            If top > JumpKnightEngine.ViewH + 80 OrElse top < -80 Then Continue For
            Dim x As Integer = CInt(Math.Round(p.X))
            Dim y As Integer = CInt(Math.Round(top))

            Select Case p.Kind
                Case JKPlatformKind.Stone
                    DrawPlatformSprite(g, art.StoneTile, x, y, p.Width, fallbackStone)
                Case JKPlatformKind.Moving
                    DrawPlatformSprite(g, art.MovingTile, x, y, p.Width, fallbackMoving)
                Case JKPlatformKind.Ice
                    DrawPlatformSprite(g, art.IceTile, x, y, p.Width, fallbackIce)
                Case JKPlatformKind.Wood
                    DrawWood(g, p, x, y)
            End Select
        Next
    End Sub

    ''' <summary>Draws bmp exactly 'width' wide (keeping its aspect ratio), top edge at y.
    ''' The drawn width is the platform's collision width, so what you see is what you land on.</summary>
    Private Sub DrawPlatformSprite(g As Graphics, bmp As Bitmap, x As Integer, y As Integer, width As Single, fallback As Brush)
        If bmp IsNot Nothing Then
            Dim w As Integer = CInt(Math.Round(width))
            Dim h As Integer = Math.Max(1, CInt(Math.Round(bmp.Height * width / bmp.Width)))
            g.DrawImage(bmp, New Rectangle(x, y, w, h), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel)
        Else
            g.FillRectangle(fallback, x, y, width, 14)
        End If
    End Sub

    Private Sub DrawWood(g As Graphics, p As JKPlatform, x As Integer, y As Integer)
        If Not p.IsBreaking Then
            DrawPlatformSprite(g, art.WoodLog, x, y, p.Width, fallbackWood)
            Return
        End If

        Const shakeTime As Single = 0.12F
        If p.BreakTime < shakeTime Then
            Dim jitter As Integer = If((CInt(p.BreakTime * 60.0F) Mod 2) = 0, -2, 2)
            DrawPlatformSprite(g, art.WoodLog, x + jitter, y, p.Width, fallbackWood)
        Else
            Dim t As Single = p.BreakTime - shakeTime
            Dim fall As Integer = CInt(0.5F * 900.0F * t * t)
            Dim drift As Integer = CInt(45.0F * t)
            If art.WoodLog IsNot Nothing AndAlso art.WoodLeft IsNot Nothing AndAlso art.WoodRight IsNot Nothing Then
                Dim sc As Single = p.Width / art.WoodLog.Width
                Dim halfW As Integer = CInt(Math.Round(art.WoodLeft.Width * sc))
                Dim hh As Integer = Math.Max(1, CInt(Math.Round(art.WoodLeft.Height * sc)))
                g.DrawImage(art.WoodLeft, New Rectangle(x - drift, y + fall, halfW, hh), 0, 0, art.WoodLeft.Width, art.WoodLeft.Height, GraphicsUnit.Pixel)
                g.DrawImage(art.WoodRight, New Rectangle(x + halfW + drift, y + fall, halfW, hh), 0, 0, art.WoodRight.Width, art.WoodRight.Height, GraphicsUnit.Pixel)
            Else
                g.FillRectangle(fallbackWood, x - drift, y + fall, CInt(p.Width / 2), 14)
                g.FillRectangle(fallbackWood, x + CInt(p.Width / 2) + drift, y + fall, CInt(p.Width / 2), 14)
            End If
        End If
    End Sub

    Private Sub DrawPowerUps(g As Graphics)
        For Each pu As JKPowerUp In engine.PowerUps
            Dim bob As Single = CSng(Math.Sin(engine.AnimClock * 4.0F + pu.X) * 3.0)
            Dim topY As Single = SY(pu.Y + pu.Height) - 4.0F - bob
            If topY > JumpKnightEngine.ViewH + 40 OrElse topY < -60 Then Continue For
            Dim leftX As Single = pu.X - pu.Width / 2.0F

            g.FillEllipse(glowBrush, leftX - 8, topY - 6, pu.Width + 16, pu.Height + 12)

            Dim bmp As Bitmap = If(pu.Kind = JKPowerUpKind.Hammer, art.HammerItem, art.MeatItem)
            If bmp IsNot Nothing Then
                g.DrawImage(bmp, CInt(leftX), CInt(topY), CInt(pu.Width), CInt(pu.Height))
            Else
                g.FillRectangle(goldBrush, leftX, topY, pu.Width, pu.Height)
            End If
        Next
    End Sub

    Private Sub DrawEnemies(g As Graphics)
        For Each en As JKEnemy In engine.Enemies
            Dim centreY As Single = SY(en.Y)
            If centreY > JumpKnightEngine.ViewH + 60 OrElse centreY < -60 Then Continue For
            Dim frame As Integer = CInt(Math.Floor(engine.AnimClock * 8.0F + en.Phase)) And 1

            If en.Hit Then
                ' Defeated bat: spins while it falls away.
                Dim bmpHit As Bitmap = art.BatFrames(0)
                Dim st As Drawing2D.GraphicsState = g.Save()
                g.TranslateTransform(en.X, centreY)
                g.RotateTransform(en.HitTime * 600.0F)
                If bmpHit IsNot Nothing Then
                    g.DrawImage(bmpHit, -16, -16, 32, 32)
                Else
                    g.FillEllipse(fallbackBat, -14, -10, 28, 20)
                End If
                g.Restore(st)
            Else
                Dim bmp As Bitmap = art.BatFrames(frame)
                If bmp IsNot Nothing Then
                    g.DrawImage(bmp, CInt(en.X - 16), CInt(centreY - 16), 32, 32)
                Else
                    g.FillEllipse(fallbackBat, en.X - 14, centreY - 10, 28, 20)
                End If
            End If
        Next
    End Sub

    Private Sub DrawKnight(g As Graphics)
        Dim scale As Integer = JumpKnightAssets.ArtScale
        Dim strip As Bitmap
        Dim frameIndex As Integer
        Dim feetArtY As Integer = JumpKnightAssets.KnightFrameH

        If engine.State = JKState.Ready OrElse engine.State = JKState.GameOver Then
            strip = If(engine.FacingLeft, art.KnightIdleFlip, art.KnightIdle)
            frameIndex = CInt(Math.Floor(engine.AnimClock * 8.0F)) Mod JumpKnightAssets.IdleFrames
        ElseIf engine.BounceTimer > 0.0F AndAlso engine.Steering Then
            strip = If(engine.FacingLeft, art.KnightWalkFlip, art.KnightWalk)
            frameIndex = CInt(Math.Floor(engine.AnimClock * 16.0F)) Mod JumpKnightAssets.WalkFrames
        Else
            strip = If(engine.FacingLeft, art.KnightJumpFlip, art.KnightJump)
            feetArtY = 31
            If engine.VelY > 350.0F Then
                frameIndex = 0
            ElseIf engine.VelY > 0.0F Then
                frameIndex = 1
            ElseIf engine.VelY > -250.0F Then
                frameIndex = 2
            Else
                frameIndex = 3
            End If
        End If

        Dim footX As Single = engine.KnightX
        Dim footY As Single = SY(engine.KnightFeetY)

        If strip Is Nothing Then
            g.FillRectangle(fallbackKnight, footX - JumpKnightEngine.KnightW / 2.0F, footY - JumpKnightEngine.KnightH,
                            JumpKnightEngine.KnightW, JumpKnightEngine.KnightH)
            Return
        End If

        Dim centreArtX As Single = If(engine.FacingLeft, 25.5F, 18.5F)
        Dim destX As Integer = CInt(Math.Round(footX - centreArtX * scale))
        Dim destY As Integer = CInt(Math.Round(footY - feetArtY * scale))
        Dim src As New Rectangle(frameIndex * JumpKnightAssets.KnightFrameW, 0, JumpKnightAssets.KnightFrameW, JumpKnightAssets.KnightFrameH)
        Dim dest As New Rectangle(destX, destY, JumpKnightAssets.KnightFrameW * scale, JumpKnightAssets.KnightFrameH * scale)
        g.DrawImage(strip, dest, src, GraphicsUnit.Pixel)

        If footX < 40 Then
            g.DrawImage(strip, New Rectangle(dest.X + CInt(JumpKnightEngine.WorldW + JumpKnightEngine.KnightW), dest.Y, dest.Width, dest.Height), src, GraphicsUnit.Pixel)
        ElseIf footX > JumpKnightEngine.WorldW - 40 Then
            g.DrawImage(strip, New Rectangle(dest.X - CInt(JumpKnightEngine.WorldW + JumpKnightEngine.KnightW), dest.Y, dest.Width, dest.Height), src, GraphicsUnit.Pixel)
        End If
    End Sub

    ' ---------- Sword slash + bat impact ----------

    ''' <summary>
    ''' A bright crescent swept in front of the knight. Its radius is the engine's AttackReach, so
    ''' what you see is what can hit a bat.
    ''' </summary>
    Private Sub DrawAttack(g As Graphics)
        If engine.AttackTime <= 0.0F Then Return

        Dim t As Single = 1.0F - engine.AttackTime / JumpKnightEngine.AttackDuration
        t = Math.Max(0.0F, Math.Min(1.0F, t))

        Dim head As Single = -70.0F + 140.0F * t            ' degrees, swings top -> bottom
        Dim tail As Single = Math.Max(-70.0F, head - 80.0F)
        Dim sweep As Single = head - tail
        If sweep < 2.0F Then Return

        Dim cx As Single = engine.KnightX
        Dim cy As Single = SY(engine.KnightFeetY) - JumpKnightEngine.AttackCenterY
        Dim r As Single = JumpKnightEngine.AttackReach - 3.0F

        ' Right-facing angles are used as-is; left-facing mirrors them.
        Dim startAngle As Single = tail
        Dim sweepAngle As Single = sweep
        If engine.AttackLeft Then
            startAngle = 180.0F - tail
            sweepAngle = -sweep
        End If

        g.DrawArc(slashPenOuter, cx - r, cy - r, r * 2.0F, r * 2.0F, startAngle, sweepAngle)
        Dim r2 As Single = r - 5.0F
        g.DrawArc(slashPenMid, cx - r2, cy - r2, r2 * 2.0F, r2 * 2.0F, startAngle, sweepAngle)
        Dim r3 As Single = r - 9.0F
        g.DrawArc(slashPenInner, cx - r3, cy - r3, r3 * 2.0F, r3 * 2.0F, startAngle, sweepAngle)
    End Sub


    Private Sub DrawImpacts(g As Graphics)
        For Each imp As JKImpact In engine.Impacts
            Dim sx As Single = imp.X
            Dim screenY As Single = SY(imp.Y)

            If screenY > JumpKnightEngine.ViewH + 40 OrElse screenY < -40 Then Continue For

            Dim a As Single = imp.Age

            ' Spark burst
            If a < 0.3F Then
                Dim r1 As Single = 6.0F + a * 90.0F
                Dim r2 As Single = r1 + 9.0F * (1.0F - a / 0.3F)

                For i As Integer = 0 To 7
                    Dim ang As Double = i * Math.PI / 4.0

                    g.DrawLine(sparkPen,
                           sx + CSng(Math.Cos(ang)) * r1,
                           screenY + CSng(Math.Sin(ang)) * r1,
                           sx + CSng(Math.Cos(ang)) * r2,
                           screenY + CSng(Math.Sin(ang)) * r2)
                Next
            End If

            ' Floating points
            If a < 0.5F Then
                DrawShadowText(
                g,
                "+" & JumpKnightEngine.BatPoints.ToString(),
                smallFont,
                goldBrush,
                sx - 12.0F,
                screenY - 26.0F - a * 40.0F
            )
            End If
        Next
    End Sub

    ' ===================== HUD and overlays =====================

    Private Sub DrawShadowText(g As Graphics, text As String, font As Font, brush As Brush, x As Single, y As Single)
        g.DrawString(text, font, shadowBrush, x + 2, y + 2)
        g.DrawString(text, font, brush, x, y)
    End Sub

    Private Sub DrawCenteredText(g As Graphics, text As String, font As Font, brush As Brush, centreY As Single)
        Dim area As New RectangleF(0, centreY - 30, JumpKnightEngine.WorldW, 60)
        g.DrawString(text, font, shadowBrush, New RectangleF(area.X + 2, area.Y + 2, area.Width, area.Height), centerFormat)
        g.DrawString(text, font, brush, area, centerFormat)
    End Sub

    Private Sub DrawHud(g As Graphics)
        DrawShadowText(g, "SCORE " & engine.Score.ToString(), hudFont, whiteBrush, 44, 10)
        Dim bestText As String = "BEST " & Math.Max(engine.BestScore, engine.Score).ToString()
        Dim bestSize As SizeF = g.MeasureString(bestText, hudFont)
        DrawShadowText(g, bestText, hudFont, goldBrush, JumpKnightEngine.WorldW - 44 - bestSize.Width, 10)

        If engine.BuffTime > 0.0F Then
            If art.MeatItem IsNot Nothing Then g.DrawImage(art.MeatItem, 44, 40, 28, 21)
            g.FillRectangle(barBack, 78, 44, 104, 14)
            g.FillRectangle(barFill, 80, 46, CInt(100.0F * engine.BuffTime / JumpKnightEngine.BuffDuration), 10)
            DrawShadowText(g, "DOUBLE JUMP: UP / W", smallFont, whiteBrush, 44, 64)
        End If

        If engine.SpringFlash > 0.0F Then DrawCenteredText(g, "SUPER JUMP!", hudFont, goldBrush, 130)

        If engine.State = JKState.Ready AndAlso Overlay = JKOverlay.None Then
            DrawCenteredText(g, "ARROW KEYS or A / D to start", hudFont, whiteBrush, 250)
            DrawCenteredText(g, "SPACE: sword     P: pause", smallFont, goldBrush, 285)
        End If
    End Sub

    Private Sub DrawPauseButton(g As Graphics)
        If Not PauseButtonVisible() Then Return
        Dim r As Rectangle = PauseRect

        g.FillRectangle(pauseShadow, r.X, r.Bottom - 3, r.Width, 3)
        g.FillRectangle(If(pauseHover, pauseBodyHover, pauseBody), r.X, r.Y, r.Width, r.Height - 3)
        g.DrawRectangle(pauseOutline, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 5)
        g.FillRectangle(whiteBrush, r.X + 10, r.Y + 7, 5, 12)
        g.FillRectangle(whiteBrush, r.X + 18, r.Y + 7, 5, 12)
    End Sub

    Private Sub DrawOverlay(g As Graphics)
        If Overlay = JKOverlay.None Then Return
        g.FillRectangle(dimBrush, 0, 0, JumpKnightEngine.WorldW, JumpKnightEngine.ViewH)

        If Overlay = JKOverlay.Paused Then
            DrawCenteredText(g, "PAUSED", bigFont, whiteBrush, 200)
        Else
            DrawCenteredText(g, "GAME OVER", bigFont, whiteBrush, 150)
            DrawCenteredText(g, "SCORE  " & engine.Score.ToString(), hudFont, whiteBrush, 215)
            DrawCenteredText(g, "BEST  " & engine.BestScore.ToString(), hudFont, goldBrush, 245)
            If engine.NewBest Then DrawCenteredText(g, "NEW BEST!", hudFont, goldBrush, 280)
        End If
    End Sub

    ''' <summary>Big centred 3 / 2 / 1 / GO! shown while the game is frozen (or just resuming).</summary>
    Private Sub DrawCountdown(g As Graphics)
        If CountdownText = "" Then Return
        If CountdownText <> "GO!" Then
            g.FillRectangle(countDimBrush, 0, 0, JumpKnightEngine.WorldW, JumpKnightEngine.ViewH)
        End If

        Dim brush As Brush = If(CountdownText = "GO!", greenBrush, goldBrush)
        Dim area As New RectangleF(0, 150, JumpKnightEngine.WorldW, 200)
        g.DrawString(CountdownText, countFont, shadowBrush, New RectangleF(area.X + 4, area.Y + 4, area.Width, area.Height), centerFormat)
        g.DrawString(CountdownText, countFont, brush, area, centerFormat)
    End Sub

    ' ===================== Cleanup =====================

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            hudFont.Dispose() : smallFont.Dispose() : bigFont.Dispose() : countFont.Dispose()
            shadowBrush.Dispose() : whiteBrush.Dispose() : goldBrush.Dispose() : greenBrush.Dispose() : glowBrush.Dispose()
            darkOverlay.Dispose() : dimBrush.Dispose() : countDimBrush.Dispose() : barBack.Dispose() : barFill.Dispose()
            fallbackStone.Dispose() : fallbackMoving.Dispose() : fallbackWood.Dispose()
            fallbackIce.Dispose() : fallbackBat.Dispose() : fallbackKnight.Dispose()
            pauseBody.Dispose() : pauseBodyHover.Dispose() : pauseShadow.Dispose() : pauseOutline.Dispose()
            slashPenOuter.Dispose() : slashPenMid.Dispose() : slashPenInner.Dispose() : sparkPen.Dispose()
            spriteAttr.Dispose()
            leftShade.Dispose() : rightShade.Dispose() : centerFormat.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class

Public Enum JKOverlay
    None
    Paused
    GameOver
End Enum

''' <summary>
''' Draws the Jump Knight world. It only READS the engine; it never changes game state.
''' The 400 x 600 logical play area is scaled (nearest-neighbour) to fit the panel and centred,
''' so physics never depends on window size.
''' </summary>
Public Class JumpKnightPanel
    Inherits Panel

    Private ReadOnly engine As JumpKnightEngine
    Private ReadOnly art As JumpKnightAssets

    Public Property Overlay As JKOverlay = JKOverlay.None

    ' Cached GDI+ objects (created once, disposed in Dispose)
    Private ReadOnly hudFont As New Font("Segoe UI", 18.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly smallFont As New Font("Segoe UI", 13.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly bigFont As New Font("Segoe UI", 40.0F, FontStyle.Bold, GraphicsUnit.Pixel)
    Private ReadOnly shadowBrush As New SolidBrush(Color.FromArgb(170, 0, 0, 0))
    Private ReadOnly whiteBrush As New SolidBrush(Color.White)
    Private ReadOnly goldBrush As New SolidBrush(Color.FromArgb(255, 230, 120))
    Private ReadOnly glowBrush As New SolidBrush(Color.FromArgb(70, 255, 230, 120))
    Private ReadOnly darkOverlay As New SolidBrush(Color.FromArgb(70, 0, 0, 12))
    Private ReadOnly dimBrush As New SolidBrush(Color.FromArgb(165, 0, 0, 0))
    Private ReadOnly barBack As New SolidBrush(Color.FromArgb(150, 0, 0, 0))
    Private ReadOnly barFill As New SolidBrush(Color.FromArgb(235, 120, 80))
    Private ReadOnly fallbackStone As New SolidBrush(Color.FromArgb(120, 130, 120))
    Private ReadOnly fallbackMoving As New SolidBrush(Color.FromArgb(110, 130, 220))
    Private ReadOnly fallbackWood As New SolidBrush(Color.FromArgb(120, 80, 50))
    Private ReadOnly fallbackIce As New SolidBrush(Color.FromArgb(180, 230, 250))
    Private ReadOnly fallbackBat As New SolidBrush(Color.FromArgb(120, 70, 150))
    Private ReadOnly fallbackKnight As New SolidBrush(Color.FromArgb(190, 190, 205))
    Private ReadOnly leftShade As Drawing2D.LinearGradientBrush
    Private ReadOnly rightShade As Drawing2D.LinearGradientBrush
    Private ReadOnly centerFormat As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

    Private Const PillarArtW As Integer = 18      ' art pixels -> 36 world units per side pillar
    Private Const PillarW As Single = PillarArtW * JumpKnightAssets.ArtScale

    Public Sub New(gameEngine As JumpKnightEngine, assets As JumpKnightAssets)
        engine = gameEngine
        art = assets
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True
        Me.BackColor = Color.Black

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

    ''' <summary>Converts a point in the 400 x 600 play area to a pixel position inside this panel.</summary>
    Public Function LogicalToScreen(x As Single, y As Single) As Point
        Dim s As Single = ViewScale()
        Dim offX As Single = (Me.Width - JumpKnightEngine.WorldW * s) / 2.0F
        Dim offY As Single = (Me.Height - JumpKnightEngine.ViewH * s) / 2.0F
        Return New Point(CInt(offX + x * s), CInt(offY + y * s))
    End Function

    ''' <summary>World Y (up) -> play-area Y (down).</summary>
    Private Function SY(worldY As Single) As Single
        Return JumpKnightEngine.ViewH - (worldY - engine.CamBottom)
    End Function

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
        DrawHud(g)
        DrawOverlay(g)

        g.Restore(saved)
        MyBase.OnPaint(e)
    End Sub

    Private Sub DrawBackground(g As Graphics)
        Dim w As Integer = CInt(JumpKnightEngine.WorldW)
        Dim h As Integer = CInt(JumpKnightEngine.ViewH)

        If art.WallFar IsNot Nothing Then
            ' Far wall: scrolls slower than the camera (parallax) = depth.
            art.DrawWallColumn(g, art.WallFar, 0, 4, w \ JumpKnightAssets.ArtScale, 0, h, engine.CamBottom * 0.4)
        Else
            g.FillRectangle(Brushes.Black, 0, 0, w, h)
        End If
        g.FillRectangle(darkOverlay, 0, 0, w, h)

        ' Near stone pillars on both sides scroll 1:1 with the camera.
        If art.WallNear IsNot Nothing Then
            art.DrawWallColumn(g, art.WallNear, 0, 0, PillarArtW, 0, h, engine.CamBottom)
            art.DrawWallColumn(g, art.WallNear, JumpKnightEngine.WorldW - PillarW, 0, PillarArtW, 0, h, engine.CamBottom)
        End If
        g.FillRectangle(leftShade, PillarW, 0, 26, h)
        g.FillRectangle(rightShade, JumpKnightEngine.WorldW - PillarW - 26, 0, 26, h)
    End Sub

    Private Sub DrawPlatforms(g As Graphics)
        For Each p As JKPlatform In engine.Platforms
            Dim top As Single = SY(p.Y)
            If top > JumpKnightEngine.ViewH + 80 OrElse top < -80 Then Continue For
            Dim x As Integer = CInt(Math.Round(p.X))
            Dim y As Integer = CInt(Math.Round(top))

            Select Case p.Kind
                Case JKPlatformKind.Stone
                    DrawSprite(g, art.StoneTile, x, y, JumpKnightAssets.ArtScale, fallbackStone, p.Width)
                Case JKPlatformKind.Moving
                    DrawSprite(g, art.MovingTile, x, y, JumpKnightAssets.ArtScale, fallbackMoving, p.Width)
                Case JKPlatformKind.Ice
                    DrawSprite(g, art.IceTile, x, y, JumpKnightAssets.ArtScale, fallbackIce, p.Width)
                Case JKPlatformKind.Wood
                    DrawWood(g, p, x, y)
            End Select
        Next
    End Sub

    Private Sub DrawWood(g As Graphics, p As JKPlatform, x As Integer, y As Integer)
        If Not p.IsBreaking Then
            DrawSprite(g, art.WoodLog, x, y, 1, fallbackWood, p.Width)
            Return
        End If

        Const shakeTime As Single = 0.12F
        If p.BreakTime < shakeTime Then
            ' Crack phase: the log shakes.
            Dim jitter As Integer = If((CInt(p.BreakTime * 60.0F) Mod 2) = 0, -2, 2)
            DrawSprite(g, art.WoodLog, x + jitter, y, 1, fallbackWood, p.Width)
        Else
            ' Break phase: two halves drop and drift apart.
            Dim t As Single = p.BreakTime - shakeTime
            Dim fall As Integer = CInt(0.5F * 900.0F * t * t)
            Dim drift As Integer = CInt(45.0F * t)
            If art.WoodLeft IsNot Nothing AndAlso art.WoodRight IsNot Nothing Then
                g.DrawImage(art.WoodLeft, x - drift, y + fall, art.WoodLeft.Width, art.WoodLeft.Height)
                g.DrawImage(art.WoodRight, x + art.WoodLeft.Width + drift, y + fall, art.WoodRight.Width, art.WoodRight.Height)
            Else
                g.FillRectangle(fallbackWood, x - drift, y + fall, CInt(p.Width / 2), 14)
                g.FillRectangle(fallbackWood, x + CInt(p.Width / 2) + drift, y + fall, CInt(p.Width / 2), 14)
            End If
        End If
    End Sub

    ''' <summary>Draws a bitmap at an integer scale, or a plain rectangle if the image is missing.</summary>
    Private Sub DrawSprite(g As Graphics, bmp As Bitmap, x As Integer, y As Integer, pixelScale As Integer,
                           fallback As Brush, fallbackWidth As Single)
        If bmp IsNot Nothing Then
            g.DrawImage(bmp, x, y, bmp.Width * pixelScale, bmp.Height * pixelScale)
        Else
            g.FillRectangle(fallback, x, y, fallbackWidth, 14)
        End If
    End Sub

    Private Sub DrawPowerUps(g As Graphics)
        For Each pu As JKPowerUp In engine.PowerUps
            Dim bob As Single = CSng(Math.Sin(engine.AnimClock * 4.0F + pu.X) * 3.0)
            Dim topY As Single = SY(pu.Y + pu.Height) - 4.0F - bob
            If topY > JumpKnightEngine.ViewH + 40 OrElse topY < -60 Then Continue For
            Dim leftX As Single = pu.X - pu.Width / 2.0F

            ' Soft glow so items stand out against the dark stone.
            g.FillEllipse(glowBrush, leftX - 8, topY - 6, pu.Width + 16, pu.Height + 12)

            Dim bmp As Bitmap = If(pu.Kind = JKPowerUpKind.Hammer, art.HammerItem, art.MeatItem)
            If bmp IsNot Nothing Then
                ' Drawn at its proper aspect ratio (meat is scaled slightly smaller to fit the platform).
                g.DrawImage(bmp, CInt(leftX), CInt(topY), CInt(pu.Width), CInt(pu.Height))
            Else
                g.FillRectangle(goldBrush, leftX, topY, pu.Width, pu.Height)
            End If
        Next
    End Sub

    Private Sub DrawEnemies(g As Graphics)
        For Each en As JKEnemy In engine.Enemies
            Dim top As Single = SY(en.Y) - 16.0F
            If top > JumpKnightEngine.ViewH + 40 OrElse top < -60 Then Continue For
            Dim frame As Integer = CInt(Math.Floor(engine.AnimClock * 8.0F + en.Phase)) And 1
            Dim bmp As Bitmap = art.BatFrames(frame)
            If bmp IsNot Nothing Then
                g.DrawImage(bmp, CInt(en.X - 16), CInt(top), 32, 32)
            Else
                g.FillEllipse(fallbackBat, en.X - 14, top + 6, 28, 20)
            End If
        Next
    End Sub

    Private Sub DrawKnight(g As Graphics)
        Dim scale As Integer = JumpKnightAssets.ArtScale
        Dim strip As Bitmap
        Dim frameIndex As Integer
        Dim feetArtY As Integer = JumpKnightAssets.KnightFrameH   ' bottom row of the frame art

        If engine.State = JKState.Ready OrElse engine.State = JKState.GameOver Then
            ' Standing still: idle animation.
            strip = If(engine.FacingLeft, art.KnightIdleFlip, art.KnightIdle)
            frameIndex = CInt(Math.Floor(engine.AnimClock * 8.0F)) Mod JumpKnightAssets.IdleFrames
        ElseIf engine.BounceTimer > 0.0F AndAlso engine.Steering Then
            ' Just pushed off a platform while steering: walk cycle.
            strip = If(engine.FacingLeft, art.KnightWalkFlip, art.KnightWalk)
            frameIndex = CInt(Math.Floor(engine.AnimClock * 16.0F)) Mod JumpKnightAssets.WalkFrames
        Else
            ' In the air: jump frames follow the vertical speed (rising -> apex -> falling).
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

        ' The sprite's body centre is at art x = 18.5 (facing right) or 25.5 (mirrored).
        Dim centreArtX As Single = If(engine.FacingLeft, 25.5F, 18.5F)
        Dim destX As Integer = CInt(Math.Round(footX - centreArtX * scale))
        Dim destY As Integer = CInt(Math.Round(footY - feetArtY * scale))
        Dim src As New Rectangle(frameIndex * JumpKnightAssets.KnightFrameW, 0, JumpKnightAssets.KnightFrameW, JumpKnightAssets.KnightFrameH)
        Dim dest As New Rectangle(destX, destY, JumpKnightAssets.KnightFrameW * scale, JumpKnightAssets.KnightFrameH * scale)
        g.DrawImage(strip, dest, src, GraphicsUnit.Pixel)

        ' Wrapped around an edge? Draw a second copy on the other side so it slides through smoothly.
        If footX < 40 Then
            g.DrawImage(strip, New Rectangle(dest.X + CInt(JumpKnightEngine.WorldW + JumpKnightEngine.KnightW), dest.Y, dest.Width, dest.Height), src, GraphicsUnit.Pixel)
        ElseIf footX > JumpKnightEngine.WorldW - 40 Then
            g.DrawImage(strip, New Rectangle(dest.X - CInt(JumpKnightEngine.WorldW + JumpKnightEngine.KnightW), dest.Y, dest.Width, dest.Height), src, GraphicsUnit.Pixel)
        End If
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

        ' Meat buff indicator: icon + shrinking bar + hint.
        If engine.BuffTime > 0.0F Then
            If art.MeatItem IsNot Nothing Then g.DrawImage(art.MeatItem, 44, 40, 28, 21)
            g.FillRectangle(barBack, 78, 44, 104, 14)
            g.FillRectangle(barFill, 80, 46, CInt(100.0F * engine.BuffTime / JumpKnightEngine.BuffDuration), 10)
            DrawShadowText(g, "DOUBLE JUMP: SPACE / UP / W", smallFont, whiteBrush, 44, 64)
        End If

        If engine.SpringFlash > 0.0F Then DrawCenteredText(g, "SUPER JUMP!", hudFont, goldBrush, 130)

        If engine.State = JKState.Ready AndAlso Overlay = JKOverlay.None Then
            DrawCenteredText(g, "ARROW KEYS or A / D to start", hudFont, whiteBrush, 250)
        End If
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

    ' ===================== Cleanup =====================

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            hudFont.Dispose() : smallFont.Dispose() : bigFont.Dispose()
            shadowBrush.Dispose() : whiteBrush.Dispose() : goldBrush.Dispose() : glowBrush.Dispose()
            darkOverlay.Dispose() : dimBrush.Dispose() : barBack.Dispose() : barFill.Dispose()
            fallbackStone.Dispose() : fallbackMoving.Dispose() : fallbackWood.Dispose()
            fallbackIce.Dispose() : fallbackBat.Dispose() : fallbackKnight.Dispose()
            leftShade.Dispose() : rightShade.Dispose() : centerFormat.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
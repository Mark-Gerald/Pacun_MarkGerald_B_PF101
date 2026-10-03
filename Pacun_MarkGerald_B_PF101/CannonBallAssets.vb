Option Strict On
Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

''' <summary>
''' Loads (and caches) every Cannon Ball picture.
'''
''' For each picture it first looks for a PNG in   Assets\CannonBall\   (next to the program).
'''     Background.png   400 x 640   whole playfield + side pillars (the HUD bar is drawn on top of it)
'''     HudBar.png       400 x  40   top HUD strip
'''     Brick.png  Stone.png  StoneDamaged.png  Gold.png  Emerald.png     23 x 13 each (any size is scaled)
'''     Ball.png         10 x 10     (any square size is scaled to the ball's diameter)
'''     Cart.png         60 x 26     (width is scaled to the cart width; the barrel/top sits above the hitbox)
'''     LifeIcon.png      9 x  9     HUD life icon
''' If a PNG is missing, a pixel-art placeholder is generated in code instead, so the game always runs.
''' </summary>
Public Class CannonBallAssets
    Implements IDisposable

    Public ReadOnly Background As Bitmap
    Public ReadOnly HudBar As Bitmap
    Public ReadOnly Brick As Bitmap
    Public ReadOnly Stone As Bitmap
    Public ReadOnly StoneDamaged As Bitmap
    Public ReadOnly Gold As Bitmap
    Public ReadOnly Emerald As Bitmap
    Public ReadOnly Ball As Bitmap
    Public ReadOnly Cart As Bitmap
    Public ReadOnly LifeIcon As Bitmap

    Private Shared ReadOnly BW As Integer = CInt(CannonBallLevelData.BlockW)
    Private Shared ReadOnly BH As Integer = CInt(CannonBallLevelData.BlockH)

    Public Sub New()
        Background = If(TryLoad("Background.png"), BuildBackground())
        HudBar = If(TryLoad("HudBar.png"), BuildHudBar())
        Brick = If(TryLoad("Brick.png"), BuildBrick())
        Stone = If(TryLoad("Stone.png"), BuildStone(False))
        StoneDamaged = If(TryLoad("StoneDamaged.png"), BuildStone(True))
        Gold = If(TryLoad("Gold.png"), BuildGold())
        Emerald = If(TryLoad("Emerald.png"), BuildEmerald())
        Ball = If(TryLoad("Ball.png"), BuildBall(10))
        Cart = If(TryLoad("Cart.png"), BuildCart())
        LifeIcon = If(TryLoad("LifeIcon.png"), BuildBall(9))
    End Sub

    ' ===================== Loading =====================

    Private Shared Function TryLoad(fileName As String) As Bitmap
        Dim path As String = IO.Path.Combine(AudioManager.AssetsRoot, "CannonBall", fileName)
        If Not IO.File.Exists(path) Then Return Nothing
        Try
            Using src As New Bitmap(path)
                Return New Bitmap(src)       ' copy, so the file is not locked
            End Using
        Catch ex As Exception
            Debug.WriteLine("CannonBallAssets: could not load " & path & " - " & ex.Message)
            Return Nothing
        End Try
    End Function

    ' ===================== Small drawing helpers =====================

    Private Shared Sub Fill(g As Graphics, c As Color, x As Integer, y As Integer, w As Integer, h As Integer)
        Using b As New SolidBrush(c)
            g.FillRectangle(b, x, y, w, h)
        End Using
    End Sub

    Private Shared Function NewCrispGraphics(bmp As Bitmap) As Graphics
        Dim g As Graphics = Graphics.FromImage(bmp)
        g.SmoothingMode = SmoothingMode.None
        g.PixelOffsetMode = PixelOffsetMode.Half
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        Return g
    End Function

    ''' <summary>Pixel-perfect filled circle with a darker rim.</summary>
    Private Shared Sub PixelDisc(bmp As Bitmap, cx As Single, cy As Single, r As Single, fill As Color, rim As Color)
        For y As Integer = 0 To bmp.Height - 1
            For x As Integer = 0 To bmp.Width - 1
                Dim d As Double = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy))
                If d <= r Then bmp.SetPixel(x, y, If(d > r - 1.2, rim, fill))
            Next
        Next
    End Sub

    ' ===================== Blocks (23 x 13) =====================

    Private Shared Sub Bevel(g As Graphics, baseC As Color, lightC As Color, darkC As Color, outlineC As Color)
        Fill(g, outlineC, 0, 0, BW, BH)
        Fill(g, baseC, 1, 1, BW - 2, BH - 2)
        Fill(g, lightC, 1, 1, BW - 2, 1)
        Fill(g, lightC, 1, 1, 1, BH - 2)
        Fill(g, darkC, 1, BH - 2, BW - 2, 1)
        Fill(g, darkC, BW - 2, 1, 1, BH - 2)
    End Sub

    Private Shared Function BuildBrick() As Bitmap
        Dim bmp As New Bitmap(BW, BH, PixelFormat.Format32bppArgb)
        Using g As Graphics = NewCrispGraphics(bmp)
            Bevel(g, Color.FromArgb(190, 84, 48), Color.FromArgb(236, 134, 84), Color.FromArgb(120, 48, 32), Color.FromArgb(60, 24, 18))
            Dim mortar As Color = Color.FromArgb(110, 50, 34)
            Fill(g, mortar, 1, 6, BW - 2, 1)
            Fill(g, mortar, 11, 2, 1, 4)
            Fill(g, mortar, 6, 7, 1, 4)
            Fill(g, mortar, 17, 7, 1, 4)
        End Using
        Return bmp
    End Function

    Private Shared Function BuildStone(damaged As Boolean) As Bitmap
        Dim bmp As New Bitmap(BW, BH, PixelFormat.Format32bppArgb)
        Using g As Graphics = NewCrispGraphics(bmp)
            Bevel(g, Color.FromArgb(118, 132, 148), Color.FromArgb(168, 182, 196), Color.FromArgb(74, 86, 102), Color.FromArgb(34, 40, 52))
            Dim moss As Color = Color.FromArgb(70, 140, 70)
            Fill(g, moss, 2, 1, 4, 1)
            Fill(g, moss, 14, 1, 3, 1)
            Fill(g, moss, 1, 5, 1, 3)
            Fill(g, Color.FromArgb(140, 154, 170), 11, 3, 1, 7)
        End Using
        If damaged Then
            Dim crack As Color = Color.FromArgb(24, 30, 40)
            Dim pts As Integer(,) = {{11, 1}, {10, 2}, {10, 3}, {9, 4}, {10, 5}, {11, 6}, {11, 7}, {12, 8}, {12, 9}, {13, 10}, {12, 11},
                                     {9, 4}, {8, 4}, {7, 5}, {13, 10}, {14, 10}, {15, 9}}
            For i As Integer = 0 To pts.GetLength(0) - 1
                bmp.SetPixel(pts(i, 0), pts(i, 1), crack)
            Next
        End If
        Return bmp
    End Function

    Private Shared Function BuildGold() As Bitmap
        Dim bmp As New Bitmap(BW, BH, PixelFormat.Format32bppArgb)
        Using g As Graphics = NewCrispGraphics(bmp)
            Bevel(g, Color.FromArgb(236, 184, 36), Color.FromArgb(255, 236, 130), Color.FromArgb(160, 110, 16), Color.FromArgb(80, 52, 8))
        End Using
        For y As Integer = 2 To BH - 3
            For x As Integer = 2 To BW - 3
                If ((x + y) Mod 9) < 2 Then bmp.SetPixel(x, y, Color.FromArgb(255, 246, 170))
            Next
        Next
        bmp.SetPixel(4, 3, Color.White)
        bmp.SetPixel(5, 3, Color.White)
        Return bmp
    End Function

    Private Shared Function BuildEmerald() As Bitmap
        Dim bmp As New Bitmap(BW, BH, PixelFormat.Format32bppArgb)
        Using g As Graphics = NewCrispGraphics(bmp)
            Bevel(g, Color.FromArgb(28, 160, 104), Color.FromArgb(110, 236, 176), Color.FromArgb(10, 84, 64), Color.FromArgb(4, 40, 32))
            Using p As New Pen(Color.FromArgb(120, 240, 190))
                g.DrawLine(p, 11, 2, 19, 6)
                g.DrawLine(p, 19, 6, 11, 10)
                g.DrawLine(p, 11, 10, 3, 6)
                g.DrawLine(p, 3, 6, 11, 2)
            End Using
            Fill(g, Color.FromArgb(70, 210, 150), 9, 5, 5, 3)
            Fill(g, Color.White, 6, 3, 2, 1)
        End Using
        Return bmp
    End Function

    ' ===================== Ball / life icon =====================

    Private Shared Function BuildBall(diameter As Integer) As Bitmap
        Dim bmp As New Bitmap(diameter, diameter, PixelFormat.Format32bppArgb)
        Dim c As Single = (diameter - 1) / 2.0F
        PixelDisc(bmp, c, c, diameter / 2.0F, Color.FromArgb(52, 56, 64), Color.FromArgb(18, 20, 24))
        Dim hl As Color = Color.FromArgb(170, 180, 195)
        bmp.SetPixel(2, 2, hl)
        bmp.SetPixel(3, 2, hl)
        bmp.SetPixel(2, 3, hl)
        Return bmp
    End Function

    ' ===================== Cart (60 x 26): barrel on top, wood body, two wheels =====================

    Private Shared Function BuildCart() As Bitmap
        Dim bmp As New Bitmap(60, 26, PixelFormat.Format32bppArgb)
        Using g As Graphics = NewCrispGraphics(bmp)
            ' cannon barrel (the ball rests in its mouth)
            Fill(g, Color.FromArgb(40, 44, 50), 22, 0, 16, 12)
            Fill(g, Color.FromArgb(66, 72, 80), 23, 1, 14, 11)
            Fill(g, Color.FromArgb(104, 112, 122), 24, 1, 2, 11)
            Fill(g, Color.FromArgb(96, 104, 114), 22, 0, 16, 2)
            Fill(g, Color.FromArgb(28, 30, 34), 25, 0, 10, 1)
            ' wooden body
            Fill(g, Color.FromArgb(70, 44, 24), 0, 8, 60, 14)
            Fill(g, Color.FromArgb(126, 82, 44), 1, 9, 58, 12)
            Fill(g, Color.FromArgb(166, 112, 60), 1, 9, 58, 2)
            Fill(g, Color.FromArgb(86, 54, 30), 1, 19, 58, 2)
            For Each x As Integer In New Integer() {13, 25, 37, 49}
                Fill(g, Color.FromArgb(92, 58, 32), x, 11, 1, 8)
            Next
            Fill(g, Color.FromArgb(230, 180, 60), 4, 13, 2, 2)
            Fill(g, Color.FromArgb(230, 180, 60), 54, 13, 2, 2)
        End Using
        ' wheels
        For Each wx As Single In New Single() {12.0F, 47.0F}
            PixelDisc(bmp, wx, 20.5F, 5.2F, Color.FromArgb(104, 66, 34), Color.FromArgb(40, 24, 12))
            bmp.SetPixel(CInt(wx), 20, Color.FromArgb(220, 170, 60))
            bmp.SetPixel(CInt(wx), 21, Color.FromArgb(220, 170, 60))
        Next
        Return bmp
    End Function

    ' ===================== HUD bar (400 x 40) =====================

    Private Shared Function BuildHudBar() As Bitmap
        Dim bmp As New Bitmap(400, 40, PixelFormat.Format32bppArgb)
        Dim rnd As New Random(11)
        Using g As Graphics = NewCrispGraphics(bmp)
            Fill(g, Color.FromArgb(36, 27, 20), 0, 0, 400, 40)
            For i As Integer = 0 To 60                       ' wood grain
                Fill(g, Color.FromArgb(28, 20, 14), rnd.Next(0, 396), rnd.Next(0, 34), rnd.Next(6, 30), 1)
            Next
            For x As Integer = 0 To 400 Step 50
                Fill(g, Color.FromArgb(24, 17, 12), x, 0, 1, 36)
            Next
            Fill(g, Color.FromArgb(84, 60, 34), 0, 0, 400, 2)
            Fill(g, Color.FromArgb(44, 104, 54), 0, 36, 400, 2)     ' moss edge
            Fill(g, Color.FromArgb(14, 11, 8), 0, 38, 400, 2)
            For Each x As Integer In New Integer() {4, 392}         ' gold studs
                Fill(g, Color.FromArgb(226, 176, 52), x, 4, 4, 4)
                Fill(g, Color.FromArgb(255, 226, 120), x, 4, 2, 2)
            Next
        End Using
        Return bmp
    End Function

    ' ===================== Jungle background (400 x 640, drawn at half size then enlarged = chunky pixels) =====================

    Private Shared Function BuildBackground() As Bitmap
        Const LW As Integer = 200
        Const LH As Integer = 320
        Dim rnd As New Random(7)
        Dim result As New Bitmap(400, 640, PixelFormat.Format32bppArgb)

        Using low As New Bitmap(LW, LH, PixelFormat.Format32bppArgb)
            Using g As Graphics = NewCrispGraphics(low)
                Using br As New LinearGradientBrush(New Rectangle(0, 0, LW, LH), Color.FromArgb(5, 18, 24), Color.FromArgb(10, 40, 30), LinearGradientMode.Vertical)
                    g.FillRectangle(br, 0, 0, LW, LH)
                End Using

                ' glow behind the temple
                Using gb As New SolidBrush(Color.FromArgb(28, 60, 200, 130))
                    g.FillEllipse(gb, 40, 120, 120, 120)
                    g.FillEllipse(gb, 60, 140, 80, 80)
                End Using

                ' distant stepped temple
                For i As Integer = 0 To 6
                    Fill(g, Color.FromArgb(12, 32, 32), 100 - (12 + 8 * i), 150 + 14 * i, 24 + 16 * i, 14)
                Next
                Fill(g, Color.FromArgb(12, 32, 32), 92, 138, 16, 12)
                Fill(g, Color.FromArgb(4, 12, 14), 92, 236, 16, 28)
                Fill(g, Color.FromArgb(40, 150, 100), 98, 230, 4, 2)

                ' tree trunks
                For i As Integer = 0 To 7
                    Dim tx As Integer = rnd.Next(10, 188)
                    Fill(g, Color.FromArgb(8, 22, 16), tx, 0, rnd.Next(4, 8), 300)
                    Fill(g, Color.FromArgb(14, 36, 24), tx, 0, 1, 300)
                Next

                ' foliage: top canopy and bottom bushes
                Dim greens() As Color = {Color.FromArgb(10, 46, 28), Color.FromArgb(14, 62, 36), Color.FromArgb(8, 34, 22)}
                For i As Integer = 0 To 34
                    Using fb As New SolidBrush(greens(rnd.Next(0, 3)))
                        g.FillEllipse(fb, rnd.Next(-10, 190), rnd.Next(-14, 22), rnd.Next(24, 50), rnd.Next(14, 30))
                    End Using
                Next
                For i As Integer = 0 To 22
                    Using fb As New SolidBrush(greens(rnd.Next(0, 3)))
                        g.FillEllipse(fb, rnd.Next(-10, 190), rnd.Next(290, 312), rnd.Next(24, 46), rnd.Next(14, 26))
                    End Using
                Next

                ' hanging vines
                For i As Integer = 0 To 15
                    Dim vx As Integer = rnd.Next(10, 190)
                    Dim len As Integer = rnd.Next(24, 110)
                    For y As Integer = 10 To len
                        If y Mod 9 = 0 Then vx += rnd.Next(-1, 2)
                        Fill(g, Color.FromArgb(34, 98, 52), vx, y, 1, 1)
                        If y Mod 14 = 0 Then Fill(g, Color.FromArgb(62, 152, 74), vx + 1, y, 2, 2)
                    Next
                Next

                ' light shafts
                Using lb As New SolidBrush(Color.FromArgb(14, 255, 240, 170))
                    For i As Integer = 0 To 2
                        Dim sx As Integer = 30 + i * 55
                        g.FillPolygon(lb, New Point() {New Point(sx, 0), New Point(sx + 10, 0), New Point(sx + 46, 300), New Point(sx + 14, 300)})
                    Next
                End Using

                ' ground
                Fill(g, Color.FromArgb(8, 22, 14), 0, 304, LW, 16)
                For x As Integer = 0 To LW Step 5
                    Fill(g, Color.FromArgb(18, 56, 30), x, 302 - rnd.Next(0, 4), 5, 6)
                Next

                ' mossy stone pillars on both sides (8 low-res px = 16 logical px)
                For Each px As Integer In New Integer() {0, LW - 8}
                    Fill(g, Color.FromArgb(62, 70, 60), px, 0, 8, LH)
                    For y As Integer = 0 To LH Step 16
                        Fill(g, Color.FromArgb(40, 46, 40), px, y, 8, 2)
                    Next
                    Fill(g, Color.FromArgb(90, 100, 84), If(px = 0, px + 1, px + 6), 0, 1, LH)
                    Fill(g, Color.FromArgb(24, 28, 24), If(px = 0, px + 7, px), 0, 1, LH)
                    For i As Integer = 0 To 14
                        Fill(g, Color.FromArgb(50, 112, 56), px + rnd.Next(0, 6), rnd.Next(0, LH), rnd.Next(1, 4), rnd.Next(1, 4))
                    Next
                Next
            End Using

            Using g2 As Graphics = Graphics.FromImage(result)
                g2.InterpolationMode = InterpolationMode.NearestNeighbor
                g2.PixelOffsetMode = PixelOffsetMode.Half
                g2.DrawImage(low, New Rectangle(0, 0, 400, 640), 0, 0, LW, LH, GraphicsUnit.Pixel)
            End Using
        End Using
        Return result
    End Function

    ' ===================== Cleanup =====================

    Public Sub Dispose() Implements IDisposable.Dispose
        Background.Dispose() : HudBar.Dispose()
        Brick.Dispose() : Stone.Dispose() : StoneDamaged.Dispose() : Gold.Dispose() : Emerald.Dispose()
        Ball.Dispose() : Cart.Dispose() : LifeIcon.Dispose()
    End Sub

End Class
Imports System.Drawing.Imaging

''' <summary>
''' Loads and caches every image Jump Knight needs, ONCE. Nothing here runs per frame.
'''
''' Files are searched by NAME anywhere under the shared Assets folder
''' (AudioManager.AssetsRoot = &lt;exe folder&gt;\Assets), so it does not matter which sub-folder
''' you put them in. Anything missing is recorded in Missing and the game draws a simple
''' fallback shape instead of crashing.
'''
''' All slice coordinates below were measured from your actual PNG files.
''' </summary>
Public Class JumpKnightAssets
    Implements IDisposable

    ''' <summary>How many world units one art pixel covers (pixel-art scale).</summary>
    Public Const ArtScale As Integer = 2

    Public Const KnightFrameW As Integer = 44
    Public Const KnightFrameH As Integer = 34
    Public Const IdleFrames As Integer = 7
    Public Const JumpFrames As Integer = 4
    Public Const WalkFrames As Integer = 8

    ' Wall period: tile (104 px) + its mirror = 208 px wide, so it repeats sideways with no seam.
    Public Const WallPeriodW As Integer = 208
    Private Const WallTileW As Integer = 104
    Private Const WallTileH As Integer = 46

    ' Knights (facing right). *Flip versions are pre-mirrored frame by frame, so no per-frame flipping.
    Public KnightIdle As Bitmap
    Public KnightIdleFlip As Bitmap
    Public KnightJump As Bitmap
    Public KnightJumpFlip As Bitmap
    Public KnightWalk As Bitmap
    Public KnightWalkFlip As Bitmap

    ' Platforms
    Public StoneTile As Bitmap
    Public MovingTile As Bitmap
    Public IceTile As Bitmap
    Public WoodLog As Bitmap
    Public WoodLeft As Bitmap
    Public WoodRight As Bitmap

    ' Items and enemies
    Public MeatItem As Bitmap
    Public HammerItem As Bitmap
    Public BatFrames(1) As Bitmap

    ' Castle background (vertically repeating "period" bitmaps)
    Public WallNear As Bitmap
    Public WallFar As Bitmap

    ''' <summary>Names of files that could not be found or read.</summary>
    Public ReadOnly Missing As New List(Of String)

    Private ReadOnly owned As New List(Of Bitmap)

    Public Sub New()
        LoadKnight()
        LoadPlatforms()
        LoadItems()
        LoadWalls()
        If Missing.Count > 0 Then
            Debug.WriteLine("JumpKnightAssets: missing -> " & String.Join(", ", Missing))
        End If
    End Sub

    ' ===================== Loading =====================

    Private Sub LoadKnight()
        KnightIdle = LoadStrip("Knight-Idle.png", IdleFrames)
        KnightJump = LoadStrip("Knight-Jump.png", JumpFrames)
        KnightWalk = LoadStrip("Knight-Walk.png", WalkFrames)
        KnightIdleFlip = MirrorFrames(KnightIdle, IdleFrames)
        KnightJumpFlip = MirrorFrames(KnightJump, JumpFrames)
        KnightWalkFlip = MirrorFrames(KnightWalk, WalkFrames)
    End Sub

    Private Sub LoadPlatforms()
        ' Stone: the narrow mossy ledge piece (32 x 29) from MossyRockTileSet.png
        Using sheet As Bitmap = LoadSheet("MossyRockTileSet.png")
            StoneTile = Crop(sheet, New Rectangle(128, 129, 32, 29))
            If StoneTile IsNot Nothing Then
                ' Moving platforms: same stone, tinted blue-violet so they are easy to tell apart.
                MovingTile = Tint(StoneTile, Color.FromArgb(120, 140, 255), 0.32F)
            End If
        End Using

        ' Wood: the splintered log (72 x 18) from wood_env.png, plus its two halves for the break effect.
        Using sheet As Bitmap = LoadSheet("wood_env.png")
            WoodLog = Crop(sheet, New Rectangle(52, 175, 72, 18))
            WoodLeft = Crop(sheet, New Rectangle(52, 175, 36, 18))
            WoodRight = Crop(sheet, New Rectangle(88, 175, 36, 18))
        End Using

        ' Ice: part of the teal ground strip in tileset_ICEBURG.png.
        ' The "white" in that file is OPAQUE white, so it is colour-keyed to transparent here.
        Using sheet As Bitmap = LoadSheet("tileset_ICEBURG.png")
            IceTile = BuildIce(sheet)
        End Using
    End Sub

    Private Sub LoadItems()
        ' Meat (double-jump buff) and Hammer (spring buff): tight crops of the 64 x 64 files.
        Using sheet As Bitmap = LoadSheet("Meat_Resource.png")
            MeatItem = Crop(sheet, New Rectangle(9, 16, 47, 36))
        End Using
        Using sheet As Bitmap = LoadSheet("Tool_Hammer.png")
            HammerItem = Crop(sheet, New Rectangle(20, 17, 28, 28))
        End Using

        ' Bat: row 3 of AnimalSheet.png, two 16 x 16 wing frames.
        Using sheet As Bitmap = LoadSheet("AnimalSheet.png")
            BatFrames(0) = Crop(sheet, New Rectangle(0, 32, 16, 16))
            BatFrames(1) = Crop(sheet, New Rectangle(16, 32, 16, 16))
        End Using
    End Sub

    Private Sub LoadWalls()
        Using sheet As Bitmap = LoadSheet("walls.png")
            WallNear = BuildWallPeriod(sheet)
        End Using
        Using sheet As Bitmap = LoadSheet("walls_far.png")
            WallFar = BuildWallPeriod(sheet)
        End Using
    End Sub

    ' ===================== Helpers =====================

    Private Function FindAssetFile(fileName As String) As String
        Try
            If Not IO.Directory.Exists(AudioManager.AssetsRoot) Then Return Nothing
            Dim hits() As String = IO.Directory.GetFiles(AudioManager.AssetsRoot, fileName, IO.SearchOption.AllDirectories)
            If hits.Length > 0 Then Return hits(0)
        Catch ex As Exception
            Debug.WriteLine("JumpKnightAssets: search failed for " & fileName & " - " & ex.Message)
        End Try
        Return Nothing
    End Function

    ''' <summary>Loads a PNG into a memory copy (so the file is not left locked). Nothing if missing.</summary>
    Private Function LoadSheet(fileName As String) As Bitmap
        Dim path As String = FindAssetFile(fileName)
        If path Is Nothing Then
            Missing.Add(fileName)
            Return Nothing
        End If
        Try
            Using img As Image = Image.FromFile(path)
                Dim bmp As New Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb)
                Using g As Graphics = Graphics.FromImage(bmp)
                    g.CompositingMode = Drawing2D.CompositingMode.SourceCopy
                    g.DrawImage(img, 0, 0, img.Width, img.Height)
                End Using
                Return bmp
            End Using
        Catch ex As Exception
            Debug.WriteLine("JumpKnightAssets: cannot read " & fileName & " - " & ex.Message)
            Missing.Add(fileName & " (unreadable)")
            Return Nothing
        End Try
    End Function

    ''' <summary>Loads a horizontal animation strip and checks its width matches the frame count.</summary>
    Private Function LoadStrip(fileName As String, frames As Integer) As Bitmap
        Dim strip As Bitmap = LoadSheet(fileName)
        If strip Is Nothing Then Return Nothing
        If strip.Width <> frames * KnightFrameW Then
            Debug.WriteLine("JumpKnightAssets: " & fileName & " is " & strip.Width & " px wide, expected " & (frames * KnightFrameW))
            Missing.Add(fileName & " (unexpected size)")
            strip.Dispose()
            Return Nothing
        End If
        Return Track(strip)
    End Function

    Private Function Track(b As Bitmap) As Bitmap
        If b IsNot Nothing Then owned.Add(b)
        Return b
    End Function

    Private Function Crop(src As Bitmap, r As Rectangle) As Bitmap
        If src Is Nothing Then Return Nothing
        If r.X < 0 OrElse r.Y < 0 OrElse r.Right > src.Width OrElse r.Bottom > src.Height Then
            Debug.WriteLine("JumpKnightAssets: crop outside image (" & r.ToString() & ")")
            Return Nothing
        End If
        Return Track(src.Clone(r, PixelFormat.Format32bppArgb))
    End Function

    Private Function MirrorFrames(strip As Bitmap, frames As Integer) As Bitmap
        If strip Is Nothing Then Return Nothing
        Dim result As New Bitmap(strip.Width, strip.Height, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(result)
            g.CompositingMode = Drawing2D.CompositingMode.SourceCopy
            For i As Integer = 0 To frames - 1
                Using frame As Bitmap = strip.Clone(New Rectangle(i * KnightFrameW, 0, KnightFrameW, strip.Height), PixelFormat.Format32bppArgb)
                    frame.RotateFlip(RotateFlipType.RotateNoneFlipX)
                    g.DrawImage(frame, i * KnightFrameW, 0, KnightFrameW, strip.Height)
                End Using
            Next
        End Using
        Return Track(result)
    End Function

    ''' <summary>Blends every visible pixel toward a colour (small images only).</summary>
    Private Function Tint(src As Bitmap, tintColor As Color, amount As Single) As Bitmap
        Dim result As New Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb)
        For y As Integer = 0 To src.Height - 1
            For x As Integer = 0 To src.Width - 1
                Dim c As Color = src.GetPixel(x, y)
                If c.A > 0 Then
                    result.SetPixel(x, y, Color.FromArgb(c.A, Blend(c.R, tintColor.R, amount), Blend(c.G, tintColor.G, amount), Blend(c.B, tintColor.B, amount)))
                End If
            Next
        Next
        Return Track(result)
    End Function

    Private Shared Function Blend(a As Integer, b As Integer, amount As Single) As Integer
        Return CInt(a + (b - a) * amount)
    End Function

    Private Function BuildIce(sheet As Bitmap) As Bitmap
        Dim raw As Bitmap = Crop(sheet, New Rectangle(16, 228, 32, 28))
        If raw Is Nothing Then Return Nothing

        ' 1) Remove the opaque white background.
        For y As Integer = 0 To raw.Height - 1
            For x As Integer = 0 To raw.Width - 1
                Dim c As Color = raw.GetPixel(x, y)
                If c.R >= 250 AndAlso c.G >= 250 AndAlso c.B >= 250 Then raw.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0))
            Next
        Next

        ' 2) Frost it: lighten toward icy blue.
        Dim frosty As Bitmap = Tint(raw, Color.FromArgb(205, 240, 255), 0.35F)

        ' 3) Glossy white-blue cap on the top surface of every column.
        For x As Integer = 0 To frosty.Width - 1
            For y As Integer = 0 To frosty.Height - 1
                If frosty.GetPixel(x, y).A > 0 Then
                    frosty.SetPixel(x, y, Color.FromArgb(255, 236, 252, 255))
                    If y + 1 < frosty.Height AndAlso frosty.GetPixel(x, y + 1).A > 0 Then
                        frosty.SetPixel(x, y + 1, Color.FromArgb(255, 190, 232, 250))
                    End If
                    Exit For
                End If
            Next
        Next

        ' 4) Trim empty rows at the top so the walking surface is exactly the top of the image.
        Dim firstRow As Integer = 0
        For y As Integer = 0 To frosty.Height - 1
            Dim any As Boolean = False
            For x As Integer = 0 To frosty.Width - 1
                If frosty.GetPixel(x, y).A > 0 Then
                    any = True
                    Exit For
                End If
            Next
            If any Then
                firstRow = y
                Exit For
            End If
        Next
        Return Crop(frosty, New Rectangle(0, firstRow, frosty.Width, frosty.Height - firstRow))
    End Function

    ''' <summary>
    ''' walls.png / walls_far.png are texture atlases, not seamless tiles. This takes one clean
    ''' brick region (104 x 46) and builds a repeating bitmap by mirroring it sideways and
    ''' vertically, which hides the seams.
    ''' </summary>
    Private Function BuildWallPeriod(sheet As Bitmap) As Bitmap
        Dim tile As Bitmap = Crop(sheet, New Rectangle(316, 122, WallTileW, WallTileH))
        If tile Is Nothing Then Return Nothing

        Dim period As New Bitmap(WallPeriodW, WallTileH * 2, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(period)
            g.CompositingMode = Drawing2D.CompositingMode.SourceCopy
            g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

            Using mirrored As Bitmap = tile.Clone(New Rectangle(0, 0, WallTileW, WallTileH), PixelFormat.Format32bppArgb)
                mirrored.RotateFlip(RotateFlipType.RotateNoneFlipX)
                g.DrawImage(tile, 0, 0, WallTileW, WallTileH)
                g.DrawImage(mirrored, WallTileW, 0, WallTileW, WallTileH)
            End Using

            Using topRow As Bitmap = period.Clone(New Rectangle(0, 0, WallPeriodW, WallTileH), PixelFormat.Format32bppArgb)
                topRow.RotateFlip(RotateFlipType.RotateNoneFlipY)
                g.DrawImage(topRow, 0, WallTileH, WallPeriodW, WallTileH)
            End Using
        End Using
        Return Track(period)
    End Function

    ' ===================== Shared drawing helper =====================

    ''' <summary>
    ''' Draws one vertically repeating strip of a wall period bitmap.
    ''' srcX/srcW choose which columns of the bitmap to use; scroll moves the pattern downward
    ''' as the camera climbs (use a smaller factor for far layers = parallax).
    ''' </summary>
    Public Sub DrawWallColumn(g As Graphics, period As Bitmap, destX As Single, srcX As Integer, srcW As Integer,
                              areaTop As Integer, areaHeight As Integer, scroll As Double)
        If period Is Nothing OrElse srcW <= 0 OrElse areaHeight <= 0 Then Return
        Dim periodH As Integer = period.Height * ArtScale
        Dim offset As Double = scroll - Math.Floor(scroll / periodH) * periodH
        Dim y As Integer = areaTop + CInt(Math.Round(offset)) - periodH
        Dim x As Integer = CInt(Math.Round(destX))
        Dim destW As Integer = srcW * ArtScale
        Dim src As New Rectangle(srcX, 0, srcW, period.Height)
        Do While y < areaTop + areaHeight
            g.DrawImage(period, New Rectangle(x, y, destW, periodH), src, GraphicsUnit.Pixel)
            y += periodH
        Loop
    End Sub

    ' ===================== Cleanup =====================

    Public Sub Dispose() Implements IDisposable.Dispose
        For Each b As Bitmap In owned
            Try
                b.Dispose()
            Catch
            End Try
        Next
        owned.Clear()
    End Sub

End Class
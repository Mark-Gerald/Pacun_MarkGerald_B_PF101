Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

''' <summary>Describes one horizontal strip of animation frames inside a sprite sheet.</summary>
Public Class StripSpec
    Public ReadOnly SheetPath As String
    Public ReadOnly Row As Integer
    Public ReadOnly Count As Integer

    Public Sub New(sheetPathValue As String, rowValue As Integer, countValue As Integer)
        SheetPath = sheetPathValue
        Row = rowValue
        Count = countValue
    End Sub
End Class

Public Enum FacingDirection
    FaceFront
    FaceLeft
    FaceRight
End Enum

Public Enum SpriteAction
    Idle
    Walk
End Enum

''' <summary>
''' Loads and caches sprite sheets and cropped frames. Frame grids were verified by
''' measuring the real image files (see notes in the SpriteLibrary class below).
''' </summary>
Public Module GameAssets

    Public Const CharFrameW As Integer = 48
    Public Const CharFrameH As Integer = 32

    ' ground_enemy-Sheet.png: 16x16 frames, 4 per row, rows described in SpriteLibrary.
    Public Const EnemyFrameW As Integer = 16
    Public Const EnemyFrameH As Integer = 16
    Public Const EnemyFrameCount As Integer = 4

    ' Boat.png: 84x96 cells; cell (0,0) is the plain raft (opaque part is the bottom 37px).
    Public Const BoatCellWidth As Integer = 84
    Public Const BoatCellHeight As Integer = 96

    Public Const RpgTileSize As Integer = 32

    ' Farmer sheets: 44x34 frames. Idle = 7 frames (308px), Walk = 8 frames (352px).
    Public Const FarmerFrameW As Integer = 44
    Public Const FarmerFrameH As Integer = 34
    Public Const FarmerIdleFrameCount As Integer = 7
    Public Const FarmerWalkFrameCount As Integer = 8

    ' Older names, kept so any other code that used them still compiles.
    Public Const FarmerIdleFrameW As Integer = 44
    Public Const FarmerIdleFrameH As Integer = 34
    Public Const FarmerWalkFrameW As Integer = 44
    Public Const FarmerWalkFrameH As Integer = 34
    Public Const FarmerJumpFrameW As Integer = 44
    Public Const FarmerJumpFrameH As Integer = 34

    Private ReadOnly sheets As New Dictionary(Of String, Image)
    Private ReadOnly frameCache As New Dictionary(Of String, Bitmap)
    Private ReadOnly stripCache As New Dictionary(Of String, Image()())
    Private missingFrame As Bitmap = Nothing

    Public Function GetSheet(relativePath As String) As Image
        If sheets.ContainsKey(relativePath) Then Return sheets(relativePath)

        Dim fullPath As String = IO.Path.Combine(AudioManager.AssetsRoot, relativePath)
        If Not IO.File.Exists(fullPath) Then
            Debug.WriteLine("GameAssets: sheet not found: " & fullPath)
            sheets(relativePath) = Nothing
            Return Nothing
        End If

        Try
            Dim img As Image = Image.FromFile(fullPath)
            sheets(relativePath) = img
            Return img
        Catch ex As Exception
            Debug.WriteLine("GameAssets: failed to load " & fullPath & " - " & ex.Message)
            sheets(relativePath) = Nothing
            Return Nothing
        End Try
    End Function

    Public Function GetSheetSize(relativePath As String) As Size?
        Dim sheet As Image = GetSheet(relativePath)
        If sheet Is Nothing Then Return Nothing
        Return New Size(sheet.Width, sheet.Height)
    End Function

    ''' <summary>Crops one frame out of a sprite sheet, caching the result.</summary>
    Public Function GetFrame(relativePath As String, col As Integer, row As Integer, frameW As Integer, frameH As Integer) As Bitmap
        Dim key As String = relativePath & ":" & col & "," & row & "," & frameW & "x" & frameH
        If frameCache.ContainsKey(key) Then Return frameCache(key)

        Dim sheet As Image = GetSheet(relativePath)
        If sheet Is Nothing Then Return Nothing

        Dim srcX As Integer = col * frameW
        Dim srcY As Integer = row * frameH
        If srcX + frameW > sheet.Width OrElse srcY + frameH > sheet.Height Then
            Debug.WriteLine("GameAssets: frame out of bounds for " & relativePath &
                            " (sheet " & sheet.Width & "x" & sheet.Height &
                            ", col=" & col & " row=" & row & " size=" & frameW & "x" & frameH & ")")
            Return Nothing
        End If

        Dim frame As New Bitmap(frameW, frameH, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(frame)
            g.InterpolationMode = InterpolationMode.NearestNeighbor
            g.DrawImage(sheet, New Rectangle(0, 0, frameW, frameH), New Rectangle(srcX, srcY, frameW, frameH), GraphicsUnit.Pixel)
        End Using

        frameCache(key) = frame
        Return frame
    End Function

    ''' <summary>
    ''' Loads several animation strips and crops ALL of their frames to one shared
    ''' bounding box of non-transparent pixels. A shared box keeps the feet on the same
    ''' line and the sprite from jittering when switching between idle and walk.
    ''' Returns one Image() per spec (entries may be Nothing if a file is missing).
    ''' </summary>
    Public Function GetTrimmedStrips(frameW As Integer, frameH As Integer, specs As IList(Of StripSpec)) As Image()()
        Dim keyBuilder As New System.Text.StringBuilder()
        keyBuilder.Append(frameW).Append("x").Append(frameH)
        For Each spec In specs
            keyBuilder.Append("|").Append(spec.SheetPath).Append(":").Append(spec.Row).Append(":").Append(spec.Count)
        Next
        Dim key As String = keyBuilder.ToString()
        If stripCache.ContainsKey(key) Then Return stripCache(key)

        Dim raw(specs.Count - 1)() As Bitmap
        Dim minX As Integer = Integer.MaxValue
        Dim minY As Integer = Integer.MaxValue
        Dim maxX As Integer = -1
        Dim maxY As Integer = -1

        For i As Integer = 0 To specs.Count - 1
            raw(i) = New Bitmap(specs(i).Count - 1) {}
            For f As Integer = 0 To specs(i).Count - 1
                Dim bmp As Bitmap = GetFrame(specs(i).SheetPath, f, specs(i).Row, frameW, frameH)
                raw(i)(f) = bmp
                If bmp Is Nothing Then Continue For
                For y As Integer = 0 To bmp.Height - 1
                    For x As Integer = 0 To bmp.Width - 1
                        If bmp.GetPixel(x, y).A > 0 Then
                            If x < minX Then minX = x
                            If y < minY Then minY = y
                            If x > maxX Then maxX = x
                            If y > maxY Then maxY = y
                        End If
                    Next
                Next
            Next
        Next

        Dim cropRect As Rectangle
        If maxX < 0 Then
            cropRect = New Rectangle(0, 0, frameW, frameH)
        Else
            cropRect = New Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)
        End If

        Dim result(specs.Count - 1)() As Image
        For i As Integer = 0 To specs.Count - 1
            result(i) = New Image(specs(i).Count - 1) {}
            For f As Integer = 0 To specs(i).Count - 1
                If raw(i)(f) Is Nothing Then Continue For
                Dim cropped As New Bitmap(cropRect.Width, cropRect.Height, PixelFormat.Format32bppArgb)
                Using g As Graphics = Graphics.FromImage(cropped)
                    g.InterpolationMode = InterpolationMode.NearestNeighbor
                    g.DrawImage(raw(i)(f), New Rectangle(0, 0, cropRect.Width, cropRect.Height), cropRect, GraphicsUnit.Pixel)
                End Using
                result(i)(f) = cropped
            Next
        Next

        stripCache(key) = result
        Return result
    End Function

    ''' <summary>Returns horizontally mirrored copies of the given frames.</summary>
    Public Function MirrorFrames(frames As Image()) As Image()
        Dim result(frames.Length - 1) As Image
        For i As Integer = 0 To frames.Length - 1
            If frames(i) Is Nothing Then Continue For
            Dim copy As New Bitmap(frames(i))
            copy.RotateFlip(RotateFlipType.RotateNoneFlipX)
            result(i) = copy
        Next
        Return result
    End Function

    ''' <summary>The raft from Boat.png, cropped to its opaque pixels (84x37).</summary>
    Public Function GetBoatRaft() As Image
        Dim specs As New List(Of StripSpec) From {
            New StripSpec("enviroment\Boat.png", 0, 1)
        }
        Dim strips As Image()() = GetTrimmedStrips(BoatCellWidth, BoatCellHeight, specs)
        Return strips(0)(0)
    End Function

    ''' <summary>Visible stand-in used when an animation's file is missing, so the game never crashes.</summary>
    Public Function GetMissingFrame() As Image
        If missingFrame Is Nothing Then
            missingFrame = New Bitmap(16, 16)
            Using g As Graphics = Graphics.FromImage(missingFrame)
                g.Clear(Color.FromArgb(160, 160, 160))
                g.DrawRectangle(Pens.Magenta, 0, 0, 15, 15)
            End Using
        End If
        Return missingFrame
    End Function

End Module

''' <summary>
''' Supplies the animation frames for each character type, action, and facing.
'''
''' Monster sheet (verified by measuring eye position and frame-to-frame change per row):
'''   rows 2/1/0 = idle  front/left/right     rows 6/5/4 = walk front/left/right
''' Farmer sheets contain FRONT-FACING art only, so FaceLeft uses mirrored frames and
''' FaceRight/FaceFront use the originals. (Real side-view art would be needed for true facing.)
''' </summary>
Public NotInheritable Class SpriteLibrary

    Private Const MonsterSheet As String = "enemies\ground_enemy-Sheet.png"
    Private Const MonsterIdleFrontRow As Integer = 2
    Private Const MonsterIdleLeftRow As Integer = 1
    Private Const MonsterIdleRightRow As Integer = 0
    Private Const MonsterWalkFrontRow As Integer = 6
    Private Const MonsterWalkLeftRow As Integer = 5
    Private Const MonsterWalkRightRow As Integer = 4

    Private Const FarmerIdleSheet As String = "Character\Farmer-Idle.png"
    Private Const FarmerWalkSheet As String = "Character\Farmer-Walk.png"

    Private Shared isBuilt As Boolean = False
    Private Shared monsterIdle As Dictionary(Of FacingDirection, Image())
    Private Shared monsterWalk As Dictionary(Of FacingDirection, Image())
    Private Shared farmerIdle As Dictionary(Of FacingDirection, Image())
    Private Shared farmerWalk As Dictionary(Of FacingDirection, Image())

    Private Sub New()
    End Sub

    Public Shared Function GetFrames(isMonster As Boolean, spriteAction As SpriteAction, facing As FacingDirection) As Image()
        EnsureBuilt()
        If isMonster Then
            If spriteAction = SpriteAction.Walk Then Return monsterWalk(facing)
            Return monsterIdle(facing)
        End If
        If spriteAction = SpriteAction.Walk Then Return farmerWalk(facing)
        Return farmerIdle(facing)
    End Function

    Public Shared Function GetFrameMs(isMonster As Boolean, spriteAction As SpriteAction) As Integer
        If isMonster Then Return If(spriteAction = SpriteAction.Walk, 120, 220)
        Return If(spriteAction = SpriteAction.Walk, 100, 150)
    End Function

    ' Whole-number pixel scale keeps pixel art crisp. Trimmed sizes: monster 16x14, farmer 18x20.
    Public Shared Function GetPixelScale(isMonster As Boolean) As Integer
        Return If(isMonster, 5, 4)
    End Function

    Private Shared Sub EnsureBuilt()
        If isBuilt Then Return
        isBuilt = True
        BuildMonsterFrames()
        BuildFarmerFrames()
    End Sub

    Private Shared Sub BuildMonsterFrames()
        Dim specs As New List(Of StripSpec) From {
            New StripSpec(MonsterSheet, MonsterIdleFrontRow, GameAssets.EnemyFrameCount),
            New StripSpec(MonsterSheet, MonsterIdleLeftRow, GameAssets.EnemyFrameCount),
            New StripSpec(MonsterSheet, MonsterIdleRightRow, GameAssets.EnemyFrameCount),
            New StripSpec(MonsterSheet, MonsterWalkFrontRow, GameAssets.EnemyFrameCount),
            New StripSpec(MonsterSheet, MonsterWalkLeftRow, GameAssets.EnemyFrameCount),
            New StripSpec(MonsterSheet, MonsterWalkRightRow, GameAssets.EnemyFrameCount)
        }
        Dim strips As Image()() = GameAssets.GetTrimmedStrips(GameAssets.EnemyFrameW, GameAssets.EnemyFrameH, specs)

        monsterIdle = New Dictionary(Of FacingDirection, Image())()
        monsterIdle(FacingDirection.FaceFront) = Sanitize(strips(0))
        monsterIdle(FacingDirection.FaceLeft) = Sanitize(strips(1))
        monsterIdle(FacingDirection.FaceRight) = Sanitize(strips(2))

        monsterWalk = New Dictionary(Of FacingDirection, Image())()
        monsterWalk(FacingDirection.FaceFront) = Sanitize(strips(3))
        monsterWalk(FacingDirection.FaceLeft) = Sanitize(strips(4))
        monsterWalk(FacingDirection.FaceRight) = Sanitize(strips(5))
    End Sub

    Private Shared Sub BuildFarmerFrames()
        Dim specs As New List(Of StripSpec) From {
            New StripSpec(FarmerIdleSheet, 0, GameAssets.FarmerIdleFrameCount),
            New StripSpec(FarmerWalkSheet, 0, GameAssets.FarmerWalkFrameCount)
        }
        Dim strips As Image()() = GameAssets.GetTrimmedStrips(GameAssets.FarmerFrameW, GameAssets.FarmerFrameH, specs)

        Dim idleFrames As Image() = Sanitize(strips(0))
        Dim walkFrames As Image() = Sanitize(strips(1))

        farmerIdle = New Dictionary(Of FacingDirection, Image())()
        farmerIdle(FacingDirection.FaceFront) = idleFrames
        farmerIdle(FacingDirection.FaceRight) = idleFrames
        farmerIdle(FacingDirection.FaceLeft) = GameAssets.MirrorFrames(idleFrames)

        farmerWalk = New Dictionary(Of FacingDirection, Image())()
        farmerWalk(FacingDirection.FaceFront) = walkFrames
        farmerWalk(FacingDirection.FaceRight) = walkFrames
        farmerWalk(FacingDirection.FaceLeft) = GameAssets.MirrorFrames(walkFrames)
    End Sub

    ' Drops missing frames; if nothing loaded, returns a visible placeholder instead of crashing.
    Private Shared Function Sanitize(frames As Image()) As Image()
        Dim valid As New List(Of Image)
        If frames IsNot Nothing Then
            For Each f In frames
                If f IsNot Nothing Then valid.Add(f)
            Next
        End If
        If valid.Count = 0 Then valid.Add(GameAssets.GetMissingFrame())
        Return valid.ToArray()
    End Function

End Class
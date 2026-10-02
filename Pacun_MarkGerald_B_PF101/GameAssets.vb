''' <summary>
''' Loads and caches sprite sheets and individual cropped frames so the same
''' bitmap is never decoded or cropped more than once. All frame-grid numbers
''' are centralized here -- adjust these constants if a sprite looks misaligned.
''' </summary>
Public Module GameAssets

    Public Const CharFrameW As Integer = 48
    Public Const CharFrameH As Integer = 32

    Public Const EnemyFrameW As Integer = 16
    Public Const EnemyFrameH As Integer = 16
    Public Const EnemyFrameCount As Integer = 4

    Public Const BoatCellWidth As Integer = 84
    Public Const BoatCellHeight As Integer = 96

    Public Const RpgTileSize As Integer = 32

    Public Const FarmerIdleFrameW As Integer = 44
    Public Const FarmerIdleFrameH As Integer = 34
    Public Const FarmerIdleFrameCount As Integer = 7

    ' FIXED: Updated to match the idle frame size. 
    ' If the walking animation still glitches, open Farmer-Walk.png in an image editor
    ' and verify these exact dimensions.
    Public Const FarmerWalkFrameW As Integer = 44
    Public Const FarmerWalkFrameH As Integer = 34
    Public Const FarmerWalkFrameCount As Integer = 4

    Public Const FarmerJumpFrameW As Integer = 32
    Public Const FarmerJumpFrameH As Integer = 32

    Private ReadOnly sheets As New Dictionary(Of String, Image)
    Private ReadOnly frameCache As New Dictionary(Of String, Bitmap)
    Private ReadOnly animCache As New Dictionary(Of String, Image())

    Public Sub ClearCache()
        For Each img In sheets.Values
            If img IsNot Nothing Then img.Dispose()
        Next
        sheets.Clear()

        For Each bmp In frameCache.Values
            If bmp IsNot Nothing Then bmp.Dispose()
        Next
        frameCache.Clear()

        For Each frames In animCache.Values
            If frames IsNot Nothing Then
                For Each f In frames
                    If f IsNot Nothing Then f.Dispose()
                Next
            End If
        Next
        animCache.Clear()
    End Sub

    Public Function GetTrimmedAnimation(relativePath As String, frameCount As Integer, row As Integer, frameW As Integer, frameH As Integer) As Image()
        Dim key As String = relativePath & "|trim|" & frameCount & "," & row & "," & frameW & "x" & frameH
        If animCache.ContainsKey(key) Then Return animCache(key)

        Dim raw(frameCount - 1) As Bitmap
        For i As Integer = 0 To frameCount - 1
            raw(i) = GetFrame(relativePath, i, row, frameW, frameH)
        Next

        Dim minX As Integer = Integer.MaxValue
        Dim minY As Integer = Integer.MaxValue
        Dim maxX As Integer = -1
        Dim maxY As Integer = -1

        For Each bmp In raw
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

        Dim result(frameCount - 1) As Image

        If maxX < 0 Then
            ' Nothing visible found -- return the untrimmed frames rather than failing.
            For i As Integer = 0 To frameCount - 1
                result(i) = raw(i)
            Next
        Else
            Dim cropRect As New Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)
            For i As Integer = 0 To frameCount - 1
                If raw(i) Is Nothing Then Continue For
                Dim cropped As New Bitmap(cropRect.Width, cropRect.Height)
                Using g As Graphics = Graphics.FromImage(cropped)
                    g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
                    g.DrawImage(raw(i), New Rectangle(0, 0, cropRect.Width, cropRect.Height), cropRect, GraphicsUnit.Pixel)
                End Using
                result(i) = cropped
            Next
        End If

        animCache(key) = result
        Return result
    End Function

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

    ''' <summary>
    ''' Reports a sheet's actual pixel dimensions, or Nothing if it can't be loaded.
    ''' Useful for confirming real frame sizes before guessing a crop grid.
    ''' </summary>
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
            Debug.WriteLine("GameAssets: requested frame is out of bounds for " & relativePath &
                             " (sheet is " & sheet.Width & "x" & sheet.Height &
                             ", requested col=" & col & " row=" & row & " size=" & frameW & "x" & frameH & ")")
            Return Nothing
        End If

        Dim frame As New Bitmap(frameW, frameH)
        Using g As Graphics = Graphics.FromImage(frame)
            g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
            g.DrawImage(sheet, New Rectangle(0, 0, frameW, frameH), New Rectangle(srcX, srcY, frameW, frameH), GraphicsUnit.Pixel)
        End Using

        frameCache(key) = frame
        Return frame
    End Function

End Module
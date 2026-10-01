''' <summary>
''' Loads and caches sprite sheets and individual cropped frames so the same
''' bitmap is never decoded or cropped more than once. All frame-grid numbers
''' are centralized here -- adjust these constants if a sprite looks misaligned.
''' </summary>
Public Module GameAssets

    Public Const CharFrameW As Integer = 48
    Public Const CharFrameH As Integer = 32

    Public Const EnemyFrameW As Integer = 64
    Public Const EnemyFrameH As Integer = 64

    Public Const BoatCellWidth As Integer = 84
    Public Const BoatCellHeight As Integer = 96

    ' Confirmed from filenames: the Topdown_RPG_32x32_* tile sheets use a 32x32 grid.
    Public Const RpgTileSize As Integer = 32

    ' NOT YET CONFIRMED -- placeholders only, pending actual Farmer-*.png pixel
    ' dimensions. Do not rely on these until updated; GetFrame() will safely
    ' return Nothing (not crash) if these turn out to be wrong, since it checks
    ' frame bounds against the real image size before cropping.
    Public Const FarmerIdleFrameW As Integer = 32
    Public Const FarmerIdleFrameH As Integer = 32
    Public Const FarmerWalkFrameW As Integer = 32
    Public Const FarmerWalkFrameH As Integer = 32
    Public Const FarmerJumpFrameW As Integer = 32
    Public Const FarmerJumpFrameH As Integer = 32

    Private ReadOnly sheets As New Dictionary(Of String, Image)
    Private ReadOnly frameCache As New Dictionary(Of String, Bitmap)

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
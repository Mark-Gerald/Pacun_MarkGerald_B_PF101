Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms

''' <summary>A drawn (non-WinForms) pixel-art button: position, label, colour and enabled state.</summary>
Public Class PixelButtonDef
    Public ReadOnly Id As String
    Public ReadOnly Caption As String
    Public ReadOnly BaseColor As Color
    Public Rect As Rectangle
    Public Enabled As Boolean = True

    Public Sub New(idValue As String, captionValue As String, colorValue As Color)
        Id = idValue
        Caption = captionValue
        BaseColor = colorValue
    End Sub
End Class

''' <summary>Shared drawing helpers used by both the menu and the gameplay scene.</summary>
Public Module PixelUI

    Public Sub DrawPixelButton(g As Graphics, btn As PixelButtonDef, font As Font, hovered As Boolean, pressed As Boolean)
        Dim r As Rectangle = btn.Rect
        If r.Width <= 0 OrElse r.Height <= 0 Then Return

        Dim baseColor As Color = btn.BaseColor
        If Not btn.Enabled Then baseColor = Color.FromArgb(125, 125, 130)

        Dim fillColor As Color = baseColor
        If btn.Enabled Then
            If pressed Then
                fillColor = ControlPaint.Dark(baseColor, 0.2F)
            ElseIf hovered Then
                fillColor = ControlPaint.Light(baseColor, 0.15F)
            End If
        End If

        Dim lift As Integer = If(pressed AndAlso btn.Enabled, 3, 0)
        Dim shadowRect As New Rectangle(r.X, r.Bottom - 6, r.Width - 1, 5)
        Dim bodyRect As New Rectangle(r.X, r.Y + lift, r.Width - 1, r.Height - 6)

        Using shadowBrush As New SolidBrush(ControlPaint.Dark(baseColor, 0.4F))
            g.FillRectangle(shadowBrush, shadowRect)
        End Using
        Using fillBrush As New SolidBrush(fillColor)
            g.FillRectangle(fillBrush, bodyRect)
        End Using
        Using outlinePen As New Pen(Color.FromArgb(30, 20, 10), 2)
            g.DrawRectangle(outlinePen, bodyRect.X + 1, bodyRect.Y + 1, bodyRect.Width - 2, bodyRect.Height - 2)
        End Using

        Using sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            Dim textRect As New RectangleF(bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height)
            Using shadowBrush As New SolidBrush(Color.FromArgb(140, 0, 0, 0))
                g.DrawString(btn.Caption, font, shadowBrush, New RectangleF(textRect.X + 1, textRect.Y + 1, textRect.Width, textRect.Height), sf)
            End Using
            Dim textColor As Color = If(btn.Enabled, Color.White, Color.FromArgb(205, 205, 210))
            Using textBrush As New SolidBrush(textColor)
                g.DrawString(btn.Caption, font, textBrush, textRect, sf)
            End Using
        End Using
    End Sub

    ''' <summary>Text with a solid outline so it stays readable over grass and water (no background box needed).</summary>
    Public Sub DrawOutlinedText(g As Graphics, text As String, font As Font, fillColor As Color, outlineColor As Color,
                                rect As RectangleF, format As StringFormat, thickness As Single)
        Using outlineBrush As New SolidBrush(outlineColor)
            For Each dx As Single In New Single() {-thickness, 0, thickness}
                For Each dy As Single In New Single() {-thickness, 0, thickness}
                    If dx = 0 AndAlso dy = 0 Then Continue For
                    g.DrawString(text, font, outlineBrush, New RectangleF(rect.X + dx, rect.Y + dy, rect.Width, rect.Height), format)
                Next
            Next
        End Using
        Using fillBrush As New SolidBrush(fillColor)
            g.DrawString(text, font, fillBrush, rect, format)
        End Using
    End Sub

End Module

''' <summary>
''' The shared map background: land on both sides and a river down the middle, all filling the
''' whole panel (no sky, no stripes). To change the look later, change the two tile paths below.
''' </summary>
Public Class RiverBackdrop
    Implements IDisposable

    Public Const BankTilePath As String = "tiles\Grass_rocks.png"
    Public Const WaterTilePath As String = "tiles\water-Sheet.png"

    Private Shared ReadOnly BankFallback As Color = Color.FromArgb(120, 190, 110)
    Private Shared ReadOnly WaterFallback As Color = Color.FromArgb(70, 140, 200)

    Public LeftBank As Rectangle
    Public River As Rectangle
    Public RightBank As Rectangle

    Private ReadOnly _bankTile As Image
    Private ReadOnly _waterTile As Image
    Private _bankCache As Bitmap = Nothing
    Private _cacheDirty As Boolean = True
    Private _waveOffset As Double = 0
    Private _width As Integer = 0
    Private _height As Integer = 0

    Public Sub New()
        _bankTile = GameAssets.GetSheet(BankTilePath)
        _waterTile = GameAssets.GetSheet(WaterTilePath)
    End Sub

    Public Sub Layout(w As Integer, h As Integer)
        w = Math.Max(1, w)
        h = Math.Max(1, h)
        If w = _width AndAlso h = _height Then Return
        _width = w
        _height = h

        Dim riverW As Integer = Math.Max(330, Math.Min(520, CInt(w * 0.36)))
        Dim riverLeft As Integer = Math.Max(0, (w - riverW) \ 2)
        River = New Rectangle(riverLeft, 0, riverW, h)
        LeftBank = New Rectangle(0, 0, riverLeft, h)
        RightBank = New Rectangle(riverLeft + riverW, 0, Math.Max(1, w - (riverLeft + riverW)), h)
        _cacheDirty = True
    End Sub

    Public Sub Advance(dt As Double)
        _waveOffset = (_waveOffset + 40.0 * dt) Mod 800.0
    End Sub

    Public Sub Draw(g As Graphics)
        If _width <= 0 OrElse _height <= 0 Then Return
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = PixelOffsetMode.Half

        If _cacheDirty OrElse _bankCache Is Nothing Then RebuildCache()
        g.DrawImage(_bankCache, 0, 0, _bankCache.Width, _bankCache.Height)

        If _waterTile IsNot Nothing Then
            TileScrollingVertical(g, _waterTile, River, _waveOffset)
        Else
            Using waterBrush As New SolidBrush(WaterFallback)
                g.FillRectangle(waterBrush, River)
            End Using
            Using wavePen As New Pen(Color.FromArgb(90, 255, 255, 255), 2)
                wavePen.DashStyle = DashStyle.Dash
                Dim y As Integer = -CInt(_waveOffset Mod 28.0)
                While y < _height
                    g.DrawLine(wavePen, River.Left + 12, y, River.Right - 12, y)
                    y += 28
                End While
            End Using
        End If

        ' Soft shoreline so land and water read as one continuous map.
        Using shorePen As New Pen(Color.FromArgb(150, 55, 105, 55), 4)
            g.DrawLine(shorePen, River.Left + 2, 0, River.Left + 2, _height)
            g.DrawLine(shorePen, River.Right - 3, 0, River.Right - 3, _height)
        End Using
    End Sub

    Private Sub RebuildCache()
        If _bankCache IsNot Nothing Then _bankCache.Dispose()
        _bankCache = New Bitmap(_width, _height)
        Using g As Graphics = Graphics.FromImage(_bankCache)
            g.InterpolationMode = InterpolationMode.NearestNeighbor
            g.PixelOffsetMode = PixelOffsetMode.Half
            If _bankTile IsNot Nothing Then
                TileImage(g, _bankTile, LeftBank)
                TileImage(g, _bankTile, RightBank)
            Else
                Using bankBrush As New SolidBrush(BankFallback)
                    g.FillRectangle(bankBrush, LeftBank)
                    g.FillRectangle(bankBrush, RightBank)
                End Using
            End If
        End Using
        _cacheDirty = False
    End Sub

    Private Shared Sub TileImage(g As Graphics, img As Image, area As Rectangle)
        If area.Width <= 0 OrElse area.Height <= 0 Then Return
        g.SetClip(area)
        Dim y As Integer = area.Top
        While y < area.Bottom
            Dim x As Integer = area.Left
            While x < area.Right
                g.DrawImage(img, x, y, img.Width, img.Height)
                x += img.Width
            End While
            y += img.Height
        End While
        g.ResetClip()
    End Sub

    Private Shared Sub TileScrollingVertical(g As Graphics, img As Image, area As Rectangle, offsetY As Double)
        If area.Width <= 0 OrElse area.Height <= 0 OrElse img.Height <= 0 Then Return
        g.SetClip(area)
        Dim y As Integer = area.Top - CInt(offsetY Mod img.Height)
        While y < area.Bottom
            Dim x As Integer = area.Left
            While x < area.Right
                g.DrawImage(img, x, y, img.Width, img.Height)
                x += img.Width
            End While
            y += img.Height
        End While
        g.ResetClip()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If _bankCache IsNot Nothing Then
            _bankCache.Dispose()
            _bankCache = Nothing
        End If
    End Sub

End Class
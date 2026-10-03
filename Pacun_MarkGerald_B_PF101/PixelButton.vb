Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' A pixel-art-styled button: flat fill, dark outline, subtle bottom shadow edge,
''' and hover/pressed color shifts. Drawn manually so it doesn't look like a
''' default WinForms Button.
'''
''' This class used to live at the bottom of the OLD Level1MenuForm.vb. The new Level1MenuForm.vb
''' draws its buttons itself (PixelButtonDef / PixelUI), so PixelButton was removed with it.
''' Level 2 (menu, game screen) still uses it, so it now lives in its own file.
''' </summary>
Public Class PixelButton
    Inherits Panel

    Private ReadOnly baseColor As Color
    Private ReadOnly captionText As String
    Private isHovering As Boolean = False
    Private isPressed As Boolean = False

    Public Sub New(caption As String, color As Color)
        captionText = caption
        baseColor = color
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.Cursor = Cursors.Hand
        Me.BackColor = Color.Transparent

        AddHandler Me.MouseEnter, Sub(s, e)
                                      isHovering = True
                                      Invalidate()
                                  End Sub
        AddHandler Me.MouseLeave, Sub(s, e)
                                      isHovering = False
                                      isPressed = False
                                      Invalidate()
                                  End Sub
        AddHandler Me.MouseDown, Sub(s, e)
                                     isPressed = True
                                     Invalidate()
                                 End Sub
        AddHandler Me.MouseUp, Sub(s, e)
                                   isPressed = False
                                   Invalidate()
                               End Sub
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = Drawing2D.SmoothingMode.None

        Dim fillColor As Color = baseColor
        If isPressed Then
            fillColor = ControlPaint.Dark(baseColor, 0.2F)
        ElseIf isHovering Then
            fillColor = ControlPaint.Light(baseColor, 0.15F)
        End If

        Dim bodyRect As New Rectangle(0, 0, Width - 1, Height - 5)
        Dim shadowRect As New Rectangle(0, Height - 5, Width - 1, 4)

        Using shadowBrush As New SolidBrush(ControlPaint.Dark(baseColor, 0.4F))
            g.FillRectangle(shadowBrush, shadowRect)
        End Using
        Using fillBrush As New SolidBrush(fillColor)
            g.FillRectangle(fillBrush, bodyRect)
        End Using
        Using outlinePen As New Pen(Color.FromArgb(30, 20, 10), 2)
            g.DrawRectangle(outlinePen, 1, 1, bodyRect.Width - 2, bodyRect.Height - 2)
        End Using

        Using font As New Font("Segoe UI", 11.0F, FontStyle.Bold)
            Dim sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            Dim textRect As New RectangleF(0, 0, Width, bodyRect.Height)
            Using shadowBrush As New SolidBrush(Color.FromArgb(140, 0, 0, 0))
                g.DrawString(captionText, font, shadowBrush, New RectangleF(textRect.X + 1, textRect.Y + 1, textRect.Width, textRect.Height), sf)
            End Using
            Using textBrush As New SolidBrush(Color.White)
                g.DrawString(captionText, font, textBrush, textRect, sf)
            End Using
        End Using

        MyBase.OnPaint(e)
    End Sub

End Class
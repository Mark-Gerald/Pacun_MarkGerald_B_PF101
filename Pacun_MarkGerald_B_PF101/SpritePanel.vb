Imports System.Drawing
Imports System.Windows.Forms

Public Class SpritePanel
    Inherits Panel

    Public Enum FacingDirection
        Left
        Right
    End Enum

    Public Property SpriteImage As Image
    Public Property IsSelected As Boolean = False
    Public Property AccentColor As Color = Color.FromArgb(255, 215, 0)
    Public Property Facing As FacingDirection = FacingDirection.Right
    Public Property IsWalking As Boolean = False
    Public Property IsOnBoat As Boolean = False

    Private _idleFrames As Image()
    Private _walkFrames As Image()
    Private _animIndex As Integer = 0
    Private _animTimer As Timer
    Private _interval As Integer = 150

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.BackColor = Color.Transparent
        Me.Cursor = Cursors.Hand

        AddHandler Me.MouseEnter, AddressOf SpritePanel_MouseEnter
        AddHandler Me.MouseLeave, AddressOf SpritePanel_MouseLeave
    End Sub

    Private Sub SpritePanel_MouseEnter(sender As Object, e As EventArgs)
        If Not IsWalking AndAlso Not IsOnBoat Then
            Facing = FacingDirection.Left
            Me.Invalidate()
        End If
    End Sub

    Private Sub SpritePanel_MouseLeave(sender As Object, e As EventArgs)
        If Not IsWalking AndAlso Not IsOnBoat Then
            Facing = FacingDirection.Right
            Me.Invalidate()
        End If
    End Sub

    Public Sub SetAnimations(idle As Image(), walk As Image(), intervalMs As Integer)
        StopAnimationTimer()
        _idleFrames = idle
        _walkFrames = walk
        _interval = Math.Max(16, intervalMs)
        _animIndex = 0

        SpriteImage = If(_idleFrames IsNot Nothing AndAlso _idleFrames.Length > 0, _idleFrames(0), Nothing)

        If (_idleFrames IsNot Nothing AndAlso _idleFrames.Length > 0) OrElse (_walkFrames IsNot Nothing AndAlso _walkFrames.Length > 0) Then
            _animTimer = New Timer() With {.Interval = _interval}
            AddHandler _animTimer.Tick, AddressOf AnimTimer_Tick
            _animTimer.Start()
        End If
    End Sub

    Private Sub AnimTimer_Tick(sender As Object, e As EventArgs)
        Dim currentFrames = If(IsWalking, _walkFrames, _idleFrames)
        If currentFrames Is Nothing OrElse currentFrames.Length = 0 Then Return

        _animIndex = (_animIndex + 1) Mod currentFrames.Length
        SpriteImage = currentFrames(_animIndex)
        Me.Invalidate()
    End Sub

    Public Sub StopAnimationTimer()
        If _animTimer IsNot Nothing Then
            _animTimer.Stop()
            _animTimer.Dispose()
            _animTimer = Nothing
        End If
    End Sub

    ' NOTE: We are intentionally NOT disposing _idleFrames or _walkFrames here.
    ' They are shared resources managed by GameAssets. Disposing them here causes
    ' a crash when the form is reopened or reset.
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            StopAnimationTimer()
            _idleFrames = Nothing
            _walkFrames = Nothing
        End If
        MyBase.Dispose(disposing)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

        If SpriteImage IsNot Nothing Then
            ' Safe-guard against disposed images
            Try
                Dim availW As Integer = Width - 4
                Dim availH As Integer = Height - 4
                Dim scale As Single = Math.Min(availW / CSng(SpriteImage.Width), availH / CSng(SpriteImage.Height))
                Dim drawW As Integer = Math.Max(1, CInt(SpriteImage.Width * scale))
                Dim drawH As Integer = Math.Max(1, CInt(SpriteImage.Height * scale))
                Dim drawX As Integer = 2 + (availW - drawW) \ 2
                Dim drawY As Integer = 2 + (availH - drawH) \ 2

                Dim destRect As New Rectangle(drawX, drawY, drawW, drawH)
                Dim srcRect As New Rectangle(0, 0, SpriteImage.Width, SpriteImage.Height)

                If Facing = FacingDirection.Left Then
                    srcRect = New Rectangle(SpriteImage.Width, 0, -SpriteImage.Width, SpriteImage.Height)
                End If

                g.DrawImage(SpriteImage, destRect, srcRect, GraphicsUnit.Pixel)
            Catch ex As Exception
                ' Fallback if image is disposed or invalid
                Using b As New SolidBrush(Color.Gray)
                    g.FillRectangle(b, 2, 2, Width - 4, Height - 4)
                End Using
            End Try
        Else
            Using b As New SolidBrush(Color.Gray)
                g.FillRectangle(b, 2, 2, Width - 4, Height - 4)
            End Using
        End If

        ' RED SELECTION BORDER REMOVED AS REQUESTED
        ' If IsSelected Then
        '     Using pen As New Pen(AccentColor, 3)
        '         g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3)
        '     End Using
        ' End If

        MyBase.OnPaint(e)
    End Sub
End Class
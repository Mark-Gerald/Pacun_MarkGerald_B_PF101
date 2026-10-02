Imports System.Drawing.Drawing2D

Public Class Level1MenuForm
    Inherits Form

    Private scenePanel As ScenePanel
    Private startButton As PixelButton
    Private settingsButton As PixelButton
    Private nextGameButton As PixelButton
    Private exitButton As PixelButton

    ' Settings Panel UI
    Private settingsPanel As Panel
    Private musicSlider As TrackBar
    Private sfxSlider As TrackBar
    Private musicLabel As Label
    Private sfxLabel As Label
    Private backSettingsButton As Button

    Private isTransitioning As Boolean = False

    Public Sub New()
        Me.Text = "Level 1 " & ChrW(8212) & " River Crossing"
        Me.Size = New Size(900, 650)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable

        scenePanel = New ScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(scenePanel)

        BuildButtons()
        BuildSettingsPanel()
        PositionButtons()
        AddHandler scenePanel.Resize, Sub(s, ev) PositionButtons()

        AddHandler Me.Shown, Sub(s, ev) AudioManager.PlayMusic("Audio\Music\Wet Hands.mp3", True)
        AddHandler Me.FormClosed, AddressOf Level1MenuForm_FormClosed
    End Sub

    Private Sub BuildButtons()
        startButton = New PixelButton("START", Color.FromArgb(76, 175, 80))
        settingsButton = New PixelButton("SETTINGS", Color.FromArgb(66, 133, 200))
        nextGameButton = New PixelButton("NEXT GAME", Color.FromArgb(150, 90, 190))
        exitButton = New PixelButton("EXIT", Color.FromArgb(200, 70, 60))

        AddHandler startButton.Click, AddressOf StartButton_Click
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        AddHandler nextGameButton.Click, AddressOf NextGameButton_Click
        AddHandler exitButton.Click, AddressOf ExitButton_Click

        For Each b In New PixelButton() {startButton, settingsButton, nextGameButton, exitButton}
            AddHandler b.Click, Sub(s, ev) AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
            scenePanel.Controls.Add(b)
        Next
    End Sub

    Private Sub BuildSettingsPanel()
        settingsPanel = New Panel() With {
            .Size = New Size(500, 480),
            .BackColor = Color.FromArgb(26, 26, 36),
            .Visible = False
        }
        Me.Controls.Add(settingsPanel)
        settingsPanel.BringToFront()

        AddHandler settingsPanel.Paint, Sub(s, pe)
                                            Dim r = settingsPanel.ClientRectangle
                                            r = New Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1)
                                            pe.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                            Using pen As New Pen(Color.FromArgb(218, 165, 32), 2)
                                                pe.Graphics.DrawRectangle(pen, r)
                                            End Using
                                        End Sub

        Dim titleLabel As New Label() With {
            .Text = "SETTINGS",
            .Font = New Font("Segoe UI", 22.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Top,
            .Height = 70,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        settingsPanel.Controls.Add(titleLabel)

        Dim mainPanel As New Panel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(40, 10, 40, 20),
            .BackColor = Color.FromArgb(26, 26, 36)
        }
        settingsPanel.Controls.Add(mainPanel)
        mainPanel.BringToFront()

        Dim musicCaption As New Label() With {
            .Text = "BACKGROUND MUSIC",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(0, 10)
        }
        mainPanel.Controls.Add(musicCaption)

        musicLabel = New Label() With {
            .Text = GameSettings.GetInstance().MusicVolume & "%",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(218, 165, 32),
            .AutoSize = True,
            .Location = New Point(300, 10)
        }
        mainPanel.Controls.Add(musicLabel)

        musicSlider = New TrackBar() With {
            .Minimum = 0, .Maximum = 100,
            .Value = GameSettings.GetInstance().MusicVolume,
            .Location = New Point(0, 40),
            .Width = 400, .TickFrequency = 10
        }
        mainPanel.Controls.Add(musicSlider)

        AddHandler musicSlider.ValueChanged, Sub(s, e)
                                                 GameSettings.GetInstance().MusicVolume = musicSlider.Value
                                                 musicLabel.Text = musicSlider.Value & "%"
                                                 AudioManager.ApplyMusicVolume()
                                             End Sub

        Dim sfxCaption As New Label() With {
            .Text = "SOUND EFFECTS",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True, .Location = New Point(0, 100)
        }
        mainPanel.Controls.Add(sfxCaption)

        sfxLabel = New Label() With {
            .Text = GameSettings.GetInstance().SfxVolume & "%",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(218, 165, 32),
            .AutoSize = True, .Location = New Point(300, 100)
        }
        mainPanel.Controls.Add(sfxLabel)

        sfxSlider = New TrackBar() With {
            .Minimum = 0, .Maximum = 100,
            .Value = GameSettings.GetInstance().SfxVolume,
            .Location = New Point(0, 130),
            .Width = 400, .TickFrequency = 10
        }
        mainPanel.Controls.Add(sfxSlider)

        AddHandler sfxSlider.ValueChanged, Sub(s, e)
                                               GameSettings.GetInstance().SfxVolume = sfxSlider.Value
                                               sfxLabel.Text = sfxSlider.Value & "%"
                                           End Sub

        backSettingsButton = New Button() With {
            .Text = "BACK",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(180, 50, 50),
            .ForeColor = Color.White,
            .Size = New Size(180, 45),
            .Location = New Point(110, 220)
        }
        backSettingsButton.FlatAppearance.BorderSize = 0
        AddHandler backSettingsButton.Click, Sub(s, e)
                                                 AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
                                                 FadeSettingsOut()
                                             End Sub
        mainPanel.Controls.Add(backSettingsButton)
    End Sub

    Private Sub PositionButtons()
        Dim btnWidth As Integer = 220
        Dim btnHeight As Integer = 52
        Dim gap As Integer = 16
        Dim totalHeight As Integer = btnHeight * 4 + gap * 3
        Dim startY As Integer = (scenePanel.ClientSize.Height - totalHeight) \ 2 + 40
        Dim x As Integer = (scenePanel.ClientSize.Width - btnWidth) \ 2

        For i As Integer = 0 To 3
            Dim btn As PixelButton = {startButton, settingsButton, nextGameButton, exitButton}(i)
            btn.Size = New Size(btnWidth, btnHeight)
            btn.Location = New Point(x, startY + i * (btnHeight + gap))
        Next
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        FadeSettingsIn()
    End Sub

    Private Sub FadeSettingsIn()
        settingsPanel.Location = New Point((Me.ClientSize.Width - settingsPanel.Width) \ 2, (Me.ClientSize.Height - settingsPanel.Height) \ 2)
        settingsPanel.Visible = True
        settingsPanel.BringToFront()

        Dim fadeTimer As New Timer() With {.Interval = 15}
        Dim targetOpacity As Double = 1.0
        Dim currentOpacity As Double = 0.0

        AddHandler fadeTimer.Tick, Sub(s, e)
                                       currentOpacity += 0.1
                                       If currentOpacity >= targetOpacity Then
                                           currentOpacity = targetOpacity
                                           fadeTimer.Stop()
                                           fadeTimer.Dispose()
                                       End If
                                       Me.Opacity = currentOpacity
                                   End Sub
        fadeTimer.Start()
    End Sub

    Private Sub FadeSettingsOut()
        Dim fadeTimer As New Timer() With {.Interval = 15}
        Dim targetOpacity As Double = 0.0
        Dim currentOpacity As Double = 1.0

        AddHandler fadeTimer.Tick, Sub(s, e)
                                       currentOpacity -= 0.1
                                       If currentOpacity <= targetOpacity Then
                                           currentOpacity = targetOpacity
                                           fadeTimer.Stop()
                                           fadeTimer.Dispose()
                                           settingsPanel.Visible = False
                                       End If
                                       Me.Opacity = currentOpacity
                                   End Sub
        fadeTimer.Start()
    End Sub

    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        If isTransitioning Then Return
        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim gameplayForm As New Level1GameplayForm()
                                  AddHandler gameplayForm.FormClosed, Sub(s2, e2)
                                                                          isTransitioning = False
                                                                          Me.Opacity = 1.0
                                                                          Me.Show()
                                                                      End Sub
                                  Me.Hide()
                                  gameplayForm.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open gameplay: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    Private Sub NextGameButton_Click(sender As Object, e As EventArgs)
        If isTransitioning Then Return

        If Level2MenuForm.IsOpen() Then
            Level2MenuForm.BringExistingToFront()
            Return
        End If

        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim level2Menu As New Level2MenuForm()
                                  AddHandler level2Menu.FormClosed, Sub(s2, e2)
                                                                        isTransitioning = False
                                                                        Me.Opacity = 1.0
                                                                        Me.Show()
                                                                    End Sub
                                  Me.Hide()
                                  level2Menu.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open Level 2: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub FadeOutThenAction(onFadeComplete As Action)
        Dim fadeTimer As New Timer() With {.Interval = 15}
        AddHandler fadeTimer.Tick, Sub(s, e)
                                       Me.Opacity = Math.Max(0.0, Me.Opacity - 0.08)
                                       If Me.Opacity <= 0.0 Then
                                           fadeTimer.Stop()
                                           fadeTimer.Dispose()
                                           onFadeComplete()
                                       End If
                                   End Sub
        fadeTimer.Start()
    End Sub

    Private Sub Level1MenuForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        scenePanel.StopAnimation()
        If Application.OpenForms.OfType(Of Level1GameplayForm)().Count() = 0 AndAlso
           Application.OpenForms.OfType(Of Level2MenuForm)().Count() = 0 Then
            AudioManager.ShutdownAll()
        End If
    End Sub

End Class

Public Class ScenePanel
    Inherits Panel

    Private animTimer As Timer
    Private waveOffset As Integer = 0

    Private skyTile As Image
    Private skyDecor As Image
    Private bankTile As Image
    Private waterTile As Image

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        LoadAsset("enviroment\sky.png", skyTile)
        LoadAsset("enviroment\sky_decor.png", skyDecor)
        LoadAsset("tiles\Grass_rocks.png", bankTile)
        LoadAsset("tiles\water-Sheet.png", waterTile)

        animTimer = New Timer() With {.Interval = 90}
        AddHandler animTimer.Tick, Sub(s, e)
                                       waveOffset = (waveOffset + 2) Mod 64
                                       Me.Invalidate()
                                   End Sub
        animTimer.Start()
    End Sub

    Private Sub LoadAsset(relativePath As String, ByRef target As Image)
        Dim fullPath As String = IO.Path.Combine(AudioManager.AssetsRoot, relativePath)
        If IO.File.Exists(fullPath) Then
            Try
                target = Image.FromFile(fullPath)
            Catch ex As Exception
                Debug.WriteLine("ScenePanel: failed to load " & fullPath & " - " & ex.Message)
                target = Nothing
            End Try
        Else
            Debug.WriteLine("ScenePanel: asset not found: " & fullPath)
            target = Nothing
        End If
    End Sub

    Public Sub StopAnimation()
        animTimer.Stop()
        DisposeIfNotNothing(skyTile)
        DisposeIfNotNothing(skyDecor)
        DisposeIfNotNothing(bankTile)
        DisposeIfNotNothing(waterTile)
    End Sub

    Private Sub DisposeIfNotNothing(img As Image)
        If img IsNot Nothing Then
            Try
                img.Dispose()
            Catch
            End Try
        End If
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        If w <= 0 OrElse h <= 0 Then Return

        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

        If skyTile IsNot Nothing Then
            TileImage(g, skyTile, New Rectangle(0, 0, w, h))
        Else
            Using skyBrush As New Drawing2D.LinearGradientBrush(New Rectangle(0, 0, w, h), Color.FromArgb(120, 190, 235), Color.FromArgb(200, 230, 245), Drawing2D.LinearGradientMode.Vertical)
                g.FillRectangle(skyBrush, 0, 0, w, h)
            End Using
        End If

        If skyDecor IsNot Nothing Then
            g.DrawImage(skyDecor, New Rectangle(w - skyDecor.Width - 16, 16, skyDecor.Width, skyDecor.Height))
        End If

        Dim riverTop As Integer = CInt(h * 0.62)
        Dim riverHeight As Integer = CInt(h * 0.2)
        Dim riverRect As New Rectangle(0, riverTop, w, riverHeight)

        If waterTile IsNot Nothing Then
            TileImageScrolling(g, waterTile, riverRect, waveOffset)
        Else
            Using riverBrush As New SolidBrush(Color.FromArgb(70, 140, 200))
                g.FillRectangle(riverBrush, riverRect)
            End Using
        End If

        Dim bankTop As Integer = CInt(h * 0.5)
        If bankTile IsNot Nothing Then
            TileImage(g, bankTile, New Rectangle(0, bankTop, w, riverTop - bankTop))
            TileImage(g, bankTile, New Rectangle(0, riverTop + riverHeight, w, h - (riverTop + riverHeight)))
        Else
            Using grassBrush As New SolidBrush(Color.FromArgb(90, 170, 80))
                g.FillRectangle(grassBrush, 0, bankTop, w, riverTop - bankTop)
            End Using
            Using dirtBrush As New SolidBrush(Color.FromArgb(120, 90, 60))
                g.FillRectangle(dirtBrush, 0, riverTop + riverHeight, w, h - (riverTop + riverHeight))
            End Using
        End If

        Using overlay As New SolidBrush(Color.FromArgb(110, 10, 10, 20))
            g.FillRectangle(overlay, 0, 0, w, h)
        End Using

        Using titleFont As New Font("Segoe UI", 34.0F, FontStyle.Bold)
            Dim titleText As String = "RIVER CROSSING"
            Dim titleSize = g.MeasureString(titleText, titleFont)
            Dim titleX As Single = (w - titleSize.Width) / 2.0F
            Dim titleY As Single = 30
            Using shadowBrush As New SolidBrush(Color.FromArgb(160, 0, 0, 0))
                g.DrawString(titleText, titleFont, shadowBrush, titleX + 3, titleY + 3)
            End Using
            Using textBrush As New SolidBrush(Color.White)
                g.DrawString(titleText, titleFont, textBrush, titleX, titleY)
            End Using
        End Using

        MyBase.OnPaint(e)
    End Sub

    Private Sub TileImage(g As Graphics, img As Image, area As Rectangle)
        If area.Width <= 0 OrElse area.Height <= 0 Then Return
        Dim oldClip = g.Clip
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
        g.Clip = oldClip
    End Sub

    Private Sub TileImageScrolling(g As Graphics, img As Image, area As Rectangle, offsetX As Integer)
        If area.Width <= 0 OrElse area.Height <= 0 OrElse img.Width <= 0 Then Return
        Dim oldClip = g.Clip
        g.SetClip(area)
        Dim startX As Integer = area.Left - (offsetX Mod img.Width)
        Dim y As Integer = area.Top
        While y < area.Bottom
            Dim x As Integer = startX
            While x < area.Right
                g.DrawImage(img, x, y, img.Width, img.Height)
                x += img.Width
            End While
            y += img.Height
        End While
        g.Clip = oldClip
    End Sub

End Class

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
        Dim g = e.Graphics
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
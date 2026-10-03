Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' Level 2 menu: JUMP KNIGHT. Same four PixelButtons, layout and fade-out as the Level 1 menu.
'''   START      -> opens the Jump Knight game (fresh run)
'''   SETTINGS   -> fades to the settings page INSIDE this same window (no second form)
'''   HOW TO PLAY (top-left) -> fades to the tutorial page in the same window
'''   NEXT GAME  -> Level 3 placeholder
'''   EXIT       -> closes this form (the form that opened it appears again)
''' </summary>
Public Class Level2MenuForm
    Inherits Form

    Private scenePanel As Level2ScenePanel
    Private startButton As PixelButton
    Private settingsButton As PixelButton
    Private nextGameButton As PixelButton
    Private exitButton As PixelButton
    Private howToButton As PixelButton
    Private ReadOnly specs As New List(Of MenuButtonSpec)

    Private isTransitioning As Boolean = False
    Private pageOpen As Boolean = False

    ' Music (paths are relative to the shared Assets folder, same style as Level 1)
    Private Const MenuMusic As String = "Audio\Music\Jump_Knight_Game_Menu_Music.mp3"
    Private Const Level1MenuMusic As String = "Audio\Music\Wet Hands.mp3"

    ' ===================== Shared entry-point helpers =====================

    Public Shared Function IsOpen() As Boolean
        Return Application.OpenForms.OfType(Of Level2MenuForm)().Any()
    End Function

    Public Shared Sub BringExistingToFront()
        Dim game As JumpKnightGameForm = Application.OpenForms.OfType(Of JumpKnightGameForm)().FirstOrDefault()
        If game IsNot Nothing Then
            If game.WindowState = FormWindowState.Minimized Then game.WindowState = FormWindowState.Normal
            game.Activate()
            Return
        End If

        Dim menu As Level2MenuForm = Application.OpenForms.OfType(Of Level2MenuForm)().FirstOrDefault()
        If menu IsNot Nothing Then
            menu.Opacity = 1.0
            If menu.WindowState = FormWindowState.Minimized Then menu.WindowState = FormWindowState.Normal
            menu.Show()
            menu.Activate()
        End If
    End Sub

    ' ===================== Construction =====================

    Public Sub New()
        Me.Text = "Level 2 " & ChrW(8212) & " Jump Knight"
        Me.Size = New Size(900, 650)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable

        scenePanel = New Level2ScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(scenePanel)

        BuildButtons()
        PositionButtons()
        AddHandler scenePanel.Resize, Sub(s, ev) PositionButtons()
        AddHandler scenePanel.TransitionFinished, AddressOf ScenePanel_TransitionFinished
        AddHandler scenePanel.BackRequested, AddressOf ScenePanel_BackRequested
        AddHandler Me.Shown, Sub(s, ev) AudioManager.PlayMusic(MenuMusic, True)
        AddHandler Me.FormClosed, AddressOf Level2MenuForm_FormClosed
    End Sub

    Private Sub BuildButtons()
        startButton = New PixelButton("START", Color.FromArgb(76, 175, 80))
        settingsButton = New PixelButton("SETTINGS", Color.FromArgb(66, 133, 200))
        nextGameButton = New PixelButton("NEXT GAME", Color.FromArgb(150, 90, 190))
        exitButton = New PixelButton("EXIT", Color.FromArgb(200, 70, 60))
        howToButton = New PixelButton("HOW TO PLAY", Color.FromArgb(205, 140, 40))

        specs.Add(New MenuButtonSpec("START", Color.FromArgb(76, 175, 80)))
        specs.Add(New MenuButtonSpec("SETTINGS", Color.FromArgb(66, 133, 200)))
        specs.Add(New MenuButtonSpec("NEXT GAME", Color.FromArgb(150, 90, 190)))
        specs.Add(New MenuButtonSpec("EXIT", Color.FromArgb(200, 70, 60)))
        specs.Add(New MenuButtonSpec("HOW TO PLAY", Color.FromArgb(205, 140, 40)))

        AddHandler startButton.Click, AddressOf StartButton_Click
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        AddHandler nextGameButton.Click, AddressOf NextGameButton_Click
        AddHandler exitButton.Click, AddressOf ExitButton_Click
        AddHandler howToButton.Click, AddressOf HowToButton_Click

        For Each b As PixelButton In New PixelButton() {startButton, settingsButton, nextGameButton, exitButton, howToButton}
            AddHandler b.Click, Sub(s, ev) AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
            scenePanel.Controls.Add(b)
        Next
    End Sub

    Private Sub PositionButtons()
        Dim btnWidth As Integer = 220
        Dim btnHeight As Integer = 52
        Dim gap As Integer = 16
        Dim totalHeight As Integer = btnHeight * 4 + gap * 3
        Dim startY As Integer = (scenePanel.ClientSize.Height - totalHeight) \ 2 + 40
        Dim x As Integer = (scenePanel.ClientSize.Width - btnWidth) \ 2

        Dim buttons() As PixelButton = {startButton, settingsButton, nextGameButton, exitButton}
        For i As Integer = 0 To 3
            buttons(i).Size = New Size(btnWidth, btnHeight)
            buttons(i).Location = New Point(x, startY + i * (btnHeight + gap))
        Next

        ' HOW TO PLAY: small button in the upper-left corner
        howToButton.Size = New Size(140, 40)
        howToButton.Location = New Point(16, 16)
    End Sub

    ''' <summary>Current button positions, so the fading copy lines up exactly with the real buttons.</summary>
    Private Function CurrentSpecs() As List(Of MenuButtonSpec)
        Dim buttons() As PixelButton = {startButton, settingsButton, nextGameButton, exitButton, howToButton}
        For i As Integer = 0 To 4
            specs(i).Bounds = buttons(i).Bounds
        Next
        Return specs
    End Function

    Private Sub SetMenuButtonsVisible(visible As Boolean)
        startButton.Visible = visible
        settingsButton.Visible = visible
        nextGameButton.Visible = visible
        exitButton.Visible = visible
        howToButton.Visible = visible
    End Sub

    ' ===================== Buttons =====================

    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        If isTransitioning OrElse pageOpen Then Return
        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim gameForm As New JumpKnightGameForm()
                                  AddHandler gameForm.FormClosed, Sub(s2, e2)
                                                                      isTransitioning = False
                                                                      If Not Me.IsDisposed Then
                                                                          Me.Opacity = 1.0
                                                                          Me.Show()
                                                                          AudioManager.PlayMusic(MenuMusic, True)   ' back to the menu music
                                                                      End If
                                                                  End Sub
                                  Me.Hide()
                                  gameForm.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open Jump Knight: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    ' SETTINGS / HOW TO PLAY: stay in this window. The real buttons are hidden and replaced by a fading copy.
    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        OpenPage(MenuPage.Settings)
    End Sub

    Private Sub HowToButton_Click(sender As Object, e As EventArgs)
        OpenPage(MenuPage.Tutorial)
    End Sub

    Private Sub OpenPage(target As MenuPage)
        If isTransitioning OrElse pageOpen Then Return
        isTransitioning = True
        pageOpen = True
        Dim list As List(Of MenuButtonSpec) = CurrentSpecs()
        SetMenuButtonsVisible(False)
        scenePanel.BeginPageTransition(target, list)
    End Sub

    Private Sub ScenePanel_BackRequested()
        GoBackToMenu()
    End Sub

    Private Sub GoBackToMenu()
        If isTransitioning OrElse Not pageOpen Then Return
        isTransitioning = True
        AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
        scenePanel.BeginMenuTransition(CurrentSpecs())
    End Sub

    ' Raised once per completed fade. Shows the real buttons again after returning to the menu.
    Private Sub ScenePanel_TransitionFinished(toPage As Boolean)
        isTransitioning = False
        If Not toPage Then
            pageOpen = False
            SetMenuButtonsVisible(True)
        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape AndAlso pageOpen Then
            GoBackToMenu()
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub NextGameButton_Click(sender As Object, e As EventArgs)
        If isTransitioning OrElse pageOpen Then Return

        ' Only one Cannon Ball menu may exist at a time.
        If CannonBallMenuForm.IsOpen() Then
            CannonBallMenuForm.BringExistingToFront()
            Return
        End If

        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim cannonMenu As New CannonBallMenuForm()
                                  AddHandler cannonMenu.FormClosed, Sub(s2, e2)
                                                                        isTransitioning = False
                                                                        If Not Me.IsDisposed Then
                                                                            Me.Opacity = 1.0
                                                                            Me.Show()
                                                                            AudioManager.PlayMusic(MenuMusic, True)
                                                                        End If
                                                                    End Sub
                                  Me.Hide()
                                  cannonMenu.Show()
                              Catch ex As Exception
                                  isTransitioning = False
                                  Me.Opacity = 1.0
                                  MessageBox.Show("Could not open Cannon Ball: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                              End Try
                          End Sub)
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub FadeOutThenAction(onFadeComplete As Action)
        Dim fadeTimer As New System.Windows.Forms.Timer() With {.Interval = 15}
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

    Private Sub Level2MenuForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        RemoveHandler scenePanel.TransitionFinished, AddressOf ScenePanel_TransitionFinished
        RemoveHandler scenePanel.BackRequested, AddressOf ScenePanel_BackRequested
        scenePanel.StopAnimation()

        ' Leaving Level 2: give the music back to Level 1 if its menu is still open behind this window,
        ' otherwise stop the menu music.
        If Application.OpenForms.OfType(Of Level1MenuForm)().Any() Then
            AudioManager.PlayMusic(Level1MenuMusic, True)
        Else
            AudioManager.StopMusic()
        End If
    End Sub

End Class
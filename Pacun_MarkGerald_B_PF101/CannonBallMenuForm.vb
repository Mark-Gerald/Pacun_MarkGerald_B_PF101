Option Strict On
Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' Level 3 menu: CANNON BALL. Same format as the Jump Knight menu:
'''   START      -> fades out and opens the Cannon Ball game (always a fresh run)
'''   SETTINGS   -> fades to the settings page INSIDE this same window
'''   HOW TO PLAY (top-left) -> fades to the tutorial page in the same window
'''   EXIT       -> closes this form (the form that opened it appears again)
''' There is no NEXT GAME button: this is the last game.
''' </summary>
Public Class CannonBallMenuForm
    Inherits Form

    ' Music (paths are relative to the shared Assets folder)
    Private Const MenuMusic As String = "Audio\Music\Cannon_Ball_Game_Menu_Music.mp3"
    Private Const Level2MenuMusic As String = "Audio\Music\Jump_Knight_Game_Menu_Music.mp3"
    Private Const Level1MenuMusic As String = "Audio\Music\Wet Hands.mp3"
    Private Const SfxButton As String = "Audio\SFX\Button_Plate_Click.mp3"

    Private ReadOnly assets As CannonBallAssets
    Private scenePanel As CannonBallMenuPanel
    Private startButton As PixelButton
    Private settingsButton As PixelButton
    Private exitButton As PixelButton
    Private howToButton As PixelButton
    Private backButton As PixelButton

    Private ReadOnly pageFadeTimer As System.Windows.Forms.Timer
    Private isTransitioning As Boolean = False
    Private pendingPage As CBMenuPage = CBMenuPage.Main
    Private fadingOut As Boolean = False

    ' ===================== Shared entry-point helpers =====================

    Public Shared Function IsOpen() As Boolean
        Return Application.OpenForms.OfType(Of CannonBallMenuForm)().Any()
    End Function

    ''' <summary>If the game is running bring it forward, otherwise bring the menu forward.</summary>
    Public Shared Sub BringExistingToFront()
        Dim game As CannonBallGameForm = Application.OpenForms.OfType(Of CannonBallGameForm)().FirstOrDefault()
        If game IsNot Nothing Then
            If game.WindowState = FormWindowState.Minimized Then game.WindowState = FormWindowState.Normal
            game.Activate()
            Return
        End If

        Dim menu As CannonBallMenuForm = Application.OpenForms.OfType(Of CannonBallMenuForm)().FirstOrDefault()
        If menu IsNot Nothing Then
            menu.Opacity = 1.0
            If menu.WindowState = FormWindowState.Minimized Then menu.WindowState = FormWindowState.Normal
            menu.Show()
            menu.Activate()
        End If
    End Sub

    ' ===================== Construction =====================

    Public Sub New()
        Me.Text = "Level 3 " & ChrW(8212) & " Cannon Ball"
        Me.Size = New Size(900, 650)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable

        assets = New CannonBallAssets()
        scenePanel = New CannonBallMenuPanel(assets) With {.Dock = DockStyle.Fill}
        Me.Controls.Add(scenePanel)

        BuildButtons()
        PositionButtons()
        ApplyButtonVisibility()

        pageFadeTimer = New System.Windows.Forms.Timer() With {.Interval = 15}
        AddHandler pageFadeTimer.Tick, AddressOf PageFadeTimer_Tick

        AddHandler scenePanel.Resize, Sub(s, ev) PositionButtons()
        AddHandler Me.Shown, Sub(s, ev) AudioManager.PlayMusic(MenuMusic, True)
        AddHandler Me.FormClosed, AddressOf CannonBallMenuForm_FormClosed
    End Sub

    Private Sub BuildButtons()
        startButton = New PixelButton("START", Color.FromArgb(76, 175, 80))
        settingsButton = New PixelButton("SETTINGS", Color.FromArgb(66, 133, 200))
        exitButton = New PixelButton("EXIT", Color.FromArgb(200, 70, 60))
        howToButton = New PixelButton("HOW TO PLAY", Color.FromArgb(205, 140, 40))
        backButton = New PixelButton("BACK", Color.FromArgb(200, 70, 60))

        AddHandler startButton.Click, AddressOf StartButton_Click
        AddHandler settingsButton.Click, Sub(s, ev) BeginTransition(CBMenuPage.Settings)
        AddHandler howToButton.Click, Sub(s, ev) BeginTransition(CBMenuPage.Tutorial)
        AddHandler exitButton.Click, AddressOf ExitButton_Click
        AddHandler backButton.Click, Sub(s, ev) BeginTransition(CBMenuPage.Main)

        For Each b As PixelButton In New PixelButton() {startButton, settingsButton, exitButton, howToButton, backButton}
            AddHandler b.Click, Sub(s, ev) AudioManager.PlaySfx(SfxButton)
            scenePanel.Controls.Add(b)
        Next
    End Sub

    Private Sub PositionButtons()
        If scenePanel Is Nothing Then Return
        Dim btnWidth As Integer = 220
        Dim btnHeight As Integer = 52
        Dim gap As Integer = 16
        Dim totalHeight As Integer = btnHeight * 3 + gap * 2
        ' keep the buttons below the title, the block row and the bouncing ball
        Dim startY As Integer = Math.Max(250, (scenePanel.ClientSize.Height - totalHeight) \ 2 + 80)
        Dim x As Integer = (scenePanel.ClientSize.Width - btnWidth) \ 2

        Dim buttons() As PixelButton = {startButton, settingsButton, exitButton}
        For i As Integer = 0 To 2
            buttons(i).Size = New Size(btnWidth, btnHeight)
            buttons(i).Location = New Point(x, startY + i * (btnHeight + gap))
        Next

        howToButton.Size = New Size(140, 40)
        howToButton.Location = New Point(16, 16)

        backButton.Size = New Size(200, 44)
        backButton.Location = New Point((scenePanel.ClientSize.Width - backButton.Width) \ 2, scenePanel.CardRect().Bottom + 14)
    End Sub

    ''' <summary>Main page shows START / SETTINGS / EXIT / HOW TO PLAY; the other pages show only BACK.</summary>
    Private Sub ApplyButtonVisibility()
        Dim onMain As Boolean = (scenePanel.Page = CBMenuPage.Main)
        startButton.Visible = onMain
        settingsButton.Visible = onMain
        exitButton.Visible = onMain
        howToButton.Visible = onMain
        backButton.Visible = Not onMain
    End Sub

    ' ===================== Page transitions (fade inside this window) =====================

    Private Sub BeginTransition(target As CBMenuPage)
        If isTransitioning OrElse target = scenePanel.Page Then Return
        isTransitioning = True
        pendingPage = target
        fadingOut = True
        startButton.Visible = False
        settingsButton.Visible = False
        exitButton.Visible = False
        howToButton.Visible = False
        backButton.Visible = False
        pageFadeTimer.Start()
    End Sub

    Private Sub PageFadeTimer_Tick(sender As Object, e As EventArgs)
        If fadingOut Then
            scenePanel.FadeAmount = Math.Min(1.0F, scenePanel.FadeAmount + 0.15F)
            If scenePanel.FadeAmount >= 1.0F Then
                scenePanel.Page = pendingPage
                PositionButtons()
                fadingOut = False
            End If
        Else
            scenePanel.FadeAmount = Math.Max(0.0F, scenePanel.FadeAmount - 0.15F)
            If scenePanel.FadeAmount <= 0.0F Then
                pageFadeTimer.Stop()
                isTransitioning = False
                ApplyButtonVisibility()
            End If
        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape AndAlso scenePanel.Page <> CBMenuPage.Main Then
            BeginTransition(CBMenuPage.Main)
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' ===================== START / EXIT =====================

    Private Sub StartButton_Click(sender As Object, e As EventArgs)
        If isTransitioning Then Return
        isTransitioning = True
        FadeOutThenAction(Sub()
                              Try
                                  Dim gameForm As New CannonBallGameForm()
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

    ' ===================== Cleanup =====================

    Private Sub CannonBallMenuForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        pageFadeTimer.Stop()
        RemoveHandler pageFadeTimer.Tick, AddressOf PageFadeTimer_Tick
        pageFadeTimer.Dispose()
        scenePanel.StopAnimation()

        ' Leaving Cannon Ball: give the music back to whichever earlier menu is still open behind this window.
        If Application.OpenForms.OfType(Of Level2MenuForm)().Any() Then
            AudioManager.PlayMusic(Level2MenuMusic, True)
        ElseIf Application.OpenForms.OfType(Of Level1MenuForm)().Any() Then
            AudioManager.PlayMusic(Level1MenuMusic, True)
        Else
            AudioManager.StopMusic()
        End If

        assets.Dispose()
    End Sub

End Class
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' The Jump Knight window. ONE game timer + ONE countdown timer, both created once.
''' Pause button, P / Esc and RESUME all use the same pause code. The pause screen has its own
''' SETTINGS page. Gameplay music: starts at the first move, pauses with the game, resumes after
''' the countdown, stops when the knight dies and starts again from the beginning on the next run.
''' </summary>
Public Class JumpKnightGameForm
    Inherits Form

    ' ---- Sound files (paths are relative to the shared Assets folder) ----
    Private Const SfxJump As String = "Audio\SFX\knight_jump_sound_effects.mp3"
    Private Const SfxSpring As String = "Audio\SFX\knight_spring_hammer_sound_effect.mp3"
    Private Const SfxMeat As String = "Audio\SFX\knight_meat_poweup_sound_effect.mp3"
    Private Const SfxDoubleJump As String = "Audio\SFX\knight_double_jump_sound_effects.mp3"
    Private Const SfxWoodBreak As String = "Audio\SFX\knight_wood_breaking_sound_effect.mp3"
    Private Const SfxAttack As String = "Audio\SFX\knight_sword_whip_sound_effect.mp3"
    Private Const SfxBatHit As String = "Audio\SFX\knight_bat_death_sound_effect.mp3"
    Private Const SfxGameOver As String = "Audio\SFX\knight_death_gameover_sound_effect.mp3"
    Private Const MusicTrack As String = "Audio\Music\knight_jump_Music_Track_sound_effect.mp3"

    ' ---- Volume of each effect relative to the SFX slider (1.0 = full, 0.3 = 30 percent) ----
    ' Power-up sounds are the loudest; the constantly repeating jump sounds are the quietest.
    Private Const GainJump As Double = 0.3
    Private Const GainDoubleJump As Double = 0.3
    Private Const GainSpring As Double = 1.0
    Private Const GainMeat As Double = 1.0
    Private Const GainWoodBreak As Double = 0.7
    Private Const GainAttack As Double = 0.6
    Private Const GainBatHit As Double = 0.7
    Private Const GainGameOver As Double = 0.8

    ' The same sound can never start twice within this many milliseconds.
    Private Const SfxMinGapMs As Long = 70

    Private Const StepSeconds As Single = 1.0F / 60.0F
    Private Const MaxFrameTime As Single = 0.1F
    Private Const CountdownStepMs As Integer = 700

    Private ReadOnly engine As JumpKnightEngine
    Private ReadOnly assets As JumpKnightAssets
    Private ReadOnly renderPanel As JumpKnightPanel
    Private ReadOnly gameTimer As System.Windows.Forms.Timer
    Private ReadOnly countdownTimer As System.Windows.Forms.Timer
    Private ReadOnly clock As New System.Diagnostics.Stopwatch()
    Private ReadOnly sfxClock As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
    Private ReadOnly lastSfxMs As New Dictionary(Of String, Long)
    Private accumulator As Single

    Private leftHeld As Boolean
    Private rightHeld As Boolean
    Private jumpHeld As Boolean
    Private attackHeld As Boolean
    Private pauseKeyHeld As Boolean

    ' paused = the game is frozen (pause screen OR resume countdown).
    Private paused As Boolean
    Private countingDown As Boolean
    Private countdownStep As Integer
    Private gameplayMusicOn As Boolean = False   ' True from the first move until the knight dies

    Private retryButton As PixelButton
    Private resumeButton As PixelButton
    Private settingsButton As PixelButton
    Private backButton As PixelButton
    Private menuButton As PixelButton

    Public Sub New()
        Me.Text = "Level 2 " & ChrW(8212) & " Jump Knight"
        Me.ClientSize = New Size(480, 720)
        Me.MinimumSize = New Size(440, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.KeyPreview = True
        Me.BackColor = Color.Black
        Me.DoubleBuffered = True

        engine = New JumpKnightEngine()
        assets = New JumpKnightAssets()

        renderPanel = New JumpKnightPanel(engine, assets) With {.Dock = DockStyle.Fill}
        Me.Controls.Add(renderPanel)

        BuildButtons()

        gameTimer = New System.Windows.Forms.Timer() With {.Interval = 15}
        AddHandler gameTimer.Tick, AddressOf GameTimer_Tick

        countdownTimer = New System.Windows.Forms.Timer() With {.Interval = CountdownStepMs}
        AddHandler countdownTimer.Tick, AddressOf CountdownTimer_Tick

        AddHandler renderPanel.PauseClicked, AddressOf RenderPanel_PauseClicked

        AddHandler engine.Bounced, AddressOf Engine_Bounced
        AddHandler engine.SpringUsed, AddressOf Engine_SpringUsed
        AddHandler engine.MeatCollected, AddressOf Engine_MeatCollected
        AddHandler engine.DoubleJumped, AddressOf Engine_DoubleJumped
        AddHandler engine.WoodBroke, AddressOf Engine_WoodBroke
        AddHandler engine.Attacked, AddressOf Engine_Attacked
        AddHandler engine.BatHit, AddressOf Engine_BatHit
        AddHandler engine.GameEnded, AddressOf Engine_GameEnded

        AddHandler Me.KeyDown, AddressOf Form_KeyDown
        AddHandler Me.KeyUp, AddressOf Form_KeyUp
        AddHandler Me.Deactivate, AddressOf Form_Deactivate
        AddHandler Me.Resize, Sub(s, ev) PositionButtons()
        AddHandler Me.Shown, AddressOf Form_Shown
        AddHandler Me.FormClosed, AddressOf Form_Closed
    End Sub

    ' ===================== Setup =====================

    Private Sub BuildButtons()
        retryButton = New PixelButton("RETRY", Color.FromArgb(76, 175, 80))
        resumeButton = New PixelButton("RESUME", Color.FromArgb(76, 175, 80))
        settingsButton = New PixelButton("SETTINGS", Color.FromArgb(66, 133, 200))
        backButton = New PixelButton("BACK", Color.FromArgb(200, 70, 60))
        menuButton = New PixelButton("MENU", Color.FromArgb(200, 70, 60))

        AddHandler retryButton.Click, AddressOf RetryButton_Click
        AddHandler resumeButton.Click, AddressOf ResumeButton_Click
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        AddHandler backButton.Click, AddressOf BackButton_Click
        AddHandler menuButton.Click, AddressOf MenuButton_Click

        For Each b As PixelButton In New PixelButton() {retryButton, resumeButton, settingsButton, backButton, menuButton}
            b.Size = New Size(200, 50)
            b.Visible = False
            renderPanel.Controls.Add(b)
        Next
    End Sub

    Private Sub PlaceButton(b As PixelButton, logicalY As Single)
        Dim p As Point = renderPanel.LogicalToScreen(JumpKnightEngine.WorldW / 2.0F, logicalY)
        b.Location = New Point(p.X - b.Width \ 2, p.Y)
    End Sub

    ' Pause screen: RESUME / SETTINGS / MENU.  Game over: RETRY / MENU.  Pause settings page: BACK.
    Private Sub PositionButtons()
        If renderPanel Is Nothing Then Return
        If renderPanel.Overlay = JKOverlay.Paused Then
            PlaceButton(resumeButton, 330)
            PlaceButton(settingsButton, 400)
            PlaceButton(menuButton, 470)
            PlaceButton(backButton, 420)
        Else
            PlaceButton(retryButton, 360)
            PlaceButton(menuButton, 440)
        End If
    End Sub

    Private Sub ShowPauseButtons(settingsPage As Boolean)
        resumeButton.Visible = Not settingsPage
        settingsButton.Visible = Not settingsPage
        menuButton.Visible = Not settingsPage
        backButton.Visible = settingsPage
        retryButton.Visible = False
        renderPanel.SettingsOpen = settingsPage
    End Sub

    Private Sub HideAllOverlayButtons()
        retryButton.Visible = False
        resumeButton.Visible = False
        settingsButton.Visible = False
        backButton.Visible = False
        menuButton.Visible = False
        renderPanel.SettingsOpen = False
    End Sub

    Private Sub Form_Shown(sender As Object, e As EventArgs)
        PositionButtons()
        AudioManager.StopMusic()          ' menu music ends here; gameplay music starts at the first move
        gameplayMusicOn = False
        accumulator = 0.0F
        clock.Restart()
        gameTimer.Start()
        Me.Focus()
    End Sub

    ' ===================== Game loop =====================

    Private Sub GameTimer_Tick(sender As Object, e As EventArgs)
        Dim elapsed As Single = CSng(clock.Elapsed.TotalSeconds)
        clock.Restart()
        accumulator += Math.Min(elapsed, MaxFrameTime)

        Dim moveDir As Integer = 0
        If rightHeld Then moveDir += 1
        If leftHeld Then moveDir -= 1

        Do While accumulator >= StepSeconds
            engine.Update(StepSeconds, moveDir)
            accumulator -= StepSeconds
        Loop

        renderPanel.Invalidate()
    End Sub

    ' ===================== Keyboard =====================

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Select Case keyData
            Case Keys.Left
                If Not paused Then
                    leftHeld = True
                    StartRunIfReady()
                End If
                Return True
            Case Keys.Right
                If Not paused Then
                    rightHeld = True
                    StartRunIfReady()
                End If
                Return True
            Case Keys.Up
                PressJump()
                Return True
            Case Keys.Escape, Keys.P
                If Not pauseKeyHeld Then
                    pauseKeyHeld = True
                    TogglePause()
                End If
                Return True
        End Select
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub Form_KeyDown(sender As Object, e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.A
                If Not paused Then
                    leftHeld = True
                    StartRunIfReady()
                End If
            Case Keys.D
                If Not paused Then
                    rightHeld = True
                    StartRunIfReady()
                End If
            Case Keys.W
                PressJump()
            Case Keys.Space
                PressAttack()
        End Select
        e.Handled = True
    End Sub

    Private Sub Form_KeyUp(sender As Object, e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.Left, Keys.A
                leftHeld = False
            Case Keys.Right, Keys.D
                rightHeld = False
            Case Keys.Up, Keys.W
                jumpHeld = False
            Case Keys.Space
                attackHeld = False
            Case Keys.P, Keys.Escape
                pauseKeyHeld = False
        End Select
    End Sub

    Private Sub PressJump()
        If jumpHeld Then Return
        jumpHeld = True
        If paused Then Return
        If engine.State = JKState.Ready Then
            StartRunIfReady()
        Else
            engine.RequestDoubleJump()
        End If
    End Sub

    Private Sub PressAttack()
        If attackHeld Then Return
        attackHeld = True
        If paused Then Return
        engine.TryAttack()
    End Sub

    Private Sub StartRunIfReady()
        If paused Then Return
        If engine.State = JKState.Ready Then
            engine.Begin()
            StartGameplayMusic()          ' the knight just moved for the first time
        End If
    End Sub

    Private Sub ClearKeys()
        leftHeld = False
        rightHeld = False
        jumpHeld = False
        attackHeld = False
        pauseKeyHeld = False
    End Sub

    ' ===================== Gameplay music =====================

    Private Sub StartGameplayMusic()
        If gameplayMusicOn Then Return
        gameplayMusicOn = True
        AudioManager.PlayMusic(MusicTrack, True)   ' loops until the knight dies
    End Sub

    Private Sub StopGameplayMusic()
        gameplayMusicOn = False
        AudioManager.StopMusic()                   ' the next run starts again from the beginning
    End Sub

    ' ===================== Pause / settings / resume countdown =====================

    Private Sub Form_Deactivate(sender As Object, e As EventArgs)
        ClearKeys()
        If engine.State = JKState.Playing AndAlso (Not paused OrElse countingDown) Then PauseGame()
    End Sub

    Private Sub RenderPanel_PauseClicked()
        PauseGame()
    End Sub

    ''' <summary>P / Esc: pause, leave the pause settings page, cancel a countdown, or start the resume countdown.</summary>
    Private Sub TogglePause()
        If engine.State <> JKState.Playing Then Return
        If countingDown Then
            PauseGame()                          ' cancel the countdown, back to the pause screen
        ElseIf paused Then
            If renderPanel.SettingsOpen Then
                CloseSettingsPage()              ' settings -> pause screen (does NOT resume)
            Else
                BeginResumeCountdown()
            End If
        Else
            PauseGame()
        End If
    End Sub

    ''' <summary>Freezes everything and shows the PAUSED screen. Safe to call more than once.</summary>
    Private Sub PauseGame()
        If engine.State <> JKState.Playing Then Return
        StopCountdown()
        paused = True
        gameTimer.Stop()
        clock.Stop()
        ClearKeys()
        If gameplayMusicOn Then AudioManager.PauseMusic()      ' the music pauses with the game
        renderPanel.Overlay = JKOverlay.Paused
        ShowPauseButtons(False)
        PositionButtons()
        renderPanel.Invalidate()
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        If Not paused OrElse countingDown Then Return
        ShowPauseButtons(True)
        PositionButtons()
        renderPanel.Invalidate()
    End Sub

    Private Sub BackButton_Click(sender As Object, e As EventArgs)
        CloseSettingsPage()
    End Sub

    Private Sub CloseSettingsPage()
        If Not paused OrElse countingDown Then Return
        ShowPauseButtons(False)
        PositionButtons()
        renderPanel.Invalidate()
    End Sub

    Private Sub ResumeButton_Click(sender As Object, e As EventArgs)
        BeginResumeCountdown()
    End Sub

    ''' <summary>Hides the pause screen and counts 3, 2, 1, GO! with the game (and music) still frozen.</summary>
    Private Sub BeginResumeCountdown()
        If Not paused OrElse countingDown Then Return
        If engine.State <> JKState.Playing Then Return

        countingDown = True
        countdownStep = 3
        renderPanel.Overlay = JKOverlay.None
        HideAllOverlayButtons()
        renderPanel.CountdownText = "3"
        countdownTimer.Stop()
        countdownTimer.Start()
        renderPanel.Invalidate()
    End Sub

    Private Sub CountdownTimer_Tick(sender As Object, e As EventArgs)
        countdownStep -= 1
        If countdownStep >= 1 Then
            renderPanel.CountdownText = countdownStep.ToString()
        ElseIf countdownStep = 0 Then
            renderPanel.CountdownText = "GO!"
            ResumeGameplay()
        Else
            StopCountdown()
        End If
        renderPanel.Invalidate()
    End Sub

    Private Sub ResumeGameplay()
        countingDown = False
        paused = False
        If gameplayMusicOn Then AudioManager.ResumeMusic()     ' only after the countdown has finished
        accumulator = 0.0F
        clock.Restart()
        gameTimer.Start()
        Me.Focus()
    End Sub

    Private Sub StopCountdown()
        countdownTimer.Stop()
        countingDown = False
        renderPanel.CountdownText = ""
    End Sub

    Private Sub RetryButton_Click(sender As Object, e As EventArgs)
        StopCountdown()
        StopGameplayMusic()               ' silent until the knight moves again, then it starts from the beginning
        engine.Reset()
        ClearKeys()
        paused = False
        renderPanel.Overlay = JKOverlay.None
        HideAllOverlayButtons()
        accumulator = 0.0F
        clock.Restart()
        gameTimer.Start()
        renderPanel.Invalidate()
        Me.Focus()
    End Sub

    Private Sub MenuButton_Click(sender As Object, e As EventArgs)
        Me.Close()   ' the Jump Knight menu re-appears from its FormClosed handler
    End Sub

    ' ===================== Engine events =====================

    Private Sub Engine_GameEnded()
        StopCountdown()
        gameTimer.Stop()
        clock.Stop()
        StopGameplayMusic()               ' the music stops when the knight dies
        PlaySfx(SfxGameOver, GainGameOver)
        renderPanel.Overlay = JKOverlay.GameOver
        HideAllOverlayButtons()
        retryButton.Visible = True
        menuButton.Visible = True
        PositionButtons()
        renderPanel.Invalidate()
    End Sub

    Private Sub Engine_Bounced(kind As JKPlatformKind)
        If kind = JKPlatformKind.Wood Then Return      ' wood plays its breaking sound instead of the jump sound
        PlaySfx(SfxJump, GainJump)
    End Sub

    Private Sub Engine_SpringUsed()
        PlaySfx(SfxSpring, GainSpring)
    End Sub

    Private Sub Engine_MeatCollected()
        PlaySfx(SfxMeat, GainMeat)
    End Sub

    Private Sub Engine_DoubleJumped()
        PlaySfx(SfxDoubleJump, GainDoubleJump)
    End Sub

    Private Sub Engine_WoodBroke()
        PlaySfx(SfxWoodBreak, GainWoodBreak)
    End Sub

    Private Sub Engine_Attacked()
        PlaySfx(SfxAttack, GainAttack)
    End Sub

    Private Sub Engine_BatHit()
        PlaySfx(SfxBatHit, GainBatHit)
    End Sub

    ' Plays a sound effect, ignoring a repeat of the same sound within SfxMinGapMs.
    Private Sub PlaySfx(relativePath As String, gain As Double)
        If relativePath = "" Then Return
        Dim nowMs As Long = sfxClock.ElapsedMilliseconds
        Dim lastMs As Long
        If lastSfxMs.TryGetValue(relativePath, lastMs) AndAlso nowMs - lastMs < SfxMinGapMs Then Return
        lastSfxMs(relativePath) = nowMs
        AudioManager.PlaySfx(relativePath, gain)
    End Sub

    ' ===================== Cleanup =====================

    Private Sub Form_Closed(sender As Object, e As FormClosedEventArgs)
        gameTimer.Stop()
        RemoveHandler gameTimer.Tick, AddressOf GameTimer_Tick
        gameTimer.Dispose()

        countdownTimer.Stop()
        RemoveHandler countdownTimer.Tick, AddressOf CountdownTimer_Tick
        countdownTimer.Dispose()

        RemoveHandler renderPanel.PauseClicked, AddressOf RenderPanel_PauseClicked
        RemoveHandler engine.Bounced, AddressOf Engine_Bounced
        RemoveHandler engine.SpringUsed, AddressOf Engine_SpringUsed
        RemoveHandler engine.MeatCollected, AddressOf Engine_MeatCollected
        RemoveHandler engine.DoubleJumped, AddressOf Engine_DoubleJumped
        RemoveHandler engine.WoodBroke, AddressOf Engine_WoodBroke
        RemoveHandler engine.Attacked, AddressOf Engine_Attacked
        RemoveHandler engine.BatHit, AddressOf Engine_BatHit
        RemoveHandler engine.GameEnded, AddressOf Engine_GameEnded

        AudioManager.StopMusic()          ' the Level 2 menu starts its own music when it re-appears
        renderPanel.Dispose()
        assets.Dispose()
    End Sub

End Class
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' The Cannon Ball window. ONE form for all five levels, the pause screen, the game-over screen and the
''' victory screen. Level changes are a fade inside this same window (never a new Form).
''' ONE game timer + ONE countdown timer, both created once. Physics advance in fixed 1/120 s steps.
''' </summary>
Public Class CannonBallGameForm
    Inherits Form

    ' ---- Sound files (relative to the shared Assets folder). Missing files are skipped silently. ----
    Private Const SfxButton As String = "Audio\SFX\Button_Plate_Click.mp3"
    Private Const SfxLock As String = "Audio\SFX\cannon_lock.mp3"
    Private Const SfxLaunch As String = "Audio\SFX\cannon_launch.mp3"
    Private Const SfxWall As String = "Audio\SFX\cannon_wall_bounce.mp3"
    Private Const SfxCart As String = "Audio\SFX\cannon_paddle_bounce.mp3"
    Private Const SfxBrickBreak As String = "Audio\SFX\cannon_brick_break.mp3"
    Private Const SfxStoneHit As String = "Audio\SFX\cannon_stone_hit.mp3"
    Private Const SfxGoldBreak As String = "Audio\SFX\cannon_gold_break.mp3"
    Private Const SfxEmerald As String = "Audio\SFX\cannon_emerald.mp3"
    Private Const SfxLifeLost As String = "Audio\SFX\cannon_life_lost.mp3"
    Private Const SfxLevelDone As String = "Audio\SFX\cannon_level_complete.mp3"
    Private Const SfxGameOver As String = "Audio\SFX\cannon_game_over.mp3"
    Private Const SfxVictory As String = "Audio\SFX\cannon_victory.mp3"
    Private Const MusicTrack As String = "Audio\Music\Cannon_Ball_Music_Track.mp3"

    ' Volume of each effect relative to the SFX slider (1.0 = full)
    Private Const GainLock As Double = 0.8
    Private Const GainLaunch As Double = 0.9
    Private Const GainWall As Double = 0.4
    Private Const GainCart As Double = 0.6
    Private Const GainBlock As Double = 0.8
    Private Const GainBig As Double = 1.0

    Private Const SfxMinGapMs As Long = 45          ' the same sound never starts twice within this time
    Private Const MaxFrameTime As Single = 0.1F
    Private Const CountdownStepMs As Integer = 700

    Private Enum FadeStage
        None
        FadingOut
        FadingIn
    End Enum

    Private Enum UiMode
        None
        Pause
        PauseSettings
        GameOver
        Victory
    End Enum

    Private ReadOnly engine As CannonBallEngine
    Private ReadOnly assets As CannonBallAssets
    Private ReadOnly renderPanel As CannonBallPanel
    Private ReadOnly gameTimer As System.Windows.Forms.Timer
    Private ReadOnly countdownTimer As System.Windows.Forms.Timer
    Private ReadOnly clock As New System.Diagnostics.Stopwatch()
    Private ReadOnly sfxClock As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
    Private ReadOnly lastSfxMs As New Dictionary(Of String, Long)
    Private accumulator As Single

    Private leftHeld As Boolean
    Private rightHeld As Boolean
    Private actionHeld As Boolean
    Private pauseKeyHeld As Boolean

    Private paused As Boolean
    Private countingDown As Boolean
    Private countdownStep As Integer
    Private gameplayMusicOn As Boolean

    Private currentFadeStage As FadeStage = FadeStage.None
    Private fadeValue As Single
    Private currentUiMode As UiMode = UiMode.None

    Private retryButton As PixelButton
    Private resumeButton As PixelButton
    Private settingsButton As PixelButton
    Private backButton As PixelButton
    Private menuButton As PixelButton

    Public Sub New()
        Me.Text = "Level 3 " & ChrW(8212) & " Cannon Ball"
        Me.ClientSize = New Size(450, 720)
        Me.MinimumSize = New Size(420, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.KeyPreview = True
        Me.BackColor = Color.Black
        Me.DoubleBuffered = True

        engine = New CannonBallEngine()
        assets = New CannonBallAssets()

        renderPanel = New CannonBallPanel(engine, assets) With {.Dock = DockStyle.Fill}
        Me.Controls.Add(renderPanel)

        BuildButtons()

        gameTimer = New System.Windows.Forms.Timer() With {.Interval = 15}
        AddHandler gameTimer.Tick, AddressOf GameTimer_Tick

        countdownTimer = New System.Windows.Forms.Timer() With {.Interval = CountdownStepMs}
        AddHandler countdownTimer.Tick, AddressOf CountdownTimer_Tick

        AddHandler engine.CartLocked, AddressOf Engine_CartLocked
        AddHandler engine.Launched, AddressOf Engine_Launched
        AddHandler engine.WallBounced, AddressOf Engine_WallBounced
        AddHandler engine.CartBounced, AddressOf Engine_CartBounced
        AddHandler engine.BlockDamaged, AddressOf Engine_BlockDamaged
        AddHandler engine.BlockDestroyed, AddressOf Engine_BlockDestroyed
        AddHandler engine.LifeGained, AddressOf Engine_LifeGained
        AddHandler engine.LifeLost, AddressOf Engine_LifeLost
        AddHandler engine.LevelCompleted, AddressOf Engine_LevelCompleted
        AddHandler engine.LevelClearFinished, AddressOf Engine_LevelClearFinished
        AddHandler engine.GameEnded, AddressOf Engine_GameEnded
        AddHandler engine.Won, AddressOf Engine_Won

        AddHandler Me.KeyDown, AddressOf Form_KeyDown
        AddHandler Me.KeyUp, AddressOf Form_KeyUp
        AddHandler Me.Deactivate, AddressOf Form_Deactivate
        AddHandler Me.Resize, Sub(s, ev) PositionButtons()
        AddHandler Me.Shown, AddressOf Form_Shown
        AddHandler Me.FormClosed, AddressOf Form_Closed
    End Sub

    ' ===================== Setup =====================

    Private Sub BuildButtons()
        retryButton = New PixelButton("TRY AGAIN", Color.FromArgb(76, 175, 80))
        resumeButton = New PixelButton("RESUME", Color.FromArgb(76, 175, 80))
        settingsButton = New PixelButton("SETTINGS", Color.FromArgb(66, 133, 200))
        backButton = New PixelButton("BACK", Color.FromArgb(200, 70, 60))
        menuButton = New PixelButton("MAIN MENU", Color.FromArgb(200, 70, 60))

        AddHandler retryButton.Click, AddressOf RetryButton_Click
        AddHandler resumeButton.Click, AddressOf ResumeButton_Click
        AddHandler settingsButton.Click, AddressOf SettingsButton_Click
        AddHandler backButton.Click, AddressOf BackButton_Click
        AddHandler menuButton.Click, AddressOf MenuButton_Click

        For Each b As PixelButton In New PixelButton() {retryButton, resumeButton, settingsButton, backButton, menuButton}
            b.Size = New Size(220, 50)
            b.Visible = False
            AddHandler b.Click, Sub(s, ev) AudioManager.PlaySfx(SfxButton)
            renderPanel.Controls.Add(b)
        Next
    End Sub

    Private Sub PlaceButton(b As PixelButton, logicalY As Single)
        Dim p As Point = renderPanel.LogicalToScreen(CannonBallEngine.WorldW / 2.0F, logicalY)
        b.Location = New Point(p.X - b.Width \ 2, p.Y)
    End Sub

    ' Button rows are in view space (the 640-unit tall view, HUD included); the panel draws its text to match.
    Private Sub PositionButtons()
        If renderPanel Is Nothing Then Return
        Select Case currentUiMode
            Case UiMode.Pause
                PlaceButton(resumeButton, 270)
                PlaceButton(settingsButton, 340)
                PlaceButton(menuButton, 410)
            Case UiMode.PauseSettings
                PlaceButton(backButton, 430)
            Case UiMode.GameOver
                PlaceButton(retryButton, 350)
                PlaceButton(menuButton, 420)
            Case UiMode.Victory
                PlaceButton(retryButton, 360)
                PlaceButton(menuButton, 430)
        End Select
    End Sub

    Private Sub SetUiMode(mode As UiMode)
        currentUiMode = mode
        resumeButton.Visible = (mode = uiMode.Pause)
        settingsButton.Visible = (mode = uiMode.Pause)
        backButton.Visible = (mode = uiMode.PauseSettings)
        retryButton.Visible = (mode = uiMode.GameOver OrElse mode = uiMode.Victory)
        menuButton.Visible = (mode = uiMode.Pause OrElse mode = uiMode.GameOver OrElse mode = uiMode.Victory)
        renderPanel.SettingsOpen = (mode = uiMode.PauseSettings)
        PositionButtons()
    End Sub

    Private Sub Form_Shown(sender As Object, e As EventArgs)
        PositionButtons()
        StartGameplayMusic()
        accumulator = 0.0F
        currentFadeStage = FadeStage.FadingIn          ' the first level fades in from black
        fadeValue = 1.0F
        renderPanel.FadeAmount = 1.0F
        clock.Restart()
        gameTimer.Start()
        Me.Focus()
    End Sub

    ' ===================== Game loop =====================

    Private Sub GameTimer_Tick(sender As Object, e As EventArgs)
        Dim elapsed As Single = Math.Min(CSng(clock.Elapsed.TotalSeconds), MaxFrameTime)
        clock.Restart()
        accumulator += elapsed

        Dim moveDir As Integer = 0
        If rightHeld Then moveDir += 1
        If leftHeld Then moveDir -= 1

        ' Fixed time step: the same physics at 60 FPS, 144 FPS, or anything else.
        Do While accumulator >= CannonBallEngine.FixedStep
            engine.Update(CannonBallEngine.FixedStep, moveDir)
            accumulator -= CannonBallEngine.FixedStep
        Loop

        UpdateFade(elapsed)
        renderPanel.Invalidate()
    End Sub

    ''' <summary>
    ''' Level transition: fade to black, load the next level (or the victory screen) at the darkest moment,
    ''' then fade back in. It all happens inside this one window.
    ''' </summary>
    Private Sub UpdateFade(dt As Single)
        Select Case currentFadeStage
            Case FadeStage.FadingOut
                fadeValue += dt / CannonBallEngine.FadeOutSeconds
                If fadeValue >= 1.0F Then
                    fadeValue = 1.0F
                    engine.AdvanceLevel()                 ' Level + 1 (cart position, score and lives are kept) or Victory
                    currentFadeStage = FadeStage.FadingIn
                End If
            Case FadeStage.FadingIn
                fadeValue -= dt / CannonBallEngine.FadeInSeconds
                If fadeValue <= 0.0F Then
                    fadeValue = 0.0F
                    currentFadeStage = FadeStage.None
                    If engine.State = CBState.Victory Then SetUiMode(UiMode.Victory)
                End If
        End Select
        renderPanel.FadeAmount = fadeValue
    End Sub

    ' ===================== Keyboard =====================

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Select Case keyData
            Case Keys.Left
                If Not paused Then leftHeld = True
                Return True
            Case Keys.Right
                If Not paused Then rightHeld = True
                Return True
            Case Keys.Up, Keys.Down
                Return True
            Case Keys.Space
                PressAction()
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
                If Not paused Then leftHeld = True
            Case Keys.D
                If Not paused Then rightHeld = True
        End Select
        e.Handled = True
    End Sub

    Private Sub Form_KeyUp(sender As Object, e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.Left, Keys.A
                leftHeld = False
            Case Keys.Right, Keys.D
                rightHeld = False
            Case Keys.Space
                actionHeld = False
            Case Keys.P, Keys.Escape
                pauseKeyHeld = False
        End Select
    End Sub

    Private Sub PressAction()
        If actionHeld Then Return          ' ignore key auto-repeat
        actionHeld = True
        If paused Then Return
        engine.PressAction()
    End Sub

    Private Sub ClearKeys()
        leftHeld = False
        rightHeld = False
        actionHeld = False
        pauseKeyHeld = False
    End Sub

    ' ===================== Music =====================

    Private Sub StartGameplayMusic()
        If gameplayMusicOn Then Return
        gameplayMusicOn = True
        AudioManager.StopMusic()                  ' menu music ends here
        AudioManager.PlayMusic(MusicTrack, True)
    End Sub

    Private Sub StopGameplayMusic()
        gameplayMusicOn = False
        AudioManager.StopMusic()
    End Sub

    ' ===================== Pause / settings / resume countdown =====================

    Private Sub Form_Deactivate(sender As Object, e As EventArgs)
        ClearKeys()
        If engine.CanPause AndAlso currentFadeStage <> FadeStage.FadingOut AndAlso (Not paused OrElse countingDown) Then PauseGame()
    End Sub

    ''' <summary>ESC / P: pause, leave the pause settings, cancel a countdown, or start the resume countdown.</summary>
    Private Sub TogglePause()
        If Not engine.CanPause OrElse currentFadeStage = FadeStage.FadingOut Then Return
        If countingDown Then
            PauseGame()
        ElseIf paused Then
            If currentUiMode = UiMode.PauseSettings Then
                SetUiMode(UiMode.Pause)
                renderPanel.Invalidate()
            Else
                BeginResumeCountdown()
            End If
        Else
            PauseGame()
        End If
    End Sub

    ''' <summary>Freezes physics, timer and the level time. Safe to call more than once.</summary>
    Private Sub PauseGame()
        If Not engine.CanPause Then Return
        StopCountdown()
        paused = True
        gameTimer.Stop()
        clock.Stop()
        ClearKeys()
        If gameplayMusicOn Then AudioManager.PauseMusic()
        renderPanel.Paused = True
        SetUiMode(uiMode.Pause)
        renderPanel.Invalidate()
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As EventArgs)
        If Not paused OrElse countingDown Then Return
        SetUiMode(uiMode.PauseSettings)
        renderPanel.Invalidate()
    End Sub

    Private Sub BackButton_Click(sender As Object, e As EventArgs)
        If Not paused OrElse countingDown Then Return
        SetUiMode(uiMode.Pause)
        renderPanel.Invalidate()
    End Sub

    Private Sub ResumeButton_Click(sender As Object, e As EventArgs)
        BeginResumeCountdown()
    End Sub

    Private Sub BeginResumeCountdown()
        If Not paused OrElse countingDown Then Return
        If Not engine.CanPause Then Return
        countingDown = True
        countdownStep = 3
        renderPanel.Paused = False
        SetUiMode(uiMode.None)
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
        If gameplayMusicOn Then AudioManager.ResumeMusic()
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

    ' ===================== TRY AGAIN / MAIN MENU =====================

    Private Sub RetryButton_Click(sender As Object, e As EventArgs)
        StopCountdown()
        StopGameplayMusic()
        engine.NewGame()                         ' score 0, timer 0, lives 3, level 1
        ClearKeys()
        paused = False
        renderPanel.Paused = False
        SetUiMode(uiMode.None)
        StartGameplayMusic()
        currentFadeStage = FadeStage.FadingIn
        fadeValue = 1.0F
        accumulator = 0.0F
        clock.Restart()
        gameTimer.Start()
        renderPanel.Invalidate()
        Me.Focus()
    End Sub

    Private Sub MenuButton_Click(sender As Object, e As EventArgs)
        Me.Close()      ' the Cannon Ball menu re-appears from its own FormClosed handler
    End Sub

    ' ===================== Engine events -> sound / screens =====================

    Private Sub Engine_CartLocked()
        PlaySfx(SfxLock, GainLock)
    End Sub

    Private Sub Engine_Launched()
        PlaySfx(SfxLaunch, GainLaunch)
    End Sub

    Private Sub Engine_WallBounced()
        PlaySfx(SfxWall, GainWall)
    End Sub

    Private Sub Engine_CartBounced()
        PlaySfx(SfxCart, GainCart)
    End Sub

    Private Sub Engine_BlockDamaged(kind As CBBlockType)
        PlaySfx(SfxStoneHit, GainBlock)
    End Sub

    Private Sub Engine_BlockDestroyed(kind As CBBlockType)
        Select Case kind
            Case CBBlockType.Gold : PlaySfx(SfxGoldBreak, GainBig)
            Case CBBlockType.Emerald : PlaySfx(SfxEmerald, GainBig)
            Case Else : PlaySfx(SfxBrickBreak, GainBlock)
        End Select
    End Sub

    Private Sub Engine_LifeGained()
        ' the emerald sound already plays from BlockDestroyed
    End Sub

    Private Sub Engine_LifeLost()
        PlaySfx(SfxLifeLost, GainBig)
    End Sub

    Private Sub Engine_LevelCompleted(levelNumber As Integer)
        PlaySfx(SfxLevelDone, GainBig)
    End Sub

    ''' <summary>The "level complete" effect is over: start fading to black.</summary>
    Private Sub Engine_LevelClearFinished()
        currentFadeStage = FadeStage.FadingOut
        fadeValue = 0.0F
    End Sub

    Private Sub Engine_GameEnded()
        StopCountdown()
        StopGameplayMusic()
        PlaySfx(SfxGameOver, GainBig)
        SetUiMode(uiMode.GameOver)
    End Sub

    Private Sub Engine_Won()
        StopGameplayMusic()
        PlaySfx(SfxVictory, GainBig)
        ' the victory buttons appear when the fade-in has finished (see UpdateFade)
    End Sub

    ' Plays a sound, ignoring a repeat of the same sound within SfxMinGapMs.
    Private Sub PlaySfx(relativePath As String, gain As Double)
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

        RemoveHandler engine.CartLocked, AddressOf Engine_CartLocked
        RemoveHandler engine.Launched, AddressOf Engine_Launched
        RemoveHandler engine.WallBounced, AddressOf Engine_WallBounced
        RemoveHandler engine.CartBounced, AddressOf Engine_CartBounced
        RemoveHandler engine.BlockDamaged, AddressOf Engine_BlockDamaged
        RemoveHandler engine.BlockDestroyed, AddressOf Engine_BlockDestroyed
        RemoveHandler engine.LifeGained, AddressOf Engine_LifeGained
        RemoveHandler engine.LifeLost, AddressOf Engine_LifeLost
        RemoveHandler engine.LevelCompleted, AddressOf Engine_LevelCompleted
        RemoveHandler engine.LevelClearFinished, AddressOf Engine_LevelClearFinished
        RemoveHandler engine.GameEnded, AddressOf Engine_GameEnded
        RemoveHandler engine.Won, AddressOf Engine_Won

        AudioManager.StopMusic()         ' the Cannon Ball menu starts its own music when it re-appears
        renderPanel.Dispose()
        assets.Dispose()
    End Sub

End Class
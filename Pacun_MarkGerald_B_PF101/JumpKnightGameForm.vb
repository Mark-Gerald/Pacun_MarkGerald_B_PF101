Public Class JumpKnightGameForm
    Inherits Form

    ' ---- Sound hooks: leave "" for silence, or put a path RELATIVE to Assets ----
    Private Const SfxJump As String = ""
    Private Const SfxSpring As String = ""
    Private Const SfxMeat As String = ""
    Private Const SfxDoubleJump As String = ""
    Private Const SfxWoodBreak As String = ""
    Private Const SfxAttack As String = ""
    Private Const SfxBatHit As String = ""
    Private Const SfxGameOver As String = ""
    Private Const MusicTrack As String = ""

    Private Const StepSeconds As Single = 1.0F / 60.0F
    Private Const MaxFrameTime As Single = 0.1F
    Private Const CountdownStepMs As Integer = 700

    Private ReadOnly engine As JumpKnightEngine
    Private ReadOnly assets As JumpKnightAssets
    Private ReadOnly renderPanel As JumpKnightPanel
    Private ReadOnly gameTimer As System.Windows.Forms.Timer
    Private ReadOnly countdownTimer As System.Windows.Forms.Timer
    Private ReadOnly clock As New System.Diagnostics.Stopwatch()
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

    Private retryButton As PixelButton
    Private resumeButton As PixelButton
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
        menuButton = New PixelButton("MENU", Color.FromArgb(200, 70, 60))

        AddHandler retryButton.Click, AddressOf RetryButton_Click
        AddHandler resumeButton.Click, AddressOf ResumeButton_Click
        AddHandler menuButton.Click, AddressOf MenuButton_Click

        For Each b As PixelButton In New PixelButton() {retryButton, resumeButton, menuButton}
            b.Size = New Size(200, 50)
            b.Visible = False
            renderPanel.Controls.Add(b)
        Next
    End Sub

    Private Sub PositionButtons()
        If renderPanel Is Nothing Then Return
        Dim first As Point = renderPanel.LogicalToScreen(JumpKnightEngine.WorldW / 2.0F, 360)
        Dim second As Point = renderPanel.LogicalToScreen(JumpKnightEngine.WorldW / 2.0F, 440)
        retryButton.Location = New Point(first.X - retryButton.Width \ 2, first.Y)
        resumeButton.Location = New Point(first.X - resumeButton.Width \ 2, first.Y)
        menuButton.Location = New Point(second.X - menuButton.Width \ 2, second.Y)
    End Sub

    Private Sub Form_Shown(sender As Object, e As EventArgs)
        PositionButtons()
        If MusicTrack <> "" Then AudioManager.PlayMusic(MusicTrack, True)
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

    ' Arrow keys, Escape and P are handled here so no control can steal them.
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

    ' UP / W: double jump (only works with the meat buff). First press only.
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

    ' SPACE: sword attack. Holding the key triggers ONE attack; the engine adds duration + cooldown.
    Private Sub PressAttack()
        If attackHeld Then Return
        attackHeld = True
        If paused Then Return                       ' pause screen and countdown: no attacks
        engine.TryAttack()                          ' the engine ignores it unless the game is running
    End Sub

    Private Sub StartRunIfReady()
        If paused Then Return
        If engine.State = JKState.Ready Then engine.Begin()
    End Sub

    Private Sub ClearKeys()
        leftHeld = False
        rightHeld = False
        jumpHeld = False
        attackHeld = False
        pauseKeyHeld = False
    End Sub

    ' ===================== Pause / resume countdown (one implementation for all inputs) =====================

    Private Sub Form_Deactivate(sender As Object, e As EventArgs)
        ClearKeys()
        If engine.State = JKState.Playing AndAlso (Not paused OrElse countingDown) Then PauseGame()
    End Sub

    Private Sub RenderPanel_PauseClicked()
        PauseGame()
    End Sub

    ''' <summary>P / Esc: pause when playing, cancel a countdown, or start the resume countdown when paused.</summary>
    Private Sub TogglePause()
        If engine.State <> JKState.Playing Then Return
        If countingDown Then
            PauseGame()                 ' cancel the countdown, back to the pause screen
        ElseIf paused Then
            BeginResumeCountdown()
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
        renderPanel.Overlay = JKOverlay.Paused
        resumeButton.Visible = True
        menuButton.Visible = True
        retryButton.Visible = False
        PositionButtons()
        renderPanel.Invalidate()
    End Sub

    Private Sub ResumeButton_Click(sender As Object, e As EventArgs)
        BeginResumeCountdown()
    End Sub

    ''' <summary>Hides the pause screen and counts 3, 2, 1, GO! with the game still frozen.</summary>
    Private Sub BeginResumeCountdown()
        If Not paused OrElse countingDown Then Return      ' cannot start twice
        If engine.State <> JKState.Playing Then Return

        countingDown = True
        countdownStep = 3
        renderPanel.Overlay = JKOverlay.None
        resumeButton.Visible = False
        menuButton.Visible = False
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
            ResumeGameplay()                                ' the timer keeps running one more tick to clear "GO!"
        Else
            StopCountdown()
        End If
        renderPanel.Invalidate()
    End Sub

    Private Sub ResumeGameplay()
        countingDown = False
        paused = False
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
        engine.Reset()
        ClearKeys()
        paused = False
        renderPanel.Overlay = JKOverlay.None
        retryButton.Visible = False
        resumeButton.Visible = False
        menuButton.Visible = False
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
        PlaySfx(SfxGameOver)
        renderPanel.Overlay = JKOverlay.GameOver
        retryButton.Visible = True
        menuButton.Visible = True
        resumeButton.Visible = False
        PositionButtons()
        renderPanel.Invalidate()
    End Sub

    Private Sub Engine_Bounced(kind As JKPlatformKind)
        PlaySfx(SfxJump)
    End Sub

    Private Sub Engine_SpringUsed()
        PlaySfx(SfxSpring)
    End Sub

    Private Sub Engine_MeatCollected()
        PlaySfx(SfxMeat)
    End Sub

    Private Sub Engine_DoubleJumped()
        PlaySfx(SfxDoubleJump)
    End Sub

    Private Sub Engine_WoodBroke()
        PlaySfx(SfxWoodBreak)
    End Sub

    Private Sub Engine_Attacked()
        PlaySfx(SfxAttack)
    End Sub

    Private Sub Engine_BatHit()
        PlaySfx(SfxBatHit)
    End Sub

    Private Sub PlaySfx(relativePath As String)
        If relativePath <> "" Then AudioManager.PlaySfx(relativePath)
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

        If MusicTrack <> "" Then AudioManager.StopMusic()
        renderPanel.Dispose()
        assets.Dispose()
    End Sub

End Class
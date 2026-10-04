Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Drawing.Text
Imports System.Linq

Public Class Level1GameplayForm
    Inherits Form

    Private gamePanel As GameScenePanel

    ' ---- puzzle state (logic only; no drawing here) ----
    Private characters As New List(Of CharacterState)
    Private ReadOnly boatPassengers As New List(Of CharacterState)   ' includes people still walking to the boat
    Private boatBank As String = "Right"                             ' the bank the boat is docked at
    Private isSailing As Boolean = False
    Private gameOver As Boolean = False
    Private moveCount As Integer = 0
    Private sailId As Integer = 0                                    ' invalidates callbacks after Reset
    Private statusText As String = ""
    Private Const GameplayMusic As String = "Audio\Music\Cross_River_Gameplay_Music_Track.mp3"

    Public Sub New()
        Me.Text = "Level 1 " & ChrW(8212) & " River Crossing"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 620)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(120, 190, 110)

        gamePanel = New GameScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(gamePanel)

        ' All three connections between the scene and this form. If any of these is missing,
        ' the matching clicks will play a sound but do nothing.
        AddHandler gamePanel.SpriteClicked, AddressOf GamePanel_SpriteClicked
        AddHandler gamePanel.HudButtonClicked, AddressOf GamePanel_HudButtonClicked
        AddHandler gamePanel.EndButtonClicked, AddressOf GamePanel_EndButtonClicked
        AddHandler Me.FormClosed, AddressOf Level1GameplayForm_FormClosed

        ' The menu music must stop here even if the gameplay track file is missing.
        AudioManager.StopMusic()
        InitializeGame()
    End Sub

    ' ==================================================
    ' SETUP
    ' ==================================================
    Private Sub InitializeGame()
        sailId += 1

        characters = New List(Of CharacterState) From {
            New CharacterState("I1", "Innocent", "Right"),
            New CharacterState("M1", "Monster", "Right"),
            New CharacterState("I2", "Innocent", "Right"),
            New CharacterState("M2", "Monster", "Right"),
            New CharacterState("I3", "Innocent", "Right"),
            New CharacterState("M3", "Monster", "Right")
        }
        boatPassengers.Clear()
        boatBank = "Right"
        isSailing = False
        gameOver = False
        moveCount = 0

        gamePanel.ResetScene(characters)
        SetStatus("Click a character to put them on the boat.")

        ' Does nothing if the gameplay track is already playing (HUD Reset).
        ' After a win or loss the music was stopped, so TRY AGAIN starts it again from the beginning.
        AudioManager.PlayMusic(GameplayMusic, True)
    End Sub

    Private Sub SetStatus(message As String)
        statusText = message
        UpdateHud()
    End Sub

    Private Sub UpdateHud()
        gamePanel.HudMoves = moveCount
        gamePanel.HudStatus = statusText
        gamePanel.InputLocked = isSailing OrElse gameOver
        gamePanel.HudCrossEnabled = Not (isSailing OrElse gameOver)
    End Sub

    ' ==================================================
    ' BUTTONS (upper-right HUD, and the Try Again / Menu buttons on the end screen)
    ' ==================================================
    Private Sub GamePanel_HudButtonClicked(buttonId As String)
        Debug.WriteLine("Level1GameplayForm: HUD button '" & buttonId & "' clicked")
        Select Case buttonId
            Case "cross"
                TryCross()
            Case "reset"
                InitializeGame()
            Case "menu"
                Me.Close()
        End Select
    End Sub

    Private Sub GamePanel_EndButtonClicked(buttonId As String)
        Debug.WriteLine("Level1GameplayForm: end-screen button '" & buttonId & "' clicked")
        Select Case buttonId
            Case "retry"
                InitializeGame()
            Case "menu"
                Me.Close()
        End Select
    End Sub

    ' ==================================================
    ' CLICKING CHARACTERS
    ' ==================================================
    Private Sub GamePanel_SpriteClicked(sprite As CharacterSprite)
        If gameOver OrElse isSailing Then Return
        If sprite.Mode = SpriteMode.Walking Then Return        ' mid-walk: ignore, not a move

        Dim ch As CharacterState = sprite.Character

        ' Clicking someone already on the boat takes them off.
        If sprite.Mode = SpriteMode.OnBoat Then
            RemovePassenger(sprite)
            Return
        End If

        If ch.Side <> boatBank Then
            Nope("The boat isn't on that bank, so you can't use that character.")
            Return
        End If

        ' Occupancy counts everyone already heading to the boat, so quick clicking can't overfill it.
        If boatPassengers.Count >= 2 Then
            Nope("The boat is full (max 2). Take someone off first.")
            Return
        End If

        BoardPassenger(sprite)
    End Sub

    Private Sub BoardPassenger(sprite As CharacterSprite)
        Dim ch As CharacterState = sprite.Character

        Dim usedSlots As List(Of Integer) = boatPassengers.
            Select(Function(p) gamePanel.FindSprite(p)).
            Where(Function(sp) sp IsNot Nothing).
            Select(Function(sp) sp.BoatSlot).ToList()
        Dim slot As Integer = If(usedSlots.Contains(0), 1, 0)

        sprite.BoatSlot = slot
        boatPassengers.Add(ch)
        ch.OnBoat = True
        moveCount += 1

        gamePanel.WalkSpriteTo(sprite, Function() gamePanel.BoatSlotPoint(slot), SpriteMode.OnBoat, Nothing)
        AudioManager.PlaySfx("Audio\SFX\Walking_On_Wood_Sound_Effect.mp3")
        SetStatus(TypeLabel(ch) & " is boarding. Boat: " & boatPassengers.Count & "/2.")
    End Sub

    Private Sub RemovePassenger(sprite As CharacterSprite)
        Dim ch As CharacterState = sprite.Character
        Dim bank As String = boatBank

        boatPassengers.Remove(ch)
        ch.OnBoat = False
        moveCount += 1

        gamePanel.WalkSpriteTo(sprite, Function() gamePanel.BankSlotPoint(bank, sprite.SlotIndex), SpriteMode.AtBank, Nothing)
        AudioManager.PlaySfx("Audio\SFX\Walking_On_Wood_Sound_Effect.mp3")
        SetStatus(TypeLabel(ch) & " left the boat. Boat: " & boatPassengers.Count & "/2.")
    End Sub

    Private Shared Function TypeLabel(ch As CharacterState) As String
        Return If(ch.Type = "Monster", "Goblin", "Farmer")
    End Function

    ' The NOPE sound is reserved for impossible actions (these never count as moves).
    Private Sub Nope(message As String)
        AudioManager.PlaySfx("Audio\SFX\Nope_Invalid_Move.mp3")
        SetStatus(message)
    End Sub

    ' ==================================================
    ' RULES
    ' ==================================================
    ' Who counts as standing on a bank. While the boat is docked its passengers count with that
    ' bank; while it sails they belong to neither bank.
    Private Function CharactersCountedAt(bank As String) As List(Of CharacterState)
        Return characters.Where(Function(c) c.Side = bank AndAlso
                                    (Not c.OnBoat OrElse (Not isSailing AndAlso boatBank = bank))).ToList()
    End Function

    ' Returns the bank where goblins outnumber farmers (with at least one farmer present), or "".
    Private Function FindUnsafeBank() As String
        For Each bank As String In New String() {"Left", "Right"}
            Dim farmers As Integer = 0
            Dim goblins As Integer = 0
            For Each c In CharactersCountedAt(bank)
                If c.Type = "Innocent" Then
                    farmers += 1
                Else
                    goblins += 1
                End If
            Next
            If farmers > 0 AndAlso goblins > farmers Then Return bank
        Next
        Return ""
    End Function

    ' ==================================================
    ' SAILING
    ' ==================================================
    Private Sub TryCross()
        If gameOver OrElse isSailing Then Return

        If gamePanel.AnyWalking() Then
            SetStatus("Wait for everyone to finish moving.")
            Return
        End If

        If boatPassengers.Count = 0 Then
            Nope("The boat is empty! Put at least one character on it first.")
            Return
        End If

        StartSailing()
    End Sub

    Private Sub StartSailing()
        isSailing = True
        moveCount += 1
        sailId += 1
        Dim myId As Integer = sailId
        Dim dest As String = If(boatBank = "Right", "Left", "Right")

        AudioManager.PlaySfx("Audio\SFX\Canoe_Paddle_Sound_Effect.mp3")

        ' Departure check: the passengers are no longer on the bank they are leaving.
        Dim unsafeBank As String = FindUnsafeBank()
        If unsafeBank <> "" Then
            LoseGame(unsafeBank)
        Else
            SetStatus("Crossing the river...")
        End If

        gamePanel.SailBoatTo(dest, Sub() OnBoatArrived(myId, dest))
    End Sub

    Private Sub OnBoatArrived(id As Integer, dest As String)
        If id <> sailId Then Return

        isSailing = False
        boatBank = dest
        For Each p In boatPassengers
            p.Side = dest          ' passengers now count with the arrival bank
        Next

        If gameOver Then
            UpdateHud()
            Return
        End If

        Dim unsafeBank As String = FindUnsafeBank()
        If unsafeBank <> "" Then
            LoseGame(unsafeBank)
            Return
        End If

        If characters.All(Function(c) c.Side = "Left") Then
            WinGame()
            Return
        End If

        SetStatus("The boat reached the " & dest.ToLower() & " bank. Click passengers to unload them, or load more.")
    End Sub

    Private Sub LoseGame(bank As String)
        gameOver = True
        AudioManager.StopMusic()
        AudioManager.PlaySfx("Audio\SFX\Failure_in_the_game_Sound_effect.mp3")

        Dim here As List(Of CharacterState) = CharactersCountedAt(bank)
        gamePanel.StartAttack(here.Where(Function(c) c.Type = "Monster").ToList(),
                              here.Where(Function(c) c.Type = "Innocent").ToList())

        statusText = "The goblins outnumbered the farmers on the " & bank.ToLower() & " bank and attacked!"
        gamePanel.ShowEndScreen(False, "DEFEAT", "The goblins outnumbered the farmers!", 1000)
        UpdateHud()
    End Sub

    Private Sub WinGame()
        gameOver = True
        AudioManager.StopMusic()
        AudioManager.PlaySfx("Audio\SFX\Victory_Jingle.mp3")

        ' Everyone still on the boat steps onto the bank (free: not a counted move).
        For Each p In boatPassengers.ToList()
            Dim passenger As CharacterState = p
            Dim sprite As CharacterSprite = gamePanel.FindSprite(passenger)
            passenger.OnBoat = False
            If sprite IsNot Nothing Then
                gamePanel.WalkSpriteTo(sprite, Function() gamePanel.BankSlotPoint("Left", sprite.SlotIndex), SpriteMode.AtBank, Nothing)
            End If
        Next
        boatPassengers.Clear()

        statusText = "Victory! Everyone crossed in " & moveCount & " moves."
        gamePanel.ShowEndScreen(True, "VICTORY!", "Everyone crossed safely!", 700)
        UpdateHud()
    End Sub

    Private Sub Level1GameplayForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        sailId += 1
        gamePanel.StopAnimation()
        AudioManager.StopMusic()   ' the menu restores its own music right after this
    End Sub

End Class

''' <summary>One puzzle character. Logic only; no drawing or animation state here.</summary>
Public Class CharacterState
    Public Property Name As String
    Public Property Type As String
    Public Property Side As String
    Public Property OnBoat As Boolean

    Public Sub New(nameValue As String, typeValue As String, sideValue As String)
        Name = nameValue
        Type = typeValue
        Side = sideValue
        OnBoat = False
    End Sub
End Class

Public Enum SpriteMode
    AtBank
    Walking
    OnBoat
End Enum

''' <summary>The visual side of one character: where it is drawn, what it is doing, which way it faces.</summary>
Public Class CharacterSprite
    Public ReadOnly Character As CharacterState
    Public ReadOnly SlotIndex As Integer
    Public ReadOnly IsMonster As Boolean

    Public Mode As SpriteMode = SpriteMode.AtBank
    Public Pos As PointF                    ' feet position (bottom-centre)
    Public IsHovered As Boolean = False
    Public MoveFacing As FacingDirection = FacingDirection.FaceFront
    Public BoatSlot As Integer = 0
    Public IsAttacking As Boolean = False
    Public AttackFacing As FacingDirection = FacingDirection.FaceRight

    Friend WalkTarget As Func(Of PointF)
    Friend WalkOnArrive As Action
    Friend ModeAfterWalk As SpriteMode = SpriteMode.AtBank
    Friend AnimMs As Double
    Friend LastAction As SpriteAction = SpriteAction.Idle
    Friend Bounds As Rectangle              ' last drawn rectangle, used for mouse hit-testing

    Public Sub New(characterValue As CharacterState, slotIndexValue As Integer)
        Character = characterValue
        SlotIndex = slotIndexValue
        IsMonster = (characterValue.Type = "Monster")
        AnimMs = slotIndexValue * 137.0     ' stagger idle animations so they aren't in lock-step
    End Sub
End Class

''' <summary>
''' Draws the whole game scene on ONE surface: background, raft, then characters sorted by foot
''' position (so passengers are always painted over the raft), then the HUD. One timer drives
''' every animation. The HUD (moves, status, buttons) is drawn directly on the map: no bars.
''' </summary>

Public Class GameScenePanel
    Inherits Panel

    Public Event SpriteClicked(sprite As CharacterSprite)
    Public Event HudButtonClicked(buttonId As String)
    Public Event EndButtonClicked(buttonId As String)

    ' ---- tuning constants ----
    Private Const RaftScale As Integer = 2
    Private Const WalkSpeed As Double = 160.0        ' pixels per second
    Private Const BoatSpeed As Double = 150.0        ' pixels per second
    Private Const BobAmplitude As Double = 2.0
    Private Const DeckFootFraction As Single = 0.35F ' how far down the raft the feet stand (0 = top edge)
    Private Const RowSpacing As Integer = 90
    Private Const ColumnSpacing As Integer = 100
    Private Const BankEdgeMargin As Integer = 60
    Private Const DockMargin As Integer = 10

    ' ---- end-screen tuning ----
    Private Const DefeatFadeMs As Double = 1800.0      ' how long the screen takes to go black
    Private Const VictoryFadeMs As Double = 1200.0     ' how long the screen takes to go bright
    Private Const DefeatMaxDark As Double = 1.0        ' 1.0 = completely black (nothing shows through)
    Private Const VictoryMaxBright As Double = 1.0     ' 1.0 = completely bright (nothing shows through)
    Private Const EndButtonsAppearAt As Double = 0.6   ' fraction of the fade at which the buttons appear

    Private Enum EndKind
        None
        Defeat
        Victory
    End Enum

    Private ReadOnly _backdrop As New RiverBackdrop()
    Private ReadOnly _sprites As New List(Of CharacterSprite)
    Private _drawOrder As New List(Of CharacterSprite)
    Private _hoveredSprite As CharacterSprite = Nothing

    Private _timer As Timer
    Private ReadOnly _clock As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
    Private _lastTickMs As Long = 0
    Private _bobPhase As Double = 0

    Private _boatX As Single = 0
    Private _boatDockSide As String = "Right"
    Private _boatTargetSide As String = "Right"
    Private _boatMoving As Boolean = False
    Private _boatOnArrive As Action = Nothing

    Private ReadOnly _raftImage As Image
    Private ReadOnly _drawAttributes As New ImageAttributes()
    Private _handShown As Boolean = False

    ' ---- HUD ----
    Private ReadOnly _hudButtons As New List(Of PixelButtonDef)
    Private _hudHover As PixelButtonDef = Nothing
    Private _hudPressed As PixelButtonDef = Nothing
    Private ReadOnly _hudFont As New Font("Segoe UI", 14.0F, FontStyle.Bold)
    Private ReadOnly _hudSmallFont As New Font("Segoe UI", 10.5F, FontStyle.Bold)
    Private ReadOnly _hudButtonFont As New Font("Segoe UI", 10.0F, FontStyle.Bold)
    Private ReadOnly _leftFormat As New StringFormat()
    Private ReadOnly _centerFormat As New StringFormat() With {.Alignment = StringAlignment.Center}

    ' ---- end screen (defeat / victory) ----
    Private _endKind As EndKind = EndKind.None
    Private _endTitle As String = ""
    Private _endSub As String = ""
    Private _endStartMs As Long = 0
    Private ReadOnly _endButtons As New List(Of PixelButtonDef)
    Private _endHover As PixelButtonDef = Nothing
    Private _endPressed As PixelButtonDef = Nothing

    ' The end-screen fonts and layout scale with the window, so they fill a big screen and still fit a small one.
    Private _endScale As Double = 1.0
    Private _endTitleFont As Font = Nothing
    Private _endSubFont As Font = Nothing
    Private _endMovesFont As Font = Nothing
    Private _endButtonFont As Font = Nothing
    Private _endTitleRect As RectangleF
    Private _endSubRect As RectangleF
    Private _endMovesRect As RectangleF

    Public Property HudMoves As Integer = 0
    Public Property HudStatus As String = ""
    Public Property InputLocked As Boolean = False

    Public Property HudCrossEnabled As Boolean
        Get
            Return _hudButtons(0).Enabled
        End Get
        Set(value As Boolean)
            _hudButtons(0).Enabled = value
        End Set
    End Property

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        _drawAttributes.SetWrapMode(WrapMode.TileFlipXY)   ' avoids faint edge lines when scaling pixel art
        _raftImage = GameAssets.GetBoatRaft()

        _hudButtons.Add(New PixelButtonDef("cross", "CROSS RIVER", Color.FromArgb(76, 175, 80)))
        _hudButtons.Add(New PixelButtonDef("reset", "RESET", Color.FromArgb(66, 133, 200)))
        _hudButtons.Add(New PixelButtonDef("menu", "MENU", Color.FromArgb(150, 90, 190)))

        _backdrop.Layout(Me.ClientSize.Width, Me.ClientSize.Height)
        LayoutHud()

        _lastTickMs = _clock.ElapsedMilliseconds
        _timer = New Timer() With {.Interval = 30}
        AddHandler _timer.Tick, AddressOf OnAnimTick
        _timer.Start()
    End Sub

    ' ==================================================
    ' PUBLIC API (used by the form)
    ' ==================================================
    Public Sub ResetScene(characters As List(Of CharacterState))
        _sprites.Clear()
        _drawOrder = New List(Of CharacterSprite)
        _hoveredSprite = Nothing
        _boatDockSide = "Right"
        _boatTargetSide = "Right"
        _boatMoving = False
        _boatOnArrive = Nothing
        ClearEndScreen()
        _boatX = DockX(_boatDockSide)

        For i As Integer = 0 To characters.Count - 1
            Dim sprite As New CharacterSprite(characters(i), i)
            sprite.Pos = BankSlotPoint(characters(i).Side, i)
            _sprites.Add(sprite)
        Next
        Me.Invalidate()
    End Sub

    Public Function FindSprite(ch As CharacterState) As CharacterSprite
        Return _sprites.FirstOrDefault(Function(sp) sp.Character Is ch)
    End Function

    Public Function AnyWalking() As Boolean
        Return _sprites.Any(Function(sp) sp.Mode = SpriteMode.Walking)
    End Function

    ''' <summary>Walk animation to a target; the sprite faces its movement direction, then takes modeAfter.</summary>
    Public Sub WalkSpriteTo(sprite As CharacterSprite, target As Func(Of PointF), modeAfter As SpriteMode, onArrive As Action)
        sprite.WalkTarget = target
        sprite.ModeAfterWalk = modeAfter
        sprite.WalkOnArrive = onArrive
        sprite.Mode = SpriteMode.Walking

        Dim firstTarget As PointF = target.Invoke()
        If Math.Abs(firstTarget.X - sprite.Pos.X) > 1.0F Then
            sprite.MoveFacing = If(firstTarget.X < sprite.Pos.X, FacingDirection.FaceLeft, FacingDirection.FaceRight)
        End If
    End Sub

    Public Sub SailBoatTo(destSide As String, onArrive As Action)
        _boatTargetSide = destSide
        _boatMoving = True
        _boatOnArrive = onArrive
    End Sub

    ''' <summary>Plays the goblins' attack animation (facing the nearest farmer) until the scene is reset.</summary>
    Public Sub StartAttack(attackers As List(Of CharacterState), victims As List(Of CharacterState))
        Dim victimSprites As List(Of CharacterSprite) = victims.
            Select(Function(v) FindSprite(v)).Where(Function(sp) sp IsNot Nothing).ToList()

        For Each a In attackers
            Dim sprite As CharacterSprite = FindSprite(a)
            If sprite Is Nothing Then Continue For

            Dim face As FacingDirection = If(a.Side = "Right", FacingDirection.FaceLeft, FacingDirection.FaceRight)
            If victimSprites.Count > 0 Then
                Dim nearest As CharacterSprite = victimSprites.OrderBy(Function(v) Math.Abs(v.Pos.X - sprite.Pos.X)).First()
                If Math.Abs(nearest.Pos.X - sprite.Pos.X) > 4.0F Then
                    face = If(nearest.Pos.X < sprite.Pos.X, FacingDirection.FaceLeft, FacingDirection.FaceRight)
                End If
            End If
            sprite.AttackFacing = face
            sprite.IsAttacking = True
        Next
    End Sub

    ''' <summary>
    ''' Starts the defeat (fade to black) or victory (fade to bright) screen after delayMs. From this call on,
    ''' the HUD buttons and the characters are blocked; only the two end-screen buttons work.
    ''' </summary>
    Public Sub ShowEndScreen(isVictory As Boolean, title As String, subtitle As String, delayMs As Integer)
        _endKind = If(isVictory, EndKind.Victory, EndKind.Defeat)
        _endTitle = title
        _endSub = subtitle
        _endStartMs = _clock.ElapsedMilliseconds + delayMs

        _endButtons.Clear()
        _endButtons.Add(New PixelButtonDef("retry", "TRY AGAIN", Color.FromArgb(76, 175, 80)))
        _endButtons.Add(New PixelButtonDef("menu", If(isVictory, "GAME MENU", "MAIN MENU"), Color.FromArgb(66, 133, 200)))
        _endHover = Nothing
        _endPressed = Nothing
        LayoutEndScreen()
    End Sub

    Private Sub ClearEndScreen()
        _endKind = EndKind.None
        _endButtons.Clear()
        _endHover = Nothing
        _endPressed = Nothing
    End Sub

    Public Function BankSlotPoint(side As String, slotIndex As Integer) As PointF
        Dim col As Integer = slotIndex Mod 2
        Dim row As Integer = slotIndex \ 2
        Dim x As Single
        If side = "Left" Then
            x = _backdrop.LeftBank.Right - BankEdgeMargin - col * ColumnSpacing
        Else
            x = _backdrop.RightBank.Left + BankEdgeMargin + col * ColumnSpacing
        End If
        Return New PointF(x, GroundLineY() + (row - 1) * RowSpacing)
    End Function

    ''' <summary>Where a passenger's feet go on the raft deck. Follows the raft (and its bobbing) every frame.</summary>
    Public Function BoatSlotPoint(slot As Integer) As PointF
        Dim fraction As Single = If(slot = 0, 0.3F, 0.7F)
        Return New PointF(_boatX + RaftWidth * fraction, GroundLineY() + BobOffset())
    End Function

    Public Sub StopAnimation()
        If _timer IsNot Nothing Then _timer.Stop()
    End Sub

    ' ==================================================
    ' END-SCREEN STATE AND LAYOUT
    ' ==================================================
    Private ReadOnly Property IsEndActive As Boolean
        Get
            Return _endKind <> EndKind.None
        End Get
    End Property

    ' 0 until the delay has passed, then rises to 1 over the fade time.
    Private Function EndProgress() As Double
        If _endKind = EndKind.None Then Return 0.0
        Dim fadeMs As Double = If(_endKind = EndKind.Victory, VictoryFadeMs, DefeatFadeMs)
        Dim p As Double = (_clock.ElapsedMilliseconds - _endStartMs) / fadeMs
        Return Math.Max(0.0, Math.Min(1.0, p))
    End Function

    Private Function EndButtonsVisible() As Boolean
        Return _endKind <> EndKind.None AndAlso EndProgress() >= EndButtonsAppearAt
    End Function

    ' Re-creates the four end-screen fonts when the window size has changed enough to matter.
    Private Sub EnsureEndFonts(scale As Double)
        If _endTitleFont IsNot Nothing AndAlso Math.Abs(scale - _endScale) < 0.03 Then Return
        _endScale = scale

        If _endTitleFont IsNot Nothing Then _endTitleFont.Dispose()
        If _endSubFont IsNot Nothing Then _endSubFont.Dispose()
        If _endMovesFont IsNot Nothing Then _endMovesFont.Dispose()
        If _endButtonFont IsNot Nothing Then _endButtonFont.Dispose()

        _endTitleFont = New Font("Segoe UI", CSng(64.0 * scale), FontStyle.Bold)
        _endSubFont = New Font("Segoe UI", CSng(24.0 * scale), FontStyle.Bold)
        _endMovesFont = New Font("Segoe UI", CSng(28.0 * scale), FontStyle.Bold)
        _endButtonFont = New Font("Segoe UI", CSng(18.0 * scale), FontStyle.Bold)
    End Sub

    ' Lays the whole block out as one group (title, subtitle, moves, buttons) centred in the window.
    Private Sub LayoutEndScreen()
        Dim w As Integer = Me.ClientSize.Width
        Dim h As Integer = Me.ClientSize.Height
        If w <= 0 OrElse h <= 0 Then Return

        EnsureEndFonts(Math.Max(0.6, Math.Min(1.6, Math.Min(w / 1100.0, h / 760.0))))
        Dim s As Single = CSng(_endScale)

        Dim titleH As Single = 120.0F * s
        Dim subH As Single = 48.0F * s
        Dim movesH As Single = 56.0F * s
        Dim gapA As Single = 4.0F * s
        Dim gapB As Single = 6.0F * s
        Dim gapC As Single = 44.0F * s
        Dim btnW As Integer = CInt(300.0F * s)
        Dim btnH As Integer = CInt(84.0F * s)
        Dim btnGap As Integer = CInt(40.0F * s)

        Dim total As Single = titleH + gapA + subH + gapB + movesH + gapC + btnH
        Dim top As Single = Math.Max(8.0F, (h - total) / 2.0F)

        _endTitleRect = New RectangleF(0, top, w, titleH)
        top += titleH + gapA
        _endSubRect = New RectangleF(0, top, w, subH)
        top += subH + gapB
        _endMovesRect = New RectangleF(0, top, w, movesH)
        top += movesH + gapC

        If _endButtons.Count >= 2 Then
            Dim x As Integer = (w - (btnW * 2 + btnGap)) \ 2
            _endButtons(0).Rect = New Rectangle(x, CInt(top), btnW, btnH)
            _endButtons(1).Rect = New Rectangle(x + btnW + btnGap, CInt(top), btnW, btnH)
        End If
    End Sub

    ' ==================================================
    ' GEOMETRY
    ' ==================================================
    Private ReadOnly Property RaftWidth As Integer
        Get
            If _raftImage IsNot Nothing Then Return _raftImage.Width * RaftScale
            Return 168
        End Get
    End Property

    Private ReadOnly Property RaftHeight As Integer
        Get
            If _raftImage IsNot Nothing Then Return _raftImage.Height * RaftScale
            Return 74
        End Get
    End Property

    Private Function CenterY() As Single
        Return _backdrop.River.Top + _backdrop.River.Height / 2.0F
    End Function

    Private Function BobOffset() As Single
        Return CSng(Math.Sin(_bobPhase) * BobAmplitude)
    End Function

    Private Function GroundLineY() As Single
        Return CenterY() - RaftHeight / 2.0F + RaftHeight * DeckFootFraction
    End Function

    Private Function DockX(side As String) As Single
        If side = "Left" Then Return _backdrop.River.Left + DockMargin
        Return _backdrop.River.Right - RaftWidth - DockMargin
    End Function

    Private Sub LayoutHud()
        Dim bw As Integer = 150
        Dim bh As Integer = 36
        Dim gap As Integer = 8
        Dim margin As Integer = 14
        For i As Integer = 0 To _hudButtons.Count - 1
            _hudButtons(i).Rect = New Rectangle(Me.ClientSize.Width - bw - margin, margin + i * (bh + gap), bw, bh)
        Next
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        _backdrop.Layout(Me.ClientSize.Width, Me.ClientSize.Height)
        LayoutHud()
        If IsEndActive Then LayoutEndScreen()
        Me.Invalidate()
    End Sub

    ' ==================================================
    ' ANIMATION (one timer for everything)
    ' ==================================================
    Private Sub OnAnimTick(sender As Object, e As EventArgs)
        If Me.IsDisposed Then Return

        Dim nowMs As Long = _clock.ElapsedMilliseconds
        Dim dtMs As Double = Math.Min(100.0, CDbl(nowMs - _lastTickMs))
        _lastTickMs = nowMs
        Dim dt As Double = dtMs / 1000.0

        _backdrop.Advance(dt)
        _bobPhase += dt * 2.2

        UpdateBoat(dt)
        For Each s In _sprites.ToList()
            UpdateSprite(s, dt, dtMs)
        Next

        UpdateHover()
        Me.Invalidate()
    End Sub

    Private Sub UpdateBoat(dt As Double)
        If _boatMoving Then
            Dim targetX As Single = DockX(_boatTargetSide)
            Dim distance As Single = targetX - _boatX
            Dim moveAmount As Single = CSng(BoatSpeed * dt)

            If Math.Abs(distance) <= moveAmount Then
                _boatX = targetX
                _boatMoving = False
                _boatDockSide = _boatTargetSide
                Dim callback As Action = _boatOnArrive
                _boatOnArrive = Nothing
                If callback IsNot Nothing Then callback.Invoke()
            Else
                _boatX += Math.Sign(distance) * moveAmount
            End If
        Else
            _boatX = DockX(_boatDockSide)
        End If
    End Sub

    Private Sub UpdateSprite(s As CharacterSprite, dt As Double, dtMs As Double)
        Select Case s.Mode
            Case SpriteMode.AtBank
                s.Pos = BankSlotPoint(s.Character.Side, s.SlotIndex)
            Case SpriteMode.OnBoat
                s.Pos = BoatSlotPoint(s.BoatSlot)
            Case SpriteMode.Walking
                StepWalk(s, dt)
        End Select

        Dim currentAction As SpriteAction = GetAction(s)
        If currentAction <> s.LastAction Then
            s.LastAction = currentAction
            s.AnimMs = 0            ' a new action always starts from its first frame
        Else
            s.AnimMs += dtMs
        End If
    End Sub

    Private Sub StepWalk(s As CharacterSprite, dt As Double)
        Dim target As PointF = s.Pos
        If s.WalkTarget IsNot Nothing Then target = s.WalkTarget.Invoke()

        Dim dx As Single = target.X - s.Pos.X
        Dim dy As Single = target.Y - s.Pos.Y
        Dim distance As Single = CSng(Math.Sqrt(dx * dx + dy * dy))
        Dim moveAmount As Single = CSng(WalkSpeed * dt)

        If Math.Abs(dx) > 1.0F Then
            s.MoveFacing = If(dx < 0, FacingDirection.FaceLeft, FacingDirection.FaceRight)
        End If

        If distance <= moveAmount Then
            s.Pos = target
            s.Mode = s.ModeAfterWalk
            Dim callback As Action = s.WalkOnArrive
            s.WalkOnArrive = Nothing
            s.WalkTarget = Nothing
            If callback IsNot Nothing Then callback.Invoke()
        Else
            s.Pos = New PointF(s.Pos.X + dx / distance * moveAmount, s.Pos.Y + dy / distance * moveAmount)
        End If
    End Sub

    Private Function GetAction(s As CharacterSprite) As SpriteAction
        If s.IsAttacking Then Return SpriteAction.Attack
        If s.Mode = SpriteMode.Walking Then Return SpriteAction.Walk
        Return SpriteAction.Idle
    End Function

    ' The direction the boat is travelling now, or will travel next while docked.
    Private Function BoatTravelFacing() As FacingDirection
        If _boatMoving Then
            Return If(_boatTargetSide = "Left", FacingDirection.FaceLeft, FacingDirection.FaceRight)
        End If
        Return If(_boatDockSide = "Right", FacingDirection.FaceLeft, FacingDirection.FaceRight)
    End Function

    ' Movement and boat travel always win over mouse hover.
    Private Function GetFacing(s As CharacterSprite) As FacingDirection
        If s.IsAttacking Then Return s.AttackFacing
        Select Case s.Mode
            Case SpriteMode.Walking
                Return s.MoveFacing
            Case SpriteMode.OnBoat
                Return BoatTravelFacing()
            Case Else
                ' Hovered goblins turn toward the river: left on the right bank, right on the left bank.
                If s.IsMonster AndAlso s.IsHovered Then
                    Return If(s.Character.Side = "Right", FacingDirection.FaceLeft, FacingDirection.FaceRight)
                End If
                Return FacingDirection.FaceFront
        End Select
    End Function

    ' ==================================================
    ' MOUSE
    ' ==================================================
    Private Function FindHudButtonAt(p As Point) As PixelButtonDef
        Return _hudButtons.FirstOrDefault(Function(b) b.Rect.Contains(p))
    End Function

    Private Function FindEndButtonAt(p As Point) As PixelButtonDef
        Return _endButtons.FirstOrDefault(Function(b) b.Rect.Contains(p))
    End Function

    Private Function FindSpriteAt(p As Point) As CharacterSprite
        Dim list As List(Of CharacterSprite) = If(_drawOrder.Count > 0, _drawOrder, _sprites)
        For i As Integer = list.Count - 1 To 0 Step -1
            If list(i).Bounds.Contains(p) Then Return list(i)
        Next
        Return Nothing
    End Function

    Private Sub SetHandCursor(show As Boolean)
        If show <> _handShown Then
            _handShown = show
            Me.Cursor = If(show, Cursors.Hand, Cursors.Default)
        End If
    End Sub

    ' Hover is re-read from the real cursor position every tick, so a sprite that walks away from
    ' (or arrives under) a stationary cursor updates correctly.
    Private Sub UpdateHover()
        Dim clientPoint As Point = Me.PointToClient(Control.MousePosition)
        Dim inside As Boolean = Me.ClientRectangle.Contains(clientPoint)

        ' While an end screen is active, only its two buttons react.
        If IsEndActive Then
            _hudHover = Nothing
            If _hoveredSprite IsNot Nothing Then
                _hoveredSprite.IsHovered = False
                _hoveredSprite = Nothing
            End If
            _endHover = Nothing
            If inside AndAlso EndButtonsVisible() Then _endHover = FindEndButtonAt(clientPoint)
            SetHandCursor(_endHover IsNot Nothing)
            Return
        End If

        Dim hudHit As PixelButtonDef = Nothing
        Dim spriteHit As CharacterSprite = Nothing
        If inside Then
            hudHit = FindHudButtonAt(clientPoint)
            If hudHit Is Nothing Then spriteHit = FindSpriteAt(clientPoint)
        End If

        _hudHover = hudHit

        If spriteHit IsNot _hoveredSprite Then
            If _hoveredSprite IsNot Nothing Then _hoveredSprite.IsHovered = False
            _hoveredSprite = spriteHit
            If _hoveredSprite IsNot Nothing Then _hoveredSprite.IsHovered = True
        End If

        Dim showHand As Boolean = (hudHit IsNot Nothing AndAlso hudHit.Enabled) OrElse
                                  (spriteHit IsNot Nothing AndAlso Not InputLocked AndAlso spriteHit.Mode <> SpriteMode.Walking)
        SetHandCursor(showHand)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        UpdateHover()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hudHover = Nothing
        _hudPressed = Nothing
        _endHover = Nothing
        _endPressed = Nothing
        If _hoveredSprite IsNot Nothing Then
            _hoveredSprite.IsHovered = False
            _hoveredSprite = Nothing
        End If
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If e.Button <> MouseButtons.Left Then Return

        If IsEndActive Then
            If EndButtonsVisible() Then _endPressed = FindEndButtonAt(e.Location)
            Return
        End If

        Dim hit As PixelButtonDef = FindHudButtonAt(e.Location)
        If hit IsNot Nothing AndAlso hit.Enabled Then _hudPressed = hit
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        _hudPressed = Nothing
        _endPressed = Nothing
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        MyBase.OnMouseClick(e)
        If e.Button <> MouseButtons.Left Then Return

        ' End screen: every other click is swallowed (HUD buttons and characters are blocked).
        If IsEndActive Then
            If EndButtonsVisible() Then
                Dim endHit As PixelButtonDef = FindEndButtonAt(e.Location)
                If endHit IsNot Nothing Then
                    AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
                    RaiseEvent EndButtonClicked(endHit.Id)
                End If
            End If
            Return
        End If

        Dim hudHit As PixelButtonDef = FindHudButtonAt(e.Location)
        If hudHit IsNot Nothing Then
            If hudHit.Enabled Then
                AudioManager.PlaySfx("Audio\SFX\Button_Plate_Click.mp3")
                RaiseEvent HudButtonClicked(hudHit.Id)
            End If
            Return
        End If

        Dim spriteHit As CharacterSprite = FindSpriteAt(e.Location)
        If spriteHit IsNot Nothing Then RaiseEvent SpriteClicked(spriteHit)
    End Sub

    ' ==================================================
    ' DRAWING
    ' ==================================================
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        If Me.ClientSize.Width <= 0 OrElse Me.ClientSize.Height <= 0 Then Return

        g.SmoothingMode = SmoothingMode.None
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = PixelOffsetMode.Half
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

        _backdrop.Draw(g)
        DrawBoat(g)

        ' Painter's algorithm: raft first, then characters from back (small Y) to front.
        ' Passengers are therefore always painted over the raft.
        _drawOrder = _sprites.OrderBy(Function(sp) sp.Pos.Y).ThenBy(Function(sp) sp.Pos.X).ToList()
        For Each s In _drawOrder
            DrawSprite(g, s)
        Next

        DrawHud(g)
        DrawEndScreen(g)     ' on top of everything: covers the whole scene, then title + buttons
        MyBase.OnPaint(e)
    End Sub

    Private Sub DrawBoat(g As Graphics)
        Dim raftTop As Single = CenterY() - RaftHeight / 2.0F + BobOffset()
        Dim dest As New Rectangle(CInt(Math.Round(_boatX)), CInt(Math.Round(raftTop)), RaftWidth, RaftHeight)

        If _raftImage IsNot Nothing Then
            g.DrawImage(_raftImage, dest, 0, 0, _raftImage.Width, _raftImage.Height, GraphicsUnit.Pixel, _drawAttributes)
        Else
            Using brownBrush As New SolidBrush(Color.FromArgb(120, 80, 45))
                g.FillRectangle(brownBrush, dest)
            End Using
        End If
    End Sub

    Private Sub DrawSprite(g As Graphics, s As CharacterSprite)
        Dim currentAction As SpriteAction = GetAction(s)
        Dim facing As FacingDirection = GetFacing(s)

        Dim frames As Image() = SpriteLibrary.GetFrames(s.IsMonster, currentAction, facing)
        If frames Is Nothing OrElse frames.Length = 0 Then Return

        Dim frameMs As Integer = SpriteLibrary.GetFrameMs(s.IsMonster, currentAction)
        Dim frameIndex As Integer = CInt(Math.Floor(s.AnimMs / frameMs)) Mod frames.Length
        Dim img As Image = frames(frameIndex)

        Dim pixelScale As Integer = SpriteLibrary.GetPixelScale(s.IsMonster)
        Dim w As Integer = img.Width * pixelScale
        Dim h As Integer = img.Height * pixelScale
        Dim dest As New Rectangle(CInt(Math.Round(s.Pos.X - w / 2.0F)), CInt(Math.Round(s.Pos.Y - h)), w, h)
        s.Bounds = Rectangle.Inflate(dest, 6, 4)

        Using shadowBrush As New SolidBrush(Color.FromArgb(70, 0, 0, 0))
            g.FillEllipse(shadowBrush, CInt(s.Pos.X - w * 0.35F), CInt(s.Pos.Y - 5), CInt(w * 0.7F), 10)
        End Using

        g.DrawImage(img, dest, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, _drawAttributes)
    End Sub

    Private Sub DrawHud(g As Graphics)
        Dim outline As Color = Color.FromArgb(20, 20, 30)

        DrawOutlinedText(g, "Moves: " & HudMoves, _hudFont, Color.White, outline, New RectangleF(16, 10, 300, 28), _leftFormat, 2)

        Dim statusWidth As Single = Math.Max(160, Math.Min(460, _hudButtons(0).Rect.X - 40))
        DrawOutlinedText(g, HudStatus, _hudSmallFont, Color.FromArgb(235, 245, 255), outline,
                         New RectangleF(16, 42, statusWidth, 80), _leftFormat, 1)

        For Each btn In _hudButtons
            DrawPixelButton(g, btn, _hudButtonFont, btn Is _hudHover, btn Is _hudPressed)
        Next
    End Sub

    ' Defeat: the whole scene fades to solid black. Victory: it fades to a solid bright cream with a
    ' warm glow behind the text. Both end fully opaque, so nothing of the game shows through.
    ' The title, subtitle, moves and the two buttons are drawn on top, centred as one block.
    Private Sub DrawEndScreen(g As Graphics)
        If _endKind = EndKind.None Then Return
        Dim p As Double = EndProgress()
        If p <= 0.0 Then Return
        If _endTitleFont Is Nothing Then LayoutEndScreen()
        If _endTitleFont Is Nothing Then Return

        Dim w As Integer = Me.ClientSize.Width
        Dim h As Integer = Me.ClientSize.Height
        Dim isVictory As Boolean = (_endKind = EndKind.Victory)
        Dim s As Single = CSng(_endScale)

        If isVictory Then
            Dim washAlpha As Integer = Math.Min(255, CInt(255.0 * VictoryMaxBright * p))
            Using washBrush As New SolidBrush(Color.FromArgb(washAlpha, 255, 250, 228))
                g.FillRectangle(washBrush, 0, 0, w, h)
            End Using

            Dim blockTop As Single = _endTitleRect.Top
            Dim blockBottom As Single = _endMovesRect.Bottom
            Dim glowW As Single = Math.Min(w * 0.95F, 1100.0F * s)
            Dim glowH As Single = (blockBottom - blockTop) + 220.0F * s
            Dim glowCenterY As Single = (blockTop + blockBottom) / 2.0F
            Using glowPath As New GraphicsPath()
                glowPath.AddEllipse(New RectangleF(w / 2.0F - glowW / 2.0F, glowCenterY - glowH / 2.0F, glowW, glowH))
                Using glowBrush As New PathGradientBrush(glowPath)
                    glowBrush.CenterColor = Color.FromArgb(CInt(210.0 * p), 255, 226, 120)
                    glowBrush.SurroundColors = New Color() {Color.FromArgb(0, 255, 226, 120)}
                    g.FillPath(glowBrush, glowPath)
                End Using
            End Using
        Else
            Dim darkAlpha As Integer = Math.Min(255, CInt(255.0 * DefeatMaxDark * p))
            Using darkBrush As New SolidBrush(Color.FromArgb(darkAlpha, 0, 0, 0))
                g.FillRectangle(darkBrush, 0, 0, w, h)
            End Using
        End If

        ' Text colours differ: white text would vanish on the bright victory background.
        Dim textAlpha As Integer = CInt(255.0 * Math.Min(1.0, p * 2.5))
        Dim titleFill As Color
        Dim titleOutline As Color
        Dim subFill As Color
        Dim subOutline As Color
        Dim movesFill As Color
        Dim movesOutline As Color
        If isVictory Then
            titleFill = Color.FromArgb(textAlpha, 255, 196, 40)
            titleOutline = Color.FromArgb(textAlpha, 105, 62, 8)
            subFill = Color.FromArgb(textAlpha, 28, 92, 56)
            subOutline = Color.FromArgb(textAlpha, 255, 250, 228)
            movesFill = Color.FromArgb(textAlpha, 35, 105, 165)
            movesOutline = Color.FromArgb(textAlpha, 255, 250, 228)
        Else
            titleFill = Color.FromArgb(textAlpha, 235, 70, 60)
            titleOutline = Color.FromArgb(textAlpha, 70, 8, 8)
            subFill = Color.FromArgb(textAlpha, 255, 255, 255)
            subOutline = Color.FromArgb(textAlpha, 0, 0, 0)
            movesFill = Color.FromArgb(textAlpha, 255, 226, 130)
            movesOutline = Color.FromArgb(textAlpha, 0, 0, 0)
        End If

        DrawOutlinedText(g, _endTitle, _endTitleFont, titleFill, titleOutline, _endTitleRect, _centerFormat, Math.Max(2.0F, 4.0F * s))
        DrawOutlinedText(g, _endSub, _endSubFont, subFill, subOutline, _endSubRect, _centerFormat, Math.Max(1.0F, 2.0F * s))
        DrawOutlinedText(g, "Moves: " & HudMoves, _endMovesFont, movesFill, movesOutline, _endMovesRect, _centerFormat, Math.Max(1.0F, 2.0F * s))

        If EndButtonsVisible() Then
            For Each btn In _endButtons
                DrawPixelButton(g, btn, _endButtonFont, btn Is _endHover, btn Is _endPressed)
            Next
        End If
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If _timer IsNot Nothing Then
                _timer.Stop()
                _timer.Dispose()
                _timer = Nothing
            End If
            _backdrop.Dispose()
            _drawAttributes.Dispose()
            _hudFont.Dispose()
            _hudSmallFont.Dispose()
            _hudButtonFont.Dispose()
            If _endTitleFont IsNot Nothing Then _endTitleFont.Dispose()
            If _endSubFont IsNot Nothing Then _endSubFont.Dispose()
            If _endMovesFont IsNot Nothing Then _endMovesFont.Dispose()
            If _endButtonFont IsNot Nothing Then _endButtonFont.Dispose()
            _leftFormat.Dispose()
            _centerFormat.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
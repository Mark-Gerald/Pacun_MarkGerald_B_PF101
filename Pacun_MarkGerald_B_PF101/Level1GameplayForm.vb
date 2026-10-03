Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Linq

Public Class Level1GameplayForm
    Inherits Form

    Private gamePanel As GameScenePanel
    Private instructionLabel As Label
    Private statusLabel As Label
    Private moveCountLabel As Label
    Private crossButton As Button
    Private resetButton As Button
    Private backButton As Button

    ' ---- puzzle state (logic only; no drawing here) ----
    Private characters As New List(Of CharacterState)
    Private ReadOnly selectedCharacters As New List(Of CharacterState)
    Private currentBank As String = "Right"
    Private moveCount As Integer = 0
    Private gameWon As Boolean = False
    Private isAnimating As Boolean = False

    ' ---- crossing sequence bookkeeping ----
    ' crossingId changes on every new crossing/reset so callbacks from an abandoned
    ' crossing can never touch the new state.
    Private crossingId As Integer = 0
    Private crossingPassengers As New List(Of CharacterState)
    Private crossingDest As String = ""
    Private boardedCount As Integer = 0
    Private landedCount As Integer = 0

    Public Sub New()
        Me.Text = "Level 1 " & ChrW(8212) & " River Crossing"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 620)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(240, 240, 240)

        BuildLayout()

        gamePanel.IsSelectable = Function(sp As CharacterSprite) (Not isAnimating) AndAlso (Not gameWon) AndAlso sp.Mode = SpriteMode.AtBank AndAlso sp.Character.Side = currentBank
        AddHandler gamePanel.SpriteClicked, AddressOf GamePanel_SpriteClicked
        AddHandler Me.FormClosed, AddressOf Level1GameplayForm_FormClosed

        InitializeGame()
    End Sub

    ' ==================================================
    ' LAYOUT
    ' ==================================================
    Private Sub BuildLayout()
        Dim topPanel As New Panel() With {.Dock = DockStyle.Top, .Height = 64, .BackColor = Color.FromArgb(24, 24, 30)}
        Me.Controls.Add(topPanel)

        instructionLabel = New Label() With {
            .Text = "Select up to 2 passengers from the boat's bank, then click Cross River.",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(16, 8)
        }
        topPanel.Controls.Add(instructionLabel)

        statusLabel = New Label() With {
            .Text = "Select passengers to begin.",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = Color.FromArgb(200, 220, 255),
            .AutoSize = True,
            .Location = New Point(16, 34)
        }
        topPanel.Controls.Add(statusLabel)

        gamePanel = New GameScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(gamePanel)

        Dim bottomPanel As New Panel() With {.Dock = DockStyle.Bottom, .Height = 84, .BackColor = Color.FromArgb(245, 245, 245)}
        Me.Controls.Add(bottomPanel)

        moveCountLabel = New Label() With {
            .Text = "Moves: 0",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 40, 40),
            .AutoSize = True,
            .Location = New Point(16, 10)
        }
        bottomPanel.Controls.Add(moveCountLabel)

        crossButton = New Button() With {
            .Text = "Cross River",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Size = New Size(140, 36),
            .Location = New Point(16, 40)
        }
        crossButton.FlatAppearance.BorderSize = 0
        AddHandler crossButton.Click, AddressOf CrossButton_Click
        bottomPanel.Controls.Add(crossButton)

        resetButton = New Button() With {
            .Text = "Reset",
            .Font = New Font("Segoe UI", 9.5F),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Size = New Size(100, 36),
            .Location = New Point(166, 40)
        }
        resetButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)
        resetButton.FlatAppearance.BorderSize = 1
        AddHandler resetButton.Click, AddressOf ResetButton_Click
        bottomPanel.Controls.Add(resetButton)

        backButton = New Button() With {
            .Text = ChrW(8592) & " Back to Menu",
            .Font = New Font("Segoe UI", 9.5F),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Size = New Size(150, 36)
        }
        backButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)
        backButton.FlatAppearance.BorderSize = 1
        AddHandler backButton.Click, AddressOf BackButton_Click
        bottomPanel.Controls.Add(backButton)

        Dim positionBackButton As Action = Sub() backButton.Location = New Point(bottomPanel.Width - backButton.Width - 16, 40)
        AddHandler bottomPanel.Resize, Sub(s, e) positionBackButton()
        positionBackButton()
    End Sub

    ' ==================================================
    ' GAME STATE
    ' ==================================================
    Private Sub InitializeGame()
        crossingId += 1   ' invalidates callbacks from any crossing in progress

        ' Order matters only for the starting layout: each row holds one innocent + one monster.
        characters = New List(Of CharacterState) From {
            New CharacterState("I1", "Innocent", "Right"),
            New CharacterState("M1", "Monster", "Right"),
            New CharacterState("I2", "Innocent", "Right"),
            New CharacterState("M2", "Monster", "Right"),
            New CharacterState("I3", "Innocent", "Right"),
            New CharacterState("M3", "Monster", "Right")
        }
        selectedCharacters.Clear()
        crossingPassengers = New List(Of CharacterState)
        currentBank = "Right"
        moveCount = 0
        gameWon = False
        isAnimating = False
        boardedCount = 0
        landedCount = 0

        gamePanel.ResetScene(characters)

        crossButton.Enabled = True
        UpdateMoveCount()
        SetStatus("Select passengers to begin.")
    End Sub

    ' ==================================================
    ' SELECTION
    ' ==================================================
    Private Sub GamePanel_SpriteClicked(sprite As CharacterSprite)
        If isAnimating OrElse gameWon Then Return
        If sprite.Mode <> SpriteMode.AtBank Then Return

        Dim ch As CharacterState = sprite.Character

        If ch.Side <> currentBank Then
            SetStatus("That character is on the other bank. Select from the boat's bank.")
            Return
        End If

        If selectedCharacters.Contains(ch) Then
            selectedCharacters.Remove(ch)
            SetStatus("Passenger deselected.")
        Else
            If selectedCharacters.Count >= 2 Then
                AudioManager.PlaySfx("Audio\SFX\Nope_Invalid_Move.mp3")
                SetStatus("The boat can only carry up to 2 passengers.")
                Return
            End If
            selectedCharacters.Add(ch)
            SetStatus("Passenger selected. Select up to 2, then click Cross River.")
        End If

        sprite.IsSelected = selectedCharacters.Contains(ch)
    End Sub

    ' ==================================================
    ' RULES (classic puzzle logic, independent of drawing)
    ' ==================================================
    Private Function IsMoveLegal(passengers As List(Of CharacterState), destBank As String) As Boolean
        If passengers.Count < 1 OrElse passengers.Count > 2 Then Return False

        For Each p In passengers
            If p.Side <> currentBank Then Return False
        Next

        Dim leftInnocents As Integer = 0
        Dim leftMonsters As Integer = 0
        Dim rightInnocents As Integer = 0
        Dim rightMonsters As Integer = 0

        ' Count everyone where they would stand AFTER the move: this checks both the
        ' bank the boat leaves and the bank it arrives at.
        For Each ch In characters
            Dim finalSide As String = ch.Side
            If passengers.Contains(ch) Then finalSide = destBank

            If finalSide = "Left" Then
                If ch.Type = "Innocent" Then leftInnocents += 1 Else leftMonsters += 1
            Else
                If ch.Type = "Innocent" Then rightInnocents += 1 Else rightMonsters += 1
            End If
        Next

        Return IsBankSafe(leftInnocents, leftMonsters) AndAlso IsBankSafe(rightInnocents, rightMonsters)
    End Function

    Private Shared Function IsBankSafe(innocents As Integer, monsters As Integer) As Boolean
        If innocents = 0 Then Return True
        Return monsters <= innocents
    End Function

    Private Function AllOnLeftBank() As Boolean
        Return characters.All(Function(c) c.Side = "Left")
    End Function

    ' ==================================================
    ' CROSSING SEQUENCE: board (walk) -> sail -> disembark (walk) -> commit
    ' ==================================================
    Private Sub CrossButton_Click(sender As Object, e As EventArgs)
        If isAnimating OrElse gameWon Then Return

        If selectedCharacters.Count = 0 Then
            SetStatus("Select at least one passenger before crossing.")
            Return
        End If

        Dim dest As String = If(currentBank = "Right", "Left", "Right")

        If Not IsMoveLegal(selectedCharacters, dest) Then
            AudioManager.PlaySfx("Audio\SFX\Nope_Invalid_Move.mp3")
            SetStatus("Invalid move " & ChrW(8212) & " monsters would outnumber innocents on a bank.")
            Return
        End If

        StartCrossing(dest)
    End Sub

    Private Sub StartCrossing(dest As String)
        isAnimating = True
        crossButton.Enabled = False

        crossingId += 1
        Dim myId As Integer = crossingId

        crossingPassengers = New List(Of CharacterState)(selectedCharacters)
        crossingDest = dest
        boardedCount = 0
        landedCount = 0

        Dim passengerCount As Integer = crossingPassengers.Count
        SetStatus("Boarding...")
        AudioManager.PlaySfx("Audio\SFX\Walking_On_Wood_Sound_Effect.mp3")

        For i As Integer = 0 To passengerCount - 1
            Dim slot As Integer = i
            Dim passenger As CharacterState = crossingPassengers(i)
            Dim sprite As CharacterSprite = gamePanel.FindSprite(passenger)
            If sprite Is Nothing Then Continue For

            sprite.BoatSlot = slot
            sprite.BoatSlotCount = passengerCount
            sprite.IsSelected = False
            passenger.OnBoat = True

            gamePanel.WalkSpriteTo(sprite,
                                   Function() gamePanel.BoatSlotPoint(slot, passengerCount),
                                   SpriteMode.OnBoat,
                                   Sub() OnPassengerBoarded(myId))
        Next
    End Sub

    Private Sub OnPassengerBoarded(id As Integer)
        If id <> crossingId Then Return

        boardedCount += 1
        If boardedCount < crossingPassengers.Count Then Return

        SetStatus("Crossing the river...")
        AudioManager.PlaySfx("Audio\SFX\Canoe_Paddle_Sound_Effect.mp3")

        Dim myId As Integer = id
        gamePanel.SailBoatTo(crossingDest, Sub() OnBoatArrived(myId))
    End Sub

    Private Sub OnBoatArrived(id As Integer)
        If id <> crossingId Then Return

        SetStatus("Disembarking...")

        Dim dest As String = crossingDest
        For Each ch In crossingPassengers
            Dim passenger As CharacterState = ch
            Dim sprite As CharacterSprite = gamePanel.FindSprite(passenger)
            If sprite Is Nothing Then Continue For

            gamePanel.WalkSpriteTo(sprite,
                                   Function() gamePanel.BankSlotPoint(dest, sprite.SlotIndex),
                                   SpriteMode.AtBank,
                                   Sub() OnPassengerLanded(id, passenger))
        Next
    End Sub

    Private Sub OnPassengerLanded(id As Integer, passenger As CharacterState)
        If id <> crossingId Then Return

        passenger.Side = crossingDest
        passenger.OnBoat = False

        landedCount += 1
        If landedCount < crossingPassengers.Count Then Return

        CompleteCrossing()
    End Sub

    Private Sub CompleteCrossing()
        currentBank = crossingDest
        moveCount += 1
        selectedCharacters.Clear()
        UpdateMoveCount()
        isAnimating = False
        crossButton.Enabled = True

        If AllOnLeftBank() Then
            gameWon = True
            crossButton.Enabled = False
            AudioManager.PlaySfx("Audio\SFX\Victory_Jingle.mp3")
            SetStatus("Victory! All characters reached the other side in " & moveCount & " move(s).")
            ' Show the dialog after the animation tick finishes (not inside it).
            Me.BeginInvoke(New MethodInvoker(AddressOf ShowVictoryDialog))
        Else
            SetStatus("Crossing complete. Select the next passengers.")
        End If
    End Sub

    Private Sub ShowVictoryDialog()
        MessageBox.Show("All characters crossed safely!" & vbCrLf & "Moves used: " & moveCount,
                        "Victory", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' ==================================================
    ' BUTTONS / NAVIGATION
    ' ==================================================
    Private Sub ResetButton_Click(sender As Object, e As EventArgs)
        InitializeGame()
    End Sub

    Private Sub BackButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub Level1GameplayForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        crossingId += 1
        gamePanel.StopAnimation()
    End Sub

    Private Sub SetStatus(message As String)
        statusLabel.Text = message
    End Sub

    Private Sub UpdateMoveCount()
        moveCountLabel.Text = "Moves: " & moveCount
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

''' <summary>
''' The visual side of one character: where it is drawn, what it is doing, and which way it faces.
''' The puzzle rules never read this; the scene never changes CharacterState.
''' </summary>
Public Class CharacterSprite
    Public ReadOnly Character As CharacterState
    Public ReadOnly SlotIndex As Integer
    Public ReadOnly IsMonster As Boolean

    Public Mode As SpriteMode = SpriteMode.AtBank
    Public Pos As PointF                    ' feet position (bottom-centre) in scene coordinates
    Public IsHovered As Boolean = False
    Public IsSelected As Boolean = False
    Public MoveFacing As FacingDirection = FacingDirection.FaceFront
    Public BoatSlot As Integer = 0
    Public BoatSlotCount As Integer = 1

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
        AnimMs = slotIndexValue * 137.0   ' stagger idle animations so they are not in lock-step
    End Sub
End Class

''' <summary>
''' Draws the whole scene on ONE surface (background, water, raft, then characters sorted
''' by foot position). Because passengers are painted after the raft, they are always on top
''' of it, and their position is derived from the raft's position every frame.
''' One timer drives every animation.
''' </summary>
Public Class GameScenePanel
    Inherits Panel

    Public Event SpriteClicked(sprite As CharacterSprite)

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

    Private ReadOnly _sprites As New List(Of CharacterSprite)
    Private _drawOrder As New List(Of CharacterSprite)
    Private _hoveredSprite As CharacterSprite = Nothing

    Private _animTimer As Timer
    Private _clock As System.Diagnostics.Stopwatch
    Private _lastTickMs As Long = 0
    Private _waveOffset As Double = 0
    Private _bobPhase As Double = 0

    Private _boatX As Single = 0
    Private _boatDockSide As String = "Right"
    Private _boatTargetSide As String = "Right"
    Private _boatMoving As Boolean = False
    Private _boatOnArrive As Action = Nothing
    Private _boatFacing As FacingDirection = FacingDirection.FaceLeft

    Private _leftBank As Rectangle
    Private _river As Rectangle
    Private _rightBank As Rectangle

    Private _skyTile As Image
    Private _bankTile As Image
    Private _waterTile As Image
    Private _raftImage As Image

    Private _backgroundCache As Bitmap = Nothing
    Private _backgroundDirty As Boolean = True
    Private ReadOnly _drawAttributes As New ImageAttributes()
    Private _handShown As Boolean = False

    ''' <summary>Set by the form: decides which sprites should show the hand cursor.</summary>
    Public Property IsSelectable As Func(Of CharacterSprite, Boolean)

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        _drawAttributes.SetWrapMode(WrapMode.TileFlipXY)   ' avoids faint edge lines when scaling pixel art

        _skyTile = GameAssets.GetSheet("enviroment\sky.png")
        _bankTile = GameAssets.GetSheet("tiles\Grass_rocks.png")
        _waterTile = GameAssets.GetSheet("tiles\water-Sheet.png")
        _raftImage = GameAssets.GetBoatRaft()

        ComputeGeometry()

        _clock = System.Diagnostics.Stopwatch.StartNew()
        _animTimer = New Timer() With {.Interval = 30}
        AddHandler _animTimer.Tick, AddressOf OnAnimTick
        _animTimer.Start()
    End Sub

    ' ==================================================
    ' PUBLIC API (used by the form)
    ' ==================================================
    Public ReadOnly Property LeftBankRect As Rectangle
        Get
            Return _leftBank
        End Get
    End Property

    Public ReadOnly Property RiverRect As Rectangle
        Get
            Return _river
        End Get
    End Property

    Public ReadOnly Property RightBankRect As Rectangle
        Get
            Return _rightBank
        End Get
    End Property

    Public Sub ResetScene(characters As List(Of CharacterState))
        _sprites.Clear()
        _drawOrder = New List(Of CharacterSprite)
        _hoveredSprite = Nothing
        _boatDockSide = "Right"
        _boatTargetSide = "Right"
        _boatMoving = False
        _boatOnArrive = Nothing
        _boatFacing = FacingDirection.FaceLeft
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

    ''' <summary>Makes a sprite walk (walk animation, facing its movement direction) to a target.</summary>
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

    ''' <summary>Sails the raft to the given bank; riders face the direction of travel.</summary>
    Public Sub SailBoatTo(destSide As String, onArrive As Action)
        _boatTargetSide = destSide
        _boatFacing = If(destSide = "Left", FacingDirection.FaceLeft, FacingDirection.FaceRight)
        _boatMoving = True
        _boatOnArrive = onArrive
    End Sub

    Public Function BankSlotPoint(side As String, slotIndex As Integer) As PointF
        Dim col As Integer = slotIndex Mod 2
        Dim row As Integer = slotIndex \ 2
        Dim x As Single
        If side = "Left" Then
            x = _leftBank.Right - BankEdgeMargin - col * ColumnSpacing
        Else
            x = _rightBank.Left + BankEdgeMargin + col * ColumnSpacing
        End If
        Dim y As Single = GroundLineY() + (row - 1) * RowSpacing
        Return New PointF(x, y)
    End Function

    ''' <summary>Where a passenger's feet go on the raft deck. Follows the raft (and its bobbing) every frame.</summary>
    Public Function BoatSlotPoint(slot As Integer, count As Integer) As PointF
        Dim fraction As Single = 0.5F
        If count > 1 Then fraction = If(slot = 0, 0.3F, 0.7F)
        Return New PointF(_boatX + RaftWidth * fraction, GroundLineY() + BobOffset())
    End Function

    Public Sub StopAnimation()
        If _animTimer IsNot Nothing Then
            _animTimer.Stop()
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
        Return _river.Top + _river.Height / 2.0F
    End Function

    Private Function BobOffset() As Single
        Return CSng(Math.Sin(_bobPhase) * BobAmplitude)
    End Function

    ' Y of the feet line for a character standing level with the raft deck (no bobbing).
    Private Function GroundLineY() As Single
        Return CenterY() - RaftHeight / 2.0F + RaftHeight * DeckFootFraction
    End Function

    Private Function DockX(side As String) As Single
        If side = "Left" Then Return _river.Left + DockMargin
        Return _river.Right - RaftWidth - DockMargin
    End Function

    Private Sub ComputeGeometry()
        Dim w As Integer = Math.Max(1, Me.ClientSize.Width)
        Dim h As Integer = Math.Max(1, Me.ClientSize.Height)
        Dim riverW As Integer = Math.Max(330, Math.Min(520, CInt(w * 0.36)))
        Dim riverLeft As Integer = Math.Max(0, (w - riverW) \ 2)
        Dim top As Integer = CInt(h * 0.16)
        Dim bandH As Integer = Math.Max(10, (h - 16) - top)

        _river = New Rectangle(riverLeft, top, riverW, bandH)
        _leftBank = New Rectangle(0, top, riverLeft, bandH)
        _rightBank = New Rectangle(riverLeft + riverW, top, Math.Max(1, w - (riverLeft + riverW)), bandH)
        _backgroundDirty = True
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        ComputeGeometry()
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

        _waveOffset = (_waveOffset + 40.0 * dt) Mod 800.0
        _bobPhase += dt * 2.2

        UpdateBoat(dt)
        For Each s In _sprites
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
                s.Pos = BoatSlotPoint(s.BoatSlot, s.BoatSlotCount)
            Case SpriteMode.Walking
                StepWalk(s, dt)
        End Select

        Dim currentAction As SpriteAction = If(s.Mode = SpriteMode.Walking, SpriteAction.Walk, SpriteAction.Idle)
        If currentAction <> s.LastAction Then
            s.LastAction = currentAction
            s.AnimMs = 0          ' a new action always starts from its first frame
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

    ' Which way a sprite faces. Movement and boat travel always win over mouse hover.
    Private Function GetFacing(s As CharacterSprite) As FacingDirection
        Select Case s.Mode
            Case SpriteMode.Walking
                Return s.MoveFacing
            Case SpriteMode.OnBoat
                Return _boatFacing
            Case Else
                If s.IsMonster AndAlso s.IsHovered Then Return FacingDirection.FaceLeft
                Return FacingDirection.FaceFront
        End Select
    End Function

    ' ==================================================
    ' MOUSE
    ' ==================================================
    Private Function FindSpriteAt(p As Point) As CharacterSprite
        Dim list As List(Of CharacterSprite) = If(_drawOrder.Count > 0, _drawOrder, _sprites)
        For i As Integer = list.Count - 1 To 0 Step -1
            If list(i).Bounds.Contains(p) Then Return list(i)
        Next
        Return Nothing
    End Function

    ' Hover is re-evaluated every tick from the real cursor position, so a sprite that walks
    ' away from (or arrives under) a stationary cursor updates correctly.
    Private Sub UpdateHover()
        Dim clientPoint As Point = Me.PointToClient(Control.MousePosition)
        Dim hit As CharacterSprite = Nothing
        If Me.ClientRectangle.Contains(clientPoint) Then hit = FindSpriteAt(clientPoint)

        If hit IsNot _hoveredSprite Then
            If _hoveredSprite IsNot Nothing Then _hoveredSprite.IsHovered = False
            _hoveredSprite = hit
            If _hoveredSprite IsNot Nothing Then _hoveredSprite.IsHovered = True
        End If

        Dim showHand As Boolean = (hit IsNot Nothing) AndAlso hit.Mode = SpriteMode.AtBank AndAlso
                                  (IsSelectable Is Nothing OrElse IsSelectable.Invoke(hit))
        If showHand <> _handShown Then
            _handShown = showHand
            Me.Cursor = If(showHand, Cursors.Hand, Cursors.Default)
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        UpdateHover()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        If _hoveredSprite IsNot Nothing Then
            _hoveredSprite.IsHovered = False
            _hoveredSprite = Nothing
        End If
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        MyBase.OnMouseClick(e)
        If e.Button <> MouseButtons.Left Then Return
        Dim hit As CharacterSprite = FindSpriteAt(e.Location)
        If hit IsNot Nothing Then RaiseEvent SpriteClicked(hit)
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

        If _backgroundDirty OrElse _backgroundCache Is Nothing Then RebuildBackground()
        g.DrawImage(_backgroundCache, 0, 0, _backgroundCache.Width, _backgroundCache.Height)

        DrawWater(g)
        DrawBoat(g)

        ' Painter's algorithm: boat first, then characters from back (small Y) to front.
        ' Passengers are therefore always painted over the raft.
        _drawOrder = _sprites.OrderBy(Function(sp) sp.Pos.Y).ToList()
        For Each s In _drawOrder
            DrawSprite(g, s)
        Next

        MyBase.OnPaint(e)
    End Sub

    Private Sub RebuildBackground()
        Dim w As Integer = Math.Max(1, Me.ClientSize.Width)
        Dim h As Integer = Math.Max(1, Me.ClientSize.Height)

        If _backgroundCache IsNot Nothing Then _backgroundCache.Dispose()
        _backgroundCache = New Bitmap(w, h)

        Using g As Graphics = Graphics.FromImage(_backgroundCache)
            g.SmoothingMode = SmoothingMode.None
            g.InterpolationMode = InterpolationMode.NearestNeighbor
            g.PixelOffsetMode = PixelOffsetMode.Half

            If _skyTile IsNot Nothing Then
                TileImage(g, _skyTile, New Rectangle(0, 0, w, h))
            Else
                Using skyBrush As New SolidBrush(Color.FromArgb(150, 200, 235))
                    g.FillRectangle(skyBrush, 0, 0, w, h)
                End Using
            End If

            If _bankTile IsNot Nothing Then
                TileImage(g, _bankTile, _leftBank)
                TileImage(g, _bankTile, _rightBank)
            Else
                Using bankBrush As New SolidBrush(Color.FromArgb(120, 190, 110))
                    g.FillRectangle(bankBrush, _leftBank)
                    g.FillRectangle(bankBrush, _rightBank)
                End Using
            End If
        End Using

        _backgroundDirty = False
    End Sub

    Private Sub DrawWater(g As Graphics)
        If _waterTile IsNot Nothing Then
            TileImageScrollingVertical(g, _waterTile, _river, _waveOffset)
        Else
            Using riverBrush As New SolidBrush(Color.FromArgb(70, 140, 200))
                g.FillRectangle(riverBrush, _river)
            End Using
            Using wavePen As New Pen(Color.FromArgb(90, 255, 255, 255), 2)
                wavePen.DashStyle = DashStyle.Dash
                Dim y As Integer = _river.Top - CInt(_waveOffset Mod 28.0)
                While y < _river.Bottom
                    If y >= _river.Top Then g.DrawLine(wavePen, _river.Left + 12, y, _river.Right - 12, y)
                    y += 28
                End While
            End Using
        End If
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
        Dim currentAction As SpriteAction = If(s.Mode = SpriteMode.Walking, SpriteAction.Walk, SpriteAction.Idle)
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
        s.Bounds = dest

        Using shadowBrush As New SolidBrush(Color.FromArgb(70, 0, 0, 0))
            g.FillEllipse(shadowBrush, CInt(s.Pos.X - w * 0.35F), CInt(s.Pos.Y - 5), CInt(w * 0.7F), 10)
        End Using

        g.DrawImage(img, dest, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, _drawAttributes)

        If s.IsSelected Then
            Dim outlineColor As Color = If(s.IsMonster, Color.FromArgb(230, 90, 90), Color.FromArgb(90, 160, 255))
            Using outlinePen As New Pen(outlineColor, 3)
                g.DrawRectangle(outlinePen, dest.X - 4, dest.Y - 4, dest.Width + 8, dest.Height + 8)
            End Using
        End If
    End Sub

    Private Sub TileImage(g As Graphics, img As Image, area As Rectangle)
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

    Private Sub TileImageScrollingVertical(g As Graphics, img As Image, area As Rectangle, offsetY As Double)
        If area.Width <= 0 OrElse area.Height <= 0 OrElse img.Height <= 0 Then Return
        g.SetClip(area)
        Dim startY As Integer = area.Top - CInt(offsetY Mod img.Height)
        Dim y As Integer = startY
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

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If _animTimer IsNot Nothing Then
                _animTimer.Stop()
                _animTimer.Dispose()
                _animTimer = Nothing
            End If
            If _backgroundCache IsNot Nothing Then
                _backgroundCache.Dispose()
                _backgroundCache = Nothing
            End If
            _drawAttributes.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
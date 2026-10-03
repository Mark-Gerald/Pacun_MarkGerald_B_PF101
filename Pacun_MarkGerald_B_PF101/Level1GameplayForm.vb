Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Linq

Public Class Level1GameplayForm
    Inherits Form

    Private gamePanel As GameScenePanel
    Private instructionLabel As Label
    Private statusLabel As Label
    Private moveCountLabel As Label
    Private crossButton As PixelButton
    Private resetButton As PixelButton
    Private backButton As PixelButton

    ' ---- puzzle state (logic only; no drawing here) ----
    Private characters As New List(Of CharacterState)
    Private currentBank As String = "Right"
    Private moveCount As Integer = 0
    Private gameWon As Boolean = False
    Private isAnimating As Boolean = False

    ' ---- crossing sequence bookkeeping ----
    Private crossingId As Integer = 0
    Private crossingPassengers As New List(Of CharacterState)
    Private crossingDest As String = ""
    Private landedCount As Integer = 0

    Public Sub New()
        Me.Text = "Level 1 " & ChrW(8212) & " River Crossing"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 620)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(20, 20, 26)

        BuildLayout()

        gamePanel.IsSelectable = Function(sp As CharacterSprite)
                                     If isAnimating OrElse gameWon Then Return False
                                     If sp.Mode = SpriteMode.Walking Then Return False
                                     If sp.Mode = SpriteMode.OnBoat Then Return True
                                     If sp.Mode = SpriteMode.AtBank AndAlso sp.Character.Side = currentBank Then Return True
                                     Return False
                                 End Function
        AddHandler gamePanel.SpriteClicked, AddressOf GamePanel_SpriteClicked
        AddHandler Me.FormClosed, AddressOf Level1GameplayForm_FormClosed

        InitializeGame()
    End Sub

    ' ==================================================
    ' LAYOUT  (themed top panel; no white bottom bar)
    ' ==================================================
    Private Sub BuildLayout()
        Dim topPanel As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 130,
            .BackColor = Color.FromArgb(24, 24, 30)
        }
        Me.Controls.Add(topPanel)

        instructionLabel = New Label() With {
            .Text = "Click a passenger on the boat's bank to walk them aboard. Click a boarded passenger to send them back.",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
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

        moveCountLabel = New Label() With {
            .Text = "Moves: 0",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(16, 62)
        }
        topPanel.Controls.Add(moveCountLabel)

        crossButton = New PixelButton("CROSS RIVER", Color.FromArgb(76, 175, 80))
        crossButton.Size = New Size(170, 46)
        crossButton.Cursor = Cursors.Hand
        AddHandler crossButton.Click, AddressOf CrossButton_Click
        topPanel.Controls.Add(crossButton)

        resetButton = New PixelButton("RESET", Color.FromArgb(66, 133, 200))
        resetButton.Size = New Size(110, 46)
        resetButton.Cursor = Cursors.Hand
        AddHandler resetButton.Click, AddressOf ResetButton_Click
        topPanel.Controls.Add(resetButton)

        backButton = New PixelButton("BACK TO MENU", Color.FromArgb(150, 90, 190))
        backButton.Size = New Size(160, 46)
        backButton.Cursor = Cursors.Hand
        AddHandler backButton.Click, AddressOf BackButton_Click
        topPanel.Controls.Add(backButton)

        Dim positionButtons As Action = Sub()
                                            Dim rightPad As Integer = 16
                                            Dim gap As Integer = 10
                                            Dim y As Integer = 62
                                            backButton.Location = New Point(topPanel.Width - rightPad - backButton.Width, y)
                                            resetButton.Location = New Point(backButton.Left - gap - resetButton.Width, y)
                                            crossButton.Location = New Point(resetButton.Left - gap - crossButton.Width, y)
                                        End Sub
        AddHandler topPanel.Resize, Sub(s, e) positionButtons()
        positionButtons()

        gamePanel = New GameScenePanel() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(gamePanel)
        gamePanel.BringToFront()
    End Sub

    ' ==================================================
    ' GAME STATE
    ' ==================================================
    Private Sub InitializeGame()
        crossingId += 1   ' invalidates callbacks from any previous crossing or walking

        ' Order matters only for the starting layout: each row holds one innocent + one monster.
        characters = New List(Of CharacterState) From {
            New CharacterState("I1", "Innocent", "Right"),
            New CharacterState("M1", "Monster", "Right"),
            New CharacterState("I2", "Innocent", "Right"),
            New CharacterState("M2", "Monster", "Right"),
            New CharacterState("I3", "Innocent", "Right"),
            New CharacterState("M3", "Monster", "Right")
        }
        crossingPassengers = New List(Of CharacterState)
        currentBank = "Right"
        moveCount = 0
        gameWon = False
        isAnimating = False
        landedCount = 0

        gamePanel.ResetScene(characters)

        UpdateMoveCount()
        SetStatus("Select passengers to begin.")
    End Sub

    ' ==================================================
    ' CHARACTER CLICK: walk to boat, or walk back to bank
    ' ==================================================
    Private Sub GamePanel_SpriteClicked(sprite As CharacterSprite)
        If isAnimating OrElse gameWon Then Return
        If sprite.Mode = SpriteMode.Walking Then Return

        Dim ch As CharacterState = sprite.Character

        If sprite.Mode = SpriteMode.OnBoat Then
            ' ---- click a boarded character: walk back to its home slot on the current bank ----
            Dim capturedSprite As CharacterSprite = sprite
            Dim capturedCh As CharacterState = ch
            Dim homeSlot As Integer = capturedSprite.SlotIndex

            gamePanel.WalkSpriteTo(capturedSprite,
                                   Function() gamePanel.BankSlotPoint(currentBank, homeSlot),
                                   SpriteMode.AtBank,
                                   Sub()
                                       If characters.Contains(capturedCh) Then
                                           capturedCh.OnBoat = False
                                           ReflowBoatSlots()
                                           SetStatus("Passenger returned to land.")
                                       End If
                                   End Sub)
            AudioManager.PlaySfx("Audio\SFX\Walking_On_Wood_Sound_Effect.mp3")
            SetStatus("Passenger returning...")
            Return
        End If

        If sprite.Mode <> SpriteMode.AtBank Then Return

        If ch.Side <> currentBank Then
            SetStatus("That character is on the other bank. Select from the boat's bank.")
            Return
        End If

        ' ---- cap at 2 boarded passengers ----
        Dim boarded As Integer = 0
        For Each c In characters
            If c.OnBoat Then boarded += 1
        Next

        If boarded >= 2 Then
            AudioManager.PlaySfx("Audio\SFX\Nope_Invalid_Move.mp3")
            SetStatus("The boat can only carry up to 2 passengers.")
            Return
        End If

        ' ---- assign the next free boat slot, then walk aboard ----
        Dim slot As Integer = boarded
        Dim totalAfter As Integer = boarded + 1

        Dim capturedSpriteB As CharacterSprite = sprite
        Dim capturedChB As CharacterState = ch
        Dim capturedSlot As Integer = slot
        Dim capturedTotal As Integer = totalAfter

        gamePanel.WalkSpriteTo(capturedSpriteB,
                               Function() gamePanel.BoatSlotPoint(capturedSlot, capturedTotal),
                               SpriteMode.OnBoat,
                               Sub()
                                   If characters.Contains(capturedChB) Then
                                       capturedChB.OnBoat = True
                                       ReflowBoatSlots()
                                       SetStatus("Passenger boarded. Select another or click Cross River.")
                                   End If
                               End Sub)
        AudioManager.PlaySfx("Audio\SFX\Walking_On_Wood_Sound_Effect.mp3")
        SetStatus("Passenger walking to boat...")
    End Sub

    ' Keeps boat-slot positions consistent when the set of boarded passengers changes.
    Private Sub ReflowBoatSlots()
        Dim onboard As New List(Of CharacterState)
        For Each c In characters
            If c.OnBoat Then onboard.Add(c)
        Next
        For i As Integer = 0 To onboard.Count - 1
            Dim sp As CharacterSprite = gamePanel.FindSprite(onboard(i))
            If sp IsNot Nothing Then
                sp.BoatSlot = i
                sp.BoatSlotCount = onboard.Count
            End If
        Next
    End Sub

    Private Function IsAnySpriteWalking() As Boolean
        For Each ch In characters
            Dim sp As CharacterSprite = gamePanel.FindSprite(ch)
            If sp IsNot Nothing AndAlso sp.Mode = SpriteMode.Walking Then Return True
        Next
        Return False
    End Function

    Private Function BoardedPassengers() As List(Of CharacterState)
        Return characters.Where(Function(c) c.OnBoat).ToList()
    End Function

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
    ' CROSSING SEQUENCE: sail (passengers already boarded) -> disembark (walk)
    ' ==================================================
    Private Sub CrossButton_Click(sender As Object, e As EventArgs)
        If isAnimating OrElse gameWon Then Return

        If IsAnySpriteWalking() Then
            SetStatus("Please wait for passengers to finish moving.")
            Return
        End If

        Dim boarded As List(Of CharacterState) = BoardedPassengers()
        If boarded.Count = 0 Then
            SetStatus("Click a passenger to board the boat first.")
            Return
        End If

        Dim dest As String = If(currentBank = "Right", "Left", "Right")

        If Not IsMoveLegal(boarded, dest) Then
            AudioManager.PlaySfx("Audio\SFX\Nope_Invalid_Move.mp3")
            SetStatus("Invalid move " & ChrW(8212) & " monsters would outnumber innocents on a bank.")
            Return
        End If

        StartCrossing(dest, boarded)
    End Sub

    Private Sub StartCrossing(dest As String, passengers As List(Of CharacterState))
        isAnimating = True

        crossingId += 1
        Dim myId As Integer = crossingId

        crossingPassengers = New List(Of CharacterState)(passengers)
        crossingDest = dest
        landedCount = 0

        SetStatus("Crossing the river...")
        AudioManager.PlaySfx("Audio\SFX\Canoe_Paddle_Sound_Effect.mp3")

        gamePanel.SailBoatTo(dest, Sub() OnBoatArrived(myId))
    End Sub

    Private Sub OnBoatArrived(id As Integer)
        If id <> crossingId Then Return

        SetStatus("Disembarking...")

        Dim dest As String = crossingDest
        For Each ch In crossingPassengers
            Dim passenger As CharacterState = ch
            Dim sprite As CharacterSprite = gamePanel.FindSprite(passenger)
            If sprite Is Nothing Then Continue For

            ' Passengers keep their original SlotIndex on the destination bank.
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
        UpdateMoveCount()
        isAnimating = False

        If AllOnLeftBank() Then
            gameWon = True
            AudioManager.PlaySfx("Audio\SFX\Victory_Jingle.mp3")
            SetStatus("Victory! All characters reached the other side in " & moveCount & " move(s).")
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
        crossingId += 1
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
    Public IsSelected As Boolean = False    ' kept for compatibility; not drawn anymore
    Public MoveFacing As FacingDirection = FacingDirection.FaceFront
    Public BoatSlot As Integer = 0
    Public BoatSlotCount As Integer = 1

    Friend WalkTarget As Func(Of PointF)
    Friend WalkOnArrive As Action
    Friend ModeAfterWalk As SpriteMode = SpriteMode.AtBank
    Friend AnimMs As Double
    Friend LastAction As SpriteAction = SpriteAction.Idle
    Friend Bounds As Rectangle

    Public Sub New(characterValue As CharacterState, slotIndexValue As Integer)
        Character = characterValue
        SlotIndex = slotIndexValue
        IsMonster = (characterValue.Type = "Monster")
        AnimMs = slotIndexValue * 137.0
    End Sub
End Class

''' <summary>
''' Draws the whole scene on ONE surface and drives every animation from one timer.
''' The raft is drawn first; characters are then drawn back-to-front so passengers are on top.
''' </summary>
Public Class GameScenePanel
    Inherits Panel

    Public Event SpriteClicked(sprite As CharacterSprite)

    ' ---- tuning constants ----
    Private Const RaftScale As Integer = 2
    Private Const WalkSpeed As Double = 160.0
    Private Const BoatSpeed As Double = 150.0
    Private Const BobAmplitude As Double = 2.0
    Private Const DeckFootFraction As Single = 0.35F
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

    Public Property IsSelectable As Func(Of CharacterSprite, Boolean)

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        _drawAttributes.SetWrapMode(WrapMode.TileFlipXY)

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
    ' ANIMATION
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
            s.AnimMs = 0
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

    Private Sub UpdateHover()
        Dim clientPoint As Point = Me.PointToClient(Control.MousePosition)
        Dim hit As CharacterSprite = Nothing
        If Me.ClientRectangle.Contains(clientPoint) Then hit = FindSpriteAt(clientPoint)

        If hit IsNot _hoveredSprite Then
            If _hoveredSprite IsNot Nothing Then _hoveredSprite.IsHovered = False
            _hoveredSprite = hit
            If _hoveredSprite IsNot Nothing Then _hoveredSprite.IsHovered = True
        End If

        Dim showHand As Boolean = (hit IsNot Nothing) AndAlso
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

        ' NOTE: the previous red/blue selection outline has been intentionally removed.
        ' Player feedback now comes from the walking animation onto/off the boat.
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
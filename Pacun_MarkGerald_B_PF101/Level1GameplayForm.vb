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

    Private characters As New List(Of CharacterState)
    Private selectedCharacters As New List(Of CharacterState)
    Private currentBank As String = "Right"
    Private moveCount As Integer = 0
    Private hasWon As Boolean = False

    ' Movement State for walking to/from boat
    Private isMoving As Boolean = False
    Private movementTimer As Timer
    Private movingCharacter As CharacterState
    Private movementWaypoints As New Queue(Of Point)
    Private isMovingToBoat As Boolean = False

    Private characterIcons As New Dictionary(Of CharacterState, SpritePanel)
    Private boatIcon As SpritePanel
    Private crossTimer As Timer

    Private crossStartX As Integer
    Private crossEndX As Integer
    Private crossStep As Integer
    Private Const CrossSteps As Integer = 24
    Private crossingPassengers As New List(Of CharacterState)
    Private crossingToBank As String = ""

    Private Const CharacterSize As Integer = 72

    Public Sub New()
        Me.Text = "Level 1 " & ChrW(8212) & " River Crossing"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 620)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(240, 240, 240)

        BuildLayout()
        InitializeGame()

        AddHandler Me.Resize, Sub(s, e) Me.BeginInvoke(New MethodInvoker(AddressOf RefreshCharacterLayout))
        AddHandler Me.FormClosed, AddressOf Level1GameplayForm_FormClosed
    End Sub

    ' ==================================================
    ' LAYOUT (Task 6: Redesigned Controls)
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

        ' Themed control bar instead of white panel
        Dim controlPanel As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 70,
            .BackColor = Color.FromArgb(24, 24, 30) ' Dark theme matching menu
        }
        Me.Controls.Add(controlPanel)

        moveCountLabel = New Label() With {
            .Text = "Moves: 0",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(16, 25)
        }
        controlPanel.Controls.Add(moveCountLabel)

        crossButton = New Button() With {
            .Text = "Cross River",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(76, 175, 80), ' Themed green
            .ForeColor = Color.White,
            .Size = New Size(140, 36),
            .Location = New Point(120, 17)
        }
        crossButton.FlatAppearance.BorderSize = 0
        AddHandler crossButton.Click, AddressOf CrossButton_Click
        controlPanel.Controls.Add(crossButton)

        resetButton = New Button() With {
            .Text = "Reset",
            .Font = New Font("Segoe UI", 9.5F),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(66, 133, 200), ' Themed blue
            .ForeColor = Color.White,
            .Size = New Size(100, 36),
            .Location = New Point(270, 17)
        }
        resetButton.FlatAppearance.BorderSize = 0
        AddHandler resetButton.Click, AddressOf ResetButton_Click
        controlPanel.Controls.Add(resetButton)

        backButton = New Button() With {
            .Text = ChrW(8592) & " Back to Menu",
            .Font = New Font("Segoe UI", 9.5F),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(150, 90, 190), ' Themed purple
            .ForeColor = Color.White,
            .Size = New Size(150, 36)
        }
        backButton.FlatAppearance.BorderSize = 0
        AddHandler backButton.Click, AddressOf BackButton_Click
        controlPanel.Controls.Add(backButton)

        Dim positionBackButton As Action = Sub() backButton.Location = New Point(controlPanel.Width - backButton.Width - 16, 17)
        AddHandler controlPanel.Resize, Sub(s, e) positionBackButton()
        positionBackButton()
    End Sub

    ' ==================================================
    ' GAME STATE
    ' ==================================================
    Private Sub InitializeGame()
        If crossTimer IsNot Nothing Then
            crossTimer.Stop()
            crossTimer.Dispose()
            crossTimer = Nothing
        End If
        If movementTimer IsNot Nothing Then
            movementTimer.Stop()
            movementTimer.Dispose()
            movementTimer = Nothing
        End If

        characters = New List(Of CharacterState) From {
            New CharacterState("I1", "Innocent", "Right"),
            New CharacterState("I2", "Innocent", "Right"),
            New CharacterState("I3", "Innocent", "Right"),
            New CharacterState("M1", "Monster", "Right"),
            New CharacterState("M2", "Monster", "Right"),
            New CharacterState("M3", "Monster", "Right")
        }
        selectedCharacters.Clear()
        currentBank = "Right"
        moveCount = 0
        hasWon = False
        isMoving = False
        movingCharacter = Nothing

        ' Remove old sprite controls
        If characterIcons.Count > 0 Then
            For Each kv In characterIcons
                kv.Value.StopAnimationTimer()
                gamePanel.Controls.Remove(kv.Value)
                kv.Value.Dispose()
            Next
            characterIcons.Clear()
        End If
        If boatIcon IsNot Nothing Then
            gamePanel.Controls.Remove(boatIcon)
            boatIcon = Nothing
        End If

        crossButton.Enabled = True
        UpdateMoveCount()
        SetStatus("Select passengers to begin.")
        EnsureIconsCreated()
        RefreshCharacterLayout()
    End Sub

    Private Sub EnsureIconsCreated()
        If boatIcon Is Nothing Then
            boatIcon = New SpritePanel()
            boatIcon.SpriteImage = GameAssets.GetFrame("enviroment\Boat.png", 0, 0, GameAssets.BoatCellWidth, GameAssets.BoatCellHeight)
            gamePanel.Controls.Add(boatIcon)
        End If

        For Each ch In characters
            If Not characterIcons.ContainsKey(ch) Then
                Dim icon As New SpritePanel()
                If ch.Type = "Innocent" Then
                    Dim idleFrames As Image() = GameAssets.GetTrimmedAnimation("Character\Farmer-Idle.png", GameAssets.FarmerIdleFrameCount, 0, GameAssets.FarmerIdleFrameW, GameAssets.FarmerIdleFrameH)
                    Dim walkFrames As Image() = GameAssets.GetTrimmedAnimation("Character\Farmer-Walk.png", 4, 0, GameAssets.FarmerWalkFrameW, GameAssets.FarmerWalkFrameH)
                    If walkFrames Is Nothing OrElse walkFrames.Length = 0 OrElse walkFrames(0) Is Nothing Then
                        walkFrames = idleFrames
                    End If
                    icon.SetAnimations(idleFrames, walkFrames, 150)
                    icon.AccentColor = Color.FromArgb(90, 160, 255)
                Else
                    Dim idleFrames As Image() = GameAssets.GetTrimmedAnimation("enemies\ground_enemy-Sheet.png", 1, 0, GameAssets.EnemyFrameW, GameAssets.EnemyFrameH)
                    Dim walkFrames As Image() = GameAssets.GetTrimmedAnimation("enemies\ground_enemy-Sheet.png", GameAssets.EnemyFrameCount, 0, GameAssets.EnemyFrameW, GameAssets.EnemyFrameH)
                    icon.SetAnimations(idleFrames, walkFrames, 150)
                    icon.AccentColor = Color.FromArgb(230, 90, 90)
                End If
                Dim capturedCh As CharacterState = ch
                AddHandler icon.Click, Sub(s, e) CharacterIcon_Click(capturedCh)
                gamePanel.Controls.Add(icon)
                characterIcons(ch) = icon
            End If
        Next
    End Sub

    ' ==================================================
    ' RENDERING (Task 2: Fixed Character Size)
    ' ==================================================
    Private Sub RefreshCharacterLayout()
        If gamePanel.Width <= 0 OrElse gamePanel.Height <= 0 Then Return

        Dim leftRect As Rectangle = gamePanel.LeftBankRect
        Dim rightRect As Rectangle = gamePanel.RightBankRect
        Dim riverRect As Rectangle = gamePanel.RiverRect

        Dim iconSize As Integer = CharacterSize
        Dim perRow As Integer = Math.Max(1, (leftRect.Width - 16) \ (iconSize + 10))

        Dim boatW As Integer = 120
        Dim boatH As Integer = CInt(boatW * (GameAssets.BoatCellHeight / CSng(GameAssets.BoatCellWidth)))
        Dim boatX As Integer = If(currentBank = "Left", riverRect.Left + 8, riverRect.Right - boatW - 8)
        Dim boatY As Integer = riverRect.Top + Math.Max(0, (riverRect.Height - boatH) \ 2)
        boatIcon.Size = New Size(boatW, boatH)
        boatIcon.Location = New Point(boatX, boatY)

        boatIcon.SendToBack()

        Dim leftIndex As Integer = 0
        Dim rightIndex As Integer = 0
        Dim boatPassengerIndex As Integer = 0

        For Each ch In characters
            Dim icon As SpritePanel = characterIcons(ch)

            If ch IsNot movingCharacter Then
                icon.IsOnBoat = ch.OnBoat
                icon.IsWalking = False
                icon.Size = New Size(CharacterSize, CharacterSize) ' Consistent size

                If ch.OnBoat Then
                    Dim passengerY As Integer = boatY + (boatH - icon.Height) \ 2
                    icon.Location = New Point(boatX + 10 + boatPassengerIndex * 54, passengerY)
                    boatPassengerIndex += 1
                    icon.BringToFront()
                ElseIf ch.Side = "Left" Then
                    Dim col As Integer = leftIndex Mod perRow
                    Dim row As Integer = leftIndex \ perRow
                    icon.Location = New Point(leftRect.Left + 16 + col * (iconSize + 10), leftRect.Top + 16 + row * (iconSize + 10))
                    ch.HomeLocation = icon.Location
                    leftIndex += 1
                Else
                    Dim col As Integer = rightIndex Mod perRow
                    Dim row As Integer = rightIndex \ perRow
                    icon.Location = New Point(rightRect.Left + 16 + col * (iconSize + 10), rightRect.Top + 16 + row * (iconSize + 10))
                    ch.HomeLocation = icon.Location
                    rightIndex += 1
                End If
            End If
        Next
    End Sub

    ' ==================================================
    ' SELECTION & MOVEMENT (Tasks 3, 4, 5)
    ' ==================================================
    Private Sub CharacterIcon_Click(ch As CharacterState)
        If isMoving OrElse hasWon Then Return
        If ch.Side <> currentBank AndAlso Not ch.OnBoat Then Return

        If ch.OnBoat Then
            selectedCharacters.Remove(ch)
            StartCharacterMovement(ch, False)
            Return
        End If

        If Not selectedCharacters.Contains(ch) Then
            If selectedCharacters.Count >= 2 Then
                SetStatus("The boat can only carry up to 2 passengers.")
                Return
            End If
            selectedCharacters.Add(ch)
            StartCharacterMovement(ch, True)
        End If
    End Sub

    Private Sub StartCharacterMovement(ch As CharacterState, toBoat As Boolean)
        isMoving = True
        isMovingToBoat = toBoat
        movingCharacter = ch
        movementWaypoints.Clear()

        Dim targetX As Integer
        Dim targetY As Integer

        If toBoat Then
            targetX = boatIcon.Location.X + 10 + (selectedCharacters.IndexOf(ch) * 54)
            targetY = boatIcon.Location.Y + (boatIcon.Height - CharacterSize) \ 2
        Else
            targetX = ch.HomeLocation.X
            targetY = ch.HomeLocation.Y
        End If

        ' Task 5: Fix walking route to avoid water
        Dim startPos = characterIcons(ch).Location
        Dim riverRect = gamePanel.RiverRect
        Dim bankEdgeX As Integer = If(ch.Side = "Left", riverRect.Left - CharacterSize, riverRect.Right)

        ' Move horizontally to bank edge, then vertically, then horizontally to target
        movementWaypoints.Enqueue(New Point(bankEdgeX, startPos.Y))
        movementWaypoints.Enqueue(New Point(bankEdgeX, targetY))
        movementWaypoints.Enqueue(New Point(targetX, targetY))

        Dim icon = characterIcons(ch)
        icon.IsWalking = True
        icon.Size = New Size(CharacterSize, CharacterSize) ' Task 2: Keep size consistent

        movementTimer = New Timer() With {.Interval = 20}
        AddHandler movementTimer.Tick, AddressOf MovementTimer_Tick
        movementTimer.Start()

        SetStatus(If(toBoat, "Passenger walking to boat...", "Passenger returning..."))
    End Sub

    Private Sub MovementTimer_Tick(sender As Object, e As EventArgs)
        Dim currentIcon As SpritePanel = Nothing

        If movementWaypoints.Count = 0 Then
            movementTimer.Stop()
            movementTimer.Dispose()
            movementTimer = Nothing
            isMoving = False

            currentIcon = characterIcons(movingCharacter)
            currentIcon.IsWalking = False

            If isMovingToBoat Then
                movingCharacter.OnBoat = True
                currentIcon.IsOnBoat = True
                SetStatus("Passenger boarded. Select another or click Cross River.")
            Else
                movingCharacter.OnBoat = False
                currentIcon.IsOnBoat = False
                SetStatus("Passenger returned to land.")
            End If

            movingCharacter = Nothing
            RefreshCharacterLayout()
            Return
        End If

        Dim target As Point = movementWaypoints.Peek()
        currentIcon = characterIcons(movingCharacter)
        Dim currentPos = currentIcon.Location

        If target.X > currentPos.X Then
            currentIcon.Facing = SpritePanel.FacingDirection.Right
        ElseIf target.X < currentPos.X Then
            currentIcon.Facing = SpritePanel.FacingDirection.Left
        End If

        ' Task 3: Double walking speed (step from 4 to 8)
        Dim stepX As Integer = Math.Sign(target.X - currentPos.X) * 8
        Dim stepY As Integer = Math.Sign(target.Y - currentPos.Y) * 8

        Dim newX As Integer = currentPos.X + stepX
        Dim newY As Integer = currentPos.Y + stepY

        If Math.Abs(target.X - currentPos.X) <= 8 Then newX = target.X
        If Math.Abs(target.Y - currentPos.Y) <= 8 Then newY = target.Y

        currentIcon.Location = New Point(newX, newY)

        If newX = target.X AndAlso newY = target.Y Then
            movementWaypoints.Dequeue()
        End If
    End Sub

    ' ==================================================
    ' CROSSING (Task 4: Walk after crossing)
    ' ==================================================
    Private Sub CrossButton_Click(sender As Object, e As EventArgs)
        If isMoving OrElse hasWon Then Return

        If selectedCharacters.Count = 0 Then
            SetStatus("Select at least one passenger before crossing.")
            Return
        End If

        If Not IsLegalMove() Then
            AudioManager.PlaySfx("Audio\SFX\Nope_Invalid_Move.mp3")
            SetStatus("Invalid move " & ChrW(8212) & " monsters would outnumber innocents on a bank.")
            Return
        End If

        StartCrossingAnimation()
    End Sub

    Private Sub StartCrossingAnimation()
        Dim crossingPassengersList As New List(Of CharacterState)(selectedCharacters)
        Dim toBank As String = If(currentBank = "Right", "Left", "Right")

        Dim riverRect As Rectangle = gamePanel.RiverRect
        crossStartX = boatIcon.Location.X
        crossEndX = If(toBank = "Left", riverRect.Left + 8, riverRect.Right - boatIcon.Width - 8)
        crossStep = 0

        AudioManager.PlaySfx("Audio\SFX\Canoe_Paddle_Sound_Effect.mp3")
        SetStatus("Crossing the river...")

        crossTimer = New Timer() With {.Interval = 30}
        AddHandler crossTimer.Tick, Sub(s, e)
                                        crossStep += 1
                                        Dim progress As Single = crossStep / CSng(CrossSteps)
                                        Dim newX As Integer = CInt(crossStartX + (crossEndX - crossStartX) * progress)
                                        boatIcon.Location = New Point(newX, boatIcon.Location.Y)

                                        Dim passengerIndex As Integer = 0
                                        For Each ch In crossingPassengersList
                                            Dim icon As SpritePanel = characterIcons(ch)
                                            Dim passengerY As Integer = boatIcon.Location.Y + (boatIcon.Height - icon.Height) \ 2
                                            icon.Location = New Point(newX + 10 + passengerIndex * 54, passengerY)

                                            icon.IsWalking = True
                                            icon.Facing = If(crossEndX > crossStartX, SpritePanel.FacingDirection.Right, SpritePanel.FacingDirection.Left)
                                            passengerIndex += 1
                                        Next

                                        If crossStep >= CrossSteps Then
                                            crossTimer.Stop()
                                            crossTimer.Dispose()
                                            crossTimer = Nothing

                                            For Each ch In crossingPassengersList
                                                ch.OnBoat = False
                                                ch.Side = toBank
                                                Dim icon As SpritePanel = characterIcons(ch)
                                                icon.IsWalking = False
                                                icon.IsOnBoat = False
                                                icon.Facing = SpritePanel.FacingDirection.Right
                                            Next

                                            currentBank = toBank
                                            moveCount += 1
                                            selectedCharacters.Clear()
                                            crossingPassengersList.Clear()
                                            UpdateMoveCount()

                                            If CheckWinCondition() Then
                                                hasWon = True
                                                crossButton.Enabled = False
                                                AudioManager.PlaySfx("Audio\SFX\Victory_Jingle.mp3")
                                                SetStatus("Victory! All characters reached the other side in " & moveCount & " move(s).")
                                                RefreshCharacterLayout()
                                                MessageBox.Show("All characters crossed safely!" & vbCrLf & "Moves used: " & moveCount, "Victory", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                            Else
                                                SetStatus("Crossing complete. Select the next passengers.")
                                                RefreshCharacterLayout()
                                            End If
                                        End If
                                    End Sub
        crossTimer.Start()
    End Sub

    ' ==================================================
    ' MOVE VALIDATION
    ' ==================================================
    Private Function IsLegalMove() As Boolean
        For Each ch In selectedCharacters
            If ch.Side <> currentBank Then Return False
        Next

        If selectedCharacters.Count < 1 OrElse selectedCharacters.Count > 2 Then
            Return False
        End If

        Dim nextBank As String = If(currentBank = "Right", "Left", "Right")
        Dim leftInnocents As Integer = 0
        Dim leftMonsters As Integer = 0
        Dim rightInnocents As Integer = 0
        Dim rightMonsters As Integer = 0

        For Each ch In characters
            If ch.OnBoat Then Continue For
            If ch.Side = "Left" Then
                If ch.Type = "Innocent" Then leftInnocents += 1 Else leftMonsters += 1
            Else
                If ch.Type = "Innocent" Then rightInnocents += 1 Else rightMonsters += 1
            End If
        Next

        For Each ch In selectedCharacters
            If ch.Side = "Left" Then
                If ch.Type = "Innocent" Then
                    leftInnocents -= 1
                    If nextBank = "Left" Then leftInnocents += 1 Else rightInnocents += 1
                Else
                    leftMonsters -= 1
                    If nextBank = "Left" Then leftMonsters += 1 Else rightMonsters += 1
                End If
            Else
                If ch.Type = "Innocent" Then
                    rightInnocents -= 1
                    If nextBank = "Left" Then leftInnocents += 1 Else rightInnocents += 1
                Else
                    rightMonsters -= 1
                    If nextBank = "Left" Then leftMonsters += 1 Else rightMonsters += 1
                End If
            End If
        Next

        Return IsBankSafe(leftInnocents, leftMonsters) AndAlso IsBankSafe(rightInnocents, rightMonsters)
    End Function

    Private Shared Function IsBankSafe(innocents As Integer, monsters As Integer) As Boolean
        If innocents = 0 Then Return True
        Return monsters <= innocents
    End Function

    Private Function CheckWinCondition() As Boolean
        Return characters.All(Function(c) c.Side = "Left")
    End Function

    ' ==================================================
    ' BUTTONS / NAVIGATION
    ' ==================================================
    Private Sub ResetButton_Click(sender As Object, e As EventArgs)
        InitializeGame()
    End Sub

    Private Sub BackButton_Click(sender As Object, e As EventArgs)
        If crossTimer IsNot Nothing Then
            crossTimer.Stop()
            crossTimer.Dispose()
            crossTimer = Nothing
        End If
        If movementTimer IsNot Nothing Then
            movementTimer.Stop()
            movementTimer.Dispose()
            movementTimer = Nothing
        End If
        Me.Close()
    End Sub

    Private Sub Level1GameplayForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        gamePanel.StopAnimation()
        If crossTimer IsNot Nothing Then
            crossTimer.Stop()
            crossTimer.Dispose()
            crossTimer = Nothing
        End If
        If movementTimer IsNot Nothing Then
            movementTimer.Stop()
            movementTimer.Dispose()
            movementTimer = Nothing
        End If
    End Sub

    Private Sub SetStatus(message As String)
        statusLabel.Text = message
    End Sub

    Private Sub UpdateMoveCount()
        moveCountLabel.Text = "Moves: " & moveCount
    End Sub

End Class

''' <summary>Represents one puzzle character. Logic-only -- no drawing here.</summary>
Public Class CharacterState
    Public Property Name As String
    Public Property Type As String
    Public Property Side As String
    Public Property OnBoat As Boolean
    Public Property HomeLocation As Point ' Stores original position on land

    Public Sub New(nameValue As String, typeValue As String, sideValue As String)
        Name = nameValue
        Type = typeValue
        Side = sideValue
        OnBoat = False
        HomeLocation = Point.Empty
    End Sub
End Class

''' <summary>
''' Gameplay background: sky, two tiled grass/rock banks, and a tiled, vertically
''' scrolling river band in between. Exposes the computed bank/river rectangles so
''' the form can position character sprites consistently with what's drawn.
''' </summary>
Public Class GameScenePanel
    Inherits Panel

    Private waterTimer As Timer
    Private waveOffset As Integer = 0

    Private skyTile As Image
    Private bankTile As Image
    Private waterTile As Image

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True

        skyTile = GameAssets.GetSheet("enviroment\sky.png")
        bankTile = GameAssets.GetSheet("tiles\Grass_rocks.png")
        waterTile = GameAssets.GetSheet("tiles\water-Sheet.png")

        waterTimer = New Timer() With {.Interval = 90}
        AddHandler waterTimer.Tick, Sub(s, e)
                                        waveOffset = (waveOffset + 2) Mod 64
                                        Me.Invalidate()
                                    End Sub
        waterTimer.Start()
    End Sub

    Public Sub StopAnimation()
        waterTimer.Stop()
    End Sub

    Private Sub ComputeGeometry(ByRef leftRect As Rectangle, ByRef riverRect As Rectangle, ByRef rightRect As Rectangle)
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        Dim riverLeft As Integer = CInt(w * 0.38)
        Dim riverWidth As Integer = CInt(w * 0.24)
        Dim top As Integer = CInt(h * 0.18)
        Dim bottom As Integer = h - 20
        Dim bandHeight As Integer = Math.Max(10, bottom - top)

        riverRect = New Rectangle(riverLeft, top, Math.Max(10, riverWidth), bandHeight)
        leftRect = New Rectangle(0, top, Math.Max(10, riverLeft), bandHeight)
        rightRect = New Rectangle(riverLeft + riverWidth, top, Math.Max(10, w - (riverLeft + riverWidth)), bandHeight)
    End Sub

    Public ReadOnly Property LeftBankRect As Rectangle
        Get
            Dim l As Rectangle, r As Rectangle, rt As Rectangle
            ComputeGeometry(l, r, rt)
            Return l
        End Get
    End Property

    Public ReadOnly Property RiverRect As Rectangle
        Get
            Dim l As Rectangle, r As Rectangle, rt As Rectangle
            ComputeGeometry(l, r, rt)
            Return r
        End Get
    End Property

    Public ReadOnly Property RightBankRect As Rectangle
        Get
            Dim l As Rectangle, r As Rectangle, rt As Rectangle
            ComputeGeometry(l, r, rt)
            Return rt
        End Get
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        Dim w As Integer = Me.Width
        Dim h As Integer = Me.Height
        If w <= 0 OrElse h <= 0 Then Return

        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor

        If skyTile IsNot Nothing Then
            TileImage(g, skyTile, New Rectangle(0, 0, w, h))
        Else
            Using b As New SolidBrush(Color.FromArgb(150, 200, 235))
                g.FillRectangle(b, 0, 0, w, h)
            End Using
        End If

        Dim leftRect As Rectangle, riverRect As Rectangle, rightRect As Rectangle
        ComputeGeometry(leftRect, riverRect, rightRect)

        If bankTile IsNot Nothing Then
            TileImage(g, bankTile, leftRect)
            TileImage(g, bankTile, rightRect)
        Else
            Using b As New SolidBrush(Color.FromArgb(120, 190, 110))
                g.FillRectangle(b, leftRect)
                g.FillRectangle(b, rightRect)
            End Using
        End If

        If waterTile IsNot Nothing Then
            TileImageScrollingVertical(g, waterTile, riverRect, waveOffset)
        Else
            Using b As New SolidBrush(Color.FromArgb(70, 140, 200))
                g.FillRectangle(b, riverRect)
            End Using
        End If

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

    Private Sub TileImageScrollingVertical(g As Graphics, img As Image, area As Rectangle, offsetY As Integer)
        If area.Width <= 0 OrElse area.Height <= 0 OrElse img.Height <= 0 Then Return
        Dim oldClip = g.Clip
        g.SetClip(area)
        Dim startY As Integer = area.Top - (offsetY Mod img.Height)
        Dim y As Integer = startY
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

End Class
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
    Private isAnimating As Boolean = False

    Private characterIcons As New Dictionary(Of CharacterState, SpritePanel)
    Private boatIcon As SpritePanel

    Private crossTimer As Timer
    Private crossStartX As Integer
    Private crossEndX As Integer
    Private crossStep As Integer
    Private Const CrossSteps As Integer = 24
    Private crossingPassengers As New List(Of CharacterState)
    Private crossingToBank As String = ""

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

        Dim bottomPanel As New Panel() With {.Dock = DockStyle.Bottom, .Height = 64, .BackColor = Color.FromArgb(245, 245, 245)}
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
            .Location = New Point(16, 30)
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
            .Location = New Point(166, 30)
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

        AddHandler bottomPanel.Resize, Sub(s, e) backButton.Location = New Point(bottomPanel.Width - backButton.Width - 16, 14)
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
        isAnimating = False

        ' Remove old sprite controls so a Reset starts completely clean.
        If characterIcons.Count > 0 Then
            For Each kv In characterIcons
                gamePanel.Controls.Remove(kv.Value)
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
                    icon.SpriteImage = GameAssets.GetFrame("Character\charater-Sheet.png", 0, 0, GameAssets.CharFrameW, GameAssets.CharFrameH)
                    icon.AccentColor = Color.FromArgb(90, 160, 255)
                Else
                    icon.SpriteImage = GameAssets.GetFrame("enemies\ground_enemy-Sheet.png", 0, 0, GameAssets.EnemyFrameW, GameAssets.EnemyFrameH)
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
    ' RENDERING (positions only -- visuals always reflect game state)
    ' ==================================================
    Private Sub RefreshCharacterLayout()
        If isAnimating Then Return
        If gamePanel.Width <= 0 OrElse gamePanel.Height <= 0 Then Return

        Dim leftRect As Rectangle = gamePanel.LeftBankRect
        Dim rightRect As Rectangle = gamePanel.RightBankRect
        Dim riverRect As Rectangle = gamePanel.RiverRect

        Dim iconSize As Integer = 48
        Dim perRow As Integer = Math.Max(1, (leftRect.Width - 16) \ (iconSize + 10))

        Dim boatW As Integer = 90
        Dim boatH As Integer = CInt(boatW * (GameAssets.BoatCellHeight / CSng(GameAssets.BoatCellWidth)))
        Dim boatX As Integer = If(currentBank = "Left", leftRect.Right - boatW - 10, rightRect.Left + 10)
        Dim boatY As Integer = riverRect.Top + Math.Max(0, (riverRect.Height - boatH) \ 2)
        boatIcon.Size = New Size(boatW, boatH)
        boatIcon.Location = New Point(boatX, boatY)
        boatIcon.BringToFront()

        Dim leftIndex As Integer = 0
        Dim rightIndex As Integer = 0
        Dim boatPassengerIndex As Integer = 0

        For Each ch In characters
            Dim icon As SpritePanel = characterIcons(ch)
            icon.IsSelected = selectedCharacters.Contains(ch)

            If ch.OnBoat Then
                icon.Size = New Size(34, 34)
                icon.Location = New Point(boatX + 8 + boatPassengerIndex * 38, boatY + boatH - 40)
                boatPassengerIndex += 1
                icon.BringToFront()
            ElseIf ch.Side = "Left" Then
                Dim col As Integer = leftIndex Mod perRow
                Dim row As Integer = leftIndex \ perRow
                icon.Size = New Size(iconSize, iconSize)
                icon.Location = New Point(leftRect.Left + 16 + col * (iconSize + 10), leftRect.Top + 16 + row * (iconSize + 10))
                leftIndex += 1
            Else
                Dim col As Integer = rightIndex Mod perRow
                Dim row As Integer = rightIndex \ perRow
                icon.Size = New Size(iconSize, iconSize)
                icon.Location = New Point(rightRect.Left + 16 + col * (iconSize + 10), rightRect.Top + 16 + row * (iconSize + 10))
                rightIndex += 1
            End If
        Next
    End Sub

    ' ==================================================
    ' SELECTION
    ' ==================================================
    Private Sub CharacterIcon_Click(ch As CharacterState)
        If isAnimating OrElse hasWon Then Return
        If ch.OnBoat Then Return
        If ch.Side <> currentBank Then Return

        If selectedCharacters.Contains(ch) Then
            selectedCharacters.Remove(ch)
            SetStatus("Passenger deselected.")
        Else
            If selectedCharacters.Count >= 2 Then
                SetStatus("The boat can only carry up to 2 passengers.")
                Return
            End If
            selectedCharacters.Add(ch)
            SetStatus("Passenger selected. Select up to 2, then click Cross River.")
        End If

        RefreshCharacterLayout()
    End Sub

    ' ==================================================
    ' MOVE VALIDATION (classic rule, unchanged)
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
    ' CROSSING (timer-based animation, not instant teleport)
    ' ==================================================
    Private Sub CrossButton_Click(sender As Object, e As EventArgs)
        If isAnimating OrElse hasWon Then Return

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
        isAnimating = True
        crossButton.Enabled = False

        crossingPassengers = New List(Of CharacterState)(selectedCharacters)
        crossingToBank = If(currentBank = "Right", "Left", "Right")

        For Each ch In crossingPassengers
            ch.OnBoat = True
        Next

        Dim leftRect As Rectangle = gamePanel.LeftBankRect
        Dim rightRect As Rectangle = gamePanel.RightBankRect
        crossStartX = boatIcon.Location.X
        crossEndX = If(crossingToBank = "Left", leftRect.Right - boatIcon.Width - 10, rightRect.Left + 10)
        crossStep = 0

        AudioManager.PlaySfx("Audio\SFX\Canoe_Paddle_Sound_Effect.mp3")
        SetStatus("Crossing the river...")

        crossTimer = New Timer() With {.Interval = 30}
        AddHandler crossTimer.Tick, AddressOf CrossTimer_Tick
        crossTimer.Start()
    End Sub

    Private Sub CrossTimer_Tick(sender As Object, e As EventArgs)
        crossStep += 1
        Dim progress As Single = crossStep / CSng(CrossSteps)
        Dim newX As Integer = CInt(crossStartX + (crossEndX - crossStartX) * progress)
        boatIcon.Location = New Point(newX, boatIcon.Location.Y)

        Dim passengerIndex As Integer = 0
        For Each ch In crossingPassengers
            Dim icon As SpritePanel = characterIcons(ch)
            icon.Location = New Point(newX + 8 + passengerIndex * 38, boatIcon.Location.Y + boatIcon.Height - 40)
            passengerIndex += 1
        Next

        If crossStep >= CrossSteps Then
            crossTimer.Stop()
            crossTimer.Dispose()
            crossTimer = Nothing
            CompleteCrossing()
        End If
    End Sub

    Private Sub CompleteCrossing()
        For Each ch In crossingPassengers
            ch.OnBoat = False
            ch.Side = crossingToBank
        Next
        currentBank = crossingToBank
        moveCount += 1
        selectedCharacters.Clear()
        crossingPassengers.Clear()
        UpdateMoveCount()
        isAnimating = False
        crossButton.Enabled = True

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
    End Sub

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
        Me.Close()
    End Sub

    Private Sub Level1GameplayForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        gamePanel.StopAnimation()
        If crossTimer IsNot Nothing Then
            crossTimer.Stop()
            crossTimer.Dispose()
            crossTimer = Nothing
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

    Public Sub New(nameValue As String, typeValue As String, sideValue As String)
        Name = nameValue
        Type = typeValue
        Side = sideValue
        OnBoat = False
    End Sub
End Class

''' <summary>A single sprite icon: draws SpriteImage with nearest-neighbor scaling and an optional selection border.</summary>
Public Class SpritePanel
    Inherits Panel

    Public Property SpriteImage As Image
    Public Property IsSelected As Boolean = False
    Public Property AccentColor As Color = Color.FromArgb(255, 215, 0)

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.BackColor = Color.Transparent
        Me.Cursor = Cursors.Hand
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

        If SpriteImage IsNot Nothing Then
            g.DrawImage(SpriteImage, New Rectangle(2, 2, Width - 4, Height - 4))
        Else
            Using b As New SolidBrush(Color.Gray)
                g.FillRectangle(b, 2, 2, Width - 4, Height - 4)
            End Using
        End If

        If IsSelected Then
            Using pen As New Pen(AccentColor, 3)
                g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3)
            End Using
        End If

        MyBase.OnPaint(e)
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
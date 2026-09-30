Public Class Level1GameplayForm
    Inherits Form

    Private leftBank As Panel
    Private rightBank As Panel
    Private boatPanel As Panel
    Private statusLabel As Label
    Private moveCountLabel As Label
    Private attemptLabel As Label
    Private goalLabel As Label
    Private moveButton As Button
    Private resetButton As Button

    Private currentBank As String = "Right"
    Private moveCount As Integer = 0
    Private attemptCount As Integer = 1
    Private characters As New List(Of CharacterState)
    Private selectedCharacters As New List(Of CharacterState)

    Public Sub New()
        Me.Text = "Level 1 — River Crossing"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(240, 240, 240)

        BuildLayout()
        InitializeGame()
    End Sub

    Private Sub BuildLayout()
        ' Top panel
        Dim topPanel As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 90,
            .BackColor = Color.FromArgb(245, 245, 245)
        }
        Me.Controls.Add(topPanel)

        goalLabel = New Label() With {
            .Text = "Goal: move everyone to the left bank",
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(20, 150, 80),
            .AutoSize = True,
            .Location = New Point(20, 16)
        }
        topPanel.Controls.Add(goalLabel)

        ' Game area
        Dim gameArea As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(240, 240, 240)
        }
        Me.Controls.Add(gameArea)

        leftBank = New Panel() With {
            .Width = 420,
            .Height = 420,
            .BackColor = Color.FromArgb(147, 199, 99),
            .Location = New Point(20, 100)
        }
        gameArea.Controls.Add(leftBank)

        rightBank = New Panel() With {
            .Width = 420,
            .Height = 420,
            .BackColor = Color.FromArgb(147, 199, 99),
            .Location = New Point(760, 100)
        }
        gameArea.Controls.Add(rightBank)

        Dim river As New Panel() With {
            .Width = 335,
            .Height = 420,
            .BackColor = Color.FromArgb(86, 163, 214),
            .Location = New Point(425, 90)
        }
        gameArea.Controls.Add(river)

        boatPanel = New Panel() With {
            .Width = 150,
            .Height = 90,
            .BackColor = Color.FromArgb(163, 111, 52),
            .Location = New Point(410, 250)
        }
        gameArea.Controls.Add(boatPanel)

        ' Status panel (bottom)
        Dim statusPanel As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 120,
            .BackColor = Color.FromArgb(250, 250, 250)
        }
        Me.Controls.Add(statusPanel)

        statusLabel = New Label() With {
            .Text = "Select a character to board the boat.",
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .AutoSize = True,
            .Location = New Point(20, 20)
        }
        statusPanel.Controls.Add(statusLabel)

        moveCountLabel = New Label() With {
            .Text = "Moves: 0",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 40, 40),
            .AutoSize = True,
            .Location = New Point(20, 52)
        }
        statusPanel.Controls.Add(moveCountLabel)

        attemptLabel = New Label() With {
            .Text = "Attempt: 1",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 40, 40),
            .AutoSize = True,
            .Location = New Point(20, 80)
        }
        statusPanel.Controls.Add(attemptLabel)

        moveButton = New Button() With {
            .Text = "Move Boat",
            .BackColor = Color.FromArgb(40, 40, 40),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Size = New Size(140, 36),
            .Location = New Point(Me.Width - 260, 40)
        }
        moveButton.FlatAppearance.BorderSize = 0
        AddHandler moveButton.Click, AddressOf MoveBoatButton_Click
        statusPanel.Controls.Add(moveButton)

        resetButton = New Button() With {
            .Text = "Reset",
            .BackColor = Color.FromArgb(200, 200, 200),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Size = New Size(120, 36),
            .Location = New Point(Me.Width - 110, 40)
        }
        resetButton.FlatAppearance.BorderSize = 0
        AddHandler resetButton.Click, AddressOf ResetGameButton_Click
        statusPanel.Controls.Add(resetButton)
    End Sub

    Private Sub InitializeGame()
        characters = New List(Of CharacterState)
        characters.Add(New CharacterState("I1", "Innocent", "Right"))
        characters.Add(New CharacterState("I2", "Innocent", "Right"))
        characters.Add(New CharacterState("I3", "Innocent", "Right"))
        characters.Add(New CharacterState("M1", "Monster", "Right"))
        characters.Add(New CharacterState("M2", "Monster", "Right"))
        characters.Add(New CharacterState("M3", "Monster", "Right"))

        selectedCharacters.Clear()
        currentBank = "Right"
        moveCount = 0
        attemptCount = 1

        RefreshCharacterLayout()
        UpdateStatus("Select a character to board the boat.")
        UpdateCounters()
    End Sub

    Private Sub RefreshCharacterLayout()
        leftBank.Controls.Clear()
        rightBank.Controls.Clear()
        boatPanel.Controls.Clear()

        Dim leftY As Integer = 20
        Dim rightY As Integer = 20
        Dim xLeft As Integer = 20
        Dim xRight As Integer = 20

        For Each ch In characters
            Dim panel As New Panel() With {
                .Size = New Size(46, 46),
                .Tag = ch
            }

            Dim label As New Label() With {
                .Text = If(ch.Type = "Innocent", "I", "M"),
                .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Dock = DockStyle.Fill
            }

            panel.BackColor = If(ch.Type = "Innocent", Color.FromArgb(87, 93, 255), Color.FromArgb(170, 65, 45))
            panel.Controls.Add(label)

            If ch.OnBoat Then
                panel.Size = New Size(42, 42)
                panel.BackColor = If(ch.Type = "Innocent", Color.FromArgb(92, 120, 255), Color.FromArgb(200, 90, 50))
                panel.Location = New Point(10 + 50 * GetBoatPassengers().IndexOf(ch), 25)
                boatPanel.Controls.Add(panel)
            ElseIf ch.Side = "Left" Then
                panel.Location = New Point(xLeft, leftY)
                leftBank.Controls.Add(panel)
                xLeft += 54
                If xLeft > leftBank.Width - 60 Then
                    xLeft = 20
                    leftY += 56
                End If
            Else
                panel.Location = New Point(xRight, rightY)
                rightBank.Controls.Add(panel)
                xRight += 54
                If xRight > rightBank.Width - 60 Then
                    xRight = 20
                    rightY += 56
                End If
            End If

            AddHandler panel.Click, Sub(s, e)
                                        If ch.OnBoat Then Return
                                        If ch.Side <> currentBank Then Return
                                        ToggleCharacterSelection(ch)
                                    End Sub

            If selectedCharacters.Contains(ch) Then
                panel.BorderStyle = BorderStyle.FixedSingle
                panel.BackColor = If(ch.Type = "Innocent", Color.FromArgb(130, 140, 255), Color.FromArgb(230, 120, 80))
            End If
        Next

        boatPanel.Location = New Point(GetBoatXPosition(), 250)
        boatPanel.BackColor = If(currentBank = "Left", Color.FromArgb(154, 94, 42), Color.FromArgb(163, 111, 52))
    End Sub

    Private Function GetBoatPassengers() As List(Of CharacterState)
        Return characters.Where(Function(c) c.OnBoat).ToList()
    End Function

    Private Function GetBoatXPosition() As Integer
        Return If(currentBank = "Left", 410, 740)
    End Function

    Private Sub ToggleCharacterSelection(ch As CharacterState)
        If selectedCharacters.Contains(ch) Then
            selectedCharacters.Remove(ch)
            UpdateStatus("Character deselected.")
        Else
            If selectedCharacters.Count >= 2 Then
                UpdateStatus("The boat can carry up to 2 characters only.")
                Return
            End If
            selectedCharacters.Add(ch)
            UpdateStatus("Character selected. Select 1 or 2 characters, then move the boat.")
        End If
        RefreshCharacterLayout()
    End Sub

    Private Sub MoveBoatButton_Click(sender As Object, e As EventArgs)
        If selectedCharacters.Count = 0 Then
            UpdateStatus("Select at least one character before moving the boat.")
            Return
        End If

        If Not IsLegalMove() Then
            attemptCount += 1
            UpdateCounters()
            UpdateStatus("Invalid move. The rules do not allow that distribution.")
            Return
        End If

        Dim nextBank As String = If(currentBank = "Right", "Left", "Right")

        For Each ch In selectedCharacters
            ch.OnBoat = True
            ch.Side = currentBank
        Next

        currentBank = nextBank

        For Each ch In selectedCharacters
            ch.OnBoat = False
            ch.Side = nextBank
        Next

        moveCount += 1
        selectedCharacters.Clear()
        UpdateCounters()

        If HasWon() Then
            UpdateStatus("Victory! Everyone reached the left bank.")
            moveButton.Enabled = False
            Return
        End If

        UpdateStatus("Valid move completed. Select the next passengers.")
        RefreshCharacterLayout()
    End Sub

    Private Function IsLegalMove() As Boolean
        For Each ch In selectedCharacters
            If ch.Side <> currentBank Then
                Return False
            End If
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

    Private Function HasWon() As Boolean
        Return characters.All(Function(c) c.Side = "Left")
    End Function

    Private Sub ResetGameButton_Click(sender As Object, e As EventArgs)
        InitializeGame()
        moveButton.Enabled = True
        UpdateStatus("Game reset. Select a character to board the boat.")
    End Sub

    Private Sub UpdateStatus(message As String)
        statusLabel.Text = message
    End Sub

    Private Sub UpdateCounters()
        moveCountLabel.Text = "Moves: " & moveCount
        attemptLabel.Text = "Attempt: " & attemptCount
    End Sub
End Class

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
' =====================================================================================
'  JumpKnightEngine.vb
'  All gameplay rules for Level 2 (Jump Knight). This file contains NO drawing code.
'
'  Coordinate system ("world coordinates"):
'    * X goes right, from 0 to WorldW (400).
'    * Y goes UP. The knight starts near y = 80 and climbs to bigger numbers.
'    * CamBottom is the world-Y shown at the bottom edge of the screen. It only ever increases.
'    * The renderer converts world -> screen, so physics never depends on window size.
' =====================================================================================

Public Enum JKState
    Ready       ' knight waits on the start platform until the first key press
    Playing
    GameOver
End Enum

Public Enum JKPlatformKind
    Stone       ' normal, permanent
    Moving      ' slides left/right between two limits
    Wood        ' bounces you once, then breaks
    Ice         ' bounces you, then you slide (low grip) until the next normal landing
End Enum

Public Enum JKPowerUpKind
    Hammer      ' spring buff: one big launch
    Meat        ' 5 seconds of double jump
End Enum

Public Class JKPlatform
    Public Kind As JKPlatformKind
    Public X As Single              ' left edge
    Public Y As Single              ' top surface (the knight lands on this height)
    Public Width As Single
    Public Dir As Single = 1.0F     ' moving platforms: +1 right, -1 left
    Public Speed As Single
    Public MinX As Single           ' moving platforms: left limit
    Public MaxX As Single           ' moving platforms: right limit
    Public LastDX As Single         ' how far it moved this step (used to carry the knight)
    Public Solid As Boolean = True  ' false once a wooden platform has been used
    Public BreakTime As Single = -1.0F   ' < 0 = intact, otherwise seconds since it started breaking

    Public ReadOnly Property IsBreaking As Boolean
        Get
            Return BreakTime >= 0.0F
        End Get
    End Property
End Class

Public Class JKPowerUp
    Public Kind As JKPowerUpKind
    Public X As Single              ' centre X
    Public Y As Single              ' bottom Y (sits on the platform)
    Public Width As Single
    Public Height As Single
End Class

Public Class JKEnemy
    Public X As Single              ' centre X
    Public Y As Single              ' centre Y
    Public VX As Single
    Public Phase As Single          ' offsets the wing animation so bats do not flap in sync
End Class

Public Class JumpKnightEngine

    ' ---------- Fixed logical play area ----------
    Public Const WorldW As Single = 400.0F
    Public Const ViewH As Single = 600.0F

    ' ---------- Knight size (hitbox, world units) ----------
    Public Const KnightW As Single = 26.0F
    Public Const KnightH As Single = 40.0F

    ' ---------- Tuning values (units per second) ----------
    Private Const Gravity As Single = 1500.0F
    Private Const JumpVel As Single = 700.0F          ' rises v^2 / 2g = ~163 units
    Private Const SpringVel As Single = 1150.0F       ' rises ~441 units (about 2.7x a normal jump)
    Private Const DoubleJumpVel As Single = 700.0F
    Private Const MoveSpeed As Single = 260.0F
    Private Const GroundAccel As Single = 2600.0F     ' snappy steering
    Private Const IceAccel As Single = 520.0F         ' sluggish steering while slippery
    Private Const IceFriction As Single = 140.0F      ' almost no braking while slippery
    Public Const BuffDuration As Single = 5.0F        ' meat: seconds of double jump

    ' ---------- Platform generation ----------
    Public Const StoneWidth As Single = 64.0F
    Public Const WoodWidth As Single = 72.0F
    Private Const MinGap As Single = 62.0F
    Private Const MaxGapStart As Single = 100.0F      ' easy early game
    Private Const MaxGapEnd As Single = 138.0F        ' always below the 163 jump height
    Private Const StartPlatformY As Single = 80.0F
    Private Const DifficultyRange As Single = 9000.0F ' world units until difficulty is at maximum

    ' ---------- Public state read by the renderer ----------
    Public ReadOnly Platforms As New List(Of JKPlatform)
    Public ReadOnly Enemies As New List(Of JKEnemy)
    Public ReadOnly PowerUps As New List(Of JKPowerUp)

    Public State As JKState = JKState.Ready
    Public KnightX As Single            ' centre X
    Public KnightFeetY As Single
    Public VelX As Single
    Public VelY As Single
    Public CamBottom As Single
    Public FacingLeft As Boolean
    Public Steering As Boolean
    Public Slippery As Boolean
    Public BounceTimer As Single        ' short timer after a bounce (used for the landing animation)
    Public BuffTime As Single           ' seconds of double jump left
    Public SpringFlash As Single        ' seconds left to show "SUPER JUMP!"
    Public AnimClock As Single          ' keeps running for animations
    Public Score As Integer
    Public BestScore As Integer
    Public NewBest As Boolean
    Public DiedByEnemy As Boolean
    Public WrapScreen As Boolean = True ' false = walls stop the knight instead

    ' ---------- Events (the form turns these into sound effects) ----------
    Public Event Bounced(kind As JKPlatformKind)
    Public Event SpringUsed()
    Public Event MeatCollected()
    Public Event DoubleJumped()
    Public Event WoodBroke()
    Public Event GameEnded()

    ' ---------- Private state ----------
    Private ReadOnly rng As New Random()
    Private startFeetY As Single
    Private maxHeight As Single
    Private topY As Single              ' Y of the highest generated platform
    Private lastX As Single             ' left X of the last main platform
    Private lastKind As JKPlatformKind
    Private nextEnemyY As Single
    Private doubleJumpAvailable As Boolean
    Private jumpQueued As Boolean

    Public Sub New()
        BestScore = JumpKnightScores.LoadBest()
        Reset()
    End Sub

    ' ===================== Run control =====================

    ''' <summary>Starts a completely fresh run. Safe to call repeatedly (Retry).</summary>
    Public Sub Reset()
        Platforms.Clear()
        Enemies.Clear()
        PowerUps.Clear()

        State = JKState.Ready
        KnightX = WorldW / 2.0F
        KnightFeetY = StartPlatformY
        startFeetY = StartPlatformY
        VelX = 0.0F
        VelY = 0.0F
        CamBottom = 0.0F
        FacingLeft = False
        Steering = False
        Slippery = False
        BounceTimer = 0.0F
        BuffTime = 0.0F
        SpringFlash = 0.0F
        Score = 0
        NewBest = False
        DiedByEnemy = False
        maxHeight = 0.0F
        doubleJumpAvailable = False
        jumpQueued = False

        ' Start platform right under the knight.
        Dim startX As Single = WorldW / 2.0F - StoneWidth / 2.0F
        Platforms.Add(New JKPlatform With {.Kind = JKPlatformKind.Stone, .X = startX, .Y = StartPlatformY, .Width = StoneWidth})
        topY = StartPlatformY
        lastX = startX
        lastKind = JKPlatformKind.Stone
        nextEnemyY = 1300.0F

        EnsureGenerated()
    End Sub

    ''' <summary>Called on the first key press. Gives the knight its first bounce.</summary>
    Public Sub Begin()
        If State <> JKState.Ready Then Return
        State = JKState.Playing
        VelY = JumpVel
        BounceTimer = 0.12F
        doubleJumpAvailable = True
        RaiseEvent Bounced(JKPlatformKind.Stone)
    End Sub

    ''' <summary>Asks for a mid-air jump. Only works while the meat buff is active.</summary>
    Public Sub RequestDoubleJump()
        If State = JKState.Playing Then jumpQueued = True
    End Sub

    ' ===================== Main update (called with a FIXED dt) =====================

    Public Sub Update(dt As Single, moveDir As Integer)
        AnimClock += dt
        If State <> JKState.Playing Then Return

        If moveDir < 0 Then
            FacingLeft = True
        ElseIf moveDir > 0 Then
            FacingLeft = False
        End If
        Steering = (moveDir <> 0)

        BuffTime = Math.Max(0.0F, BuffTime - dt)
        BounceTimer = Math.Max(0.0F, BounceTimer - dt)
        SpringFlash = Math.Max(0.0F, SpringFlash - dt)

        ' --- Queued double jump (meat buff) ---
        If jumpQueued Then
            jumpQueued = False
            If BuffTime > 0.0F AndAlso doubleJumpAvailable AndAlso VelY < DoubleJumpVel * 0.9F Then
                VelY = DoubleJumpVel
                doubleJumpAvailable = False
                RaiseEvent DoubleJumped()
            End If
        End If

        ' --- Horizontal steering (normal = snappy, slippery = keeps momentum) ---
        Dim accel As Single = If(Slippery, IceAccel, GroundAccel)
        Dim friction As Single = If(Slippery, IceFriction, GroundAccel)
        If moveDir <> 0 Then
            VelX = MoveToward(VelX, moveDir * MoveSpeed, accel * dt)
        Else
            VelX = MoveToward(VelX, 0.0F, friction * dt)
        End If

        UpdatePlatforms(dt)
        UpdateEnemies(dt)

        ' --- Vertical movement ---
        Dim prevFeetY As Single = KnightFeetY
        VelY -= Gravity * dt
        KnightFeetY += VelY * dt
        KnightX += VelX * dt
        HandleHorizontalBounds()

        ' --- Landing: only while falling, only from above ---
        If VelY <= 0.0F Then CheckLanding(prevFeetY)

        CheckPowerUps()
        If State = JKState.Playing Then CheckEnemies()

        ' --- Camera only moves up ---
        Dim followLine As Single = CamBottom + ViewH * 0.55F
        If KnightFeetY > followLine Then CamBottom = KnightFeetY - ViewH * 0.55F

        ' --- Score = highest point reached ---
        Dim height As Single = KnightFeetY - startFeetY
        If height > maxHeight Then
            maxHeight = height
            Score = CInt(Math.Floor(maxHeight / 10.0F))
        End If

        EnsureGenerated()
        Cleanup()

        ' --- Fell below the screen ---
        If State = JKState.Playing AndAlso KnightFeetY + 10.0F < CamBottom Then EndGame(False)
    End Sub

    ' ===================== Movement helpers =====================

    Private Shared Function MoveToward(current As Single, target As Single, maxDelta As Single) As Single
        If Math.Abs(target - current) <= maxDelta Then Return target
        Return current + Math.Sign(target - current) * maxDelta
    End Function

    Private Sub HandleHorizontalBounds()
        Dim half As Single = KnightW / 2.0F
        If WrapScreen Then
            ' Fully off one edge -> reappear fully off the opposite edge.
            If KnightX + half < 0.0F Then
                KnightX += WorldW + KnightW
            ElseIf KnightX - half > WorldW Then
                KnightX -= WorldW + KnightW
            End If
        Else
            If KnightX < half Then
                KnightX = half
                VelX = 0.0F
            ElseIf KnightX > WorldW - half Then
                KnightX = WorldW - half
                VelX = 0.0F
            End If
        End If
    End Sub

    Private Sub UpdatePlatforms(dt As Single)
        For Each p As JKPlatform In Platforms
            If p.Kind = JKPlatformKind.Moving Then
                Dim oldX As Single = p.X
                p.X += p.Dir * p.Speed * dt
                If p.X <= p.MinX Then
                    p.X = p.MinX
                    p.Dir = 1.0F
                ElseIf p.X >= p.MaxX Then
                    p.X = p.MaxX
                    p.Dir = -1.0F
                End If
                p.LastDX = p.X - oldX
            End If
            If p.IsBreaking Then p.BreakTime += dt
        Next
    End Sub

    Private Sub UpdateEnemies(dt As Single)
        For Each en As JKEnemy In Enemies
            en.X += en.VX * dt
            If en.X < 20.0F Then
                en.X = 20.0F
                en.VX = Math.Abs(en.VX)
            ElseIf en.X > WorldW - 20.0F Then
                en.X = WorldW - 20.0F
                en.VX = -Math.Abs(en.VX)
            End If
        Next
    End Sub

    Private Sub CheckLanding(prevFeetY As Single)
        Dim left As Single = KnightX - KnightW / 2.0F
        Dim right As Single = KnightX + KnightW / 2.0F
        Dim best As JKPlatform = Nothing

        For Each p As JKPlatform In Platforms
            If Not p.Solid Then Continue For
            ' Feet were at/above the surface last step and are at/below it now = came from above.
            If prevFeetY >= p.Y - 1.0F AndAlso KnightFeetY <= p.Y Then
                If right > p.X + 6.0F AndAlso left < p.X + p.Width - 6.0F Then
                    If best Is Nothing OrElse p.Y > best.Y Then best = p
                End If
            End If
        Next

        If best Is Nothing Then Return

        KnightFeetY = best.Y
        VelY = JumpVel
        BounceTimer = 0.12F
        doubleJumpAvailable = True
        Slippery = (best.Kind = JKPlatformKind.Ice)   ' normal landings restore normal grip

        If best.Kind = JKPlatformKind.Moving Then KnightX += best.LastDX   ' ride along for this step
        If best.Kind = JKPlatformKind.Wood Then
            best.Solid = False          ' cannot be landed on again
            best.BreakTime = 0.0F
            RaiseEvent WoodBroke()
        End If
        RaiseEvent Bounced(best.Kind)
    End Sub

    Private Sub CheckPowerUps()
        Dim kl As Single = KnightX - KnightW / 2.0F
        Dim kr As Single = KnightX + KnightW / 2.0F
        Dim kb As Single = KnightFeetY
        Dim kt As Single = KnightFeetY + KnightH

        For i As Integer = PowerUps.Count - 1 To 0 Step -1
            Dim pu As JKPowerUp = PowerUps(i)
            Dim overlap As Boolean =
                kr > pu.X - pu.Width / 2.0F AndAlso kl < pu.X + pu.Width / 2.0F AndAlso
                kt > pu.Y AndAlso kb < pu.Y + pu.Height
            If Not overlap Then Continue For

            PowerUps.RemoveAt(i)        ' consumed: can never trigger twice
            If pu.Kind = JKPowerUpKind.Hammer Then
                VelY = Math.Max(VelY, SpringVel)
                doubleJumpAvailable = True
                SpringFlash = 0.9F
                BounceTimer = 0.12F
                RaiseEvent SpringUsed()
            Else
                BuffTime = BuffDuration           ' picking up another meat just refreshes the timer
                doubleJumpAvailable = True
                RaiseEvent MeatCollected()
            End If
        Next
    End Sub

    Private Sub CheckEnemies()
        ' Slightly forgiving hitboxes so deaths feel fair.
        Dim kl As Single = KnightX - KnightW / 2.0F + 3.0F
        Dim kr As Single = KnightX + KnightW / 2.0F - 3.0F
        Dim kb As Single = KnightFeetY + 3.0F
        Dim kt As Single = KnightFeetY + KnightH - 4.0F

        For Each en As JKEnemy In Enemies
            If kr > en.X - 12.0F AndAlso kl < en.X + 12.0F AndAlso kt > en.Y - 9.0F AndAlso kb < en.Y + 9.0F Then
                EndGame(True)
                Return
            End If
        Next
    End Sub

    Private Sub EndGame(byEnemy As Boolean)
        If State = JKState.GameOver Then Return
        State = JKState.GameOver
        DiedByEnemy = byEnemy
        If Score > BestScore Then
            BestScore = Score
            NewBest = True
            JumpKnightScores.SaveBest(BestScore)
        End If
        RaiseEvent GameEnded()
    End Sub

    ' ===================== Procedural generation =====================

    Private Function Difficulty(worldY As Single) As Single
        Return Math.Max(0.0F, Math.Min(1.0F, (worldY - StartPlatformY) / DifficultyRange))
    End Function

    Private Sub EnsureGenerated()
        ' Keep platforms generated well above the top of the screen.
        While topY < CamBottom + ViewH + 400.0F
            AddRow()
        End While
    End Sub

    Private Sub AddRow()
        Dim d As Single = Difficulty(topY)
        Dim maxGap As Single = MaxGapStart + (MaxGapEnd - MaxGapStart) * d
        Dim gap As Single = MinGap + CSng(rng.NextDouble()) * (maxGap - MinGap)
        topY += gap

        Dim kind As JKPlatformKind = PickKind(d, topY)
        Dim pw As Single = If(kind = JKPlatformKind.Wood, WoodWidth, StoneWidth)
        Dim x As Single = Math.Max(0.0F, Math.Min(WorldW - pw, lastX + rng.Next(-170, 171)))

        AddPlatform(kind, x, topY, pw, d)
        lastX = x
        lastKind = kind

        ' Sometimes a second (always stone) platform on the same row, far from the first.
        If topY > 300.0F AndAlso rng.NextDouble() < 0.25 Then
            Dim x2 As Single
            If x + pw / 2.0F < WorldW / 2.0F Then
                x2 = WorldW - StoneWidth - rng.Next(0, 60)
            Else
                x2 = rng.Next(0, 60)
            End If
            AddPlatform(JKPlatformKind.Stone, x2, topY, StoneWidth, d)
        End If

        ' Power-ups only sit on plain stone platforms (they never move or break away).
        If kind = JKPlatformKind.Stone AndAlso topY > 500.0F Then
            Dim roll As Double = rng.NextDouble()
            If roll < 0.07 Then
                PowerUps.Add(New JKPowerUp With {.Kind = JKPowerUpKind.Hammer, .X = x + pw / 2.0F, .Y = topY, .Width = 28.0F, .Height = 28.0F})
            ElseIf roll < 0.12 Then
                PowerUps.Add(New JKPowerUp With {.Kind = JKPowerUpKind.Meat, .X = x + pw / 2.0F, .Y = topY, .Width = 38.0F, .Height = 29.0F})
            End If
        End If

        ' Flying enemies: first one appears high up, then more often as difficulty rises.
        If topY >= nextEnemyY Then
            Dim speed As Single = 70.0F + 60.0F * d + CSng(rng.NextDouble()) * 30.0F
            Dim direction As Single = If(rng.Next(0, 2) = 0, -1.0F, 1.0F)
            Enemies.Add(New JKEnemy With {
                .X = rng.Next(40, CInt(WorldW) - 40),
                .Y = topY - gap / 2.0F,
                .VX = speed * direction,
                .Phase = CSng(rng.NextDouble()) * 2.0F})
            Dim interval As Single = 1300.0F - 650.0F * d
            nextEnemyY = topY + interval * (0.75F + 0.5F * CSng(rng.NextDouble()))
        End If
    End Sub

    Private Sub AddPlatform(kind As JKPlatformKind, x As Single, y As Single, pw As Single, d As Single)
        Dim p As New JKPlatform With {.Kind = kind, .X = x, .Y = y, .Width = pw}
        If kind = JKPlatformKind.Moving Then
            Dim range As Single = 60.0F + CSng(rng.NextDouble()) * 80.0F
            p.MinX = Math.Max(0.0F, x - range)
            p.MaxX = Math.Min(WorldW - pw, x + range)
            p.Speed = 45.0F + 55.0F * d + CSng(rng.NextDouble()) * 20.0F
            p.Dir = If(rng.Next(0, 2) = 0, -1.0F, 1.0F)
        End If
        Platforms.Add(p)
    End Sub

    Private Function PickKind(d As Single, worldY As Single) As JKPlatformKind
        ' Each special type unlocks at a height and becomes more common as d grows.
        Dim pWood As Double = If(worldY >= 400.0F, 0.06 + 0.2 * d, 0.0)
        Dim pIce As Double = If(worldY >= 800.0F, 0.05 + 0.15 * d, 0.0)
        Dim pMove As Double = If(worldY >= 1200.0F, 0.05 + 0.2 * d, 0.0)

        ' Never chain two breakables or two ice platforms in a row.
        If lastKind = JKPlatformKind.Wood Then pWood = 0.0
        If lastKind = JKPlatformKind.Ice Then pIce = 0.0

        Dim roll As Double = rng.NextDouble()
        If roll < pWood Then Return JKPlatformKind.Wood
        roll -= pWood
        If roll < pIce Then Return JKPlatformKind.Ice
        roll -= pIce
        If roll < pMove Then Return JKPlatformKind.Moving
        Return JKPlatformKind.Stone
    End Function

    Private Sub Cleanup()
        ' Remove everything that is safely below the screen so memory never grows.
        Dim limit As Single = CamBottom - 150.0F
        Platforms.RemoveAll(Function(p) p.Y < limit OrElse (p.IsBreaking AndAlso p.BreakTime > 1.0F))
        Enemies.RemoveAll(Function(en) en.Y < limit)
        PowerUps.RemoveAll(Function(pu) pu.Y < limit)
    End Sub

End Class
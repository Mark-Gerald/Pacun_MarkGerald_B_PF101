Option Strict On
Imports System
Imports System.Collections.Generic

' =====================================================================================
'  CANNON BALL - engine (game state + physics only; it never draws and never plays sound)
'  Coordinates: "play space" = 400 x 600, origin top-left of the playfield, Y grows DOWN.
'  The renderer adds a 40-unit HUD band above it (CannonBallEngine.HudH).
' =====================================================================================

Public Enum CBState
    Ready           ' cart can move, ball sits on the cart. (A short LEVEL title blocks launching first.)
    Locked          ' cart position locked, aim marker sweeps left/right, dotted preview is shown
    Playing         ' ball is flying
    BallLost        ' ball fell below the cart: short pause, then Ready again (or GameOver)
    LevelComplete   ' ball left through the top: everything frozen, effect plays, form fades out
    GameOver
    Victory
End Enum

Public Class CBBlock
    Public Kind As CBBlockType
    Public X As Single
    Public Y As Single
    Public Width As Single
    Public Height As Single
    Public HitsLeft As Integer
    Public MaxHits As Integer
    Public Alive As Boolean = True
    Public FlashTime As Single

    Public ReadOnly Property IsDamaged As Boolean
        Get
            Return HitsLeft < MaxHits
        End Get
    End Property
End Class

Public Class CBParticle
    Public X As Single
    Public Y As Single
    Public VX As Single
    Public VY As Single
    Public Age As Single
    Public Life As Single
    Public Size As Single
    Public Tint As CBBlockType
End Class

Public Class CBPopup
    Public X As Single
    Public Y As Single
    Public Age As Single
    Public Text As String = ""
    Public Tint As CBBlockType
End Class

Public Class CannonBallEngine

    ' ===================== Configuration (change numbers here, nowhere else) =====================

    Public Const DEBUG_MODE As Boolean = False         ' True = shows state / velocity / hitboxes

    ' ---- World ----
    Public Const WorldW As Single = 400.0F
    Public Const PlayH As Single = 600.0F
    Public Const HudH As Single = 40.0F
    Public Const ViewH As Single = PlayH + HudH
    Public Const WallLeft As Single = 16.0F            ' inner face of the left stone pillar
    Public Const WallRight As Single = WorldW - 16.0F  ' inner face of the right stone pillar

    ' ---- Time step: physics always advances in exact 1/120 s steps (frame-rate independent) ----
    Public Const FixedStep As Single = 1.0F / 120.0F

    ' ---- Ball ----
    Public Const BallRadius As Single = 5.0F
    Public Const MaxStepDistance As Single = 3.0F      ' ball never moves further than this per sub-step (no tunnelling)
    Public Const MinVerticalRatio As Single = 0.34F    ' |vy| is always at least this fraction of the speed
    Public Const MinHorizontalRatio As Single = 0.1F   ' |vx| is always at least this fraction (no endless vertical loops)
    Public Const MaxBounceAngle As Single = 1.0F       ' radians from vertical, at the very edge of the cart (~57 degrees)
    Public Const CollisionSkin As Single = 0.05F       ' extra push-out distance so a block is never hit twice in a row

    ' ---- Cart (paddle) ----
    Public Const PaddleW As Single = 60.0F
    Public Const PaddleH As Single = 16.0F
    Public Const PaddleY As Single = 540.0F            ' top surface of the cart
    Public Const PaddleSpeed As Single = 340.0F
    Public Const PaddleTopTolerance As Single = 4.0F   ' ball centre above (top + this) = top-face hit, otherwise side hit

    ' ---- Rules ----
    Public Const StartLives As Integer = 3
    Public Const MaxLives As Integer = 5
    Public Const PointsBrick As Integer = 10
    Public Const PointsStone As Integer = 20
    Public Const PointsGold As Integer = 50
    Public Const PointsEmerald As Integer = 25

    ' ---- Timing (seconds) ----
    Public Const IntroSeconds As Single = 1.4F         ' "LEVEL X" title at the start of each level
    Public Const BallLostDelay As Single = 0.9F
    Public Const LevelCompleteHold As Single = 1.1F    ' effect time before the fade-out starts
    Public Const FadeOutSeconds As Single = 0.4F
    Public Const FadeInSeconds As Single = 0.45F

    ' ---- Aiming / visuals ----
    Public Const AimMaxAngle As Single = 0.85F
    Public Const AimSweepSpeed As Single = 2.4F
    Public Const PreviewDots As Integer = 7
    Public Const PreviewSpacing As Single = 18.0F
    Public Const PreviewStep As Single = 4.0F
    Public Const TrailLength As Integer = 10
    Private Const ParticleGravity As Single = 520.0F
    Private Const MaxParticles As Integer = 300

    ' ===================== Public state (read by the renderer) =====================

    Public State As CBState = CBState.Ready
    Public StateTime As Single
    Public Level As Integer = 1
    Public LevelName As String = ""
    Public Lives As Integer = StartLives
    Public Score As Integer
    Public ElapsedTime As Double
    Public BestScore As Integer
    Public BestTime As Double
    Public NewBestScore As Boolean
    Public NewBestTime As Boolean

    Public CartX As Single
    Public BallX As Single
    Public BallY As Single
    Public VelX As Single
    Public VelY As Single
    Public BallSpeed As Single

    Public AimAngle As Single
    Public IntroTime As Single
    Public AnimClock As Single
    Public LifeFlash As Single
    Public LifeLostFlash As Single
    Public ShakeTime As Single

    Public ReadOnly Blocks As New List(Of CBBlock)
    Public ReadOnly Particles As New List(Of CBParticle)
    Public ReadOnly Popups As New List(Of CBPopup)

    Public ReadOnly PreviewX(PreviewDots - 1) As Single
    Public ReadOnly PreviewY(PreviewDots - 1) As Single
    Public PreviewCount As Integer
    Public ReadOnly TrailX(TrailLength - 1) As Single
    Public ReadOnly TrailY(TrailLength - 1) As Single
    Public TrailCount As Integer

    ' ===================== Events (the form turns these into sound) =====================

    Public Event CartLocked()
    Public Event Launched()
    Public Event WallBounced()
    Public Event CartBounced()
    Public Event BlockDamaged(kind As CBBlockType)
    Public Event BlockDestroyed(kind As CBBlockType)
    Public Event LifeGained()
    Public Event LifeLost()
    Public Event LevelCompleted(levelNumber As Integer)
    Public Event LevelClearFinished()       ' effect time is over: the form should fade out now
    Public Event GameEnded()
    Public Event Won()

    ' ===================== Private state =====================

    Private ReadOnly rng As New Random()
    Private timing As Boolean
    Private clearAnnounced As Boolean
    Private aimClock As Single
    Private trailTick As Integer

    Public Sub New()
        CannonBallScores.Load(BestScore, BestTime)
        NewGame()
    End Sub

    ' ===================== Run control =====================

    ''' <summary>Fresh run: score 0, timer 0, 3 lives, level 1, cart in the middle.</summary>
    Public Sub NewGame()
        Level = 1
        Lives = StartLives
        Score = 0
        ElapsedTime = 0.0
        NewBestScore = False
        NewBestTime = False
        LifeFlash = 0.0F
        LifeLostFlash = 0.0F
        ShakeTime = 0.0F
        CartX = WorldW / 2.0F
        timing = True
        Particles.Clear()
        Popups.Clear()
        LoadLevel(Level)
        PrepareBall(True)
    End Sub

    Public ReadOnly Property CanPause As Boolean
        Get
            Return State = CBState.Ready OrElse State = CBState.Locked OrElse
                   State = CBState.Playing OrElse State = CBState.BallLost
        End Get
    End Property

    ''' <summary>SPACE: first press locks the cart, second press fires.</summary>
    Public Sub PressAction()
        Select Case State
            Case CBState.Ready
                If IntroTime > 0.0F Then Return            ' still showing the LEVEL title
                State = CBState.Locked
                StateTime = 0.0F
                aimClock = 0.0F
                AimAngle = 0.0F
                BuildPreview()
                RaiseEvent CartLocked()
            Case CBState.Locked
                Launch()
        End Select
    End Sub

    ''' <summary>Called by the form at the middle of the fade-out: loads the next level, or ends the run.</summary>
    Public Sub AdvanceLevel()
        If State <> CBState.LevelComplete Then Return
        If Level >= CannonBallLevelData.LevelCount Then
            State = CBState.Victory
            StateTime = 0.0F
            CommitBest(True)
            RaiseEvent Won()
        Else
            Level += 1
            LoadLevel(Level)
            PrepareBall(True)          ' score, lives and cart position are kept
        End If
    End Sub

    Private Sub LoadLevel(n As Integer)
        Dim data As CannonBallLevelData = CannonBallLevelData.Create(n)
        Blocks.Clear()
        For Each d As CannonBallBlockData In data.Blocks
            Blocks.Add(New CBBlock With {
                .Kind = d.Kind, .X = d.X, .Y = d.Y, .Width = d.Width, .Height = d.Height,
                .HitsLeft = d.HitPoints, .MaxHits = d.HitPoints})
        Next
        BallSpeed = data.BallSpeed
        LevelName = data.Name
        Particles.Clear()
        Popups.Clear()
    End Sub

    Private Sub PrepareBall(withIntro As Boolean)
        State = CBState.Ready
        StateTime = 0.0F
        IntroTime = If(withIntro, IntroSeconds, 0.0F)
        VelX = 0.0F
        VelY = 0.0F
        TrailCount = 0
        PreviewCount = 0
        CartX = ClampCart(CartX)
        SyncBallToCart()
    End Sub

    Private Sub SyncBallToCart()
        BallX = CartX
        BallY = PaddleY - BallRadius
    End Sub

    Private Sub Launch()
        State = CBState.Playing
        StateTime = 0.0F
        VelX = BallSpeed * CSng(Math.Sin(AimAngle))
        VelY = -BallSpeed * CSng(Math.Cos(AimAngle))
        TrailCount = 0
        PreviewCount = 0
        RaiseEvent Launched()
    End Sub

    ' ===================== Main update (the form calls this with FixedStep) =====================

    Public Sub Update(dt As Single, moveDir As Integer)
        AnimClock += dt
        StateTime += dt
        If timing Then ElapsedTime += CDbl(dt)
        UpdateEffects(dt)

        Select Case State
            Case CBState.Ready
                MoveCart(dt, moveDir)
                SyncBallToCart()
                If IntroTime > 0.0F Then IntroTime = Math.Max(0.0F, IntroTime - dt)

            Case CBState.Locked
                aimClock += dt
                AimAngle = AimMaxAngle * CSng(Math.Sin(aimClock * AimSweepSpeed))
                BuildPreview()

            Case CBState.Playing
                MoveCart(dt, moveDir)
                StepBall(dt)

            Case CBState.BallLost
                MoveCart(dt, moveDir)
                If StateTime >= BallLostDelay Then
                    If Lives > 0 Then PrepareBall(False) Else EndGame()
                End If

            Case CBState.LevelComplete
                If Not clearAnnounced AndAlso StateTime >= LevelCompleteHold Then
                    clearAnnounced = True
                    RaiseEvent LevelClearFinished()
                End If
        End Select
    End Sub

    ' ===================== Cart =====================

    Private Shared Function Clamp(v As Single, lo As Single, hi As Single) As Single
        Return Math.Max(lo, Math.Min(hi, v))
    End Function

    Private Function ClampCart(x As Single) As Single
        Return Clamp(x, WallLeft + PaddleW / 2.0F, WallRight - PaddleW / 2.0F)
    End Function

    Private Sub MoveCart(dt As Single, moveDir As Integer)
        If moveDir <> 0 Then CartX = ClampCart(CartX + moveDir * PaddleSpeed * dt)
    End Sub

    ' ===================== Ball physics =====================

    ''' <summary>
    ''' Moves the ball for one fixed step. The step is split into sub-steps so the ball never travels
    ''' more than MaxStepDistance at once; that is what makes collisions reliable at any speed.
    ''' </summary>
    Private Sub StepBall(dt As Single)
        Dim distance As Single = BallSpeed * dt
        Dim steps As Integer = Math.Max(1, CInt(Math.Ceiling(distance / MaxStepDistance)))
        Dim subDt As Single = dt / steps
        For i As Integer = 1 To steps
            MoveBallOnce(subDt)
            If State <> CBState.Playing Then Exit For
        Next
        trailTick += 1
        If trailTick Mod 2 = 0 Then RecordTrail()
    End Sub

    Private Sub MoveBallOnce(dt As Single)
        BallX += VelX * dt
        BallY += VelY * dt

        ' Side walls: push out and point the velocity away from the wall (cannot get stuck).
        If BallX - BallRadius < WallLeft Then
            BallX = WallLeft + BallRadius
            VelX = Math.Abs(VelX)
            RaiseEvent WallBounced()
        ElseIf BallX + BallRadius > WallRight Then
            BallX = WallRight - BallRadius
            VelX = -Math.Abs(VelX)
            RaiseEvent WallBounced()
        End If

        ' The TOP IS OPEN: no bounce. Once the ball is completely above the playfield the level is won.
        If BallY + BallRadius < 0.0F Then
            CompleteLevel()
            Return
        End If

        ' Below the bottom edge: ball lost.
        If BallY - BallRadius > PlayH Then
            LoseBall()
            Return
        End If

        CollideCart()
        CollideBlocks()
    End Sub

    ''' <summary>
    ''' Keeps the speed EXACTLY BallSpeed and guarantees minimum vertical and horizontal components,
    ''' so the ball can never creep along horizontally or ping-pong vertically forever.
    ''' </summary>
    Private Sub NormalizeVelocity()
        Dim vySign As Single = If(VelY > 0.0F, 1.0F, -1.0F)
        Dim vxSign As Single = If(VelX >= 0.0F, 1.0F, -1.0F)
        Dim absVx As Single = Math.Abs(VelX)
        Dim absVy As Single = Math.Abs(VelY)
        Dim current As Single = CSng(Math.Sqrt(absVx * absVx + absVy * absVy))

        If current < 0.001F Then
            absVx = 0.0F
            absVy = BallSpeed
            vySign = -1.0F
        Else
            absVx = absVx / current * BallSpeed
            absVy = absVy / current * BallSpeed
        End If

        Dim minVy As Single = BallSpeed * MinVerticalRatio
        Dim minVx As Single = BallSpeed * MinHorizontalRatio
        If absVy < minVy Then absVy = minVy
        absVx = CSng(Math.Sqrt(Math.Max(0.0F, BallSpeed * BallSpeed - absVy * absVy)))
        If absVx < minVx Then
            absVx = minVx
            absVy = CSng(Math.Sqrt(Math.Max(0.0F, BallSpeed * BallSpeed - absVx * absVx)))
        End If

        VelX = vxSign * absVx
        VelY = vySign * absVy
    End Sub

    ''' <summary>
    ''' Cart collision. Top face: the outgoing angle depends on WHERE the ball lands
    ''' (centre = straight up, edges = up to MaxBounceAngle). Side face: the ball is simply pushed away.
    ''' </summary>
    Private Sub CollideCart()
        If VelY <= 0.0F Then Return          ' only a falling ball can hit the cart

        Dim left As Single = CartX - PaddleW / 2.0F
        Dim right As Single = CartX + PaddleW / 2.0F
        Dim top As Single = PaddleY
        Dim bottom As Single = PaddleY + PaddleH

        Dim cx As Single = Clamp(BallX, left, right)
        Dim cy As Single = Clamp(BallY, top, bottom)
        Dim dx As Single = BallX - cx
        Dim dy As Single = BallY - cy
        If dx * dx + dy * dy >= BallRadius * BallRadius Then Return

        If BallY < top + PaddleTopTolerance Then
            BallY = top - BallRadius - CollisionSkin
            Dim hit As Single = Clamp((BallX - CartX) / (PaddleW / 2.0F), -1.0F, 1.0F)
            Dim angle As Single = hit * MaxBounceAngle
            Dim keepSign As Single = If(VelX >= 0.0F, 1.0F, -1.0F)
            VelX = BallSpeed * CSng(Math.Sin(angle))
            VelY = -BallSpeed * CSng(Math.Cos(angle))
            If Math.Abs(VelX) < 0.01F Then VelX = 0.01F * keepSign
            NormalizeVelocity()
        Else
            If BallX < CartX Then
                BallX = left - BallRadius - CollisionSkin
                VelX = -Math.Abs(VelX)
            Else
                BallX = right + BallRadius + CollisionSkin
                VelX = Math.Abs(VelX)
            End If
            NormalizeVelocity()
        End If
        RaiseEvent CartBounced()
    End Sub

    ''' <summary>
    ''' Circle-vs-rectangle. Only the CLOSEST overlapping block is handled per sub-step, the ball is pushed
    ''' out of it, and the velocity is pointed away along the contact axis. Result: one hit = one block,
    ''' no double scoring, no double flipping, no sticking.
    ''' </summary>
    Private Sub CollideBlocks()
        Dim best As CBBlock = Nothing
        Dim bestD2 As Single = Single.MaxValue
        Dim bestCx As Single
        Dim bestCy As Single
        Dim r2 As Single = BallRadius * BallRadius

        For Each b As CBBlock In Blocks
            If Not b.Alive Then Continue For
            If BallX + BallRadius < b.X OrElse BallX - BallRadius > b.X + b.Width OrElse
               BallY + BallRadius < b.Y OrElse BallY - BallRadius > b.Y + b.Height Then Continue For

            Dim cx As Single = Clamp(BallX, b.X, b.X + b.Width)
            Dim cy As Single = Clamp(BallY, b.Y, b.Y + b.Height)
            Dim dx As Single = BallX - cx
            Dim dy As Single = BallY - cy
            Dim d2 As Single = dx * dx + dy * dy
            If d2 < r2 AndAlso d2 < bestD2 Then
                best = b
                bestD2 = d2
                bestCx = cx
                bestCy = cy
            End If
        Next

        If best Is Nothing Then Return
        ResolveBlockHit(best, bestCx, bestCy, bestD2)
        HitBlock(best)
    End Sub

    Private Sub ResolveBlockHit(b As CBBlock, cx As Single, cy As Single, d2 As Single)
        Dim dx As Single = BallX - cx
        Dim dy As Single = BallY - cy
        Dim horizontal As Boolean

        If d2 > 0.0001F Then
            ' Ball centre is outside the block: push out along the contact normal.
            Dim d As Single = CSng(Math.Sqrt(d2))
            Dim push As Single = BallRadius - d + CollisionSkin
            BallX += dx / d * push
            BallY += dy / d * push
            horizontal = Math.Abs(dx) > Math.Abs(dy)      ' side hit or top/bottom hit
        Else
            ' Ball centre is inside the block (very rare): leave through the nearest face.
            Dim toLeft As Single = BallX - b.X
            Dim toRight As Single = b.X + b.Width - BallX
            Dim toTop As Single = BallY - b.Y
            Dim toBottom As Single = b.Y + b.Height - BallY
            Dim m As Single = Math.Min(Math.Min(toLeft, toRight), Math.Min(toTop, toBottom))
            If m = toLeft Then
                BallX = b.X - BallRadius - CollisionSkin : dx = -1.0F : horizontal = True
            ElseIf m = toRight Then
                BallX = b.X + b.Width + BallRadius + CollisionSkin : dx = 1.0F : horizontal = True
            ElseIf m = toTop Then
                BallY = b.Y - BallRadius - CollisionSkin : dy = -1.0F : horizontal = False
            Else
                BallY = b.Y + b.Height + BallRadius + CollisionSkin : dy = 1.0F : horizontal = False
            End If
        End If

        If horizontal Then
            VelX = If(dx < 0.0F, -Math.Abs(VelX), Math.Abs(VelX))
        Else
            VelY = If(dy < 0.0F, -Math.Abs(VelY), Math.Abs(VelY))
        End If
        NormalizeVelocity()
    End Sub

    Private Sub HitBlock(b As CBBlock)
        b.HitsLeft -= 1
        b.FlashTime = 0.12F

        If b.HitsLeft > 0 Then
            ' Stone, first hit: it bounces but gives NO points.
            SpawnBurst(BallX, BallY, b.Kind, 4, 90.0F)
            RaiseEvent BlockDamaged(b.Kind)
            Return
        End If

        b.Alive = False
        Dim pts As Integer = PointsFor(b.Kind)
        Score += pts
        Dim mx As Single = b.X + b.Width / 2.0F
        Dim my As Single = b.Y + b.Height / 2.0F
        SpawnBurst(mx, my, b.Kind, 10, 130.0F)
        Popups.Add(New CBPopup With {.X = mx, .Y = my, .Text = "+" & pts.ToString(), .Tint = b.Kind})
        If b.Kind = CBBlockType.Emerald Then GainLife(mx, my)
        RaiseEvent BlockDestroyed(b.Kind)
    End Sub

    Public Shared Function PointsFor(kind As CBBlockType) As Integer
        Select Case kind
            Case CBBlockType.Stone : Return PointsStone
            Case CBBlockType.Gold : Return PointsGold
            Case CBBlockType.Emerald : Return PointsEmerald
        End Select
        Return PointsBrick
    End Function

    Private Sub GainLife(x As Single, y As Single)
        If Lives < MaxLives Then Lives += 1
        LifeFlash = 1.0F
        Popups.Add(New CBPopup With {.X = x, .Y = y - 14.0F, .Text = "+1 BALL", .Tint = CBBlockType.Emerald})
        RaiseEvent LifeGained()
    End Sub

    Private Sub LoseBall()
        Lives -= 1
        State = CBState.BallLost
        StateTime = 0.0F
        TrailCount = 0
        LifeLostFlash = 0.6F
        ShakeTime = 0.3F
        RaiseEvent LifeLost()
    End Sub

    Private Sub CompleteLevel()
        State = CBState.LevelComplete
        StateTime = 0.0F
        clearAnnounced = False
        TrailCount = 0
        If Level >= CannonBallLevelData.LevelCount Then timing = False   ' the run timer stops when level 5 is done
        SpawnBurst(BallX, 10.0F, CBBlockType.Gold, 30, 170.0F)
        RaiseEvent LevelCompleted(Level)
    End Sub

    Private Sub EndGame()
        State = CBState.GameOver
        StateTime = 0.0F
        timing = False
        CommitBest(False)
        RaiseEvent GameEnded()
    End Sub

    Private Sub CommitBest(won As Boolean)
        Dim changed As Boolean = False
        If Score > BestScore Then
            BestScore = Score
            NewBestScore = True
            changed = True
        End If
        If won AndAlso (BestTime <= 0.0 OrElse ElapsedTime < BestTime) Then
            BestTime = ElapsedTime
            NewBestTime = True
            changed = True
        End If
        If changed Then CannonBallScores.Save(BestScore, BestTime)
    End Sub

    ' ===================== Aim preview / trail / effects =====================

    ''' <summary>Dotted line showing where the shot will go (bounces off walls, stops at the first block).</summary>
    Private Sub BuildPreview()
        Dim dirX As Single = CSng(Math.Sin(AimAngle))
        Dim dirY As Single = -CSng(Math.Cos(AimAngle))
        Dim x As Single = BallX
        Dim y As Single = BallY
        Dim travelled As Single = 0.0F
        Dim nextDot As Single = PreviewSpacing
        Dim guard As Integer = 0
        PreviewCount = 0

        Do While PreviewCount < PreviewDots AndAlso guard < 400
            guard += 1
            x += dirX * PreviewStep
            y += dirY * PreviewStep
            If x - BallRadius < WallLeft Then
                x = WallLeft + BallRadius
                dirX = Math.Abs(dirX)
            ElseIf x + BallRadius > WallRight Then
                x = WallRight - BallRadius
                dirX = -Math.Abs(dirX)
            End If
            If y < 0.0F Then Exit Do
            If PointHitsBlock(x, y) Then Exit Do
            travelled += PreviewStep
            If travelled >= nextDot Then
                PreviewX(PreviewCount) = x
                PreviewY(PreviewCount) = y
                PreviewCount += 1
                nextDot += PreviewSpacing
            End If
        Loop
    End Sub

    Private Function PointHitsBlock(x As Single, y As Single) As Boolean
        For Each b As CBBlock In Blocks
            If Not b.Alive Then Continue For
            If x > b.X - BallRadius AndAlso x < b.X + b.Width + BallRadius AndAlso
               y > b.Y - BallRadius AndAlso y < b.Y + b.Height + BallRadius Then Return True
        Next
        Return False
    End Function

    Private Sub RecordTrail()
        For i As Integer = TrailLength - 1 To 1 Step -1
            TrailX(i) = TrailX(i - 1)
            TrailY(i) = TrailY(i - 1)
        Next
        TrailX(0) = BallX
        TrailY(0) = BallY
        If TrailCount < TrailLength Then TrailCount += 1
    End Sub

    Private Sub SpawnBurst(x As Single, y As Single, tint As CBBlockType, count As Integer, speed As Single)
        For i As Integer = 1 To count
            If Particles.Count >= MaxParticles Then Return
            Dim ang As Double = rng.NextDouble() * Math.PI * 2.0
            Dim spd As Single = speed * (0.4F + CSng(rng.NextDouble()) * 0.8F)
            Particles.Add(New CBParticle With {
                .X = x, .Y = y,
                .VX = CSng(Math.Cos(ang)) * spd,
                .VY = CSng(Math.Sin(ang)) * spd - 40.0F,
                .Life = 0.45F + CSng(rng.NextDouble()) * 0.35F,
                .Size = 2.0F + CSng(rng.Next(0, 2)),
                .Tint = tint})
        Next
    End Sub

    Private Sub UpdateEffects(dt As Single)
        For i As Integer = Particles.Count - 1 To 0 Step -1
            Dim p As CBParticle = Particles(i)
            p.Age += dt
            If p.Age >= p.Life Then
                Particles.RemoveAt(i)
            Else
                p.VY += ParticleGravity * dt
                p.X += p.VX * dt
                p.Y += p.VY * dt
            End If
        Next
        For i As Integer = Popups.Count - 1 To 0 Step -1
            Popups(i).Age += dt
            If Popups(i).Age >= 0.8F Then Popups.RemoveAt(i)
        Next
        For Each b As CBBlock In Blocks
            If b.FlashTime > 0.0F Then b.FlashTime = Math.Max(0.0F, b.FlashTime - dt)
        Next
        LifeFlash = Math.Max(0.0F, LifeFlash - dt)
        LifeLostFlash = Math.Max(0.0F, LifeLostFlash - dt)
        ShakeTime = Math.Max(0.0F, ShakeTime - dt)
    End Sub

    ' ===================== Helpers =====================

    ''' <summary>mm:ss.cc (for example 01:23.45)</summary>
    Public Shared Function FormatTime(seconds As Double) As String
        If seconds < 0.0 Then seconds = 0.0
        Dim totalCs As Long = CLng(Math.Floor(seconds * 100.0))
        Dim cs As Long = totalCs Mod 100L
        Dim totalSec As Long = totalCs \ 100L
        Dim s As Long = totalSec Mod 60L
        Dim m As Long = totalSec \ 60L
        Return m.ToString("00") & ":" & s.ToString("00") & "." & cs.ToString("00")
    End Function

End Class
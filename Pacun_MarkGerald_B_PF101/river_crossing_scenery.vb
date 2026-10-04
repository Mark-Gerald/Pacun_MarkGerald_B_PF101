Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D

''' <summary>
''' River Crossing scenery. Everything is drawn in code (no image files):
'''   * STATIC (drawn once into the bank cache): ground patches, grass texture, tufts, flowers,
'''     stones, mushrooms, reeds on the shoreline, bushes and trees.
'''   * ANIMATED (drawn every frame): rabbits and birds hopping around, butterflies, falling leaves.
'''
''' Rules that keep the characters readable:
'''   * Trees and bushes are only placed OUTSIDE the "character zone" (where the farmers and goblins
'''     stand) and away from the HUD (moves text, status text, upper-right buttons).
'''   * Animals only walk in free areas outside the character zone.
'''   * All of it is drawn by RiverBackdrop BEFORE the boat and the characters, so characters are
'''     always painted on top.
''' The layout uses a fixed random seed, so the map looks the same every time it is built.
''' </summary>
Public Class RiverCrossingScenery
    Implements IDisposable

    Private Const PX As Integer = 3     ' one "art pixel" on screen (matches the pixel-art look)

    Private Enum PropKind
        Tuft
        Flower
        Stone
        Mushroom
        Reed
        Bush
        Tree
    End Enum

    Private Class GroundMark
        Public Kind As Integer          ' 0 light patch, 1 dark patch, 2 dirt, 3 light speck, 4 dark speck
        Public Bounds As Rectangle
    End Class

    Private Class Prop
        Public Kind As PropKind
        Public X As Integer             ' bottom-centre
        Public Y As Integer
        Public W As Integer
        Public H As Integer
        Public V As Integer             ' main variant
        Public V2 As Integer            ' extra variant
    End Class

    Private Class Leaf
        Public BaseX As Single
        Public Y As Single
        Public Speed As Single
        Public SwayAmp As Single
        Public SwaySpeed As Single
        Public SwayPhase As Single
        Public Rot As Single
        Public RotSpeed As Single
        Public Size As Integer
        Public ColorIndex As Integer
    End Class

    Private Class Butterfly
        Public BaseX As Single
        Public BaseY As Single
        Public RadiusX As Single
        Public RadiusY As Single
        Public Speed As Single
        Public Phase As Single
        Public ColorIndex As Integer
        Public Area As Rectangle
    End Class

    Private Class Critter
        Public IsBird As Boolean
        Public X As Single              ' feet position
        Public Y As Single
        Public Facing As Integer = 1
        Public Area As Rectangle
        Public Hopping As Boolean
        Public Timer As Single
        Public FromX As Single
        Public FromY As Single
        Public ToX As Single
        Public ToY As Single
        Public HopT As Single
        Public HopDur As Single
        Public HopH As Single
        Public ArcY As Single
    End Class

    ' ---- layout ----
    Private _w As Integer
    Private _h As Integer
    Private _left As Rectangle
    Private _river As Rectangle
    Private _right As Rectangle
    Private _rng As New Random(20240611)

    Private ReadOnly _zones As New List(Of Rectangle)        ' where characters stand: no tall scenery here
    Private ReadOnly _exclusions As New List(Of Rectangle)   ' HUD areas: no tall scenery here
    Private ReadOnly _free As New List(Of Rectangle)         ' where animals may walk
    Private ReadOnly _tallBoxes As New List(Of Rectangle)

    Private ReadOnly _ground As New List(Of GroundMark)
    Private ReadOnly _props As New List(Of Prop)

    ' ---- animation ----
    Private ReadOnly _leaves As New List(Of Leaf)
    Private ReadOnly _butterflies As New List(Of Butterfly)
    Private ReadOnly _critters As New List(Of Critter)
    Private _time As Double = 0

    Private ReadOnly _leafBrushes() As SolidBrush = {
        New SolidBrush(Color.FromArgb(210, 96, 168, 70)),
        New SolidBrush(Color.FromArgb(210, 140, 190, 70)),
        New SolidBrush(Color.FromArgb(215, 222, 160, 50)),
        New SolidBrush(Color.FromArgb(215, 210, 110, 40)),
        New SolidBrush(Color.FromArgb(215, 176, 72, 42))
    }
    Private Shared ReadOnly ButterflyColors() As Color = {
        Color.FromArgb(250, 170, 60), Color.FromArgb(240, 120, 170),
        Color.FromArgb(130, 170, 250), Color.FromArgb(250, 240, 120)
    }
    Private Shared ReadOnly FlowerColors() As Color = {
        Color.FromArgb(250, 250, 245), Color.FromArgb(240, 150, 190), Color.FromArgb(250, 225, 90),
        Color.FromArgb(150, 190, 250), Color.FromArgb(190, 150, 230)
    }

    ' ==================================================
    ' LAYOUT (called by RiverBackdrop.Layout)
    ' ==================================================
    Public Sub Layout(w As Integer, h As Integer, leftBank As Rectangle, river As Rectangle, rightBank As Rectangle)
        _w = w
        _h = h
        _left = leftBank
        _river = river
        _right = rightBank
        _rng = New Random(20240611)

        BuildZones()
        _ground.Clear()
        _props.Clear()
        _tallBoxes.Clear()
        PopulateBank(_left, True)
        PopulateBank(_right, False)
        _props.Sort(Function(a, b) a.Y.CompareTo(b.Y))

        BuildAnimals()
        BuildLeaves()
    End Sub

    Private Sub BuildZones()
        _zones.Clear()
        _exclusions.Clear()
        _free.Clear()

        Dim cy As Integer = _h \ 2
        Const zoneW As Integer = 255
        ' Where the farmers and goblins stand (matches GameScenePanel: columns near the river, rows around the middle).
        _zones.Add(New Rectangle(_left.Right - zoneW, cy - 255, zoneW, 400))
        _zones.Add(New Rectangle(_right.Left, cy - 255, zoneW, 400))

        ' HUD: moves + status text (upper left) and the three buttons (upper right).
        _exclusions.Add(New Rectangle(0, 0, 520, 135))
        _exclusions.Add(New Rectangle(_w - 200, 0, 200, 175))

        For Each bank As Rectangle In New Rectangle() {_left, _right}
            Dim isLeft As Boolean = (bank.X = _left.X)
            Dim outerW As Integer = bank.Width - zoneW - 16
            If outerW >= 70 AndAlso _h - 162 >= 60 Then
                Dim ox As Integer = If(isLeft, bank.X + 8, bank.Right - 8 - outerW)
                _free.Add(New Rectangle(ox, 150, outerW, _h - 162))
            End If
            Dim bandTop As Integer = cy + 150
            If _h - 14 - bandTop >= 60 AndAlso bank.Width > 40 Then
                _free.Add(New Rectangle(bank.X + 8, bandTop, bank.Width - 16, _h - 14 - bandTop))
            End If
        Next
    End Sub

    Private Shared Function HitsAny(r As Rectangle, list As List(Of Rectangle)) As Boolean
        For Each item As Rectangle In list
            If r.IntersectsWith(item) Then Return True
        Next
        Return False
    End Function

    ' Small details may sit in the character zone but are thinned out there.
    Private Function SkipInZone(r As Rectangle) As Boolean
        Return HitsAny(r, _zones) AndAlso _rng.NextDouble() < 0.65
    End Function

    Private Sub PopulateBank(b As Rectangle, isLeft As Boolean)
        If b.Width < 30 OrElse b.Height < 30 Then Return
        Dim area As Double = CDbl(b.Width) * b.Height

        ' ----- ground: soft patches, dirt, grass specks -----
        For i As Integer = 1 To Math.Max(3, CInt(area / 42000.0))
            Dim pw As Integer = _rng.Next(70, 170)
            Dim ph As Integer = _rng.Next(40, 95)
            _ground.Add(New GroundMark With {
                .Kind = _rng.Next(0, 2),
                .Bounds = New Rectangle(b.X + _rng.Next(0, Math.Max(1, b.Width - pw)), _rng.Next(0, Math.Max(1, _h - ph)), pw, ph)})
        Next
        For i As Integer = 1 To 2 + CInt(area / 200000.0)
            Dim pw As Integer = _rng.Next(40, 80)
            Dim ph As Integer = _rng.Next(22, 42)
            _ground.Add(New GroundMark With {
                .Kind = 2,
                .Bounds = New Rectangle(b.X + _rng.Next(0, Math.Max(1, b.Width - pw)), _rng.Next(0, Math.Max(1, _h - ph)), pw, ph)})
        Next
        For i As Integer = 1 To CInt(area / 1700.0)
            _ground.Add(New GroundMark With {
                .Kind = _rng.Next(3, 5),
                .Bounds = New Rectangle(b.X + _rng.Next(2, Math.Max(3, b.Width - 4)), _rng.Next(2, Math.Max(3, _h - 4)), PX, PX)})
        Next

        ' ----- shoreline reeds -----
        Dim edgeX As Integer = If(isLeft, b.Right - 8, b.X + 8)
        For i As Integer = 1 To Math.Max(3, b.Height \ 80)
            Dim y As Integer = _rng.Next(30, Math.Max(31, _h - 6))
            Dim count As Integer = _rng.Next(2, 4)
            For k As Integer = 0 To count - 1
                _props.Add(New Prop With {.Kind = PropKind.Reed, .X = edgeX + _rng.Next(-3, 4) + (If(isLeft, -k, k)) * 7,
                                          .Y = y + _rng.Next(-3, 4), .W = PX, .H = _rng.Next(18, 30), .V = _rng.Next(0, 2)})
            Next
        Next

        ' ----- grass tufts -----
        For i As Integer = 1 To CInt(area / 5000.0)
            Dim x As Integer = _rng.Next(b.X + 6, Math.Max(b.X + 7, b.Right - 6))
            Dim y As Integer = _rng.Next(24, Math.Max(25, _h - 4))
            If SkipInZone(New Rectangle(x - 8, y - 14, 16, 14)) Then Continue For
            _props.Add(New Prop With {.Kind = PropKind.Tuft, .X = x, .Y = y, .W = 12, .H = 12, .V = _rng.Next(0, 3), .V2 = _rng.Next(3, 5)})
        Next

        ' ----- flower clusters -----
        For c As Integer = 1 To Math.Max(2, CInt(area / 60000.0))
            Dim cx As Integer = _rng.Next(b.X + 30, Math.Max(b.X + 31, b.Right - 30))
            Dim cy As Integer = _rng.Next(40, Math.Max(41, _h - 20))
            Dim colorIndex As Integer = _rng.Next(0, FlowerColors.Length)
            For n As Integer = 1 To _rng.Next(3, 7)
                Dim x As Integer = cx + _rng.Next(-40, 41)
                Dim y As Integer = cy + _rng.Next(-22, 23)
                If x < b.X + 6 OrElse x > b.Right - 6 OrElse y < 20 OrElse y > _h - 4 Then Continue For
                If SkipInZone(New Rectangle(x - 6, y - 16, 12, 16)) Then Continue For
                _props.Add(New Prop With {.Kind = PropKind.Flower, .X = x, .Y = y, .W = 9, .H = 15,
                                          .V = If(_rng.NextDouble() < 0.8, colorIndex, _rng.Next(0, FlowerColors.Length))})
            Next
        Next

        ' ----- stones -----
        For i As Integer = 1 To Math.Max(3, CInt(area / 24000.0))
            Dim x As Integer = _rng.Next(b.X + 16, Math.Max(b.X + 17, b.Right - 16))
            Dim y As Integer = _rng.Next(28, Math.Max(29, _h - 4))
            If SkipInZone(New Rectangle(x - 15, y - 12, 30, 12)) Then Continue For
            _props.Add(New Prop With {.Kind = PropKind.Stone, .X = x, .Y = y, .W = 12, .H = 10, .V = _rng.Next(0, 4), .V2 = _rng.Next(0, 2)})
        Next

        ' ----- mushrooms -----
        For i As Integer = 1 To _rng.Next(3, 6)
            Dim x As Integer = _rng.Next(b.X + 10, Math.Max(b.X + 11, b.Right - 10))
            Dim y As Integer = _rng.Next(40, Math.Max(41, _h - 4))
            If SkipInZone(New Rectangle(x - 8, y - 14, 16, 14)) Then Continue For
            _props.Add(New Prop With {.Kind = PropKind.Mushroom, .X = x, .Y = y, .W = 9, .H = 12, .V = _rng.Next(0, 2), .V2 = _rng.Next(1, 3)})
        Next

        ' ----- bushes and trees: only outside the character zone and the HUD -----
        TryPlaceTall(b, PropKind.Bush, Math.Min(7, 3 + CInt(area / 100000.0)), 46, 74, 30, 46)
        TryPlaceTall(b, PropKind.Tree, Math.Min(6, 2 + CInt(area / 90000.0)), 74, 104, 112, 150)
    End Sub

    Private Sub TryPlaceTall(b As Rectangle, kind As PropKind, target As Integer, wMin As Integer, wMax As Integer, hMin As Integer, hMax As Integer)
        Dim placed As Integer = 0
        For attempt As Integer = 0 To 160
            If placed >= target Then Exit For
            Dim w As Integer = _rng.Next(wMin, wMax + 1)
            Dim h As Integer = _rng.Next(hMin, hMax + 1)
            Dim minX As Integer = b.X + w \ 2 + 2
            Dim maxX As Integer = b.Right - w \ 2 - 2
            Dim minY As Integer = h + 8
            Dim maxY As Integer = _h - 8
            If maxX <= minX OrElse maxY <= minY Then Continue For

            Dim x As Integer = _rng.Next(minX, maxX + 1)
            Dim y As Integer = _rng.Next(minY, maxY + 1)
            Dim box As New Rectangle(x - w \ 2, y - h, w, h + 6)
            Dim padded As Rectangle = Rectangle.Inflate(box, 6, 4)
            If HitsAny(padded, _zones) OrElse HitsAny(padded, _exclusions) OrElse HitsAny(padded, _tallBoxes) Then Continue For

            _tallBoxes.Add(padded)
            _props.Add(New Prop With {.Kind = kind, .X = x, .Y = y, .W = w, .H = h,
                                      .V = If(kind = PropKind.Tree AndAlso _rng.NextDouble() < 0.22, 1, 0), .V2 = _rng.Next(0, 2)})
            placed += 1
        Next
    End Sub

    ' ==================================================
    ' ANIMALS AND LEAVES (set up)
    ' ==================================================
    Private Sub BuildAnimals()
        _critters.Clear()
        _butterflies.Clear()

        ' Rabbits and birds only where there is free room outside the character zone.
        If _free.Count > 0 Then
            For i As Integer = 0 To 3
                Dim area As Rectangle = _free(i Mod _free.Count)
                Dim c As New Critter With {.IsBird = (i >= 2), .Area = area}
                c.X = _rng.Next(area.X + 20, Math.Max(area.X + 21, area.Right - 20))
                c.Y = _rng.Next(area.Y + 30, Math.Max(area.Y + 31, area.Bottom - 6))
                c.Timer = CSng(_rng.NextDouble() * 2.0)
                c.Facing = If(_rng.Next(0, 2) = 0, -1, 1)
                _critters.Add(c)
            Next
        End If

        ' Butterflies flutter over the grass.
        For Each bank As Rectangle In New Rectangle() {_left, _right}
            If bank.Width < 60 Then Continue For
            For i As Integer = 1 To 2
                _butterflies.Add(New Butterfly With {
                    .Area = bank,
                    .BaseX = _rng.Next(bank.X + 40, Math.Max(bank.X + 41, bank.Right - 40)),
                    .BaseY = _rng.Next(160, Math.Max(161, _h - 60)),
                    .RadiusX = _rng.Next(30, 80),
                    .RadiusY = _rng.Next(18, 44),
                    .Speed = CSng(0.5 + _rng.NextDouble() * 0.7),
                    .Phase = CSng(_rng.NextDouble() * 6.28),
                    .ColorIndex = _rng.Next(0, ButterflyColors.Length)})
            Next
        Next
    End Sub

    Private Sub BuildLeaves()
        _leaves.Clear()
        If _w <= 0 OrElse _h <= 0 Then Return
        For i As Integer = 1 To 18
            _leaves.Add(New Leaf With {
                .BaseX = _rng.Next(0, _w),
                .Y = _rng.Next(-20, _h),
                .Speed = CSng(26 + _rng.NextDouble() * 30),
                .SwayAmp = CSng(14 + _rng.NextDouble() * 26),
                .SwaySpeed = CSng(0.8 + _rng.NextDouble() * 1.2),
                .SwayPhase = CSng(_rng.NextDouble() * 6.28),
                .Rot = CSng(_rng.NextDouble() * 360),
                .RotSpeed = CSng(-90 + _rng.NextDouble() * 180),
                .Size = _rng.Next(3, 6),
                .ColorIndex = _rng.Next(0, _leafBrushes.Length)})
        Next
    End Sub

    ' ==================================================
    ' UPDATE
    ' ==================================================
    Public Sub Advance(dt As Double)
        _time += dt
        Dim step1 As Single = CSng(dt)

        For Each lf As Leaf In _leaves
            lf.Y += lf.Speed * step1
            lf.Rot += lf.RotSpeed * step1
            If lf.Y > _h + 12 Then
                lf.Y = -12
                lf.BaseX = _rng.Next(0, Math.Max(1, _w))
            End If
        Next

        For Each c As Critter In _critters
            UpdateCritter(c, step1)
        Next
    End Sub

    Private Sub UpdateCritter(c As Critter, dt As Single)
        If c.Hopping Then
            c.HopT += dt
            Dim f As Single = Math.Min(1.0F, c.HopT / c.HopDur)
            c.X = c.FromX + (c.ToX - c.FromX) * f
            c.Y = c.FromY + (c.ToY - c.FromY) * f
            c.ArcY = CSng(Math.Sin(Math.PI * f)) * c.HopH
            If f >= 1.0F Then
                c.Hopping = False
                c.ArcY = 0
                c.Timer = If(c.IsBird, CSng(0.3 + _rng.NextDouble() * 1.4), CSng(0.9 + _rng.NextDouble() * 2.2))
            End If
            Return
        End If

        c.Timer -= dt
        If c.Timer > 0 Then Return

        Dim range As Single = If(c.IsBird, 34.0F, 62.0F)
        Dim dx As Single = CSng((_rng.NextDouble() * 2.0 - 1.0) * range)
        Dim dy As Single = CSng((_rng.NextDouble() * 2.0 - 1.0) * range * 0.4)
        Dim tx As Single = Math.Max(c.Area.X + 16, Math.Min(c.Area.Right - 16, c.X + dx))
        Dim ty As Single = Math.Max(c.Area.Y + 28, Math.Min(c.Area.Bottom - 4, c.Y + dy))
        If Math.Abs(tx - c.X) > 4 Then c.Facing = If(tx > c.X, 1, -1)

        c.FromX = c.X
        c.FromY = c.Y
        c.ToX = tx
        c.ToY = ty
        c.HopT = 0
        c.HopDur = If(c.IsBird, 0.22F, 0.42F)
        c.HopH = If(c.IsBird, 7.0F, 15.0F)
        c.Hopping = True
    End Sub

    ' ==================================================
    ' DRAWING: STATIC (into the bank cache)
    ' ==================================================
    Public Sub DrawStatic(g As Graphics)
        If _w <= 0 OrElse _h <= 0 Then Return
        Dim oldSmoothing As SmoothingMode = g.SmoothingMode
        g.SmoothingMode = SmoothingMode.None

        Using clipRegion As New Region(_left)
            clipRegion.Union(_right)
            g.SetClip(clipRegion, CombineMode.Replace)
        End Using

        For Each m As GroundMark In _ground
            DrawGroundMark(g, m)
        Next
        For Each p As Prop In _props
            Select Case p.Kind
                Case PropKind.Tuft
                    DrawTuft(g, p)
                Case PropKind.Flower
                    DrawFlower(g, p)
                Case PropKind.Stone
                    DrawStone(g, p)
                Case PropKind.Mushroom
                    DrawMushroom(g, p)
                Case PropKind.Reed
                    DrawReed(g, p)
                Case PropKind.Bush
                    DrawBush(g, p)
                Case PropKind.Tree
                    DrawTree(g, p)
            End Select
        Next

        g.ResetClip()
        g.SmoothingMode = oldSmoothing
    End Sub

    Private Shared Sub FillRect(g As Graphics, c As Color, x As Integer, y As Integer, w As Integer, h As Integer)
        Using b As New SolidBrush(c)
            g.FillRectangle(b, x, y, w, h)
        End Using
    End Sub

    Private Shared Sub FillOval(g As Graphics, c As Color, x As Double, y As Double, w As Double, h As Double)
        Using b As New SolidBrush(c)
            g.FillEllipse(b, CInt(Math.Round(x)), CInt(Math.Round(y)), Math.Max(1, CInt(Math.Round(w))), Math.Max(1, CInt(Math.Round(h))))
        End Using
    End Sub

    Private Shared Sub DrawGroundMark(g As Graphics, m As GroundMark)
        Dim r As Rectangle = m.Bounds
        Select Case m.Kind
            Case 0
                FillOval(g, Color.FromArgb(44, 200, 240, 150), r.X, r.Y, r.Width, r.Height)
            Case 1
                FillOval(g, Color.FromArgb(46, 50, 110, 60), r.X, r.Y, r.Width, r.Height)
            Case 2
                FillOval(g, Color.FromArgb(120, 150, 112, 70), r.X, r.Y, r.Width, r.Height)
                FillOval(g, Color.FromArgb(90, 176, 138, 90), r.X + 6, r.Y + 4, r.Width - 12, r.Height - 10)
            Case 3
                FillRect(g, Color.FromArgb(150, 170, 225, 130), r.X, r.Y, r.Width, r.Height)
            Case Else
                FillRect(g, Color.FromArgb(130, 60, 125, 64), r.X, r.Y, r.Width, r.Height)
        End Select
    End Sub

    Private Shared Sub DrawTuft(g As Graphics, p As Prop)
        Dim shades() As Color = {Color.FromArgb(84, 160, 70), Color.FromArgb(108, 186, 86), Color.FromArgb(62, 128, 62)}
        Dim heights() As Integer = {9, 13, 8, 11}
        Dim blades As Integer = p.V2
        For i As Integer = 0 To blades - 1
            Dim h As Integer = heights((i + p.V) Mod heights.Length)
            Dim bx As Integer = p.X + (i - blades \ 2) * PX
            Dim c As Color = shades((i + p.V) Mod shades.Length)
            FillRect(g, c, bx, p.Y - h, PX, h)
            Dim lean As Integer = If(i Mod 2 = 0, -PX, PX)
            FillRect(g, c, bx + lean, p.Y - h - PX, PX, PX)
        Next
    End Sub

    Private Shared Sub DrawFlower(g As Graphics, p As Prop)
        Dim petal As Color = FlowerColors(p.V Mod FlowerColors.Length)
        FillRect(g, Color.FromArgb(70, 140, 64), p.X, p.Y - 9, 2, 9)
        FillRect(g, Color.FromArgb(90, 160, 74), p.X - 3, p.Y - 5, 3, 2)
        Dim cx As Integer = p.X - 1
        Dim cy As Integer = p.Y - 12
        FillRect(g, petal, cx - 3, cy, PX, PX)
        FillRect(g, petal, cx + 3, cy, PX, PX)
        FillRect(g, petal, cx, cy - 3, PX, PX)
        FillRect(g, petal, cx, cy + 3, PX, PX)
        FillRect(g, Color.FromArgb(240, 190, 50), cx, cy, PX, PX)
    End Sub

    Private Shared Sub DrawStone(g As Graphics, p As Prop)
        Dim widths() As Integer = {12, 18, 24, 30}
        Dim w As Integer = widths(p.V Mod widths.Length)
        FillOval(g, Color.FromArgb(55, 0, 0, 0), p.X - w / 2.0 - 2, p.Y - 3, w + 6, 6)
        FillRect(g, Color.FromArgb(104, 106, 118), p.X - w \ 2 + 2, p.Y - 3, w - 4, 3)
        FillRect(g, Color.FromArgb(140, 142, 152), p.X - w \ 2, p.Y - 7, w, 4)
        FillRect(g, Color.FromArgb(176, 178, 186), p.X - w \ 2 + 3, p.Y - 10, w - 6, 3)
        If p.V2 = 1 Then FillRect(g, Color.FromArgb(96, 156, 80), p.X - w \ 2 + 3, p.Y - 10, 6, 3)
    End Sub

    Private Shared Sub DrawMushroom(g As Graphics, p As Prop)
        Dim cap As Color = If(p.V = 0, Color.FromArgb(205, 60, 50), Color.FromArgb(190, 140, 90))
        FillRect(g, Color.FromArgb(240, 230, 200), p.X - 1, p.Y - 6, PX, 6)
        FillRect(g, cap, p.X - 4, p.Y - 9, 9, 3)
        FillRect(g, cap, p.X - 2, p.Y - 12, 6, 3)
        FillRect(g, Color.FromArgb(250, 245, 235), p.X - 3, p.Y - 9, 2, 2)
        FillRect(g, Color.FromArgb(250, 245, 235), p.X + 2, p.Y - 11, 2, 2)
    End Sub

    Private Shared Sub DrawReed(g As Graphics, p As Prop)
        FillRect(g, Color.FromArgb(104, 150, 70), p.X, p.Y - p.H, PX, p.H)
        FillRect(g, Color.FromArgb(84, 126, 56), p.X + 1, p.Y - p.H + 6, 2, p.H - 6)
        If p.V = 0 Then
            FillRect(g, Color.FromArgb(120, 80, 40), p.X - 1, p.Y - p.H - 4, 5, 8)    ' cattail
        Else
            FillRect(g, Color.FromArgb(104, 150, 70), p.X + PX, p.Y - p.H + 2, PX, PX)
        End If
    End Sub

    Private Shared Sub DrawBush(g As Graphics, p As Prop)
        Dim x As Double = p.X - p.W / 2.0
        Dim y As Double = p.Y - p.H
        FillOval(g, Color.FromArgb(60, 0, 0, 0), x + p.W * 0.05, p.Y - 7, p.W * 0.9, 12)
        FillOval(g, Color.FromArgb(52, 118, 64), x, y + p.H * 0.35, p.W, p.H * 0.65)
        FillOval(g, Color.FromArgb(74, 148, 76), x + p.W * 0.02, y + p.H * 0.18, p.W * 0.56, p.H * 0.7)
        FillOval(g, Color.FromArgb(74, 148, 76), x + p.W * 0.4, y + p.H * 0.1, p.W * 0.58, p.H * 0.78)
        FillOval(g, Color.FromArgb(112, 184, 92), x + p.W * 0.14, y + p.H * 0.14, p.W * 0.28, p.H * 0.3)
        FillOval(g, Color.FromArgb(112, 184, 92), x + p.W * 0.56, y + p.H * 0.12, p.W * 0.2, p.H * 0.24)
        If p.V2 = 1 Then
            Dim r As New Random(p.X * 31 + p.Y)
            For i As Integer = 1 To 5
                FillRect(g, Color.FromArgb(214, 60, 70), CInt(x + p.W * (0.15 + r.NextDouble() * 0.7)), CInt(y + p.H * (0.3 + r.NextDouble() * 0.5)), PX, PX)
            Next
        End If
    End Sub

    Private Shared Sub DrawTree(g As Graphics, p As Prop)
        Dim w As Double = p.W
        Dim trunkW As Integer = Math.Max(9, (p.W \ 7) \ PX * PX)
        Dim trunkH As Integer = CInt(p.H * 0.34)
        Dim canopyH As Double = p.H - trunkH * 0.55
        Dim top As Double = p.Y - p.H
        Dim x As Double = p.X - w / 2.0

        ' shadow, trunk, roots
        FillOval(g, Color.FromArgb(55, 0, 0, 0), p.X - w * 0.42, p.Y - 9, w * 0.84, 18)
        Dim tx As Integer = p.X - trunkW \ 2
        FillRect(g, Color.FromArgb(112, 78, 48), tx, p.Y - trunkH, trunkW, trunkH)
        FillRect(g, Color.FromArgb(84, 58, 36), tx + trunkW * 2 \ 3, p.Y - trunkH, trunkW - trunkW * 2 \ 3, trunkH)
        FillRect(g, Color.FromArgb(112, 78, 48), tx - 3, p.Y - 6, trunkW + 6, 6)

        ' canopy (green, or an autumn colour for a few trees)
        Dim dark As Color = If(p.V = 0, Color.FromArgb(52, 118, 64), Color.FromArgb(168, 104, 38))
        Dim mid As Color = If(p.V = 0, Color.FromArgb(74, 148, 76), Color.FromArgb(210, 150, 50))
        Dim light As Color = If(p.V = 0, Color.FromArgb(112, 184, 92), Color.FromArgb(236, 192, 72))

        FillOval(g, dark, x, top + canopyH * 0.18, w, canopyH * 0.82)
        FillOval(g, dark, x + w * 0.08, top, w * 0.55, canopyH * 0.6)
        FillOval(g, dark, x + w * 0.36, top + 4, w * 0.56, canopyH * 0.58)
        FillOval(g, mid, x + w * 0.06, top + canopyH * 0.1, w * 0.62, canopyH * 0.5)
        FillOval(g, mid, x + w * 0.34, top + canopyH * 0.16, w * 0.58, canopyH * 0.46)
        FillOval(g, light, x + w * 0.14, top + canopyH * 0.1, w * 0.3, canopyH * 0.24)
        FillOval(g, light, x + w * 0.5, top + canopyH * 0.2, w * 0.22, canopyH * 0.18)

        Dim r As New Random(p.X * 17 + p.Y)
        For i As Integer = 1 To 10
            FillRect(g, light, CInt(x + w * (0.12 + r.NextDouble() * 0.76)), CInt(top + canopyH * (0.12 + r.NextDouble() * 0.7)), PX, PX)
        Next
    End Sub

    ' ==================================================
    ' DRAWING: ANIMATED (every frame)
    ' ==================================================
    ''' <summary>Rabbits, birds and butterflies. Drawn after the bank cache, before the boat and characters.</summary>
    Public Sub DrawAnimals(g As Graphics)
        If _w <= 0 Then Return
        Dim oldSmoothing As SmoothingMode = g.SmoothingMode
        g.SmoothingMode = SmoothingMode.None

        For Each c As Critter In _critters
            If c.IsBird Then DrawBird(g, c) Else DrawRabbit(g, c)
        Next
        For Each bf As Butterfly In _butterflies
            DrawButterfly(g, bf)
        Next

        g.SmoothingMode = oldSmoothing
    End Sub

    ''' <summary>Falling leaves. Drawn over the whole map (banks and river), still behind the boat and characters.</summary>
    Public Sub DrawLeaves(g As Graphics)
        If _w <= 0 Then Return
        Dim oldSmoothing As SmoothingMode = g.SmoothingMode
        g.SmoothingMode = SmoothingMode.None

        For Each lf As Leaf In _leaves
            Dim x As Single = lf.BaseX + CSng(Math.Sin(_time * lf.SwaySpeed + lf.SwayPhase)) * lf.SwayAmp
            Dim state As GraphicsState = g.Save()
            g.TranslateTransform(x, lf.Y)
            g.RotateTransform(lf.Rot)
            g.FillEllipse(_leafBrushes(lf.ColorIndex), -lf.Size, -lf.Size \ 2, lf.Size * 2, lf.Size)
            g.Restore(state)
        Next

        g.SmoothingMode = oldSmoothing
    End Sub

    ' Draws a pixel-art shape described as rectangles in art-pixel units (y counted upward from the feet).
    Private Shared Sub DrawUnit(g As Graphics, c As Color, originX As Integer, feetY As Integer, widthUnits As Integer,
                                facing As Integer, ux As Integer, uy As Integer, uw As Integer, uh As Integer)
        Dim sx As Integer = If(facing >= 0, originX + ux * PX, originX + (widthUnits - ux - uw) * PX)
        Dim sy As Integer = feetY - (uy + uh) * PX
        FillRect(g, c, sx, sy, uw * PX, uh * PX)
    End Sub

    Private Shared Sub DrawRabbit(g As Graphics, c As Critter)
        Dim ox As Integer = CInt(Math.Round(c.X)) - 4 * PX
        Dim fy As Integer = CInt(Math.Round(c.Y - c.ArcY))
        Dim fur As Color = Color.FromArgb(214, 196, 170)
        Dim dark As Color = Color.FromArgb(184, 160, 130)
        FillOval(g, Color.FromArgb(55, 0, 0, 0), c.X - 14, c.Y - 3, 28, 6)

        DrawUnit(g, dark, ox, fy, 8, c.Facing, 1, 0, 2, 1)
        DrawUnit(g, dark, ox, fy, 8, c.Facing, 5, 0, 2, 1)
        DrawUnit(g, fur, ox, fy, 8, c.Facing, 0, 1, 6, 3)
        DrawUnit(g, fur, ox, fy, 8, c.Facing, 5, 2, 3, 3)
        DrawUnit(g, fur, ox, fy, 8, c.Facing, 5, 5, 1, 3)
        DrawUnit(g, fur, ox, fy, 8, c.Facing, 7, 5, 1, 3)
        DrawUnit(g, Color.FromArgb(232, 170, 170), ox, fy, 8, c.Facing, 5, 7, 1, 1)
        DrawUnit(g, Color.FromArgb(250, 250, 245), ox, fy, 8, c.Facing, -1, 2, 1, 1)
        DrawUnit(g, Color.FromArgb(50, 40, 40), ox, fy, 8, c.Facing, 7, 4, 1, 1)
    End Sub

    Private Shared Sub DrawBird(g As Graphics, c As Critter)
        Dim ox As Integer = CInt(Math.Round(c.X)) - 3 * PX
        Dim fy As Integer = CInt(Math.Round(c.Y - c.ArcY))
        FillOval(g, Color.FromArgb(50, 0, 0, 0), c.X - 8, c.Y - 2, 16, 4)

        DrawUnit(g, Color.FromArgb(200, 150, 80), ox, fy, 6, c.Facing, 1, 0, 1, 1)
        DrawUnit(g, Color.FromArgb(200, 150, 80), ox, fy, 6, c.Facing, 3, 0, 1, 1)
        DrawUnit(g, Color.FromArgb(150, 110, 70), ox, fy, 6, c.Facing, 0, 1, 4, 2)
        DrawUnit(g, Color.FromArgb(220, 200, 170), ox, fy, 6, c.Facing, 1, 1, 3, 1)
        DrawUnit(g, Color.FromArgb(150, 110, 70), ox, fy, 6, c.Facing, 3, 2, 2, 2)
        DrawUnit(g, Color.FromArgb(110, 80, 50), ox, fy, 6, c.Facing, -1, 2, 1, 1)
        DrawUnit(g, Color.FromArgb(240, 190, 60), ox, fy, 6, c.Facing, 5, 3, 1, 1)
        DrawUnit(g, Color.FromArgb(40, 30, 30), ox, fy, 6, c.Facing, 4, 3, 1, 1)
    End Sub

    Private Sub DrawButterfly(g As Graphics, bf As Butterfly)
        Dim t As Double = _time * bf.Speed + bf.Phase
        Dim x As Integer = CInt(bf.BaseX + Math.Cos(t) * bf.RadiusX)
        Dim y As Integer = CInt(bf.BaseY + Math.Sin(t * 1.3) * bf.RadiusY)
        Dim flap As Double = Math.Abs(Math.Sin(_time * 14.0 + bf.Phase))
        Dim wingW As Integer = 2 + CInt(flap * 4)
        Dim wing As Color = ButterflyColors(bf.ColorIndex)

        FillRect(g, wing, x - wingW, y - 3, wingW, 4)
        FillRect(g, wing, x + 1, y - 3, wingW, 4)
        FillRect(g, Color.FromArgb(40, wing.R \ 3, wing.G \ 3, wing.B \ 3), x - wingW, y + 1, wingW \ 2 + 1, 2)
        FillRect(g, Color.FromArgb(60, 40, 40), x, y - 4, 1, 7)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        For Each b As SolidBrush In _leafBrushes
            b.Dispose()
        Next
    End Sub

End Class
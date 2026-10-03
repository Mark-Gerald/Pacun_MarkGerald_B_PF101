Option Strict On
Imports System
Imports System.Collections.Generic

Public Enum CBBlockType
    Brick       ' 1 hit,  10 pts
    Stone       ' 2 hits, 20 pts (only on the final hit)
    Gold        ' 1 hit,  50 pts
    Emerald     ' 1 hit,  25 pts + 1 life
End Enum

''' <summary>Plain description of one block (what the level data produces).</summary>
Public Class CannonBallBlockData
    Public Kind As CBBlockType
    Public X As Single
    Public Y As Single
    Public Width As Single
    Public Height As Single
    Public HitPoints As Integer
End Class

''' <summary>
''' The five hand-made levels. Each layout is a grid of 16 columns:
'''   .  empty     B  Brick     S  Stone     G  Gold     E  Emerald
''' To redesign a level, just edit the strings below. Nothing else needs to change.
''' </summary>
Public Class CannonBallLevelData

    Public Const LevelCount As Integer = 5
    Public Const GridColumns As Integer = 16
    Public Const BlockW As Single = 23.0F                          ' 16 columns x 23 = 368 = the playfield width
    Public Const BlockH As Single = 13.0F
    Public Const GridLeft As Single = CannonBallEngine.WallLeft
    Public Const GridTop As Single = 36.0F                         ' free space above row 0 so the ball can slip out

    Public LevelNumber As Integer
    Public Name As String = ""
    Public BallSpeed As Single
    Public ReadOnly Blocks As New List(Of CannonBallBlockData)

    Private Shared ReadOnly LevelNames() As String = {
        "ANCIENT ARCH", "TEMPLE ENTRANCE", "EMERALD SHRINE", "JUNGLE RUINS", "THE GRAND TEMPLE"}

    ' Ball speed per level, world units per second (gets gradually faster).
    Private Shared ReadOnly LevelSpeeds() As Single = {260.0F, 275.0F, 290.0F, 305.0F, 320.0F}

    Private Shared ReadOnly Layouts As String()() = New String()() {
        New String() {                                  ' ---- LEVEL 1: ancient arch (Brick + a few Stone)
            ".....BBBBBB.....",
            "...BBB....BBB...",
            "..BB........BB..",
            ".BB..........BB.",
            ".BS..........SB.",
            ".BB...B..B...BB.",
            ".SB...S..S...BS.",
            ".BB...B..B...BB.",
            ".BB...B..B...BB.",
            "................",
            "SSSS........SSSS"},
        New String() {                                  ' ---- LEVEL 2: temple entrance (Brick + Stone + Gold)
            "..G..........G..",
            ".BBB...GG...BBB.",
            ".BSB..BBBB..BSB.",
            ".BSB.BSSSSB.BSB.",
            ".BBB.BSGGSB.BBB.",
            ".BBB.BSSSSB.BBB.",
            ".BBB..BBBB..BBB.",
            "................",
            "GG............GG",
            "SSS..........SSS"},
        New String() {                                  ' ---- LEVEL 3: emerald shrine (separate structures)
            "................",
            "..BB...EE...BB..",
            "..BS..BGGB..SB..",
            "..SS..SEES..SS..",
            "..BB..BBBB..BB..",
            "................",
            ".G....SSSS....G.",
            "BBB..........BBB",
            ".SS...E..E...SS.",
            ".B............B."},
        New String() {                                  ' ---- LEVEL 4: dense jungle ruins (all four types)
            "BB.BB.BB..BB.BB.",
            "BSBSB..GG..BSBSB",
            ".SSB..BEEB..BSS.",
            "..BB.SSSSSS.BB..",
            "G..B..B..B..B..G",
            "SB.SB......BS.BS",
            ".B..E..GG..E..B.",
            "BBB..SS..SS..BBB",
            ".....G....G.....",
            "SSSS.B....B.SSSS"},
        New String() {                                  ' ---- LEVEL 5: the grand temple (symmetrical)
            ".......GG.......",
            "......GBBG......",
            ".....BSSSSB.....",
            "...G.BSEESB.G...",
            "..BBBBSSSSBBBB..",
            ".BS..........SB.",
            ".BS.GG....GG.SB.",
            ".BS.BB.EE.BB.SB.",
            ".BS.SS.GG.SS.SB.",
            ".BS..........SB.",
            ".BBBB..EE..BBBB.",
            "SSSSS......SSSSS"}
    }

    Public Shared Function HitPointsFor(kind As CBBlockType) As Integer
        If kind = CBBlockType.Stone Then Return 2
        Return 1
    End Function

    Private Shared Function TryGetKind(c As Char, ByRef kind As CBBlockType) As Boolean
        Select Case Char.ToUpperInvariant(c)
            Case "B"c : kind = CBBlockType.Brick : Return True
            Case "S"c : kind = CBBlockType.Stone : Return True
            Case "G"c : kind = CBBlockType.Gold : Return True
            Case "E"c : kind = CBBlockType.Emerald : Return True
        End Select
        Return False
    End Function

    ''' <summary>Builds the level (1 to 5). Out-of-range numbers are clamped.</summary>
    Public Shared Function Create(levelNumber As Integer) As CannonBallLevelData
        Dim n As Integer = Math.Max(1, Math.Min(LevelCount, levelNumber))
        Dim data As New CannonBallLevelData()
        data.LevelNumber = n
        data.Name = LevelNames(n - 1)
        data.BallSpeed = LevelSpeeds(n - 1)

        Dim layout() As String = Layouts(n - 1)
        For row As Integer = 0 To layout.Length - 1
            Dim line As String = layout(row)
            For col As Integer = 0 To Math.Min(line.Length, GridColumns) - 1
                Dim kind As CBBlockType
                If Not TryGetKind(line(col), kind) Then Continue For
                data.Blocks.Add(New CannonBallBlockData With {
                    .Kind = kind,
                    .X = GridLeft + col * BlockW,
                    .Y = GridTop + row * BlockH,
                    .Width = BlockW,
                    .Height = BlockH,
                    .HitPoints = HitPointsFor(kind)})
            Next
        Next
        Return data
    End Function

End Class
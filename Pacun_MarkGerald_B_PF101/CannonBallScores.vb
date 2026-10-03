Option Strict On
Imports System
Imports System.Globalization

''' <summary>
''' Stores the Cannon Ball best score and best (fastest) winning time in a tiny text file
''' under %AppData% (same folder style as JumpKnightScores). Missing/damaged file = no best yet.
''' </summary>
Public Module CannonBallScores

    Private ReadOnly ScoreFilePath As String = IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Pacun_MarkGerald_B_PF101",
        "CannonBallBest.txt")

    Public Sub Load(ByRef bestScore As Integer, ByRef bestTime As Double)
        bestScore = 0
        bestTime = 0.0
        Try
            If IO.File.Exists(ScoreFilePath) Then
                Dim parts() As String = IO.File.ReadAllText(ScoreFilePath).Trim().Split(";"c)
                If parts.Length >= 2 Then
                    Dim s As Integer
                    Dim t As Double
                    If Integer.TryParse(parts(0), s) AndAlso s >= 0 Then bestScore = s
                    If Double.TryParse(parts(1), NumberStyles.Float, CultureInfo.InvariantCulture, t) AndAlso t > 0.0 Then bestTime = t
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine("CannonBallScores: could not read best - " & ex.Message)
        End Try
    End Sub

    Public Sub Save(bestScore As Integer, bestTime As Double)
        Try
            IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(ScoreFilePath))
            IO.File.WriteAllText(ScoreFilePath,
                bestScore.ToString(CultureInfo.InvariantCulture) & ";" & bestTime.ToString("R", CultureInfo.InvariantCulture))
        Catch ex As Exception
            Debug.WriteLine("CannonBallScores: could not save best - " & ex.Message)
        End Try
    End Sub

End Module
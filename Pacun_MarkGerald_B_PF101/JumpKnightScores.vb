''' <summary>
''' Stores the Jump Knight best score in a tiny text file under %AppData%.
''' No database needed. If the file is missing or damaged, the best score is simply 0.
''' </summary>
Public Module JumpKnightScores

    Private ReadOnly ScoreFilePath As String = IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Pacun_MarkGerald_B_PF101",
        "JumpKnightBest.txt")

    Public Function LoadBest() As Integer
        Try
            If IO.File.Exists(ScoreFilePath) Then
                Dim value As Integer
                If Integer.TryParse(IO.File.ReadAllText(ScoreFilePath).Trim(), value) AndAlso value >= 0 Then
                    Return value
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine("JumpKnightScores: could not read best score - " & ex.Message)
        End Try
        Return 0
    End Function

    Public Sub SaveBest(score As Integer)
        Try
            IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(ScoreFilePath))
            IO.File.WriteAllText(ScoreFilePath, score.ToString())
        Catch ex As Exception
            Debug.WriteLine("JumpKnightScores: could not save best score - " & ex.Message)
        End Try
    End Sub

End Module
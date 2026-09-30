''' <summary>Demonstrates basic procedures/methods: takes grades in, computes an average and a result.</summary>
Public Class GradeCalculator

    Private studentNameValue As String
    Private gradeList As New List(Of Double)

    Public Sub New(studentName As String)
        studentNameValue = studentName
    End Sub

    Public ReadOnly Property StudentName As String
        Get
            Return studentNameValue
        End Get
    End Property

    Public Sub AddGrade(grade As Double)
        gradeList.Add(grade)
    End Sub

    Public ReadOnly Property GradeCount As Integer
        Get
            Return gradeList.Count
        End Get
    End Property

    Public Function ComputeAverage() As Double
        If gradeList.Count = 0 Then Return 0
        Dim total As Double = 0
        For Each g As Double In gradeList
            total += g
        Next
        Return total / gradeList.Count
    End Function

    Public Function GetResultLabel() As String
        If ComputeAverage() >= 75 Then
            Return "Passed"
        Else
            Return "Needs Improvement"
        End If
    End Function

End Class
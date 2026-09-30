''' <summary>
''' A simple class representing a Student. Demonstrates the basic OOP idea that
''' a class is a blueprint, and an object (a specific Student) is created from it.
''' </summary>
Public Class Student

    Private studentName As String
    Private idNumber As String
    Private studentAge As Integer
    Private studentCourse As String

    Public Sub New(name As String, id As String, age As Integer, course As String)
        studentName = name
        idNumber = id
        studentAge = age
        studentCourse = course
    End Sub

    Public ReadOnly Property Name As String
        Get
            Return studentName
        End Get
    End Property

    Public ReadOnly Property StudentID As String
        Get
            Return idNumber
        End Get
    End Property

    Public ReadOnly Property Age As Integer
        Get
            Return studentAge
        End Get
    End Property

    Public ReadOnly Property Course As String
        Get
            Return studentCourse
        End Get
    End Property

End Class
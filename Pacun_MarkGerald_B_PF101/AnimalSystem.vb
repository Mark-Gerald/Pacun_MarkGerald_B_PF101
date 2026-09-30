Public MustInherit Class Animal

    Protected petName As String

    Public Sub New(nameInput As String)
        petName = nameInput
    End Sub

    Public ReadOnly Property AnimalName As String
        Get
            Return petName
        End Get
    End Property

    Public Overridable Function Speak() As String
        Return petName & " makes a sound."
    End Function

End Class

Public Class Dog
    Inherits Animal

    Public Sub New(nameInput As String)
        MyBase.New(nameInput)
    End Sub

    Public Overrides Function Speak() As String
        Return petName & " says: Woof! Woof!"
    End Function

End Class

Public Class Cat
    Inherits Animal

    Public Sub New(nameInput As String)
        MyBase.New(nameInput)
    End Sub

    Public Overrides Function Speak() As String
        Return petName & " says: Meow!"
    End Function

End Class

Public Class Bird
    Inherits Animal

    Public Sub New(nameInput As String)
        MyBase.New(nameInput)
    End Sub

    Public Overrides Function Speak() As String
        Return petName & " says: Tweet tweet!"
    End Function

End Class
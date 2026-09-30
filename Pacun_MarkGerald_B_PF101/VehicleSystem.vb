Public MustInherit Class Vehicle

    Protected vehicleBrand As String
    Protected vehicleModel As String
    Protected vehicleWheels As Integer

    Public Sub New(brandInput As String, modelInput As String, wheelsInput As Integer)
        vehicleBrand = brandInput
        vehicleModel = modelInput
        vehicleWheels = wheelsInput
    End Sub

    Public ReadOnly Property Brand As String
        Get
            Return vehicleBrand
        End Get
    End Property

    Public ReadOnly Property Model As String
        Get
            Return vehicleModel
        End Get
    End Property

    Public ReadOnly Property Wheels As Integer
        Get
            Return vehicleWheels
        End Get
    End Property

    Public Overridable Function GetInfo() As String
        Return "Brand: " & vehicleBrand & vbCrLf & "Model: " & vehicleModel & vbCrLf & "Wheels: " & vehicleWheels
    End Function

End Class

Public Class Car
    Inherits Vehicle

    Protected carTrunkCapacity As Integer

    Public Sub New(brandInput As String, modelInput As String, trunkInput As Integer)
        MyBase.New(brandInput, modelInput, 4)
        carTrunkCapacity = trunkInput
    End Sub

    Public ReadOnly Property TrunkCapacity As Integer
        Get
            Return carTrunkCapacity
        End Get
    End Property

    Public Overrides Function GetInfo() As String
        Return MyBase.GetInfo() & vbCrLf & "Trunk Capacity: " & carTrunkCapacity & " L" & vbCrLf & "Type: Car"
    End Function

End Class

Public Class Motorcycle
    Inherits Vehicle

    Protected motorcycleHasSidecar As Boolean

    Public Sub New(brandInput As String, modelInput As String, sidecarInput As Boolean)
        MyBase.New(brandInput, modelInput, 2)
        motorcycleHasSidecar = sidecarInput
    End Sub

    Public ReadOnly Property HasSidecar As Boolean
        Get
            Return motorcycleHasSidecar
        End Get
    End Property

    Public Overrides Function GetInfo() As String
        Return MyBase.GetInfo() & vbCrLf & "Has Sidecar: " & If(motorcycleHasSidecar, "Yes", "No") & vbCrLf & "Type: Motorcycle"
    End Function

End Class
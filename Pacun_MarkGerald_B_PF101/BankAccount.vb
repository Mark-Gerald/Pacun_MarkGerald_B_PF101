''' <summary>
''' A minimal bank account used to demonstrate encapsulation for Week 2.
''' The balance is private and can only change through Deposit()/Withdraw() —
''' there is no way for outside code to set it directly.
''' </summary>
Public Class BankAccount

    Private accountHolderName As String
    Private balance As Decimal

    Public Sub New(holderName As String)
        accountHolderName = holderName
        balance = 0D ' every account always starts at ₱0.00
    End Sub

    ' Read-only from the outside — callers can see the name but can't just assign a new one
    ' without creating a fresh account, matching "new name = new account" from the spec.
    Public ReadOnly Property HolderName As String
        Get
            Return accountHolderName
        End Get
    End Property

    ' Read-only from the outside — this is the encapsulation being demonstrated:
    ' there is no public Set for this. The only way to change it is Deposit()/Withdraw().
    Public ReadOnly Property CurrentBalance As Decimal
        Get
            Return balance
        End Get
    End Property

    ''' <summary>Adds a positive amount to the balance. Returns True if the deposit succeeded.</summary>
    Public Function Deposit(amount As Decimal) As Boolean
        If amount <= 0D Then
            Return False
        End If
        balance += amount
        Return True
    End Function

    ''' <summary>
    ''' Subtracts a positive amount from the balance, but only if there's enough money.
    ''' Returns True if the withdrawal succeeded, False if it was refused
    ''' (invalid amount or insufficient balance) — the balance is left unchanged on failure.
    ''' </summary>
    Public Function Withdraw(amount As Decimal) As Boolean
        If amount <= 0D Then
            Return False
        End If
        If amount > balance Then
            Return False
        End If
        balance -= amount
        Return True
    End Function

End Class
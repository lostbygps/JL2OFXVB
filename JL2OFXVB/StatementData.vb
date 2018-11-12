Public Class StatementData
    Private DateProcessedP As System.DateTime, DescriptionP As String, AmountP As Double, CreditP As Boolean

    Public Sub New(ByVal DateProcessed As String, ByVal Description As String, ByVal Amount As String, ByVal Credit As String)
        Dim Regex As New Text.RegularExpressions.Regex("[^\d.]")
        DateProcessedP = System.DateTime.Parse(DateProcessed)
        DescriptionP = Description
        AmountP = Convert.ToDouble(Regex.Replace(Amount, ""))
        If Credit = "CR" Then
            AmountP = AmountP * -1
            CreditP = True
        Else
            CreditP = False
        End If
    End Sub

    Public Property DateProcessed() As System.DateTime
        Get
            Return DateProcessedP
        End Get
        Set(ByVal Value As System.DateTime)
            DateProcessedP = Value
        End Set
    End Property

    Public Property Description() As String
        Get
            Return DescriptionP
        End Get
        Set(ByVal Value As String)
            DescriptionP = Value
        End Set
    End Property
    Public Property Amount() As Double
        Get
            Return AmountP
        End Get
        Set(ByVal Value As Double)
            AmountP = Value
        End Set
    End Property

    Public Property Credit() As Boolean
        Get
            Return CreditP
        End Get
        Set(ByVal Value As Boolean)
            CreditP = Value
        End Set
    End Property
End Class

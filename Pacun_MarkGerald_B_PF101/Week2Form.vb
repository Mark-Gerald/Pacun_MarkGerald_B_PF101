Public Class Week2Form
    Inherits Form

    Private leftScrollPanel As Panel
    Private leftSlideStack As FlowLayoutPanel
    Private rightPanel As Panel

    Private currentAccount As BankAccount
    Private holderTextBox As TextBox
    Private balanceValueLabel As Label
    Private amountTextBox As TextBox
    Private transactionPanel As Panel
    Private transactionIconLabel As Label
    Private transactionMessageLabel As Label

    Private Enum TransactionState
        Neutral
        Success
        Failure
    End Enum

    Public Sub New()
        Me.Text = "Week 2 " & ChrW(8212) & " Encapsulation " & ChrW(8212) & " Object-Oriented Programming in Visual Basic .NET"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White

        BuildHeader()
        BuildBody()
        BuildBottomNav()
    End Sub

    Private Sub BuildHeader()
        Dim headerPanel As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = Color.White,
            .Padding = New Padding(24, 16, 24, 0)
        }
        Me.Controls.Add(headerPanel)

        Dim weekBadge As New Label() With {
            .Text = "Week 2 " & ChrW(8212) & " Encapsulation",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .BackColor = Color.FromArgb(238, 238, 242),
            .AutoSize = True,
            .Padding = New Padding(10, 4, 10, 4),
            .Location = New Point(24, 12)
        }
        headerPanel.Controls.Add(weekBadge)
        AddHandler weekBadge.Resize, Sub(s, ev) ApplyRoundedCorners(weekBadge, 10)

        Dim titleLbl As New Label() With {
            .Text = "Encapsulation",
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(24, weekBadge.Bottom + 6)
        }
        headerPanel.Controls.Add(titleLbl)

        Dim bottomBorder As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 1,
            .BackColor = Color.FromArgb(230, 230, 230)
        }
        headerPanel.Controls.Add(bottomBorder)
    End Sub

    Private Sub BuildBody()
        Dim bodyTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Padding = New Padding(24, 16, 24, 16),
            .BackColor = Color.White
        }
        bodyTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 65.0F))
        bodyTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35.0F))
        Me.Controls.Add(bodyTable)
        bodyTable.BringToFront()

        ' --- LEFT: scrollable lesson/PPT area (its own scrollbar) ---
        leftScrollPanel = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = Color.White,
            .Margin = New Padding(0, 0, 12, 0),
            .BorderStyle = BorderStyle.FixedSingle
        }
        bodyTable.Controls.Add(leftScrollPanel, 0, 0)

        leftSlideStack = New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Padding = New Padding(16),
            .Margin = New Padding(0)
        }
        leftScrollPanel.Controls.Add(leftSlideStack)

        LoadWeek2Slides()

        Dim centerLeftStack As Action = Sub()
                                            Dim x As Integer = Math.Max(0, (leftScrollPanel.ClientSize.Width - leftSlideStack.Width) \ 2)
                                            leftSlideStack.Left = x
                                        End Sub
        AddHandler leftScrollPanel.Resize, Sub(s, ev) centerLeftStack()
        AddHandler leftSlideStack.Resize, Sub(s, ev) centerLeftStack()
        centerLeftStack()

        ' --- RIGHT: interactive example area (fixed, not scrollable) ---
        rightPanel = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Margin = New Padding(12, 0, 0, 0),
            .Padding = New Padding(8)
        }
        bodyTable.Controls.Add(rightPanel, 1, 0)

        PopulateMiniBankAccount()
    End Sub

    ' Builds the entire Mini Bank Account card UI inside rightPanel and wires it to BankAccount.
    Private Sub PopulateMiniBankAccount()
        Dim sectionLabel As New Label() With {
            .Text = "INTERACTIVE EXAMPLE",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(120, 120, 120),
            .Dock = DockStyle.Top,
            .Height = 24,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        rightPanel.Controls.Add(sectionLabel)

        Dim cardOuter As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White
        }
        rightPanel.Controls.Add(cardOuter)
        AddHandler cardOuter.Resize, Sub(s, ev) ApplyRoundedCorners(cardOuter, 10)
        AddHandler cardOuter.Paint, Sub(s, pe)
                                        Dim r As New Rectangle(0, 0, cardOuter.Width - 1, cardOuter.Height - 1)
                                        pe.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                        Using pen As New Pen(Color.FromArgb(225, 225, 230), 1)
                                            pe.Graphics.DrawRectangle(pen, r)
                                        End Using
                                    End Sub

        Dim cardStack As New FlowLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = False,
            .Padding = New Padding(16),
            .Margin = New Padding(0),
            .BackColor = Color.White
        }
        cardOuter.Controls.Add(cardStack)

        ' --- header bar ---
        Dim headerBar As New Panel() With {
            .Height = 48,
            .BackColor = Color.FromArgb(24, 24, 30),
            .Margin = New Padding(0, 0, 0, 16)
        }
        Dim headerLbl As New Label() With {
            .Text = "MINI BANK ACCOUNT",
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(14, 0, 0, 0)
        }
        headerBar.Controls.Add(headerLbl)
        cardStack.Controls.Add(headerBar)

        ' --- account holder ---
        Dim holderCaption As New Label() With {
            .Text = "ACCOUNT HOLDER",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 4)
        }
        cardStack.Controls.Add(holderCaption)

        holderTextBox = New TextBox() With {
            .Text = "Juan Dela Cruz",
            .Font = New Font("Segoe UI", 10.0F),
            .Margin = New Padding(0, 0, 0, 16)
        }
        AddHandler holderTextBox.TextChanged, AddressOf HolderTextBox_TextChanged
        cardStack.Controls.Add(holderTextBox)

        ' --- balance box ---
        Dim balanceBox As New Panel() With {
            .BackColor = Color.FromArgb(247, 247, 249),
            .Margin = New Padding(0, 0, 0, 16),
            .Height = 110
        }
        cardStack.Controls.Add(balanceBox)

        Dim balanceCaption As New Label() With {
            .Text = "CURRENT BALANCE",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(14, 12)
        }
        balanceBox.Controls.Add(balanceCaption)

        Dim protectedBadge As New Label() With {
            .Text = "Protected field",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(90, 90, 90),
            .BackColor = Color.FromArgb(236, 236, 240),
            .AutoSize = True,
            .Padding = New Padding(8, 3, 8, 3)
        }
        balanceBox.Controls.Add(protectedBadge)
        AddHandler protectedBadge.Resize, Sub(s, ev) ApplyRoundedCorners(protectedBadge, 8)

        balanceValueLabel = New Label() With {
            .Text = FormatPeso(0D),
            .Font = New Font("Segoe UI", 22.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(14, 32)
        }
        balanceBox.Controls.Add(balanceValueLabel)

        Dim balanceDesc As New Label() With {
            .Text = "The balance is private " & ChrW(8212) & " it can only change through the Deposit() and Withdraw() methods.",
            .Font = New Font("Segoe UI", 8.0F),
            .ForeColor = Color.Gray,
            .AutoSize = False,
            .Location = New Point(14, balanceValueLabel.Bottom + 6),
            .Size = New Size(200, 32)
        }
        balanceBox.Controls.Add(balanceDesc)

        AddHandler balanceBox.Resize, Sub(s, ev)
                                          protectedBadge.Location = New Point(balanceBox.Width - protectedBadge.Width - 14, 10)
                                          balanceDesc.Width = Math.Max(60, balanceBox.Width - 28)
                                      End Sub

        ' --- amount ---
        Dim amountCaption As New Label() With {
            .Text = "AMOUNT",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 4)
        }
        cardStack.Controls.Add(amountCaption)

        amountTextBox = New TextBox() With {
            .Text = "",
            .Font = New Font("Segoe UI", 10.0F),
            .Margin = New Padding(0, 0, 0, 16)
        }
        cardStack.Controls.Add(amountTextBox)

        ' --- Deposit / Withdraw buttons ---
        Dim buttonsRow As New Panel() With {
            .Height = 44,
            .Margin = New Padding(0, 0, 0, 16)
        }
        cardStack.Controls.Add(buttonsRow)

        Dim depositBtn As New Button() With {
            .Text = ChrW(8595) & "  Deposit",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Height = 44
        }
        depositBtn.FlatAppearance.BorderSize = 0
        AddHandler depositBtn.Click, AddressOf DepositButton_Click
        buttonsRow.Controls.Add(depositBtn)

        Dim withdrawBtn As New Button() With {
            .Text = ChrW(8593) & "  Withdraw",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Height = 44
        }
        withdrawBtn.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210)
        withdrawBtn.FlatAppearance.BorderSize = 1
        AddHandler withdrawBtn.Click, AddressOf WithdrawButton_Click
        buttonsRow.Controls.Add(withdrawBtn)

        AddHandler buttonsRow.Resize, Sub(s, ev)
                                          Dim gap As Integer = 10
                                          Dim halfW As Integer = (buttonsRow.Width - gap) \ 2
                                          depositBtn.Width = halfW
                                          withdrawBtn.Width = buttonsRow.Width - halfW - gap
                                          depositBtn.Location = New Point(0, 0)
                                          withdrawBtn.Location = New Point(depositBtn.Right + gap, 0)
                                      End Sub

        ' --- transaction message ---
        transactionPanel = New Panel() With {
            .BackColor = Color.FromArgb(247, 247, 249),
            .Margin = New Padding(0, 0, 0, 16),
            .Height = 56
        }
        cardStack.Controls.Add(transactionPanel)

        transactionIconLabel = New Label() With {
            .Text = ChrW(8505),
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(12, 8)
        }
        transactionPanel.Controls.Add(transactionIconLabel)

        Dim transactionCaption As New Label() With {
            .Text = "TRANSACTION",
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(34, 8)
        }
        transactionPanel.Controls.Add(transactionCaption)

        transactionMessageLabel = New Label() With {
            .Text = "Ready to make a transaction.",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(50, 50, 50),
            .AutoSize = True,
            .Location = New Point(34, 26)
        }
        transactionPanel.Controls.Add(transactionMessageLabel)

        ' --- footer hint ---
        Dim footerHint As New Label() With {
            .Text = "The balance field is encapsulated. Use the buttons to call the Deposit() / Withdraw() methods and watch how the protected balance responds.",
            .Font = New Font("Segoe UI", 8.0F),
            .ForeColor = Color.Gray,
            .AutoSize = False,
            .Size = New Size(200, 40)
        }
        cardStack.Controls.Add(footerHint)

        ' Keep every full-width child in sync with the card's actual width as the window resizes.
        Dim adjustCardWidths As Action = Sub()
                                             Dim innerWidth As Integer = Math.Max(60, cardStack.ClientSize.Width - cardStack.Padding.Left - cardStack.Padding.Right)
                                             headerBar.Width = innerWidth
                                             holderTextBox.Width = innerWidth
                                             balanceBox.Width = innerWidth
                                             amountTextBox.Width = innerWidth
                                             buttonsRow.Width = innerWidth
                                             transactionPanel.Width = innerWidth
                                             footerHint.Width = innerWidth
                                         End Sub
        AddHandler cardStack.Resize, Sub(s, ev) adjustCardWidths()
        adjustCardWidths()

        ' Start the demo account fresh, matching the textbox's default holder name.
        currentAccount = New BankAccount(holderTextBox.Text)
        RefreshBalanceDisplay()
    End Sub

    ' Changing the account holder starts a brand-new demo account -- always resets to zero.
    Private Sub HolderTextBox_TextChanged(sender As Object, e As EventArgs)
        currentAccount = New BankAccount(holderTextBox.Text)
        RefreshBalanceDisplay()
        ShowTransactionMessage("Ready to make a transaction.", TransactionState.Neutral)
    End Sub

    Private Sub DepositButton_Click(sender As Object, e As EventArgs)
        Dim amount As Decimal
        If Not Decimal.TryParse(amountTextBox.Text, amount) OrElse amount <= 0D Then
            ShowTransactionMessage("Please enter a valid amount to deposit.", TransactionState.Failure)
            Return
        End If

        If currentAccount.Deposit(amount) Then
            RefreshBalanceDisplay()
            ShowTransactionMessage(FormatPeso(amount) & " deposited successfully.", TransactionState.Success)
        Else
            ShowTransactionMessage("Please enter a valid amount to deposit.", TransactionState.Failure)
        End If
    End Sub

    Private Sub WithdrawButton_Click(sender As Object, e As EventArgs)
        Dim amount As Decimal
        If Not Decimal.TryParse(amountTextBox.Text, amount) OrElse amount <= 0D Then
            ShowTransactionMessage("Please enter a valid amount to withdraw.", TransactionState.Failure)
            Return
        End If

        If currentAccount.Withdraw(amount) Then
            RefreshBalanceDisplay()
            ShowTransactionMessage(FormatPeso(amount) & " withdrawn successfully.", TransactionState.Success)
        Else
            ShowTransactionMessage("Insufficient balance. Withdrawal refused.", TransactionState.Failure)
        End If
    End Sub

    Private Sub RefreshBalanceDisplay()
        balanceValueLabel.Text = FormatPeso(currentAccount.CurrentBalance)
    End Sub

    Private Sub ShowTransactionMessage(message As String, state As TransactionState)
        transactionMessageLabel.Text = message
        Select Case state
            Case TransactionState.Success
                transactionPanel.BackColor = Color.FromArgb(230, 247, 237)
                transactionMessageLabel.ForeColor = Color.FromArgb(30, 120, 70)
                transactionIconLabel.ForeColor = Color.FromArgb(30, 120, 70)
            Case TransactionState.Failure
                transactionPanel.BackColor = Color.FromArgb(253, 235, 235)
                transactionMessageLabel.ForeColor = Color.FromArgb(180, 40, 40)
                transactionIconLabel.ForeColor = Color.FromArgb(180, 40, 40)
            Case Else
                transactionPanel.BackColor = Color.FromArgb(247, 247, 249)
                transactionMessageLabel.ForeColor = Color.FromArgb(50, 50, 50)
                transactionIconLabel.ForeColor = Color.Gray
        End Select
    End Sub

    Private Function FormatPeso(amount As Decimal) As String
        Return ChrW(8369) & amount.ToString("N2")
    End Function

    ' Loads the selected Week 2 slide PNGs, in order, as full-width images
    ' that preserve their original aspect ratio (no stretching/distortion).
    Private Sub LoadWeek2Slides()
        Dim folderPath As String = IO.Path.Combine(Application.StartupPath, "LessonContent", "Week02_IntroToOOP", "Slides")

        If Not IO.Directory.Exists(folderPath) Then
            leftSlideStack.Controls.Add(New Label() With {
                .Text = "Slides not found at:" & vbCrLf & folderPath,
                .ForeColor = Color.Red,
                .AutoSize = True
            })
            Return
        End If

        Dim files = IO.Directory.GetFiles(folderPath, "*.png")
        Array.Sort(files) ' keeps week2_slide_01, 02, 03, 04, 05 in order

        If files.Length = 0 Then
            leftSlideStack.Controls.Add(New Label() With {
                .Text = "No PNG files found in:" & vbCrLf & folderPath,
                .ForeColor = Color.Red,
                .AutoSize = True
            })
            Return
        End If

        Const displayWidth As Integer = 700

        For Each filePath In files
            ' Load via a MemoryStream so the file isn't locked while the app is running.
            Dim bytes As Byte() = IO.File.ReadAllBytes(filePath)
            Dim ms As New IO.MemoryStream(bytes)
            Dim img As Image = Image.FromStream(ms)

            ' Preserve aspect ratio: scale height based on the fixed display width.
            Dim scaledHeight As Integer = CInt(displayWidth * (img.Height / CSng(img.Width)))

            Dim pic As New PictureBox() With {
                .Image = img,
                .SizeMode = PictureBoxSizeMode.Zoom,
                .Size = New Size(displayWidth, scaledHeight),
                .Margin = New Padding(0, 0, 0, 20),
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle
            }
            leftSlideStack.Controls.Add(pic)
        Next
    End Sub

    Private Sub BuildBottomNav()
        Dim navPanel As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 60,
            .BackColor = Color.White
        }
        Me.Controls.Add(navPanel)

        Dim topBorder As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 1,
            .BackColor = Color.FromArgb(230, 230, 230)
        }
        navPanel.Controls.Add(topBorder)

        Dim backBtn As New Button() With {
            .Text = ChrW(8592) & "  Back",
            .AutoSize = True,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Padding = New Padding(12, 8, 12, 8)
        }
        backBtn.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210)
        backBtn.FlatAppearance.BorderSize = 1
        AddHandler backBtn.Click, AddressOf BackButton_Click
        navPanel.Controls.Add(backBtn)

        Dim continueBtn As New Button() With {
            .Text = "Continue  " & ChrW(8594),
            .AutoSize = True,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Padding = New Padding(12, 8, 12, 8)
        }
        continueBtn.FlatAppearance.BorderSize = 0
        AddHandler continueBtn.Click, AddressOf ContinueButton_Click
        navPanel.Controls.Add(continueBtn)

        Dim progressLbl As New Label() With {
            .Text = "Week 2 of 19",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.Gray,
            .AutoSize = True
        }
        navPanel.Controls.Add(progressLbl)

        Dim positionNavControls As Action = Sub()
                                                backBtn.Location = New Point(24, (navPanel.Height - backBtn.Height) \ 2)
                                                continueBtn.Location = New Point(navPanel.Width - continueBtn.Width - 24, (navPanel.Height - continueBtn.Height) \ 2)
                                                progressLbl.Location = New Point((navPanel.Width - progressLbl.Width) \ 2, (navPanel.Height - progressLbl.Height) \ 2)
                                            End Sub
        AddHandler navPanel.Resize, Sub(s, ev) positionNavControls()
        positionNavControls()
    End Sub

    Private Sub BackButton_Click(sender As Object, e As EventArgs)
        Dim classesWindow As New Week2ClassesAndObjectsForm()
        classesWindow.Show()
        Me.Close()
    End Sub

    Private Sub ContinueButton_Click(sender As Object, e As EventArgs)
        Dim inheritanceWindow As New Week2InheritanceForm()
        inheritanceWindow.Show()
        Me.Close()
    End Sub

    ' Local helper (kept private to this form, same pattern as Form1's own copy).
    Private Sub ApplyRoundedCorners(ctrl As Control, radius As Integer)
        Try
            If ctrl Is Nothing OrElse ctrl.Width <= 0 OrElse ctrl.Height <= 0 Then Return
            Dim path As New Drawing2D.GraphicsPath()
            Dim r As Integer = radius
            Dim rect As New Rectangle(0, 0, ctrl.Width, ctrl.Height)
            path.StartFigure()
            path.AddArc(rect.Left, rect.Top, r, r, 180, 90)
            path.AddArc(rect.Right - r, rect.Top, r, r, 270, 90)
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90)
            path.AddArc(rect.Left, rect.Bottom - r, r, r, 90, 90)
            path.CloseFigure()
            ctrl.Region = New Region(path)
        Catch
        End Try
    End Sub

End Class
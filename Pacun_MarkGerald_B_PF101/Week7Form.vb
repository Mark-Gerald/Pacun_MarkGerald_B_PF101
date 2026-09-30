Public Class Week7Form
    Inherits Form

    ' ===== Layout =====
    Private leftScrollPanel As Panel
    Private leftSlideStack As FlowLayoutPanel
    Private rightPanel As Panel

    Private ReadOnly months() As String = {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    }

    Private monthsListBox As ListBox
    Private selectedNameValueLabel As Label
    Private selectedNumberValueLabel As Label
    Private selectedIndexValueLabel As Label
    Private searchTextBox As TextBox
    Private statusPanel As Panel
    Private statusIconLabel As Label
    Private statusMessageLabel As Label

    Private Enum ActivityState
        Neutral
        Success
        Failure
    End Enum

    Public Sub New()
        Me.Text = "Week 7 " & ChrW(8212) & " Arrays " & ChrW(8212) & " Object-Oriented Programming in Visual Basic .NET"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White

        BuildHeader()
        BuildBody()
        BuildBottomNav()
    End Sub

    ' ==================================================
    ' FORM INITIALIZATION
    ' ==================================================
    Private Sub BuildHeader()
        Dim headerPanel As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = Color.White,
            .Padding = New Padding(24, 16, 24, 0)
        }
        Me.Controls.Add(headerPanel)

        Dim weekBadge As New Label() With {
            .Text = "Week 7 " & ChrW(8212) & " Arrays",
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
            .Text = "Arrays",
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

        LoadSlides()

        Dim centerLeftStack As Action = Sub()
                                            Dim x As Integer = Math.Max(0, (leftScrollPanel.ClientSize.Width - leftSlideStack.Width) \ 2)
                                            leftSlideStack.Left = x
                                        End Sub
        AddHandler leftScrollPanel.Resize, Sub(s, ev) centerLeftStack()
        AddHandler leftSlideStack.Resize, Sub(s, ev) centerLeftStack()
        centerLeftStack()

        rightPanel = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = Color.White,
            .Margin = New Padding(12, 0, 0, 0),
            .Padding = New Padding(8)
        }
        bodyTable.Controls.Add(rightPanel, 1, 0)

        PopulateMonthListActivity()
    End Sub

    ' ==================================================
    ' MONTH LIST -- ARRAY EXPLORER (Week 7 mini-project)
    ' ==================================================
    Private Sub PopulateMonthListActivity()
        Dim sectionLabel As New Label() With {
            .Text = "INTERACTIVE EXAMPLE",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(120, 120, 120),
            .Dock = DockStyle.Top,
            .Height = 24,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        rightPanel.Controls.Add(sectionLabel)

        Dim cardStack As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Padding = New Padding(16),
            .Margin = New Padding(0),
            .BackColor = Color.White
        }
        rightPanel.Controls.Add(cardStack)
        AddHandler cardStack.Paint, Sub(s, pe)
                                        Dim r As New Rectangle(0, 0, cardStack.Width - 1, cardStack.Height - 1)
                                        pe.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                        Using pen As New Pen(Color.FromArgb(225, 225, 230), 1)
                                            pe.Graphics.DrawRectangle(pen, r)
                                        End Using
                                    End Sub

        Dim headerBar As New Panel() With {.Height = 48, .BackColor = Color.FromArgb(24, 24, 30), .Margin = New Padding(0, 0, 0, 16)}
        Dim headerLbl As New Label() With {
            .Text = "MONTH LIST " & ChrW(8212) & " ARRAY EXPLORER",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(14, 0, 0, 0)
        }
        headerBar.Controls.Add(headerLbl)
        cardStack.Controls.Add(headerBar)

        ' --- ListBox of months ---
        Dim listCaption As New Label() With {.Text = "MONTHS ARRAY", .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold), .ForeColor = Color.Gray, .AutoSize = True, .Margin = New Padding(0, 0, 0, 4)}
        cardStack.Controls.Add(listCaption)

        monthsListBox = New ListBox() With {
            .Font = New Font("Segoe UI", 9.5F),
            .Height = 180,
            .IntegralHeight = False,
            .Margin = New Padding(0, 0, 0, 12)
        }
        AddHandler monthsListBox.SelectedIndexChanged, AddressOf MonthsListBox_SelectedIndexChanged
        cardStack.Controls.Add(monthsListBox)
        FillMonthsListBox()

        ' --- Selected month details ---
        Dim detailsPanel As New Panel() With {.BackColor = Color.FromArgb(247, 247, 249), .Margin = New Padding(0, 0, 0, 16), .Height = 96}
        cardStack.Controls.Add(detailsPanel)

        Dim detailsCaption As New Label() With {.Text = "SELECTED MONTH DETAILS", .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold), .ForeColor = Color.Gray, .AutoSize = True, .Location = New Point(14, 10)}
        detailsPanel.Controls.Add(detailsCaption)

        selectedNameValueLabel = New Label() With {.Text = "Selected Month: (none)", .Font = New Font("Segoe UI", 9.0F), .ForeColor = Color.FromArgb(40, 40, 40), .AutoSize = True, .Location = New Point(14, 30)}
        detailsPanel.Controls.Add(selectedNameValueLabel)

        selectedNumberValueLabel = New Label() With {.Text = "Month Number: -", .Font = New Font("Segoe UI", 9.0F), .ForeColor = Color.FromArgb(40, 40, 40), .AutoSize = True, .Location = New Point(14, 52)}
        detailsPanel.Controls.Add(selectedNumberValueLabel)

        selectedIndexValueLabel = New Label() With {.Text = "Array Index: -", .Font = New Font("Segoe UI", 9.0F), .ForeColor = Color.FromArgb(40, 40, 40), .AutoSize = True, .Location = New Point(14, 74)}
        detailsPanel.Controls.Add(selectedIndexValueLabel)

        ' --- Search ---
        Dim searchCaption As New Label() With {.Text = "SEARCH MONTH", .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold), .ForeColor = Color.Gray, .AutoSize = True, .Margin = New Padding(0, 0, 0, 4)}
        cardStack.Controls.Add(searchCaption)

        Dim searchRow As New Panel() With {.Height = 34, .Margin = New Padding(0, 0, 0, 10)}
        searchTextBox = New TextBox() With {.Font = New Font("Segoe UI", 9.5F), .Location = New Point(0, 2), .Width = 150}
        searchRow.Controls.Add(searchTextBox)
        Dim searchBtn As New Button() With {
            .Text = "Search",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Height = 28,
            .Location = New Point(160, 0)
        }
        searchBtn.FlatAppearance.BorderSize = 0
        AddHandler searchBtn.Click, AddressOf SearchButton_Click
        searchRow.Controls.Add(searchBtn)
        cardStack.Controls.Add(searchRow)

        ' --- Restore order ---
        Dim restoreBtn As New Button() With {
            .Text = "Restore Original Order",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Height = 38,
            .Margin = New Padding(0, 0, 0, 16)
        }
        restoreBtn.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210)
        restoreBtn.FlatAppearance.BorderSize = 1
        AddHandler restoreBtn.Click, AddressOf RestoreButton_Click
        cardStack.Controls.Add(restoreBtn)

        ' --- Status line ---
        statusPanel = New Panel() With {.BackColor = Color.FromArgb(247, 247, 249), .Height = 40}
        cardStack.Controls.Add(statusPanel)

        statusIconLabel = New Label() With {.Text = ChrW(8505), .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold), .ForeColor = Color.Gray, .AutoSize = True, .Location = New Point(10, 10)}
        statusPanel.Controls.Add(statusIconLabel)

        statusMessageLabel = New Label() With {
            .Text = "Select a month, or search for one above.",
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.FromArgb(50, 50, 50),
            .AutoSize = True,
            .Location = New Point(30, 11)
        }
        statusPanel.Controls.Add(statusMessageLabel)

        Dim adjustCardWidths As Action = Sub()
                                             Dim innerWidth As Integer = Math.Max(60, cardStack.ClientSize.Width - cardStack.Padding.Left - cardStack.Padding.Right)
                                             headerBar.Width = innerWidth
                                             monthsListBox.Width = innerWidth
                                             detailsPanel.Width = innerWidth
                                             searchRow.Width = innerWidth
                                             restoreBtn.Width = innerWidth
                                             statusPanel.Width = innerWidth
                                         End Sub
        AddHandler cardStack.Resize, Sub(s, ev) adjustCardWidths()
        adjustCardWidths()
    End Sub

    ' Populates the ListBox from the months array using a loop -- January stays first,
    ' December stays last, matching the required top-to-bottom order.
    Private Sub FillMonthsListBox()
        monthsListBox.Items.Clear()
        For i As Integer = 0 To months.Length - 1
            monthsListBox.Items.Add(months(i))
        Next
    End Sub

    Private Sub MonthsListBox_SelectedIndexChanged(sender As Object, e As EventArgs)
        If monthsListBox.SelectedIndex = -1 Then
            selectedNameValueLabel.Text = "Selected Month: (none)"
            selectedNumberValueLabel.Text = "Month Number: -"
            selectedIndexValueLabel.Text = "Array Index: -"
            Return
        End If

        Dim selectedIndex As Integer = monthsListBox.SelectedIndex
        Dim selectedName As String = months(selectedIndex)
        Dim monthNumber As Integer = selectedIndex + 1 ' January = 1, ... December = 12

        selectedNameValueLabel.Text = "Selected Month: " & selectedName
        selectedNumberValueLabel.Text = "Month Number: " & monthNumber
        selectedIndexValueLabel.Text = "Array Index: " & selectedIndex
    End Sub

    Private Sub SearchButton_Click(sender As Object, e As EventArgs)
        Dim searchTerm As String = searchTextBox.Text.Trim()

        If searchTerm = "" Then
            SetActivityStatus("Please type a month name to search.", ActivityState.Failure)
            Return
        End If

        Dim foundIndex As Integer = -1
        For i As Integer = 0 To months.Length - 1
            If String.Equals(months(i), searchTerm, StringComparison.OrdinalIgnoreCase) Then
                foundIndex = i
                Exit For
            End If
        Next

        If foundIndex = -1 Then
            SetActivityStatus("""" & searchTerm & """ is not a valid month name.", ActivityState.Failure)
            Return
        End If

        monthsListBox.SelectedIndex = foundIndex
        SetActivityStatus("Found " & months(foundIndex) & " at array index " & foundIndex & ".", ActivityState.Success)
    End Sub

    Private Sub RestoreButton_Click(sender As Object, e As EventArgs)
        FillMonthsListBox()
        monthsListBox.ClearSelected()
        MonthsListBox_SelectedIndexChanged(Nothing, EventArgs.Empty)
        SetActivityStatus("List restored to the original January " & ChrW(8212) & " December order.", ActivityState.Success)
    End Sub

    Private Sub SetActivityStatus(message As String, state As ActivityState)
        statusMessageLabel.Text = message
        Select Case state
            Case ActivityState.Success
                statusPanel.BackColor = Color.FromArgb(230, 247, 237)
                statusMessageLabel.ForeColor = Color.FromArgb(30, 120, 70)
                statusIconLabel.ForeColor = Color.FromArgb(30, 120, 70)
            Case ActivityState.Failure
                statusPanel.BackColor = Color.FromArgb(253, 235, 235)
                statusMessageLabel.ForeColor = Color.FromArgb(180, 40, 40)
                statusIconLabel.ForeColor = Color.FromArgb(180, 40, 40)
            Case Else
                statusPanel.BackColor = Color.FromArgb(247, 247, 249)
                statusMessageLabel.ForeColor = Color.FromArgb(50, 50, 50)
                statusIconLabel.ForeColor = Color.Gray
        End Select
    End Sub

    ' ==================================================
    ' PRESENTATION SLIDE LOADING
    ' ==================================================
    Private Sub LoadSlides()
        Dim folderPath As String = IO.Path.Combine(Application.StartupPath, "LessonContent", "Week07_Arrays", "Slides")

        If Not IO.Directory.Exists(folderPath) Then
            leftSlideStack.Controls.Add(New Label() With {
                .Text = "Slides not found at:" & vbCrLf & folderPath,
                .ForeColor = Color.Red,
                .AutoSize = True
            })
            Return
        End If

        Dim files = IO.Directory.GetFiles(folderPath, "*.png")
        Array.Sort(files)

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
            Dim bytes As Byte() = IO.File.ReadAllBytes(filePath)
            Dim ms As New IO.MemoryStream(bytes)
            Dim img As Image = Image.FromStream(ms)
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

    ' ==================================================
    ' NAVIGATION
    ' ==================================================
    Private Sub BuildBottomNav()
        Dim navPanel As New Panel() With {.Dock = DockStyle.Bottom, .Height = 60, .BackColor = Color.White}
        Me.Controls.Add(navPanel)

        Dim topBorder As New Panel() With {.Dock = DockStyle.Top, .Height = 1, .BackColor = Color.FromArgb(230, 230, 230)}
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

        Dim progressLbl As New Label() With {.Text = "Week 7 of 19", .Font = New Font("Segoe UI", 9.0F), .ForeColor = Color.Gray, .AutoSize = True}
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
        Dim timerWindow As New Week6TimerForm()
        timerWindow.Show()
        Me.Close()
    End Sub

    Private Sub ContinueButton_Click(sender As Object, e As EventArgs)
        Dim week8Window As New Week8Form()
        week8Window.Show()
        Me.Close()
    End Sub

    ' ==================================================
    ' SHARED HELPER
    ' ==================================================
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
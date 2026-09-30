Public Class Week2ClassesAndObjectsForm
    Inherits Form

    Private leftScrollPanel As Panel
    Private leftSlideStack As FlowLayoutPanel
    Private rightPanel As Panel

    Private currentStudent As Student
    Private nameTextBox As TextBox
    Private idTextBox As TextBox
    Private ageTextBox As TextBox
    Private courseTextBox As TextBox
    Private outputPanel As Panel
    Private outputLabel As Label
    Private statusPanel As Panel
    Private statusIconLabel As Label
    Private statusMessageLabel As Label

    Private Enum StudentFormState
        Neutral
        Success
        Failure
    End Enum

    Public Sub New()
        Me.Text = "Week 2 " & ChrW(8212) & " Classes and Objects " & ChrW(8212) & "Object-Oriented Programming in Visual Basic .NET"
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
            .Text = "Week 2 " & ChrW(8212) & " Classes and Objects",
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
            .Text = "Classes and Objects",
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
            .BackColor = Color.White,
            .Margin = New Padding(12, 0, 0, 0),
            .Padding = New Padding(8)
        }
        bodyTable.Controls.Add(rightPanel, 1, 0)

        PopulateStudentProfile()
    End Sub

    Private Sub PopulateStudentProfile()
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

        Dim headerBar As New Panel() With {
            .Height = 48,
            .BackColor = Color.FromArgb(24, 24, 30),
            .Margin = New Padding(0, 0, 0, 16)
        }
        Dim headerLbl As New Label() With {
            .Text = "STUDENT PROFILE",
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(14, 0, 0, 0)
        }
        headerBar.Controls.Add(headerLbl)
        cardStack.Controls.Add(headerBar)

        Dim nameCaption As New Label() With {
            .Text = "STUDENT NAME",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 4)
        }
        cardStack.Controls.Add(nameCaption)
        nameTextBox = New TextBox() With {.Font = New Font("Segoe UI", 10.0F), .Margin = New Padding(0, 0, 0, 12)}
        cardStack.Controls.Add(nameTextBox)

        Dim idCaption As New Label() With {
            .Text = "STUDENT ID",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 4)
        }
        cardStack.Controls.Add(idCaption)
        idTextBox = New TextBox() With {.Font = New Font("Segoe UI", 10.0F), .Margin = New Padding(0, 0, 0, 12)}
        cardStack.Controls.Add(idTextBox)

        Dim ageCaption As New Label() With {
            .Text = "AGE",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 4)
        }
        cardStack.Controls.Add(ageCaption)
        ageTextBox = New TextBox() With {.Font = New Font("Segoe UI", 10.0F), .Margin = New Padding(0, 0, 0, 12)}
        cardStack.Controls.Add(ageTextBox)

        Dim courseCaption As New Label() With {
            .Text = "COURSE",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 4)
        }
        cardStack.Controls.Add(courseCaption)
        courseTextBox = New TextBox() With {.Font = New Font("Segoe UI", 10.0F), .Margin = New Padding(0, 0, 0, 16)}
        cardStack.Controls.Add(courseTextBox)

        Dim createBtn As New Button() With {
            .Text = "Create Student",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Height = 40,
            .Margin = New Padding(0, 0, 0, 16)
        }
        createBtn.FlatAppearance.BorderSize = 0
        AddHandler createBtn.Click, AddressOf CreateStudentButton_Click
        cardStack.Controls.Add(createBtn)

        outputPanel = New Panel() With {
            .BackColor = Color.FromArgb(247, 247, 249),
            .Margin = New Padding(0, 0, 0, 16),
            .Height = 100
        }
        cardStack.Controls.Add(outputPanel)

        Dim outputCaption As New Label() With {
            .Text = "STUDENT OBJECT",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(14, 10)
        }
        outputPanel.Controls.Add(outputCaption)

        outputLabel = New Label() With {
            .Text = "No student object created yet.",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .AutoSize = False,
            .Location = New Point(14, 30),
            .Size = New Size(200, 64)
        }
        outputPanel.Controls.Add(outputLabel)

        statusPanel = New Panel() With {
            .BackColor = Color.FromArgb(247, 247, 249),
            .Height = 40
        }
        cardStack.Controls.Add(statusPanel)

        statusIconLabel = New Label() With {
            .Text = ChrW(8505),
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(10, 10)
        }
        statusPanel.Controls.Add(statusIconLabel)

        statusMessageLabel = New Label() With {
            .Text = "Fill in the fields and click Create Student.",
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.FromArgb(50, 50, 50),
            .AutoSize = True,
            .Location = New Point(30, 11)
        }
        statusPanel.Controls.Add(statusMessageLabel)

        Dim adjustCardWidths As Action = Sub()
                                             Dim innerWidth As Integer = Math.Max(60, cardStack.ClientSize.Width - cardStack.Padding.Left - cardStack.Padding.Right)
                                             headerBar.Width = innerWidth
                                             nameTextBox.Width = innerWidth
                                             idTextBox.Width = innerWidth
                                             ageTextBox.Width = innerWidth
                                             courseTextBox.Width = innerWidth
                                             createBtn.Width = innerWidth
                                             outputPanel.Width = innerWidth
                                             outputLabel.Width = Math.Max(60, innerWidth - 28)
                                             statusPanel.Width = innerWidth
                                         End Sub
        AddHandler cardStack.Resize, Sub(s, ev) adjustCardWidths()
        adjustCardWidths()
    End Sub

    Private Sub CreateStudentButton_Click(sender As Object, e As EventArgs)
        Dim nameVal As String = nameTextBox.Text.Trim()
        Dim idVal As String = idTextBox.Text.Trim()
        Dim ageVal As Integer

        If nameVal = "" OrElse idVal = "" OrElse courseTextBox.Text.Trim() = "" Then
            SetStatus("Please fill in Name, ID, and Course.", StudentFormState.Failure)
            Return
        End If

        If Not Integer.TryParse(ageTextBox.Text.Trim(), ageVal) OrElse ageVal <= 0 Then
            SetStatus("Please enter a valid age.", StudentFormState.Failure)
            Return
        End If

        currentStudent = New Student(nameVal, idVal, ageVal, courseTextBox.Text.Trim())

        outputLabel.Text = "Name: " & currentStudent.Name & vbCrLf &
                            "ID: " & currentStudent.StudentID & vbCrLf &
                            "Age: " & currentStudent.Age & vbCrLf &
                            "Course: " & currentStudent.Course

        SetStatus("Student object created successfully.", StudentFormState.Success)
    End Sub

    Private Sub SetStatus(message As String, state As StudentFormState)
        statusMessageLabel.Text = message
        Select Case state
            Case StudentFormState.Success
                statusPanel.BackColor = Color.FromArgb(230, 247, 237)
                statusMessageLabel.ForeColor = Color.FromArgb(30, 120, 70)
                statusIconLabel.ForeColor = Color.FromArgb(30, 120, 70)
            Case StudentFormState.Failure
                statusPanel.BackColor = Color.FromArgb(253, 235, 235)
                statusMessageLabel.ForeColor = Color.FromArgb(180, 40, 40)
                statusIconLabel.ForeColor = Color.FromArgb(180, 40, 40)
            Case Else
                statusPanel.BackColor = Color.FromArgb(247, 247, 249)
                statusMessageLabel.ForeColor = Color.FromArgb(50, 50, 50)
                statusIconLabel.ForeColor = Color.Gray
        End Select
    End Sub

    Private Sub LoadSlides()
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
        Dim orientationWindow As New OrientationForm()
        orientationWindow.Show()
        Me.Close()
    End Sub

    Private Sub ContinueButton_Click(sender As Object, e As EventArgs)
        Dim encapsulationWindow As New Week2Form()
        encapsulationWindow.Show()
        Me.Close()
    End Sub

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
Public Class OrientationForm
    Inherits Form

    Private scrollPanel As Panel
    Private mainStack As FlowLayoutPanel

    Public Sub New()
        Me.Text = "Lesson 1 " & ChrW(8212) & " Orientation " & ChrW(8212) & " Object-Oriented Programming in Visual Basic .NET"
        Me.Size = New Size(1000, 750)
        Me.MinimumSize = New Size(600, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(235, 235, 235)

        BuildContent()
        BuildBottomNav()
    End Sub

    Private Sub BuildContent()
        scrollPanel = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = Color.FromArgb(235, 235, 235)
        }
        Me.Controls.Add(scrollPanel)

        mainStack = New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Padding = New Padding(24),
            .Margin = New Padding(0),
            .BackColor = Color.Transparent
        }
        scrollPanel.Controls.Add(mainStack)

        Dim baseFolder As String = IO.Path.Combine(Application.StartupPath, "LessonContent", "Lesson01_Orientation")

        AddSectionHeader("Course Syllabus")
        AddImagePagesFromFolder(IO.Path.Combine(baseFolder, "Syllabus"))

        AddSectionHeader("Course Orientation")
        AddImagePagesFromFolder(IO.Path.Combine(baseFolder, "Presentation"))

        Dim centerMainStack As Action = Sub()
                                            Dim x As Integer = Math.Max(0, (scrollPanel.ClientSize.Width - mainStack.Width) \ 2)
                                            mainStack.Left = x
                                        End Sub
        AddHandler scrollPanel.Resize, Sub(s, ev) centerMainStack()
        AddHandler mainStack.Resize, Sub(s, ev) centerMainStack()
        centerMainStack()
    End Sub

    ' Adds a bold title + smaller gray subtitle line above a presentation section.
    Private Sub AddSectionHeader(titleText As String)
        Dim topMargin As Integer = If(mainStack.Controls.Count = 0, 0, 28)

        Dim headerPanel As New Panel() With {
            .AutoSize = True,
            .Margin = New Padding(0, topMargin, 0, 12)
        }

        Dim titleLbl As New Label() With {
            .Text = titleText,
            .Font = New Font("Segoe UI", 20.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(0, 0)
        }
        headerPanel.Controls.Add(titleLbl)

        Dim subtitleLbl As New Label() With {
            .Text = "Quezon City University " & ChrW(183) & " College of Computer Studies " & ChrW(183) & " Information Technology Department",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(0, titleLbl.Bottom + 4)
        }
        headerPanel.Controls.Add(subtitleLbl)

        mainStack.Controls.Add(headerPanel)
    End Sub

    ' Loads every PNG in the given folder, in alphabetical order, as a full-width page image.
    Private Sub AddImagePagesFromFolder(folderPath As String)
        If Not IO.Directory.Exists(folderPath) Then
            mainStack.Controls.Add(New Label() With {
                .Text = "Images not found at:" & vbCrLf & folderPath,
                .ForeColor = Color.Red,
                .AutoSize = True,
                .Margin = New Padding(0, 0, 0, 20)
            })
            Return
        End If

        Dim files = IO.Directory.GetFiles(folderPath, "*.png")
        Array.Sort(files) ' keeps orientation_page_01, 02, ... 09 in order

        If files.Length = 0 Then
            mainStack.Controls.Add(New Label() With {
                .Text = "No PNG files found in:" & vbCrLf & folderPath,
                .ForeColor = Color.Red,
                .AutoSize = True,
                .Margin = New Padding(0, 0, 0, 20)
            })
            Return
        End If

        Const displayWidth As Integer = 900

        For Each filePath In files
            ' Load via a MemoryStream so the file isn't locked while the app is running.
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
            mainStack.Controls.Add(pic)
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
            .Text = "Lesson 1 of 19",
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
        Me.Close()
    End Sub

    Private Sub ContinueButton_Click(sender As Object, e As EventArgs)
        Dim classesWindow As New Week2ClassesAndObjectsForm()
        classesWindow.Show()
        Me.Close()
    End Sub

End Class
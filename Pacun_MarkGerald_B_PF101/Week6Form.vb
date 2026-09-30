Public Class Week6Form
    Inherits Form

    ' ===== Form-level layout =====
    Private leftScrollPanel As Panel
    Private leftSlideStack As FlowLayoutPanel
    Private rightPanel As Panel

    ' ===== Truth table controls (one list per table) =====
    Private andRows As New List(Of Tuple(Of ComboBox, ComboBox, Label))
    Private orRows As New List(Of Tuple(Of ComboBox, ComboBox, Label))
    Private xorRows As New List(Of Tuple(Of ComboBox, ComboBox, Label))
    Private notRows As New List(Of Tuple(Of ComboBox, Label))

    Public Sub New()
        Me.Text = "Week 6 " & ChrW(8212) & " The Truth Table " & ChrW(8212) & " Object-Oriented Programming in Visual Basic .NET"
        Me.Size = New Size(1200, 800)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White

        BuildHeader()
        BuildBody()
        BuildBottomNav()
    End Sub

    ' ==================================================
    ' FORM SETUP
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
            .Text = "Week 6 " & ChrW(8212) & " The Selection and Repetition Structure",
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
            .Text = "The Selection and Repetition Structure",
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
        bodyTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58.0F))
        bodyTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42.0F))
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

        PopulateTruthTables()
    End Sub

    ' ==================================================
    ' PRESENTATION / SLIDE LOADING
    ' ==================================================
    Private Sub LoadSlides()
        Dim folderPath As String = IO.Path.Combine(Application.StartupPath, "LessonContent", "Week06_SelectionRepetition", "Slides")

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

        Const displayWidth As Integer = 620

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
    ' INTERACTIVE AREA - all four truth tables
    ' ==================================================
    Private Sub PopulateTruthTables()
        Dim sectionLabel As New Label() With {
            .Text = "INTERACTIVE EXAMPLES",
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
            .Text = "BOOLEAN TRUTH TABLES",
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(14, 0, 0, 0)
        }
        headerBar.Controls.Add(headerLbl)
        cardStack.Controls.Add(headerBar)

        ' --- The four sections ---
        Dim andTable As TableLayoutPanel = BuildTwoInputSection(
            cardStack, "1. AND Truth Table",
            "Both expressions must be True for the result to be True.",
            "Expression 1 And Expression 2", andRows, AddressOf RecalculateAnd)

        Dim orTable As TableLayoutPanel = BuildTwoInputSection(
            cardStack, "2. OR Truth Table",
            "The result is True if at least one expression is True.",
            "Expression 1 Or Expression 2", orRows, AddressOf RecalculateOr)

        Dim xorTable As TableLayoutPanel = BuildTwoInputSection(
            cardStack, "3. XOR Truth Table",
            "Exclusive OR " & ChrW(8212) & " True only when exactly one expression is True.",
            "Expression 1 Xor Expression 2", xorRows, AddressOf RecalculateXor)

        Dim notTable As TableLayoutPanel = BuildNotSection(cardStack)

        ' --- Keep everything full width on resize ---
        Dim adjustCardWidths As Action = Sub()
                                             Dim innerWidth As Integer = Math.Max(240, cardStack.ClientSize.Width - cardStack.Padding.Left - cardStack.Padding.Right)
                                             headerBar.Width = innerWidth
                                             For Each ctl As Control In cardStack.Controls
                                                 If TypeOf ctl Is TableLayoutPanel OrElse TypeOf ctl Is Label OrElse TypeOf ctl Is Panel Then
                                                     ctl.Width = innerWidth
                                                 End If
                                             Next
                                         End Sub
        AddHandler cardStack.Resize, Sub(s, ev) adjustCardWidths()
        adjustCardWidths()

        ' Initial calculation so every result cell is correct on open
        RecalculateAnd()
        RecalculateOr()
        RecalculateXor()
        RecalculateNot()
    End Sub

    ' Builds title + description + a 3-column truth table (AND / OR / XOR share this shape).
    Private Function BuildTwoInputSection(parentStack As FlowLayoutPanel,
                                           titleText As String,
                                           descText As String,
                                           resultHeaderText As String,
                                           rowStore As List(Of Tuple(Of ComboBox, ComboBox, Label)),
                                           recalcAction As Action) As TableLayoutPanel

        parentStack.Controls.Add(New Label() With {
            .Text = titleText,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Margin = New Padding(0, 4, 0, 2)
        })

        parentStack.Controls.Add(New Label() With {
            .Text = descText,
            .Font = New Font("Segoe UI", 8.0F),
            .ForeColor = Color.Gray,
            .AutoSize = False,
            .Height = 18,
            .Margin = New Padding(0, 0, 0, 6)
        })

        ' Starting combinations required by the spec
        Dim startValues As Boolean()() = {
            New Boolean() {True, False},
            New Boolean() {False, True},
            New Boolean() {False, False},
            New Boolean() {True, True}
        }

        Dim table As New TableLayoutPanel() With {
            .ColumnCount = 3,
            .RowCount = 5,
            .Height = 5 * 34,
            .Margin = New Padding(0, 0, 0, 18),
            .Padding = New Padding(0),
            .BackColor = Color.White
        }
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 28.0F))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 28.0F))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 44.0F))
        For i As Integer = 1 To 5
            table.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
        Next

        table.Controls.Add(MakeHeaderCell("Expression 1"), 0, 0)
        table.Controls.Add(MakeHeaderCell("Expression 2"), 1, 0)
        table.Controls.Add(MakeHeaderCell(resultHeaderText), 2, 0)

        For i As Integer = 0 To 3
            Dim combo1 As ComboBox = MakeBoolCombo(startValues(i)(0))
            Dim combo2 As ComboBox = MakeBoolCombo(startValues(i)(1))
            Dim resultLbl As Label = MakeResultCell()

            AddHandler combo1.SelectedIndexChanged, Sub(s, ev) recalcAction()
            AddHandler combo2.SelectedIndexChanged, Sub(s, ev) recalcAction()

            table.Controls.Add(combo1, 0, i + 1)
            table.Controls.Add(combo2, 1, i + 1)
            table.Controls.Add(resultLbl, 2, i + 1)

            rowStore.Add(New Tuple(Of ComboBox, ComboBox, Label)(combo1, combo2, resultLbl))
        Next

        parentStack.Controls.Add(table)
        Return table
    End Function

    ' NOT only has one input, so it gets its own 2-column, 2-row shape.
    Private Function BuildNotSection(parentStack As FlowLayoutPanel) As TableLayoutPanel
        parentStack.Controls.Add(New Label() With {
            .Text = "4. NOT Truth Table",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Margin = New Padding(0, 4, 0, 2)
        })

        parentStack.Controls.Add(New Label() With {
            .Text = "NOT takes a single expression and reverses its value.",
            .Font = New Font("Segoe UI", 8.0F),
            .ForeColor = Color.Gray,
            .AutoSize = False,
            .Height = 18,
            .Margin = New Padding(0, 0, 0, 6)
        })

        Dim startValues As Boolean() = {True, False}

        Dim table As New TableLayoutPanel() With {
            .ColumnCount = 2,
            .RowCount = 3,
            .Height = 3 * 34,
            .Margin = New Padding(0, 0, 0, 8),
            .Padding = New Padding(0),
            .BackColor = Color.White
        }
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        For i As Integer = 1 To 3
            table.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
        Next

        table.Controls.Add(MakeHeaderCell("Expression"), 0, 0)
        table.Controls.Add(MakeHeaderCell("Not Expression"), 1, 0)

        For i As Integer = 0 To 1
            Dim combo As ComboBox = MakeBoolCombo(startValues(i))
            Dim resultLbl As Label = MakeResultCell()

            AddHandler combo.SelectedIndexChanged, Sub(s, ev) RecalculateNot()

            table.Controls.Add(combo, 0, i + 1)
            table.Controls.Add(resultLbl, 1, i + 1)

            notRows.Add(New Tuple(Of ComboBox, Label)(combo, resultLbl))
        Next

        parentStack.Controls.Add(table)
        Return table
    End Function

    ' ===== Small cell builders =====
    Private Function MakeHeaderCell(text As String) As Label
        Return New Label() With {
            .Text = text,
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(50, 50, 50),
            .BackColor = Color.FromArgb(240, 240, 244),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Margin = New Padding(1)
        }
    End Function

    Private Function MakeBoolCombo(initialValue As Boolean) As ComboBox
        Dim combo As New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 9.0F),
            .Dock = DockStyle.Fill,
            .Margin = New Padding(3, 4, 3, 4)
        }
        combo.Items.Add("True")
        combo.Items.Add("False")
        combo.SelectedItem = If(initialValue, "True", "False")
        Return combo
    End Function

    Private Function MakeResultCell() As Label
        Return New Label() With {
            .Text = "",
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(30, 30, 30),
            .BackColor = Color.FromArgb(249, 249, 251),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Margin = New Padding(1)
        }
    End Function

    ' ==================================================
    ' TRUTH TABLE CALCULATION LOGIC
    ' ==================================================
    Private Function ComboValue(combo As ComboBox) As Boolean
        Return (combo.SelectedItem IsNot Nothing AndAlso combo.SelectedItem.ToString() = "True")
    End Function

    Private Sub ShowResult(lbl As Label, value As Boolean)
        lbl.Text = If(value, "True", "False")
        lbl.ForeColor = If(value, Color.FromArgb(30, 120, 70), Color.FromArgb(150, 60, 60))
    End Sub

    Private Sub RecalculateAnd()
        For Each row In andRows
            ShowResult(row.Item3, ComboValue(row.Item1) And ComboValue(row.Item2))
        Next
    End Sub

    Private Sub RecalculateOr()
        For Each row In orRows
            ShowResult(row.Item3, ComboValue(row.Item1) Or ComboValue(row.Item2))
        Next
    End Sub

    Private Sub RecalculateXor()
        For Each row In xorRows
            ShowResult(row.Item3, ComboValue(row.Item1) Xor ComboValue(row.Item2))
        Next
    End Sub

    Private Sub RecalculateNot()
        For Each row In notRows
            ShowResult(row.Item2, Not ComboValue(row.Item1))
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

        Dim progressLbl As New Label() With {.Text = "Week 6 of 19", .Font = New Font("Segoe UI", 9.0F), .ForeColor = Color.Gray, .AutoSize = True}
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
        Dim week5Window As New Week5Form()
        week5Window.Show()
        Me.Close()
    End Sub

    Private Sub ContinueButton_Click(sender As Object, e As EventArgs)
        Dim timerWindow As New Week6TimerForm()
        timerWindow.Show()
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
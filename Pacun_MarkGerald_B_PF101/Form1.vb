Public Class Form1

    ' Layout controls
    Private navPanel As Panel
    Private navLeftFlow As FlowLayoutPanel
    Private lessonsButton As Button
    Private helpButton As Button
    Private exitButton As Button
    Private sbitLabel As Label

    Private lessonsDropdownPanel As Panel
    Private lessonsDropdownContent As Panel
    Private lessonsDropdownOpen As Boolean = False

    Private featuresContainer As Panel
    Private lessonMenuContainer As Panel
    Private welcomeSeparator As Panel
    Private helpContentPanel As Panel
    Private isHelpViewActive As Boolean = False

    Private week2SubmenuOpen As Boolean = False
    Private week2ChevronLabel As Label

    Private week6SubmenuOpen As Boolean = False
    Private week6ChevronLabel As Label

    Private animationSubmenuOpen As Boolean = False
    Private animationChevronLabel As Label

    Private scrollContainer As Panel
    Private scrollFlow As FlowLayoutPanel

    Private headerPanel As Panel
    Private titleLabel As Label
    Private descLabel As Label
    Private heroBrowseButton As Button
    Private heroGettingStartedButton As Button

    Private contentPanel As Panel

    ' Stats table (class-level so Resize handler can reference it)
    Private statsTable As TableLayoutPanel

    ' Cache for the header background image (gradient + dots)
    Private headerBackgroundCache As Bitmap = Nothing
    Private headerBackgroundSize As Size = Size.Empty

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Form basics
        Me.Text = "PF101 — Object Oriented Programming  Created By. Pacun, Mark Gerald B."
        Me.MinimumSize = New Size(900, 620)
        Me.BackColor = Color.FromArgb(245, 245, 245)

        ' --- SCROLLABLE AREA (contains header + page content) ---
        scrollContainer = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = Color.White
        }
        Me.Controls.Add(scrollContainer)

        ' --- NAV BAR (will be added into scrollFlow) ---
        navPanel = New Panel() With {
            .Width = Math.Max(800, scrollContainer.ClientSize.Width),
            .Height = 56,
            .BackColor = Color.WhiteSmoke,
            .Padding = New Padding(12, 6, 12, 6),
            .Margin = New Padding(0)
        }
        navLeftFlow = New FlowLayoutPanel() With {
            .Dock = DockStyle.Left,
            .AutoSize = True,
            .WrapContents = False,
            .FlowDirection = FlowDirection.LeftToRight,
            .Padding = New Padding(0),
            .Margin = New Padding(0)
        }
        navPanel.Controls.Add(navLeftFlow)

        lessonsButton = New Button() With {
            .Text = "Lessons  ▾",
            .AutoSize = True,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.0F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Padding = New Padding(12, 8, 12, 8),
            .Margin = New Padding(6, 6, 6, 6)
        }
        lessonsButton.FlatAppearance.BorderSize = 0
        AddHandler lessonsButton.Click, AddressOf LessonsButton_Click
        navLeftFlow.Controls.Add(lessonsButton)

        helpButton = New Button() With {
            .Text = "Help",
            .AutoSize = True,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.0F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Padding = New Padding(12, 8, 12, 8),
            .Margin = New Padding(6, 6, 6, 6)
        }
        helpButton.FlatAppearance.BorderSize = 0
        AddHandler helpButton.Click, AddressOf HelpButton_Click
        navLeftFlow.Controls.Add(helpButton)

        exitButton = New Button() With {
            .Text = "Exit",
            .AutoSize = True,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.0F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Padding = New Padding(12, 8, 12, 8),
            .Margin = New Padding(6, 6, 6, 6)
        }
        exitButton.FlatAppearance.BorderSize = 0
        AddHandler exitButton.Click, AddressOf ExitButton_Click
        navLeftFlow.Controls.Add(exitButton)

        sbitLabel = New Label() With {
            .Text = "SBIT-2E",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Margin = New Padding(5, 20, 10, 10),
            .BackColor = Color.Transparent
        }
        navLeftFlow.Controls.Add(sbitLabel)

        ' ================== LESSONS DROPDOWN (nav bar) — shell only, Step 1 ==================
        Dim dropdownWidth As Integer = 360
        Dim dropdownMaxHeight As Integer = 420

        lessonsDropdownPanel = New Panel() With {
            .Size = New Size(dropdownWidth, dropdownMaxHeight),
            .BackColor = Color.White,
            .Visible = False
        }
        AddHandler lessonsDropdownPanel.Paint, Sub(s, pe)
                                                   Dim r = lessonsDropdownPanel.ClientRectangle
                                                   r = New Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1)
                                                   pe.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                                   Using pen As New Pen(Color.FromArgb(220, 220, 225), 1)
                                                       pe.Graphics.DrawRectangle(pen, r)
                                                   End Using
                                               End Sub
        Me.Controls.Add(lessonsDropdownPanel)

        Dim dropdownHeader As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 44,
            .BackColor = Color.White
        }
        Dim dropdownHeaderTitle As New Label() With {
            .Text = "PF101 LESSONS",
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .AutoSize = True,
            .Location = New Point(14, 14)
        }
        dropdownHeader.Controls.Add(dropdownHeaderTitle)
        Dim dropdownHeaderCount As New Label() With {
            .Text = "17 topics",
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.Gray,
            .AutoSize = True
        }
        dropdownHeader.Controls.Add(dropdownHeaderCount)
        AddHandler dropdownHeader.Resize, Sub(s, ev)
                                              dropdownHeaderCount.Location = New Point(dropdownHeader.Width - dropdownHeaderCount.Width - 14, 14)
                                          End Sub

        Dim dropdownSeparator As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 1,
            .BackColor = Color.FromArgb(230, 230, 230)
        }

        lessonsDropdownContent = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = Color.White
        }

        ' Add order matters for Dock=Top stacking: later-added docks closer to the edge,
        ' so add content, then the separator, then the header last (so header ends up on top).
        lessonsDropdownPanel.Controls.Add(lessonsDropdownContent)
        lessonsDropdownPanel.Controls.Add(dropdownSeparator)
        lessonsDropdownPanel.Controls.Add(dropdownHeader)

        AddHandler Me.Resize, Sub(s, ev) If lessonsDropdownOpen Then PositionLessonsDropdown()
        AddHandler scrollContainer.Scroll, Sub(s, ev)
                                               If lessonsDropdownOpen Then
                                                   CloseLessonsDropdown()
                                               End If
                                           End Sub

        ' Flow panel inside scrollContainer stacks nav + header + content vertically
        scrollFlow = New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoSize = True,
            .Padding = New Padding(0, 12, 0, 0),
            .Margin = New Padding(0)
        }
        scrollContainer.Controls.Add(scrollFlow)

        ' Add navPanel as first child of the scrollFlow (nav will scroll with the page)
        scrollFlow.Controls.Add(navPanel)

        ' --- HEADER / HERO (inside scrollable area so it will scroll) ---
        headerPanel = New Panel() With {
            .Width = Math.Max(800, scrollContainer.ClientSize.Width),
            .Height = 300,
            .Padding = New Padding(48, 36, 48, 36),
            .Margin = New Padding(0),
            .BackColor = Color.Transparent,
            .AutoSize = False
        }
        AddHandler headerPanel.Paint, AddressOf HeaderPanel_Paint

        titleLabel = New Label() With {
            .Text = "PF101 — Object Oriented Programming",
            .ForeColor = Color.White,
            .BackColor = Color.Transparent,
            .Font = New Font("Segoe UI", 28.0F, FontStyle.Bold),
            .AutoSize = True
        }
        headerPanel.Controls.Add(titleLabel)

        descLabel = New Label() With {
            .Text = "A digital study guide for all PF101 lessons. Read presentations, review summaries, and explore Visual Basic examples all in one organized place.",
            .ForeColor = Color.FromArgb(220, 220, 220),
            .BackColor = Color.Transparent,
            .Font = New Font("Segoe UI", 11.0F),
            .AutoSize = False,
            .Size = New Size(760, 70)
        }
        headerPanel.Controls.Add(descLabel)

        heroBrowseButton = New Button() With {
            .Text = "📚  Browse Lessons",
            .Size = New Size(180, 46),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10.0F),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Padding = New Padding(10),
            .Margin = New Padding(0)
        }
        heroBrowseButton.FlatAppearance.BorderSize = 0
        heroBrowseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240)
        headerPanel.Controls.Add(heroBrowseButton)

        AddHandler heroBrowseButton.Click,
            AddressOf HeroBrowseButton_Click

        heroGettingStartedButton = New Button() With {
            .Text = "▶  Getting Started",
            .Size = New Size(180, 46),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10.0F),
            .BackColor = Color.FromArgb(24, 24, 30),
            .ForeColor = Color.White,
            .Padding = New Padding(10),
            .Margin = New Padding(0)
        }
        heroGettingStartedButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80)
        heroGettingStartedButton.FlatAppearance.BorderSize = 1
        heroGettingStartedButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 34, 38)
        headerPanel.Controls.Add(heroGettingStartedButton)

        AddHandler heroGettingStartedButton.Click,
            AddressOf HeroGettingStartedButton_Click

        ' Improved header layout function (uses padding for left/top/right)
        Dim placeHeaderContents = Sub()
                                      Dim left As Integer = headerPanel.Padding.Left
                                      Dim top As Integer = headerPanel.Padding.Top
                                      Dim rightInset As Integer = headerPanel.Padding.Right

                                      titleLabel.Location = New Point(left, top)

                                      Dim descAvailableWidth As Integer = Math.Max(200, headerPanel.ClientSize.Width - left - rightInset)
                                      descLabel.Width = descAvailableWidth

                                      Using g = headerPanel.CreateGraphics()
                                          Dim sf = New StringFormat()
                                          sf.Alignment = StringAlignment.Near
                                          sf.LineAlignment = StringAlignment.Near
                                          Dim measured = g.MeasureString(descLabel.Text, descLabel.Font, descAvailableWidth, sf)
                                          descLabel.Height = CInt(Math.Ceiling(measured.Height))
                                      End Using

                                      descLabel.Location = New Point(left, titleLabel.Bottom + 12)

                                      Dim gapAfterDesc As Integer = 16
                                      Dim marginBottom As Integer = headerPanel.Padding.Bottom
                                      Dim yButtons As Integer = Math.Max(descLabel.Bottom + gapAfterDesc, headerPanel.ClientSize.Height - heroBrowseButton.Height - marginBottom)

                                      heroBrowseButton.Location = New Point(left, yButtons)
                                      heroGettingStartedButton.Location = New Point(left + heroBrowseButton.Width + 16, yButtons)

                                      ApplyRoundedCorners(heroBrowseButton, 8)
                                      ApplyRoundedCorners(heroGettingStartedButton, 8)

                                      headerPanel.Invalidate()
                                  End Sub

        AddHandler headerPanel.Resize, Sub(s, ev) placeHeaderContents()
        placeHeaderContents()

        scrollFlow.Controls.Add(headerPanel)

        ' --- Three-column statistics section (responsive) ---
        statsTable = New TableLayoutPanel() With {
            .AutoSize = False,
            .BackColor = Color.White,
            .Width = headerPanel.Width,
            .Height = 120,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0),
            .Padding = New Padding(0)
        }
        statsTable.ColumnCount = 3
        statsTable.RowCount = 1
        statsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
        statsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
        statsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))

        Dim MakeStatCell = Function(numberText As String, caption As String) As Panel
                               Dim cell As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White}
                               Dim inner As New TableLayoutPanel() With {
                                   .Dock = DockStyle.Fill,
                                   .RowCount = 2,
                                   .ColumnCount = 1,
                                   .Padding = New Padding(0),
                                   .Margin = New Padding(0)
                               }
                               inner.RowStyles.Add(New RowStyle(SizeType.Percent, 60))
                               inner.RowStyles.Add(New RowStyle(SizeType.Percent, 40))
                               Dim lblNum As New Label() With {
                                   .Text = numberText,
                                   .Font = New Font("Segoe UI", 20.0F, FontStyle.Bold),
                                   .AutoSize = False,
                                   .Dock = DockStyle.Fill,
                                   .TextAlign = ContentAlignment.MiddleCenter,
                                   .ForeColor = Color.Black
                               }
                               Dim lblText As New Label() With {
                                   .Text = caption,
                                   .Font = New Font("Segoe UI", 9.0F),
                                   .AutoSize = False,
                                   .Dock = DockStyle.Fill,
                                   .TextAlign = ContentAlignment.TopCenter,
                                   .ForeColor = Color.Gray
                               }
                               inner.Controls.Add(lblNum, 0, 0)
                               inner.Controls.Add(lblText, 0, 1)
                               cell.Controls.Add(inner)
                               Return cell
                           End Function

        statsTable.Controls.Add(MakeStatCell("17", "Lessons"), 0, 0)
        statsTable.Controls.Add(MakeStatCell("VB.NET", "Language"), 1, 0)
        statsTable.Controls.Add(MakeStatCell("1", "Archive"), 2, 0)

        welcomeSeparator = New Panel() With {.Height = 1, .BackColor = Color.FromArgb(230, 230, 230), .Dock = DockStyle.Top, .Margin = New Padding(0)}
        scrollFlow.Controls.Add(welcomeSeparator)
        scrollFlow.Controls.Add(statsTable)

        ' --- WHAT YOU'LL FIND IN EACH LESSON ---
        featuresContainer = New Panel() With {
            .Dock = DockStyle.Top,
            .Padding = New Padding(24, 24, 24, 12),
            .BackColor = Color.White,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink   ' <-- lets it grow to fit its children
}
        scrollFlow.Controls.Add(featuresContainer)

        Dim featuresTitle As New Label() With {
            .Text = "WHAT YOU'LL FIND IN EACH LESSON",
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .AutoSize = True,
            .Location = New Point(8, 4)
        }
        featuresContainer.Controls.Add(featuresTitle)

        Dim featuresTable As New TableLayoutPanel() With {
            .ColumnCount = 4,
            .RowCount = 1,
            .Height = 180,
            .Width = Math.Max(760, scrollContainer.ClientSize.Width) - 40,
            .Location = New Point(8, featuresTitle.Bottom + 12),   ' now actually takes effect
            .Padding = New Padding(0),
            .Margin = New Padding(0)
        }
        featuresTable.ColumnStyles.Clear()
        For i As Integer = 1 To 4
            featuresTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        Next
        featuresContainer.Controls.Add(featuresTable)
        featuresTable.Location = New Point(8, featuresTitle.Bottom + 12)

        ' Helper to create each card (TableLayoutPanel-based for reliable text layout)
        Dim AddInfoCard = Sub(titleText As String, descText As String, iconText As String)
                              Dim card As New Panel() With {
                                  .Dock = DockStyle.Fill,
                                  .BackColor = Color.White,
                                  .Margin = New Padding(8),
                                  .Padding = New Padding(12)
                              }

                              ' Draw thin rounded border for the card
                              AddHandler card.Paint, Sub(s, pe)
                                                         Dim r = CType(s, Panel).ClientRectangle
                                                         If r.Width <= 0 Or r.Height <= 0 Then Return
                                                         Dim radius As Integer = 10
                                                         Using path As New Drawing2D.GraphicsPath()
                                                             path.StartFigure()
                                                             path.AddArc(r.Left, r.Top, radius, radius, 180, 90)
                                                             path.AddArc(r.Right - radius - 1, r.Top, radius, radius, 270, 90)
                                                             path.AddArc(r.Right - radius - 1, r.Bottom - radius - 1, radius, radius, 0, 90)
                                                             path.AddArc(r.Left, r.Bottom - radius - 1, radius, radius, 90, 90)
                                                             path.CloseFigure()
                                                             pe.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                                             Using pen As New Pen(Color.FromArgb(230, 230, 230), 1)
                                                                 pe.Graphics.DrawPath(pen, path)
                                                             End Using
                                                         End Using
                                                     End Sub

                              ' Use a TableLayoutPanel inside the card to ensure predictable stacking
                              Dim innerTL As New TableLayoutPanel() With {
                                  .Dock = DockStyle.Fill,
                                  .ColumnCount = 1,
                                  .RowCount = 3,
                                  .Padding = New Padding(6),
                                  .Margin = New Padding(0)
                              }
                              innerTL.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' icon
                              innerTL.RowStyles.Add(New RowStyle(SizeType.AutoSize))   ' title
                              innerTL.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' description (fills remaining)
                              card.Controls.Add(innerTL)

                              ' Icon area (purple square with center text icon)
                              Dim iconHolder As New Panel() With {
                                  .Size = New Size(44, 44),
                                  .BackColor = Color.FromArgb(138, 100, 255),
                                  .Margin = New Padding(4),
                                  .Padding = New Padding(0)
                              }
                              Dim iconLabel As New Label() With {
                                  .Text = iconText,
                                  .Font = New Font("Segoe UI", 12.0F, FontStyle.Regular),
                                  .ForeColor = Color.White,
                                  .TextAlign = ContentAlignment.MiddleCenter,
                                  .Dock = DockStyle.Fill
                              }
                              iconHolder.Controls.Add(iconLabel)
                              AddHandler iconHolder.Resize, Sub(s, ev)
                                                                ApplyRoundedCorners(iconHolder, 10)
                                                            End Sub
                              innerTL.Controls.Add(iconHolder, 0, 0)

                              ' Title
                              Dim t As New Label() With {
                                  .Text = titleText,
                                  .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
                                  .ForeColor = Color.Black,
                                  .AutoSize = True,
                                  .Margin = New Padding(6, 6, 6, 0)
                              }
                              innerTL.Controls.Add(t, 0, 1)

                              ' Description (wraps)
                              Dim d As New Label() With {
                                  .Text = descText,
                                  .Font = New Font("Segoe UI", 9.0F),
                                  .ForeColor = Color.FromArgb(110, 110, 110),
                                  .AutoSize = False,
                                  .Dock = DockStyle.Fill,
                                  .Margin = New Padding(6, 6, 6, 6)
                              }
                              innerTL.Controls.Add(d, 0, 2)

                              ' Keep rounded corners when card resizes
                              AddHandler card.Resize, Sub(s, ev)
                                                          Try
                                                              ApplyRoundedCorners(CType(s, Panel), 10)
                                                          Catch
                                                          End Try
                                                      End Sub

                              ' Add the card into the next cell
                              Dim columnIndex As Integer = featuresTable.Controls.Count Mod featuresTable.ColumnCount
                              featuresTable.Controls.Add(card, columnIndex, 0)
                          End Sub

        ' Add the four info cards
        AddInfoCard("Lesson Presentation", "Scroll through the lesson slides directly inside the app.", "🖥")
        AddInfoCard("Lesson Summary", "A short explanation of the key concepts.", "📄")
        AddInfoCard("Example Code", "Visual Basic examples for every concept.", "</>")
        AddInfoCard("Code Demonstration", "Interact with and run examples where appropriate.", "▶")

        ' ================== LESSON MENU (17 fixed lessons, two columns) ==================
        lessonMenuContainer = New Panel() With {
            .Dock = DockStyle.Top,
            .Padding = New Padding(24, 8, 24, 48),
            .BackColor = Color.White,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
        }
        scrollFlow.Controls.Add(lessonMenuContainer)

        Dim lessonMenuTitleLbl As New Label() With {
            .Text = "Lesson Menu",
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(8, 8)
        }
        lessonMenuContainer.Controls.Add(lessonMenuTitleLbl)

        Dim lessonMenuSubtitle As New Label() With {
            .Text = "Open any lesson from the Lessons menu above, or preview the topics here.",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(8, lessonMenuTitleLbl.Bottom + 4)
        }
        lessonMenuContainer.Controls.Add(lessonMenuSubtitle)

        Dim lessonMenuCount As New Label() With {
            .Text = "17 topics",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.Gray,
            .AutoSize = True
        }
        lessonMenuContainer.Controls.Add(lessonMenuCount)

        Dim positionLessonCount = Sub()
                                      lessonMenuCount.Location = New Point(
                                          lessonMenuContainer.Width - lessonMenuContainer.Padding.Right - lessonMenuCount.Width - 8,
                                          12)
                                  End Sub
        AddHandler lessonMenuContainer.Resize, Sub(s, ev) positionLessonCount()
        positionLessonCount()

        ' The 17 fixed lesson titles, in order. Week number always equals the lesson number.
        Dim lessonTitles() As String = {
            "Orientations",
            "Introduction in OOP",
            "Getting Started with Microsoft Visual Basic .NET",
            "Planning Applications and Designing Interfaces",
            "Data Handling",
            "Coding With Variables Name Constants and Calculations",
            "Arrays",
            "Working With Controls and Properties",
            "Midterm Examinations",
            "Debugging and Tracing",
            "Working with .NET Framework and MDI",
            "Database Connection",
            "Developing Data Driven Application",
            "Presentation",
            "Final Examination",
            "Animation",
            "Data Driven"
        }
        PopulateLessonsDropdown(lessonTitles)

        Dim lessonMenuTable As New TableLayoutPanel() With {
            .ColumnCount = 2,
            .RowCount = CInt(Math.Ceiling(lessonTitles.Length / 2.0)),
            .Location = New Point(8, lessonMenuSubtitle.Bottom + 20),
            .Width = Math.Max(760, scrollContainer.ClientSize.Width) - 40,
            .Height = 720,
            .Padding = New Padding(0),
            .Margin = New Padding(0)
        }
        lessonMenuTable.ColumnStyles.Clear()
        lessonMenuTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        lessonMenuTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        For i As Integer = 1 To lessonMenuTable.RowCount
            lessonMenuTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next
        lessonMenuContainer.Controls.Add(lessonMenuTable)

        Dim lessonRowRefs As New List(Of Tuple(Of Integer, Panel, Label))

        ' Builds one clickable lesson row: [number badge] [title] [week] [arrow]
        Dim MakeLessonRow = Function(lessonNumber As Integer, lessonTitle As String, weekNumber As Integer) As KeyValuePair(Of Panel, Label)
                                Dim row As New Panel() With {
                                    .Dock = DockStyle.Fill,
                                    .Height = 60,
                                    .Margin = New Padding(6),
                                    .Padding = New Padding(0),
                                    .BackColor = Color.FromArgb(244, 244, 247),
                                    .Cursor = Cursors.Hand
                                }

                                AddHandler row.Paint, Sub(s, pe)
                                                          Dim r = CType(s, Panel).ClientRectangle
                                                          If r.Width <= 0 Or r.Height <= 0 Then Return
                                                          Dim radius As Integer = 10
                                                          Using path As New Drawing2D.GraphicsPath()
                                                              path.StartFigure()
                                                              path.AddArc(r.Left, r.Top, radius, radius, 180, 90)
                                                              path.AddArc(r.Right - radius - 1, r.Top, radius, radius, 270, 90)
                                                              path.AddArc(r.Right - radius - 1, r.Bottom - radius - 1, radius, radius, 0, 90)
                                                              path.AddArc(r.Left, r.Bottom - radius - 1, radius, radius, 90, 90)
                                                              path.CloseFigure()
                                                              pe.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                                              Using pen As New Pen(Color.FromArgb(225, 225, 232), 1)
                                                                  pe.Graphics.DrawPath(pen, path)
                                                              End Using
                                                          End Using
                                                      End Sub

                                Dim inner As New TableLayoutPanel() With {
                                    .Dock = DockStyle.Fill,
                                    .ColumnCount = 3,
                                    .RowCount = 1,
                                    .Padding = New Padding(12, 0, 12, 0),
                                    .Margin = New Padding(0)
                                }
                                inner.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 50))
                                inner.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                                inner.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 90))
                                row.Controls.Add(inner)

                                Dim badge As New Panel() With {
                                    .Size = New Size(36, 36),
                                    .BackColor = Color.FromArgb(138, 100, 255),
                                    .Margin = New Padding(0, 12, 8, 12),
                                    .Anchor = AnchorStyles.Top
                                }
                                Dim badgeLabel As New Label() With {
                                    .Text = lessonNumber.ToString("00"),
                                    .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
                                    .ForeColor = Color.White,
                                    .Dock = DockStyle.Fill,
                                    .TextAlign = ContentAlignment.MiddleCenter
                                }
                                badge.Controls.Add(badgeLabel)
                                AddHandler badge.Resize, Sub(s, ev) ApplyRoundedCorners(badge, 8)
                                inner.Controls.Add(badge, 0, 0)

                                Dim titleLbl As New Label() With {
                                        .Text = lessonTitle,
                                        .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
                                        .ForeColor = Color.Black,
                                        .Dock = DockStyle.Fill,
                                        .TextAlign = ContentAlignment.TopLeft,
                                        .Padding = New Padding(0, 10, 0, 0),
                                        .AutoEllipsis = False,
                                        .Margin = New Padding(0)
                                }
                                inner.Controls.Add(titleLbl, 1, 0)

                                Dim rightPanel As New Panel() With {
                                    .Dock = DockStyle.Fill,
                                    .Margin = New Padding(0)
                                }
                                Dim weekLbl As New Label() With {
                                    .Text = "Week " & weekNumber.ToString(),
                                    .Font = New Font("Segoe UI", 9.0F),
                                    .ForeColor = Color.Gray,
                                    .AutoSize = True
                                }
                                Dim arrowLbl As New Label() With {
                                    .Text = "→",
                                    .Font = New Font("Segoe UI", 11.0F),
                                    .ForeColor = Color.Gray,
                                    .AutoSize = True
                                }
                                rightPanel.Controls.Add(weekLbl)
                                rightPanel.Controls.Add(arrowLbl)
                                AddHandler rightPanel.Resize, Sub(s, ev)
                                                                  weekLbl.Location = New Point(
                                                                        rightPanel.Width - weekLbl.Width - arrowLbl.Width - 10,
                                                                        12)
                                                                  arrowLbl.Location = New Point(
                                                                        rightPanel.Width - arrowLbl.Width,
                                                                        12)
                                                              End Sub
                                inner.Controls.Add(rightPanel, 2, 0)

                                Dim clickHandler As EventHandler =
                                    Sub(s, ev)

                                        If lessonNumber = 1 Then

                                            Dim orientationWindow As New OrientationForm()
                                            orientationWindow.Show()

                                        ElseIf lessonNumber = 2 Then

                                            Dim week2Window As New Week2Form()
                                            week2Window.Show()

                                        ElseIf lessonNumber = 3 Then

                                            Dim week3Window As New Week3Form()
                                            week3Window.Show()

                                        ElseIf lessonNumber = 4 Then

                                            Dim week4Window As New Week4Form()
                                            week4Window.Show()

                                        ElseIf lessonNumber = 5 Then

                                            Dim week5Window As New Week5Form()
                                            week5Window.Show()

                                        ElseIf lessonNumber = 6 Then

                                            Dim week6Window As New Week6Form()
                                            week6Window.Show()

                                        ElseIf lessonNumber = 7 Then

                                            Dim week7Window As New Week7Form()
                                            week7Window.Show()

                                        ElseIf lessonNumber = 8 Then

                                            Dim week8Window As New Week8Form()
                                            week8Window.Show()

                                        ElseIf lessonNumber = 16 Then

                                            ' Open the existing Level 1 Animation game.
                                            OpenAnimationLevel1(s, ev)

                                        Else

                                            MessageBox.Show(
                                                "Lesson " & lessonNumber & " " &
                                                ChrW(8212) & " " & lessonTitle &
                                                " will be implemented in a future update.",
                                                "Lesson " & lessonNumber,
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Information)

                                        End If

                                    End Sub
                                AttachClickToAll(row, clickHandler)

                                Return New KeyValuePair(Of Panel, Label)(row, titleLbl)
                            End Function

        For i As Integer = 1 To lessonTitles.Length
            Dim num As Integer = i
            Dim title As String = lessonTitles(i - 1)
            Dim col As Integer = (num - 1) Mod 2
            Dim rowIndex As Integer = (num - 1) \ 2
            Dim pair = MakeLessonRow(num, title, num)
            lessonMenuTable.Controls.Add(pair.Key, col, rowIndex)
            lessonRowRefs.Add(New Tuple(Of Integer, Panel, Label)(rowIndex, pair.Key, pair.Value))
        Next

        lessonMenuTable.PerformLayout()

        Dim AdjustLessonRowHeights = Sub()
                                         Dim maxHeightByRow As New Dictionary(Of Integer, Integer)
                                         For Each info In lessonRowRefs
                                             Dim rowIndex = info.Item1
                                             Dim rowCtrl = info.Item2
                                             Dim lbl = info.Item3
                                             rowCtrl.PerformLayout()
                                             Dim columnWidth As Integer = lessonMenuTable.Width \ 2
                                             Dim availWidth As Integer = Math.Max(80, columnWidth - 180) ' 12 row margin + 24 inner padding + 50 badge col + 90 arrow col + buffer
                                             Dim measured As Size = TextRenderer.MeasureText(
                                                 lbl.Text, lbl.Font,
                                                 New Size(availWidth, Integer.MaxValue),
                                                 TextFormatFlags.WordBreak Or TextFormatFlags.Left Or TextFormatFlags.NoPrefix)
                                             Dim neededHeight As Integer = Math.Max(64, measured.Height + 34)
                                             If Not maxHeightByRow.ContainsKey(rowIndex) OrElse neededHeight > maxHeightByRow(rowIndex) Then
                                                 maxHeightByRow(rowIndex) = neededHeight
                                             End If
                                         Next
                                         Dim totalHeight As Integer = 0
                                         For Each kvp In maxHeightByRow
                                             If kvp.Key >= 0 AndAlso kvp.Key < lessonMenuTable.RowStyles.Count Then
                                                 lessonMenuTable.RowStyles(kvp.Key) = New RowStyle(SizeType.Absolute, kvp.Value)
                                                 totalHeight += kvp.Value
                                             End If
                                         Next
                                         lessonMenuTable.Height = totalHeight
                                         lessonMenuTable.PerformLayout()
                                     End Sub
        AdjustLessonRowHeights()

        ' Keep the lesson menu full-width as the window is resized, without touching your existing Resize handler.
        AddHandler Me.Resize, Sub(s, ev)
                                  lessonMenuTable.Width = Math.Max(760, scrollContainer.ClientSize.Width) - 40
                                  lessonMenuTable.PerformLayout()
                                  AdjustLessonRowHeights()
                              End Sub

        ' --- Form resize handling: keep header/stats/content widths in sync and reposition header contents ---
        AddHandler Me.Resize, Sub(s, ev)
                                  Dim w = Math.Max(800, scrollContainer.ClientSize.Width)
                                  If navPanel IsNot Nothing Then navPanel.Width = w
                                  If headerPanel IsNot Nothing Then
                                      headerPanel.Width = w
                                      headerPanel.PerformLayout()
                                      headerPanel.Invalidate()
                                  End If
                                  If contentPanel IsNot Nothing Then contentPanel.Width = w
                                  If statsTable IsNot Nothing Then statsTable.Width = w
                                  If featuresTable IsNot Nothing Then featuresTable.Width = w
                                  ' re-place header element positions and reapply rounded corners
                                  If headerPanel IsNot Nothing Then
                                      titleLabel.Location = New Point(headerPanel.Padding.Left, headerPanel.Padding.Top)
                                      descLabel.Location = New Point(headerPanel.Padding.Left, titleLabel.Bottom + 12)
                                      Dim left As Integer = headerPanel.Padding.Left
                                      Dim marginBottom As Integer = headerPanel.Padding.Bottom
                                      Dim yButtons As Integer = Math.Max(descLabel.Bottom + 16, headerPanel.ClientSize.Height - heroBrowseButton.Height - marginBottom)
                                      heroBrowseButton.Location = New Point(left, yButtons)
                                      heroGettingStartedButton.Location = New Point(left + heroBrowseButton.Width + 16, yButtons)
                                      ApplyRoundedCorners(heroBrowseButton, 8)
                                      ApplyRoundedCorners(heroGettingStartedButton, 8)
                                  End If
                              End Sub

        ' Enable double-buffering to reduce flicker and tearing
        EnableDoubleBuffering(Me)
        EnableDoubleBuffering(scrollContainer)
        EnableDoubleBuffering(scrollFlow)
        EnableDoubleBuffering(headerPanel)
        If statsTable IsNot Nothing Then EnableDoubleBuffering(statsTable)

        AddHandler Me.FormClosed, Sub(s, ev)
                                      If headerBackgroundCache IsNot Nothing Then
                                          Try
                                              headerBackgroundCache.Dispose()
                                          Catch
                                          End Try
                                          headerBackgroundCache = Nothing
                                      End If
                                  End Sub

        ' Attach scrolling handlers after the welcome page controls are created
        AttachWelcomeScrollHandlers(scrollContainer)
    End Sub

    ' Paint handler draws gradient + subtle dotted overlay for the header
    Private Sub HeaderPanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality

        Dim rect = headerPanel.ClientRectangle
        If rect.Width <= 0 Or rect.Height <= 0 Then Return

        ' Recreate cached background only when size changes
        If headerBackgroundCache Is Nothing OrElse headerBackgroundSize <> rect.Size Then
            If headerBackgroundCache IsNot Nothing Then
                Try
                    headerBackgroundCache.Dispose()
                Catch
                End Try
                headerBackgroundCache = Nothing
            End If

            headerBackgroundCache = New Bitmap(rect.Width, rect.Height)
            headerBackgroundSize = rect.Size

            Using bg As Graphics = Graphics.FromImage(headerBackgroundCache)
                bg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality

                Using brush As New System.Drawing.Drawing2D.LinearGradientBrush(rect, Color.FromArgb(26, 26, 28), Color.FromArgb(64, 43, 220), System.Drawing.Drawing2D.LinearGradientMode.Vertical)
                    bg.FillRectangle(brush, rect)
                End Using

                Using dotBrush As New SolidBrush(Color.FromArgb(18, 255, 255, 255))
                    Dim stepX As Integer = 24
                    Dim stepY As Integer = 24
                    Dim dotSize As Integer = 2
                    For x As Integer = 0 To rect.Width Step stepX
                        For y As Integer = 0 To rect.Height Step stepY
                            bg.FillEllipse(dotBrush, x, y, dotSize, dotSize)
                        Next
                    Next
                End Using
            End Using
        End If

        If headerBackgroundCache IsNot Nothing Then
            e.Graphics.DrawImageUnscaled(headerBackgroundCache, 0, 0)
        End If
    End Sub

    ' --- Button handlers (placeholders) ---
    Private Sub LessonsButton_Click(sender As Object, e As EventArgs)
        If lessonsDropdownOpen Then
            CloseLessonsDropdown()
        Else
            OpenLessonsDropdown()
        End If
    End Sub

    Private Sub HeroGettingStartedButton_Click(
    sender As Object, e As EventArgs)

        Dim orientationWindow As New OrientationForm()
        orientationWindow.Show()

    End Sub

    Private Sub HeroBrowseButton_Click(
    sender As Object, e As EventArgs)

        ' Make sure the Welcome page is displayed.
        If isHelpViewActive Then
            ShowWelcomePage()
        End If

        ' Refresh the layout before scrolling.
        scrollContainer.PerformLayout()
        scrollFlow.PerformLayout()

        ' Scroll down to the Lesson Menu section.
        scrollContainer.AutoScrollPosition =
        New Point(0, Math.Max(0, lessonMenuContainer.Top - 8))

    End Sub

    Private Sub OpenLessonsDropdown()
        PositionLessonsDropdown()
        lessonsDropdownPanel.BringToFront()
        lessonsDropdownPanel.Visible = True
        lessonsDropdownOpen = True
        lessonsButton.Text = "Lessons  ▴"
    End Sub

    Private Sub CloseLessonsDropdown()
        lessonsDropdownPanel.Visible = False
        lessonsDropdownOpen = False
        lessonsButton.Text = "Lessons  ▾"
    End Sub


    ' Close the Lessons dropdown when the welcome page is scrolled
    Private Sub WelcomePage_MouseWheel(sender As Object, e As MouseEventArgs)
        If lessonsDropdownOpen Then
            CloseLessonsDropdown()
        End If
    End Sub

    ' Close the Lessons dropdown when a scrollable control scrolls
    Private Sub WelcomePage_Scrolled(sender As Object, e As ScrollEventArgs)
        If lessonsDropdownOpen Then
            CloseLessonsDropdown()
        End If
    End Sub

    ' Attach scrolling handlers to the welcome page and its child controls
    Private Sub AttachWelcomeScrollHandlers(parent As Control)

        If parent Is Nothing Then Return

        ' Detect mouse-wheel scrolling on this control
        AddHandler parent.MouseWheel, AddressOf WelcomePage_MouseWheel

        ' Only ScrollableControl supports the Scroll event
        If TypeOf parent Is ScrollableControl Then
            AddHandler DirectCast(parent, ScrollableControl).Scroll,
                   AddressOf WelcomePage_Scrolled
        End If

        ' Attach the handlers to all existing child controls
        For Each child As Control In parent.Controls
            AttachWelcomeScrollHandlers(child)
        Next

    End Sub

    Private Sub PositionLessonsDropdown()
        If lessonsDropdownPanel Is Nothing OrElse lessonsButton Is Nothing Then Return
        Dim screenPt As Point = lessonsButton.PointToScreen(New Point(0, lessonsButton.Height))
        Dim clientPt As Point = Me.PointToClient(screenPt)
        lessonsDropdownPanel.Location = clientPt
    End Sub

    Private Sub HelpButton_Click(sender As Object, e As EventArgs)
        ShowHelpPage()
    End Sub

    ' ================== HELP PAGE (swaps into the existing Welcome window/scroll area) ==================

    Private Sub ShowHelpPage()
        If isHelpViewActive Then Return
        isHelpViewActive = True

        If lessonsDropdownOpen Then CloseLessonsDropdown()

        headerPanel.Visible = False
        welcomeSeparator.Visible = False
        statsTable.Visible = False
        featuresContainer.Visible = False
        lessonMenuContainer.Visible = False

        If helpContentPanel Is Nothing Then
            BuildHelpContent()
        End If
        helpContentPanel.Visible = True

        scrollContainer.AutoScrollPosition = New Point(0, 0)
    End Sub

    Private Sub ShowWelcomePage()
        isHelpViewActive = False

        If helpContentPanel IsNot Nothing Then helpContentPanel.Visible = False

        headerPanel.Visible = True
        welcomeSeparator.Visible = True
        statsTable.Visible = True
        featuresContainer.Visible = True
        lessonMenuContainer.Visible = True

        scrollContainer.AutoScrollPosition = New Point(0, 0)
    End Sub

    ' Builds the Help content once, adds it into scrollFlow right after the Welcome
    ' sections, and leaves it hidden until ShowHelpPage() reveals it.
    Private Sub BuildHelpContent()
        helpContentPanel = New Panel() With {
            .Dock = DockStyle.Top,
            .Padding = New Padding(24, 24, 24, 48),
            .BackColor = Color.White,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Visible = False
        }
        scrollFlow.Controls.Add(helpContentPanel)

        Dim titleLbl As New Label() With {
            .Text = "Help & User Guide",
            .Font = New Font("Segoe UI", 20.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(8, 8)
        }
        helpContentPanel.Controls.Add(titleLbl)

        Dim subtitleLbl As New Label() With {
            .Text = "What this program is, and how to use it.",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Location = New Point(8, titleLbl.Bottom + 4)
        }
        helpContentPanel.Controls.Add(subtitleLbl)

        Dim currentBottom As Integer = subtitleLbl.Bottom + 24

        currentBottom = AddHelpCard(helpContentPanel, currentBottom,
            "About the Program",
            "PF101 " & ChrW(8212) & " Object Oriented Programming is a digital study guide designed to help students review their PF101 lessons in one organized application. It provides presentation slides, lesson materials, and practical Visual Basic examples to support learning and understanding.")

        currentBottom = AddHelpCard(helpContentPanel, currentBottom,
            "Subject Description",
            "PF101 introduces students to Object Oriented Programming concepts and their application in programming. The application organizes learning materials by week, so students can review each topic in a structured way.")

        currentBottom = AddHelpCard(helpContentPanel, currentBottom,
            "Why This Program Was Created",
            "The program was created to make reviewing lessons more convenient and organized. Instead of searching through separate presentation files, students can access all their learning materials in one place and review examples that help them understand the concepts.")

        currentBottom = AddMenusFeaturesCard(helpContentPanel, currentBottom)
        currentBottom = AddHowToUseCard(helpContentPanel, currentBottom)

        Dim returnBtn As New Button() With {
            .Text = ChrW(8592) & "  Return to Welcome",
            .AutoSize = True,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Padding = New Padding(12, 8, 12, 8)
        }
        returnBtn.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210)
        returnBtn.FlatAppearance.BorderSize = 1
        AddHandler returnBtn.Click, Sub(s, ev) ShowWelcomePage()
        helpContentPanel.Controls.Add(returnBtn)

        Dim positionReturnBtn As Action = Sub()
                                              returnBtn.Location = New Point(helpContentPanel.Width - helpContentPanel.Padding.Right - returnBtn.Width - 8, currentBottom + 8)
                                          End Sub
        AddHandler helpContentPanel.Resize, Sub(s, ev) positionReturnBtn()
        positionReturnBtn()
    End Sub

    ' Adds one simple bordered card with a bold title and a wrapping paragraph.
    ' Returns the Y position where the next section should start.
    Private Function AddHelpCard(container As Panel, topY As Integer, titleText As String, bodyText As String) As Integer
        Dim cardWidth As Integer = Math.Max(300, Math.Max(760, scrollContainer.ClientSize.Width) - 40)

        Dim card As New Panel() With {
            .BackColor = Color.White,
            .Location = New Point(8, topY),
            .Width = cardWidth
        }
        AddHandler card.Paint, AddressOf DrawHelpCardBorder

        Dim cardTitleLbl As New Label() With {
            .Text = titleText,
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        card.Controls.Add(cardTitleLbl)

        Dim bodyLbl As New Label() With {
            .Text = bodyText,
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .AutoSize = True,
            .MaximumSize = New Size(cardWidth - 32, 0),
            .Location = New Point(16, cardTitleLbl.Bottom + 8)
        }
        card.Controls.Add(bodyLbl)

        card.Height = bodyLbl.Bottom + 16
        container.Controls.Add(card)

        AddHandler container.Resize, Sub(s, ev)
                                         Dim newWidth As Integer = Math.Max(300, Math.Max(760, scrollContainer.ClientSize.Width) - 40)
                                         card.Width = newWidth
                                         bodyLbl.MaximumSize = New Size(newWidth - 32, 0)
                                         card.Height = bodyLbl.Bottom + 16
                                     End Sub

        Return topY + card.Height + 16
    End Function

    Private Sub DrawHelpCardBorder(sender As Object, e As PaintEventArgs)
        Dim card = CType(sender, Panel)
        Dim r As New Rectangle(0, 0, card.Width - 1, card.Height - 1)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Dim radius As Integer = 10
        Using path As New Drawing2D.GraphicsPath()
            path.StartFigure()
            path.AddArc(r.Left, r.Top, radius, radius, 180, 90)
            path.AddArc(r.Right - radius, r.Top, radius, radius, 270, 90)
            path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90)
            path.AddArc(r.Left, r.Bottom - radius, radius, radius, 90, 90)
            path.CloseFigure()
            Using pen As New Pen(Color.FromArgb(228, 228, 235), 1)
                e.Graphics.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    Private Function AddMenusFeaturesCard(container As Panel, topY As Integer) As Integer
        Dim cardWidth As Integer = Math.Max(300, Math.Max(760, scrollContainer.ClientSize.Width) - 40)

        Dim card As New Panel() With {
            .BackColor = Color.White,
            .Location = New Point(8, topY),
            .Width = cardWidth
        }
        AddHandler card.Paint, AddressOf DrawHelpCardBorder

        Dim cardTitleLbl As New Label() With {
            .Text = "Program Menus and Features",
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        card.Controls.Add(cardTitleLbl)

        Dim introLbl As New Label() With {
            .Text = "The welcome window has three menus at the top:",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .AutoSize = True,
            .Location = New Point(16, cardTitleLbl.Bottom + 8)
        }
        card.Controls.Add(introLbl)

        Dim innerWidth As Integer = cardWidth - 32
        Dim rowY As Integer = introLbl.Bottom + 12

        Dim MakeMenuRow = Function(rowName As String, rowDesc As String, y As Integer) As Panel
                              Dim row As New Panel() With {
                                   .BackColor = Color.FromArgb(247, 247, 249),
                                   .Location = New Point(16, y),
                                   .Width = innerWidth
                               }
                              Dim nameLbl As New Label() With {
                                   .Text = rowName,
                                   .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
                                   .ForeColor = Color.Black,
                                   .AutoSize = True,
                                   .Location = New Point(12, 10)
                               }
                              row.Controls.Add(nameLbl)
                              Dim descLbl As New Label() With {
                                   .Text = rowDesc,
                                   .Font = New Font("Segoe UI", 9.0F),
                                   .ForeColor = Color.Gray,
                                   .AutoSize = True,
                                   .MaximumSize = New Size(innerWidth - 24, 0),
                                   .Location = New Point(12, nameLbl.Bottom + 2)
                               }
                              row.Controls.Add(descLbl)
                              row.Height = descLbl.Bottom + 10
                              Return row
                          End Function

        Dim lessonsRow = MakeMenuRow("Lessons", "Browse the available weekly lessons and open their corresponding learning windows.", rowY)
        card.Controls.Add(lessonsRow)
        rowY = lessonsRow.Bottom + 8

        Dim helpRow = MakeMenuRow("Help", "Read information about the program, its purpose, and how to use it.", rowY)
        card.Controls.Add(helpRow)
        rowY = helpRow.Bottom + 8

        Dim exitRow = MakeMenuRow("Exit", "Leave the application.", rowY)
        card.Controls.Add(exitRow)
        rowY = exitRow.Bottom + 12

        Dim noteLbl As New Label() With {
            .Text = "In each lesson window, you can view the presentation slides, read the lesson materials, and use the interactive example or activity when one is available. The Back and Continue buttons let you move between lessons.",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .AutoSize = True,
            .MaximumSize = New Size(innerWidth, 0),
            .Location = New Point(16, rowY)
        }
        card.Controls.Add(noteLbl)

        card.Height = noteLbl.Bottom + 16
        container.Controls.Add(card)

        AddHandler container.Resize, Sub(s, ev)
                                         Dim newWidth As Integer = Math.Max(300, Math.Max(760, scrollContainer.ClientSize.Width) - 40)
                                         card.Width = newWidth
                                         Dim newInner As Integer = newWidth - 32
                                         lessonsRow.Width = newInner
                                         helpRow.Width = newInner
                                         exitRow.Width = newInner
                                         noteLbl.MaximumSize = New Size(newInner, 0)
                                         card.Height = noteLbl.Bottom + 16
                                     End Sub

        Return topY + card.Height + 16
    End Function

    Private Function AddHowToUseCard(container As Panel, topY As Integer) As Integer
        Dim cardWidth As Integer = Math.Max(300, Math.Max(760, scrollContainer.ClientSize.Width) - 40)

        Dim card As New Panel() With {
            .BackColor = Color.White,
            .Location = New Point(8, topY),
            .Width = cardWidth
        }
        AddHandler card.Paint, AddressOf DrawHelpCardBorder

        Dim cardTitleLbl As New Label() With {
            .Text = "How to Use the Program",
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        card.Controls.Add(cardTitleLbl)

        Dim steps() As String = {
            "Open the Lessons menu on the welcome page.",
            "Select the week or lesson you want to study.",
            "Read the presentation slides in the lesson window.",
            "Explore the example or interactive activity on the right side when one is available.",
            "Use the Back and Continue buttons to move between lessons.",
            "Return to the welcome page or use the available navigation options when you want to explore another lesson.",
            "Open the Help page whenever you need a reminder about the program."
        }

        Dim innerWidth As Integer = cardWidth - 32
        Dim y As Integer = cardTitleLbl.Bottom + 10
        Dim stepLabels As New List(Of Label)

        For i As Integer = 0 To steps.Length - 1
            Dim stepLbl As New Label() With {
                .Text = (i + 1).ToString() & ". " & steps(i),
                .Font = New Font("Segoe UI", 9.5F),
                .ForeColor = Color.FromArgb(50, 50, 50),
                .AutoSize = True,
                .MaximumSize = New Size(innerWidth, 0),
                .Location = New Point(16, y)
            }
            card.Controls.Add(stepLbl)
            stepLabels.Add(stepLbl)
            y = stepLbl.Bottom + 8
        Next

        card.Height = y + 8
        container.Controls.Add(card)

        AddHandler container.Resize, Sub(s, ev)
                                         Dim newWidth As Integer = Math.Max(300, Math.Max(760, scrollContainer.ClientSize.Width) - 40)
                                         card.Width = newWidth
                                         Dim newInner As Integer = newWidth - 32
                                         Dim yy As Integer = cardTitleLbl.Bottom + 10
                                         For Each lbl In stepLabels
                                             lbl.MaximumSize = New Size(newInner, 0)
                                             lbl.Location = New Point(16, yy)
                                             yy = lbl.Bottom + 8
                                         Next
                                         card.Height = yy + 8
                                     End Sub

        Return topY + card.Height + 16
    End Function

    Private Sub ExitButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    ' Helper to apply gentle rounded corners to a control (call after size/position is stable)
    Private Sub ApplyRoundedCorners(ctrl As Control, radius As Integer)
        Try
            If ctrl Is Nothing OrElse ctrl.Width <= 0 OrElse ctrl.Height <= 0 Then Return
            Dim path As New System.Drawing.Drawing2D.GraphicsPath()
            Dim r As Integer = radius
            Dim rect As Rectangle = New Rectangle(0, 0, ctrl.Width, ctrl.Height)
            path.StartFigure()
            path.AddArc(rect.Left, rect.Top, r, r, 180, 90)
            path.AddArc(rect.Right - r, rect.Top, r, r, 270, 90)
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90)
            path.AddArc(rect.Left, rect.Bottom - r, r, r, 90, 90)
            path.CloseFigure()
            ctrl.Region = New Region(path)
        Catch ex As Exception
            ' If rounding fails for any reason, silently ignore so UI still works
        End Try
    End Sub

    ' Makes an entire composite control (and everything inside it) act as one clickable unit.
    Private Sub AttachClickToAll(ctrl As Control, handler As EventHandler)
        AddHandler ctrl.Click, handler
        For Each child As Control In ctrl.Controls
            AttachClickToAll(child, handler)
        Next
    End Sub

    ' Fills the Lessons nav dropdown with one clickable row per lesson (single column, fixed width).
    Private Sub PopulateLessonsDropdown(titles() As String)
        lessonsDropdownContent.Controls.Clear()

        Dim rowWidth As Integer = 332
        Dim badgeSize As Integer = 32
        Dim weekColWidth As Integer = 40

        Dim stack As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Width = rowWidth,
            .Padding = New Padding(8, 8, 8, 8),
            .Margin = New Padding(0)
        }
        lessonsDropdownContent.Controls.Add(stack)

        Dim titleFont As New Font("Segoe UI", 9.5F, FontStyle.Regular)
        Dim availTitleWidth As Integer = rowWidth - badgeSize - weekColWidth - 24

        For i As Integer = 1 To titles.Length
            Dim lessonNumber As Integer = i
            Dim lessonTitle As String = titles(i - 1)
            Dim weekNumber As Integer = i

            Dim measured As Size = TextRenderer.MeasureText(
                lessonTitle, titleFont,
                New Size(Math.Max(60, availTitleWidth), Integer.MaxValue),
                TextFormatFlags.WordBreak Or TextFormatFlags.Left Or TextFormatFlags.NoPrefix)
            Dim rowHeight As Integer = Math.Max(48, measured.Height + 24)
            Dim rowW As Integer = rowWidth - 16

            Dim row As New Panel() With {
                .Size = New Size(rowW, rowHeight),
                .Margin = New Padding(8, 0, 0, 8),
                .BackColor = Color.White,
                .Cursor = Cursors.Hand
            }

            Dim badge As New Panel() With {
                .Size = New Size(badgeSize, badgeSize),
                .Location = New Point(0, 8),
                .BackColor = Color.FromArgb(231, 231, 238)
            }
            Dim badgeLabel As New Label() With {
                .Text = lessonNumber.ToString("00"),
                .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
                .ForeColor = Color.Black,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            badge.Controls.Add(badgeLabel)
            AddHandler badge.Resize, Sub(s, ev) ApplyRoundedCorners(badge, 6)
            row.Controls.Add(badge)

            Dim titleLbl As New Label() With {
                .Text = lessonTitle,
                .Font = titleFont,
                .ForeColor = Color.FromArgb(30, 30, 30),
                .Location = New Point(badgeSize + 10, 6),
                .Size = New Size(availTitleWidth - 20, rowHeight - 8),
                .TextAlign = ContentAlignment.TopLeft
            }
            row.Controls.Add(titleLbl)

            If lessonNumber = 2 OrElse
                lessonNumber = 6 OrElse
                lessonNumber = 16 Then
                Dim chevron As New Label() With {
                    .Text = If(
                        (lessonNumber = 2 AndAlso week2SubmenuOpen) OrElse
                        (lessonNumber = 6 AndAlso week6SubmenuOpen) OrElse
                        (lessonNumber = 16 AndAlso animationSubmenuOpen),
                    ChrW(9662),
                ChrW(9656)),
                    .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
                    .ForeColor = Color.Gray,
                    .AutoSize = True,
                    .Location = New Point(rowW - 20, 8)
                }
                If lessonNumber = 2 Then
                    week2ChevronLabel = chevron
                ElseIf lessonNumber = 6 Then
                    week6ChevronLabel = chevron
                ElseIf lessonNumber = 16 Then
                    animationChevronLabel = chevron
                End If
                row.Controls.Add(chevron)
            Else
                Dim weekLbl As New Label() With {
                    .Text = "W" & weekNumber.ToString(),
                    .Font = New Font("Segoe UI", 8.5F),
                    .ForeColor = Color.Gray,
                    .AutoSize = True,
                    .Location = New Point(rowW - weekColWidth, 8)
                }
                row.Controls.Add(weekLbl)
            End If

            If lessonNumber = 2 Then

                Dim toggleHandler As EventHandler =
                    Sub(s, ev)
                        week2SubmenuOpen = Not week2SubmenuOpen
                        RebuildLessonsDropdown()
                    End Sub

                AttachClickToAll(row, toggleHandler)
                stack.Controls.Add(row)

                If week2SubmenuOpen Then
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Classes and Objects", rowW, AddressOf OpenClassesAndObjects))
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Encapsulation", rowW, AddressOf OpenEncapsulation))
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Inheritance", rowW, AddressOf OpenInheritance))
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Polymorphism", rowW, AddressOf OpenPolymorphism))
                End If

            ElseIf lessonNumber = 6 Then

                Dim toggle6 As EventHandler =
                    Sub(s, ev)
                        week6SubmenuOpen = Not week6SubmenuOpen
                        RebuildLessonsDropdown()
                    End Sub

                AttachClickToAll(row, toggle6)
                stack.Controls.Add(row)

                If week6SubmenuOpen Then
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Truth Table", rowW, AddressOf OpenWeek6TruthTable))
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Stopwatch Timer", rowW, AddressOf OpenWeek6Timer))
                End If

            ElseIf lessonNumber = 16 Then

                Dim toggleAnimation As EventHandler =
                    Sub(s, ev)
                        animationSubmenuOpen = Not animationSubmenuOpen
                        RebuildLessonsDropdown()
                    End Sub

                AttachClickToAll(row, toggleAnimation)
                stack.Controls.Add(row)

                If animationSubmenuOpen Then
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Level 1", rowW, AddressOf OpenAnimationLevel1))
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Level 2", rowW, AddressOf OpenAnimationLevel2))
                    stack.Controls.Add(MakeWeek2SubtopicRow(
                        "Level 3", rowW, AddressOf OpenAnimationLevel3))
                End If

            Else

                Dim clickHandler As EventHandler =
                    Sub(s, ev)

                        CloseLessonsDropdown()

                        If lessonNumber = 1 Then
                            Dim w As New OrientationForm()
                            w.Show()

                        ElseIf lessonNumber = 3 Then
                            Dim w As New Week3Form()
                            w.Show()

                        ElseIf lessonNumber = 4 Then
                            Dim w As New Week4Form()
                            w.Show()

                        ElseIf lessonNumber = 5 Then
                            Dim w As New Week5Form()
                            w.Show()

                        ElseIf lessonNumber = 7 Then
                            Dim w As New Week7Form()
                            w.Show()

                        ElseIf lessonNumber = 8 Then
                            Dim w As New Week8Form()
                            w.Show()

                        Else
                            MessageBox.Show(
                                "Lesson " & lessonNumber & " " &
                                ChrW(8212) & " " & lessonTitle &
                                " will be implemented in a future update.",
                                "Lesson " & lessonNumber,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                        End If

                    End Sub

                AttachClickToAll(row, clickHandler)
                stack.Controls.Add(row)

            End If
        Next
    End Sub

    Private Sub RebuildLessonsDropdown()

        ' Remember the current scroll position before rebuilding.
        Dim previousScrollY As Integer = 0

        If lessonsDropdownContent IsNot Nothing Then
            previousScrollY =
            Math.Abs(lessonsDropdownContent.AutoScrollPosition.Y)
        End If

        Dim lessonTitles() As String = {
        "Orientations",
        "Introduction in OOP",
        "Getting Started with Microsoft Visual Basic .NET",
        "Planning Applications and Designing Interfaces",
        "Data Handling",
        "Coding With Variables Name Constants and Calculations",
        "Arrays",
        "Working With Controls and Properties",
        "Midterm Examinations",
        "Debugging and Tracing",
        "Working with .NET Framework and MDI",
        "Database Connection",
        "Developing Data Driven Application",
        "Presentation",
        "Final Examination",
        "Animation",
        "Data Driven"
    }

        PopulateLessonsDropdown(lessonTitles)

        ' Restore the scroll position after the new rows are laid out.
        If lessonsDropdownContent IsNot Nothing AndAlso
       lessonsDropdownContent.IsHandleCreated Then

            Me.BeginInvoke(New MethodInvoker(
            Sub()
                If lessonsDropdownContent IsNot Nothing AndAlso
                   Not lessonsDropdownContent.IsDisposed Then

                    lessonsDropdownContent.AutoScrollPosition =
                        New Point(0, previousScrollY)
                End If
            End Sub))
        End If

    End Sub

    ' Builds one indented Week 2 subtopic row (no number badge, arrow bullet instead).
    Private Function MakeWeek2SubtopicRow(subtopicText As String, rowW As Integer, clickAction As EventHandler) As Panel
        Dim row As New Panel() With {
            .Size = New Size(rowW, 40),
            .Margin = New Padding(8, 0, 0, 4),
            .BackColor = Color.FromArgb(248, 248, 250),
            .Cursor = Cursors.Hand
        }

        Dim bulletLbl As New Label() With {
            .Text = ChrW(8226),
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = Color.FromArgb(150, 150, 150),
            .AutoSize = True,
            .Location = New Point(16, 10)
        }
        row.Controls.Add(bulletLbl)

        Dim textLbl As New Label() With {
            .Text = subtopicText,
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(40, 40, 40),
            .AutoSize = True,
            .Location = New Point(34, 10)
        }
        row.Controls.Add(textLbl)

        AttachClickToAll(row, clickAction)
        Return row
    End Function

    Private Sub OpenWeek6TruthTable(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Dim w As New Week6Form()
        w.Show()
    End Sub

    Private Sub OpenWeek6Timer(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Dim w As New Week6TimerForm()
        w.Show()
    End Sub

    Private Sub OpenClassesAndObjects(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Dim classesWindow As New Week2ClassesAndObjectsForm()
        classesWindow.Show()
    End Sub

    Private Sub OpenAnimationLevel1(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Try
            Dim menu As New Level1MenuForm()
            menu.StartPosition = FormStartPosition.CenterParent
            menu.Show()
        Catch ex As Exception
            MessageBox.Show("Level 1 menu could not be opened." & vbCrLf & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OpenAnimationLevel2(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Try
            ' Only one Level 2 menu may exist at a time.
            If Level2MenuForm.IsOpen() Then
                Level2MenuForm.BringExistingToFront()
                Return
            End If

            Dim level2Menu As New Level2MenuForm()
            level2Menu.StartPosition = FormStartPosition.CenterParent
            level2Menu.Show()
        Catch ex As Exception
            MessageBox.Show("Level 2 menu could not be opened." & vbCrLf & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OpenAnimationLevel3(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Try
            ' Only one Cannon Ball menu may exist at a time.
            If CannonBallMenuForm.IsOpen() Then
                CannonBallMenuForm.BringExistingToFront()
                Return
            End If

            Dim cannonMenu As New CannonBallMenuForm()
            cannonMenu.StartPosition = FormStartPosition.CenterParent
            cannonMenu.Show()
        Catch ex As Exception
            MessageBox.Show("Cannon Ball menu could not be opened." & vbCrLf & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OpenEncapsulation(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Dim week2Window As New Week2Form()
        week2Window.Show()
    End Sub

    Private Sub OpenInheritance(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Dim inheritanceWindow As New Week2InheritanceForm()
        inheritanceWindow.Show()
    End Sub

    Private Sub OpenPolymorphism(sender As Object, e As EventArgs)
        CloseLessonsDropdown()
        Dim polymorphismWindow As New Week2PolymorphismForm()
        polymorphismWindow.Show()
    End Sub

    Private Sub EnableDoubleBuffering(ctrl As Control)
        Try
            If ctrl Is Nothing Then Return
            Dim prop = ctrl.GetType().GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
            If prop IsNot Nothing Then
                prop.SetValue(ctrl, True, Nothing)
            Else
                ctrl.GetType().InvokeMember("DoubleBuffered", Reflection.BindingFlags.SetProperty Or Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public, Nothing, ctrl, New Object() {True})
            End If
        Catch ex As Exception
            ' ignore
        End Try
    End Sub

End Class
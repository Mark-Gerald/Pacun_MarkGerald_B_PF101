Public Class Week8Form
    Inherits Form

    ' ===== Layout =====
    Private leftScrollPanel As Panel
    Private leftSlideStack As FlowLayoutPanel
    Private rightPanel As Panel

    Private editor As RichTextBox
    Private editorColorDialog As New ColorDialog()
    Private editorFontDialog As New FontDialog()

    Public Sub New()
        Me.Text = "Week 8 " & ChrW(8212) & " Working with Controls and Properties " & ChrW(8212) & " Object-Oriented Programming in Visual Basic .NET"
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
            .Text = "Week 8 " & ChrW(8212) & " Working with Controls and Properties",
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
            .Text = "Working with Controls and Properties",
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

        PopulateMiniWordEditor()
    End Sub

    ' ==================================================
    ' PRESENTATION SLIDE LOADING
    ' ==================================================
    Private Sub LoadSlides()
        Dim folderPath As String = IO.Path.Combine(Application.StartupPath, "LessonContent", "Week08_WorkingWithControlsAndProperties", "Slides")

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
    ' WEEK 8 MINI-PROJECT: MINI WORD TEXT EDITOR
    ' ==================================================

    Private Sub PopulateMiniWordEditor()

        ' Main editor card
        Dim cardLayout As New TableLayoutPanel() With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 1,
        .RowCount = 4,
        .BackColor = Color.White,
        .Padding = New Padding(1),
        .Margin = New Padding(0)
    }

        cardLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 48))
        cardLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38))
        cardLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 224))
        cardLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        ' Dark title banner
        Dim titleBar As New Panel() With {
        .Dock = DockStyle.Fill,
        .BackColor = Color.FromArgb(24, 24, 30),
        .Margin = New Padding(0)
    }

        Dim titleLabel As New Label() With {
        .Text = "MINI WORD TEXT EDITOR",
        .Dock = DockStyle.Fill,
        .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
        .ForeColor = Color.White,
        .TextAlign = ContentAlignment.MiddleLeft,
        .Padding = New Padding(16, 0, 0, 0)
    }

        titleBar.Controls.Add(titleLabel)

        ' Short instruction below the title
        Dim instructionLabel As New Label() With {
        .Text = "Type your text, highlight it, then choose a tool.",
        .Dock = DockStyle.Fill,
        .Font = New Font("Segoe UI", 8.5F),
        .ForeColor = Color.FromArgb(100, 100, 110),
        .TextAlign = ContentAlignment.MiddleLeft,
        .Padding = New Padding(4, 0, 0, 0),
        .Margin = New Padding(0)
    }

        ' Centered toolbar grid
        Dim toolbar As New TableLayoutPanel() With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 3,
        .RowCount = 6,
        .Padding = New Padding(4, 6, 4, 6),
        .BackColor = Color.FromArgb(248, 248, 250),
        .Margin = New Padding(0)
    }

        For i As Integer = 1 To 3
            toolbar.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 33.333F))
        Next

        For i As Integer = 1 To 6
            toolbar.RowStyles.Add(
            New RowStyle(SizeType.Percent, 16.666F))
        Next

        ' Row 1: Text styles
        toolbar.Controls.Add(CreateEditorButton(
        "Bold", AddressOf BoldButton_Click), 0, 0)

        toolbar.Controls.Add(CreateEditorButton(
        "Italic", AddressOf ItalicButton_Click), 1, 0)

        toolbar.Controls.Add(CreateEditorButton(
        "Underline", AddressOf UnderlineButton_Click), 2, 0)

        ' Row 2: Letter case and color
        toolbar.Controls.Add(CreateEditorButton(
        "UPPERCASE", AddressOf UppercaseButton_Click), 0, 1)

        toolbar.Controls.Add(CreateEditorButton(
        "lowercase", AddressOf LowercaseButton_Click), 1, 1)

        toolbar.Controls.Add(CreateEditorButton(
        "Text Color", AddressOf TextColorButton_Click), 2, 1)

        ' Row 3: Font and clipboard
        toolbar.Controls.Add(CreateEditorButton(
        "Font", AddressOf FontButton_Click), 0, 2)

        toolbar.Controls.Add(CreateEditorButton(
        "Cut", AddressOf CutButton_Click), 1, 2)

        toolbar.Controls.Add(CreateEditorButton(
        "Copy", AddressOf CopyButton_Click), 2, 2)

        ' Row 4: More editing tools
        toolbar.Controls.Add(CreateEditorButton(
        "Paste", AddressOf PasteButton_Click), 0, 3)

        toolbar.Controls.Add(CreateEditorButton(
        "Open", AddressOf OpenButton_Click), 1, 3)

        toolbar.Controls.Add(CreateEditorButton(
        "Save", AddressOf SaveButton_Click), 2, 3)

        ' Row 5: Paragraph alignment
        toolbar.Controls.Add(CreateEditorButton(
        "Align Left", AddressOf AlignLeftButton_Click), 0, 4)

        toolbar.Controls.Add(CreateEditorButton(
        "Align Center", AddressOf AlignCenterButton_Click), 1, 4)

        toolbar.Controls.Add(CreateEditorButton(
        "Align Right", AddressOf AlignRightButton_Click), 2, 4)

        ' Row 6: Document tools
        toolbar.Controls.Add(CreateEditorButton(
        "Clear", AddressOf ClearButton_Click), 1, 5)

        ' Writing area
        editor = New RichTextBox() With {
        .Dock = DockStyle.Fill,
        .Font = New Font("Segoe UI", 10.0F),
        .BorderStyle = BorderStyle.FixedSingle,
        .AcceptsTab = True,
        .DetectUrls = False,
        .BackColor = Color.White,
        .Text = "Welcome to Mini Word!" & vbCrLf &
                "Type your text here, select it, and try the tools above.",
        .Margin = New Padding(0)
    }

        ' Assemble the card
        cardLayout.Controls.Add(titleBar, 0, 0)
        cardLayout.Controls.Add(instructionLabel, 0, 1)
        cardLayout.Controls.Add(toolbar, 0, 2)
        cardLayout.Controls.Add(editor, 0, 3)

        rightPanel.Controls.Add(cardLayout)

    End Sub

    ' Creates consistently styled toolbar buttons.
    Private Function CreateEditorButton(
    buttonText As String,
    clickHandler As EventHandler
) As Button

        Dim btn As New Button() With {
        .Text = buttonText,
        .Dock = DockStyle.Fill,
        .Margin = New Padding(4),
        .FlatStyle = FlatStyle.Flat,
        .Font = New Font("Segoe UI", 8.5F),
        .BackColor = Color.White,
        .ForeColor = Color.FromArgb(40, 40, 45),
        .Cursor = Cursors.Hand,
        .UseVisualStyleBackColor = False
    }

        btn.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 225)
        btn.FlatAppearance.BorderSize = 1

        AddHandler btn.Click, clickHandler

        Return btn

    End Function

    ' Applies or removes a formatting style on selected text.
    Private Sub ToggleTextStyle(style As FontStyle)

        If editor.SelectionLength = 0 Then
            MessageBox.Show(
            "Please highlight the text you want to format.",
            "Select Text",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
            Return
        End If

        Dim currentFont As Font = editor.SelectionFont

        If currentFont Is Nothing Then
            currentFont = editor.Font
        End If

        Dim newStyle As FontStyle =
        CType(CInt(currentFont.Style) Xor CInt(style), FontStyle)

        editor.SelectionFont = New Font(
        currentFont.FontFamily,
        currentFont.Size,
        newStyle)

    End Sub

    Private Sub BoldButton_Click(sender As Object, e As EventArgs)
        ToggleTextStyle(FontStyle.Bold)
    End Sub

    Private Sub ItalicButton_Click(sender As Object, e As EventArgs)
        ToggleTextStyle(FontStyle.Italic)
    End Sub

    Private Sub UnderlineButton_Click(sender As Object, e As EventArgs)
        ToggleTextStyle(FontStyle.Underline)
    End Sub

    ' Changes only the highlighted text.
    Private Sub TransformSelectedText(makeUppercase As Boolean)

        If editor.SelectionLength = 0 Then
            MessageBox.Show(
            "Please highlight a word or sentence first.",
            "Select Text",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
            Return
        End If

        Dim selectionStart As Integer = editor.SelectionStart
        Dim selectedText As String = editor.SelectedText

        If makeUppercase Then
            selectedText = selectedText.ToUpperInvariant()
        Else
            selectedText = selectedText.ToLowerInvariant()
        End If

        editor.SelectedText = selectedText
        editor.Select(selectionStart, selectedText.Length)
        editor.Focus()

    End Sub

    Private Sub UppercaseButton_Click(sender As Object, e As EventArgs)
        TransformSelectedText(True)
    End Sub

    Private Sub LowercaseButton_Click(sender As Object, e As EventArgs)
        TransformSelectedText(False)
    End Sub

    ' Align the current paragraph to the left.
    Private Sub AlignLeftButton_Click(sender As Object, e As EventArgs)

        editor.SelectionAlignment = HorizontalAlignment.Left
        editor.Focus()

    End Sub

    ' Center the current paragraph.
    Private Sub AlignCenterButton_Click(sender As Object, e As EventArgs)

        editor.SelectionAlignment = HorizontalAlignment.Center
        editor.Focus()

    End Sub

    ' Align the current paragraph to the right.
    Private Sub AlignRightButton_Click(sender As Object, e As EventArgs)

        editor.SelectionAlignment = HorizontalAlignment.Right
        editor.Focus()

    End Sub

    ' Opens the standard Windows color selection dialog.
    Private Sub TextColorButton_Click(sender As Object, e As EventArgs)

        If editor.SelectionLength = 0 Then
            MessageBox.Show(
            "Please highlight the text whose color you want to change.",
            "Select Text",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
            Return
        End If

        If editorColorDialog.ShowDialog(Me) = DialogResult.OK Then
            editor.SelectionColor = editorColorDialog.Color
            editor.Focus()
        End If

    End Sub

    ' Opens the standard Windows font selection dialog.
    Private Sub FontButton_Click(sender As Object, e As EventArgs)

        If editor.SelectionLength = 0 Then
            MessageBox.Show(
            "Please highlight the text whose font you want to change.",
            "Select Text",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
            Return
        End If

        Dim selectedFont As Font = editor.SelectionFont

        If selectedFont Is Nothing Then
            selectedFont = editor.Font
        End If

        editorFontDialog.Font = selectedFont
        editorFontDialog.ShowColor = False

        If editorFontDialog.ShowDialog(Me) = DialogResult.OK Then
            editor.SelectionFont = editorFontDialog.Font
            editor.Focus()
        End If

    End Sub

    ' Basic editing operations.
    Private Sub CutButton_Click(sender As Object, e As EventArgs)
        If editor.SelectionLength > 0 Then editor.Cut()
    End Sub

    Private Sub CopyButton_Click(sender As Object, e As EventArgs)
        If editor.SelectionLength > 0 Then editor.Copy()
    End Sub

    Private Sub PasteButton_Click(sender As Object, e As EventArgs)
        editor.Paste()
    End Sub

    Private Sub ClearButton_Click(sender As Object, e As EventArgs)

        If MessageBox.Show(
        "Clear all text from the document?",
        "Clear Document",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question) = DialogResult.Yes Then

            editor.Clear()
            editor.Focus()

        End If

    End Sub

    ' Opens an RTF document or a plain-text file.
    Private Sub OpenButton_Click(sender As Object, e As EventArgs)

        Using dlg As New OpenFileDialog()

            dlg.Title = "Open Document"
            dlg.Filter =
            "Rich Text Format (*.rtf)|*.rtf|Text Files (*.txt)|*.txt"
            dlg.CheckFileExists = True

            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Try
                If IO.Path.GetExtension(dlg.FileName).Equals(
                ".rtf", StringComparison.OrdinalIgnoreCase) Then

                    editor.LoadFile(
                    dlg.FileName,
                    RichTextBoxStreamType.RichText)
                Else
                    editor.LoadFile(
                    dlg.FileName,
                    RichTextBoxStreamType.PlainText)
                End If

            Catch ex As Exception
                MessageBox.Show(
                "Unable to open the document." & vbCrLf & ex.Message,
                "Open Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            End Try

        End Using

    End Sub

    ' Saves as RTF to preserve formatting, or TXT for plain text.
    Private Sub SaveButton_Click(sender As Object, e As EventArgs)

        Using dlg As New SaveFileDialog()

            dlg.Title = "Save Document"
            dlg.Filter =
            "Rich Text Format (*.rtf)|*.rtf|Text Files (*.txt)|*.txt"
            dlg.DefaultExt = "rtf"
            dlg.AddExtension = True
            dlg.OverwritePrompt = True

            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Try
                If IO.Path.GetExtension(dlg.FileName).Equals(
                ".rtf", StringComparison.OrdinalIgnoreCase) Then

                    editor.SaveFile(
                    dlg.FileName,
                    RichTextBoxStreamType.RichText)
                Else
                    editor.SaveFile(
                    dlg.FileName,
                    RichTextBoxStreamType.PlainText)
                End If

                MessageBox.Show(
                "Document saved successfully.",
                "Save Document",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

            Catch ex As Exception
                MessageBox.Show(
                "Unable to save the document." & vbCrLf & ex.Message,
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            End Try

        End Using

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

        Dim progressLbl As New Label() With {.Text = "Week 8 of 19", .Font = New Font("Segoe UI", 9.0F), .ForeColor = Color.Gray, .AutoSize = True}
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
        Dim week7Window As New Week7Form()
        week7Window.Show()
        Me.Close()
    End Sub

    Private Sub ContinueButton_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Week 9 will be implemented in a future update.", "Week 9", MessageBoxButtons.OK, MessageBoxIcon.Information)
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
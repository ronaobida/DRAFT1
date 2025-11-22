Imports System.Configuration
Imports MySqlConnector
Imports BCrypt.Net




Public Class SubscriberSignup
    Private ReadOnly CONNECTION_STRING As String =
        ConfigurationManager.ConnectionStrings("SparxDb").ConnectionString
    Private Sub Label1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub btnSignup_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub lblPassword_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub logo_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub lblUserLevel_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub lblEmail_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub pnlLoginCard_Paint(sender As Object, e As PaintEventArgs)

    End Sub

    Private Sub chkRemember_CheckedChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub pnlLoginCard_Paint_1(sender As Object, e As PaintEventArgs)

    End Sub

    Public Event SignUpCompleted(email As String)
    Public Event SignupRequested(email As String, password As String)

    Private Sub SubscriberSignup_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        EnableScroll()
        ' Wire events dynamically to avoid Handles/WithEvents requirement
        Try
            ' Bind known names if present and style them to avoid gray hover background
            Dim ctrl = Me.Controls.Find("LinkBtnLogin", True).FirstOrDefault()
            If ctrl IsNot Nothing Then
                Dim l As LinkLabel = CType(ctrl, LinkLabel)
                AddHandler l.LinkClicked, AddressOf ButtonRounded5_Click
                StyleLoginLink(l)
            End If
            Dim ctrl2 = Me.Controls.Find("LinkBtnSignup", True).FirstOrDefault()
            If ctrl2 IsNot Nothing Then
                Dim l2 As LinkLabel = CType(ctrl2, LinkLabel)
                AddHandler l2.LinkClicked, AddressOf ButtonRounded5_Click
                StyleLoginLink(l2)
            End If
            ' Fallback: bind any LinkLabel whose text contains "Login"
            For Each lnk As LinkLabel In GetAllLinkLabels(Me)
                If lnk.Text IsNot Nothing AndAlso lnk.Text.Trim().ToLower().Contains("login") Then
                    AddHandler lnk.LinkClicked, AddressOf ButtonRounded5_Click
                    StyleLoginLink(lnk)
                End If
            Next

            If PhoneNumber IsNot Nothing Then
                AddHandler PhoneNumber.KeyPress, AddressOf PhoneNumber_KeyPress
                AddHandler PhoneNumber.TextChanged, AddressOf PhoneNumber_TextChanged
            End If
        Catch
        End Try
    End Sub

    Private Sub ButtonRounded3_Click(sender As Object, e As EventArgs) Handles ButtonRounded3.Click
        ' Collect inputs from the form
        Dim firstName As String = If(txtEmail IsNot Nothing, txtEmail.Text.Trim(), String.Empty)  ' Assuming txtEmail is First Name
        Dim lastName As String = If(TextBox1 IsNot Nothing, TextBox1.Text.Trim(), String.Empty)   ' Last Name
        Dim email As String = If(txtPassword IsNot Nothing, txtPassword.Text.Trim(), String.Empty)  ' Email Address
        Dim phone As String = If(PhoneNumber IsNot Nothing, PhoneNumber.Text.Trim(), String.Empty)  ' Phone Number
        Dim password As String = If(TextBox3 IsNot Nothing, TextBox3.Text.Trim(), String.Empty)  ' Password
        Dim rePassword As String = If(TextBox4 IsNot Nothing, TextBox4.Text.Trim(), String.Empty) ' Re-enter Password

        ' Basic validation
        If String.IsNullOrEmpty(firstName) OrElse String.IsNullOrEmpty(lastName) OrElse String.IsNullOrEmpty(email) OrElse String.IsNullOrEmpty(phone) OrElse String.IsNullOrEmpty(password) Then
            MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If password <> rePassword Then
            MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Email validation (simple regex)
        Dim emailRegex As New System.Text.RegularExpressions.Regex("^[^@\s]+@[^@\s]+\.[^@\s]+$")
        If Not emailRegex.IsMatch(email) Then
            MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Phone validation (e.g., 11 digits)
        If phone.Length <> 11 OrElse Not IsNumeric(phone) Then
            MessageBox.Show("Phone number must be 11 digits.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Hash the password
        Dim hashedPassword As String = BCrypt.Net.BCrypt.HashPassword(password)

        ' Connect to MySQL and insert data
        Try
            Using conn As New MySqlConnection(CONNECTION_STRING)
                conn.Open()

                ' Check if email already exists
                Dim checkQuery As String = "SELECT COUNT(*) FROM customer WHERE email_address = @email"
                Using checkCmd As New MySqlCommand(checkQuery, conn)
                    checkCmd.Parameters.AddWithValue("@email", email)
                    Dim count As Integer = Convert.ToInt32(checkCmd.ExecuteScalar())
                    If count > 0 Then
                        MessageBox.Show("An account with this email already exists.", "Signup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If
                End Using

                ' Insert into customer table
                Dim insertQuery As String = "
                    INSERT INTO customer
                    (first_name, last_name, contact_number, email_address, password_hash, billing_address, installation_address, plan_type, date_installed, monthly_rate, account_status)
                    VALUES
                    (@firstName, @lastName, @phone, @email, @hashedPassword, @billing, @installation, @plan, @dateInstalled, @rate, @status)"

                Using cmd As New MySqlCommand(insertQuery, conn)
                    cmd.Parameters.AddWithValue("@firstName", firstName)
                    cmd.Parameters.AddWithValue("@lastName", lastName)
                    cmd.Parameters.AddWithValue("@phone", phone)
                    cmd.Parameters.AddWithValue("@email", email)
                    cmd.Parameters.AddWithValue("@hashedPassword", hashedPassword)
                    cmd.Parameters.AddWithValue("@billing", "Default Billing Address")
                    cmd.Parameters.AddWithValue("@installation", "Default Installation Address")
                    cmd.Parameters.AddWithValue("@plan", "Basic")
                    cmd.Parameters.AddWithValue("@dateInstalled", DateTime.Now.Date)
                    cmd.Parameters.AddWithValue("@rate", 500.0)
                    cmd.Parameters.AddWithValue("@status", "Active")

                    Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
                    If rowsAffected > 0 Then
                        MessageBox.Show("Signup successful! You can now log in.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        ' Clear fields
                        ClearFields()
                        ' Optionally, switch back to login view
                        Dim parentForm = TryCast(Me.FindForm(), sparxLogin)
                        If parentForm IsNot Nothing Then
                            ' Use reflection to call the private method
                            Dim methodInfo = parentForm.GetType().GetMethod("LinkBtnLogin_LinkClicked", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                            If methodInfo IsNot Nothing Then
                                methodInfo.Invoke(parentForm, New Object() {Nothing, Nothing})
                            End If
                        End If
                    Else
                        MessageBox.Show("Signup failed. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Database error: " & ex.Message, "Signup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Helper method to clear fields
    Private Sub ClearFields()
        If txtEmail IsNot Nothing Then txtEmail.Text = ""
        If TextBox1 IsNot Nothing Then TextBox1.Text = ""
        If txtPassword IsNot Nothing Then txtPassword.Text = ""
        If PhoneNumber IsNot Nothing Then PhoneNumber.Text = ""
        If TextBox3 IsNot Nothing Then TextBox3.Text = ""
        If TextBox4 IsNot Nothing Then TextBox4.Text = ""
    End Sub

    Private Sub StyleLoginLink(lnk As LinkLabel)
        If lnk Is Nothing Then Return
        lnk.Enabled = True
        lnk.BringToFront()
        lnk.BackColor = Color.Transparent
        lnk.LinkBehavior = LinkBehavior.NeverUnderline
        lnk.LinkColor = Color.RoyalBlue
        lnk.ActiveLinkColor = lnk.LinkColor
        lnk.VisitedLinkColor = lnk.LinkColor
        lnk.TabStop = False
        AddHandler lnk.MouseEnter, Sub(sender As Object, e As EventArgs) lnk.BackColor = Color.Transparent
        AddHandler lnk.MouseLeave, Sub(sender As Object, e As EventArgs) lnk.BackColor = Color.Transparent
        AddHandler lnk.MouseDown, Sub(sender As Object, e As MouseEventArgs) lnk.BackColor = Color.Transparent
        AddHandler lnk.MouseUp, Sub(sender As Object, e As MouseEventArgs) lnk.BackColor = Color.Transparent
    End Sub

    Private Function GetAllLinkLabels(root As Control) As IEnumerable(Of LinkLabel)
        Dim list As New List(Of LinkLabel)
        Dim stack As New Stack(Of Control)
        stack.Push(root)
        While stack.Count > 0
            Dim c = stack.Pop()
            If TypeOf c Is LinkLabel Then list.Add(DirectCast(c, LinkLabel))
            For Each child As Control In c.Controls
                stack.Push(child)
            Next
        End While
        Return list
    End Function

    Private Sub SubscriberSignup_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        EnableScroll()
    End Sub

    Private Sub EnableScroll()
        Try
            Dim panel = pnlLoginCard
            If panel Is Nothing Then Return
            panel.AutoScroll = True

            Dim maxBottom As Integer = 0
            For Each c As Control In panel.Controls
                If c.Visible Then
                    maxBottom = Math.Max(maxBottom, c.Bottom)
                End If
            Next

            Dim neededHeight As Integer = Math.Max(maxBottom + 20, panel.ClientSize.Height + 1)
            panel.AutoScrollMinSize = New Size(0, neededHeight)
        Catch
        End Try
    End Sub

    ' Numeric-only input for phone number with max length
    Private Sub PhoneNumber_KeyPress(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Private Sub PhoneNumber_TextChanged(sender As Object, e As EventArgs)
        Dim maxLen As Integer = 11 ' adjust for your locale
        If PhoneNumber.TextLength > maxLen Then
            PhoneNumber.Text = PhoneNumber.Text.Substring(0, maxLen)
            PhoneNumber.SelectionStart = PhoneNumber.TextLength
        End If
    End Sub

    ' When a "Login"-type link is clicked inside the signup view, switch parent form to Subscriber login
    Private Sub ButtonRounded5_Click(sender As Object, e As EventArgs) Handles ButtonRounded5.Click
        Dim parentForm = TryCast(Me.FindForm(), sparxLogin)
        If parentForm Is Nothing Then Return

        Me.Visible = False
        Dim methodInfo = parentForm.GetType().GetMethod("UserRole_Click", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
        If methodInfo IsNot Nothing Then
            methodInfo.Invoke(parentForm, New Object() {parentForm, EventArgs.Empty})
        End If
    End Sub
End Class

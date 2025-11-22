Imports System.Configuration
Imports System.Data
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports BCrypt.Net
Imports MySqlConnector

Public Class sparxLogin
    Private Const ROLE_SUPER_ADMIN As String = "Super Admin"
    Private Const ROLE_ADMIN As String = "Admin"
    Private Const ROLE_SUBSCRIBER As String = "Subscriber"
    Private Const ROLE_CUSTOMER_SERVICE As String = "Customer Service"

    Private Shared ReadOnly http As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}
    Private Const API_URL As String = "http://127.0.0.1/sparx-api/login.php"
    Private ForgotView As ForgotPassword
    Private forgotVerificationView As ForgotVerification
    Private subscriberSignUpControl As SubscriberSignup

    Private _connectionString As String = Nothing
    Private ReadOnly Property CONNECTION_STRING As String
        Get
            If _connectionString Is Nothing AndAlso Not DesignMode Then
                Try
                    _connectionString = ConfigurationManager.ConnectionStrings("SparxDb").ConnectionString
                Catch
                    _connectionString = String.Empty
                End Try
            End If
            Return If(_connectionString IsNot Nothing, _connectionString, String.Empty)
        End Get
    End Property

    Private Sub sparxLogin_Load(sender As Object, e As EventArgs) Handles MyBase.Load, MyBase.Resize
        If DesignMode Then Return
        Dim ok = TestConnection()

        ' Initialize picShowHide image
        If picShowHide IsNot Nothing AndAlso picShowHide.Image Is Nothing Then
            picShowHide.Image = My.Resources.eye_slashed
        End If

        ' Initialize logo image
        If logo IsNot Nothing AndAlso logo.Image Is Nothing Then
            Try
                logo.Image = My.Resources.Resources.SparxLogo2
            Catch
                ' Resource not found, skip
            End Try
        End If

        ' Initialize background image
        If SplitContainer1 IsNot Nothing AndAlso SplitContainer1.Panel1.BackgroundImage Is Nothing Then
            Try
                SplitContainer1.Panel1.BackgroundImage = My.Resources.Resources.SparxBackground
            Catch
                ' Resource not found, skip
            End Try
        End If

        If subscriberSignUpControl Is Nothing Then
            subscriberSignUpControl = New SubscriberSignup()
            subscriberSignUpControl.Dock = DockStyle.Fill
            pnlLoginCard.Controls.Add(subscriberSignUpControl)
            subscriberSignUpControl.Visible = False
        End If

        ' Default to login view – keep signup hidden until user explicitly requests it.
        If subscriberSignUpControl IsNot Nothing Then
            subscriberSignUpControl.Visible = False
        End If
    End Sub

    Private Function TestConnection() As Boolean
        Try
            Using conn As New MySqlConnection(CONNECTION_STRING)
                conn.Open()
                Return conn.State = ConnectionState.Open
            End Using
        Catch ex As Exception
            MessageBox.Show("DB connection failed: " & ex.Message)
            Return False
        End Try
    End Function

    Private Sub picShowHide_Click(sender As Object, e As EventArgs) Handles picShowHide.Click
        If txtPassword.PasswordChar = "●" Then
            txtPassword.PasswordChar = Chr(0)
            picShowHide.Image = My.Resources.eye_open
        Else
            txtPassword.PasswordChar = "●"
            picShowHide.Image = My.Resources.eye_slashed
        End If
    End Sub

    Private Sub pnlPassword_Paint(sender As Object, e As PaintEventArgs) Handles pnlPassword.Paint
    End Sub


    Private Sub UserRole_Click(sender As Object, e As EventArgs)

        Dim clickedLabel = CType(sender, Label)
        clickedLabel.Font = New Font(clickedLabel.Font, FontStyle.Bold)
        If subscriberSignUpControl IsNot Nothing Then subscriberSignUpControl.Visible = False
        ' after you remove overlays and before showing login inputsIf subscriberSignUpControl IsNot Nothing Then subscriberSignUpControl.Visible = False
        lblWelcome.Text = "Welcome Back!"
        btnSignup.Text = "Sign In"
        lblUserLevel.Text = clickedLabel.Text & " Login"

        ' If we were in any forgot password flow, remove those overlays and restore login inputs
        Dim overlays As New List(Of Control)
        For Each c As Control In pnlLoginCard.Controls
            If c.GetType.Name = "ForgotPassword" OrElse c.GetType.Name = "ForgotVerification" OrElse c.GetType.Name = "NewPassword" OrElse c.GetType.Name = "forgotPasswordView" Then
                overlays.Add(c)
            End If
        Next
        For Each c In overlays
            pnlLoginCard.Controls.Remove(c)
        Next

        If ForgotView IsNot Nothing Then
            ForgotView.Visible = False
        End If

        txtEmail.Visible = True
        txtPassword.Visible = True
        pnlEmail.Visible = True
        pnlPassword.Visible = True
        chkRemember.Visible = True
        lnkForgot.Visible = True


    End Sub

    Private Sub LinkBtnSignup_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkBtnSignup.LinkClicked
        lblWelcome.Text = "Hello, Welcome!"
        lblUserLevel.Text = "Subscriber Sign Up"
        btnSignup.Text = "Sign Up"

        ' Hide login inputs
        lblEmail.Visible = False
        lblPassword.Visible = False
        txtEmail.Visible = False
        txtPassword.Visible = False
        pnlEmail.Visible = False
        pnlPassword.Visible = False
        chkRemember.Visible = False
        lnkForgot.Visible = False



        ' Show the sign-up view
        subscriberSignUpControl.Visible = True
        subscriberSignUpControl.BringToFront()
    End Sub


    Private Sub lblUserLevel_Click(sender As Object, e As EventArgs) Handles lblUserLevel.Click
    End Sub

    Private Sub lnkForgot_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkForgot.LinkClicked
        If ForgotView Is Nothing Then
            ForgotView = New ForgotPassword()
            AddHandler ForgotView.SendCodeRequested, AddressOf OnSendCodeRequested
            ForgotView.Dock = DockStyle.Fill
        End If
        If Not pnlLoginCard.Controls.Contains(ForgotView) Then
            pnlLoginCard.Controls.Add(ForgotView)
        End If
        ForgotView.BringToFront()
    End Sub

    Private Sub OnSendCodeRequested()
        If forgotVerificationView Is Nothing Then
            forgotVerificationView = New ForgotVerification()
            forgotVerificationView.Dock = DockStyle.Fill
        End If
        If Not pnlLoginCard.Controls.Contains(forgotVerificationView) Then
            pnlLoginCard.Controls.Add(forgotVerificationView)
        End If
        forgotVerificationView.BringToFront()
    End Sub
    Private Function IsValidEmail(ByVal email As String) As Boolean
        Dim pattern As String = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
        Return Regex.IsMatch(email, pattern)
    End Function

    Private Async Sub btnSignup_Click(sender As Object, e As EventArgs) Handles btnSignup.Click
        Dim email As String = txtEmail.Text.Trim()
        Dim password As String = txtPassword.Text

        If String.IsNullOrEmpty(email) AndAlso String.IsNullOrEmpty(password) Then
            MessageBox.Show("Please enter both an email and a password to sign up.", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        ElseIf String.IsNullOrEmpty(email) Then
            MessageBox.Show("Please enter your email.", "Missing Email", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        ElseIf String.IsNullOrEmpty(password) Then
            MessageBox.Show("Please enter your password.", "Missing Password", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        If Not IsValidEmail(email) Then
            MessageBox.Show("Please enter a valid email address.", "Invalid Email", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        If btnSignup.Text = "Sign Up" Then
            ' Existing Sign Up logic remains unchanged
            Dim form As New Dictionary(Of String, String) From {
            {"action", "signup"},
            {"email", email},
            {"username", email},
            {"password", password}
        }
            Dim content = New FormUrlEncodedContent(form)

            Try
                Dim resp = Await http.PostAsync(API_URL, content)
                Dim responseString = Await resp.Content.ReadAsStringAsync()

                If Not resp.IsSuccessStatusCode Then
                    MessageBox.Show("Server returned " & CInt(resp.StatusCode) & ": " & resp.ReasonPhrase, "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Exit Sub
                End If

                If responseString.Contains("""status"":""success""") OrElse responseString.Contains("""success"":true") Then
                    MessageBox.Show("Sign up successful! Please log in.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    ' Return to login view
                    If subscriberSignUpControl IsNot Nothing Then
                        subscriberSignUpControl.Visible = False
                    End If
                    lblWelcome.Text = "Welcome Back!"
                    btnSignup.Text = "Sign In"
                    lblEmail.Visible = True
                    lblPassword.Visible = True
                    txtEmail.Visible = True
                    txtPassword.Visible = True
                    pnlEmail.Visible = True
                    pnlPassword.Visible = True
                    chkRemember.Visible = True
                    lnkForgot.Visible = True
                    txtEmail.Text = email ' Keep email for convenience
                    txtPassword.Text = ""
                Else
                    Dim messageMatch As Match = Regex.Match(responseString, """message"":""([^""]+)""")
                    Dim errorMessage As String = If(messageMatch.Success, messageMatch.Groups(1).Value, "Sign up failed.")
                    MessageBox.Show(errorMessage, "Sign Up Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Catch ex As Exception
                MessageBox.Show("Error connecting to server: " & ex.Message & " (Check XAMPP and URL: " & API_URL & ")", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try

            ' --- PATH 2: Handle Sign In (Unified Login) ---
        Else ' btnSignup.Text = "Sign In"

            ' --- 2a: Try API Login (for Staff Roles) ---
            Dim formLogin As New Dictionary(Of String, String) From {
            {"action", "login"},
            {"email", email},
            {"username", email},
            {"password", password}}

            Dim contentLogin = New FormUrlEncodedContent(formLogin)

            Try
                Dim resp = Await http.PostAsync(API_URL, contentLogin)
                Dim responseString = Await resp.Content.ReadAsStringAsync()

                If resp.IsSuccessStatusCode AndAlso (responseString.Contains("""status"":""success""") OrElse responseString.Contains("""success"":true")) Then

                    ' API Login Success! Determine role and navigate.
                    Dim roleMatch As Match = Regex.Match(responseString, """user_role"":""([^""]+)""")
                    Dim userRole As String = If(roleMatch.Success, roleMatch.Groups(1).Value, "User")

                    MessageBox.Show($"Staff Login successful! Role: {userRole}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                    Select Case userRole
                        Case ROLE_SUPER_ADMIN
                            Dim dash As New dashboardSuperAdmin()
                            Me.Hide()
                            dash.Show()
                            AddHandler dash.FormClosed, Sub() Me.Close()

                        Case ROLE_ADMIN
                            Dim adminPortal As New Tabs()
                            Me.Hide()
                            adminPortal.Show()
                            AddHandler adminPortal.FormClosed, Sub() Me.Close()

                        Case Else
                            ' Handle other staff/unknown roles here if necessary
                            MessageBox.Show($"Unknown Staff Role ({userRole}) logged in.", "Login Success", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End Select
                    Exit Sub ' IMPORTANT: Exit after successful API login and navigation.
                End If

                ' If API failed, it falls through to the next Try block (Local DB check).

            Catch ex As Exception
                ' Log network error but continue to try local DB check
                Console.WriteLine("API Network Error or Server Unreachable. Trying local DB...")
            End Try

            ' --- 2b: Try Direct MySQL Login (for Subscriber Role) ---
            Try
                Using conn As New MySqlConnection(CONNECTION_STRING)
                    conn.Open()

                    Dim passwordColumn As String = ResolvePasswordColumn(conn, "customer")
                    If String.IsNullOrEmpty(passwordColumn) Then
                        MessageBox.Show("System Error: Missing password column in customer table.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Exit Sub
                    End If

                    Dim query As String = $"SELECT customer_id, first_name, last_name, {passwordColumn}, account_status FROM customer WHERE email_address = @email"
                    Using cmd As New MySqlCommand(query, conn)
                        cmd.Parameters.AddWithValue("@email", email)
                        Using reader As MySqlDataReader = cmd.ExecuteReader()
                            If reader.Read() Then
                                Dim storedPassword As String = reader(passwordColumn).ToString()
                                Dim customerId As Integer = Convert.ToInt32(reader("customer_id"))
                                Dim firstName As String = reader("first_name").ToString()
                                Dim lastName As String = reader("last_name").ToString()
                                Dim status As String = reader("account_status").ToString()

                                If VerifyPasswordValue(password, storedPassword) Then ' Verify password hash
                                    If status = "Active" Then
                                        MessageBox.Show("Subscriber Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                        Dim subView As New SidePanel()
                                        subView.CurrentCustomerId = customerId
                                        subView.CurrentFirstName = firstName
                                        subView.CurrentLastName = lastName
                                        subView.CurrentEmail = email
                                        Me.Hide()
                                        subView.Show()
                                        AddHandler subView.FormClosed, AddressOf OnSubViewClosing
                                        Exit Sub ' IMPORTANT: Exit after successful DB login and navigation.
                                    Else
                                        MessageBox.Show("Account is not active.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                        Exit Sub
                                    End If
                                End If
                            End If
                        End Using
                    End Using
                End Using

            Catch ex As Exception
                MessageBox.Show("Database error during subscriber login: " & ex.Message, "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Exit Sub
            End Try

            ' --- Final Failure Message ---
            ' If execution reaches here, neither API nor DB login succeeded.
            MessageBox.Show("Invalid email or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)

        End If ' End of Sign Up / Sign In block
    End Sub


    Private Sub pnlLoginCard_Paint(sender As Object, e As PaintEventArgs) Handles pnlLoginCard.Paint

    End Sub

    Private Sub logo_Click(sender As Object, e As EventArgs) Handles logo.Click

    End Sub

    Private Sub OnSubViewClosing(sender As Object, e As FormClosedEventArgs)
        Me.Close()
    End Sub

    Private Sub SplitContainer1_Panel2_Paint(sender As Object, e As PaintEventArgs) Handles SplitContainer1.Panel2.Paint

    End Sub

    Private Sub SplitContainer1_Panel1_Paint(sender As Object, e As PaintEventArgs) Handles SplitContainer1.Panel1.Paint

    End Sub

    Private Function ResolvePasswordColumn(conn As MySqlConnection, tableName As String) As String
        Try
            Dim query As String = "SHOW COLUMNS FROM " & tableName & " WHERE Field LIKE '%password%'"
            Using cmd As New MySqlCommand(query, conn)
                Using reader As MySqlDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Return reader("Field").ToString()
                    End If
                End Using
            End Using
        Catch ex As Exception
            ' Fallback to common column names
            Return "password"
        End Try
        Return "password"
    End Function

    Private Function VerifyPasswordValue(inputPassword As String, storedPassword As String) As Boolean
        Try
            ' Try BCrypt verification first
            If storedPassword.StartsWith("$2") Then
                Return BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword)
            End If
            ' Fallback to plain text comparison (not recommended for production)
            Return inputPassword = storedPassword
        Catch
            ' If BCrypt fails, try plain text
            Return inputPassword = storedPassword
        End Try
    End Function

End Class

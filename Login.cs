using Gusheshe.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gusheshe
{
    public partial class Login : Form
    {
        public bool LoginSuccessful { get; private set; } = false;
        public int LoggedInUserID { get; private set; }
        public string LoggedInUsername { get; private set; }


        public Login()
        {
            InitializeComponent();
            this.AcceptButton = btnLogin; // Allow Enter key to trigger login
            this.CancelButton = btnCancel; // Allow Escape key to cancel

            // Initialize database to ensure Users table exists
            try
            {
                SQLiteHelper.InitializeDatabase();
                EnsureDefaultUser();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database initialization error: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Login_Load(object sender, EventArgs e)
        {
            InitializeComponent();
            this.AcceptButton = btnLogin; // Allow Enter key to trigger login
            this.CancelButton = btnCancel; // Allow Escape key to cancel
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            PerformLogin();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void PerformLogin()
        {           
            lblError.Visible = false;
           
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                ShowError("Please enter a username.");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                ShowError("Please enter a password.");
                txtPassword.Focus();
                return;
            }

            // Disable login button to prevent multiple clicks
            btnLogin.Enabled = false;
            btnLogin.Text = "Logging in...";

            try
            {
                if (ValidateUser(txtUsername.Text.Trim(), txtPassword.Text))
                {
                    LoginSuccessful = true;

                    // Update last login time
                    UpdateLastLoginTime(LoggedInUserID);
                                       
                    this.DialogResult = DialogResult.OK;
                    this.Hide();
                    var dashbord = new Dashboard();
                    dashbord.FormClosed += (s, args) => this.Show(); // Re-show dashboard when UniformSalesForm is closed
                    dashbord.Show();

                }
                else
                {
                    ShowError("Invalid username or password.");
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login error: {ex.Message}");
            }
            finally
            {
                // Re-enable login button
                btnLogin.Enabled = true;
                btnLogin.Text = "Login";
            }
        }
        private bool ValidateUser(string username, string password)
        {
            string hashedPassword = HashPassword(password);

            try
            {
                using (SQLiteConnection connection = SQLiteHelper.GetConnection())
                {
                    string query = @"SELECT UserID, Username FROM Users 
                                   WHERE Username = @Username AND PasswordHash = @PasswordHash AND IsActive = 1";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                LoggedInUserID = Convert.ToInt32(reader["UserID"]);
                                LoggedInUsername = reader["Username"].ToString();
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database error during login: {ex.Message}");
            }

            return false;
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void UpdateLastLoginTime(int userID)
        {
            try
            {
                using (SQLiteConnection connection = SQLiteHelper.GetConnection())
                {
                    string query = "UPDATE Users SET LastLoginDate = datetime('now', 'localtime') WHERE UserID = @UserID";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't prevent login
                Console.WriteLine($"Error updating last login time: {ex.Message}");
            }
        }
        private void EnsureDefaultUser()
        {
            try
            {
                using (SQLiteConnection connection = SQLiteHelper.GetConnection())
                {
                    // Check if any users exist
                    string countQuery = "SELECT COUNT(*) FROM Users";
                    using (SQLiteCommand countCmd = new SQLiteCommand(countQuery, connection))
                    {
                        int userCount = Convert.ToInt32(countCmd.ExecuteScalar());

                        if (userCount == 0)
                        {
                            // Create default admin user
                            string hashedPassword = HashPassword("password123");
                            string insertQuery = @"INSERT INTO Users (Username, PasswordHash, Email, FirstName, LastName) 
                                                 VALUES (@Username, @PasswordHash, @Email, @FirstName, @LastName)";

                            using (SQLiteCommand insertCmd = new SQLiteCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@Username", "admin");
                                insertCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                                insertCmd.Parameters.AddWithValue("@Email", "admin@school.com");
                                insertCmd.Parameters.AddWithValue("@FirstName", "Admin");
                                insertCmd.Parameters.AddWithValue("@LastName", "User");
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating default user: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
       
        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            

            // Focus on the appropriate field
            if (string.IsNullOrEmpty(txtUsername.Text))
            {
                txtUsername.Focus();
            }
            else
            {
                txtPassword.Focus();
            }
        }
    }
}

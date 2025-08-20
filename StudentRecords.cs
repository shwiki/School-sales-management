using Gusheshe.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;

namespace Gusheshe
{
   
    public partial class StudentRecords : Form
    {
        private const decimal REGISTRATION_FEE = 20.00m;
        private string receiptContent = "";

        public StudentRecords()
        {
            InitializeComponent();
        }

        private void StudentRecords_Load(object sender, EventArgs e)
        {
            LoadStudentRecords();
        }
        private void LoadStudentRecords()
        {
            dgvRecords.Rows.Clear();
            using (var conn = SQLiteHelper.GetConnection())
            using (var cmd = new SQLiteCommand("SELECT Firstname, LastName, Class, DOB, Gender, GuardianName, Contact, Address, ExpectedFees FROM Students", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    try
                    {
                        dgvRecords.Rows.Add(
                            rdr["FirstName"]?.ToString() ?? "",
                            rdr["LastName"]?.ToString() ?? "",
                            rdr["Class"]?.ToString() ?? "",

                            // Safe DateTime handling
                            rdr["DOB"] != DBNull.Value && DateTime.TryParse(rdr["DOB"].ToString(), out DateTime dob)
                                ? dob.ToString("yyyy-MM-dd")  // Format as string for display
                                : "",

                            rdr["Gender"]?.ToString() ?? "",
                            rdr["GuardianName"]?.ToString() ?? "",
                            rdr["Contact"]?.ToString() ?? "",
                            rdr["Address"]?.ToString() ?? "",
                            rdr["ExpectedFees"] != DBNull.Value ? Convert.ToDouble(rdr["ExpectedFees"]) : 0.0
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log the specific error for debugging
                        Console.WriteLine($"Error adding row: {ex.Message}");
                    }
                }
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLastName.Text) && string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("Enter a name for the student. Complete all the records properly");
                return;
            }

            try
            {
                // Check if student already exists to prevent duplicates
                using (var conn = SQLiteHelper.GetConnection())
                using (var checkCmd = new SQLiteCommand(conn))
                {
                    checkCmd.CommandText = "SELECT COUNT(*) FROM Students WHERE FirstName = @fname AND LastName = @lname AND Class = @class";
                    checkCmd.Parameters.AddWithValue("@fname", txtFirstName.Text.Trim());
                    checkCmd.Parameters.AddWithValue("@lname", txtLastName.Text.Trim());
                    checkCmd.Parameters.AddWithValue("@class", txtClass.Text.Trim());

                    int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (existingCount > 0)
                    {
                        MessageBox.Show($"{txtFirstName.Text} {txtLastName.Text} in {txtClass.Text} already exists in the system.",
                            "Duplicate Student", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Insert new student - FIXED: Proper column to parameter mapping
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        cmd.CommandText = @"
                INSERT INTO Students (FirstName, LastName, Class, DOB, Gender, GuardianName, Contact, Address, ExpectedFees)
                VALUES (@firstname, @lastname, @class, @dob, @gender, @guardian, @contact, @address, @expected)";

                        // IMPORTANT: Make sure parameter names match what's expected
                        cmd.Parameters.AddWithValue("@firstname", txtFirstName.Text.Trim());
                        cmd.Parameters.AddWithValue("@lastname", txtLastName.Text.Trim());
                        cmd.Parameters.AddWithValue("@class", txtClass.Text.Trim());
                        cmd.Parameters.AddWithValue("@dob", dtpDOB.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@gender", cbGender.SelectedItem?.ToString() ?? string.Empty);
                        cmd.Parameters.AddWithValue("@guardian", txtGuardian.Text.Trim());
                        cmd.Parameters.AddWithValue("@contact", txtContact.Text.Trim());
                        cmd.Parameters.AddWithValue("@address", txtAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@expected", (double)nudFees.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"{txtFirstName.Text} {txtLastName.Text} has been added to the system.");
                LoadStudentRecords(); // Refresh the grid
                clear(); // Clear the form
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving student: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void clear()
        {
            txtFirstName.Clear();
            txtLastName.Clear();
            txtClass.Clear();
            dtpDOB.Value = DateTime.Now;
            cbGender.SelectedIndex = -1;
            txtGuardian.Clear();
            txtContact.Clear();
            txtAddress.Clear();
            nudFees.Value = 0;

            // Clear receipt
            receiptContent = "";
            if (this.Controls.Find("richTextBox1", true).FirstOrDefault() is RichTextBox rtb)
            {
                rtb.Clear();
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            // Validate that student details are filled
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Please fill in student details before registration.", "Missing Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirm registration payment
            var result = MessageBox.Show(
                $"Registration fee: ${REGISTRATION_FEE:F2}\n\n" +
                $"Student: {txtFirstName.Text} {txtLastName.Text}\n" +
                $"Class: {txtClass.Text}\n\n" +
                "Proceed with registration payment?",
                "Confirm Registration",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // First save the student if not already saved
                    SaveStudentForRegistration();

                    // Process registration payment
                    ProcessRegistrationPayment();

                    // Generate and display receipt
                    GenerateReceipt();

                    MessageBox.Show("Registration completed successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Registration failed: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void SaveStudentForRegistration()
        {
            using (var conn = SQLiteHelper.GetConnection())
            using (var cmd = new SQLiteCommand(conn))
            {
                // Check if student already exists
                cmd.CommandText = "SELECT COUNT(*) FROM Students WHERE FirstName = @fname AND LastName = @lname";
                cmd.Parameters.AddWithValue("@fname", txtFirstName.Text.Trim());
                cmd.Parameters.AddWithValue("@lname", txtLastName.Text.Trim());

                int existingCount = Convert.ToInt32(cmd.ExecuteScalar());

                if (existingCount == 0)
                {
                    // Student doesn't exist, insert them
                    cmd.CommandText = @"
                    INSERT INTO Students (Firstname, LastName, Class, DOB, Gender, GuardianName, Contact, Address, ExpectedFees)
                    VALUES (@fname, @lname, @class, @dob, @gender, @guardian, @contact, @address, @expected)";

                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@fname", txtFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@lname", txtLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@class", txtClass.Text.Trim());
                    cmd.Parameters.AddWithValue("@dob", dtpDOB.Value.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@gender", cbGender.SelectedItem?.ToString() ?? string.Empty);
                    cmd.Parameters.AddWithValue("@guardian", txtGuardian.Text.Trim());
                    cmd.Parameters.AddWithValue("@contact", txtContact.Text.Trim());
                    cmd.Parameters.AddWithValue("@address", txtAddress.Text.Trim());
                    cmd.Parameters.AddWithValue("@expected", (double)nudFees.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void ProcessRegistrationPayment()
        {
            using (var conn = SQLiteHelper.GetConnection())
            using (var cmd = new SQLiteCommand(conn))
            {
                // Get the student ID
                cmd.CommandText = "SELECT StudentId FROM Students WHERE FirstName = @fname AND LastName = @lname ORDER BY StudentId DESC LIMIT 1";
                cmd.Parameters.AddWithValue("@fname", txtFirstName.Text.Trim());
                cmd.Parameters.AddWithValue("@lname", txtLastName.Text.Trim());

                var studentId = cmd.ExecuteScalar();

                if (studentId != null)
                {
                    // Record the registration payment (you'll need to create the Payments table)
                    cmd.CommandText = @"
                    INSERT INTO Payments (StudentId, Amount, PaymentType, PaymentDate, Description)
                    VALUES (@studentId, @amount, @type, @date, @description)";

                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    cmd.Parameters.AddWithValue("@amount", (double)REGISTRATION_FEE);
                    cmd.Parameters.AddWithValue("@type", "Registration");
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@description", "Student Registration Fee");
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void GenerateReceipt()
        {
            StringBuilder receipt = new StringBuilder();
            
            receipt.AppendLine("═══════════════════════════════════════");
            receipt.AppendLine("        GOOD SHEPHERD  COLLEGE");
            receipt.AppendLine("         REGISTRATION RECEIPT");
            receipt.AppendLine("═══════════════════════════════════════");
            receipt.AppendLine();
            receipt.AppendLine($"Receipt Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            receipt.AppendLine($"Receipt No: REG{DateTime.Now:yyyyMMddHHmmss}");
            receipt.AppendLine();
            receipt.AppendLine("STUDENT INFORMATION:");
            receipt.AppendLine("───────────────────────────────────────");
            receipt.AppendLine($"Name: {txtFirstName.Text} {txtLastName.Text}");
            receipt.AppendLine($"Class: {txtClass.Text}");
            receipt.AppendLine($"Date of Birth: {dtpDOB.Value:yyyy-MM-dd}");
            receipt.AppendLine($"Gender: {cbGender.SelectedItem?.ToString() ?? ""}");
            receipt.AppendLine($"Guardian: {txtGuardian.Text}");
            receipt.AppendLine($"Contact: {txtContact.Text}");
            receipt.AppendLine($"Address: {txtAddress.Text}");
            receipt.AppendLine();
            receipt.AppendLine("PAYMENT DETAILS:");
            receipt.AppendLine("───────────────────────────────────────");
            receipt.AppendLine($"Registration Fee: ${REGISTRATION_FEE:F2}");
            receipt.AppendLine($"Amount Paid: ${REGISTRATION_FEE:F2}");
            receipt.AppendLine($"Payment Status: PAID");
            receipt.AppendLine();
            receipt.AppendLine("═══════════════════════════════════════");
            receipt.AppendLine("Thank you for choosing Gusheshe School!");
            receipt.AppendLine("Keep this receipt for your records.");
            receipt.AppendLine("═══════════════════════════════════════");

            // Store receipt content for printing
            receiptContent = receipt.ToString();

            // Display in RichTextBox (assuming you have richTextBox1)
            if (this.Controls.Find("rtbReceipt", true).FirstOrDefault() is RichTextBox rtb)
            {
                rtb.Text = receiptContent;
                rtb.Font = new Font("Courier New", 10, FontStyle.Regular);
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(receiptContent))
            {
                MessageBox.Show("No receipt to print. Please complete a registration first.",
                    "Nothing to Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                PrintDocument printDoc = new PrintDocument();
                printDoc.PrintPage += PrintDoc_PrintPage;

                PrintDialog printDialog = new PrintDialog();
                printDialog.Document = printDoc;

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDoc.Print();
                    MessageBox.Show("Receipt printed successfully!", "Print Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Printing error: {ex.Message}", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Set up fonts
            Font printFont = new Font("Courier New", 10);
            Font headerFont = new Font("Courier New", 12, FontStyle.Bold);

            // Set up margins
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float yPosition = topMargin;
            float lineHeight = printFont.GetHeight(e.Graphics);

            // Split receipt content into lines
            string[] lines = receiptContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // Print each line
            foreach (string line in lines)
            {
                if (yPosition + lineHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                // Use header font for title lines
                Font currentFont = (line.Contains("GUSHESHE SCHOOL") || line.Contains("REGISTRATION RECEIPT"))
                    ? headerFont : printFont;

                e.Graphics.DrawString(line, currentFont, Brushes.Black, leftMargin, yPosition);
                yPosition += lineHeight;
            }

            e.HasMorePages = false;
        }
    }
}

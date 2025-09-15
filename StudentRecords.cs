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
        private int? editingTermId = null;
        private string connectionString;
        private DataTable feeAccountsData;
        private PrintDocument printDocument;


        public StudentRecords()
        {
            connectionString = "Data Source=school.db;Version=3;"; // Use same database as LoadTerms method
            InitializePrintDocument();
            InitializeComponent();
            LoadTerms();
        }
        public StudentRecords(string connString)
        {
            InitializeComponent();
            connectionString = connString;
            InitializePrintDocument();
        }
        
        private void StudentRecords_Load(object sender, EventArgs e)
        {
            LoadStudentRecords();
            try
            {
                LoadInitialData();
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading panel: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void LoadTerms()
        {
            using (var connection = new SQLiteConnection(connectionString)) // Use instance connectionString instead of hardcoded
            {
                connection.Open();
                string query = "SELECT TermId, TermName, StartDate, EndDate, IsActive, CreatedDate FROM AcademicTerms";

                using (var adapter = new SQLiteDataAdapter(query, connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgvTerms.DataSource = dt;
                }
            }

            dgvTerms.Columns["TermId"].Visible = false;  // Hide primary key
            dgvTerms.Columns["IsActive"].HeaderText = "Active";
            dgvTerms.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Clear input fields for a new term
        private void ClearInputs()
        {
            editingTermId = null;
            txtTermName.Clear();
            dtpStartDate.Value = DateTime.Today;
            dtpEndDate.Value = DateTime.Today;
            chkIsActive.Checked = false;
        }

        private void btnSave1_Click(object sender, EventArgs e)
        {
            string termName = txtTermName.Text.Trim();
            DateTime startDate = dtpStartDate.Value;
            DateTime endDate = dtpEndDate.Value;
            bool isActive = chkIsActive.Checked;

            if (string.IsNullOrWhiteSpace(termName))
            {
                MessageBox.Show("Please enter a term name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (endDate < startDate)
            {
                MessageBox.Show("End date cannot be earlier than start date.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new SQLiteConnection(connectionString)) // Use instance connectionString
            {
                connection.Open();

                if (isActive)
                {
                    using (var cmdDeactivate = new SQLiteCommand("UPDATE AcademicTerms SET IsActive = 0", connection))
                        cmdDeactivate.ExecuteNonQuery();
                }

                string sql;
                if (editingTermId == null)
                {
                    // Insert new term
                    sql = "INSERT INTO AcademicTerms (TermName, StartDate, EndDate, IsActive) " +
                          "VALUES (@TermName, @StartDate, @EndDate, @IsActive)";
                }
                else
                {
                    // Update existing term
                    sql = "UPDATE AcademicTerms SET TermName=@TermName, StartDate=@StartDate, EndDate=@EndDate, IsActive=@IsActive " +
                          "WHERE TermId=@TermId";
                }

                using (var cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@TermName", termName);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);

                    if (editingTermId != null)
                        cmd.Parameters.AddWithValue("@TermId", editingTermId.Value);

                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Term saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInputs();
            LoadTerms();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvTerms.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dgvTerms.SelectedRows[0];
                editingTermId = Convert.ToInt32(row.Cells["TermId"].Value);
                txtTermName.Text = row.Cells["TermName"].Value.ToString();
                dtpStartDate.Value = Convert.ToDateTime(row.Cells["StartDate"].Value);
                dtpEndDate.Value = Convert.ToDateTime(row.Cells["EndDate"].Value);
                chkIsActive.Checked = Convert.ToBoolean(row.Cells["IsActive"].Value);
            }
            else
            {
                MessageBox.Show("Please select a term to edit.", "Edit Term", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnAddnew_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void tabPage4_Click(object sender, EventArgs e)
        {
            
        }
        private void InitializePrintDocument()
        {
            printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
        }

        private void LoadInitialData()
        {
            LoadClassFilter();
            LoadTermFilter();
            LoadFeeAccountsData();
        }

        private void LoadClassFilter()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    // Get distinct classes from Students table since there's no separate Classes table
                    string query = "SELECT DISTINCT Class FROM Students WHERE Class IS NOT NULL AND Class != '' ORDER BY Class";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Columns.Add("ClassId", typeof(int));
                            dt.Columns.Add("ClassName", typeof(string));

                            // Add "All Classes" option first
                            DataRow allRow = dt.NewRow();
                            allRow["ClassId"] = 0;
                            allRow["ClassName"] = "All Classes";
                            dt.Rows.Add(allRow);

                            // Add distinct classes from Students table
                            int classId = 1;
                            while (reader.Read())
                            {
                                DataRow row = dt.NewRow();
                                row["ClassId"] = classId++;
                                row["ClassName"] = reader["Class"].ToString();
                                dt.Rows.Add(row);
                            }

                            cmbClass.DisplayMember = "ClassName";
                            cmbClass.ValueMember = "ClassName"; // Use ClassName as value since we don't have ClassId
                            cmbClass.DataSource = dt;
                            cmbClass.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading classes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTermFilter()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    // Use consistent database connection - make sure we're using the same database
                    string query = "SELECT DISTINCT TermId, TermName FROM AcademicTerms ORDER BY TermName";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            if (dt.Rows.Count == 0)
                            {
                                MessageBox.Show("No terms found in the AcademicTerms table. Please add some terms first.",
                                    "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }

                            // Add "All Terms" option
                            DataRow allRow = dt.NewRow();
                            allRow["TermId"] = 0;
                            allRow["TermName"] = "All Terms";
                            dt.Rows.InsertAt(allRow, 0);

                            cmbTerm.DisplayMember = "TermName";
                            cmbTerm.ValueMember = "TermId";
                            cmbTerm.DataSource = dt;
                            cmbTerm.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading terms: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadFeeAccountsData(string className = null, int? termId = null)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT 
                    tfa.AccountId,
                    s.StudentId,
                    s.FirstName || ' ' || s.LastName AS StudentName,
                    s.Class AS ClassName,
                    at.TermName,
                    tfa.ExpectedFees,
                    tfa.TotalPaid,
                    tfa.Balance,
                    CASE 
                        WHEN tfa.Balance > 0 THEN 'Outstanding'
                        WHEN tfa.Balance = 0 THEN 'Paid'
                        ELSE 'Overpaid'
                    END AS Status,
                    tfa.LastPaymentDate,
                    tfa.CreatedDate
                FROM TermFeeAccounts tfa
                INNER JOIN Students s ON tfa.StudentId = s.StudentId
                INNER JOIN AcademicTerms at ON tfa.TermId = at.TermId
                WHERE 1=1";

                    if (!string.IsNullOrEmpty(className) && className != "All Classes")
                        query += " AND s.Class = @ClassName";

                    if (termId.HasValue && termId.Value > 0)
                        query += " AND at.TermId = @TermId";

                    query += " ORDER BY s.Class, s.LastName, s.FirstName";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(className) && className != "All Classes")
                            cmd.Parameters.AddWithValue("@ClassName", className);

                        if (termId.HasValue && termId.Value > 0)
                            cmd.Parameters.AddWithValue("@TermId", termId.Value);

                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        {
                            feeAccountsData = new DataTable();
                            adapter.Fill(feeAccountsData);

                            dgvFeeAccounts.DataSource = feeAccountsData;
                            FormatDataGridView();
                            UpdateRecordCount();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading fee accounts data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatDataGridView()
        {
            if (dgvFeeAccounts.Columns.Count == 0) return;

            // Hide ID columns
            if (dgvFeeAccounts.Columns["AccountId"] != null)
                dgvFeeAccounts.Columns["AccountId"].Visible = false;
            if (dgvFeeAccounts.Columns["StudentId"] != null)
                dgvFeeAccounts.Columns["StudentId"].Visible = false;

            // Set column headers and formatting
            var columnConfig = new[]
            {
                new { Name = "StudentName", Header = "Student Name", Width = 0.2f },
                new { Name = "ClassName", Header = "Class", Width = 0.1f },
                new { Name = "TermName", Header = "Term", Width = 0.12f },
                new { Name = "ExpectedFees", Header = "Expected Fees", Width = 0.12f },
                new { Name = "TotalPaid", Header = "Total Paid", Width = 0.12f },
                new { Name = "Balance", Header = "Balance", Width = 0.12f },
                new { Name = "Status", Header = "Status", Width = 0.08f },
                new { Name = "LastPaymentDate", Header = "Last Payment", Width = 0.12f }
            };

            foreach (var config in columnConfig)
            {
                if (dgvFeeAccounts.Columns[config.Name] != null)
                {
                    dgvFeeAccounts.Columns[config.Name].HeaderText = config.Header;
                    dgvFeeAccounts.Columns[config.Name].FillWeight = config.Width * 100;

                    // Format currency columns
                    if (config.Name.Contains("Fees") || config.Name.Contains("Paid") || config.Name == "Balance")
                    {
                        dgvFeeAccounts.Columns[config.Name].DefaultCellStyle.Format = "C2";
                        dgvFeeAccounts.Columns[config.Name].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }

                    // Format date columns
                    if (config.Name.Contains("Date"))
                    {
                        dgvFeeAccounts.Columns[config.Name].DefaultCellStyle.Format = "dd/MM/yyyy";
                        dgvFeeAccounts.Columns[config.Name].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }

                    // Center status column
                    if (config.Name == "Status")
                    {
                        dgvFeeAccounts.Columns[config.Name].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }
            }

            // Hide CreatedDate column
            if (dgvFeeAccounts.Columns["CreatedDate"] != null)
                dgvFeeAccounts.Columns["CreatedDate"].Visible = false;

            // Apply status color coding
            foreach (DataGridViewRow row in dgvFeeAccounts.Rows)
            {
                if (row.Cells["Status"].Value != null)
                {
                    string status = row.Cells["Status"].Value.ToString();
                    switch (status)
                    {
                        case "Outstanding":
                            row.Cells["Status"].Style.ForeColor = Color.Red;
                            row.Cells["Status"].Style.Font = new Font(dgvFeeAccounts.Font, FontStyle.Bold);
                            break;
                        case "Paid":
                            row.Cells["Status"].Style.ForeColor = Color.Green;
                            row.Cells["Status"].Style.Font = new Font(dgvFeeAccounts.Font, FontStyle.Bold);
                            break;
                        case "Overpaid":
                            row.Cells["Status"].Style.ForeColor = Color.Blue;
                            row.Cells["Status"].Style.Font = new Font(dgvFeeAccounts.Font, FontStyle.Bold);
                            break;
                    }
                }
            }

            // Style the DataGridView
            dgvFeeAccounts.EnableHeadersVisualStyles = false;
            dgvFeeAccounts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 58, 64);
            dgvFeeAccounts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvFeeAccounts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvFeeAccounts.ColumnHeadersHeight = 35;
            dgvFeeAccounts.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgvFeeAccounts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 123, 255);
            dgvFeeAccounts.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvFeeAccounts.GridColor = Color.FromArgb(222, 226, 230);
        }

        private void UpdateRecordCount()
        {
            int totalRecords = feeAccountsData?.Rows.Count ?? 0;
            lblRecordCount.Text = $"Total Records: {totalRecords:N0}";
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            try
            {
                string className = null;
                int? termId = null;

                // Handle class filter
                if (cmbClass.SelectedValue != null && cmbClass.SelectedValue.ToString() != "All Classes")
                {
                    className = cmbClass.SelectedValue.ToString();
                }

                // Handle term filter with safe casting
                if (cmbTerm.SelectedValue != null)
                {
                    if (int.TryParse(cmbTerm.SelectedValue.ToString(), out int termIdValue) && termIdValue > 0)
                    {
                        termId = termIdValue;
                    }
                }

                LoadFeeAccountsData(className, termId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filter: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                cmbClass.SelectedIndex = 0; // Select "All Classes"
                cmbTerm.SelectedIndex = 0;  // Select "All Terms"
                LoadFeeAccountsData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing filter: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btnPrint1_Click(object sender, EventArgs e)
        {
            try
            {
                if (feeAccountsData == null || feeAccountsData.Rows.Count == 0)
                {
                    MessageBox.Show("No data to print.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                PrintPreviewDialog printPreview = new PrintPreviewDialog();
                printPreview.Document = printDocument;
                printPreview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing print: {ex.Message}", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                Graphics graphics = e.Graphics;
                Font titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
                Font headerFont = new Font("Segoe UI", 10, FontStyle.Bold);
                Font cellFont = new Font("Segoe UI", 9);

                float yPos = 50;
                float leftMargin = 50;
                float rightMargin = e.PageBounds.Width - 50;

                // Print title
                string title = "Term Fee Accounts Report";
                SizeF titleSize = graphics.MeasureString(title, titleFont);
                graphics.DrawString(title, titleFont, Brushes.Black,
                    (e.PageBounds.Width - titleSize.Width) / 2, yPos);
                yPos += titleSize.Height + 20;

                // Print filter information
                string filterInfo = "";
                if (cmbClass.SelectedIndex > 0)
                    filterInfo += $"Class: {cmbClass.Text} ";
                if (cmbTerm.SelectedIndex > 0)
                    filterInfo += $"Term: {cmbTerm.Text}";

                if (!string.IsNullOrEmpty(filterInfo))
                {
                    graphics.DrawString($"Filters Applied: {filterInfo}", cellFont, Brushes.Gray, leftMargin, yPos);
                    yPos += 20;
                }

                graphics.DrawString($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}", cellFont, Brushes.Gray, leftMargin, yPos);
                yPos += 30;

                // Calculate column widths
                float availableWidth = rightMargin - leftMargin;
                float[] columnWidths = { 0.25f, 0.12f, 0.1f, 0.12f, 0.12f, 0.12f, 0.12f, 0.05f };

                // Print headers
                string[] headers = { "Student Name", "Class", "Term", "Expected", "Paid", "Balance", "Status" };
                float xPos = leftMargin;

                for (int i = 0; i < headers.Length; i++)
                {
                    float colWidth = availableWidth * columnWidths[i];
                    graphics.DrawString(headers[i], headerFont, Brushes.Black,
                        new RectangleF(xPos, yPos, colWidth, 20),
                        new StringFormat { Alignment = StringAlignment.Near });
                    xPos += colWidth;
                }
                yPos += 25;

                // Draw header line
                graphics.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
                yPos += 5;

                // Print data rows
                foreach (DataRow row in feeAccountsData.Rows)
                {
                    if (yPos > e.PageBounds.Height - 100)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    xPos = leftMargin;
                    string[] values = {
                        row["StudentName"].ToString(),
                        row["ClassName"].ToString(),
                        row["TermName"].ToString(),
                        Convert.ToDecimal(row["ExpectedFees"]).ToString("C2"),
                        Convert.ToDecimal(row["TotalPaid"]).ToString("C2"),
                        Convert.ToDecimal(row["Balance"]).ToString("C2"),
                        row["Status"].ToString()
                    };

                    for (int i = 0; i < values.Length; i++)
                    {
                        float colWidth = availableWidth * columnWidths[i];
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Near };
                        if (i >= 4 && i <= 6) // Currency columns
                            sf.Alignment = StringAlignment.Far;

                        graphics.DrawString(values[i], cellFont, Brushes.Black,
                            new RectangleF(xPos, yPos, colWidth, 20), sf);
                        xPos += colWidth;
                    }
                    yPos += 18;
                }

                // Print summary
                yPos += 20;
                graphics.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
                yPos += 10;

                decimal totalExpected = 0, totalPaid = 0, totalBalance = 0;
                foreach (DataRow row in feeAccountsData.Rows)
                {
                    totalExpected += Convert.ToDecimal(row["ExpectedFees"]);
                    totalPaid += Convert.ToDecimal(row["TotalPaid"]);
                    totalBalance += Convert.ToDecimal(row["Balance"]);
                }

                graphics.DrawString($"Total Expected: {totalExpected:C2}", headerFont, Brushes.Black, leftMargin, yPos);
                graphics.DrawString($"Total Paid: {totalPaid:C2}", headerFont, Brushes.Black, leftMargin + 200, yPos);
                graphics.DrawString($"Total Balance: {totalBalance:C2}", headerFont, Brushes.Black, leftMargin + 400, yPos);

                e.HasMorePages = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

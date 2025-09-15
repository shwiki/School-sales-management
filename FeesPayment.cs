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
using System.IO;

namespace Gusheshe
{
    public partial class FeesPayment : Form
    {
        private string receiptContent = "";
        private int selectedStudentId = 0;
        private int selectedTermId = 0; // NEW: Track selected term
        private List<TermInfo> availableTerms = new List<TermInfo>();
        private int currentActiveTermId = 0;
        private string selectedClass;

        private void InitializeClassFilter()
        {
            try
            {
                // If you're creating the combobox programmatically, uncomment these lines:
                /*
                cmbClassFilter = new ComboBox();
                cmbClassFilter.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbClassFilter.Size = new Size(150, 21);
                cmbClassFilter.Location = new Point(10, 10); // Adjust as needed
                this.Controls.Add(cmbClassFilter);
                */

                cmbClassFilter.Items.Clear();
                cmbClassFilter.Items.Add("All Classes"); // Option to show all students

                using (var conn = SQLiteHelper.GetConnection())
                {
                    using (var cmd = new SQLiteCommand("SELECT DISTINCT Class FROM Students WHERE Class IS NOT NULL AND Class != '' ORDER BY Class", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string className = reader["Class"]?.ToString();
                                if (!string.IsNullOrEmpty(className))
                                {
                                    cmbClassFilter.Items.Add(className);
                                }
                            }
                        }
                    }
                }

                cmbClassFilter.SelectedIndex = 0; // Select "All Classes" by default
                cmbClassFilter.SelectedIndexChanged += cmbClassFilter_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading class filter: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void cmbClassFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedClass = cmbClassFilter.SelectedItem?.ToString() ?? "";
            LoadStudentsComboBox(selectedClass);

            // Clear payment history filter when changing class
            LoadPaymentHistory();
        }

        public FeesPayment()
        {
            InitializeComponent();
            LoadAvailableTerms();
            LoadTermsComboBox();
            InitializeClassFilter(); // Add this line
            LoadStudentsComboBox(); // This will now load all students initially
            LoadTermBasedFeeAccounts();
            LoadPaymentHistory();

        }
        private void RefreshWithCurrentFilter()
        {
            LoadStudentsComboBox(selectedClass);
            LoadTermBasedFeeAccounts();

            if (selectedStudentId > 0)
            {
                LoadPaymentHistory(selectedStudentId);
                LoadStudentMultiTermDetails(selectedStudentId);
            }
            else
            {
                LoadPaymentHistory();
            }
        }
        private void FeesPayment_Load(object sender, EventArgs e)
        {
            dtpPaymentDate.Value = DateTime.Now;
            txtDescription.Text = "School Fees Payment";
            SetupDataGridViews();
        }

        private void LoadAvailableTerms()
        {
            try
            {
                availableTerms.Clear();
                currentActiveTermId = SQLiteHelper.GetActiveTermId();

                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    SELECT TermId, TermName, StartDate, EndDate, IsActive 
                    FROM AcademicTerms 
                    ORDER BY StartDate DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        availableTerms.Add(new TermInfo
                        {
                            TermId = Convert.ToInt32(reader["TermId"]),
                            TermName = reader["TermName"].ToString(),
                            StartDate = Convert.ToDateTime(reader["StartDate"]),
                            EndDate = Convert.ToDateTime(reader["EndDate"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }

                // Ensure we have at least one term
                if (availableTerms.Count == 0)
                {
                    CreateDefaultTerm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading terms: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NEW: Load terms into combobox
        private void LoadTermsComboBox()
        {
            try
            {
                cmbTerm.Items.Clear();

                // Add "All Terms" option for payment history filtering
                cmbTerm.Items.Add(new ComboBoxItem { Value = -1, Text = "All Terms" });

                foreach (var term in availableTerms)
                {
                    var item = new ComboBoxItem
                    {
                        Value = term.TermId,
                        Text = term.IsActive ? $"{term.TermName} (Active)" : term.TermName
                    };
                    cmbTerm.Items.Add(item);
                }

                // Select active term by default
                var activeTerm = cmbTerm.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Value == currentActiveTermId);
                if (activeTerm != null)
                {
                    cmbTerm.SelectedItem = activeTerm;
                    selectedTermId = currentActiveTermId;
                }
                else if (cmbTerm.Items.Count > 1)
                {
                    cmbTerm.SelectedIndex = 1; // Select first actual term (skip "All Terms")
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading terms combobox: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateDefaultTerm()
        {
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO AcademicTerms (TermName, StartDate, EndDate, IsActive)
                    VALUES ('Term 1 - 2025', '2025-01-01', '2025-04-30', 1)", conn))
                {
                    cmd.ExecuteNonQuery();
                }
                LoadAvailableTerms(); // Reload after creating
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating default term: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupDataGridViews()
        {
            // Setup Fee Accounts DataGridView for term-based view
            if (dgvFeeAccounts.Columns.Count == 0)
            {
                dgvFeeAccounts.Columns.Add("StudentId", "Student ID");
                dgvFeeAccounts.Columns.Add("StudentName", "Student Name");
                dgvFeeAccounts.Columns.Add("Class", "Class");
                dgvFeeAccounts.Columns.Add("CurrentTerm", "Current Term");
                dgvFeeAccounts.Columns.Add("TermExpected", "Term Expected");
                dgvFeeAccounts.Columns.Add("TermPaid", "Term Paid");
                dgvFeeAccounts.Columns.Add("TermBalance", "Term Balance");
                dgvFeeAccounts.Columns.Add("TotalExpected", "Total Expected");
                dgvFeeAccounts.Columns.Add("TotalPaid", "Total Paid");
                dgvFeeAccounts.Columns.Add("OverallBalance", "Overall Balance");
                dgvFeeAccounts.Columns.Add("LastPaymentDate", "Last Payment");

                // Format currency columns
                string[] currencyColumns = { "TermExpected", "TermPaid", "TermBalance", "TotalExpected", "TotalPaid", "OverallBalance" };
                foreach (string col in currencyColumns)
                {
                    dgvFeeAccounts.Columns[col].DefaultCellStyle.Format = "C2";
                }

                dgvFeeAccounts.Columns["StudentId"].Visible = false;
            }

            // Setup Payment History DataGridView
            if (dgvPaymentHistory.Columns.Count == 0)
            {
                dgvPaymentHistory.Columns.Add("PaymentId", "Payment ID");
                dgvPaymentHistory.Columns.Add("StudentId", "Student ID"); // NEW: Added for filtering
                dgvPaymentHistory.Columns.Add("StudentName", "Student Name");
                dgvPaymentHistory.Columns.Add("TermName", "Term");
                dgvPaymentHistory.Columns.Add("AmountPaid", "Amount Paid");
                dgvPaymentHistory.Columns.Add("DatePaid", "Date Paid");
                dgvPaymentHistory.Columns.Add("Description", "Description");

                dgvPaymentHistory.Columns["AmountPaid"].DefaultCellStyle.Format = "C2";
                dgvPaymentHistory.Columns["PaymentId"].Visible = false;
                dgvPaymentHistory.Columns["StudentId"].Visible = false; // Hide but keep for filtering
            }
        }

        private void LoadStudentsComboBox(string classFilter = "")
        {
            try
            {
                cmbStudent.Items.Clear();

                using (var conn = SQLiteHelper.GetConnection())
                {
                    string query;
                    if (string.IsNullOrEmpty(classFilter) || classFilter == "All Classes")
                    {
                        // Load all students
                        query = "SELECT StudentId, FirstName, LastName, Class FROM Students ORDER BY FirstName, LastName";
                    }
                    else
                    {
                        // Load students from specific class
                        query = "SELECT StudentId, FirstName, LastName, Class FROM Students WHERE Class = @class ORDER BY FirstName, LastName";
                    }

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(classFilter) && classFilter != "All Classes")
                        {
                            cmd.Parameters.AddWithValue("@class", classFilter);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new ComboBoxItem
                                {
                                    Value = Convert.ToInt32(reader["StudentId"]),
                                    Text = $"{reader["FirstName"]} {reader["LastName"]} - {reader["Class"]}"
                                };
                                cmbStudent.Items.Add(item);
                            }
                        }
                    }
                }

                // Reset student selection
                selectedStudentId = 0;

                // Clear student details when changing filter
                lblExpectedFees.Text = "Total Expected: $0.00";
                lblTotalPaid.Text = "Total Paid: $0.00";
                lblArrears.Text = "Total Balance: $0.00";
                lblLastPayment.Text = "Last Payment: Never";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading students: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadTermBasedFeeAccounts()
        {
            try
            {
                dgvFeeAccounts.Rows.Clear();
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    WITH StudentTermSummary AS (
                        SELECT 
                            s.StudentId,
                            s.FirstName || ' ' || s.LastName as StudentName,
                            s.Class,
                            -- Current active term info
                            tfa_active.ExpectedFees as CurrentTermExpected,
                            COALESCE(tfa_active.TotalPaid, 0) as CurrentTermPaid,
                            COALESCE(tfa_active.Balance, tfa_active.ExpectedFees) as CurrentTermBalance,
                            at_active.TermName as CurrentTermName,
                            -- Total across all terms
                            COALESCE(SUM(tfa_all.ExpectedFees), 0) as TotalExpected,
                            COALESCE(SUM(tfa_all.TotalPaid), 0) as TotalPaid,
                            COALESCE(SUM(tfa_all.Balance), 0) as OverallBalance,
                            MAX(tfa_all.LastPaymentDate) as LastPaymentDate
                        FROM Students s
                        LEFT JOIN TermFeeAccounts tfa_active ON s.StudentId = tfa_active.StudentId AND tfa_active.TermId = @activeTermId
                        LEFT JOIN AcademicTerms at_active ON tfa_active.TermId = at_active.TermId
                        LEFT JOIN TermFeeAccounts tfa_all ON s.StudentId = tfa_all.StudentId
                        GROUP BY s.StudentId, s.FirstName, s.LastName, s.Class, 
                                 tfa_active.ExpectedFees, tfa_active.TotalPaid, tfa_active.Balance, at_active.TermName
                    )
                    SELECT * FROM StudentTermSummary
                    ORDER BY StudentName", conn))
                {
                    cmd.Parameters.AddWithValue("@activeTermId", currentActiveTermId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dgvFeeAccounts.Rows.Add(
                                reader["StudentId"],
                                reader["StudentName"],
                                reader["Class"],
                                reader["CurrentTermName"]?.ToString() ?? "No Term",
                                reader["CurrentTermExpected"] != DBNull.Value ? Convert.ToDouble(reader["CurrentTermExpected"]) : 0.0,
                                reader["CurrentTermPaid"] != DBNull.Value ? Convert.ToDouble(reader["CurrentTermPaid"]) : 0.0,
                                reader["CurrentTermBalance"] != DBNull.Value ? Convert.ToDouble(reader["CurrentTermBalance"]) : 0.0,
                                Convert.ToDouble(reader["TotalExpected"]),
                                Convert.ToDouble(reader["TotalPaid"]),
                                Convert.ToDouble(reader["OverallBalance"]),
                                reader["LastPaymentDate"]?.ToString() ?? "Never"
                            );
                        }
                    }
                }

                // Color code rows based on payment status
                ColorCodeFeeAccountRows();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading fee accounts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ColorCodeFeeAccountRows()
        {
            foreach (DataGridViewRow row in dgvFeeAccounts.Rows)
            {
                if (row.Cells["OverallBalance"].Value != null)
                {
                    double overallBalance = Convert.ToDouble(row.Cells["OverallBalance"].Value);
                    double totalPaid = Convert.ToDouble(row.Cells["TotalPaid"].Value);

                    if (overallBalance <= 0)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightGreen; // Paid in full
                    }
                    else if (totalPaid > 0)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightYellow; // Partial payment
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral; // No payment
                    }
                }
            }
        }

        // MODIFIED: Load payment history with filtering capability
        private void LoadPaymentHistory(int filterStudentId = 0)
        {
            try
            {
                dgvPaymentHistory.Rows.Clear();

                string whereClause = "";
                if (filterStudentId > 0)
                {
                    whereClause = "WHERE fp.StudentId = @studentId";
                }

                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand($@"
                    SELECT 
                        fp.PaymentId,
                        fp.StudentId,
                        s.FirstName || ' ' || s.LastName as StudentName,
                        COALESCE(at.TermName, 'Legacy Payment') as TermName,
                        fp.AmountPaid,
                        fp.DatePaid,
                        fp.Description
                    FROM FeePayments fp
                    JOIN Students s ON fp.StudentId = s.StudentId
                    LEFT JOIN AcademicTerms at ON fp.TermId = at.TermId
                    {whereClause}
                    ORDER BY fp.DatePaid DESC, fp.PaymentId DESC
                    LIMIT 100", conn))
                {
                    if (filterStudentId > 0)
                    {
                        cmd.Parameters.AddWithValue("@studentId", filterStudentId);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dgvPaymentHistory.Rows.Add(
                                reader["PaymentId"],
                                reader["StudentId"],
                                reader["StudentName"],
                                reader["TermName"],
                                Convert.ToDouble(reader["AmountPaid"]),
                                reader["DatePaid"],
                                reader["Description"]
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payment history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NEW: Handle term combobox selection
        private void cmbTerm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTerm.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedTermId = selectedItem.Value;
                UpdateDescriptionBasedOnTerm();
            }
        }

        // NEW: Update description when term changes
        private void UpdateDescriptionBasedOnTerm()
        {
            if (selectedTermId > 0)
            {
                var selectedTerm = availableTerms.FirstOrDefault(t => t.TermId == selectedTermId);
                if (selectedTerm != null)
                {
                    txtDescription.Text = $"School Fees Payment - {selectedTerm.TermName}";
                }
            }
            else
            {
                txtDescription.Text = "School Fees Payment";
            }
        }

        // MODIFIED: Handle student selection and filter payment history
        private void cmbStudent_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbStudent.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedStudentId = selectedItem.Value;
                LoadStudentMultiTermDetails(selectedStudentId);
                // NEW: Filter payment history for selected student
                LoadPaymentHistory(selectedStudentId);
            }
            else
            {
                selectedStudentId = 0;
                // Show all payment history when no student selected
                LoadPaymentHistory();
            }
        }

        private void LoadStudentMultiTermDetails(int studentId)
        {
            try
            {
                StringBuilder termDetails = new StringBuilder();
                double totalExpected = 0, totalPaid = 0, totalBalance = 0;
                DateTime? lastPayment = null;

                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    SELECT 
                        at.TermName,
                        at.IsActive,
                        COALESCE(tfa.ExpectedFees, 0) as ExpectedFees,
                        COALESCE(tfa.TotalPaid, 0) as TotalPaid,
                        COALESCE(tfa.Balance, tfa.ExpectedFees) as Balance,
                        tfa.LastPaymentDate
                    FROM AcademicTerms at
                    LEFT JOIN TermFeeAccounts tfa ON at.TermId = tfa.TermId AND tfa.StudentId = @studentId
                    ORDER BY at.StartDate", conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            double expected = Convert.ToDouble(reader["ExpectedFees"]);
                            double paid = Convert.ToDouble(reader["TotalPaid"]);
                            double balance = Convert.ToDouble(reader["Balance"]);
                            bool isActive = Convert.ToBoolean(reader["IsActive"]);

                            totalExpected += expected;
                            totalPaid += paid;
                            totalBalance += balance;

                            if (reader["LastPaymentDate"] != DBNull.Value)
                            {
                                var paymentDate = Convert.ToDateTime(reader["LastPaymentDate"]);
                                if (lastPayment == null || paymentDate > lastPayment)
                                    lastPayment = paymentDate;
                            }

                            string status = isActive ? " (Active)" : "";
                            termDetails.AppendLine($"{reader["TermName"]}{status}: Expected ${expected:F2}, Paid ${paid:F2}, Balance ${balance:F2}");
                        }
                    }
                }

                lblExpectedFees.Text = $"Total Expected: ${totalExpected:F2}";
                lblTotalPaid.Text = $"Total Paid: ${totalPaid:F2}";
                lblArrears.Text = $"Total Balance: ${totalBalance:F2}";
                lblLastPayment.Text = $"Last Payment: {lastPayment?.ToString("yyyy-MM-dd") ?? "Never"}";

                // Set suggested payment amount to remaining balance
                if (totalBalance > 0)
                {
                    nudPaymentAmount.Value = (decimal)Math.Min(totalBalance, (double)nudPaymentAmount.Maximum);
                }

                // Show term breakdown in a tooltip or label
                if (this.Controls.Find("lblTermBreakdown", true).FirstOrDefault() is Label lblBreakdown)
                {
                    lblBreakdown.Text = termDetails.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading student details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MODIFIED: Updated to use selected term if specified
        private void btnMakePayment_Click(object sender, EventArgs e)
        {
            if (selectedStudentId == 0)
            {
                MessageBox.Show("Please select a student.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (nudPaymentAmount.Value <= 0)
            {
                MessageBox.Show("Please enter a valid payment amount.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double paymentAmount = (double)nudPaymentAmount.Value;
            string description = txtDescription.Text.Trim();
            if (string.IsNullOrEmpty(description))
            {
                description = "School Fees Payment";
            }

            // If specific term selected, use single-term payment, otherwise use multi-term distribution
            if (selectedTermId > 0 && selectedTermId != -1)
            {
                ProcessSingleTermPayment(selectedStudentId, selectedTermId, paymentAmount, dtpPaymentDate.Value, description);
            }
            else
            {
                // Show payment breakdown before confirming
                var breakdown = CalculatePaymentDistribution(selectedStudentId, paymentAmount);
                StringBuilder confirmationMessage = new StringBuilder();
                confirmationMessage.AppendLine($"Student: {cmbStudent.Text}");
                confirmationMessage.AppendLine($"Total Payment: ${paymentAmount:F2}");
                confirmationMessage.AppendLine($"Date: {dtpPaymentDate.Value:yyyy-MM-dd}");
                confirmationMessage.AppendLine($"Description: {description}");
                confirmationMessage.AppendLine();
                confirmationMessage.AppendLine("Payment Distribution:");

                foreach (var item in breakdown)
                {
                    confirmationMessage.AppendLine($"  {item.TermName}: ${item.Amount:F2}");
                }

                var result = MessageBox.Show(confirmationMessage.ToString() + "\n\nProcess this payment?",
                    "Confirm Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        ProcessMultiTermPayment(selectedStudentId, paymentAmount, dtpPaymentDate.Value, description, breakdown);
                        MessageBox.Show("Payment processed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Update daily sales
                        UpdateDailySales(dtpPaymentDate.Value.Date, paymentAmount);

                        // Refresh all data
                        LoadTermBasedFeeAccounts();
                        LoadPaymentHistory(selectedStudentId); // Keep filter on selected student
                        LoadStudentMultiTermDetails(selectedStudentId);

                        // Generate receipt
                        GenerateMultiTermPaymentReceipt(selectedStudentId, paymentAmount, dtpPaymentDate.Value, description, breakdown);

                        ClearPaymentForm();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // NEW: Process payment for specific term
        private void ProcessSingleTermPayment(int studentId, int termId, double amount, DateTime paymentDate, string description)
        {
            var result = MessageBox.Show($"Process ${amount:F2} payment for the selected term?\n\nStudent: {cmbStudent.Text}\nTerm: {cmbTerm.Text}\nAmount: ${amount:F2}",
                "Confirm Single Term Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var conn = SQLiteHelper.GetConnection())
                    using (var transaction = conn.BeginTransaction())
                    {
                        // Insert payment record
                        using (var paymentCmd = new SQLiteCommand(@"
                            INSERT INTO FeePayments (StudentId, TermId, AmountPaid, DatePaid, Description, IsSynced, CreatedDate)
                            VALUES (@studentId, @termId, @amount, @date, @description, 0, @createdDate)", conn, transaction))
                        {
                            paymentCmd.Parameters.AddWithValue("@studentId", studentId);
                            paymentCmd.Parameters.AddWithValue("@termId", termId);
                            paymentCmd.Parameters.AddWithValue("@amount", amount);
                            paymentCmd.Parameters.AddWithValue("@date", paymentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                            paymentCmd.Parameters.AddWithValue("@description", description);
                            paymentCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            paymentCmd.ExecuteNonQuery();
                        }

                        // Update or create TermFeeAccounts record
                        using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM TermFeeAccounts WHERE StudentId = @studentId AND TermId = @termId", conn, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@studentId", studentId);
                            checkCmd.Parameters.AddWithValue("@termId", termId);
                            int accountExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (accountExists > 0)
                            {
                                // Update existing account
                                using (var updateCmd = new SQLiteCommand(@"
                                    UPDATE TermFeeAccounts 
                                    SET TotalPaid = TotalPaid + @amount,
                                        Balance = ExpectedFees - (TotalPaid + @amount),
                                        LastPaymentDate = @date,
                                        UpdatedDate = @updatedDate
                                    WHERE StudentId = @studentId AND TermId = @termId", conn, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@studentId", studentId);
                                    updateCmd.Parameters.AddWithValue("@termId", termId);
                                    updateCmd.Parameters.AddWithValue("@amount", amount);
                                    updateCmd.Parameters.AddWithValue("@date", paymentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    updateCmd.Parameters.AddWithValue("@updatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Get expected fees for this term
                                double expectedFees = 0;
                                using (var feeCmd = new SQLiteCommand("SELECT ExpectedFees FROM StudentTermFees WHERE StudentId = @studentId AND TermId = @termId", conn, transaction))
                                {
                                    feeCmd.Parameters.AddWithValue("@studentId", studentId);
                                    feeCmd.Parameters.AddWithValue("@termId", termId);
                                    var result2 = feeCmd.ExecuteScalar();
                                    if (result2 != null)
                                    {
                                        expectedFees = Convert.ToDouble(result2);
                                    }
                                    else
                                    {
                                        // Get from Students table as fallback
                                        using (var studentFeeCmd = new SQLiteCommand("SELECT ExpectedFees FROM Students WHERE StudentId = @studentId", conn, transaction))
                                        {
                                            studentFeeCmd.Parameters.AddWithValue("@studentId", studentId);
                                            var studentFeeResult = studentFeeCmd.ExecuteScalar();
                                            expectedFees = studentFeeResult != null ? Convert.ToDouble(studentFeeResult) : 0;
                                        }
                                    }
                                }

                                // Create new account record
                                using (var insertCmd = new SQLiteCommand(@"
                                    INSERT INTO TermFeeAccounts (StudentId, TermId, ExpectedFees, TotalPaid, Balance, LastPaymentDate, CreatedDate, UpdatedDate)
                                    VALUES (@studentId, @termId, @expectedFees, @amount, @balance, @date, @createdDate, @updatedDate)", conn, transaction))
                                {
                                    insertCmd.Parameters.AddWithValue("@studentId", studentId);
                                    insertCmd.Parameters.AddWithValue("@termId", termId);
                                    insertCmd.Parameters.AddWithValue("@expectedFees", expectedFees);
                                    insertCmd.Parameters.AddWithValue("@amount", amount);
                                    insertCmd.Parameters.AddWithValue("@balance", expectedFees - amount);
                                    insertCmd.Parameters.AddWithValue("@date", paymentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCmd.Parameters.AddWithValue("@updatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                    }

                    MessageBox.Show("Payment processed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Update daily sales
                    UpdateDailySales(paymentDate.Date, amount);

                    // Refresh all data
                    LoadTermBasedFeeAccounts();
                    LoadPaymentHistory(selectedStudentId);
                    LoadStudentMultiTermDetails(selectedStudentId);

                    // Generate simple receipt
                    GenerateSingleTermPaymentReceipt(studentId, termId, amount, paymentDate, description);

                    ClearPaymentForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // NEW: Generate receipt for single term payment
        private void GenerateSingleTermPaymentReceipt(int studentId, int termId, double amount, DateTime paymentDate, string description)
        {
            try
            {
                StringBuilder receipt = new StringBuilder();

                // Get student and term details
                string studentName = "", studentClass = "", termName = "";

                using (var conn = SQLiteHelper.GetConnection())
                {
                    // Get student info
                    using (var studentCmd = new SQLiteCommand(@"
                        SELECT FirstName || ' ' || LastName as StudentName, Class 
                        FROM Students WHERE StudentId = @studentId", conn))
                    {
                        studentCmd.Parameters.AddWithValue("@studentId", studentId);
                        using (var reader = studentCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                studentName = reader["StudentName"].ToString();
                                studentClass = reader["Class"].ToString();
                            }
                        }
                    }

                    // Get term info
                    using (var termCmd = new SQLiteCommand("SELECT TermName FROM AcademicTerms WHERE TermId = @termId", conn))
                    {
                        termCmd.Parameters.AddWithValue("@termId", termId);
                        var result = termCmd.ExecuteScalar();
                        termName = result?.ToString() ?? "Unknown Term";
                    }
                }

                receipt.AppendLine("═══════════════════════════════════════");
                receipt.AppendLine("           GUSHESHE SCHOOL");
                receipt.AppendLine("         FEE PAYMENT RECEIPT");
                receipt.AppendLine("═══════════════════════════════════════");
                receipt.AppendLine();
                receipt.AppendLine($"Receipt Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                receipt.AppendLine($"Receipt No: FEE{DateTime.Now:yyyyMMddHHmmss}");
                receipt.AppendLine();
                receipt.AppendLine("STUDENT INFORMATION:");
                receipt.AppendLine("───────────────────────────────────────");
                receipt.AppendLine($"Name: {studentName}");
                receipt.AppendLine($"Class: {studentClass}");
                receipt.AppendLine();
                receipt.AppendLine("PAYMENT DETAILS:");
                receipt.AppendLine("───────────────────────────────────────");
                receipt.AppendLine($"Term: {termName}");
                receipt.AppendLine($"Payment Date: {paymentDate:yyyy-MM-dd}");
                receipt.AppendLine($"Amount Paid: ${amount:F2}");
                receipt.AppendLine($"Description: {description}");
                receipt.AppendLine();
                receipt.AppendLine("═══════════════════════════════════════");
                receipt.AppendLine("Thank you for your payment!");
                receipt.AppendLine("Keep this receipt for your records.");
                receipt.AppendLine("═══════════════════════════════════════");

                receiptContent = receipt.ToString();

                // Display in RichTextBox if available
                if (this.Controls.Find("rtbReceipt", true).FirstOrDefault() is RichTextBox rtb)
                {
                    rtb.Text = receiptContent;
                    rtb.Font = new Font("Courier New", 10, FontStyle.Regular);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NEW: Clear payment form and reset filters
        private void ClearPaymentForm()
        {
            nudPaymentAmount.Value = 0;
            txtDescription.Text = "School Fees Payment";
            cmbStudent.SelectedIndex = -1;
            cmbTerm.SelectedIndex = cmbTerm.Items.Count > 0 ? 0 : -1; // Reset to "All Terms"
            selectedStudentId = 0;
            selectedTermId = 0;
            receiptContent = "";

            // Reset payment history to show all
            LoadPaymentHistory();

            // Clear student details labels
            lblExpectedFees.Text = "Total Expected: $0.00";
            lblTotalPaid.Text = "Total Paid: $0.00";
            lblArrears.Text = "Total Balance: $0.00";
            lblLastPayment.Text = "Last Payment: Never";
        }

        // NEW: Refresh button handler
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadTermBasedFeeAccounts();
            if (selectedStudentId > 0)
            {
                LoadPaymentHistory(selectedStudentId);
                LoadStudentMultiTermDetails(selectedStudentId);
            }
            else
            {
                LoadPaymentHistory();
            }
        }

        // Continue with the rest of the existing methods...
        private List<PaymentDistribution> CalculatePaymentDistribution(int studentId, double paymentAmount)
        {
            var distribution = new List<PaymentDistribution>();
            double remainingAmount = paymentAmount;

            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    SELECT 
                        at.TermId,
                        at.TermName,
                        COALESCE(tfa.ExpectedFees, 0) as ExpectedFees,
                        COALESCE(tfa.TotalPaid, 0) as TotalPaid,
                        COALESCE(tfa.Balance, tfa.ExpectedFees, 0) as Balance
                    FROM AcademicTerms at
                    LEFT JOIN TermFeeAccounts tfa ON at.TermId = tfa.TermId AND tfa.StudentId = @studentId
                    WHERE COALESCE(tfa.Balance, tfa.ExpectedFees, 0) > 0
                    ORDER BY at.StartDate", conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read() && remainingAmount > 0)
                        {
                            double balance = Convert.ToDouble(reader["Balance"]);
                            if (balance > 0)
                            {
                                double amountForThisTerm = Math.Min(remainingAmount, balance);
                                distribution.Add(new PaymentDistribution
                                {
                                    TermId = Convert.ToInt32(reader["TermId"]),
                                    TermName = reader["TermName"].ToString(),
                                    Amount = amountForThisTerm
                                });
                                remainingAmount -= amountForThisTerm;
                            }
                        }
                    }
                }

                // If there's still remaining amount (overpayment), apply to the latest active term
                if (remainingAmount > 0)
                {
                    var latestTerm = availableTerms.Where(t => t.IsActive).FirstOrDefault() ?? availableTerms.LastOrDefault();
                    if (latestTerm != null)
                    {
                        var existingDistribution = distribution.FirstOrDefault(d => d.TermId == latestTerm.TermId);
                        if (existingDistribution != null)
                        {
                            existingDistribution.Amount += remainingAmount;
                        }
                        else
                        {
                            distribution.Add(new PaymentDistribution
                            {
                                TermId = latestTerm.TermId,
                                TermName = latestTerm.TermName + " (Advance)",
                                Amount = remainingAmount
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating payment distribution: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return distribution;
        }

        private void ProcessMultiTermPayment(int studentId, double totalAmount, DateTime paymentDate, string description, List<PaymentDistribution> distribution)
        {
            using (var conn = SQLiteHelper.GetConnection())
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var item in distribution)
                    {
                        // Insert payment record for each term
                        using (var paymentCmd = new SQLiteCommand(@"
                            INSERT INTO FeePayments (StudentId, TermId, AmountPaid, DatePaid, Description, IsSynced, CreatedDate)
                            VALUES (@studentId, @termId, @amount, @date, @description, 0, @createdDate)", conn, transaction))
                        {
                            paymentCmd.Parameters.AddWithValue("@studentId", studentId);
                            paymentCmd.Parameters.AddWithValue("@termId", item.TermId);
                            paymentCmd.Parameters.AddWithValue("@amount", item.Amount);
                            paymentCmd.Parameters.AddWithValue("@date", paymentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                            paymentCmd.Parameters.AddWithValue("@description", $"{description} - {item.TermName}");
                            paymentCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            paymentCmd.ExecuteNonQuery();
                        }

                        // Update or create TermFeeAccounts record
                        using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM TermFeeAccounts WHERE StudentId = @studentId AND TermId = @termId", conn, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@studentId", studentId);
                            checkCmd.Parameters.AddWithValue("@termId", item.TermId);
                            int accountExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (accountExists > 0)
                            {
                                // Update existing account
                                using (var updateCmd = new SQLiteCommand(@"
                                    UPDATE TermFeeAccounts 
                                    SET TotalPaid = TotalPaid + @amount,
                                        Balance = ExpectedFees - (TotalPaid + @amount),
                                        LastPaymentDate = @date,
                                        UpdatedDate = @updatedDate
                                    WHERE StudentId = @studentId AND TermId = @termId", conn, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@studentId", studentId);
                                    updateCmd.Parameters.AddWithValue("@termId", item.TermId);
                                    updateCmd.Parameters.AddWithValue("@amount", item.Amount);
                                    updateCmd.Parameters.AddWithValue("@date", paymentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    updateCmd.Parameters.AddWithValue("@updatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Get expected fees for this term from StudentTermFees or use default
                                double expectedFees = 0;
                                using (var feeCmd = new SQLiteCommand("SELECT ExpectedFees FROM StudentTermFees WHERE StudentId = @studentId AND TermId = @termId", conn, transaction))
                                {
                                    feeCmd.Parameters.AddWithValue("@studentId", studentId);
                                    feeCmd.Parameters.AddWithValue("@termId", item.TermId);
                                    var result = feeCmd.ExecuteScalar();
                                    if (result != null)
                                    {
                                        expectedFees = Convert.ToDouble(result);
                                    }
                                    else
                                    {
                                        // Get from Students table as fallback
                                        using (var studentFeeCmd = new SQLiteCommand("SELECT ExpectedFees FROM Students WHERE StudentId = @studentId", conn, transaction))
                                        {
                                            studentFeeCmd.Parameters.AddWithValue("@studentId", studentId);
                                            var studentFeeResult = studentFeeCmd.ExecuteScalar();
                                            expectedFees = studentFeeResult != null ? Convert.ToDouble(studentFeeResult) : 0;
                                        }
                                    }
                                }

                                // Create new account record
                                using (var insertCmd = new SQLiteCommand(@"
                                    INSERT INTO TermFeeAccounts (StudentId, TermId, ExpectedFees, TotalPaid, Balance, LastPaymentDate, CreatedDate, UpdatedDate)
                                    VALUES (@studentId, @termId, @expectedFees, @amount, @balance, @date, @createdDate, @updatedDate)", conn, transaction))
                                {
                                    insertCmd.Parameters.AddWithValue("@studentId", studentId);
                                    insertCmd.Parameters.AddWithValue("@termId", item.TermId);
                                    insertCmd.Parameters.AddWithValue("@expectedFees", expectedFees);
                                    insertCmd.Parameters.AddWithValue("@amount", item.Amount);
                                    insertCmd.Parameters.AddWithValue("@balance", expectedFees - item.Amount);
                                    insertCmd.Parameters.AddWithValue("@date", paymentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCmd.Parameters.AddWithValue("@updatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private void UpdateDailySales(DateTime salesDate, double amount)
        {
            try
            {
                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO DailySales (SalesDate, TotalAmount, TransactionCount, GeneratedBy, GeneratedDate)
                    VALUES (@salesDate, @amount, 1, @generatedBy, @generatedDate)
                    ON CONFLICT(SalesDate) DO UPDATE SET
                        TotalAmount = TotalAmount + @amount,
                        TransactionCount = TransactionCount + 1,
                        GeneratedDate = @generatedDate", conn))
                {
                    cmd.Parameters.AddWithValue("@salesDate", salesDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@generatedBy", Environment.UserName);
                    cmd.Parameters.AddWithValue("@generatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Don't fail the entire transaction for sales tracking
                MessageBox.Show($"Warning: Could not update daily sales: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void GenerateMultiTermPaymentReceipt(int studentId, double totalAmount, DateTime paymentDate, string description, List<PaymentDistribution> distribution)
        {
            try
            {
                StringBuilder receipt = new StringBuilder();

                // Get student details
                string studentName = "";
                string studentClass = "";

                using (var conn = SQLiteHelper.GetConnection())
                using (var cmd = new SQLiteCommand(@"
                    SELECT FirstName || ' ' || LastName as StudentName, Class 
                    FROM Students WHERE StudentId = @studentId", conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            studentName = reader["StudentName"].ToString();
                            studentClass = reader["Class"].ToString();
                        }
                    }
                }

                receipt.AppendLine("═══════════════════════════════════════");
                receipt.AppendLine("           GUSHESHE SCHOOL");
                receipt.AppendLine("         FEE PAYMENT RECEIPT");
                receipt.AppendLine("═══════════════════════════════════════");
                receipt.AppendLine();
                receipt.AppendLine($"Receipt Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                receipt.AppendLine($"Receipt No: FEE{DateTime.Now:yyyyMMddHHmmss}");
                receipt.AppendLine();
                receipt.AppendLine("STUDENT INFORMATION:");
                receipt.AppendLine("───────────────────────────────────────");
                receipt.AppendLine($"Name: {studentName}");
                receipt.AppendLine($"Class: {studentClass}");
                receipt.AppendLine();
                receipt.AppendLine("PAYMENT DETAILS:");
                receipt.AppendLine("───────────────────────────────────────");
                receipt.AppendLine($"Payment Date: {paymentDate:yyyy-MM-dd}");
                receipt.AppendLine($"Total Amount: ${totalAmount:F2}");
                receipt.AppendLine($"Description: {description}");
                receipt.AppendLine();
                receipt.AppendLine("PAYMENT DISTRIBUTION:");
                receipt.AppendLine("───────────────────────────────────────");
                foreach (var item in distribution)
                {
                    receipt.AppendLine($"{item.TermName}: ${item.Amount:F2}");
                }
                receipt.AppendLine();

                // Get updated account summary
                using (var conn2 = SQLiteHelper.GetConnection())
                using (var summaryCmd = new SQLiteCommand(@"
                    SELECT 
                        SUM(COALESCE(tfa.ExpectedFees, 0)) as TotalExpected,
                        SUM(COALESCE(tfa.TotalPaid, 0)) as TotalPaid,
                        SUM(COALESCE(tfa.Balance, 0)) as TotalBalance
                    FROM TermFeeAccounts tfa
                    WHERE tfa.StudentId = @studentId", conn2))
                {
                    summaryCmd.Parameters.AddWithValue("@studentId", studentId);
                    using (var summaryReader = summaryCmd.ExecuteReader())
                    {
                        if (summaryReader.Read())
                        {
                            double totalExpected = Convert.ToDouble(summaryReader["TotalExpected"]);
                            double totalPaid = Convert.ToDouble(summaryReader["TotalPaid"]);
                            double totalBalance = Convert.ToDouble(summaryReader["TotalBalance"]);

                            receipt.AppendLine("ACCOUNT SUMMARY:");
                            receipt.AppendLine("───────────────────────────────────────");
                            receipt.AppendLine($"Total Expected Fees: ${totalExpected:F2}");
                            receipt.AppendLine($"Total Amount Paid: ${totalPaid:F2}");
                            receipt.AppendLine($"Outstanding Balance: ${totalBalance:F2}");
                            receipt.AppendLine();

                            if (totalBalance <= 0)
                            {
                                receipt.AppendLine("✓ ALL FEES PAID IN FULL");
                            }
                            else
                            {
                                receipt.AppendLine($"⚠ OUTSTANDING BALANCE: ${totalBalance:F2}");
                            }
                        }
                    }
                }

                receipt.AppendLine();
                receipt.AppendLine("═══════════════════════════════════════");
                receipt.AppendLine("Thank you for your payment!");
                receipt.AppendLine("Keep this receipt for your records.");
                receipt.AppendLine("═══════════════════════════════════════");

                receiptContent = receipt.ToString();

                // Display in RichTextBox if available
                if (this.Controls.Find("rtbReceipt", true).FirstOrDefault() is RichTextBox rtb)
                {
                    rtb.Text = receiptContent;
                    rtb.Font = new Font("Courier New", 10, FontStyle.Regular);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rtbReceipt.Text))
            {
                MessageBox.Show("No receipt to print. Please process a payment first.",
                    "Nothing to Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                PrintDocument printDoc = new PrintDocument();
                printDoc.PrintPage += PrintReceipt_PrintPage;

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

        private void PrintReceipt_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Get the receipt text directly from the RichTextBox
            string receiptContent = rtbReceipt.Text;

            Font printFont = new Font("Courier New", 10);
            Font headerFont = new Font("Courier New", 12, FontStyle.Bold);

            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float yPosition = topMargin;
            float lineHeight = printFont.GetHeight(e.Graphics);

            string[] lines = receiptContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                // Check if there's space for another line
                if (yPosition + lineHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return; // Will trigger another PrintPage event
                }

                // Apply bold font for specific lines
                Font currentFont = (line.Contains("GUSHESHE SCHOOL") || line.Contains("FEE PAYMENT RECEIPT"))
                    ? headerFont : printFont;

                e.Graphics.DrawString(line, currentFont, Brushes.Black, leftMargin, yPosition);
                yPosition += lineHeight;
            }

            e.HasMorePages = false;
        }

        private void GenerateDailySalesReport(DateTime reportDate)
        {
            try
            {
                StringBuilder report = new StringBuilder();

                // Get daily sales summary
                double totalSales = 0;
                int transactionCount = 0;

                using (var conn = SQLiteHelper.GetConnection())
                {
                    // Get fee payments for the date
                    using (var cmd = new SQLiteCommand(@"
                SELECT 
                    fp.PaymentId,
                    s.FirstName || ' ' || s.LastName as StudentName,
                    s.Class,
                    COALESCE(at.TermName, 'Legacy') as TermName,
                    fp.AmountPaid,
                    fp.DatePaid,
                    fp.Description
                FROM FeePayments fp
                JOIN Students s ON fp.StudentId = s.StudentId
                LEFT JOIN AcademicTerms at ON fp.TermId = at.TermId
                WHERE DATE(fp.DatePaid) = @reportDate
                ORDER BY fp.DatePaid, s.FirstName, s.LastName", conn))
                    {
                        cmd.Parameters.AddWithValue("@reportDate", reportDate.ToString("yyyy-MM-dd"));

                        report.AppendLine("═══════════════════════════════════════");
                        report.AppendLine("        Good Shepherd College");
                        report.AppendLine("         DAILY SALES REPORT");
                        report.AppendLine("═══════════════════════════════════════");
                        report.AppendLine();
                        report.AppendLine($"Report Date: {reportDate:yyyy-MM-dd}");
                        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        report.AppendLine($"Generated By: {Environment.UserName}");
                        report.AppendLine();
                        report.AppendLine("FEE PAYMENTS:");
                        report.AppendLine("───────────────────────────────────────");
                        report.AppendLine("Time     | Student Name          | Class | Term      | Amount  | Description");
                        report.AppendLine("─────────┼───────────────────────┼───────┼───────────┼─────────┼─────────────");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Safe conversion with DBNull checks
                                DateTime paymentTime = reader["DatePaid"] != DBNull.Value ?
                                    Convert.ToDateTime(reader["DatePaid"]) : DateTime.MinValue;

                                string studentName = reader["StudentName"]?.ToString() ?? "Unknown";
                                string studentClass = reader["Class"]?.ToString() ?? "";
                                string termName = reader["TermName"]?.ToString() ?? "Legacy";

                                double amount = reader["AmountPaid"] != DBNull.Value ?
                                    Convert.ToDouble(reader["AmountPaid"]) : 0.0;

                                string description = reader["Description"]?.ToString() ?? "";

                                report.AppendLine($"{paymentTime:HH:mm:ss} | {studentName,-21} | {studentClass,-5} | {termName,-9} | ${amount,7:F2} | {description}");

                                totalSales += amount;
                                transactionCount++;
                            }
                        }
                    }

                    // Get uniform sales for the same date if they exist
                    using (var uniformCmd = new SQLiteCommand(@"
                SELECT 
                    ItemName,
                    FirstName || ' ' || LastName as StudentName,
                    Class,
                    Quantity,
                    UnitPrice,
                    Total,
                    DateSold
                FROM UniformSales
                WHERE DATE(DateSold) = @reportDate
                ORDER BY DateSold", conn))
                    {
                        uniformCmd.Parameters.AddWithValue("@reportDate", reportDate.ToString("yyyy-MM-dd"));

                        bool hasUniformSales = false;
                        using (var uniformReader = uniformCmd.ExecuteReader())
                        {
                            while (uniformReader.Read())
                            {
                                if (!hasUniformSales)
                                {
                                    report.AppendLine();
                                    report.AppendLine("UNIFORM SALES:");
                                    report.AppendLine("───────────────────────────────────────");
                                    report.AppendLine("Time     | Student Name          | Class | Item             | Qty | Unit Price | Total");
                                    report.AppendLine("─────────┼───────────────────────┼───────┼──────────────────┼─────┼────────────┼──────");
                                    hasUniformSales = true;
                                }

                                // Safe conversion with DBNull checks
                                DateTime saleTime = uniformReader["DateSold"] != DBNull.Value ?
                                    Convert.ToDateTime(uniformReader["DateSold"]) : DateTime.MinValue;

                                string studentName = uniformReader["StudentName"]?.ToString() ?? "Unknown";
                                string studentClass = uniformReader["Class"]?.ToString() ?? "";
                                string itemName = uniformReader["ItemName"]?.ToString() ?? "";

                                int quantity = uniformReader["Quantity"] != DBNull.Value ?
                                    Convert.ToInt32(uniformReader["Quantity"]) : 0;

                                double unitPrice = uniformReader["UnitPrice"] != DBNull.Value ?
                                    Convert.ToDouble(uniformReader["UnitPrice"]) : 0.0;

                                double total = uniformReader["Total"] != DBNull.Value ?
                                    Convert.ToDouble(uniformReader["Total"]) : 0.0;

                                report.AppendLine($"{saleTime:HH:mm:ss} | {studentName,-21} | {studentClass,-5} | {itemName,-16} | {quantity,3} | ${unitPrice,8:F2} | ${total,6:F2}");

                                totalSales += total;
                                transactionCount++;
                            }
                        }
                    }

                    if (transactionCount == 0)
                    {
                        report.AppendLine("No transactions recorded for this date.");
                    }

                    report.AppendLine();
                    report.AppendLine("SUMMARY:");
                    report.AppendLine("───────────────────────────────────────");
                    report.AppendLine($"Total Transactions: {transactionCount}");
                    report.AppendLine($"Total Sales Amount: ${totalSales:F2}");
                    report.AppendLine();
                    report.AppendLine("BREAKDOWN BY CATEGORY:");
                    report.AppendLine("───────────────────────────────────────");

                    // Fee payments breakdown
                    using (var feeBreakdownCmd = new SQLiteCommand(@"
                SELECT 
                    COALESCE(at.TermName, 'Legacy Payments') as Category,
                    COUNT(*) as Count,
                    SUM(fp.AmountPaid) as Total
                FROM FeePayments fp
                LEFT JOIN AcademicTerms at ON fp.TermId = at.TermId
                WHERE DATE(fp.DatePaid) = @reportDate
                GROUP BY at.TermName
                ORDER BY Total DESC", conn))
                    {
                        feeBreakdownCmd.Parameters.AddWithValue("@reportDate", reportDate.ToString("yyyy-MM-dd"));
                        using (var breakdownReader = feeBreakdownCmd.ExecuteReader())
                        {
                            while (breakdownReader.Read())
                            {
                                string category = breakdownReader["Category"]?.ToString() ?? "Unknown";
                                int count = breakdownReader["Count"] != DBNull.Value ?
                                    Convert.ToInt32(breakdownReader["Count"]) : 0;
                                double total = breakdownReader["Total"] != DBNull.Value ?
                                    Convert.ToDouble(breakdownReader["Total"]) : 0.0;

                                report.AppendLine($"{category}: {count} transactions, ${total:F2}");
                            }
                        }
                    }

                    // Uniform sales breakdown
                    using (var uniformBreakdownCmd = new SQLiteCommand(@"
                SELECT 
                    COUNT(*) as Count,
                    SUM(Total) as Total
                FROM UniformSales
                WHERE DATE(DateSold) = @reportDate", conn))
                    {
                        uniformBreakdownCmd.Parameters.AddWithValue("@reportDate", reportDate.ToString("yyyy-MM-dd"));
                        using (var uniformBreakdownReader = uniformBreakdownCmd.ExecuteReader())
                        {
                            if (uniformBreakdownReader.Read())
                            {
                                int count = uniformBreakdownReader["Count"] != DBNull.Value ?
                                    Convert.ToInt32(uniformBreakdownReader["Count"]) : 0;
                                double total = uniformBreakdownReader["Total"] != DBNull.Value ?
                                    Convert.ToDouble(uniformBreakdownReader["Total"]) : 0.0;

                                if (count > 0)
                                {
                                    report.AppendLine($"Uniform Sales: {count} transactions, ${total:F2}");
                                }
                            }
                        }
                    }

                    report.AppendLine();
                    report.AppendLine("═══════════════════════════════════════");
                    report.AppendLine("End of Report");
                    report.AppendLine("═══════════════════════════════════════");

                    // Update/insert daily sales record
                    using (var updateSalesCmd = new SQLiteCommand(@"
                INSERT OR REPLACE INTO DailySales (SalesDate, TotalAmount, TransactionCount, GeneratedBy, GeneratedDate)
                VALUES (@salesDate, @totalAmount, @transactionCount, @generatedBy, @generatedDate)", conn))
                    {
                        updateSalesCmd.Parameters.AddWithValue("@salesDate", reportDate.ToString("yyyy-MM-dd"));
                        updateSalesCmd.Parameters.AddWithValue("@totalAmount", totalSales);
                        updateSalesCmd.Parameters.AddWithValue("@transactionCount", transactionCount);
                        updateSalesCmd.Parameters.AddWithValue("@generatedBy", Environment.UserName);
                        updateSalesCmd.Parameters.AddWithValue("@generatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        updateSalesCmd.ExecuteNonQuery();
                    }
                }

                // Display report
                ShowDailySalesReport(report.ToString(), reportDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating daily sales report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowDailySalesReport(string reportContent, DateTime reportDate)
        {
            // Create and show report form
            var reportForm = new Form
            {
                Text = $"Daily Sales Report - {reportDate:yyyy-MM-dd}",
                Size = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent
            };

            var rtbReport = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Text = reportContent,
                Font = new Font("Courier New", 10),
                ReadOnly = true,
                BackColor = Color.White
            };

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnPrint = new Button
            {
                Text = "Print Report",
                Size = new Size(100, 30),
                Location = new Point(10, 10)
            };
            var btnSave = new Button
            {
                Text = "Save to File",
                Size = new Size(100, 30),
                Location = new Point(120, 10)
            };

            btnPrint.Click += (s, e) => PrintDailySalesReport(reportContent);
            btnSave.Click += (s, e) => SaveDailySalesReport(reportContent, reportDate);

            panel.Controls.AddRange(new Control[] { btnPrint, btnSave });
            reportForm.Controls.AddRange(new Control[] { rtbReport, panel });

            reportForm.ShowDialog();
        }

        private void PrintDailySalesReport(string reportContent)
        {
            try
            {
                PrintDocument printDoc = new PrintDocument();
                printDoc.PrintPage += (sender, e) => PrintReport_PrintPage(sender, e, reportContent);

                PrintDialog printDialog = new PrintDialog { Document = printDoc };
                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDoc.Print();
                    MessageBox.Show("Report printed successfully!", "Print Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Printing error: {ex.Message}", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintReport_PrintPage(object sender, PrintPageEventArgs e, string content)
        {
            Font printFont = new Font("Courier New", 9);
            Font headerFont = new Font("Courier New", 11, FontStyle.Bold);

            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float yPosition = topMargin;
            float lineHeight = printFont.GetHeight(e.Graphics);

            string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (yPosition + lineHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                Font currentFont = (line.Contains("GUSHESHE SCHOOL") || line.Contains("DAILY SALES REPORT"))
                    ? headerFont : printFont;

                e.Graphics.DrawString(line, currentFont, Brushes.Black, leftMargin, yPosition);
                yPosition += lineHeight;
            }

            e.HasMorePages = false;
        }

        private void SaveDailySalesReport(string reportContent, DateTime reportDate)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"Daily_Sales_Report_{reportDate:yyyy-MM-dd}.txt",
                    DefaultExt = "txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, reportContent);
                    MessageBox.Show($"Report saved to: {saveDialog.FileName}", "Save Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save error: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearPaymentForm();
            receiptContent = "";
            if (this.Controls.Find("rtbReceipt", true).FirstOrDefault() is RichTextBox rtb)
            {
                rtb.Clear();
            }
        }

       

        // Helper Classes
        public class ComboBoxItem
        {
            public int Value { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        public class TermInfo
        {
            public int TermId { get; set; }
            public string TermName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public bool IsActive { get; set; }
        }

        public class PaymentDistribution
        {
            public int TermId { get; set; }
            public string TermName { get; set; }
            public double Amount { get; set; }
        }

        private void btnRefresh_Click_1(object sender, EventArgs e)
        {
            RefreshWithCurrentFilter();
            LoadTermBasedFeeAccounts();
            LoadPaymentHistory();
            if (selectedStudentId > 0)
            {
                LoadStudentMultiTermDetails(selectedStudentId);
            }
        }

        private void btnGenerateDailySales_Click_1(object sender, EventArgs e)
        {
            var dateForm = new DateSelectionForm();
            if (dateForm.ShowDialog() == DialogResult.OK)
            {
                GenerateDailySalesReport(dateForm.SelectedDate);
            }
        }
    }

    // Simple Date Selection Form for Reports
    public partial class DateSelectionForm : Form
    {
        public DateTime SelectedDate { get; private set; }

        public DateSelectionForm()
        {
            InitializeComponent();
            SelectedDate = DateTime.Now.Date;
        }

        private void InitializeComponent()
        {
            this.Text = "Select Report Date";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblDate = new Label
            {
                Text = "Select Date:",
                Location = new Point(20, 20),
                Size = new Size(80, 23)
            };

            var dtpDate = new DateTimePicker
            {
                Location = new Point(110, 20),
                Size = new Size(150, 23),
                Value = DateTime.Now.Date
            };

            var btnOK = new Button
            {
                Text = "OK",
                Location = new Point(110, 60),
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(190, 60),
                Size = new Size(75, 23),
                DialogResult = DialogResult.Cancel
            };

            btnOK.Click += (s, e) => { SelectedDate = dtpDate.Value.Date; };

            this.Controls.AddRange(new Control[] { lblDate, dtpDate, btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}
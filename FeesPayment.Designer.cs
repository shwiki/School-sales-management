namespace Gusheshe
{
    partial class FeesPayment
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cmbStudent = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nudPaymentAmount = new System.Windows.Forms.NumericUpDown();
            this.dtpPaymentDate = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.rtbReceipt = new System.Windows.Forms.RichTextBox();
            this.btnMakePayment = new System.Windows.Forms.Button();
            this.btnPrintReceipt = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lblExpectedFees = new System.Windows.Forms.Label();
            this.lblTotalPaid = new System.Windows.Forms.Label();
            this.lblArrears = new System.Windows.Forms.Label();
            this.lblLastPayment = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cmbTerm = new System.Windows.Forms.ComboBox();
            this.btnGenerateDailySales = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbClassFilter = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dgvFeeAccounts = new System.Windows.Forms.DataGridView();
            this.StudentID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Class = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.currentterm = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalPaid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Arrears = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastPayment = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OverallBalance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.l = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dgvPaymentHistory = new System.Windows.Forms.DataGridView();
            this.PaymentId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentsID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AmountPaid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DatePaid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.nudPaymentAmount)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFeeAccounts)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPaymentHistory)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbStudent
            // 
            this.cmbStudent.FormattingEnabled = true;
            this.cmbStudent.Location = new System.Drawing.Point(331, 64);
            this.cmbStudent.Name = "cmbStudent";
            this.cmbStudent.Size = new System.Drawing.Size(121, 21);
            this.cmbStudent.TabIndex = 0;
            this.cmbStudent.SelectedIndexChanged += new System.EventHandler(this.cmbStudent_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(133, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Student Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(133, 104);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Payment Amount";
            // 
            // nudPaymentAmount
            // 
            this.nudPaymentAmount.Location = new System.Drawing.Point(331, 100);
            this.nudPaymentAmount.Maximum = new decimal(new int[] {
            400,
            0,
            0,
            0});
            this.nudPaymentAmount.Name = "nudPaymentAmount";
            this.nudPaymentAmount.Size = new System.Drawing.Size(120, 20);
            this.nudPaymentAmount.TabIndex = 3;
            // 
            // dtpPaymentDate
            // 
            this.dtpPaymentDate.Location = new System.Drawing.Point(331, 163);
            this.dtpPaymentDate.Name = "dtpPaymentDate";
            this.dtpPaymentDate.Size = new System.Drawing.Size(200, 20);
            this.dtpPaymentDate.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(133, 158);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Date Paid";
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(331, 199);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(100, 20);
            this.txtDescription.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(133, 201);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Description";
            // 
            // rtbReceipt
            // 
            this.rtbReceipt.Location = new System.Drawing.Point(866, 12);
            this.rtbReceipt.Name = "rtbReceipt";
            this.rtbReceipt.Size = new System.Drawing.Size(360, 269);
            this.rtbReceipt.TabIndex = 10;
            this.rtbReceipt.Text = "";
            // 
            // btnMakePayment
            // 
            this.btnMakePayment.Location = new System.Drawing.Point(144, 689);
            this.btnMakePayment.Name = "btnMakePayment";
            this.btnMakePayment.Size = new System.Drawing.Size(104, 23);
            this.btnMakePayment.TabIndex = 11;
            this.btnMakePayment.Text = "Make Payment";
            this.btnMakePayment.UseVisualStyleBackColor = true;
            this.btnMakePayment.Click += new System.EventHandler(this.btnMakePayment_Click);
            // 
            // btnPrintReceipt
            // 
            this.btnPrintReceipt.Location = new System.Drawing.Point(302, 689);
            this.btnPrintReceipt.Name = "btnPrintReceipt";
            this.btnPrintReceipt.Size = new System.Drawing.Size(109, 23);
            this.btnPrintReceipt.TabIndex = 12;
            this.btnPrintReceipt.Text = "Print Receipt";
            this.btnPrintReceipt.UseVisualStyleBackColor = true;
            this.btnPrintReceipt.Click += new System.EventHandler(this.btnPrintReceipt_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(518, 689);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 13;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(651, 689);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 14;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click_1);
            // 
            // lblExpectedFees
            // 
            this.lblExpectedFees.AutoSize = true;
            this.lblExpectedFees.Location = new System.Drawing.Point(713, 80);
            this.lblExpectedFees.Name = "lblExpectedFees";
            this.lblExpectedFees.Size = new System.Drawing.Size(13, 13);
            this.lblExpectedFees.TabIndex = 19;
            this.lblExpectedFees.Text = "0";
            // 
            // lblTotalPaid
            // 
            this.lblTotalPaid.AutoSize = true;
            this.lblTotalPaid.Location = new System.Drawing.Point(713, 107);
            this.lblTotalPaid.Name = "lblTotalPaid";
            this.lblTotalPaid.Size = new System.Drawing.Size(13, 13);
            this.lblTotalPaid.TabIndex = 20;
            this.lblTotalPaid.Text = "0";
            // 
            // lblArrears
            // 
            this.lblArrears.AutoSize = true;
            this.lblArrears.Location = new System.Drawing.Point(713, 133);
            this.lblArrears.Name = "lblArrears";
            this.lblArrears.Size = new System.Drawing.Size(13, 13);
            this.lblArrears.TabIndex = 21;
            this.lblArrears.Text = "0";
            // 
            // lblLastPayment
            // 
            this.lblLastPayment.AutoSize = true;
            this.lblLastPayment.Location = new System.Drawing.Point(713, 158);
            this.lblLastPayment.Name = "lblLastPayment";
            this.lblLastPayment.Size = new System.Drawing.Size(13, 13);
            this.lblLastPayment.TabIndex = 22;
            this.lblLastPayment.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(133, 132);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(31, 13);
            this.label5.TabIndex = 23;
            this.label5.Text = "Term";
            // 
            // cmbTerm
            // 
            this.cmbTerm.FormattingEnabled = true;
            this.cmbTerm.Location = new System.Drawing.Point(331, 137);
            this.cmbTerm.Name = "cmbTerm";
            this.cmbTerm.Size = new System.Drawing.Size(121, 21);
            this.cmbTerm.TabIndex = 24;
            // 
            // btnGenerateDailySales
            // 
            this.btnGenerateDailySales.Location = new System.Drawing.Point(785, 689);
            this.btnGenerateDailySales.Name = "btnGenerateDailySales";
            this.btnGenerateDailySales.Size = new System.Drawing.Size(109, 23);
            this.btnGenerateDailySales.TabIndex = 25;
            this.btnGenerateDailySales.Text = "Generate Report";
            this.btnGenerateDailySales.UseVisualStyleBackColor = true;
            this.btnGenerateDailySales.Click += new System.EventHandler(this.btnGenerateDailySales_Click_1);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(133, 24);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(99, 13);
            this.label6.TabIndex = 26;
            this.label6.Text = "Filter by class name";
            // 
            // cmbClassFilter
            // 
            this.cmbClassFilter.FormattingEnabled = true;
            this.cmbClassFilter.Location = new System.Drawing.Point(238, 24);
            this.cmbClassFilter.Name = "cmbClassFilter";
            this.cmbClassFilter.Size = new System.Drawing.Size(121, 21);
            this.cmbClassFilter.TabIndex = 27;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dgvFeeAccounts);
            this.groupBox1.Location = new System.Drawing.Point(12, 253);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(823, 415);
            this.groupBox1.TabIndex = 28;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Complete Student Fee information";
            // 
            // dgvFeeAccounts
            // 
            this.dgvFeeAccounts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFeeAccounts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.StudentID,
            this.StudentName,
            this.Class,
            this.currentterm,
            this.TotalPaid,
            this.Arrears,
            this.LastPayment,
            this.StudID,
            this.Column2,
            this.OverallBalance,
            this.l});
            this.dgvFeeAccounts.Location = new System.Drawing.Point(17, 19);
            this.dgvFeeAccounts.Name = "dgvFeeAccounts";
            this.dgvFeeAccounts.Size = new System.Drawing.Size(800, 389);
            this.dgvFeeAccounts.TabIndex = 9;
            // 
            // StudentID
            // 
            this.StudentID.HeaderText = "StudentID";
            this.StudentID.Name = "StudentID";
            // 
            // StudentName
            // 
            this.StudentName.HeaderText = "Student Name";
            this.StudentName.Name = "StudentName";
            // 
            // Class
            // 
            this.Class.HeaderText = "Class";
            this.Class.Name = "Class";
            // 
            // currentterm
            // 
            this.currentterm.HeaderText = "Current Term";
            this.currentterm.Name = "currentterm";
            // 
            // TotalPaid
            // 
            this.TotalPaid.HeaderText = "Fee/term";
            this.TotalPaid.Name = "TotalPaid";
            // 
            // Arrears
            // 
            this.Arrears.HeaderText = "Paid amount";
            this.Arrears.Name = "Arrears";
            // 
            // LastPayment
            // 
            this.LastPayment.HeaderText = "Term balance";
            this.LastPayment.Name = "LastPayment";
            // 
            // StudID
            // 
            this.StudID.HeaderText = "Total Paid";
            this.StudID.Name = "StudID";
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Total expected";
            this.Column2.Name = "Column2";
            // 
            // OverallBalance
            // 
            this.OverallBalance.HeaderText = "Overall balance";
            this.OverallBalance.Name = "OverallBalance";
            // 
            // l
            // 
            this.l.HeaderText = "Last payment ";
            this.l.Name = "l";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dgvPaymentHistory);
            this.groupBox2.Location = new System.Drawing.Point(841, 304);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(471, 364);
            this.groupBox2.TabIndex = 29;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Transaction details";
            // 
            // dgvPaymentHistory
            // 
            this.dgvPaymentHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPaymentHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PaymentId,
            this.StudentsID,
            this.Name,
            this.AmountPaid,
            this.DatePaid,
            this.Description});
            this.dgvPaymentHistory.Location = new System.Drawing.Point(6, 19);
            this.dgvPaymentHistory.Name = "dgvPaymentHistory";
            this.dgvPaymentHistory.Size = new System.Drawing.Size(459, 338);
            this.dgvPaymentHistory.TabIndex = 10;
            // 
            // PaymentId
            // 
            this.PaymentId.HeaderText = "Payment Id";
            this.PaymentId.Name = "PaymentId";
            this.PaymentId.Visible = false;
            // 
            // StudentsID
            // 
            this.StudentsID.HeaderText = "StudentID";
            this.StudentsID.Name = "StudentsID";
            this.StudentsID.Visible = false;
            // 
            // Name
            // 
            this.Name.HeaderText = "Student Name";
            this.Name.Name = "Name";
            // 
            // AmountPaid
            // 
            this.AmountPaid.HeaderText = "Amount Paid";
            this.AmountPaid.Name = "AmountPaid";
            // 
            // DatePaid
            // 
            this.DatePaid.HeaderText = "Date Paid";
            this.DatePaid.Name = "DatePaid";
            // 
            // Description
            // 
            this.Description.HeaderText = "Description";
            this.Description.Name = "Description";
            // 
            // FeesPayment
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1370, 749);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cmbClassFilter);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.btnGenerateDailySales);
            this.Controls.Add(this.cmbTerm);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblLastPayment);
            this.Controls.Add(this.lblArrears);
            this.Controls.Add(this.lblTotalPaid);
            this.Controls.Add(this.lblExpectedFees);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnPrintReceipt);
            this.Controls.Add(this.btnMakePayment);
            this.Controls.Add(this.rtbReceipt);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtDescription);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dtpPaymentDate);
            this.Controls.Add(this.nudPaymentAmount);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbStudent);
            this.Text = "FeesPayment";
            this.Load += new System.EventHandler(this.FeesPayment_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudPaymentAmount)).EndInit();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFeeAccounts)).EndInit();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPaymentHistory)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbStudent;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudPaymentAmount;
        private System.Windows.Forms.DateTimePicker dtpPaymentDate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RichTextBox rtbReceipt;
        private System.Windows.Forms.Button btnMakePayment;
        private System.Windows.Forms.Button btnPrintReceipt;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Label lblExpectedFees;
        private System.Windows.Forms.Label lblTotalPaid;
        private System.Windows.Forms.Label lblArrears;
        private System.Windows.Forms.Label lblLastPayment;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cmbTerm;
        private System.Windows.Forms.Button btnGenerateDailySales;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbClassFilter;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView dgvFeeAccounts;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentID;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn Class;
        private System.Windows.Forms.DataGridViewTextBoxColumn currentterm;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalPaid;
        private System.Windows.Forms.DataGridViewTextBoxColumn Arrears;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastPayment;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn OverallBalance;
        private System.Windows.Forms.DataGridViewTextBoxColumn l;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.DataGridView dgvPaymentHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn PaymentId;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentsID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn AmountPaid;
        private System.Windows.Forms.DataGridViewTextBoxColumn DatePaid;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
    }
}
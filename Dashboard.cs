using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gusheshe
{
    public partial class Dashboard : Form
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        private void uniformbtn_Click(object sender, EventArgs e)
        {
            this.Hide(); // Hide the main dashboard

            var uniformSalesForm = new UniformSales();
            uniformSalesForm.FormClosed += (s, args) => this.Show(); // Re-show dashboard when UniformSalesForm is closed
            uniformSalesForm.Show();
        }

        private void feebtn_Click(object sender, EventArgs e)
        {
            this.Hide(); // Hide the main dashboard

            var feespaymentForm = new FeesPayment();
            feespaymentForm.FormClosed += (s, args) => this.Show(); // Re-show dashboard when UniformSalesForm is closed
            feespaymentForm.Show();
        }

        private void recordsbtn_Click(object sender, EventArgs e)
        {
            this.Hide(); // Hide the main dashboard

            var studentRecordsForm = new StudentRecords();
            studentRecordsForm.FormClosed += (s, args) => this.Show(); // Re-show dashboard when UniformSalesForm is closed
            studentRecordsForm.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
            "Are you sure you want to exit the application?",
            "Exit Confirmation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Application.Exit(); // closes entire app
            }
        }

        private void btnStockManagement_Click(object sender, EventArgs e)
        {
            this.Hide(); // Hide the main dashboard

            var stockManagement = new StockManagementForm();
            stockManagement.FormClosed += (s, args) => this.Show(); // Re-show dashboard when UniformSalesForm is closed
            stockManagement.Show();
        }

        private void syncbtn_Click(object sender, EventArgs e)
        {

        }

        private void Dashboard_Load(object sender, EventArgs e)
        {

        }
    }
}

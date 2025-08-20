using Gusheshe.Data;
using Gusheshe.Models;
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

namespace Gusheshe
{
    public partial class UniformSales : Form
    {
        // DataTables for each SQL table
        private string lastReceiptText;  // store from ShowReceipt
        private DataTable dtItems = new DataTable();     // UniformItems
        private DataTable dtStudents = new DataTable();     // Students
        private DataTable dtSales = new DataTable();     // UniformSales

        // DataAdapters to fill + update
        private SQLiteDataAdapter daItems;
        private SQLiteDataAdapter daStudents;
        private SQLiteDataAdapter daSales;

        // BindingSources to simplify data-binding
        private BindingSource bsItems = new BindingSource();
        private BindingSource bsStudents = new BindingSource();
        private BindingSource bsSales = new BindingSource();

        public UniformSales()
        {
            InitializeComponent();
            
        }

        private void UniformSales_Load(object sender, EventArgs e)
        {
            // 1) ITEMS
            DataTable dtItems = new DataTable();
            using (SQLiteConnection conn = SQLiteHelper.GetConnection())
            using (SQLiteDataAdapter daItems = new SQLiteDataAdapter(
                   "SELECT ItemName, Price FROM UniformItems", conn))
            {
                daItems.Fill(dtItems);
            }

            cbItemName.DataSource = dtItems;
            cbItemName.DisplayMember = "ItemName";
            cbItemName.ValueMember = "Price";      // so SelectedValue is the unit-price

            // 2) STUDENTS (assuming your Students table has columns StudentName, Class)
            DataTable dtStudents = new DataTable();
            using (SQLiteConnection conn = SQLiteHelper.GetConnection())
            using (SQLiteDataAdapter daStudents = new SQLiteDataAdapter(
                   "SELECT FirstName, LastName, Class FROM Students", conn))
            {
                daStudents.Fill(dtStudents);
            }

            cbFirstName.DataSource = dtStudents;
            cbFirstName.DisplayMember = "FirstName";
            cbFirstName.ValueMember = "FirstName";

            cbLastName.DataSource = dtStudents;
            cbLastName.DisplayMember = "LastName";
            cbLastName.ValueMember = "LastName";

            cbClass.DataSource = dtStudents;
            cbClass.DisplayMember = "Class";
            cbClass.ValueMember = "Class";

            // initialize controls
            numQty.Minimum = 1;
            numQty.Value = 1;
            txtUnitPrice.Text = "0.00";
            txtTotal.Text = "0.00";

            cbItemName.SelectedIndexChanged += cbItemName_SelectedIndexChanged;
            numQty.ValueChanged += (s, e2) => RecalcTotal();
            cbLastName.SelectedIndexChanged += cbStudentName_SelectedIndexChanged;
            cbFirstName.SelectedIndexChanged += cbStudentName_SelectedIndexChanged;
        }

        private void cbItemName_SelectedIndexChanged(object sender, EventArgs e)
        {
            // the combo’s ValueMember is "Price"
            decimal price;
            Decimal.TryParse(cbItemName.SelectedValue.ToString(), out price);
            txtUnitPrice.Text = price.ToString("F2");
            RecalcTotal();
        }
        private void RecalcTotal()
        {
            decimal price;
            Decimal.TryParse(txtUnitPrice.Text, out price);
            int qty = (int)numQty.Value;
            txtTotal.Text = (price * qty).ToString("F2");
        }

        private void btnAddtoCart_Click(object sender, EventArgs e)
        {
            // 1. Validate
            if (cbItemName.SelectedItem == null ||
                cbLastName.SelectedItem == null)
            {
                MessageBox.Show(
                  "Please select both an item and a student.",
                  "Missing Data",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Warning);
                return;
            }

            // 2. Grab values
            string item = cbItemName.Text;
            decimal price = 0m;
            decimal.TryParse(txtUnitPrice.Text, out price);
            int qty = (int)numQty.Value;
            decimal total = price * qty;
            string firstName = cbFirstName.Text;
            string lastName = cbLastName.Text;
            string @class = cbClass.Text;

            // 3. Add to grid
            dgvCart.Rows.Add(
              item,
              qty,
              price.ToString("F2"),
              total.ToString("F2"),
              firstName,
              lastName,
              @class);

            // 4. Feedback
            statusLabel.Text = $"{qty}× {item} added for {firstName} {lastName}.";
        
        }

        private void cbStudentName_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnDeleteItem_Click(object sender, EventArgs e)
        {
            if (dgvCart.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                  "Select a row in the cart first.",
                  "Nothing Selected",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information);
                return;
            }

            dgvCart.Rows.RemoveAt(dgvCart.SelectedRows[0].Index);
            statusLabel.Text = "Item removed from cart.";
        }

        private void btnCompleteSale_Click(object sender, EventArgs e)
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show(
                  "Your cart is empty.",
                  "Nothing to Save",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information);
                return;
            }

            using (var conn = SQLiteHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = @"
          INSERT INTO UniformSales
            (ItemName, Quantity, UnitPrice, Total, FirstName, LastName, Class, DateSold)
          VALUES
            (@i,@q,@u,@t,@f,@l,@c,@d)";

                foreach (DataGridViewRow row in dgvCart.Rows)
                {
                    if (row.IsNewRow) continue;

                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@i", row.Cells[0].Value);
                    cmd.Parameters.AddWithValue("@q", row.Cells[1].Value);
                    cmd.Parameters.AddWithValue("@u", row.Cells[2].Value);
                    cmd.Parameters.AddWithValue("@t", row.Cells[3].Value);
                    cmd.Parameters.AddWithValue("@f", row.Cells[4].Value);
                    cmd.Parameters.AddWithValue("@l", row.Cells[5].Value);
                    cmd.Parameters.AddWithValue("@c", row.Cells[6].Value);
                    cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));

                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
            }

            MessageBox.Show(
              "Sale recorded successfully!",
              "Success",
              MessageBoxButtons.OK,
              MessageBoxIcon.Information);

            dgvCart.Rows.Clear();
            statusLabel.Text = "Cart cleared.";
        }

        private void btnShowReceipt_Click(object sender, EventArgs e)
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("Cart is empty.", "No Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("---- Uniform Sale Receipt ----\n");
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.IsNewRow) continue;
                sb.AppendFormat("{0} x{1} @ {2} = {3}\n",
                    row.Cells[0].Value,
                    row.Cells[1].Value,
                    row.Cells[2].Value,
                    row.Cells[3].Value);
            }
            sb.AppendLine($"\nDate: {DateTime.Now:yyyy-MM-dd}");

            // show in a scrollable dialog
            MessageBox.Show(
              sb.ToString(),
              "Receipt Preview",
              MessageBoxButtons.OK,
              MessageBoxIcon.Information);
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(lastReceiptText)) btnShowReceipt_Click(sender, e);
            if (string.IsNullOrEmpty(lastReceiptText)) return;

            var pd = new System.Drawing.Printing.PrintDocument();
            pd.PrintPage += (s, ev) =>
            {
                ev.Graphics.DrawString(
                  lastReceiptText,
                  new System.Drawing.Font("Consolas", 10),
                  System.Drawing.Brushes.Black,
                  ev.MarginBounds.Left,
                  ev.MarginBounds.Top);
            };

            var dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
                pd.Print();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

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

namespace Gusheshe
{
    public partial class StockManagementForm : Form
    {
        public StockManagementForm()
        {
            InitializeComponent();
        }

        private void StockManagementForm_Load(object sender, EventArgs e)
        {
            LoadItems();
        }

        private void LoadItems()
        {
            dvgStockItems.Rows.Clear();
            using (var conn = SQLiteHelper.GetConnection())
            using (var cmd = new SQLiteCommand("SELECT ItemId, ItemName, Price, Quantity FROM UniformItems", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    dvgStockItems.Rows.Add(
                        rdr.GetInt32(0),      // ID
                        rdr.GetString(1),     // ItemName
                        rdr.GetDouble(2),     // Price
                        rdr.GetInt32(3)       // Quantity
                    );
                }
            }
            // disable update/delete until an item is selected
            btnUpdateItem.Enabled = btnDeleteItem.Enabled = false;
        }

        private void btnAddItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                MessageBox.Show("Enter an item name.");
                return;
            }

            using (var conn = SQLiteHelper.GetConnection())
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = @"
                INSERT INTO UniformItems (ItemName, Price, Quantity)
                VALUES (@name, @price, @qty)";
                cmd.Parameters.AddWithValue("@name", txtItemName.Text.Trim());
                cmd.Parameters.AddWithValue("@price", (double)numPrice.Value);
                cmd.Parameters.AddWithValue("@qty", (int)numQuantity.Value);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Item added.");
            ClearInputs();
            LoadItems();
        }

        private void ClearInputs()
        {
            txtItemName.Clear();
            numPrice.Value = 0;
            numQuantity.Value = 0;
            btnUpdateItem.Enabled = btnDeleteItem.Enabled = false;
        }

        private void btnUpdateItem_Click(object sender, EventArgs e)
        {
            var row = dvgStockItems.SelectedRows[0];
            int id = Convert.ToInt32(row.Cells[0].Value);

            using (var conn = SQLiteHelper.GetConnection())
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = @"
                UPDATE UniformItems
                SET ItemName = @name, Price = @price, Quantity = @qty
                WHERE ItemId = @id";
                cmd.Parameters.AddWithValue("@name", txtItemName.Text.Trim());
                cmd.Parameters.AddWithValue("@price", (double)numPrice.Value);
                cmd.Parameters.AddWithValue("@qty", (int)numQuantity.Value);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Item updated.");
            ClearInputs();
            LoadItems();
        }

        private void btnDeleteItem_Click(object sender, EventArgs e)
        {
            var row = dvgStockItems.SelectedRows[0];
            int id = Convert.ToInt32(row.Cells[0].Value);

            var dr = MessageBox.Show(
                "Delete this item?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr != DialogResult.Yes) return;

            using (var conn = SQLiteHelper.GetConnection())
            using (var cmd = new SQLiteCommand("DELETE FROM UniformItems WHERE ItemId = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Item deleted.");
            ClearInputs();
            LoadItems();
        }

        private void dgvStockItems_SelectionChanged(object sender, EventArgs e)
        {
            // If you have full-row selection on, SelectedRows[0] is the current row
            if (dvgStockItems.SelectedRows.Count > 0)
            {
                var row = dvgStockItems.SelectedRows[0];
                txtItemName.Text = row.Cells["ItemName"].Value.ToString();
                numPrice.Value = Convert.ToDecimal(row.Cells["Price"].Value);
                numQuantity.Value = Convert.ToDecimal(row.Cells["Quantity"].Value);

                btnUpdateItem.Enabled = true;
                btnDeleteItem.Enabled = true;
            }
            else
            {
                // no row selected
                btnUpdateItem.Enabled = false;
                btnDeleteItem.Enabled = false;
                ClearInputs();
            }
        }
    }
}

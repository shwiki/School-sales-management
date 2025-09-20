using Gusheshe.Data;
using Gusheshe.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gusheshe
{
    public partial class UniformSales : Form
    {
        // Calculator state variables
        private double currentValue = 0;
        private double previousValue = 0;
        private string operation = "";
        private bool operationPressed = false;
        private bool equalsPressed = false;

        // Receipt and stock management
        private string lastReceiptText;
        private DataTable dtItems = new DataTable();
        private DataTable dtStudents = new DataTable();
        private DataTable dtSales = new DataTable();

        // Memory value for calculator
        private double memoryValue = 0;

        // DataAdapters and BindingSources
        private SQLiteDataAdapter daItems;
        private SQLiteDataAdapter daStudents;
        private SQLiteDataAdapter daSales;
        private BindingSource bsItems = new BindingSource();
        private BindingSource bsStudents = new BindingSource();
        private BindingSource bsSales = new BindingSource();

        public UniformSales()
        {
            InitializeComponent();
            
        }

        private void UniformSales_Load(object sender, EventArgs e)
        {
            LoadItemsWithStockCheck();
            LoadStudents();
            InitializeControls();
            CheckLowStockItems();
        }
        private void LoadItemsWithStockCheck()
        {
            dtItems = new DataTable();
            using (SQLiteConnection conn = SQLiteHelper.GetConnection())
            using (SQLiteDataAdapter daItems = new SQLiteDataAdapter(
                   "SELECT ItemName, Price, Quantity FROM UniformItems ORDER BY ItemName", conn))
            {
                daItems.Fill(dtItems);
            }

            cbItemName.DataSource = dtItems;
            cbItemName.DisplayMember = "ItemName";
            cbItemName.ValueMember = "Price";
        }
        private void LoadStudents()
        {
            DataTable dtStudents = new DataTable();
            using (SQLiteConnection conn = SQLiteHelper.GetConnection())
            using (SQLiteDataAdapter daStudents = new SQLiteDataAdapter(
                   "SELECT DISTINCT FirstName, LastName, Class FROM Students ORDER BY FirstName", conn))
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
        }

        private void InitializeControls()
        {
            numQty.Minimum = 1;
            numQty.Value = 1;
            txtUnitPrice.Text = "0.00";
            txtTotal.Text = "0.00";

            cbItemName.SelectedIndexChanged += cbItemName_SelectedIndexChanged;
            numQty.ValueChanged += (s, e2) => RecalcTotal();
            cbLastName.SelectedIndexChanged += cbStudentName_SelectedIndexChanged;
            cbFirstName.SelectedIndexChanged += cbStudentName_SelectedIndexChanged;
        
        }
        private void CheckLowStockItems()
        {
            try
            {
                var lowStockItems = new List<string>();
                var outOfStockItems = new List<string>();

                using (SQLiteConnection conn = SQLiteHelper.GetConnection())
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ItemName, Quantity FROM UniformItems", conn))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string itemName = reader["ItemName"].ToString();
                        int quantity = Convert.ToInt32(reader["Quantity"]);

                        if (quantity == 0)
                        {
                            outOfStockItems.Add(itemName);
                        }
                        else if (quantity <= 5) // Low stock threshold
                        {
                            lowStockItems.Add($"{itemName} ({quantity} left)");
                        }
                    }
                }

                // Show alerts for stock issues
                if (outOfStockItems.Count > 0)
                {
                    MessageBox.Show(
                        $"OUT OF STOCK ITEMS:\n\n{string.Join("\n", outOfStockItems)}\n\n" +
                        "These items cannot be sold until restocked.",
                        "Stock Alert - Out of Stock",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                if (lowStockItems.Count > 0)
                {
                    MessageBox.Show(
                        $"LOW STOCK ALERT:\n\n{string.Join("\n", lowStockItems)}\n\n" +
                        "Consider restocking these items soon.",
                        "Stock Alert - Low Stock",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking stock levels: {ex.Message}", "Stock Check Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

       
        private void cbItemName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbItemName.SelectedItem == null) return;

            DataRowView selectedRow = (DataRowView)cbItemName.SelectedItem;
            decimal price = Convert.ToDecimal(selectedRow["Price"]);
            int availableStock = Convert.ToInt32(selectedRow["Quantity"]);

            txtUnitPrice.Text = price.ToString("F2");

            // Update quantity controls based on available stock
            if (availableStock == 0)
            {
                numQty.Enabled = false;

                try
                {
                    // Ensure Minimum allows 0 before assigning
                    if (numQty.Minimum > 0)
                        numQty.Minimum = 0;

                    numQty.Value = 0;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Fallback: set Value safely within allowed range
                    numQty.Value = numQty.Minimum;

                    // Optional: log or show message (during debugging)
                    Console.WriteLine($"Error setting numQty.Value: {ex.Message}");
                }

                statusLabel.Text = $"⚠️ {cbItemName.Text} is OUT OF STOCK!";
                statusLabel.ForeColor = Color.Red;
            }
            else
            {
                numQty.Enabled = true;

                // Make sure Minimum is valid before setting Value
                numQty.Minimum = 1;
                numQty.Maximum = availableStock;

                try
                {
                    numQty.Value = Math.Min(1, availableStock);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    numQty.Value = numQty.Minimum;
                    Console.WriteLine($"Error setting numQty.Value: {ex.Message}");
                }

                if (availableStock <= 5)
                {
                    statusLabel.Text = $"⚠️ Low stock: {availableStock} {cbItemName.Text} remaining";
                    statusLabel.ForeColor = Color.Orange;
                }
                else
                {
                    statusLabel.Text = $"{availableStock} {cbItemName.Text} available";
                    statusLabel.ForeColor = Color.Green;
                }
            }


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
            // Validation
            if (cbItemName.SelectedItem == null || cbLastName.SelectedItem == null)
            {
                MessageBox.Show("Please select both an item and a student.", "Missing Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check stock availability
            DataRowView selectedRow = (DataRowView)cbItemName.SelectedItem;
            int availableStock = Convert.ToInt32(selectedRow["Quantity"]);
            int requestedQty = (int)numQty.Value;

            if (availableStock < requestedQty)
            {
                MessageBox.Show($"Not enough stock! Only {availableStock} units available.",
                    "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if adding this quantity would exceed current cart items for this product
            int currentCartQty = GetCurrentCartQuantity(cbItemName.Text);
            if (availableStock < (currentCartQty + requestedQty))
            {
                MessageBox.Show($"Cannot add {requestedQty} more units. " +
                    $"You already have {currentCartQty} in cart. " +
                    $"Only {availableStock - currentCartQty} more available.",
                    "Stock Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Add to cart
            string item = cbItemName.Text;
            decimal price = Convert.ToDecimal(txtUnitPrice.Text);
            int qty = requestedQty;
            decimal total = price * qty;
            string firstName = cbFirstName.Text;
            string lastName = cbLastName.Text;
            string studentClass = cbClass.Text;

            dgvCart.Rows.Add(item, qty, price.ToString("F2"), total.ToString("F2"),
                firstName, lastName, studentClass);

            // Update receipt display
            UpdateReceiptDisplay();

            statusLabel.Text = $"✓ {qty}× {item} added for {firstName} {lastName}";
            statusLabel.ForeColor = Color.Green;
        }

        private int GetCurrentCartQuantity(string itemName)
        {
            int totalQty = 0;
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells[0].Value?.ToString() == itemName)
                {
                    totalQty += Convert.ToInt32(row.Cells[1].Value);
                }
            }
            return totalQty;
        }
        private void UpdateReceiptDisplay()
        {
            if (dgvCart.Rows.Count == 0)
            {
                ClearReceiptDisplay();
                return;
            }

            GenerateReceiptForRichTextBox();
        }
        private void ClearReceiptDisplay()
        {
            if (this.Controls.Find("richTextBoxReceipt", true).FirstOrDefault() is RichTextBox rtb)
            {
                rtb.Clear();
            }
            lastReceiptText = "";
        }
        private void cbStudentName_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnDeleteItem_Click(object sender, EventArgs e)
        {
            if (dgvCart.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a row in the cart first.", "Nothing Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string itemName = dgvCart.SelectedRows[0].Cells[0].Value?.ToString();
            string studentName = $"{dgvCart.SelectedRows[0].Cells[4].Value} {dgvCart.SelectedRows[0].Cells[5].Value}";

            var result = MessageBox.Show(
                $"Remove '{itemName}' for '{studentName}' from cart?",
                "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                dgvCart.Rows.RemoveAt(dgvCart.SelectedRows[0].Index);
                UpdateReceiptDisplay();
                statusLabel.Text = "Item removed from cart.";
                statusLabel.ForeColor = Color.Blue;
            }
        }

        private void btnCompleteSale_Click(object sender, EventArgs e)
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("Your cart is empty. Please add items to cart first.",
                    "Cart Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check stock availability before completing sale
            if (!ValidateStockBeforeSale())
            {
                return;
            }

            GenerateReceiptForRichTextBox();

            MessageBox.Show(
                "Receipt generated! Review the receipt in the display area.\n\n" +
                "Click 'Print Receipt' to finalize the sale and update inventory.",
                "Receipt Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool ValidateStockBeforeSale()
        {
            var stockIssues = new List<string>();

            // Group cart items by product
            var cartItems = new Dictionary<string, int>();
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.IsNewRow) continue;
                string itemName = row.Cells[0].Value.ToString();
                int quantity = Convert.ToInt32(row.Cells[1].Value);

                if (cartItems.ContainsKey(itemName))
                    cartItems[itemName] += quantity;
                else
                    cartItems[itemName] = quantity;
            }

            // Check current stock for each item
            using (SQLiteConnection conn = SQLiteHelper.GetConnection())
            {
                foreach (var item in cartItems)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "SELECT Quantity FROM UniformItems WHERE ItemName = @itemName", conn))
                    {
                        cmd.Parameters.AddWithValue("@itemName", item.Key);
                        var result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            stockIssues.Add($"{item.Key}: Item not found in inventory");
                            continue;
                        }

                        int availableStock = Convert.ToInt32(result);
                        if (availableStock < item.Value)
                        {
                            stockIssues.Add($"{item.Key}: Need {item.Value}, only {availableStock} available");
                        }
                    }
                }
            }

            if (stockIssues.Count > 0)
            {
                MessageBox.Show(
                    "STOCK ISSUES FOUND:\n\n" + string.Join("\n", stockIssues) + "\n\n" +
                    "Please adjust quantities or remove items before completing the sale.",
                    "Stock Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void GenerateReceiptForRichTextBox()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("           GUSHESHE SCHOOL");
            sb.AppendLine("         UNIFORM SALES RECEIPT");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Receipt Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Receipt No: UNI{DateTime.Now:yyyyMMddHHmmss}");
            sb.AppendLine();

            // Items section
            sb.AppendLine("ITEMS PURCHASED:");
            sb.AppendLine("───────────────────────────────────────");

            decimal grandTotal = 0;
            int itemCount = 0;

            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.IsNewRow) continue;

                itemCount++;
                string itemName = row.Cells[0].Value?.ToString();
                string quantity = row.Cells[1].Value?.ToString();
                string unitPrice = row.Cells[2].Value?.ToString();
                string total = row.Cells[3].Value?.ToString();
                string firstName = row.Cells[4].Value?.ToString();
                string lastName = row.Cells[5].Value?.ToString();
                string studentClass = row.Cells[6].Value?.ToString();

                sb.AppendLine($"{itemCount}. {itemName}");
                sb.AppendLine($"   Student: {firstName} {lastName} - {studentClass}");
                sb.AppendLine($"   Qty: {quantity} × ${unitPrice} = ${total}");
                sb.AppendLine();

                if (decimal.TryParse(total, out decimal itemTotal))
                {
                    grandTotal += itemTotal;
                }
            }

            // Total section
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"TOTAL ITEMS: {itemCount}");
            sb.AppendLine($"GRAND TOTAL: ${grandTotal:F2}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("Thank you for your purchase!");
            sb.AppendLine("Keep this receipt for your records.");
            sb.AppendLine("═══════════════════════════════════════");

            lastReceiptText = sb.ToString();

            // Display in RichTextBox
            if (this.Controls.Find("richTextBoxReceipt", true).FirstOrDefault() is RichTextBox rtb)
            {
                rtb.Text = lastReceiptText;
                rtb.Font = new Font("Courier New", 9, FontStyle.Regular);
                rtb.SelectionStart = 0;
                rtb.ScrollToCaret();
            }
        }


        private void btnShowReceipt_Click(object sender, EventArgs e)
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("Cart is empty.", "No Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            GenerateReceiptForRichTextBox();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("Cart is empty. Please add items to cart before printing.",
                    "Cart Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Final stock validation
            if (!ValidateStockBeforeSale())
            {
                return;
            }

            if (string.IsNullOrEmpty(lastReceiptText))
            {
                GenerateReceiptForRichTextBox();
            }

            decimal cartTotal = CalculateCartTotal();
            int totalItems = CountCartItems();

            var verifyResult = MessageBox.Show(
                $"FINALIZE SALE:\n\n" +
                $"Total Items: {totalItems}\n" +
                $"Total Amount: ${cartTotal:F2}\n" +
                $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                "This will:\n" +
                "• Save sale to database\n" +
                "• Update inventory quantities\n" +
                "• Print receipt\n" +
                "• Clear cart\n\n" +
                "Proceed with sale?",
                "Confirm Sale", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (verifyResult != DialogResult.Yes) return;

            try
            {
                SaveSaleAndUpdateStock();
                PrintReceipt();

                MessageBox.Show(
                    $"Sale completed successfully!\n\n" +
                    $"• {totalItems} items sold\n" +
                    $"• Inventory updated\n" +
                    $"• Receipt printed\n" +
                    $"• Total: ${cartTotal:F2}",
                    "Sale Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Clear everything
                dgvCart.Rows.Clear();
                ClearReceiptDisplay();

                // Reload items to reflect updated stock
                LoadItemsWithStockCheck();

                statusLabel.Text = "Sale completed successfully!";
                statusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing sale:\n\n{ex.Message}",
                    "Sale Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveSaleAndUpdateStock()
        {
            using (var conn = SQLiteHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // Save sales records
                    using (var salesCmd = new SQLiteCommand(conn))
                    {
                        salesCmd.CommandText = @"
                            INSERT INTO UniformSales (ItemName, Quantity, UnitPrice, Total, FirstName, LastName, Class, DateSold)
                            VALUES (@i,@q,@u,@t,@f,@l,@c,@d)";

                        foreach (DataGridViewRow row in dgvCart.Rows)
                        {
                            if (row.IsNewRow) continue;

                            salesCmd.Parameters.Clear();
                            salesCmd.Parameters.AddWithValue("@i", row.Cells[0].Value);
                            salesCmd.Parameters.AddWithValue("@q", row.Cells[1].Value);
                            salesCmd.Parameters.AddWithValue("@u", row.Cells[2].Value);
                            salesCmd.Parameters.AddWithValue("@t", row.Cells[3].Value);
                            salesCmd.Parameters.AddWithValue("@f", row.Cells[4].Value);
                            salesCmd.Parameters.AddWithValue("@l", row.Cells[5].Value);
                            salesCmd.Parameters.AddWithValue("@c", row.Cells[6].Value);
                            salesCmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            salesCmd.ExecuteNonQuery();
                        }
                    }

                    // Update inventory quantities
                    using (var updateCmd = new SQLiteCommand(conn))
                    {
                        updateCmd.CommandText = @"
                            UPDATE UniformItems 
                            SET Quantity = Quantity - @soldQty 
                            WHERE ItemName = @itemName";

                        // Group items by name to handle multiple quantities
                        var itemQuantities = new Dictionary<string, int>();
                        foreach (DataGridViewRow row in dgvCart.Rows)
                        {
                            if (row.IsNewRow) continue;
                            string itemName = row.Cells[0].Value.ToString();
                            int quantity = Convert.ToInt32(row.Cells[1].Value);

                            if (itemQuantities.ContainsKey(itemName))
                                itemQuantities[itemName] += quantity;
                            else
                                itemQuantities[itemName] = quantity;
                        }

                        // Update each unique item
                        foreach (var item in itemQuantities)
                        {
                            updateCmd.Parameters.Clear();
                            updateCmd.Parameters.AddWithValue("@itemName", item.Key);
                            updateCmd.Parameters.AddWithValue("@soldQty", item.Value);
                            updateCmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }
        private decimal CalculateCartTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.IsNewRow) continue;
                if (decimal.TryParse(row.Cells[3].Value?.ToString(), out decimal itemTotal))
                {
                    total += itemTotal;
                }
            }
            return total;
        }

        private void SaveSaleToDatabase()
        {
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
                    cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
            }
        }
        private void PrintReceipt()
        {
            var pd = new PrintDocument();
            pd.PrintPage += (s, ev) =>
            {
                ev.Graphics.DrawString(
                  lastReceiptText,
                  new Font("Courier New", 10),
                  Brushes.Black,
                  ev.MarginBounds.Left,
                  ev.MarginBounds.Top);
            };

            var dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                pd.Print();
            }
            else
            {
                throw new Exception("Printing was cancelled by user.");
            }
        }

        private int CountCartItems()
        {
            int count = 0;
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.IsNewRow) continue;
                if (int.TryParse(row.Cells[1].Value?.ToString(), out int quantity))
                {
                    count += quantity;
                }
            }
            return count;
        }
        private void btnBack_Click(object sender, EventArgs e)
        {
            // Check if there are unsaved items in cart
            if (dgvCart.Rows.Count > 0)
            {
                var result = MessageBox.Show(
                    "You have items in your cart that haven't been saved.\n\n" +
                    "Are you sure you want to exit without completing the sale?",
                    "Unsaved Items",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    this.Close();
                }
            }
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            NumberButtonClick("1");
        }

        private void btn2_Click(object sender, EventArgs e)
        {
            NumberButtonClick("2");
        }

        private void btn3_Click(object sender, EventArgs e)
        {
            NumberButtonClick("3");
        }

        private void btn4_Click(object sender, EventArgs e)
        {
            NumberButtonClick("4");
        }

        private void btn5_Click(object sender, EventArgs e)
        {
            NumberButtonClick("5");
        }

        private void btn6_Click(object sender, EventArgs e)
        {
            NumberButtonClick("6");
        }

        private void btn9_Click(object sender, EventArgs e)
        {
            NumberButtonClick("9");
        }

        private void btn8_Click(object sender, EventArgs e)
        {
            NumberButtonClick("8");
        }

        private void btn7_Click(object sender, EventArgs e)
        {
            NumberButtonClick("7");
        }

        private void btn0_Click(object sender, EventArgs e)
        {
            NumberButtonClick("0");
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            OperationButtonClick("+");
        }

        private void btnSubtract_Click(object sender, EventArgs e)
        {
            OperationButtonClick("-");
        }

        private void btnMultiply_Click(object sender, EventArgs e)
        {
            OperationButtonClick("x");
        }

        private void btnDivide_Click(object sender, EventArgs e)
        {
            OperationButtonClick("÷");
        }

        private void btnEquals_Click(object sender, EventArgs e)
        {
            PerformCalculation();
            equalsPressed = true;
            operation = "";
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearCalculator();
        }
        private void NumberButtonClick(string number)
        {
            // If we just performed an operation or equals, start fresh
            if (operationPressed || equalsPressed)
            {
                lblDisplay.Text = "0";
                operationPressed = false;
                equalsPressed = false;
            }

            // Handle display logic
            if (lblDisplay.Text == "0")
            {
                lblDisplay.Text = number;
            }
            else
            {
                // Prevent display from becoming too long
                if (lblDisplay.Text.Length < 15)
                {
                    lblDisplay.Text += number;
                }
            }

            // Update current value
            if (double.TryParse(lblDisplay.Text, out double result))
            {
                currentValue = result;
            }
        }
        private void OperationButtonClick(string op)
        {
            if (!string.IsNullOrEmpty(operation) && !operationPressed)
            {
                PerformCalculation();
            }
            else
            {
                previousValue = currentValue;
            }
            operation = op;
            operationPressed = true;
            equalsPressed = false;
        }
        private void PerformCalculation()
        {
            if (string.IsNullOrEmpty(operation))
                return;

            double result = 0;
            bool validOperation = true;

            try
            {
                switch (operation)
                {
                    case "+":
                        result = previousValue + currentValue;
                        break;
                    case "-":
                        result = previousValue - currentValue;
                        break;
                    case "×":
                        result = previousValue * currentValue;
                        break;
                    case "÷":
                        if (currentValue == 0)
                        {
                            lblDisplay.Text = "Error";
                            validOperation = false;
                        }
                        else
                        {
                            result = previousValue / currentValue;
                        }
                        break;
                    default:
                        validOperation = false;
                        break;
                }

                if (validOperation)
                {
                    // Handle very large numbers
                    if (Math.Abs(result) > 999999999999999)
                    {
                        lblDisplay.Text = result.ToString("E2");
                    }
                    else
                    {
                        // Format the result to remove unnecessary decimal places
                        if (result == Math.Floor(result))
                        {
                            lblDisplay.Text = result.ToString("F0");
                        }
                        else
                        {
                            lblDisplay.Text = result.ToString("G15");
                        }
                    }

                    // Update values
                    currentValue = result;
                    previousValue = result;
                }
            }
            catch (Exception)
            {
                lblDisplay.Text = "Error";
                validOperation = false;
            }

            // If there was an error, reset the calculator
            if (!validOperation)
            {
                currentValue = 0;
                previousValue = 0;
                operation = "";
            }
        }

        private void ClearCalculator()
        {
            currentValue = 0;
            previousValue = 0;
            operation = "";
            operationPressed = false;
            equalsPressed = false;
            lblDisplay.Text = "0";
        }

        private void btnDot_Click(object sender, EventArgs e)
        {
            // If we just performed an operation or equals, start fresh
            if (operationPressed || equalsPressed)
            {
                lblDisplay.Text = "0";
                operationPressed = false;
                equalsPressed = false;
            }

            // Add decimal point if it doesn't already exist
            if (!lblDisplay.Text.Contains("."))
            {
                lblDisplay.Text += ".";
            }
        }
        // Keyboard support (optional)
        private void CalculatorForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    NumberButtonClick(e.KeyChar.ToString());
                    break;
                case '+':
                    OperationButtonClick("+");
                    break;
                case '-':
                    OperationButtonClick("-");
                    break;
                case '*':
                    OperationButtonClick("×");
                    break;
                case '/':
                    OperationButtonClick("÷");
                    e.Handled = true; // Prevent system beep
                    break;
                case '=':
                case '\r': // Enter key
                    PerformCalculation();
                    equalsPressed = true;
                    operation = "";
                    break;
                case (char)27: // Escape key
                    ClearCalculator();
                    break;
                case '.':
                    btnDot_Click(sender, EventArgs.Empty);
                    break;
            }
        }

        // Stock Management Helper Methods
        private void RefreshStockDisplay()
        {
            LoadItemsWithStockCheck();
            if (cbItemName.SelectedItem != null)
            {
                cbItemName_SelectedIndexChanged(cbItemName, EventArgs.Empty);
            }
        }
        private void ShowStockReport()
        {
            try
            {
                var stockReport = new StringBuilder();
                stockReport.AppendLine("CURRENT STOCK LEVELS");
                stockReport.AppendLine("══════════════════════════════════");

                using (SQLiteConnection conn = SQLiteHelper.GetConnection())
                using (SQLiteCommand cmd = new SQLiteCommand(
                    "SELECT ItemName, Quantity, Price FROM UniformItems ORDER BY ItemName", conn))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string itemName = reader["ItemName"].ToString();
                        int quantity = Convert.ToInt32(reader["Quantity"]);
                        decimal price = Convert.ToDecimal(reader["Price"]);

                        string status = quantity == 0 ? " [OUT OF STOCK]" :
                                       quantity <= 5 ? " [LOW STOCK]" : "";

                        stockReport.AppendLine($"{itemName,-25} {quantity,3} units  ${price:F2}{status}");
                    }
                }

                MessageBox.Show(stockReport.ToString(), "Stock Report",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating stock report: {ex.Message}",
                    "Stock Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Additional helper method to handle cart clearing with confirmation
        private void ClearCartWithConfirmation()
        {
            if (dgvCart.Rows.Count > 0)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to clear all items from the cart?",
                    "Clear Cart", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    dgvCart.Rows.Clear();
                    ClearReceiptDisplay();
                    statusLabel.Text = "Cart cleared.";
                    statusLabel.ForeColor = Color.Blue;
                }
            }
        }
}
}

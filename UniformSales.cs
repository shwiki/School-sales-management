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

            // 2) STUDENTS
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
            // the combo's ValueMember is "Price"
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

            string itemName = dgvCart.SelectedRows[0].Cells[0].Value?.ToString();
            string studentName = $"{dgvCart.SelectedRows[0].Cells[4].Value} {dgvCart.SelectedRows[0].Cells[5].Value}";

            var result = MessageBox.Show(
                $"Are you sure you want to remove '{itemName}' for '{studentName}' from the cart?",
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                dgvCart.Rows.RemoveAt(dgvCart.SelectedRows[0].Index);
                statusLabel.Text = "Item removed from cart.";
            }
        }

        private void btnCompleteSale_Click(object sender, EventArgs e)
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show(
                  "Your cart is empty. Please add items to cart first.",
                  "Cart Empty",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information);
                return;
            }

            // Generate receipt and display in RichTextBox
            GenerateReceiptForRichTextBox();

            // Show success message
            MessageBox.Show(
                "Receipt has been generated and displayed in the receipt area.\n\n" +
                "To finalize the sale, please click the 'Print Receipt' button.",
                "Receipt Generated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.IsNewRow) continue;

                string itemName = row.Cells[0].Value?.ToString();
                string quantity = row.Cells[1].Value?.ToString();
                string unitPrice = row.Cells[2].Value?.ToString();
                string total = row.Cells[3].Value?.ToString();
                string firstName = row.Cells[4].Value?.ToString();
                string lastName = row.Cells[5].Value?.ToString();
                string studentClass = row.Cells[6].Value?.ToString();

                sb.AppendLine($"Item: {itemName}");
                sb.AppendLine($"Student: {firstName} {lastName} - {studentClass}");
                sb.AppendLine($"Quantity: {quantity} × ${unitPrice} = ${total}");
                sb.AppendLine();

                if (decimal.TryParse(total, out decimal itemTotal))
                {
                    grandTotal += itemTotal;
                }
            }

            // Total section
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"GRAND TOTAL: ${grandTotal:F2}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("Thank you for your purchase!");
            sb.AppendLine("Keep this receipt for your records.");
            sb.AppendLine("═══════════════════════════════════════");

            // Store for printing and display in RichTextBox
            lastReceiptText = sb.ToString();

            // Display in RichTextBox (assuming you have richTextBoxReceipt)
            if (this.Controls.Find("richTextBoxReceipt", true).FirstOrDefault() is RichTextBox rtb)
            {
                rtb.Text = lastReceiptText;
                rtb.Font = new Font("Courier New", 10, FontStyle.Regular);
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

            // Show preview in MessageBox as well
            MessageBox.Show(
              lastReceiptText,
              "Receipt Preview",
              MessageBoxButtons.OK,
              MessageBoxIcon.Information);
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            // Validation before saving
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show(
                    "Cart is empty. Please add items to cart before printing.",
                    "Cart Empty",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Generate receipt if not already done
            if (string.IsNullOrEmpty(lastReceiptText))
            {
                GenerateReceiptForRichTextBox();
            }

            // Calculate total for verification
            decimal cartTotal = CalculateCartTotal();
            int totalItems = CountCartItems();

            // Pre-save verification dialog
            var verifyResult = MessageBox.Show(
                $"VERIFY SALE DETAILS:\n\n" +
                $"Total Items: {totalItems}\n" +
                $"Total Amount: ${cartTotal:F2}\n" +
                $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                "This will:\n" +
                "• Save all items to the database\n" +
                "• Print the receipt\n" +
                "• Clear the cart\n\n" +
                "Do you want to proceed with this sale?",
                "Confirm Sale & Print",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (verifyResult != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Save to database
                SaveSaleToDatabase();

                // Print the receipt
                PrintReceipt();

                // Success notification
                MessageBox.Show(
                    $"Sale completed successfully!\n\n" +
                    $"• {totalItems} items saved to database\n" +
                    $"• Receipt printed\n" +
                    $"• Total: ${cartTotal:F2}",
                    "Sale Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Clear cart after successful save and print
                dgvCart.Rows.Clear();
                lastReceiptText = "";

                // Clear RichTextBox
                if (this.Controls.Find("richTextBoxReceipt", true).FirstOrDefault() is RichTextBox rtb)
                {
                    rtb.Clear();
                }

                statusLabel.Text = "Sale completed and cart cleared.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error processing sale:\n\n{ex.Message}\n\n" +
                    "The sale was not completed. Please try again.",
                    "Sale Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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

        // Optional: Add memory functions
        private double memoryValue = 0;

        private void btnMemoryStore_Click(object sender, EventArgs e)
        {
            memoryValue = currentValue;
        }
    }
}

namespace Gusheshe
{
    partial class Dashboard
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
            this.uniformbtn = new System.Windows.Forms.Button();
            this.feebtn = new System.Windows.Forms.Button();
            this.recordsbtn = new System.Windows.Forms.Button();
            this.syncbtn = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btnStockManagement = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // uniformbtn
            // 
            this.uniformbtn.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uniformbtn.Location = new System.Drawing.Point(122, 171);
            this.uniformbtn.Name = "uniformbtn";
            this.uniformbtn.Size = new System.Drawing.Size(155, 78);
            this.uniformbtn.TabIndex = 0;
            this.uniformbtn.Text = "Uniform Sales";
            this.uniformbtn.UseVisualStyleBackColor = true;
            this.uniformbtn.Click += new System.EventHandler(this.uniformbtn_Click);
            // 
            // feebtn
            // 
            this.feebtn.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.feebtn.Location = new System.Drawing.Point(337, 171);
            this.feebtn.Name = "feebtn";
            this.feebtn.Size = new System.Drawing.Size(155, 78);
            this.feebtn.TabIndex = 1;
            this.feebtn.Text = "Fees Payment";
            this.feebtn.UseVisualStyleBackColor = true;
            this.feebtn.Click += new System.EventHandler(this.feebtn_Click);
            // 
            // recordsbtn
            // 
            this.recordsbtn.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.recordsbtn.Location = new System.Drawing.Point(552, 171);
            this.recordsbtn.Name = "recordsbtn";
            this.recordsbtn.Size = new System.Drawing.Size(155, 78);
            this.recordsbtn.TabIndex = 3;
            this.recordsbtn.Text = "Student Records";
            this.recordsbtn.UseVisualStyleBackColor = true;
            this.recordsbtn.Click += new System.EventHandler(this.recordsbtn_Click);
            // 
            // syncbtn
            // 
            this.syncbtn.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.syncbtn.Location = new System.Drawing.Point(767, 171);
            this.syncbtn.Name = "syncbtn";
            this.syncbtn.Size = new System.Drawing.Size(155, 78);
            this.syncbtn.TabIndex = 2;
            this.syncbtn.Text = "Sync Online";
            this.syncbtn.UseVisualStyleBackColor = true;
            this.syncbtn.Click += new System.EventHandler(this.syncbtn_Click);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(845, 422);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(155, 78);
            this.button1.TabIndex = 4;
            this.button1.Text = "Exit";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(36, 422);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(155, 78);
            this.button2.TabIndex = 5;
            this.button2.Text = "Back";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // btnStockManagement
            // 
            this.btnStockManagement.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStockManagement.Location = new System.Drawing.Point(439, 295);
            this.btnStockManagement.Name = "btnStockManagement";
            this.btnStockManagement.Size = new System.Drawing.Size(155, 78);
            this.btnStockManagement.TabIndex = 6;
            this.btnStockManagement.Text = "Stock Management";
            this.btnStockManagement.UseVisualStyleBackColor = true;
            this.btnStockManagement.Click += new System.EventHandler(this.btnStockManagement_Click);
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1106, 553);
            this.Controls.Add(this.btnStockManagement);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.recordsbtn);
            this.Controls.Add(this.syncbtn);
            this.Controls.Add(this.feebtn);
            this.Controls.Add(this.uniformbtn);
            this.Name = "Dashboard";
            this.Text = "Dashboard";
            this.Load += new System.EventHandler(this.Dashboard_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button uniformbtn;
        private System.Windows.Forms.Button feebtn;
        private System.Windows.Forms.Button recordsbtn;
        private System.Windows.Forms.Button syncbtn;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnStockManagement;
    }
}


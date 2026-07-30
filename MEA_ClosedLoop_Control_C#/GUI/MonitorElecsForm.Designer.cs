namespace GUI
{
    partial class MonitorElecsForm
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
            this.selectAll_check = new System.Windows.Forms.CheckBox();
            this.btn_update = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // selectAll_check
            // 
            this.selectAll_check.AutoSize = true;
            this.selectAll_check.Location = new System.Drawing.Point(38, 36);
            this.selectAll_check.Name = "selectAll_check";
            this.selectAll_check.Size = new System.Drawing.Size(69, 17);
            this.selectAll_check.TabIndex = 0;
            this.selectAll_check.Text = "Select all";
            this.selectAll_check.UseVisualStyleBackColor = true;
            this.selectAll_check.CheckedChanged += new System.EventHandler(this.selectAll_check_CheckedChanged);
            // 
            // btn_update
            // 
            this.btn_update.Location = new System.Drawing.Point(134, 32);
            this.btn_update.Name = "btn_update";
            this.btn_update.Size = new System.Drawing.Size(75, 23);
            this.btn_update.TabIndex = 1;
            this.btn_update.Text = "Update";
            this.btn_update.UseVisualStyleBackColor = true;
            this.btn_update.Click += new System.EventHandler(this.update_button_Click);
            // 
            // MonitorElecsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(678, 597);
            this.Controls.Add(this.btn_update);
            this.Controls.Add(this.selectAll_check);
            this.Name = "MonitorElecsForm";
            this.Text = "MonitorElecsForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox selectAll_check;
        private System.Windows.Forms.Button btn_update;
    }
}
namespace GUI
{
    partial class ThresholderForm
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
            this.btn_startDacq = new System.Windows.Forms.Button();
            this.label_Stds = new System.Windows.Forms.Label();
            this.txt_Stds = new System.Windows.Forms.TextBox();
            this.txt_Filter = new System.Windows.Forms.TextBox();
            this.label_Filter = new System.Windows.Forms.Label();
            this.btn_set_Filter = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_startDacq
            // 
            this.btn_startDacq.Location = new System.Drawing.Point(352, 25);
            this.btn_startDacq.Name = "btn_startDacq";
            this.btn_startDacq.Size = new System.Drawing.Size(93, 22);
            this.btn_startDacq.TabIndex = 36;
            this.btn_startDacq.Text = "Set Threshold";
            this.btn_startDacq.UseVisualStyleBackColor = true;
            this.btn_startDacq.Click += new System.EventHandler(this.btn_startDacq_Click);
            // 
            // label_Stds
            // 
            this.label_Stds.AutoSize = true;
            this.label_Stds.Location = new System.Drawing.Point(282, 29);
            this.label_Stds.Name = "label_Stds";
            this.label_Stds.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label_Stds.Size = new System.Drawing.Size(31, 13);
            this.label_Stds.TabIndex = 37;
            this.label_Stds.Text = "Stds:";
            // 
            // txt_Stds
            // 
            this.txt_Stds.Location = new System.Drawing.Point(310, 26);
            this.txt_Stds.Name = "txt_Stds";
            this.txt_Stds.Size = new System.Drawing.Size(36, 20);
            this.txt_Stds.TabIndex = 38;
            this.txt_Stds.Text = "5";
            this.txt_Stds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txt_Filter
            // 
            this.txt_Filter.Location = new System.Drawing.Point(156, 26);
            this.txt_Filter.Name = "txt_Filter";
            this.txt_Filter.Size = new System.Drawing.Size(40, 20);
            this.txt_Filter.TabIndex = 39;
            this.txt_Filter.TextChanged += new System.EventHandler(this.txt_filter_TextChanged);
            // 
            // label_Filter
            // 
            this.label_Filter.AutoSize = true;
            this.label_Filter.Location = new System.Drawing.Point(49, 30);
            this.label_Filter.Name = "label_Filter";
            this.label_Filter.Size = new System.Drawing.Size(102, 13);
            this.label_Filter.TabIndex = 40;
            this.label_Filter.Text = "High Pass Filter [Hz]";
            // 
            // btn_set_Filter
            // 
            this.btn_set_Filter.Location = new System.Drawing.Point(201, 25);
            this.btn_set_Filter.Name = "btn_set_Filter";
            this.btn_set_Filter.Size = new System.Drawing.Size(32, 23);
            this.btn_set_Filter.TabIndex = 41;
            this.btn_set_Filter.Text = "Set";
            this.btn_set_Filter.UseVisualStyleBackColor = true;
            this.btn_set_Filter.MouseCaptureChanged += new System.EventHandler(this.btn_set_filter_Click);
            // 
            // ThresholderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(533, 292);
            this.Controls.Add(this.btn_set_Filter);
            this.Controls.Add(this.label_Filter);
            this.Controls.Add(this.txt_Filter);
            this.Controls.Add(this.txt_Stds);
            this.Controls.Add(this.label_Stds);
            this.Controls.Add(this.btn_startDacq);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "ThresholderForm";
            this.Text = "Thresholder Form";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_startDacq;
        private System.Windows.Forms.Label label_Stds;
        private System.Windows.Forms.TextBox txt_Stds;
        private System.Windows.Forms.TextBox txt_Filter;
        private System.Windows.Forms.Label label_Filter;
        private System.Windows.Forms.Button btn_set_Filter;
    }
}
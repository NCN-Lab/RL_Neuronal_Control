namespace GUI
{
    partial class Main
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
            this.label1 = new System.Windows.Forms.Label();
            this.Button_SpikesForm = new System.Windows.Forms.Button();
            this.Button_ElecData = new System.Windows.Forms.Button();
            this.Button_Stimulator = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.List_devices = new System.Windows.Forms.ComboBox();
            this.List_MeaLayout = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.SamplingRatesList = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.button_thresholding = new System.Windows.Forms.Button();
            this.button_newRL = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label1.Location = new System.Drawing.Point(142, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(201, 22);
            this.label1.TabIndex = 0;
            this.label1.Text = "NCN MEA Control Suite";
            // 
            // Button_SpikesForm
            // 
            this.Button_SpikesForm.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Button_SpikesForm.ForeColor = System.Drawing.SystemColors.WindowFrame;
            this.Button_SpikesForm.Location = new System.Drawing.Point(19, 80);
            this.Button_SpikesForm.Name = "Button_SpikesForm";
            this.Button_SpikesForm.Size = new System.Drawing.Size(148, 34);
            this.Button_SpikesForm.TabIndex = 5;
            this.Button_SpikesForm.Text = "Spike Detection";
            this.Button_SpikesForm.UseVisualStyleBackColor = true;
            this.Button_SpikesForm.Click += new System.EventHandler(this.Button_SpikesForm_Click);
            // 
            // Button_ElecData
            // 
            this.Button_ElecData.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Button_ElecData.ForeColor = System.Drawing.SystemColors.WindowFrame;
            this.Button_ElecData.Location = new System.Drawing.Point(19, 28);
            this.Button_ElecData.Name = "Button_ElecData";
            this.Button_ElecData.Size = new System.Drawing.Size(148, 34);
            this.Button_ElecData.TabIndex = 6;
            this.Button_ElecData.Text = "Electrode Data";
            this.Button_ElecData.UseVisualStyleBackColor = true;
            this.Button_ElecData.Click += new System.EventHandler(this.Button_ElecData_Click);
            // 
            // Button_Stimulator
            // 
            this.Button_Stimulator.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Button_Stimulator.ForeColor = System.Drawing.SystemColors.WindowFrame;
            this.Button_Stimulator.Location = new System.Drawing.Point(19, 134);
            this.Button_Stimulator.Name = "Button_Stimulator";
            this.Button_Stimulator.Size = new System.Drawing.Size(148, 34);
            this.Button_Stimulator.TabIndex = 7;
            this.Button_Stimulator.Text = "Stimulator";
            this.Button_Stimulator.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Button_ElecData);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.Button_Stimulator);
            this.groupBox1.Controls.Add(this.Button_SpikesForm);
            this.groupBox1.Location = new System.Drawing.Point(256, 60);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(187, 188);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Teal;
            this.label2.Location = new System.Drawing.Point(64, -2);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 17);
            this.label2.TabIndex = 12;
            this.label2.Text = "Functions";
            // 
            // List_devices
            // 
            this.List_devices.FormattingEnabled = true;
            this.List_devices.Location = new System.Drawing.Point(20, 41);
            this.List_devices.Name = "List_devices";
            this.List_devices.Size = new System.Drawing.Size(148, 21);
            this.List_devices.TabIndex = 14;
            this.List_devices.SelectedIndexChanged += new System.EventHandler(this.List_devices_SelectedIndexChanged);
            // 
            // List_MeaLayout
            // 
            this.List_MeaLayout.FormattingEnabled = true;
            this.List_MeaLayout.Location = new System.Drawing.Point(20, 93);
            this.List_MeaLayout.Name = "List_MeaLayout";
            this.List_MeaLayout.Size = new System.Drawing.Size(148, 21);
            this.List_MeaLayout.TabIndex = 15;
            this.List_MeaLayout.SelectedIndexChanged += new System.EventHandler(this.List_MeaLayout_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label5.Location = new System.Drawing.Point(18, 24);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(155, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "MEA System - USB Connection";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label6.Location = new System.Drawing.Point(20, 76);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "MEA Chip Layout";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.SamplingRatesList);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.List_devices);
            this.groupBox4.Controls.Add(this.List_MeaLayout);
            this.groupBox4.Location = new System.Drawing.Point(39, 60);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(187, 188);
            this.groupBox4.TabIndex = 18;
            this.groupBox4.TabStop = false;
            // 
            // SamplingRatesList
            // 
            this.SamplingRatesList.FormattingEnabled = true;
            this.SamplingRatesList.Location = new System.Drawing.Point(20, 147);
            this.SamplingRatesList.Name = "SamplingRatesList";
            this.SamplingRatesList.Size = new System.Drawing.Size(148, 21);
            this.SamplingRatesList.TabIndex = 19;
            this.SamplingRatesList.SelectedIndexChanged += new System.EventHandler(this.SamplingRatesList_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.Teal;
            this.label8.Location = new System.Drawing.Point(48, -2);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(99, 17);
            this.label8.TabIndex = 13;
            this.label8.Text = "Configurations";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label7.Location = new System.Drawing.Point(19, 130);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(98, 13);
            this.label7.TabIndex = 18;
            this.label7.Text = "Sampling Rate (Hz)";
            // 
            // button_thresholding
            // 
            this.button_thresholding.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_thresholding.Location = new System.Drawing.Point(492, 145);
            this.button_thresholding.Name = "button_thresholding";
            this.button_thresholding.Size = new System.Drawing.Size(118, 34);
            this.button_thresholding.TabIndex = 20;
            this.button_thresholding.Text = "Thresholding";
            this.button_thresholding.UseVisualStyleBackColor = true;
            this.button_thresholding.Click += new System.EventHandler(this.button_thresholding_Click);
            // 
            // button_newRL
            // 
            this.button_newRL.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_newRL.Location = new System.Drawing.Point(492, 194);
            this.button_newRL.Name = "button_newRL";
            this.button_newRL.Size = new System.Drawing.Size(118, 34);
            this.button_newRL.TabIndex = 21;
            this.button_newRL.Text = "New RL";
            this.button_newRL.UseVisualStyleBackColor = true;
            this.button_newRL.Click += new System.EventHandler(this.button_newRL_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 276);
            this.Controls.Add(this.button_newRL);
            this.Controls.Add(this.button_thresholding);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Name = "Main";
            this.Text = "Main";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Button_SpikesForm;
        private System.Windows.Forms.Button Button_ElecData;
        private System.Windows.Forms.Button Button_Stimulator;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox List_devices;
        private System.Windows.Forms.ComboBox List_MeaLayout;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox SamplingRatesList;
        private System.Windows.Forms.Button button_thresholding;
        private System.Windows.Forms.Button button_newRL;
    }
}
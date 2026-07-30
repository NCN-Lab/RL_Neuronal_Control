namespace GUI
{
    partial class StimulatorForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.STG_2_button = new System.Windows.Forms.Button();
            this.STG_1_button = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.download_Button = new System.Windows.Forms.Button();
            this.duration2_Text = new System.Windows.Forms.TextBox();
            this.duration1_Text = new System.Windows.Forms.TextBox();
            this.Amp2_text = new System.Windows.Forms.TextBox();
            this.Amp1_text = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.stimulate_Button = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.STG_2_button);
            this.groupBox1.Controls.Add(this.STG_1_button);
            this.groupBox1.Location = new System.Drawing.Point(23, 19);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(138, 55);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Stimulator";
            // 
            // STG_2_button
            // 
            this.STG_2_button.Location = new System.Drawing.Point(73, 19);
            this.STG_2_button.Name = "STG_2_button";
            this.STG_2_button.Size = new System.Drawing.Size(56, 23);
            this.STG_2_button.TabIndex = 2;
            this.STG_2_button.Text = "STG 2";
            this.STG_2_button.UseVisualStyleBackColor = true;
            this.STG_2_button.Click += new System.EventHandler(this.STG_2_button_Click);
            // 
            // STG_1_button
            // 
            this.STG_1_button.Location = new System.Drawing.Point(9, 19);
            this.STG_1_button.Name = "STG_1_button";
            this.STG_1_button.Size = new System.Drawing.Size(56, 23);
            this.STG_1_button.TabIndex = 1;
            this.STG_1_button.Text = "STG 1";
            this.STG_1_button.UseVisualStyleBackColor = true;
            this.STG_1_button.Click += new System.EventHandler(this.STG_1_button_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.download_Button);
            this.groupBox2.Controls.Add(this.duration2_Text);
            this.groupBox2.Controls.Add(this.duration1_Text);
            this.groupBox2.Controls.Add(this.Amp2_text);
            this.groupBox2.Controls.Add(this.Amp1_text);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(23, 90);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(138, 164);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Stimulator Pulse";
            // 
            // download_Button
            // 
            this.download_Button.Location = new System.Drawing.Point(7, 125);
            this.download_Button.Name = "download_Button";
            this.download_Button.Size = new System.Drawing.Size(120, 27);
            this.download_Button.TabIndex = 3;
            this.download_Button.Text = "Download";
            this.download_Button.UseVisualStyleBackColor = true;
            this.download_Button.Click += new System.EventHandler(this.download_button_Click);
            // 
            // duration2_Text
            // 
            this.duration2_Text.Location = new System.Drawing.Point(71, 90);
            this.duration2_Text.Name = "duration2_Text";
            this.duration2_Text.Size = new System.Drawing.Size(56, 20);
            this.duration2_Text.TabIndex = 5;
            this.duration2_Text.Text = "200";
            this.duration2_Text.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // duration1_Text
            // 
            this.duration1_Text.Location = new System.Drawing.Point(9, 90);
            this.duration1_Text.Name = "duration1_Text";
            this.duration1_Text.Size = new System.Drawing.Size(56, 20);
            this.duration1_Text.TabIndex = 4;
            this.duration1_Text.Text = "200";
            this.duration1_Text.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Amp2_text
            // 
            this.Amp2_text.Location = new System.Drawing.Point(71, 41);
            this.Amp2_text.Name = "Amp2_text";
            this.Amp2_text.Size = new System.Drawing.Size(56, 20);
            this.Amp2_text.TabIndex = 3;
            this.Amp2_text.Text = "0";
            this.Amp2_text.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Amp1_text
            // 
            this.Amp1_text.Location = new System.Drawing.Point(9, 41);
            this.Amp1_text.Name = "Amp1_text";
            this.Amp1_text.Size = new System.Drawing.Size(56, 20);
            this.Amp1_text.TabIndex = 2;
            this.Amp1_text.Text = "-400";
            this.Amp1_text.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Duration (us)";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Amplitude (mV)";
            // 
            // stimulate_Button
            // 
            this.stimulate_Button.Enabled = false;
            this.stimulate_Button.Location = new System.Drawing.Point(23, 269);
            this.stimulate_Button.Name = "stimulate_Button";
            this.stimulate_Button.Size = new System.Drawing.Size(138, 27);
            this.stimulate_Button.TabIndex = 2;
            this.stimulate_Button.Text = "Stimulate";
            this.stimulate_Button.UseVisualStyleBackColor = true;
            this.stimulate_Button.Click += new System.EventHandler(this.stimulate__Button_Click);
            // 
            // StimulatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(186, 313);
            this.Controls.Add(this.stimulate_Button);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "StimulatorForm";
            this.Text = "                                                                                 " +
    "                         ";
            this.Load += new System.EventHandler(this.StimulatorForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox Amp2_text;
        private System.Windows.Forms.TextBox Amp1_text;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox duration2_Text;
        private System.Windows.Forms.TextBox duration1_Text;
        private System.Windows.Forms.Button stimulate_Button;
        private System.Windows.Forms.Button download_Button;
        private System.Windows.Forms.Button STG_2_button;
        private System.Windows.Forms.Button STG_1_button;
    }
}
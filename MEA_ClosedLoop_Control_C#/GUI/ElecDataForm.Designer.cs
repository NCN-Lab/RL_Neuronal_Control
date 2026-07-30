namespace GUI
{
    partial class ElecDataForm
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.Chart_ElecRec = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.Button_startDaq = new System.Windows.Forms.Button();
            this.Button_stopDaq = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.Text_recBlock = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.Chart_ElecRec)).BeginInit();
            this.SuspendLayout();
            // 
            // Chart_ElecRec
            // 
            this.Chart_ElecRec.AntiAliasing = System.Windows.Forms.DataVisualization.Charting.AntiAliasingStyles.None;
            chartArea3.Name = "ChartArea1";
            this.Chart_ElecRec.ChartAreas.Add(chartArea3);
            this.Chart_ElecRec.Location = new System.Drawing.Point(45, 46);
            this.Chart_ElecRec.Name = "Chart_ElecRec";
            series3.ChartArea = "ChartArea1";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            series3.Name = "Series1";
            this.Chart_ElecRec.Series.Add(series3);
            this.Chart_ElecRec.Size = new System.Drawing.Size(549, 338);
            this.Chart_ElecRec.TabIndex = 4;
            this.Chart_ElecRec.Text = "chart1";
            // 
            // Button_startDaq
            // 
            this.Button_startDaq.Location = new System.Drawing.Point(94, 28);
            this.Button_startDaq.Name = "Button_startDaq";
            this.Button_startDaq.Size = new System.Drawing.Size(75, 23);
            this.Button_startDaq.TabIndex = 9;
            this.Button_startDaq.Text = "Start Daq";
            this.Button_startDaq.UseVisualStyleBackColor = true;
            this.Button_startDaq.Click += new System.EventHandler(this.Button_startDaq_Click);
            // 
            // Button_stopDaq
            // 
            this.Button_stopDaq.Location = new System.Drawing.Point(175, 28);
            this.Button_stopDaq.Name = "Button_stopDaq";
            this.Button_stopDaq.Size = new System.Drawing.Size(75, 23);
            this.Button_stopDaq.TabIndex = 10;
            this.Button_stopDaq.Text = "Stop Daq";
            this.Button_stopDaq.UseVisualStyleBackColor = true;
            this.Button_stopDaq.Click += new System.EventHandler(this.Button_stopDaq_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(280, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Recording block [s]";
            // 
            // Text_recBlock
            // 
            this.Text_recBlock.Location = new System.Drawing.Point(388, 34);
            this.Text_recBlock.Name = "Text_recBlock";
            this.Text_recBlock.Size = new System.Drawing.Size(39, 20);
            this.Text_recBlock.TabIndex = 12;
            this.Text_recBlock.Text = "0.1";
            this.Text_recBlock.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(307, 376);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 17);
            this.label2.TabIndex = 13;
            this.label2.Text = "Samples";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(9, 196);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 17);
            this.label3.TabIndex = 14;
            this.label3.Text = "Voltage [uV]";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ElecDataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.ClientSize = new System.Drawing.Size(668, 434);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.Text_recBlock);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Button_stopDaq);
            this.Controls.Add(this.Button_startDaq);
            this.Controls.Add(this.Chart_ElecRec);
            this.Name = "ElecDataForm";
            this.Text = "ElecDataForm";
            this.Load += new System.EventHandler(this.ElecDataForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.Chart_ElecRec)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart Chart_ElecRec;
        private System.Windows.Forms.Button Button_startDaq;
        private System.Windows.Forms.Button Button_stopDaq;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox Text_recBlock;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}
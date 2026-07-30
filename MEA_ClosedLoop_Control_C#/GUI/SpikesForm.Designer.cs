namespace GUI
{
    partial class SpikesForm
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
            System.Windows.Forms.DataVisualization.Charting.Legend legend3 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.tab_container = new System.Windows.Forms.TabControl();
            this.thresholder_tab = new System.Windows.Forms.TabPage();
            this.MonitorElecs_tab = new System.Windows.Forms.TabPage();
            this.SpkDetector_tab = new System.Windows.Forms.TabPage();
            this.future_txt = new System.Windows.Forms.TextBox();
            this.set_future_button = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.set_past_button = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.past_txt = new System.Windows.Forms.TextBox();
            this.stop_button = new System.Windows.Forms.Button();
            this.deadtime_textbox = new System.Windows.Forms.TextBox();
            this.start_button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.raster_chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label5 = new System.Windows.Forms.Label();
            this.tab_container.SuspendLayout();
            this.SpkDetector_tab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.raster_chart)).BeginInit();
            this.SuspendLayout();
            // 
            // tab_container
            // 
            this.tab_container.Controls.Add(this.thresholder_tab);
            this.tab_container.Controls.Add(this.MonitorElecs_tab);
            this.tab_container.Controls.Add(this.SpkDetector_tab);
            this.tab_container.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tab_container.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tab_container.Location = new System.Drawing.Point(0, 0);
            this.tab_container.Margin = new System.Windows.Forms.Padding(2);
            this.tab_container.Name = "tab_container";
            this.tab_container.SelectedIndex = 0;
            this.tab_container.Size = new System.Drawing.Size(694, 528);
            this.tab_container.TabIndex = 42;
            // 
            // thresholder_tab
            // 
            this.thresholder_tab.BackColor = System.Drawing.SystemColors.Control;
            this.thresholder_tab.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.thresholder_tab.Location = new System.Drawing.Point(4, 25);
            this.thresholder_tab.Margin = new System.Windows.Forms.Padding(2);
            this.thresholder_tab.Name = "thresholder_tab";
            this.thresholder_tab.Padding = new System.Windows.Forms.Padding(2);
            this.thresholder_tab.Size = new System.Drawing.Size(686, 499);
            this.thresholder_tab.TabIndex = 0;
            this.thresholder_tab.Text = "Spike Thresholds";
            // 
            // MonitorElecs_tab
            // 
            this.MonitorElecs_tab.BackColor = System.Drawing.SystemColors.Control;
            this.MonitorElecs_tab.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MonitorElecs_tab.Location = new System.Drawing.Point(4, 25);
            this.MonitorElecs_tab.Margin = new System.Windows.Forms.Padding(2);
            this.MonitorElecs_tab.Name = "MonitorElecs_tab";
            this.MonitorElecs_tab.Padding = new System.Windows.Forms.Padding(2);
            this.MonitorElecs_tab.Size = new System.Drawing.Size(686, 499);
            this.MonitorElecs_tab.TabIndex = 1;
            this.MonitorElecs_tab.Text = "Monitoring Electrodes";
            // 
            // SpkDetector_tab
            // 
            this.SpkDetector_tab.BackColor = System.Drawing.SystemColors.Control;
            this.SpkDetector_tab.Controls.Add(this.label5);
            this.SpkDetector_tab.Controls.Add(this.future_txt);
            this.SpkDetector_tab.Controls.Add(this.set_future_button);
            this.SpkDetector_tab.Controls.Add(this.label3);
            this.SpkDetector_tab.Controls.Add(this.set_past_button);
            this.SpkDetector_tab.Controls.Add(this.label2);
            this.SpkDetector_tab.Controls.Add(this.past_txt);
            this.SpkDetector_tab.Controls.Add(this.stop_button);
            this.SpkDetector_tab.Controls.Add(this.deadtime_textbox);
            this.SpkDetector_tab.Controls.Add(this.start_button);
            this.SpkDetector_tab.Controls.Add(this.label1);
            this.SpkDetector_tab.Controls.Add(this.raster_chart);
            this.SpkDetector_tab.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SpkDetector_tab.Location = new System.Drawing.Point(4, 25);
            this.SpkDetector_tab.Margin = new System.Windows.Forms.Padding(2);
            this.SpkDetector_tab.Name = "SpkDetector_tab";
            this.SpkDetector_tab.Padding = new System.Windows.Forms.Padding(2);
            this.SpkDetector_tab.Size = new System.Drawing.Size(686, 499);
            this.SpkDetector_tab.TabIndex = 2;
            this.SpkDetector_tab.Text = "Spike Detector";
            this.SpkDetector_tab.Click += new System.EventHandler(this.SpkDetector_tab_Click);
            // 
            // future_txt
            // 
            this.future_txt.Location = new System.Drawing.Point(443, 30);
            this.future_txt.Name = "future_txt";
            this.future_txt.Size = new System.Drawing.Size(44, 20);
            this.future_txt.TabIndex = 34;
            // 
            // set_future_button
            // 
            this.set_future_button.Location = new System.Drawing.Point(491, 29);
            this.set_future_button.Name = "set_future_button";
            this.set_future_button.Size = new System.Drawing.Size(32, 23);
            this.set_future_button.TabIndex = 33;
            this.set_future_button.Text = "set";
            this.set_future_button.UseVisualStyleBackColor = true;
            this.set_future_button.Click += new System.EventHandler(this.set_future_button_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(441, 11);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 13);
            this.label3.TabIndex = 32;
            this.label3.Text = "Future [s]";
            // 
            // set_past_button
            // 
            this.set_past_button.Location = new System.Drawing.Point(396, 27);
            this.set_past_button.Name = "set_past_button";
            this.set_past_button.Size = new System.Drawing.Size(32, 23);
            this.set_past_button.TabIndex = 31;
            this.set_past_button.Text = "set";
            this.set_past_button.UseVisualStyleBackColor = true;
            this.set_past_button.Click += new System.EventHandler(this.set_past_button_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(346, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 13);
            this.label2.TabIndex = 30;
            this.label2.Text = "Past [s]";
            // 
            // past_txt
            // 
            this.past_txt.Location = new System.Drawing.Point(348, 28);
            this.past_txt.Name = "past_txt";
            this.past_txt.Size = new System.Drawing.Size(44, 20);
            this.past_txt.TabIndex = 29;
            // 
            // stop_button
            // 
            this.stop_button.Location = new System.Drawing.Point(216, 27);
            this.stop_button.Name = "stop_button";
            this.stop_button.Size = new System.Drawing.Size(72, 23);
            this.stop_button.TabIndex = 28;
            this.stop_button.Text = "Stop";
            this.stop_button.UseVisualStyleBackColor = true;
            this.stop_button.Click += new System.EventHandler(this.stop_button_Click);
            // 
            // deadtime_textbox
            // 
            this.deadtime_textbox.Location = new System.Drawing.Point(62, 28);
            this.deadtime_textbox.Margin = new System.Windows.Forms.Padding(2);
            this.deadtime_textbox.Name = "deadtime_textbox";
            this.deadtime_textbox.Size = new System.Drawing.Size(72, 20);
            this.deadtime_textbox.TabIndex = 27;
            this.deadtime_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // start_button
            // 
            this.start_button.Location = new System.Drawing.Point(140, 27);
            this.start_button.Name = "start_button";
            this.start_button.Size = new System.Drawing.Size(72, 23);
            this.start_button.TabIndex = 25;
            this.start_button.Text = "Start";
            this.start_button.UseVisualStyleBackColor = true;
            this.start_button.Click += new System.EventHandler(this.start_button_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(60, 12);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 26;
            this.label1.Text = "Deadtime [ms]";
            // 
            // raster_chart
            // 
            this.raster_chart.BorderlineColor = System.Drawing.Color.Transparent;
            chartArea3.AxisX.InterlacedColor = System.Drawing.Color.MintCream;
            chartArea3.AxisX.LineColor = System.Drawing.Color.DarkGray;
            chartArea3.AxisX.MajorGrid.LineColor = System.Drawing.Color.DarkGray;
            chartArea3.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea3.AxisY.LineColor = System.Drawing.Color.DarkGray;
            chartArea3.AxisY.MajorGrid.LineColor = System.Drawing.Color.DarkGray;
            chartArea3.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chartArea3.Name = "ChartArea1";
            chartArea3.Position.Auto = false;
            chartArea3.Position.Height = 90F;
            chartArea3.Position.Width = 100F;
            chartArea3.Position.Y = 10F;
            this.raster_chart.ChartAreas.Add(chartArea3);
            this.raster_chart.Dock = System.Windows.Forms.DockStyle.Fill;
            legend3.Name = "Legend1";
            this.raster_chart.Legends.Add(legend3);
            this.raster_chart.Location = new System.Drawing.Point(2, 2);
            this.raster_chart.Margin = new System.Windows.Forms.Padding(2);
            this.raster_chart.Name = "raster_chart";
            series3.ChartArea = "ChartArea1";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastPoint;
            series3.IsVisibleInLegend = false;
            series3.Legend = "Legend1";
            series3.Name = "Series1";
            series3.YValuesPerPoint = 4;
            this.raster_chart.Series.Add(series3);
            this.raster_chart.Size = new System.Drawing.Size(682, 495);
            this.raster_chart.TabIndex = 24;
            this.raster_chart.Text = "chart1";
            this.raster_chart.Click += new System.EventHandler(this.raster_chart_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.White;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(8, 33);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 17);
            this.label5.TabIndex = 36;
            this.label5.Text = "Elec";
            // 
            // SpikesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 528);
            this.Controls.Add(this.tab_container);
            this.Name = "SpikesForm";
            this.Text = "SpikesForm";
            this.tab_container.ResumeLayout(false);
            this.SpkDetector_tab.ResumeLayout(false);
            this.SpkDetector_tab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.raster_chart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TabControl tab_container;
        private System.Windows.Forms.TabPage thresholder_tab;
        private System.Windows.Forms.TabPage MonitorElecs_tab;
        private System.Windows.Forms.TabPage SpkDetector_tab;
        private System.Windows.Forms.DataVisualization.Charting.Chart raster_chart;
        private System.Windows.Forms.TextBox future_txt;
        private System.Windows.Forms.Button set_future_button;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button set_past_button;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox past_txt;
        private System.Windows.Forms.Button stop_button;
        private System.Windows.Forms.TextBox deadtime_textbox;
        private System.Windows.Forms.Button start_button;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label5;
    }
}
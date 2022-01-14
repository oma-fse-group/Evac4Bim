
namespace CmdPlotCharts
{
    partial class Figure
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
            this.plt = new ScottPlot.FormsPlot();
            this.SuspendLayout();
            // 
            // plt
            // 
            this.plt.AutoSize = true;
            this.plt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plt.Location = new System.Drawing.Point(0, 0);
            this.plt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.plt.Name = "plt";
            this.plt.Size = new System.Drawing.Size(800, 450);
            this.plt.TabIndex = 0;
            this.plt.Load += new System.EventHandler(this.formsPlot1_Load);
            // 
            // Figure
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.plt);
            this.Name = "Figure";
            this.Text = "Figure";
            this.Load += new System.EventHandler(this.Figure_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public ScottPlot.FormsPlot plt;
    }
}
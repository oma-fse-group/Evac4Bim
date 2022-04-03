
namespace Revit.SDK.Samples.ModelessForm_ExternalEvent.CS
{
    partial class ModelessForm
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
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.btnExit = new System.Windows.Forms.Button();
            this.timerLabel = new System.Windows.Forms.Label();
            this.minTimeLabel = new System.Windows.Forms.Label();
            this.maxTimeLabel = new System.Windows.Forms.Label();
            this.bwdButtom = new System.Windows.Forms.Button();
            this.fwdButtom = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(12, 27);
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(818, 56);
            this.trackBar1.TabIndex = 0;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll_1);
            this.trackBar1.ValueChanged += new System.EventHandler(this.trackBar1_Scroll_1);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(710, 70);
            this.btnExit.Margin = new System.Windows.Forms.Padding(4);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(119, 28);
            this.btnExit.TabIndex = 0;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click_1);
            // 
            // timerLabel
            // 
            this.timerLabel.AutoSize = true;
            this.timerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timerLabel.Location = new System.Drawing.Point(399, 6);
            this.timerLabel.Name = "timerLabel";
            this.timerLabel.Size = new System.Drawing.Size(50, 20);
            this.timerLabel.TabIndex = 1;
            this.timerLabel.Text = "00/00";
            this.timerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.timerLabel.Click += new System.EventHandler(this.timerLabel_Click);
            // 
            // minTimeLabel
            // 
            this.minTimeLabel.AutoSize = true;
            this.minTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.minTimeLabel.Location = new System.Drawing.Point(21, 6);
            this.minTimeLabel.Name = "minTimeLabel";
            this.minTimeLabel.Size = new System.Drawing.Size(18, 20);
            this.minTimeLabel.TabIndex = 2;
            this.minTimeLabel.Text = "0";
            this.minTimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // maxTimeLabel
            // 
            this.maxTimeLabel.AutoSize = true;
            this.maxTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.maxTimeLabel.Location = new System.Drawing.Point(795, 6);
            this.maxTimeLabel.Name = "maxTimeLabel";
            this.maxTimeLabel.Size = new System.Drawing.Size(45, 20);
            this.maxTimeLabel.TabIndex = 3;
            this.maxTimeLabel.Text = "0000";
            this.maxTimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // bwdButtom
            // 
            this.bwdButtom.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bwdButtom.Location = new System.Drawing.Point(377, 70);
            this.bwdButtom.Name = "bwdButtom";
            this.bwdButtom.Size = new System.Drawing.Size(40, 28);
            this.bwdButtom.TabIndex = 4;
            this.bwdButtom.Text = "<<";
            this.bwdButtom.UseVisualStyleBackColor = true;
            this.bwdButtom.Click += new System.EventHandler(this.bwdButtom_Click);
            // 
            // fwdButtom
            // 
            this.fwdButtom.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fwdButtom.Location = new System.Drawing.Point(423, 70);
            this.fwdButtom.Name = "fwdButtom";
            this.fwdButtom.Size = new System.Drawing.Size(41, 28);
            this.fwdButtom.TabIndex = 5;
            this.fwdButtom.Text = ">>";
            this.fwdButtom.UseVisualStyleBackColor = true;
            this.fwdButtom.Click += new System.EventHandler(this.fwdButtom_Click);
            // 
            // ModelessForm
            // 
            this.AcceptButton = this.btnExit;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(842, 102);
            this.Controls.Add(this.fwdButtom);
            this.Controls.Add(this.bwdButtom);
            this.Controls.Add(this.maxTimeLabel);
            this.Controls.Add(this.minTimeLabel);
            this.Controls.Add(this.timerLabel);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.trackBar1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModelessForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Results Player";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ModelessForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label timerLabel;
        private System.Windows.Forms.Label minTimeLabel;
        private System.Windows.Forms.Label maxTimeLabel;
        private System.Windows.Forms.Button bwdButtom;
        private System.Windows.Forms.Button fwdButtom;
    }
}
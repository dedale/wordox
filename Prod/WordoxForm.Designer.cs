namespace Ded.Wordox
{
    partial class WordoxForm
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
            this.label = new System.Windows.Forms.Label();
            this.boardUserControl = new Ded.Wordox.BoardUserControl();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Font = new System.Drawing.Font("Wingdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.label.Location = new System.Drawing.Point(346, 12);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(25, 17);
            this.label.TabIndex = 1;
            this.label.Text = "è";
            // 
            // boardUserControl
            // 
            this.boardUserControl.AutoSize = true;
            this.boardUserControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.boardUserControl.Location = new System.Drawing.Point(12, 12);
            this.boardUserControl.Name = "boardUserControl";
            this.boardUserControl.Size = new System.Drawing.Size(312, 312);
            this.boardUserControl.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Wingdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.label1.Location = new System.Drawing.Point(377, 92);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "°¶R";
            // 
            // WordoxForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(475, 330);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label);
            this.Controls.Add(this.boardUserControl);
            this.Name = "WordoxForm";
            this.Text = "WordoxForm";
            this.Load += new System.EventHandler(this.WordoxForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private BoardUserControl boardUserControl;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.Label label1;
    }
}
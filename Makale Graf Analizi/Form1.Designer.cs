namespace Makale_Graf_Analizi
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            button1 = new Button();
            btnSifirla = new Button();
            btnAnaliz = new Button();
            txtK = new TextBox();
            lblSeciliDugum = new Label();
            lblBilgi = new Label();
            btnYukle = new Button();
            pbGraf = new PictureBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbGraf).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            panel1.Controls.Add(button1);
            panel1.Controls.Add(btnSifirla);
            panel1.Controls.Add(btnAnaliz);
            panel1.Controls.Add(txtK);
            panel1.Controls.Add(lblSeciliDugum);
            panel1.Controls.Add(lblBilgi);
            panel1.Controls.Add(btnYukle);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(300, 450);
            panel1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button1.Location = new Point(3, 379);
            button1.Name = "button1";
            button1.Size = new Size(194, 30);
            button1.TabIndex = 6;
            button1.Text = "Full Grafiği Göster";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // btnSifirla
            // 
            btnSifirla.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSifirla.Location = new Point(3, 415);
            btnSifirla.Name = "btnSifirla";
            btnSifirla.Size = new Size(194, 30);
            btnSifirla.TabIndex = 4;
            btnSifirla.Text = "Grafı Sıfırla";
            btnSifirla.UseVisualStyleBackColor = true;
            btnSifirla.Click += btnSifirla_Click;
            // 
            // btnAnaliz
            // 
            btnAnaliz.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAnaliz.Location = new Point(3, 343);
            btnAnaliz.Name = "btnAnaliz";
            btnAnaliz.Size = new Size(194, 30);
            btnAnaliz.TabIndex = 3;
            btnAnaliz.Text = "K-Core Analizi Yap";
            btnAnaliz.UseVisualStyleBackColor = true;
            btnAnaliz.Click += btnAnaliz_Click;
            // 
            // txtK
            // 
            txtK.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            txtK.Location = new Point(74, 318);
            txtK.Name = "txtK";
            txtK.Size = new Size(49, 23);
            txtK.TabIndex = 2;
            // 
            // lblSeciliDugum
            // 
            lblSeciliDugum.BackColor = SystemColors.Control;
            lblSeciliDugum.Font = new Font("Segoe UI", 8F);
            lblSeciliDugum.Location = new Point(3, 412);
            lblSeciliDugum.Name = "lblSeciliDugum";
            lblSeciliDugum.Size = new Size(244, 200);
            lblSeciliDugum.TabIndex = 5;
            // 
            // lblBilgi
            // 
            lblBilgi.Location = new Point(3, 103);
            lblBilgi.Name = "lblBilgi";
            lblBilgi.Size = new Size(244, 300);
            lblBilgi.TabIndex = 1;
            lblBilgi.Text = "İstatistikler...";
            lblBilgi.Click += lblBilgi_Click;
            // 
            // btnYukle
            // 
            btnYukle.Location = new Point(3, 70);
            btnYukle.Name = "btnYukle";
            btnYukle.Size = new Size(244, 30);
            btnYukle.TabIndex = 0;
            btnYukle.Text = "JSON Yükle";
            btnYukle.UseVisualStyleBackColor = true;
            btnYukle.Click += btnYukle_Click;
            // 
            // pbGraf
            // 
            pbGraf.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbGraf.BackColor = Color.White;
            pbGraf.Location = new Point(250, 0);
            pbGraf.Name = "pbGraf";
            pbGraf.Size = new Size(550, 450);
            pbGraf.TabIndex = 1;
            pbGraf.TabStop = false;
            pbGraf.MouseClick += pbGraf_MouseClick;
            pbGraf.MouseDown += pbGraf_MouseDown;
            pbGraf.MouseMove += pbGraf_MouseMove;
            pbGraf.MouseUp += pbGraf_MouseUp;
            pbGraf.MouseWheel += pbGraf_MouseWheel;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pbGraf);
            Controls.Add(panel1);
            Name = "Form1";
            Text = "Form1";
            Resize += Form1_Resize;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbGraf).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Button btnYukle;
        private Label lblBilgi;
        private Label lblSeciliDugum;
        private PictureBox pbGraf;
        private Button btnAnaliz;
        private TextBox txtK;
        private Button btnSifirla;
        private Button button1;
    }
}

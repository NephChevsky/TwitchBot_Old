namespace StreamDeck.Forms
{
	partial class StartStream
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
			this.TitleLabel = new System.Windows.Forms.Label();
			this.TitleText = new System.Windows.Forms.TextBox();
			this.GameTitleText = new System.Windows.Forms.TextBox();
			this.GameTitleLabel = new System.Windows.Forms.Label();
			this.StartButton = new System.Windows.Forms.Button();
			this.QuitButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// TitleLabel
			// 
			this.TitleLabel.AutoSize = true;
			this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.TitleLabel.Location = new System.Drawing.Point(12, 6);
			this.TitleLabel.Name = "TitleLabel";
			this.TitleLabel.Size = new System.Drawing.Size(111, 25);
			this.TitleLabel.TabIndex = 0;
			this.TitleLabel.Text = "Titre du live";
			// 
			// TitleText
			// 
			this.TitleText.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.TitleText.Location = new System.Drawing.Point(12, 37);
			this.TitleText.Multiline = true;
			this.TitleText.Name = "TitleText";
			this.TitleText.Size = new System.Drawing.Size(552, 71);
			this.TitleText.TabIndex = 1;
			// 
			// GameTitleText
			// 
			this.GameTitleText.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.GameTitleText.Location = new System.Drawing.Point(12, 162);
			this.GameTitleText.Name = "GameTitleText";
			this.GameTitleText.Size = new System.Drawing.Size(552, 32);
			this.GameTitleText.TabIndex = 3;
			// 
			// GameTitleLabel
			// 
			this.GameTitleLabel.AutoSize = true;
			this.GameTitleLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.GameTitleLabel.Location = new System.Drawing.Point(12, 130);
			this.GameTitleLabel.Name = "GameTitleLabel";
			this.GameTitleLabel.Size = new System.Drawing.Size(108, 25);
			this.GameTitleLabel.TabIndex = 5;
			this.GameTitleLabel.Text = "Titre du jeu";
			// 
			// StartButton
			// 
			this.StartButton.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.StartButton.Location = new System.Drawing.Point(364, 231);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(200, 50);
			this.StartButton.TabIndex = 6;
			this.StartButton.Text = "Lancer le stream";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// QuitButton
			// 
			this.QuitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.QuitButton.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.QuitButton.Location = new System.Drawing.Point(12, 231);
			this.QuitButton.Name = "QuitButton";
			this.QuitButton.Size = new System.Drawing.Size(200, 50);
			this.QuitButton.TabIndex = 7;
			this.QuitButton.Text = "Annuler";
			this.QuitButton.UseVisualStyleBackColor = true;
			// 
			// StartStream
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(576, 293);
			this.Controls.Add(this.QuitButton);
			this.Controls.Add(this.StartButton);
			this.Controls.Add(this.GameTitleLabel);
			this.Controls.Add(this.GameTitleText);
			this.Controls.Add(this.TitleText);
			this.Controls.Add(this.TitleLabel);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "StartStream";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Lancer le stream";
			this.Load += new System.EventHandler(this.StartStream_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Label TitleLabel;
		private TextBox TitleText;
		private TextBox GameTitleText;
		private Label GameTitleLabel;
		private Button StartButton;
		private Button QuitButton;
	}
}
namespace GenericTextFunctions
{
	partial class ExtractISBNdata
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
			this.buttonSearchISBN = new System.Windows.Forms.Button();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.buttonOpenFile = new System.Windows.Forms.Button();
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.ISBNcolumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DollarColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PoundColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductFormatColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PageColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DimensionsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ImprintColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SeriesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.buttonSeekISBNrelatedData = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxTextToSeek = new System.Windows.Forms.TextBox();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonSearchISBN
			// 
			this.buttonSearchISBN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSearchISBN.Location = new System.Drawing.Point(432, 451);
			this.buttonSearchISBN.Name = "buttonSearchISBN";
			this.buttonSearchISBN.Size = new System.Drawing.Size(131, 23);
			this.buttonSearchISBN.TabIndex = 0;
			this.buttonSearchISBN.Text = "Find and extract ISBNcolumn";
			this.buttonSearchISBN.UseVisualStyleBackColor = true;
			this.buttonSearchISBN.Click += new System.EventHandler(this.button1_Click);
			// 
			// richTextBox1
			// 
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Location = new System.Drawing.Point(0, 30);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.Size = new System.Drawing.Size(360, 395);
			this.richTextBox1.TabIndex = 1;
			this.richTextBox1.Text = "";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(15, 12);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.buttonOpenFile);
			this.splitContainer1.Panel1.Controls.Add(this.richTextBox1);
			this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(0, 30, 0, 0);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.dataGridView1);
			this.splitContainer1.Size = new System.Drawing.Size(1274, 425);
			this.splitContainer1.SplitterDistance = 360;
			this.splitContainer1.TabIndex = 2;
			// 
			// buttonOpenFile
			// 
			this.buttonOpenFile.Location = new System.Drawing.Point(0, 1);
			this.buttonOpenFile.Name = "buttonOpenFile";
			this.buttonOpenFile.Size = new System.Drawing.Size(75, 23);
			this.buttonOpenFile.TabIndex = 2;
			this.buttonOpenFile.Text = "&Open file";
			this.buttonOpenFile.UseVisualStyleBackColor = true;
			this.buttonOpenFile.Click += new System.EventHandler(this.buttonOpenFile_Click);
			// 
			// dataGridView1
			// 
			this.dataGridView1.AllowUserToAddRows = false;
			this.dataGridView1.AllowUserToDeleteRows = false;
			this.dataGridView1.AllowUserToOrderColumns = true;
			this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.dataGridView1.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ISBNcolumn,
            this.DollarColumn,
            this.PoundColumn,
            this.ProductFormatColumn,
            this.DateColumn,
            this.PageColumn,
            this.DimensionsColumn,
            this.ImprintColumn,
            this.SeriesColumn});
			this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridView1.Location = new System.Drawing.Point(0, 0);
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.ReadOnly = true;
			this.dataGridView1.RowHeadersVisible = false;
			this.dataGridView1.Size = new System.Drawing.Size(910, 425);
			this.dataGridView1.TabIndex = 0;
			// 
			// ISBNcolumn
			// 
			this.ISBNcolumn.HeaderText = "ISBN";
			this.ISBNcolumn.Name = "ISBNcolumn";
			this.ISBNcolumn.ReadOnly = true;
			this.ISBNcolumn.Width = 57;
			// 
			// DollarColumn
			// 
			this.DollarColumn.HeaderText = "Dollar";
			this.DollarColumn.Name = "DollarColumn";
			this.DollarColumn.ReadOnly = true;
			this.DollarColumn.Width = 59;
			// 
			// PoundColumn
			// 
			this.PoundColumn.HeaderText = "Pound";
			this.PoundColumn.Name = "PoundColumn";
			this.PoundColumn.ReadOnly = true;
			this.PoundColumn.Width = 63;
			// 
			// ProductFormatColumn
			// 
			this.ProductFormatColumn.HeaderText = "ProductFormat";
			this.ProductFormatColumn.Name = "ProductFormatColumn";
			this.ProductFormatColumn.ReadOnly = true;
			this.ProductFormatColumn.Width = 101;
			// 
			// DateColumn
			// 
			this.DateColumn.HeaderText = "Date";
			this.DateColumn.Name = "DateColumn";
			this.DateColumn.ReadOnly = true;
			this.DateColumn.Width = 55;
			// 
			// PageColumn
			// 
			this.PageColumn.HeaderText = "Page";
			this.PageColumn.Name = "PageColumn";
			this.PageColumn.ReadOnly = true;
			this.PageColumn.Width = 57;
			// 
			// DimensionsColumn
			// 
			this.DimensionsColumn.HeaderText = "Dimensions";
			this.DimensionsColumn.Name = "DimensionsColumn";
			this.DimensionsColumn.ReadOnly = true;
			this.DimensionsColumn.Width = 86;
			// 
			// ImprintColumn
			// 
			this.ImprintColumn.HeaderText = "Imprint";
			this.ImprintColumn.Name = "ImprintColumn";
			this.ImprintColumn.ReadOnly = true;
			this.ImprintColumn.Width = 63;
			// 
			// SeriesColumn
			// 
			this.SeriesColumn.HeaderText = "Series";
			this.SeriesColumn.Name = "SeriesColumn";
			this.SeriesColumn.ReadOnly = true;
			this.SeriesColumn.Width = 61;
			// 
			// buttonSeekISBNrelatedData
			// 
			this.buttonSeekISBNrelatedData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSeekISBNrelatedData.Location = new System.Drawing.Point(1174, 451);
			this.buttonSeekISBNrelatedData.Name = "buttonSeekISBNrelatedData";
			this.buttonSeekISBNrelatedData.Size = new System.Drawing.Size(115, 23);
			this.buttonSeekISBNrelatedData.TabIndex = 3;
			this.buttonSeekISBNrelatedData.Text = "Seek other related";
			this.buttonSeekISBNrelatedData.UseVisualStyleBackColor = true;
			this.buttonSeekISBNrelatedData.Click += new System.EventHandler(this.buttonSeekISBNrelatedData_Click);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 456);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(223, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "Seek this text on line and extract ISBNcolumn";
			// 
			// textBoxTextToSeek
			// 
			this.textBoxTextToSeek.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTextToSeek.Location = new System.Drawing.Point(207, 453);
			this.textBoxTextToSeek.Name = "textBoxTextToSeek";
			this.textBoxTextToSeek.Size = new System.Drawing.Size(219, 20);
			this.textBoxTextToSeek.TabIndex = 5;
			this.textBoxTextToSeek.Text = "EAN 978";
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
			this.statusStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.statusStrip1.Location = new System.Drawing.Point(0, 485);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.statusStrip1.Size = new System.Drawing.Size(1301, 22);
			this.statusStrip1.SizingGrip = false;
			this.statusStrip1.TabIndex = 7;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
			this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
			// 
			// toolStripProgressBar1
			// 
			this.toolStripProgressBar1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripProgressBar1.Name = "toolStripProgressBar1";
			this.toolStripProgressBar1.Size = new System.Drawing.Size(300, 16);
			this.toolStripProgressBar1.Visible = false;
			// 
			// ExtractISBNdata
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1301, 507);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.textBoxTextToSeek);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonSeekISBNrelatedData);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.buttonSearchISBN);
			this.DoubleBuffered = true;
			this.Name = "ExtractISBNdata";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "ExtractISBNdata";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonSearchISBN;
		private System.Windows.Forms.RichTextBox richTextBox1;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.Button buttonSeekISBNrelatedData;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxTextToSeek;
		private System.Windows.Forms.DataGridViewTextBoxColumn ISBNcolumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DollarColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PoundColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductFormatColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DateColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PageColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DimensionsColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ImprintColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SeriesColumn;
		private System.Windows.Forms.Button buttonOpenFile;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
	}
}


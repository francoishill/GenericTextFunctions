using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace GenericTextFunctions
{
	public partial class ExtractISBNdata : Form
	{
		public ExtractISBNdata()
		{
			InitializeComponent();
			toolStripStatusLabel1.Text = null;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			using (new WaitIndicator())
			{
				dataGridView1.Rows.Clear();
				dataGridView1.SuspendLayout();
				for (int i = 0; i < richTextBox1.Lines.Length; i++)
				{
					string curLine = richTextBox1.Lines[i];
					if (curLine.IndexOf(textBoxTextToSeek.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
					{
						int newrow = dataGridView1.Rows.Add();
						int ISBNindex = curLine.IndexOf("978");
						dataGridView1[0, newrow].Value = curLine.Substring(ISBNindex, 13);
					}
				}
				dataGridView1.ResumeLayout();
			}
		}

		private void buttonSeekISBNrelatedData_Click(object sender, EventArgs e)
		{
			toolStripProgressBar1.Visible = true;
			toolStripProgressBar1.Value = 0;
			toolStripProgressBar1.Maximum = richTextBox1.Lines.Length;

			toolStripStatusLabel1.Text = "Extracting data, please wait...";

			Stopwatch sw = Stopwatch.StartNew();
			//using (new WaitIndicator())
			//{
			dataGridView1.Rows.Clear();
			dataGridView1.SuspendLayout();
			for (int i = 0; i < richTextBox1.Lines.Length; i++)
			{
				string curLine = richTextBox1.Lines[i];
				if (curLine.IndexOf("EAN 978", StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					int newrow = dataGridView1.Rows.Add();
					int EANindex = curLine.IndexOf("EAN 978", StringComparison.InvariantCultureIgnoreCase);
					dataGridView1[ISBNcolumn.Index, newrow].Value = curLine.Substring(EANindex + 4, 13);

					string prevLine = richTextBox1.Lines[i - 1];
					string[] wordsPrevInLine = prevLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
					dataGridView1[DateColumn.Index, newrow].Value = wordsPrevInLine[0];
					dataGridView1[PageColumn.Index, newrow].Value = wordsPrevInLine[1];
					dataGridView1[DimensionsColumn.Index, newrow].Value = wordsPrevInLine[2];

					string nextLine = richTextBox1.Lines[i + 1];
					string[] wordsNextInLine = nextLine.Split(new char[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
					dataGridView1[DollarColumn.Index, newrow].Value = wordsNextInLine.First(s => s.StartsWith("$"));
					dataGridView1[PoundColumn.Index, newrow].Value = wordsNextInLine.First(s => s.StartsWith("£"));
					dataGridView1[ProductFormatColumn.Index, newrow].Value = wordsNextInLine[2];

					for (int j = i + 1; j < i + 1 + 9; j++)
					{
						if (j >= richTextBox1.Lines.Length)
							continue;
						string nextfewLines = richTextBox1.Lines[j];
						if (nextfewLines.StartsWith("Imprint:", StringComparison.InvariantCultureIgnoreCase))
							dataGridView1[ImprintColumn.Index, newrow].Value = nextfewLines.Substring("Imprint:".Length).Trim();
						if (nextfewLines.StartsWith("Series:", StringComparison.InvariantCultureIgnoreCase))
							dataGridView1[SeriesColumn.Index, newrow].Value = nextfewLines.Substring("Series:".Length).Trim();
					}
				}
				toolStripProgressBar1.Increment(1);
				Application.DoEvents();
			}
			dataGridView1.ResumeLayout();
			//}
			sw.Stop();
			toolStripStatusLabel1.Text = string.Format("Extraction complete with duration of {0} seconds", sw.Elapsed.TotalSeconds);

			toolStripProgressBar1.Value = 0;
			toolStripProgressBar1.Maximum = 100;
			toolStripProgressBar1.Visible = false;
		}

		private void buttonOpenFile_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Rich text files (*.rtf)|*.rtf";
			if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				toolStripStatusLabel1.Text = "Hallo";
				PerformTimedActionAndUpdateStatus(
					() =>
					{

						richTextBox1.LoadFile(ofd.FileName, RichTextBoxStreamType.RichText);
					},
					"Loading file, please be patient...",
					"File load completed in {0} seconds.",
					false);//,
				//5000);
			}
		}

		/// <summary>
		/// The actionToPerform will be performed and status will be updated before and after (including the time taken).
		/// </summary>
		/// <param name="actionToPerform">The action to perform.</param>
		/// <param name="StatusToSayStarting">The status text to say the action is starting, please wait.</param>
		/// <param name="StatusToSayCompletedAddZeroParameter">The status text to say it is completed i.e. "Action completed in {0} seconds.</param>
		/// <param name="MakeProgressbarVisibleDuring">Whether the progressbar should be made visible during the action.</param>
		/// <param name="completionMessageTimeout">The timeout after how long the message showed for completion is hidden.</param>
		private void PerformTimedActionAndUpdateStatus(Action actionToPerform, string StatusToSayStarting, string StatusToSayCompletedAddZeroParameter, bool MakeProgressbarVisibleDuring, int completionMessageTimeout = 0)
		{
			if (MakeProgressbarVisibleDuring)
				toolStripProgressBar1.Visible = true;
			toolStripStatusLabel1.Text = StatusToSayStarting;
			//Application.DoEvents();
			DoEvents();
			Stopwatch sw = Stopwatch.StartNew();
			actionToPerform();
			sw.Stop();
			string completionMessage = string.Format(StatusToSayCompletedAddZeroParameter, sw.Elapsed.TotalSeconds);
			toolStripStatusLabel1.Text = completionMessage;
			if (completionMessageTimeout > 0)
			{
				System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
				timer.Interval = completionMessageTimeout;
				timer.Tag = completionMessage;
				timer.Tick += (s, e) =>
				{
					System.Windows.Forms.Timer t = s as System.Windows.Forms.Timer;
					if (t == null) return;
					t.Stop();
					if (t.Tag != null && t.Tag.ToString() == completionMessage)
						toolStripStatusLabel1.Text = null;
					t.Dispose();
					t = null;
				};
				timer.Start();
			}
			if (MakeProgressbarVisibleDuring)
				toolStripProgressBar1.Visible = false;
			//Application.DoEvents();
			DoEvents();
		}

		public static void DoEvents()
		{
			System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
		}
	}

	/// <summary>
	/// Usage: using (new WaitIndicator()) { Your code here... }
	/// </summary>
	public class WaitIndicator : IDisposable
	{
		public class ProgressForm : Form
		{
			public ProgressForm()
			{
				ControlBox = false;
				ShowInTaskbar = false;
				StartPosition = FormStartPosition.CenterScreen;
				TopMost = true;
				FormBorderStyle = FormBorderStyle.None;
				var progreassBar = new ProgressBar()
				{
					Style = ProgressBarStyle.Marquee,
					Size = new System.Drawing.Size(200, 20),
					Value = 40,
					ForeColor = Color.Orange,
					BackColor = Color.Purple,
					MarqueeAnimationSpeed = 40
				};
				AutoSize = true;
				AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
				//Size = progreassBar.Size;
				Controls.Add(progreassBar);
				BackColor = Color.Yellow;
				TransparencyKey = Color.Yellow;

				progreassBar.MouseDown += (snder, evtargs) =>
				{
					firstPoint = evtargs.Location;
					IsMouseDown = true;
				};
				progreassBar.MouseUp += (snder, evtargs) =>
				{
					IsMouseDown = false;
				};
				progreassBar.MouseMove += (snder, evtargs) =>
				{
					this.OnMouseMove(evtargs);
				};
			}

			private bool IsMouseDown = false;
			private Point firstPoint;
			protected override void OnMouseDown(MouseEventArgs e)
			{
				firstPoint = e.Location;
				IsMouseDown = true;
				base.OnMouseDown(e);
			}

			protected override void OnMouseUp(MouseEventArgs e)
			{
				IsMouseDown = false;
				base.OnMouseUp(e);
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				if (IsMouseDown)
				{
					// Get the difference between the two points
					int xDiff = firstPoint.X - e.Location.X;
					int yDiff = firstPoint.Y - e.Location.Y;

					// Set the new point
					int x = this.Location.X - xDiff;
					int y = this.Location.Y - yDiff;
					this.Location = new Point(x, y);
				} base.OnMouseMove(e);
			}
		}

		public ProgressForm progressForm;
		Thread thread;
		bool disposed = false; //to avoid redundant call
		public WaitIndicator()
		{
			progressForm = new ProgressForm();
			thread = new Thread(_ => progressForm.ShowDialog());
			thread.Start();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				thread.Abort();
				progressForm = null;
			}
			disposed = true;
		}
	}
}

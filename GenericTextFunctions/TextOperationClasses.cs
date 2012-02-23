using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using eisiWare;
using System.ComponentModel;
using System.Collections.ObjectModel;
using DataGridView = System.Windows.Forms.DataGridView;

namespace GenericTextFunctions
{
	/*public class DragdropObject
	{
		public string Name { get; set; }
		public TextOperations.ITextOperation TextOperation { get; set; }
		public bool HasInputControls { get { return TextOperation != null && TextOperation.InputControls != null && TextOperation.InputControls.Length > 0; } }
		public bool IsExpanded { get; set; }
		public DragdropObject(TextOperations.ITextOperation textOperation = null)
		{
			this.Name = textOperation.DisplayName;
			this.TextOperation = textOperation;
			IsExpanded = true;
		}

		private IList<DragdropObject> children;
		public IList<DragdropObject> Children
		{
			get { if (children == null) children = new ObservableCollection<DragdropObject>(); return children; }
			set { children = value; }
		}

		public DragdropObject Clone()
		{
			return new DragdropObject(TextOperation == null ? null : TextOperation.Clone());
		}
	}*/

	public class IntegerRange
	{
		public int? Start;
		public int? Length;
		private IntegerRange(int? Start, int? Length)//Only used for static Empty and Full
		{
			this.Start = Start;
			this.Length = Length;
		}
		public IntegerRange(uint Start, uint Length)
		{
			this.Start = (int)Start;
			this.Length = (int)Length;
		}

		public bool IsFull() { return Start == 0 && Length == null; }
		public bool IsEmpty() { return Start == null && Length == null; }
		public static IntegerRange Empty { get { return new IntegerRange(null, null); } }
		public static IntegerRange Full { get { return new IntegerRange(0, null); } }
	}

	public static class TextOperations
	{
		public interface ITextOperation
		{
			string DisplayName { get; }
			Control[] InputControls { get; }
			bool HasInputControls { get; }
			/// <summary>
			/// The actual processing of the text.
			/// </summary>
			/// <param name="UsedText">The reference to the string object to use.</param>
			/// <param name="textRange">The range in this string to use.</param>
			/// <param name="AdditionalObject">An additional object used in the routine.</param>
			/// <returns>Returns the resulting ranges of the text as a result of its processing.</returns>
			IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);
			ITextOperation Clone();
			IList<ITextOperation> Children { get; set; }
			bool IsExpanded { get; set; }
		}

		public abstract class TextOperation : ITextOperation
		{
			public virtual string DisplayName { get { return this.GetType().Name.InsertSpacesBeforeCamelCase(); } }
			public virtual Control[] InputControls { get { return new Control[0]; } }
			public bool HasInputControls { get { return InputControls != null && InputControls.Length > 0; } }
			public abstract IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);
			public virtual ITextOperation Clone()
			{
				TextOperation to = this.GetType().GetConstructor(new Type[0]).Invoke(new object[0]) as TextOperation;
				//Must still clone the values of the InputControls
				return to;
			}

			private IList<ITextOperation> children;
			public IList<ITextOperation> Children
			{
				get { if (children == null) children = new ObservableCollection<ITextOperation>(); return children; }
				set { children = value; }
			}

			public TextOperation()
			{
				IsExpanded = true;
			}

			public bool IsExpanded { get; set; }
		}

		public abstract class TextOperationWithDataGridView : TextOperation
		{
			public override abstract IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);

			public DataGridView dataGridView { protected get; set; }
			protected int CurrentGridColumn { get; set; }
			protected int CurrentGridRow { get; set; }
			public void SetDataGridAndProperties(ref DataGridView dataGridView, int CurrentGridColumn, int CurrentGridRow)
			{
				this.dataGridView = dataGridView;
				this.CurrentGridColumn = CurrentGridColumn;
				this.CurrentGridRow = CurrentGridRow;
			}
			public int GetNewColumnIndex() { return CurrentGridColumn; }
			public int GetNewRowIndex() { return CurrentGridRow; }
		}

		public class ForEachLine : TextOperation
		{
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				List<IntegerRange> tmpRanges = new List<IntegerRange>();

				int nextStartPos = 0;
				for (int chr = 0; chr < UsedText.Length; chr++)
				{
					if (chr < nextStartPos)
						continue;
					//if (chr > 0 && chr < usedText.Length - 1 && (usedText.Substring(chr, 2) == Environment.NewLine || chr == usedText.Length - 2))
					if (chr > 0 && (UsedText[chr] == '\n' || chr == UsedText.Length - 1))
					{
						tmpRanges.Add(new IntegerRange((uint)nextStartPos, (uint)(chr - nextStartPos)));
						IntegerRange lineRange = new IntegerRange((uint)nextStartPos, (uint)(chr - nextStartPos));

						nextStartPos = chr + 1;//2;
					}
				}

				return tmpRanges.ToArray();
			}
		}

		public class IfItContains : TextOperation
		{
			private TextBox SearchForText = new TextBox() { Name = "SearchForText", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { SearchForText }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, string InputParam)
			{
				if (
					(textRange.IsFull() ? UsedText
						: textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value))

					.Contains(SearchForText.Text))
					return new IntegerRange[] { textRange };
				else return new IntegerRange[0];
			}
		}

		public class ExtractTextRange : TextOperation
		{
			private NumericUpDown StartPosition = new NumericUpDown() { Name = "StartPosition", Width = 50, MinValue = 0 };
			private NumericUpDown Length = new NumericUpDown() { Name = "Length", Width = 50, MinValue = 0 };
			public override Control[] InputControls { get { return new Control[] { StartPosition, Length }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				return new IntegerRange[]
				{
					//TODO: textRange.Length not used here, so if Length.Value is larger than textRange.Length it will work but is actually wrong..?
					new IntegerRange((uint)(textRange.Start + StartPosition.Value), (uint)(Length.Value))
				};
			}
		}

		public class SplitUsingString : TextOperation
		{
			private TextBox SplitTextOrChar = new TextBox() { Name = "SplitTextOrChar", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { SplitTextOrChar }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				List<IntegerRange> rangeList = new List<IntegerRange>();
				int maxEndpoint = (int)(textRange.Start + textRange.Length);
				int startIndex = (int)textRange.Start;
				while (startIndex <= textRange.Start + textRange.Length)
				{
					int splitstringPos = UsedText.IndexOf(
						SplitTextOrChar.Text,
						startIndex,
						(int)(textRange.Start + textRange.Length - startIndex));
					if (splitstringPos == -1)
					{
						if ((int)(textRange.Start + textRange.Length - startIndex) > 0)
							rangeList.Add(new IntegerRange((uint)startIndex, (uint)(maxEndpoint - startIndex)));
						break;
					}
					else
					{
						rangeList.Add(new IntegerRange((uint)startIndex, (uint)(splitstringPos - startIndex)));
						startIndex = splitstringPos + 1;
					}
				}

				return rangeList.ToArray();
			}
		}

		public class Trim : TextOperation
		{
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				int tmpStartPos = textRange.Start.Value;
				int tmpEndPos = textRange.Start.Value + textRange.Length.Value;
				while (UsedText[tmpStartPos] == ' ' && tmpStartPos < textRange.Start.Value + textRange.Length)
					tmpStartPos++;
				while (UsedText[tmpEndPos] == ' ' && tmpEndPos >= textRange.Start.Value)
					tmpEndPos--;
				return new IntegerRange[] { new IntegerRange((uint)tmpStartPos, (uint)(tmpEndPos - tmpStartPos)) };
			}
		}

		public class GetPreviousLine : TextOperation
		{
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				int prevLineStartPos = -1;
				int prevLineEndPos = -1;
				for (int i = textRange.Start.Value - 1; i >= 0; i--)
				{
					if (UsedText[i] == '\n')
					{
						if (prevLineEndPos == -1)
							prevLineEndPos = i - 1;
						else
							prevLineStartPos = i + 1;
					}
					if (prevLineStartPos != -1 && prevLineEndPos != -1)
						return new IntegerRange[] { new IntegerRange((uint)prevLineStartPos, (uint)(prevLineEndPos - prevLineStartPos + 1)) };
				}
				return new IntegerRange[0];
			}
		}

		public class GetNextLine : TextOperation
		{
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				int nextLineStartPos = -1;
				int nextLineEndPos = -1;
				for (int i = (int)(textRange.Start.Value + textRange.Length); i < UsedText.Length; i++)
				{
					if (UsedText[i] == '\n')
					{
						if (nextLineStartPos == -1)
							nextLineStartPos = i + 1;
						else
							nextLineEndPos = i - 1;
					}
					if (nextLineStartPos != -1 && nextLineEndPos != -1)
						return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
				}
				return new IntegerRange[0];
			}
		}

		public class WriteCell : TextOperationWithDataGridView
		{
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				if (dataGridView.ColumnCount <= CurrentGridColumn)
					dataGridView.Columns.Add("Column" + (CurrentGridColumn + 1), "Column" + (CurrentGridColumn + 1));
				if (dataGridView.Rows.Count == 0)
					dataGridView.Rows.Add();
				dataGridView[CurrentGridColumn, CurrentGridRow].Value =
					textRange.IsFull() ? UsedText
					: textRange.IsEmpty() ? ""
					: UsedText.Substring(textRange.Start.Value, textRange.Length.Value);
				CurrentGridColumn++;
				return new IntegerRange[] { textRange };
			}
		}

		public class AdvanceNewLine : TextOperationWithDataGridView
		{
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				dataGridView.Rows.Add();
				CurrentGridRow++;
				CurrentGridColumn = 0;
				return new IntegerRange[] { textRange };
			}
		}
	}
}
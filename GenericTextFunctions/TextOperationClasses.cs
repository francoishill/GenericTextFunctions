using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using eisiWare;
using System.ComponentModel;
using System.Collections.ObjectModel;
using DataGridView = System.Windows.Forms.DataGridView;
using ITextOperation = GenericTextFunctions.TextOperations.ITextOperation;
using System.Text.RegularExpressions;

namespace GenericTextFunctions
{
	#region Extension functions
	public static class ExtensionsToTextOperations
	{
		public static bool ContainsChildInTree(this ITextOperation haystack, ref ITextOperation needle)
		{
			if (haystack == null || needle == null)
				return false;
			foreach (ITextOperation child in haystack.Children)
				if (child.ContainsChildInTree(ref needle))
					return true;
				else if (child == needle)
					return true;
			return false;
		}
	}
	#endregion Extension functions

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
			string Tooltip { get; }
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
			public const RegexOptions RegexOptionsToUse = RegexOptions.Singleline;//.Multiline;
			public virtual string DisplayName { get { return this.GetType().Name.InsertSpacesBeforeCamelCase(); } }
			public abstract string Tooltip { get; }
			public virtual Control[] InputControls { get { return new Control[0]; } }
			public bool HasInputControls { get { return InputControls != null && InputControls.Length > 0; } }
			public abstract IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);
			public virtual ITextOperation Clone()
			{
				TextOperation to = this.GetType().GetConstructor(new Type[0]).Invoke(new object[0]) as TextOperation;
				for (int i = 0; i < this.InputControls.Length; i++)
					this.InputControls[i].CopyControlValue(ref to.InputControls[i]);
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
				//if (HasInputControls)
				//    foreach (var ic in InputControls)
				//    {
				//        ic.MouseDown += (s, e) => { e.Handled = true; };
				//        ic.MouseMove += (s, e) => { e.Handled = true; };
				//        ic.PreviewMouseDown += (s, e) => { e.Handled = true; };
				//        ic.PreviewMouseMove += (s, e) => { e.Handled = true; };
				//    }
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
			public override string Tooltip { get { return "Breaks the 'current' text (of the input text) up into lines."; } }
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

		public class ForEachRegex : TextOperation
		{
			public override string Tooltip { get { return "Breaks the 'current' text up into matches of this Regular Expression."; } }
			private TextBox RegularExpression = new TextBox() { Name = "RegularExpression", MinWidth = 200 };
			public override Control[] InputControls { get { return new Control[] { RegularExpression }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				try
				{
					var matches = Regex.Matches(
							textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
							: UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
							RegularExpression.Text,
							RegexOptionsToUse);
					var ranges = new IntegerRange[matches.Count];
					for (int i = 0; i < matches.Count; i++)
						ranges[i] = new IntegerRange((uint)(textRange.Start + matches[i].Index),
							(uint)(matches[i].Length));
					return ranges;
				}
				catch (Exception exc)
				{
					TempUserMessages.ShowErrorMessage("Cannot use regular expression: " + exc.Message);
					return new IntegerRange[0];
				}
				//if (Regex.IsMatch(
				//    textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
				//        : UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
				//    RegularExpression.Text, RegexOptionsToUse))
				//    return new IntegerRange[] { textRange };
				//else return new IntegerRange[0];
			}
		}

		public class IfItContains : TextOperation
		{
			public override string Tooltip { get { return "If the 'current' text contains this text."; } }
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
			public override string Tooltip { get { return "Extract a range, if -1 is specified for Length it will extract up to the end."; } }
			private NumericUpDown StartPosition = new NumericUpDown() { Name = "StartPosition", Width = 50, MinValue = 0 };
			private NumericUpDown Length = new NumericUpDown() { Name = "Length", Width = 50, MinValue = 0 };
			public override Control[] InputControls { get { return new Control[] { StartPosition, Length }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				return new IntegerRange[]
				{
					//textRange.Length not used here, so if Length.Value is larger than
					//textRange.Length, it will work but is actually wrong..?
					new IntegerRange(
						(uint)(textRange.Start + StartPosition.Value),
						(uint)(Length.Value == -1
						? (int)(textRange.Start.Value + textRange.Length.Value - textRange.Start - StartPosition.Value)
						: Length.Value))
				};
			}
		}

		public class ExtractRegex : TextOperation
		{
			public override string Tooltip { get { return "Extract the first match of a Regular Expression in the 'current' text."; } }
			private TextBox RegularExpression = new TextBox() { Name = "RegularExpression", MinWidth = 200 };
			public override Control[] InputControls { get { return new Control[] { RegularExpression }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				try
				{

					var match = Regex.Match(
						textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
						RegularExpression.Text,
						RegexOptionsToUse);
					if (match.Success)
						return new IntegerRange[]
						{
							new IntegerRange((uint)(textRange.Start + match.Index),
							(uint)(match.Length))
						};
					else
						return new IntegerRange[0];
				}
				catch (Exception exc)
				{
					TempUserMessages.ShowErrorMessage("Cannot use regular expression: " + exc.Message);
					return new IntegerRange[0];
				}
			}
		}

		public class SplitUsingString : TextOperation
		{
			public override string Tooltip { get { return "Split the 'current' text into two strings using this text."; } }
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

		public class SplitUsingCharacters : TextOperation
		{
			public override string Tooltip { get { return "Split the 'current' text into multiple strings using these characters."; } }
			private TextBox SplitCharacters = new TextBox() { Name = "SplitCharacters", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { SplitCharacters }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				List<IntegerRange> rangeList = new List<IntegerRange>();
				int maxEndpoint = (int)(textRange.Start + textRange.Length);
				int startIndex = (int)textRange.Start;
				while (startIndex <= textRange.Start + textRange.Length)
				{
					int splitstringPos = UsedText.IndexOfAny(
						SplitCharacters.Text.ToCharArray(),
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
			public override string Tooltip { get { return "Trim of the spaces at begin&end of 'current' text."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				int tmpStartPos = textRange.Start.Value;
				int tmpEndPos = textRange.Start.Value + textRange.Length.Value - 1;
				while (UsedText[tmpStartPos] == ' ' && tmpStartPos < textRange.Start.Value + textRange.Length)
					tmpStartPos++;
				while (UsedText[tmpEndPos] == ' ' && tmpEndPos >= textRange.Start.Value)
					tmpEndPos--;
				return new IntegerRange[] { new IntegerRange((uint)tmpStartPos, (uint)(tmpEndPos - tmpStartPos + 1)) };
			}
		}

		public class GetPreviousLine : TextOperation
		{
			public override string Tooltip { get { return "Get the previous line before the 'current' text."; } }
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

		public class GetPreviousNumberOfLines : TextOperation
		{
			public override string Tooltip { get { return "Get a number of lines before the 'current' text."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				List<IntegerRange> ranges = new List<IntegerRange>();

				int startSeekPos = textRange.Start.Value - 1;
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int prevLineStartPos = -1;
					int prevLineEndPos = -1;
					for (int i = startSeekPos; i >= 0; i--)
					{
						if (UsedText[i] == '\n')
						{
							if (prevLineEndPos == -1)
								prevLineEndPos = i - 1;
							else
								prevLineStartPos = i + 1;
						}

						if (prevLineStartPos != -1 && prevLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							ranges.Add(new IntegerRange((uint)prevLineStartPos, (uint)(prevLineEndPos - prevLineStartPos + 1)));
							startSeekPos = i;
							break;
						}
					}
				}
				return ranges.ToArray();
			}
		}

		public class IfPreviousNumberOfLinesContains : TextOperation
		{
			public override string Tooltip { get { return "Checks if the previous lines (before 'current' text) contains this text. Optionally a blank can be returned if no match."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			private TextBox SearchForText = new TextBox() { Name = "SearchForText", MinWidth = 100 };
			private CheckBox ReturnBlankIfNotFound = new CheckBox() { Name = "ReturnBlankIfNotFound", IsChecked = true };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines, SearchForText, ReturnBlankIfNotFound }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, string InputParam)
			{
				int startSeekPos = textRange.Start.Value - 1;
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int prevLineStartPos = -1;
					int prevLineEndPos = -1;
					for (int i = startSeekPos; i >= 0; i--)
					{
						if (UsedText[i] == '\n')
						{
							if (prevLineEndPos == -1)
								prevLineEndPos = i - 1;
							else
								prevLineStartPos = i + 1;
						}

						if (prevLineStartPos != -1 && prevLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							if (UsedText.Substring(prevLineStartPos, prevLineEndPos - prevLineStartPos + 1).Contains(SearchForText.Text))
								return new IntegerRange[] { new IntegerRange((uint)prevLineStartPos, (uint)(prevLineEndPos - prevLineStartPos + 1)) };
						}
					}
				}

				if (ReturnBlankIfNotFound.IsChecked == true)
					return new IntegerRange[] { IntegerRange.Empty };
				else
					return new IntegerRange[0];
				//if (
				//    (textRange.IsFull() ? UsedText
				//        : textRange.IsEmpty() ? ""
				//        : UsedText.Substring(textRange.Start.Value, textRange.Length.Value))

				//    .Contains(SearchForText.Text))
				//    return new IntegerRange[] { textRange };
				//else return new IntegerRange[0];
			}
		}

		public class GetNextLine : TextOperation
		{
			public override string Tooltip { get { return "Get the next line after the 'current' text."; } }
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

		public class GetNextNumberOfLines : TextOperation
		{
			public override string Tooltip { get { return "Get the next number of lines after the 'current' text."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				List<IntegerRange> ranges = new List<IntegerRange>();

				int startSeekPos = (int)(textRange.Start + textRange.Length);
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int nextLineStartPos = -1;
					int nextLineEndPos = -1;
					for (int i = startSeekPos; i < UsedText.Length; i++)
					{
						if (UsedText[i] == '\n')
						{
							if (nextLineStartPos == -1)
								nextLineStartPos = i + 1;
							else
								nextLineEndPos = i - 1;
						}
						if (nextLineStartPos != -1 && nextLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							ranges.Add(new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)));
							startSeekPos = i;
							break;
						}
					}
				}
				return ranges.ToArray();
			}
		}

		public class IfNextNumberOfLinesContains : TextOperation
		{
			public override string Tooltip { get { return "Checks if the next number of lines (after the 'current' text) contains this text. Optionally a blank can be returned if not match."; } }
			private NumericUpDown NumberOfLines = new NumericUpDown() { Name = "NumberOfLines", Width = 50 };
			private TextBox SearchForText = new TextBox() { Name = "SearchForText", MinWidth = 100 };
			private CheckBox ReturnBlankIfNotFound = new CheckBox() { Name = "ReturnBlankIfNotFound", IsChecked = true };
			public override Control[] InputControls { get { return new Control[] { NumberOfLines, SearchForText, ReturnBlankIfNotFound }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, string InputParam)
			{
				int startSeekPos = (int)(textRange.Start + textRange.Length);
				for (int j = 0; j < NumberOfLines.Value; j++)
				{
					int nextLineStartPos = -1;
					int nextLineEndPos = -1;
					for (int i = startSeekPos; i < UsedText.Length; i++)
					{
						if (UsedText[i] == '\n')
						{
							if (nextLineStartPos == -1)
								nextLineStartPos = i + 1;
							else
								nextLineEndPos = i - 1;
						}
						if (nextLineStartPos != -1 && nextLineEndPos != -1)
						{
							//return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							if (UsedText.Substring(nextLineStartPos, nextLineEndPos - nextLineStartPos + 1).Contains(SearchForText.Text))
								return new IntegerRange[] { new IntegerRange((uint)nextLineStartPos, (uint)(nextLineEndPos - nextLineStartPos + 1)) };
							startSeekPos = i;
							break;
						}
					}
				}

				if (ReturnBlankIfNotFound.IsChecked == true)
					return new IntegerRange[] { new IntegerRange(0, 0) };
				else
					return new IntegerRange[0];
			}
		}

		public class MatchesRegularExpression : TextOperation
		{
			public override string Tooltip { get { return "Checks if the 'current' text matches a Regular Expression."; } }
			private TextBox RegularExpression = new TextBox() { Name = "RegularExpression", MinWidth = 100 };
			public override Control[] InputControls { get { return new Control[] { RegularExpression }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (Regex.IsMatch(
					textRange.IsFull() ? UsedText : textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value),
					RegularExpression.Text,
					RegexOptionsToUse))
					return new IntegerRange[] { textRange };
				else return new IntegerRange[0];
			}
		}

		public class WriteCompleteCell : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Writes the 'current' text in the 'current' cell of the Grid and then moves to the next cell."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				if (dataGridView.ColumnCount <= CurrentGridColumn)
					dataGridView.Columns.Add("Column" + (CurrentGridColumn + 1), "Column" + (CurrentGridColumn + 1));
				if (dataGridView.Rows.Count == 0)
				{
					int newRowIndex = dataGridView.Rows.Add();
					dataGridView.Rows[newRowIndex].HeaderCell.Value = (CurrentGridRow + 1).ToString();
				}
				dataGridView[CurrentGridColumn, CurrentGridRow].Value =
					textRange.IsFull() ? UsedText
					: textRange.IsEmpty() ? ""
					: UsedText.Substring(textRange.Start.Value, textRange.Length.Value >= 0 ? textRange.Length.Value : 0);
				CurrentGridColumn++;
				return new IntegerRange[] { textRange };
			}
		}

		public class WriteSameCell : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Writes the 'current' text in the 'current' cell in the Grid and stays in this cell."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				if (dataGridView.ColumnCount <= CurrentGridColumn)
					dataGridView.Columns.Add("Column" + (CurrentGridColumn + 1), "Column" + (CurrentGridColumn + 1));
				if (dataGridView.Rows.Count == 0)
				{
					int newRowIndex = dataGridView.Rows.Add();
					dataGridView.Rows[newRowIndex].HeaderCell.Value = (CurrentGridRow + 1).ToString();
				}
				dataGridView[CurrentGridColumn, CurrentGridRow].Value +=
					textRange.IsFull() ? UsedText
					: textRange.IsEmpty() ? ""
					: UsedText.Substring(textRange.Start.Value, textRange.Length.Value >= 0 ? textRange.Length.Value : 0);
				//CurrentGridColumn++;
				return new IntegerRange[] { textRange };
			}
		}

		public class WriteSameCellWithPrefix : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Writes the 'current' text (with specified prefix) in the 'current' cell in the Grid and stays in this cell."; } }
			private TextBox PrefixText = new TextBox() { Name = "PrefixText", MinWidth = 30 };
			public override Control[] InputControls { get { return new Control[] { PrefixText }; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				if (dataGridView.ColumnCount <= CurrentGridColumn)
					dataGridView.Columns.Add("Column" + (CurrentGridColumn + 1), "Column" + (CurrentGridColumn + 1));
				if (dataGridView.Rows.Count == 0)
				{
					int newRowIndex = dataGridView.Rows.Add();
					dataGridView.Rows[newRowIndex].HeaderCell.Value = (CurrentGridRow + 1).ToString();
				}
				dataGridView[CurrentGridColumn, CurrentGridRow].Value +=
					(PrefixText.Text ?? "") +
					(textRange.IsFull() ? UsedText
					: textRange.IsEmpty() ? ""
					: UsedText.Substring(textRange.Start.Value, textRange.Length.Value >= 0 ? textRange.Length.Value : 0));
				//CurrentGridColumn++;
				return new IntegerRange[] { textRange };
			}
		}

		public class GotoNextRow : TextOperationWithDataGridView
		{
			public override string Tooltip { get { return "Moves to the next row in the Grid."; } }
			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)
			{
				if (dataGridView == null)
					return new IntegerRange[] { textRange };
				int newRowIndex = dataGridView.Rows.Add();
				dataGridView.Rows[newRowIndex].HeaderCell.Value = (++CurrentGridRow + 1).ToString();
				//CurrentGridRow++;
				CurrentGridColumn = 0;
				return new IntegerRange[] { textRange };
			}
		}

		public class IfContainsThenExtractLength : TextOperation
		{
			public override string Tooltip { get { return "If the 'current' text contains this search text, then extract from where the match starts for the length specified (use -1 to extract up to end of 'current' text)."; } }
			private TextBox TextToSeek = new TextBox() { Name = "TextToSeek", Width = 50 };
			private NumericUpDown LengthToExtract = new NumericUpDown() { Name = "LengthToExtract", Width = 50, MinValue = 0 };
			public override Control[] InputControls { get { return new Control[] { TextToSeek, LengthToExtract }; } }

			public override IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				int startIndexOfTextToSeek = (textRange.IsFull() ? UsedText
						: textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value)).IndexOf(TextToSeek.Text);
				if (startIndexOfTextToSeek != -1)
					return new IntegerRange[]
					{
						//textRange.Length not used here, so if Length.Value is larger than
					//textRange.Length, it will work but is actually wrong..?
						new IntegerRange(
							(uint)(textRange.Start + startIndexOfTextToSeek),
							(uint)(LengthToExtract.Value == -1
							? (int)(textRange.Start.Value + textRange.Length.Value - textRange.Start - startIndexOfTextToSeek)
							: LengthToExtract.Value))
					};
				else
					return new IntegerRange[0];
			}
		}
	}
}
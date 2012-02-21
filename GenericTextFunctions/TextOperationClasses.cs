using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using eisiWare;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace GenericTextFunctions
{
	public enum OperationType { ForEachLine, Trim, WriteCell, GetPreviousLine, AdvanceNewLine, GenericTextOperation };
	public class DragdropObject
	{
		public OperationType operationType;
		public string Name { get; set; }
		public TextOperations.ITextOperation TextOperation { get; set; }
		public bool HasInputControls { get { return TextOperation != null && TextOperation.InputControls != null && TextOperation.InputControls.Length > 0; } }
		public bool IsExpanded { get; set; }
		public DragdropObject(OperationType operationType, TextOperations.ITextOperation textOperation = null)//, string Name, Control[] InputControls)
		{
			this.operationType = operationType;
			if (textOperation == null)
				this.Name = operationType.ToString().InsertSpacesBeforeCamelCase();
			else
			{
				this.Name = textOperation.DisplayName;
				this.TextOperation = textOperation;
			}
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
			return new DragdropObject(operationType, TextOperation == null ? null : TextOperation.Clone());
		}
	}

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

	//public class TextInLinesRange
	//{
	//    public int? LineNumber;
	//    public IntegerRange RangeOnLine;
	//    private TextInLinesRange(int? LineNumber, IntegerRange RangeOnLine = null)
	//    {
	//        this.LineNumber = LineNumber;
	//        this.RangeOnLine = RangeOnLine;
	//    }
	//    public TextInLinesRange(uint LineNumber, IntegerRange RangeOnLine)
	//    {
	//        this.LineNumber = (int)LineNumber;
	//        this.RangeOnLine = RangeOnLine;
	//    }
	//    public static TextInLinesRange Empty { get { return new TextInLinesRange(null); } }
	//}

	public static class TextOperations
	{
		public interface ITextOperation
		{
			string DisplayName { get; }
			Control[] InputControls { get; }
			IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange);
			ITextOperation Clone();
		}
		//public interface ITextOperation<T> : ITextOperation
		//{
		//    //bool HasInputControls { get; }
		//    TextInLinesRange[] ProcessText(ref string UsedText, TextInLinesRange textRange, T InputParam);
		//}

		//public abstract class TextOperation<T> : ITextOperation<T>
		//{
		//    public abstract string DisplayName { get; }

		//    public abstract Control[] InputControls { get; }

		//    public bool HasInputControls { get { return InputControls != null & InputControls.Length > 0; } }

		//    public abstract TextInLinesRange[] ProcessText(ref string[] Lines, TextInLinesRange textRange, T InputParam);
		//}

		//ForEachLine, SearchFor, ExtractTextRange, ExtractTextFrom, ForNumberOfCharacters, LookInPreviousLinesFor, LookInNextLinesFor
		//public class ForEachLine : ITextOperation<string>
		//{
		//    public string DisplayName { get { return "For each line"; } }

		//    public Control[] InputControls { get { return new Control[0]; } }

		//    public string ProcessText(string InputText)
		//    {
		//        throw new NotImplementedException();
		//    }
		//}

		public class IfItContains : ITextOperation//<string>//TextOperation<string>
		{
			public string DisplayName { get { return "If it contains"; } }

			private TextBox SearchForText = new TextBox() { Name = "SearchForText", MinWidth = 100 };
			public Control[] InputControls { get { return new Control[] { SearchForText }; } }

			public IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, string InputParam)
			{
				if (
					(textRange.IsFull() ? UsedText
						: textRange.IsEmpty() ? ""
						: UsedText.Substring(textRange.Start.Value, textRange.Length.Value))

					//UsedText.Substring(
					//textRange.Start.Value, textRange.Length.Value)

					.Contains(SearchForText.Text))
					return new IntegerRange[] { textRange };
				else return new IntegerRange[0];
			}


			public ITextOperation Clone()
			{
				return new IfItContains();
			}
		}

		public class ExtractTextRange : ITextOperation//<IntegerRange>
		{
			public string DisplayName { get { return "Extract text range"; } }

			private NumericUpDown StartPosition = new NumericUpDown() { Name = "StartPosition", Width = 50, MinValue = 0 };
			private NumericUpDown Length = new NumericUpDown() { Name = "Length", Width = 50, MinValue = 0 };
			public Control[] InputControls { get { return new Control[] { StartPosition, Length }; } }

			public IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
			{
				return new IntegerRange[]
				{
					//TODO: textRange.Length not used here, so if Length.Value is larger than textRange.Length it will work but is actually wrong..?
					new IntegerRange((uint)(textRange.Start + StartPosition.Value), (uint)(Length.Value))
				};
			}

			public ITextOperation Clone()
			{
				return new ExtractTextRange();
			}
		}

		public class SplitUsingString : ITextOperation//<IntegerRange>
		{
			public string DisplayName { get { return "Split using string"; } }

			private TextBox SplitTextOrChar = new TextBox() { Name = "SplitTextOrChar", MinWidth = 100 };
			public Control[] InputControls { get { return new Control[] { SplitTextOrChar }; } }

			public IntegerRange[] ProcessText(ref string UsedText, IntegerRange textRange)//, IntegerRange InputParam)
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

				//return new IntegerRange[]
				//{
				//    //TODO: textRange.Length not used here, so if Length.Value is larger than textRange.Length it will work but is actually wrong..?
				//    //new IntegerRange((uint)(textRange.Start + StartPosition.Value), (uint)(Length.Value))
				//};
			}

			public ITextOperation Clone()
			{
				return new SplitUsingString();
			}
		}

		//public class ExtractTextFrom : ITextOperation<string>
		//{
		//    public string DisplayName { get { return "Extract text from"; } }

		//    private TextBox StartExtractingFrom = new TextBox();
		//    public Control[] InputControls { get { return new Control[] { StartExtractingFrom }; } }

		//    public TextInLinesRange[] ProcessText(ref string UsedText, TextInLinesRange textRange, string InputParam)
		//    {
		//        throw new NotImplementedException();
		//    }
		//}
	}
}
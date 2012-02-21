using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Xml;
using eisiWare;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

namespace GenericTextFunctions
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		TextFeedbackEventHandler textFeedbackEvent;
		ObservableCollection<DragdropObject> CurrentList = new ObservableCollection<DragdropObject>();

		public MainWindow()
		{
			InitializeComponent();

			textFeedbackEvent += (s, e) =>
			{
				statusBarItem1.Content = e.FeedbackText;
			};
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			LoadDragdropItems();

			richTextBox1.LoadFile(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\GenericTextFunctions\GenericTextFunctions\ABI\ABI word files - Copy.rtf");
		}

		private void LoadDragdropItems()
		{
			listBox1.ItemsSource = DragdropObjectList;
			treeView1.ItemsSource = CurrentList;
		}

		private ObservableCollection<DragdropObject> dragdropObjectList;
		private ObservableCollection<DragdropObject> DragdropObjectList
		{
			get
			{
				if (dragdropObjectList == null)
					dragdropObjectList = new ObservableCollection<DragdropObject>();
				foreach (OperationType ot in Enum.GetValues(typeof(OperationType)))
					if (ot != OperationType.GenericTextOperation)
						dragdropObjectList.Add(new DragdropObject(ot));//operationTypesWithTextInput.Contains(ot)));

				foreach (Type to in typeof(TextOperations).GetNestedTypes())
				{
					if (to.IsClass && !to.IsAbstract)
						if (to.GetInterface(typeof(TextOperations.ITextOperation).Name) != null)//<dynamic>).Name) != null)
						{
							TextOperations.ITextOperation tmpobj = to.GetConstructor(new Type[0]).Invoke(new object[0]) as TextOperations.ITextOperation;
							dragdropObjectList.Add(new DragdropObject(OperationType.GenericTextOperation, tmpobj));
							//if (tmpobj as TextOperations.ITextOperation != null)//<string> != null)
							//    AddDragDrop<string>(tmpobj);
							//else if (tmpobj as TextOperations.ITextOperation<IntegerRange> != null)
							//    AddDragDrop<IntegerRange>(tmpobj);
							//foreach (Type t in to.GetInterfaces())
							//    if (t == typeof(TextOperations.ITextOperation<string>))
							//        dragdropObjectList.Add(new DragdropObject(
							//            OperationType.GenericTextOperation,
							//            t.Name,
							//            (t as TextOperations.ITextOperation<string>).InputControls));
						}
				}

				return dragdropObjectList;
			}
		}

		//private void AddDragDrop<T>(object obj)
		//{
		//    TextOperations.ITextOperation<T> s = (TextOperations.ITextOperation<T>)obj;
		//    dragdropObjectList.Add(new DragdropObject(OperationType.GenericTextOperation, s));
		//}
		//private List<OperationType> operationTypesWithTextInput = new List<OperationType>()
		//{
		//    OperationType.ExtractTextFrom,
		//    OperationType.ForNumberOfCharacters,
		//    OperationType.LookInNextLinesFor,
		//    OperationType.LookInPreviousLinesFor,
		//    OperationType.SearchFor
		//};

		private void MenuitemExtractISBNdata_Click(object sender, RoutedEventArgs e)
		{
			ExtractISBNdata formEID = new ExtractISBNdata();
			formEID.Show();
		}

		private void MenuitemExit_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void listBox1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			ListBox parent = (ListBox)sender;
			object data = GetDataFromItemsControl(parent, e.GetPosition(parent));

			if (data != null)
			{
				DragDrop.DoDragDrop(parent, data, DragDropEffects.Move);
			}
		}

		private void listBox1_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
		}

		private static object GetDataFromItemsControl(ItemsControl source, Point point)
		{
			UIElement element = source.InputHitTest(point) as UIElement;
			if (element != null)
			{
				object data = DependencyProperty.UnsetValue;
				while (data == DependencyProperty.UnsetValue)
				{
					data = source.ItemContainerGenerator.ItemFromContainer(element);
					if (data == DependencyProperty.UnsetValue)
					{
						element = VisualTreeHelper.GetParent(element) as UIElement;
					}
					if (element == source)
					{
						return null;
					}
				}
				if (data != DependencyProperty.UnsetValue)
				{
					return data;
				}
			}
			return null;
		}

		private void treeView1_Drop(object sender, DragEventArgs e)
		{
			if (e.Data != null && e.Data.GetDataPresent(typeof(DragdropObject).FullName))
			{
				TreeViewItem dropTarget = FindDropTreeViewItem(e);

				DragdropObject ddo = e.Data.GetData(typeof(DragdropObject).FullName) as DragdropObject;

				bool isMoving = e.AllowedEffects == DragDropEffects.Move;
				bool hasbeenRemoved = false;

				if (isMoving)
				{
					TreeViewItem originalItem = treeView1.ContainerFromItem(ddo);
					if (originalItem != null)
					{
						ItemsControl ic = GetSelectedTreeViewItemParent(originalItem);
						if (ic is TreeView)
						{
							hasbeenRemoved = CurrentList.Remove(ddo);
						}
						else
						{
							hasbeenRemoved = ((ic as TreeViewItem).Header as DragdropObject).Children.Remove(ddo);
						}
						//if (ic != null)
						//    ic.Items.Remove(ddo);
					}
				}

				DragdropObject newItem = isMoving && hasbeenRemoved ? ddo : ddo.Clone();

				if (dropTarget != null && treeView1.ContainerFromItem(ddo) != null && dropTarget == treeView1.ContainerFromItem(ddo))
				{
					TempUserMessages.ShowWarningMessage("Cannot drop an item unto itsself.");
					return;
				}

				if (dropTarget == null)
				{
					CurrentList.Add(newItem);
				}
				else
				{
					(dropTarget.Header as DragdropObject).Children.Add(newItem);
				}
			}
		}

		public ItemsControl GetSelectedTreeViewItemParent(TreeViewItem item)
		{
			DependencyObject parent = VisualTreeHelper.GetParent(item);
			while (!(parent is TreeViewItem || parent is TreeView))
			{
				parent = VisualTreeHelper.GetParent(parent);
			}

			return parent as ItemsControl;
		}

		private TreeViewItem FindDropTreeViewItem(DragEventArgs pDragEventArgs)
		{
			DependencyObject k = VisualTreeHelper.HitTest(treeView1, pDragEventArgs.GetPosition(treeView1)).VisualHit;

			while (k != null)
			{
				if (k is TreeViewItem)
				{
					return k as TreeViewItem;
				}
				k = VisualTreeHelper.GetParent(k);
			}

			return null;
		}

		private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//Otherwise the selection has gray background if focus changes
			listBox1.SelectedItem = null;
		}

		private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			//Otherwise the selection has gray background if focus changes
			//if (e.NewValue == null || !(e.NewValue is TreeViewItem))
			//    return;
			//TreeViewItem tvi = e.NewValue as TreeViewItem;
			//tvi.IsSelected = false;
		}

		private void treeView1_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			TreeView parent = (TreeView)sender;
			DragdropObject ddo = GetDataFromItemsControl(parent, e.GetPosition(parent)) as DragdropObject;
			if (ddo == null)
				return;
			(parent.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem).IsSelected = true;
			_startPoint = e.GetPosition(null);
		}

		Point _startPoint;
		bool _IsDragging = false;
		private void treeView1_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && !_IsDragging)
			{
				Point position = e.GetPosition(null);
				if (Math.Abs(position.X - _startPoint.X) >
						SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(position.Y - _startPoint.Y) >
						SystemParameters.MinimumVerticalDragDistance)
				{
					StartDrag(e);
				}
			}
		}

		private void StartDrag(MouseEventArgs e)
		{
			_IsDragging = true;
			DragdropObject ddo = this.treeView1.SelectedItem as DragdropObject;
			//DataObject data = null;

			//data = new DataObject("inadt", temp);

			if (ddo != null)
			{
				DragDropEffects dde = DragDropEffects.Move;
				if (e.RightButton == MouseButtonState.Pressed)
				{
					dde = DragDropEffects.Copy;
				}
				DragDropEffects de = DragDrop.DoDragDrop(this.treeView1, ddo, dde);
			}
			_IsDragging = false;
		}

		private void listBox1_Drop(object sender, DragEventArgs e)
		{
			//if (e.Data != null && e.Data.GetDataPresent(typeof(DragdropObject).FullName))
			//{
			//    DragdropObject ddo = e.Data.GetData(typeof(DragdropObject).FullName) as DragdropObject;
			//    treeView1.Items.Remove(ddo);
			//}

			if (e.Data != null && e.Data.GetDataPresent(typeof(DragdropObject).FullName))
			{
				DragdropObject ddo = e.Data.GetData(typeof(DragdropObject).FullName) as DragdropObject;

				TreeViewItem originalItem = treeView1.ContainerFromItem(ddo);
				if (originalItem != null)
				{
					ItemsControl ic = GetSelectedTreeViewItemParent(originalItem);
					if (ic is TreeView)
						CurrentList.Remove(ddo);
					else
						((ic as TreeViewItem).Header as DragdropObject).Children.Remove(ddo);
					//if (ic != null)
					//    ic.Items.Remove(ddo);
				}
			}
		}

		private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}

		private void treeviewItemContextmenuRemove_Click(object sender, RoutedEventArgs e)
		{
			//treeView1.Items.Remove(sender);
			System.Windows.Forms.MessageBox.Show("Function not incorporated yet.");
		}

		private void buttonProcessNow_Click(object sender, RoutedEventArgs e)
		{
			new Action(delegate
			{
				dataGrid1.Rows.Clear();
				//dataGrid1.Rows.Add();
				currentGridRow = 0;
				dataGrid1.Columns.Clear();
				currentGridColumn = 0;

				string richTextboxText = richTextBox1.Text;

				foreach (DragdropObject ddo in treeView1.Items)
				{
					TreeViewItem tvi = treeView1.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem;
					ProcessTreeViewItem(tvi, ref richTextboxText, IntegerRange.Full);
				}
			}).PerformTimedActionAndUpdateStatus(
				textFeedbackEvent,
				"Processing text, please wait...",
				"Processing completed with duration of {0} seconds",
				3000);
		}

		int currentGridRow = 0;
		int currentGridColumn = 0;
		private void ProcessTreeViewItem(TreeViewItem tvi, ref string usedText, IntegerRange textRangeToUse)
		{
			if (tvi == null) return;
			DragdropObject ddo = tvi.Header as DragdropObject;
			if (ddo == null) return;

			switch (ddo.operationType)
			{
				case OperationType.ForEachLine:
					//foreach (string line in richTextBox1.Lines)
					int nextStartPos = 0;
					for (int chr = 0; chr < usedText.Length; chr++)
					{
						if (chr < nextStartPos)
							continue;
						//if (chr > 0 && chr < usedText.Length - 1 && (usedText.Substring(chr, 2) == Environment.NewLine || chr == usedText.Length - 2))
						if (chr > 0 && (usedText[chr] == '\n' || chr == usedText.Length - 1))
						{
							//string lineText = usedText.Substring(nextStartPos, chr - 1);
							IntegerRange lineRange = new IntegerRange((uint)nextStartPos, (uint)(chr - nextStartPos));

							foreach (DragdropObject ddo1 in tvi.Items)
							{
								TreeViewItem tvi1 = tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem;
								ProcessTreeViewItem(tvi1, ref usedText, lineRange);
							}

							nextStartPos = chr + 1;//2;
						}
					}
					break;
				case OperationType.WriteCell:
					foreach (DragdropObject ddo1 in tvi.Items)
					{
						TreeViewItem tvi1 = tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem;
						ProcessTreeViewItem(tvi1, ref usedText, textRangeToUse);
					}
					if (dataGrid1.ColumnCount <= currentGridColumn)
						dataGrid1.Columns.Add("Column" + (currentGridColumn + 1), "Column" + (currentGridColumn + 1));
					if (dataGrid1.Rows.Count == 0)
						dataGrid1.Rows.Add();
					dataGrid1[currentGridColumn, currentGridRow].Value =
						textRangeToUse.IsFull() ? usedText
						: textRangeToUse.IsEmpty() ? ""
						: usedText.Substring(textRangeToUse.Start.Value, textRangeToUse.Length.Value);
					currentGridColumn++;
					break;
				case OperationType.AdvanceNewLine:
					dataGrid1.Rows.Add();
					currentGridRow++;
					currentGridColumn = 0;
					break;
				case OperationType.GenericTextOperation:
					if (ddo.TextOperation is TextOperations.ITextOperation)//<string>)
					{
						TextOperations.ITextOperation textOperation = ddo.TextOperation as TextOperations.ITextOperation;
						IntegerRange[] rangesToUse = textOperation.ProcessText(ref usedText, textRangeToUse);
						foreach (IntegerRange ir in rangesToUse)
							foreach (DragdropObject ddo1 in tvi.Items)
							{
								TreeViewItem tvi1 = tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem;
								ProcessTreeViewItem(tvi1, ref usedText, ir);
							}
					}
					break;
				default:
					break;
			}
		}

		private void MenuitemExportFile_Click(object sender, RoutedEventArgs e)
		{
			ExportFile();
		}

		private void ExportFile()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Xml files (*.xml)|*.xml";
			if (sfd.ShowDialog().Value)
			{
				using (var xw = new XmlTextWriter(sfd.FileName, System.Text.Encoding.ASCII) { Formatting = Formatting.Indented })
				{
					xw.WriteStartElement("TreeView");
					foreach (DragdropObject ddo in treeView1.Items)
						WriteTreeViewItemToXmlTextWriter(treeView1.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem, xw);
					xw.WriteEndElement();
				}
			}
		}

		private void WriteTreeViewItemToXmlTextWriter(TreeViewItem tvi, XmlTextWriter xmltextWriter)
		{
			DragdropObject ddo = tvi.Header as DragdropObject;

			xmltextWriter.WriteStartElement("TreeViewItem");
			//The name is actually only used for display messages (when importing the XML file)
			xmltextWriter.WriteAttributeString("Name", ddo.Name);
			xmltextWriter.WriteAttributeString("OperationType", ddo.operationType.ToString());
			WriteInputControlsToXmlTextWriter(ddo, xmltextWriter);
			//Must write out all the values of the InputControls
			if (ddo.TextOperation != null)
				xmltextWriter.WriteAttributeString("TypeName", ddo.TextOperation.GetType().FullName);
			foreach (DragdropObject ddo1 in tvi.Items)
				WriteTreeViewItemToXmlTextWriter(tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem, xmltextWriter);
			xmltextWriter.WriteEndElement();
		}

		private void WriteInputControlsToXmlTextWriter(DragdropObject ddo, XmlTextWriter xmltextWriter)
		{
			List<string> tmpNameList = new List<string>();
			if (ddo == null || ddo.TextOperation == null || !ddo.HasInputControls)
				return;
			foreach (Control control in ddo.TextOperation.InputControls)
			{
				string InputControlValue = "";
				if (control is TextBox)
					InputControlValue = (control as TextBox).Text;
				else if (control is NumericUpDown)
					InputControlValue = (control as NumericUpDown).Value.ToString();
				else
					TempUserMessages.ShowWarningMessage("Input control type not supported: " + control.GetType().Name);

				if (string.IsNullOrWhiteSpace(control.Name))
					TempUserMessages.ShowWarningMessage("No name found for InputControl in " + ddo.Name);
				else if (tmpNameList.Contains(control.Name))
					TempUserMessages.ShowWarningMessage("Duplicate control names found: " + control.Name);
				else
				{
					tmpNameList.Add(control.Name);
					xmltextWriter.WriteAttributeString(control.Name, InputControlValue);
				}
			}
		}

		private void MenuitemImportFile_Click(object sender, RoutedEventArgs e)
		{
			ImportFile();
		}

		private void ImportFile()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Xml files (*.xml)|*.xml";
			if (ofd.ShowDialog().Value)
			{
				if (treeView1.Items.Count == 0 || TempUserMessages.Confirm("The operation list is currently not empty and will be cleared when importing file, continue?"))
				{
					//treeView1.Items.Clear();
					CurrentList.Clear();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.Load(ofd.FileName);
					XmlNodeList tvitems = xmlDoc.SelectNodes("TreeView/TreeViewItem");
					foreach (XmlNode node in tvitems)
						AddNodeAndSubNodesToTreeviewItem(null, node);
				}
			}
		}

		private void AddNodeAndSubNodesToTreeviewItem(TreeViewItem baseTreeViewItem, XmlNode xmlnode)
		{
			string tmpName = xmlnode.Attributes["Name"].Value;
			string operationType = xmlnode.Attributes["OperationType"].Value;
			OperationType ot;
			if (string.IsNullOrWhiteSpace(tmpName))
				TempUserMessages.ShowWarningMessage("Cannot read TreeViewItem name: " + xmlnode.OuterXml);
			else if (string.IsNullOrWhiteSpace(operationType))
				TempUserMessages.ShowWarningMessage("Cannot read TreeViewItem operation type: " + xmlnode.OuterXml);
			else if (!Enum.TryParse<OperationType>(operationType, out ot))
				TempUserMessages.ShowWarningMessage("Could not obtain operation type (ENUM) from string = " + operationType);
			else
			{
				if (ot != OperationType.GenericTextOperation)
				{
					DragdropObject ddo = new DragdropObject(ot);
					if (baseTreeViewItem == null)
					{
						//treeView1.Items.Add(ddo);
						CurrentList.Add(ddo);
						treeView1.UpdateLayout();
						XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
						foreach (XmlNode node in subnodes)
							AddNodeAndSubNodesToTreeviewItem(treeView1.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem, node);
					}
					else
					{
						//baseTreeViewItem.Items.Add(ddo);
						(baseTreeViewItem.Header as DragdropObject).Children.Add(ddo);
						baseTreeViewItem.IsExpanded = true;
						baseTreeViewItem.UpdateLayout();
						XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
						foreach (XmlNode node in subnodes)
							AddNodeAndSubNodesToTreeviewItem(baseTreeViewItem.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem, node);
					}
				}
				else// if (ot == OperationType.GenericTextOperation)
				{
					string typeFullName = xmlnode.Attributes["TypeName"].Value;
					if (string.IsNullOrWhiteSpace(typeFullName))
						TempUserMessages.ShowWarningMessage("Cannot read TypeName from '" + tmpName + "' node: " + xmlnode.OuterXml);
					else
					{
						TextOperations.ITextOperation to = GetNewITextOperationFromTypeFullName(typeFullName);
						if (to == null)
							TempUserMessages.ShowWarningMessage("Could not create new object from FullName = " + typeFullName);
						else
						{
							PopulateInputControlsFromXmlNode(to, xmlnode);
							DragdropObject ddo = new DragdropObject(ot, to);

							if (baseTreeViewItem == null)
							{
								//treeView1.Items.Add(ddo);
								CurrentList.Add(ddo);
								treeView1.UpdateLayout();
								XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
								foreach (XmlNode node in subnodes)
									AddNodeAndSubNodesToTreeviewItem(treeView1.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem, node);
							}
							else
							{
								//baseTreeViewItem.Items.Add(ddo);
								(baseTreeViewItem.Header as DragdropObject).Children.Add(ddo);
								baseTreeViewItem.IsExpanded = true;
								baseTreeViewItem.UpdateLayout();
								XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
								foreach (XmlNode node in subnodes)
									AddNodeAndSubNodesToTreeviewItem(baseTreeViewItem.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem, node);
							}
						}
					}
				}
			}
		}


		private void PopulateInputControlsFromXmlNode(TextOperations.ITextOperation to, XmlNode xmlnode)
		{
			foreach (Control control in to.InputControls)
			{
				string tmpControlName = xmlnode.Attributes[control.Name].Value;
				if (string.IsNullOrWhiteSpace(tmpControlName))
					TempUserMessages.ShowWarningMessage("Could not populate control value, cannot find attribute '" + control.Name + "': " + xmlnode.OuterXml);
				else
				{
					if (control is TextBox)
						(control as TextBox).Text = tmpControlName;
					else if (control is NumericUpDown)
					{
						int intval;
						if (!int.TryParse(tmpControlName, out intval))
							TempUserMessages.ShowWarningMessage("Invalid numeric value for " + control.Name + ": " + xmlnode.OuterXml);
						else
						{
							(control as NumericUpDown).Value = intval;
						}
					}
					else
						TempUserMessages.ShowWarningMessage("Input control type not supported: " + control.GetType().Name);
				}
			}
		}

		private static TextOperations.ITextOperation GetNewITextOperationFromTypeFullName(string typeName)
		{
			foreach (Type to in typeof(TextOperations).GetNestedTypes())
			{
				if (to.IsClass && !to.IsAbstract)
					if (to.GetInterface(typeof(TextOperations.ITextOperation).Name) != null)//<dynamic>).Name) != null)
					{
						if (to.FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
						{
							return to.GetConstructor(new Type[0]).Invoke(new object[0]) as TextOperations.ITextOperation;
						}
					}
			}
			return null;
		}

		private void buttonImportFile_Click(object sender, RoutedEventArgs e)
		{
			ImportFile();
		}

		private void buttonExportFile_Click(object sender, RoutedEventArgs e)
		{
			ExportFile();
		}
	}

	#region ExtensionMethods
	public static class ExtensionMethods
	{
		/// <summary>
		/// The actionToPerform will be performed and status will be updated before and after (including the time taken).
		/// </summary>
		/// <param name="actionToPerform">The action to perform.</param>
		/// <param name="StatusToSayStarting">The status text to say the action is starting, please wait.</param>
		/// <param name="StatusToSayCompletedAddZeroParameter">The status text to say it is completed i.e. "Action completed in {0} seconds.</param>
		/*/// <param name="MakeProgressbarVisibleDuring">Whether the progressbar should be made visible during the action.</param>*/
		/// <param name="completionMessageTimeout">The timeout after how long the message showed for completion is hidden.</param>
		public static void PerformTimedActionAndUpdateStatus(this Action actionToPerform, TextFeedbackEventHandler textFeedbackEvent, string StatusToSayStarting, string StatusToSayCompletedAddZeroParameter, /*bool MakeProgressbarVisibleDuring, */int completionMessageTimeout = 0)
		{
			if (textFeedbackEvent == null)
				textFeedbackEvent = new TextFeedbackEventHandler(delegate { });

			textFeedbackEvent(actionToPerform, new TextFeedbackEventArgs(StatusToSayStarting));

			DoEvents();
			Stopwatch sw = Stopwatch.StartNew();
			actionToPerform();
			sw.Stop();
			string completionMessage = string.Format(StatusToSayCompletedAddZeroParameter, Math.Round(sw.Elapsed.TotalSeconds, 3));
			textFeedbackEvent(actionToPerform, new TextFeedbackEventArgs(completionMessage));
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
						textFeedbackEvent(actionToPerform, new TextFeedbackEventArgs(null));
					t.Dispose();
					t = null;
				};
				timer.Start();
			}
			DoEvents();
		}

		public static void DoEvents()
		{
			System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
		}

		public static string InsertSpacesBeforeCamelCase(this string s)
		{
			if (s == null) return s;
			for (int i = s.Length - 1; i >= 1; i--)
			{
				if (s[i].ToString().ToUpper() == s[i].ToString())
					s = s.Insert(i, " ");
			}
			return s;
		}

		public static TreeViewItem ContainerFromItem(this TreeView treeView, object item)
		{
			TreeViewItem containerThatMightContainItem = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(item);
			if (containerThatMightContainItem != null)
				return containerThatMightContainItem;
			else
				return ContainerFromItem(treeView.ItemContainerGenerator, treeView.Items, item);
		}

		private static TreeViewItem ContainerFromItem(ItemContainerGenerator parentItemContainerGenerator, ItemCollection itemCollection, object item)
		{
			foreach (object curChildItem in itemCollection)
			{
				TreeViewItem parentContainer = (TreeViewItem)parentItemContainerGenerator.ContainerFromItem(curChildItem);
				if (parentContainer == null)
					continue;
				TreeViewItem containerThatMightContainItem = (TreeViewItem)parentContainer.ItemContainerGenerator.ContainerFromItem(item);
				if (containerThatMightContainItem != null)
					return containerThatMightContainItem;
				TreeViewItem recursionResult = ContainerFromItem(parentContainer.ItemContainerGenerator, parentContainer.Items, item);
				if (recursionResult != null)
					return recursionResult;
			}
			return null;
		}
	}
	#endregion ExtensionMethods

	#region Converters
	public class TrueVisibleFalseCollapsedConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (bool)value ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
	#endregion Converters
}

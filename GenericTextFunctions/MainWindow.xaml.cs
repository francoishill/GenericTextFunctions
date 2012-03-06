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
using ITextOperation = GenericTextFunctions.TextOperations.ITextOperation;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Windows.Interop;
using SharedClasses;

namespace GenericTextFunctions
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		TextFeedbackEventHandler textFeedbackEvent;
		ObservableCollection<ITextOperation> CurrentList = new ObservableCollection<ITextOperation>();

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
			WindowMessagesInterop.InitializeClientMessages();

			string tmpLoadFile = @"C:\Users\francois\Documents\Visual Studio 2010\Projects\GenericTextFunctions\GenericTextFunctions\ABI\Catalogue 2012.rtf";
			if (File.Exists(tmpLoadFile))
			{
				try
				{
					richTextBox1.LoadFile(tmpLoadFile);
				}
				catch (Exception exc)
				{
					TempUserMessages.ShowWarningMessage("Unable to open file: " + exc.Message);
				}
			}

			LoadDragdropItems();
			//DONE: WTF sometimes the dock does not display (on home pc) until minimized/maximized or clicked on the client area. Was because the expander was IsExpanded=false and then items are loaded into it via LoadDragdropItems();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			WindowMessagesInterop.MessageTypes mt;
			WindowMessagesInterop.ClientHandleMessage(msg, wParam, lParam, out mt);
			if (mt == WindowMessagesInterop.MessageTypes.Show)
			{
				this.Show();
				if (this.WindowState == System.Windows.WindowState.Minimized)
					this.WindowState = System.Windows.WindowState.Normal;
				bool tmptopmost = this.Topmost;
				this.Topmost = true;
				this.Topmost = tmptopmost;
				this.Activate();
				this.UpdateLayout();
			}
			else if (mt == WindowMessagesInterop.MessageTypes.Close)
			{
				this.Close();
			}
			else if (mt == WindowMessagesInterop.MessageTypes.Hide)
			{
				this.Hide();
			}

			return IntPtr.Zero;
		}

		private void LoadDragdropItems()
		{
			listBox1.ItemsSource = DragdropObjectList;
			treeView1.ItemsSource = CurrentList;
		}

		private ObservableCollection<ITextOperation> dragdropObjectList;
		private ObservableCollection<ITextOperation> DragdropObjectList
		{
			get
			{
				if (dragdropObjectList == null)
				{
					ObservableCollection<ITextOperation> tmpList = new ObservableCollection<ITextOperation>();

					foreach (Type to in typeof(TextOperations).GetNestedTypes())
					{
						if (to.IsClass && !to.IsAbstract)
							if (to.GetInterface(typeof(TextOperations.ITextOperation).Name) != null)//<dynamic>).Name) != null)
							{
								ITextOperation tmpobj = to.GetConstructor(new Type[0]).Invoke(new object[0]) as ITextOperation;
								tmpList.Add(tmpobj);
								//dragdropObjectList.Add(new ITextOperation(tmpobj));
							}
					}

					var sortedOC = from item in tmpList
								   orderby item.DisplayName
								   select item;

					dragdropObjectList = new ObservableCollection<ITextOperation>(sortedOC);
					tmpList = null;
					sortedOC = null;
				}

				return dragdropObjectList;
			}
		}

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

		private bool HasITextOperationInterface(Type typeToCheck)
		{
			if (typeToCheck == null)
				return false;
			Type[] interfaces = typeToCheck.GetInterfaces();
			foreach (Type i in interfaces)
				if (i.FullName.Equals(typeof(ITextOperation).FullName, StringComparison.InvariantCultureIgnoreCase))
					return true;
			return false;
		}

		private void treeView1_Drop(object sender, DragEventArgs e)
		{
			//if (e.Data != null && e.Data.GetDataPresent(typeof(ITextOperation).FullName))
			Type dataType = Assembly.GetExecutingAssembly().GetType(e.Data.GetFormats()[0]);//TODO: Must also support more than one format
			if (e.Data != null && dataType != null && HasITextOperationInterface(dataType))
			{
				ITextOperation dropTarget = FindDropTreeViewItem(e) == null ? null : FindDropTreeViewItem(e).Header as ITextOperation;

				ITextOperation droppedItem = e.Data.GetData(e.Data.GetFormats()[0]) as ITextOperation;//TODO: Must also support more than one format

				bool isMoving = e.AllowedEffects == DragDropEffects.Move;
				bool hasbeenRemoved = false;

				//TODO: Must also check if the the droppedItem is not dropped unto one of its own children
				if (droppedItem != null && droppedItem.ContainsChildInTree(ref dropTarget))
				{
					TempUserMessages.ShowWarningMessage("Cannot drop unto one of its own children.");
					return;
				}
				if (isMoving)
				{
					//if (dropTarget != null && treeView1.ContainerFromItem(droppedItem) != null && dropTarget == treeView1.ContainerFromItem(droppedItem))
					if (dropTarget != null && droppedItem != null && dropTarget == droppedItem)
					{
						TempUserMessages.ShowWarningMessage("Cannot drop an item unto itsself.");
						return;
					}

					TreeViewItem originalItem = treeView1.ContainerFromItem(droppedItem);
					if (originalItem != null)
					{
						ItemsControl ic = GetSelectedTreeViewItemParent(originalItem);
						if (ic is TreeView)
						{
							hasbeenRemoved = CurrentList.Remove(droppedItem);
						}
						else
						{
							hasbeenRemoved = ((ic as TreeViewItem).Header as ITextOperation).Children.Remove(droppedItem);
						}
						//if (ic != null)
						//    ic.Items.Remove(ddo);
					}
				}

				ITextOperation newItem = isMoving && hasbeenRemoved ? droppedItem : droppedItem.Clone();

				if (dropTarget == null)
				{
					CurrentList.Add(newItem);
				}
				else
				{
					//(dropTarget.Header as ITextOperation).Children.Add(newItem);
					dropTarget.Children.Add(newItem);
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
			ITextOperation ddo = GetDataFromItemsControl(parent, e.GetPosition(parent)) as ITextOperation;
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
			ITextOperation ddo = this.treeView1.SelectedItem as ITextOperation;
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

			//if (e.Data != null && e.Data.GetDataPresent(typeof(ITextOperation).FullName))
			Type dataType = Assembly.GetExecutingAssembly().GetType(e.Data.GetFormats()[0]);//TODO: Must also support more than one format
			if (e.Data != null && dataType != null && HasITextOperationInterface(dataType))
			{
				//ITextOperation ddo = e.Data.GetData(typeof(ITextOperation).FullName) as ITextOperation;
				ITextOperation ddo = e.Data.GetData(e.Data.GetFormats()[0]) as ITextOperation;//TODO: Must also support more than one format

				TreeViewItem originalItem = treeView1.ContainerFromItem(ddo);
				if (originalItem != null)
				{
					ItemsControl ic = GetSelectedTreeViewItemParent(originalItem);
					if (ic is TreeView)
						CurrentList.Remove(ddo);
					else
						((ic as TreeViewItem).Header as ITextOperation).Children.Remove(ddo);
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

		private bool IsBusyProcess = false;
		private void buttonProcessNow_Click(object sender, RoutedEventArgs e)
		{
			if (IsBusyProcess)
				TempUserMessages.ShowWarningMessage("Already busy processing, please wait for it to finish.");
			else
			{
				IsBusyProcess = true;
				System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode tmpRowHeaderSizeMode = dataGrid1.RowHeadersWidthSizeMode;
				dataGrid1.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
				dataGrid1.SuspendLayout();
				new Action(delegate
				{
					dataGrid1.Rows.Clear();
					//dataGrid1.Rows.Add();
					currentGridRow = 0;
					dataGrid1.Columns.Clear();
					currentGridColumn = 0;

					string richTextboxText = richTextBox1.Text;

					foreach (ITextOperation ddo in treeView1.Items)
					{
						TreeViewItem tvi = treeView1.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem;
						ProcessTreeViewItem(tvi, ref richTextboxText, IntegerRange.Full);
					}
				}).PerformTimedActionAndUpdateStatus(
					textFeedbackEvent,
					"Processing text, please wait...",
					"Processing completed with duration of {0} seconds. Total row count = {1}",
					7000,
					new Func<string>(() => (dataGrid1.RowCount - 1).ToString()));
				dataGrid1.ResumeLayout();
				dataGrid1.RowHeadersWidthSizeMode = tmpRowHeaderSizeMode;
				IsBusyProcess = false;
			}
		}

		int currentGridRow = 0;
		int currentGridColumn = 0;
		private void ProcessTreeViewItem(TreeViewItem tvi, ref string usedText, IntegerRange textRangeToUse)
		{
			if (tvi == null) return;
			ITextOperation textop = tvi.Header as ITextOperation;
			if (textop == null) return;

			if (textop is TextOperations.ITextOperation)//<string>)
			{
				TextOperations.ITextOperation textOperation = textop as TextOperations.ITextOperation;
				TextOperations.TextOperationWithDataGridView toWithDg = textOperation as TextOperations.TextOperationWithDataGridView;

				if (toWithDg != null)
					toWithDg.SetDataGridAndProperties(ref dataGrid1, currentGridColumn, currentGridRow);

				IntegerRange[] rangesToUse = textOperation.ProcessText(ref usedText, textRangeToUse);

				if (toWithDg != null)
				{
					currentGridColumn = toWithDg.GetNewColumnIndex();
					currentGridRow = toWithDg.GetNewRowIndex();
				}

				foreach (IntegerRange ir in rangesToUse)
					foreach (ITextOperation ddo1 in tvi.Items)
					{
						TreeViewItem tvi1 = tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem;
						ProcessTreeViewItem(tvi1, ref usedText, ir);
					}
			}
		}

		private void MenuitemExportProcessFile_Click(object sender, RoutedEventArgs e)
		{
			ExportProcessFile();
		}

		private void ExportProcessFile()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Xml files (*.xml)|*.xml";
			if (sfd.ShowDialog().Value)
			{
				using (var xw = new XmlTextWriter(sfd.FileName, System.Text.Encoding.ASCII) { Formatting = Formatting.Indented })
				{
					xw.WriteStartElement("TreeView");
					foreach (ITextOperation ddo in treeView1.Items)
						WriteTreeViewItemToXmlTextWriter(treeView1.ItemContainerGenerator.ContainerFromItem(ddo) as TreeViewItem, xw);
					xw.WriteEndElement();
				}
			}
		}

		private void WriteTreeViewItemToXmlTextWriter(TreeViewItem tvi, XmlTextWriter xmltextWriter)
		{
			ITextOperation textop = tvi.Header as ITextOperation;

			xmltextWriter.WriteStartElement("TreeViewItem");
			xmltextWriter.WriteAttributeString("DisplayName", textop.DisplayName);
			WriteInputControlsToXmlTextWriter(textop, xmltextWriter);
			if (textop != null)
				xmltextWriter.WriteAttributeString("TypeName", textop.GetType().FullName);
			foreach (ITextOperation ddo1 in tvi.Items)
				WriteTreeViewItemToXmlTextWriter(tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem, xmltextWriter);
			xmltextWriter.WriteEndElement();
		}

		private void WriteInputControlsToXmlTextWriter(ITextOperation textop, XmlTextWriter xmltextWriter)
		{
			List<string> tmpNameList = new List<string>();
			if (textop == null || !textop.HasInputControls)
				return;
			foreach (Control control in textop.InputControls)
			{
				string InputControlValue = "";
				if (control is TextBox)
					InputControlValue = (control as TextBox).Text;
				else if (control is NumericUpDown)
					InputControlValue = (control as NumericUpDown).Value.ToString();
				else if (control is CheckBox)
					InputControlValue = (control as CheckBox).IsChecked == null ? false.ToString() : (control as CheckBox).IsChecked.Value.ToString();
				else
					TempUserMessages.ShowWarningMessage("Input control type not supported: " + control.GetType().Name);

				if (string.IsNullOrWhiteSpace(control.Name))
					TempUserMessages.ShowWarningMessage("No name found for InputControl in " + textop.DisplayName);
				else if (tmpNameList.Contains(control.Name))
					TempUserMessages.ShowWarningMessage("Duplicate control names found: " + control.Name);
				else
				{
					tmpNameList.Add(control.Name);
					xmltextWriter.WriteAttributeString(control.Name, InputControlValue);
				}
			}
		}

		private void MenuitemImportProcessFile_Click(object sender, RoutedEventArgs e)
		{
			ImportProcessFile();
		}

		private void ImportProcessFile()
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
			string tmpName = xmlnode.Attributes["DisplayName"].Value;
			if (string.IsNullOrWhiteSpace(tmpName))
				TempUserMessages.ShowWarningMessage("Cannot read TreeViewItem name: " + xmlnode.OuterXml);
			else
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
						//ITextOperation ddo = new ITextOperation(to);

						if (baseTreeViewItem == null)
						{
							//treeView1.Items.Add(ddo);
							CurrentList.Add(to);
							treeView1.UpdateLayout();
							XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
							foreach (XmlNode node in subnodes)
								AddNodeAndSubNodesToTreeviewItem(treeView1.ItemContainerGenerator.ContainerFromItem(to) as TreeViewItem, node);
						}
						else
						{
							//baseTreeViewItem.Items.Add(ddo);
							(baseTreeViewItem.Header as ITextOperation).Children.Add(to);
							baseTreeViewItem.IsExpanded = true;
							baseTreeViewItem.UpdateLayout();
							XmlNodeList subnodes = xmlnode.SelectNodes("TreeViewItem");
							foreach (XmlNode node in subnodes)
								AddNodeAndSubNodesToTreeviewItem(baseTreeViewItem.ItemContainerGenerator.ContainerFromItem(to) as TreeViewItem, node);
						}
					}
				}
			}
		}


		private void PopulateInputControlsFromXmlNode(TextOperations.ITextOperation to, XmlNode xmlnode)
		{
			foreach (Control control in to.InputControls)
			{
				string tmpControlValue = xmlnode.Attributes[control.Name].Value;
				//if (string.IsNullOrWhiteSpace(tmpControlName))
				if (string.IsNullOrEmpty(tmpControlValue))//Do not use IsNullOrWhiteSpace otherwise if for instance the SplitUsingString textbox value was " " it will warn
					TempUserMessages.ShowWarningMessage("Could not populate control value, cannot find attribute '" + control.Name + "': " + xmlnode.OuterXml);
				else
				{
					if (control is TextBox)
						(control as TextBox).Text = tmpControlValue;
					else if (control is NumericUpDown)
					{
						int intval;
						if (!int.TryParse(tmpControlValue, out intval))
							TempUserMessages.ShowWarningMessage("Invalid numeric value for " + control.Name + ": " + xmlnode.OuterXml);
						else
						{
							(control as NumericUpDown).Value = intval;
						}
					}
					else if (control is CheckBox)
					{
						bool boolval;
						if (!bool.TryParse(tmpControlValue, out boolval))
						{
							TempUserMessages.ShowWarningMessage("Invalid string for checkbox checked (boolean) value: '" + tmpControlValue + "', will use false");
							(control as CheckBox).IsChecked = false;
						}
						else
							(control as CheckBox).IsChecked = boolval;
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
			ImportProcessFile();
		}

		private void buttonExportFile_Click(object sender, RoutedEventArgs e)
		{
			ExportProcessFile();
		}

		private void listBox1_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;//Becuase if the user holds down right button and move over to treeview, it starts dragging the first treeview item
		}

		private void MenuitemSaveLayout_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Title = "Select a filename for saving the layout";
			sfd.Filter = "Layout files (*.lof)|*.lof";
			if (sfd.ShowDialog(this) == true)
				dockingManager1.SaveLayout(sfd.FileName);
		}

		private void MenuitemLoadLayout_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Select a filename to load the layout from";
			ofd.Filter = "Layout files (*.lof)|*.lof";
			if (ofd.ShowDialog(this) == true)
				dockingManager1.RestoreLayout(ofd.FileName);
		}

		private void MenuitemOpenInfoFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Select a file to open with the source info";
			ofd.Filter = "Rich text files (*.rtf)|*.rtf";
			if (ofd.ShowDialog(this) == true)
			{
				try
				{
					richTextBox1.LoadFile(ofd.FileName);
				}
				catch (Exception exc)
				{
					TempUserMessages.ShowWarningMessage("Unable to open file: " + exc.Message);
				}
			}
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
		public static void PerformTimedActionAndUpdateStatus(this Action actionToPerform, TextFeedbackEventHandler textFeedbackEvent, string StatusToSayStarting, string StatusToSayCompletedAddZeroParameter, /*bool MakeProgressbarVisibleDuring, */int completionMessageTimeout = 0, params Func<string>[] AdditionalArguments)
		{
			if (textFeedbackEvent == null)
				textFeedbackEvent = new TextFeedbackEventHandler(delegate { });

			textFeedbackEvent(actionToPerform, new TextFeedbackEventArgs(StatusToSayStarting));

			DoEvents();
			Stopwatch sw = Stopwatch.StartNew();
			actionToPerform();
			sw.Stop();
			//string completionMessage = string.Format(StatusToSayCompletedAddZeroParameter, Math.Round(sw.Elapsed.TotalSeconds, 3));
			string completionMessage = StatusToSayCompletedAddZeroParameter.Replace("{0}", Math.Round(sw.Elapsed.TotalSeconds, 3).ToString());
			for (int i = 0; i < AdditionalArguments.Length; i++)
				completionMessage = completionMessage.Replace("{" + (i + 1) + "}", AdditionalArguments[i]());
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

		/// <summary>
		/// Recursively checks for a treeview Item
		/// </summary>
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

		public static void CopyControlValue(this Control control, ref Control otherControl)
		{
			if (control == null || otherControl == null)
				return;
			if (control.GetType() != otherControl.GetType())
				return;

			if (control is TextBox)
				(otherControl as TextBox).Text = (control as TextBox).Text;
			else if (control is NumericUpDown)
				(otherControl as NumericUpDown).Value = (control as NumericUpDown).Value;
			else if (control is CheckBox)
				(otherControl as CheckBox).IsChecked = (control as CheckBox).IsChecked;
			else
				TempUserMessages.ShowWarningMessage(string.Format("Currently control of type '{0}' is currently not supported in cloning."));
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

	public class AddToConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if ((!(value is double) && !(value is int))
				|| (!(parameter is double) && !(parameter is int)))
			{
				double tmpdouble;
				if (parameter is string && double.TryParse(parameter as string, out tmpdouble))
					parameter = tmpdouble;
				else
					return value;
			}

			if (value is double)
				return (double)((double)value + (parameter is double ? (double)parameter : (int)parameter));
			else// if (value is int)
				return (int)((int)value + (parameter is double ? (double)parameter : (int)parameter));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class SubtractFromConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if ((!(value is double) && !(value is int))
				|| (!(parameter is double) && !(parameter is int)))
			{
				double tmpdouble;
				if (parameter is string && double.TryParse(parameter as string, out tmpdouble))
					parameter = tmpdouble;
				else
					return value;
			}

			if (value is double)
				return (double)((double)value - (parameter is double ? (double)parameter : (int)parameter));
			else// if (value is int)
				return (int)((int)value - (parameter is double ? (double)parameter : (int)parameter));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
	#endregion Converters
}

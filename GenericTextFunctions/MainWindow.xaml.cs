﻿using System.Collections.ObjectModel;
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
			Type type = Assembly.GetExecutingAssembly().GetType("GenericTextFunctions.TextOperations+IfItContains");


			LoadDragdropItems();

			richTextBox1.LoadFile(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\GenericTextFunctions\GenericTextFunctions\ABI\ABI word files - Copy.rtf");
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
					dragdropObjectList = new ObservableCollection<ITextOperation>();

				foreach (Type to in typeof(TextOperations).GetNestedTypes())
				{
					if (to.IsClass && !to.IsAbstract)
						if (to.GetInterface(typeof(TextOperations.ITextOperation).Name) != null)//<dynamic>).Name) != null)
						{
							ITextOperation tmpobj = to.GetConstructor(new Type[0]).Invoke(new object[0]) as ITextOperation;
							dragdropObjectList.Add(tmpobj);
							//dragdropObjectList.Add(new ITextOperation(tmpobj));
						}
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
				TreeViewItem dropTarget = FindDropTreeViewItem(e);

				ITextOperation ddo = e.Data.GetData(e.Data.GetFormats()[0]) as ITextOperation;//TODO: Must also support more than one format

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
							hasbeenRemoved = ((ic as TreeViewItem).Header as ITextOperation).Children.Remove(ddo);
						}
						//if (ic != null)
						//    ic.Items.Remove(ddo);
					}
				}

				ITextOperation newItem = isMoving && hasbeenRemoved ? ddo : ddo.Clone();

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
					(dropTarget.Header as ITextOperation).Children.Add(newItem);
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

			if (e.Data != null && e.Data.GetDataPresent(typeof(ITextOperation).FullName))
			{
				ITextOperation ddo = e.Data.GetData(typeof(ITextOperation).FullName) as ITextOperation;

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

				foreach (ITextOperation ddo in treeView1.Items)
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
			//The name is actually only used for display messages (when importing the XML file)
			xmltextWriter.WriteAttributeString("Name", textop.DisplayName);
			WriteInputControlsToXmlTextWriter(textop, xmltextWriter);
			//Must write out all the values of the InputControls
			if (textop != null)
				xmltextWriter.WriteAttributeString("TypeName", textop.GetType().FullName);
			foreach (ITextOperation ddo1 in tvi.Items)
				WriteTreeViewItemToXmlTextWriter(tvi.ItemContainerGenerator.ContainerFromItem(ddo1) as TreeViewItem, xmltextWriter);
			xmltextWriter.WriteEndElement();
		}

		private void WriteInputControlsToXmlTextWriter(ITextOperation textop, XmlTextWriter xmltextWriter)
		{
			List<string> tmpNameList = new List<string>();
			if (textop == null  || !textop.HasInputControls)
				return;
			foreach (Control control in textop.InputControls)
			{
				string InputControlValue = "";
				if (control is TextBox)
					InputControlValue = (control as TextBox).Text;
				else if (control is NumericUpDown)
					InputControlValue = (control as NumericUpDown).Value.ToString();
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
				string tmpControlName = xmlnode.Attributes[control.Name].Value;
				//if (string.IsNullOrWhiteSpace(tmpControlName))
				if (string.IsNullOrEmpty(tmpControlName))//Do not use IsNullOrWhiteSpace otherwise if for instance the SplitUsingString textbox value was " " it will warn
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

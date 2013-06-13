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
using ITextOperation = SharedClasses.TextOperations.ITextOperation;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Windows.Interop;
using SharedClasses;
using AvalonDock.Layout.Serialization;
using AvalonDock.Layout;
using NUnit.Framework;

namespace GenericTextFunctions
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		TextFeedbackEventHandler textFeedbackEvent;
		ObservableCollection<ITextOperation> CurrentList = new ObservableCollection<ITextOperation>();
		Timer timerVersionString;

		public MainWindow()
		{
			//Build the following into this app:
			//1. tickbox to automatically pick up clipboard changes, paste inside "Input rich text" block
			//Also have tickbox to say automatically process on pasted (so only if application is active)

			InitializeComponent();

			timerVersionString = new Timer(
			delegate
			{
				if (App.CurrentVersionString != null)
				{
					this.Dispatcher.Invoke((Action)delegate
					{
						this.Title += " (up to date version " + App.CurrentVersionString + ")";
						timerVersionString.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
						timerVersionString.Dispose();
						timerVersionString = null;
					});
				}
				else if (App.CannotCheckVersionError != null)
				{
					this.Dispatcher.Invoke((Action)delegate
					{
						this.Title += " (" + App.CurrentVersionString + ")";
						try
						{
							timerVersionString.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
							timerVersionString.Dispose();
							timerVersionString = null;
						}
						catch { }
					});
				}
			},
			null,
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1));

			textFeedbackEvent += (s, e) =>
			{
				statusBarItem1.Content = e.FeedbackText;
			};
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//StartPipeClient();
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

			HwndSource source = (HwndSource)PresentationSource.FromDependencyObject(this);
			source.AddHook(WindowProc);
		}

		private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			WindowMessagesInterop.MessageTypes mt;
			WindowMessagesInterop.ClientHandleMessage(msg, wParam, lParam, out mt);
			if (mt == WindowMessagesInterop.MessageTypes.Show)
				this.Show();
			else if (mt == WindowMessagesInterop.MessageTypes.Hide)
				this.Hide();
			else if (mt == WindowMessagesInterop.MessageTypes.Close)
			{
				this.Close();
			}
			return IntPtr.Zero;
		}

		/*NamedPipesInterop.NamedPipeClient pipeclient;
		private void StartPipeClient()
		{
			pipeclient = NamedPipesInterop.NamedPipeClient.StartNewPipeClient(
				ActionOnError: (e) => { Console.WriteLine("Error occured: " + e.GetException().Message); },
				ActionOnMessageReceived: (m) =>
				{
					if (m.MessageType == PipeMessageTypes.AcknowledgeClientRegistration)
						Console.WriteLine("Client successfully registered.");
					else
					{
						if (m.MessageType == PipeMessageTypes.Show)
							Dispatcher.BeginInvoke((Action)delegate
							{
								this.Show();
								if (this.WindowState == System.Windows.WindowState.Minimized)
									this.WindowState = System.Windows.WindowState.Normal;
								bool tmptopmost = this.Topmost;
								this.Topmost = true;
								this.Topmost = tmptopmost;
								this.Activate();
								this.UpdateLayout();
							});
						else if (m.MessageType == PipeMessageTypes.Hide)
							Dispatcher.BeginInvoke((Action)delegate { this.Hide(); });
						else if (m.MessageType == PipeMessageTypes.Close)
							Dispatcher.BeginInvoke((Action)delegate { this.Close(); });
					}
				});
			this.Closing += delegate { if (pipeclient != null) { pipeclient.ForceCancelRetryLoop = true; } };
		}*/

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
			Type dataType = Assembly.GetExecutingAssembly().GetType(e.Data.GetFormats()[0]);
			if (e.Data != null && dataType != null && HasITextOperationInterface(dataType))
			{
				ITextOperation dropTarget = FindDropTreeViewItem(e) == null ? null : FindDropTreeViewItem(e).Header as ITextOperation;

				ITextOperation droppedItem = e.Data.GetData(e.Data.GetFormats()[0]) as ITextOperation;

				bool isMoving = e.AllowedEffects == DragDropEffects.Move;
				bool hasbeenRemoved = false;

				//Must also check if the the droppedItem is not dropped unto one of its own children
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
			try
			{
				if (ddo == null)
					return;

				if (ddo.HasInputControls)
					foreach (var ic in ddo.InputControls)
						if (ic.IsMouseCaptureWithin)
							return;

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
			}
			finally
			{
				_IsDragging = false;
			}
		}

		private void listBox1_Drop(object sender, DragEventArgs e)
		{
			//if (e.Data != null && e.Data.GetDataPresent(typeof(DragdropObject).FullName))
			//{
			//    DragdropObject ddo = e.Data.GetData(typeof(DragdropObject).FullName) as DragdropObject;
			//    treeView1.Items.Remove(ddo);
			//}

			//if (e.Data != null && e.Data.GetDataPresent(typeof(ITextOperation).FullName))
			Type dataType = Assembly.GetExecutingAssembly().GetType(e.Data.GetFormats()[0]);
			if (e.Data != null && dataType != null && HasITextOperationInterface(dataType))
			{
				//ITextOperation ddo = e.Data.GetData(typeof(ITextOperation).FullName) as ITextOperation;
				ITextOperation ddo = e.Data.GetData(e.Data.GetFormats()[0]) as ITextOperation;

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
			TextOperationsUI.ProcessInputTextToGrid(this.richTextBox1.Text, /*this.treeView1*/CurrentList, this.dataGrid1, this.textFeedbackEvent);
		}

		private void MenuitemExportProcessFile_Click(object sender, RoutedEventArgs e)
		{
			TextOperationsUI.ExportProcessFile(CurrentList, treeView1);
		}

		private void MenuitemImportProcessFile_Click(object sender, RoutedEventArgs e)
		{
			string tmpurl;
			TextOperationsUI.ImportProcessFile(CurrentList, treeView1, out tmpurl);
		}

		private void buttonImportFile_Click(object sender, RoutedEventArgs e)
		{
			string tmpurl;
			TextOperationsUI.ImportProcessFile(CurrentList, treeView1, out tmpurl);
		}

		private void buttonExportFile_Click(object sender, RoutedEventArgs e)
		{
			TextOperationsUI.ExportProcessFile(CurrentList, treeView1);
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
			{
				//dockingManager1.SaveLayout(sfd.FileName);
				var serializer = new XmlLayoutSerializer(dockingManager1);
				using (var stream = new StreamWriter(sfd.FileName))
					serializer.Serialize(stream);
			}
		}

		private void MenuitemLoadLayout_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Select a filename to load the layout from";
			ofd.Filter = "Layout files (*.lof)|*.lof";
			if (ofd.ShowDialog(this) == true)
			{
				var currentContentsList = dockingManager1.Layout.Descendents().OfType<LayoutContent>().Where(c => c.ContentId != null).ToArray();

				string fileName = (sender as MenuItem).Header.ToString();
				var serializer = new XmlLayoutSerializer(dockingManager1);
				//serializer.LayoutSerializationCallback += (s, args) =>
				//    {
				//        var prevContent = currentContentsList.FirstOrDefault(c => c.ContentId == args.Model.ContentId);
				//        if (prevContent != null)
				//            args.Content = prevContent.Content;
				//    };
				using (var stream = new StreamReader(ofd.FileName))
					serializer.Deserialize(stream);
				//dockingManager1.RestoreLayout(ofd.FileName);
			}
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

		ScaleTransform originalScale = new ScaleTransform(1, 1);
		ScaleTransform smallScale = new ScaleTransform(0.2, 0.2);
		private void Window_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//if (e.ChangedButton == MouseButton.Middle)
			if (Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				ToggleBetweenNormalAndSmall();
			}
		}

		private bool IsSmall()
		{
			return mainDockPanel.LayoutTransform == smallScale;
		}

		private void ToggleBetweenNormalAndSmall()
		{
			if (IsSmall())//Make big again
				MakeNormalSize();
			else//Make small
				MakeSmall();
		}

		private void MakeSmall()
		{
			detectClipboardChangesAndAutoProcess = true;
			mainDockPanel.LayoutTransform = smallScale;
			this.Width = this.ActualWidth * smallScale.ScaleX;
			this.Height = this.ActualHeight * smallScale.ScaleY;
			this.WindowState = System.Windows.WindowState.Normal;
			//this.WindowStyle = System.Windows.WindowStyle.None;
			expander1.Visibility = System.Windows.Visibility.Collapsed;
			treeView1.IsEnabled = false;
			richTextBox1.Enabled = false;
			this.Topmost = true;
			this.UpdateLayout();
		}

		private void MakeNormalSize()
		{
			detectClipboardChangesAndAutoProcess = false;
			mainDockPanel.LayoutTransform = originalScale;
			this.WindowState = System.Windows.WindowState.Maximized;
			//this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
			expander1.Visibility = System.Windows.Visibility.Visible;
			treeView1.IsEnabled = true;
			richTextBox1.Enabled = true;
			this.Topmost = false;
			this.UpdateLayout();
		}

		bool detectClipboardChangesAndAutoProcess = false;
		string lastClipboard = null;
		private void Window_Activated(object sender, EventArgs e)
		{
			var clipboardText = Clipboard.GetText();
			if (clipboardText != lastClipboard && !string.IsNullOrEmpty(clipboardText))
			{
				if (detectClipboardChangesAndAutoProcess)
				{
					//textBoxSearchText.Text = clipboardText;
					//SearchText = clipboardText;
					//textBoxSearchText.Focus();
					//labelStatusbar.Text = "Pasted text into search box: " + clipboardText;
					richTextBox1.SelectionFont = new System.Drawing.Font("Arial", 10);
					richTextBox1.Font = richTextBox1.SelectionFont;
					richTextBox1.Text = clipboardText;
					TextOperationsUI.ProcessInputTextToGrid(this.richTextBox1.Text, /*this.treeView1*/ CurrentList, this.dataGrid1, this.textFeedbackEvent);
				}
			}
			lastClipboard = clipboardText;

			//ShowNoCallbackNotificationInterop.ShowNotificationNoCallback_UsingExternalApp(
			//    (err) => UserMessages.ShowErrorMessage(err),
			//    "Activated");
		}

		private void Window_Deactivated(object sender, EventArgs e)
		{
			lastClipboard = Clipboard.GetText();
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			if (this.WindowState == System.Windows.WindowState.Maximized && IsSmall())
				MakeNormalSize();
		}

		private void LayoutAnchorable_Closed(object sender, EventArgs e)
		{
			((LayoutAnchorable)sender).Show();
		}

		private void menuitemAbout_Click(object sender, RoutedEventArgs e)
		{
			AboutWindow2.ShowAboutWindow(new System.Collections.ObjectModel.ObservableCollection<DisplayItem>()
			{
				new DisplayItem("Author", "Francois Hill"),
				new DisplayItem("Icon(s) obtained from", null)
			});
		}
	}

	#region ExtensionMethods
	public static class ExtensionMethods
	{
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

	#region Tests using NUnit
	[TestFixture]
	public class TestingWithNUnit
	{
		MainWindow mainwindow;
		[SetUp]
		public void SetupTests()
		{
			mainwindow = new MainWindow();
			mainwindow.Show();
		}
		[Test]
		public void TestInputOutput()
		{
			mainwindow.richTextBox1.Text =
				string.Join(Environment.NewLine, "Line 1", "Line 2", "Line 3", "Line 4");
			List<ITextOperation> list = new List<ITextOperation>();
			foreach (var obj in mainwindow.treeView1.Items)
				list.Add(obj as ITextOperation);

			TextOperationsUI.ProcessInputTextToGrid(mainwindow.richTextBox1.Text, /*mainwindow.treeView1*/list, mainwindow.dataGrid1, null);
		}
		[TearDown]
		public void TeardownTests()
		{
			if (mainwindow.IsVisible)
				mainwindow.Close();
			mainwindow = null;
		}
	}
	#endregion Tests using NUnit
}

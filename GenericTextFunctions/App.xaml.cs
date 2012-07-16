using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GenericTextFunctions
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static string CurrentVersionString = null;

		protected override void OnStartup(StartupEventArgs e)
		{
			System.Windows.Forms.Application.EnableVisualStyles();
			SharedClasses.AutoUpdatingForm.CheckForUpdates(
				exitApplicationAction: delegate { Dispatcher.Invoke((Action)delegate { this.Shutdown(); }); },
				ActionIfUptoDate_Versionstring: (versionstring) => { CurrentVersionString = versionstring; });
			base.OnStartup(e);
		}
	}
}
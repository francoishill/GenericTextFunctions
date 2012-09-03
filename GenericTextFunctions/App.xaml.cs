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
		public static string CannotCheckVersionError = null;

		protected override void OnStartup(StartupEventArgs e)
		{
			System.Windows.Forms.Application.EnableVisualStyles();
			
			AppDomain.CurrentDomain.UnhandledException += (snd, unhandledExc) =>
			{
				Exception exc = (Exception)unhandledExc.ExceptionObject;
				UserMessages.ShowErrorMessage("Generic Text Functions has experienced an unknown error, take a screenshot of this message and email it to the developer."
					+ Environment.NewLine
					+ Environment.NewLine + "Exception message:"
					+ Environment.NewLine + exc.Message
					+ Environment.NewLine
					+ Environment.NewLine + "Stacktrace:"
					+ Environment.NewLine + exc.StackTrace);
			};

			SharedClasses.AutoUpdating.CheckForUpdates(
			//SharedClasses.AutoUpdatingForm.CheckForUpdates(
				//exitApplicationAction: () => Dispatcher.Invoke((Action)delegate { this.Shutdown(); }),
				ActionIfUptoDate_Versionstring: (versionstring) => CurrentVersionString = versionstring);//,
				//ActionIfUnableToCheckForUpdates: (errms) => CannotCheckVersionError = errms,
				//ShowModally: true);

			base.OnStartup(e);
		}
	}
}
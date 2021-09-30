using Reverse_SLM;
using System;
using System.Windows.Forms;
using ZOSAPI;

namespace CSharpUserExtensionApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            // Find the installed version of OpticStudio
            bool isInitialized = ZOSAPI_NetHelper.ZOSAPI_Initializer.Initialize();
            // Note -- uncomment the following line to use a custom initialization path
            //bool isInitialized = ZOSAPI_NetHelper.ZOSAPI_Initializer.Initialize(@"C:\Program Files\OpticStudio\");
            if (isInitialized)
            {
                LogInfo("Found OpticStudio at: " + ZOSAPI_NetHelper.ZOSAPI_Initializer.GetZemaxDirectory());
            }
            else
            {
                HandleError("Failed to locate OpticStudio!");
                return;
            }
            
            BeginUserExtension();
        }

        static void BeginUserExtension()
        {
            // Create the initial connection class
            ZOSAPI_Connection TheConnection = new ZOSAPI_Connection();

            // Attempt to connect to the existing OpticStudio instance
            IZOSAPI_Application TheApplication = null;
            try
            {
                TheApplication = TheConnection.ConnectToApplication(); // this will throw an exception if not launched from OpticStudio
            }
            catch (Exception ex)
            {
                HandleError(ex.Message);
                return;
            }
            if (TheApplication == null)
            {
                HandleError("An unknown connection error occurred!");
                return;
            }
            if (TheApplication.Mode != ZOSAPI_Mode.Plugin)
            {
                HandleError("User plugin was started in the wrong mode: expected Plugin, found " + TheApplication.Mode.ToString());
                return;
            }
			
            // Chech the connection status
            if (!TheApplication.IsValidLicenseForAPI)
            {
                HandleError("Failed to connect to OpticStudio: " + TheApplication.LicenseStatus);
                return;
            }

            TheApplication.ProgressPercent = 0;
            TheApplication.ProgressMessage = "Running Extension...";

            IOpticalSystem TheSystem = TheApplication.PrimarySystem;
			if (!TheApplication.TerminateRequested) // This will be 'true' if the user clicks on the Cancel button
            {
                // Add your custom code here...
                // That doesn’t affect the undo behavior, but can make extensions run significantly faster
                TheApplication.ShowChangesInUI = false;

                //Get input from the user through the WinAppFrom
                Settings SettingsForm = ShowUserExtensionSettings(TheApplication);
            }
			
			
			// Clean up
            FinishUserExtension(TheApplication);
        }
		
		static void FinishUserExtension(IZOSAPI_Application TheApplication)
		{
            // Note - OpticStudio will stay in User Extension mode until this application exits
			if (TheApplication != null)
			{
                TheApplication.ProgressMessage = "Complete";
                TheApplication.ProgressPercent = 100;
			}
		}

        static void LogInfo(string message)
        {
            // TODO - add custom logging
            Console.WriteLine(message);
        }

        static void HandleError(string errorMessage)
        {
            // TODO - add custom error handling
            throw new Exception(errorMessage);
        }

        static Settings ShowUserExtensionSettings(IZOSAPI_Application TheApplication)
        {
            IOpticalSystem TheSystem = TheApplication.PrimarySystem;

            // TODO - retrieve the settings specific to your analysis here

            // This will show a form to modify your settings (currently blank)...
            Settings SettingsForm = new Settings();

            // Add your custom code here, and to the SettingsForm...
            Application.Run(SettingsForm);

            // return the settings form
            return SettingsForm;
        }
    }
}

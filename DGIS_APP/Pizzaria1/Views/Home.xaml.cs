
using DGISAPP.SessionManagement;
using SignService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinniesMessageBox;

namespace DGISApp
{
    public partial class Home : UserControl
    {
        private static readonly string VersionUrl = ConfigurationManager.AppSettings["VersionUrl"].ToString() + "version.txt";
        private static readonly string UpdatePackageUrl = ConfigurationManager.AppSettings["VersionUrl"].ToString() + "/DGISApp.zip";
        public  Home()
        {
            InitializeComponent();

            if(!GlobalVariables.IsStatus)
            Checkupdate();
          
          

        }
        public async Task Checkupdate()
        {
            Service1 service1 = new Service1();
            if (!await service1.HasInternetConnectionAsyncTest())
            {
               // MyMessageBox.ShowDialog("For updating the DGIS App, ensure your PC is internet PC or standalone system. Download from ADN (" + ConfigurationManager.AppSettings["UrlForDGISDownloadFromADN"] + ") the DGIS App ZIP file, extract its contents, and run setup.exe to complete the update.");
            }
            else
            {
                _ = GetUpdateAsync();
            }
        }
        private static async Task<string> GetLatestVersionAsync()
        {
            try
            {
                HttpClient client = new HttpClient();
                return await client.GetStringAsync(VersionUrl);
            }
            catch (Exception ex)
            {
                return "";
            }


        }
        private static bool IsNewVersionAvailable(string latestVersion)
        {
            return Version.Parse(latestVersion) > GetCurrentVesrion();
        }
        public static Version GetCurrentVesrion()
        {

            Version version = Version.Parse(ConfigurationManager.AppSettings["Version"].ToString());
            return version;
        }
        public async Task GetUpdateAsync()
        {
            try
            {
               
                    string latestVersion = await GetLatestVersionAsync();
                    if (IsNewVersionAvailable(latestVersion))
                    {
                        Console.WriteLine($"New version {latestVersion} is available!");
                        string result = MyMessageBox.ShowDialog($"New version {latestVersion} is available ! \n\n Update will take few minutes. Would you like to continue ?", MyMessageBox.Buttons.Yes_No);

                        if (result == "1")
                        {
                            DownloadZipFromUrl(UpdatePackageUrl);

                        }
                    else
                    {
                        GlobalVariables.IsStatus = true;
                    }
                        // Optionally, ask the user if they want to download the update

                    }
                    
              
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
            }
        }
        public string DownloadZipFromUrl(string url)
        {
            try
            {
                // Get the system's temp folder path
                string tempPath = System.IO.Path.GetTempPath();

                // Create a file name for the downloaded ZIP file
                string fileName = System.IO.Path.GetFileName(url);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "DownloadedZipFile.zip"; // Default file name
                }

                string filePath = System.IO.Path.Combine(tempPath, fileName);

                // Download the ZIP file from the URL and save it to the temp folder
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(new Uri(url), filePath);
                }

                // Return the path where the file was saved
                // Unzip the file
                string filePath1 = System.IO.Path.Combine(tempPath, "DGISApp" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                ZipFile.ExtractToDirectory(filePath, filePath1);
                // Start the process
                using (Process process = Process.Start(System.IO.Path.Combine(filePath1, "DGISApp\\setup.exe")))
                {
                    // Read the output
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();  // Wait for the process to complete

                    // Console.WriteLine("Output: " + output);
                }

                return filePath;
                ///
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                // Handle exceptions and return the error message
                return $"Error downloading the file: {ex.Message}";

            }
        }
    }
}

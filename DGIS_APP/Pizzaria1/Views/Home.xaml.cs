
using DGISAPP.SessionManagement;
using Org.BouncyCastle.Crypto.Tls;
using SignService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
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
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5) // Adjust timeout as needed
        };
        public  Home()
        {
            InitializeComponent();

            if(!GlobalVariables.IsStatus)
            Checkupdate();

            //Checking Self Api , Host File , Port , Trusted Certificate and Host File
            CheckUrlStatusAsync(); 

        }
      
        private async void CheckUrlStatusAsync()
        {
            await Task.Delay(2000);

            string url = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/HasInternetConnectionAsyncTest";
            //Check Self Api Running Or Not
            bool isRunning = await IsUrlRunningAsync(url);
            if(isRunning==false)
            {
               // MyMessageBox.ShowDialog("URL is not reachable.");
               //Check Host File Exists or Not
              bool ishost=  checkHostIsorNot();
                if(ishost==false)
                {
                    string error = "dgisapp.army.mil is Not in Host File";
                    //MyMessageBox.ShowDialog(error);
                    ErrorLog.LogErrorToFile(null, error);
                }
                //Check Oprt 55102 Open Or Not
                bool isPortOpen = IsPortOpen(55102);
                if (isPortOpen == false)
                {
                    string error = "Port 55102 is CLOSED.";
                    //MyMessageBox.ShowDialog(error);
                    ErrorLog.LogErrorToFile(null, error);
                }
                //Check SSL Certificate is in trusted store
                bool iscert = ISCertTrusted("dgisapp.army.mil");
                if (iscert == false)
                {
                    string error = "SSL Certificate is not in trusted store.";
                    //MyMessageBox.ShowDialog(error);
                    ErrorLog.LogErrorToFile(null, error);
                }
            }
        }
        static bool ISCertTrusted(string Issuer)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C certutil -store root | findstr {Issuer}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // If the output contains the port number, it means the port is open
                return !string.IsNullOrEmpty(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        static bool IsPortOpen(int port)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C netstat -ano | findstr :{port}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // If the output contains the port number, it means the port is open
                return !string.IsNullOrEmpty(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        public bool checkHostIsorNot()
        {
            try
            {
                string hostsFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

                // Read all lines from the hosts file
                var lines = File.ReadAllLines(hostsFilePath);

                // Filter out the line to be removed
                string entryToCheck = $"{"127.0.0.1"} {"dgisapp.army.mil"}";
                var updatedLines = new List<string>();
                int count = 0;
                foreach (var line in lines)
                {
                    if (line.Trim().Equals(entryToCheck, StringComparison.OrdinalIgnoreCase))
                    {
                        count = 1;
                    }
                }

                if (count == 0)
                {
                    return false;
                }
                return true;

            }
            catch (Exception)
            {
                return false ;

            }
        }
        private static async Task<bool> IsUrlRunningAsync(string url)
        {
            try
            {
                    var response = await httpClient.GetAsync(url);
                    return response.IsSuccessStatusCode;
              
            }
            catch (HttpRequestException)
            {
                return false; // Handle network failures
            }
            catch (TaskCanceledException)
            {
                return false; // Handle timeout
            }
        }
     
        public async Task Checkupdate()
        {
            Service1 service1 = new Service1();
            if (!await service1.HasInternetConnectionAsyncTest())
            {
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

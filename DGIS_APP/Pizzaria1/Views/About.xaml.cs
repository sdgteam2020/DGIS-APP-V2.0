﻿//using AutoUpdaterDotNET;
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
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Security.Cryptography.X509Certificates;
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
using System.Xml;
using WinniesMessageBox;
 
namespace DGISApp
{
    /// <summary>
    /// Interação lógica para UserControlInicio.xam
    /// </summary>
    public partial class About : UserControl
    {
        [DllImport("wininet.dll")] 
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        private static readonly string VersionUrl = ConfigurationManager.AppSettings["VersionUrl"].ToString()+"version.txt";
        private static readonly string UpdatePackageUrl = ConfigurationManager.AppSettings["VersionUrl"].ToString()+"/DGISApp.zip";

        public About()
        {
           
            InitializeComponent();
            if (System.Diagnostics.Debugger.IsAttached)
            {
                lblVer.Text = "Debug Mode";
               
                lblVer.Text = $"Current Version : " + GetCurrentVesrion();
                
            }
            else
            {
                //lblVer.Text = $"Current Version : " + ad.CurrentVersion.ToString();
                lblVer.Text = $"Current Version : " + GetCurrentVesrion();
               // _ = GetUpdateAsync();
            }



        }
        public static Version GetCurrentVesrion()
        {
            Assembly assembly = Assembly.GetEntryAssembly();
           
            Version version = Version.Parse(assembly.GetName().Version.ToString());
            //Version version = Version.Parse(ConfigurationManager.AppSettings["Version"].ToString());
            return version;
        }

        private void BeginUpdate()
        {
            this.BusyBar.IsBusy = true;
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            ad.UpdateCompleted += new AsyncCompletedEventHandler(ad_UpdateCompleted);

            // Indicate progress in the application's status bar.
            ad.UpdateProgressChanged += new DeploymentProgressChangedEventHandler(ad_UpdateProgressChanged);
            ad.UpdateAsync();
        }

        void ad_UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            String progressText = String.Format("{0:D}K out of {1:D}K downloaded - {2:D}% completed.", e.BytesCompleted / 1024, e.BytesTotal / 1024, e.ProgressPercentage);
           
        }

        void ad_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Update download cancelled.");
                return;
            }
            else if (e.Error != null)
            {
                MessageBox.Show("ERROR: Could not install the latest version of the application. \n Reason: \n" + e.Error.Message + "\n Please contact system administrator.");
                return;
            }

            this.BusyBar.IsBusy = false;
            MyMessageBox.ShowDialog("Congratulations ! \n\n DGIS App is successfully updated. \n Please restart.");
                    
            Application.Current.Shutdown();
            
        }


        private static String updaterModulePath="";

        
        private static void StartSilent()
        {
            Thread.Sleep(10000);

            Process process = Process.Start(updaterModulePath, "/silent");
            process.Close();
        }

        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
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

        private async void Button_Click(object sender1, RoutedEventArgs e)
        {
            Service1 service1 = new Service1();
            if (!await service1.HasInternetConnectionAsyncTest())
            {
                MyMessageBox.ShowDialog("Your System is Offline Mode. Please Download from ADN ("+ ConfigurationManager.AppSettings["UrlForDGISDownloadFromADN"] + ") the DGIS App ZIP file, extract its contents, and run setup.exe to complete the update.");
            }
            else
            {
                _ = GetUpdateAsync();
            }

            //if (IsConnectedToInternet())
            //{
            ////////////////////old code////////////
            //UpdateCheckInfo info = null;

            //if (ApplicationDeployment.IsNetworkDeployed)
            //{
            //    ApplicationDeployment deployment = ApplicationDeployment.CurrentDeployment;

            //    var appId = new ApplicationIdentity(deployment.UpdatedApplicationFullName);
            //    var unrestrictedPerms = new PermissionSet(PermissionState.Unrestricted);
            //    var appTrust = new ApplicationTrust(appId)
            //    {
            //        DefaultGrantSet = new PolicyStatement(unrestrictedPerms),
            //        IsApplicationTrustedToRun = true,
            //        Persist = true
            //    };

            //    ApplicationSecurityManager.UserApplicationTrusts.Add(appTrust);

            //    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

            //    try
            //    {
            //        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //        info = ad.CheckForDetailedUpdate();

            //    }

            //    catch (DeploymentDownloadException dde)
            //    {
            //        MyMessageBox.ShowDialog(dde.Message);

            //        return;
            //    }
            //    catch (InvalidDeploymentException ide)
            //    {
            //        MyMessageBox.ShowDialog("No update found as update repository is not reachable. Kindly contact application Administrator. \n Error: " + ide.Message);
            //        return;
            //    }
            //    catch (InvalidOperationException ioe)
            //    {
            //        MyMessageBox.ShowDialog("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
            //        return;
            //    }

            //    catch (Exception ex)
            //    {
            //        MyMessageBox.ShowDialog(ex.Message);
            //    }
            //    if (info.UpdateAvailable)
            //    {
            //        Boolean doUpdate = true;

            //        if (!info.IsUpdateRequired)
            //        {
            //            string result = MyMessageBox.ShowDialog("New update is found ! \n\n Update will take few minutes. Would you like to continue ?", MyMessageBox.Buttons.Yes_No);

            //            if (result == "0")
            //            {
            //                doUpdate = false;

            //            }


            //        }
            //        else
            //        {
            //            // Display a message that the app MUST reboot. Display the minimum required version.
            //            MyMessageBox.ShowDialog("Attention ! \n\n Mandatory update found for your current " +
            //                "version to version " + info.MinimumRequiredVersion.ToString() + ". The application will install the update and restart.");
            //        }

            //        if (doUpdate)
            //        {
            //            try
            //            {
            //                BeginUpdate();



            //            }
            //            catch (DeploymentDownloadException dde)
            //            {
            //                MyMessageBox.ShowDialog("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
            //                return;
            //            }
            //        }

            //    }
            //    else
            //    {
            //        MyMessageBox.ShowDialog("DGIS App is already updated.");
            //    }
            //}
            //else
            //{
            //    MyMessageBox.ShowDialog("DGIS App is not deployed on network.");
            //}

            ////////////////////End Code////////////////////////
            //}
            //else
            //{
            //    MyMessageBox.ShowDialog("Application updated cannot be done in Offline mode.");
            //}
        }
        public async Task GetUpdateAsync()
        {
            try
            {
                if (IsConnectedToInternet())
                {
                    string latestVersion = await GetLatestVersionAsync();
                    if (latestVersion == "")
                    {
                        MyMessageBox.Show("There is a problem updating the DGIS app.");
                    }
                    else if (IsNewVersionAvailable(latestVersion))
                    {
                        Console.WriteLine($"New version {latestVersion} is available!");
                        string result = MyMessageBox.ShowDialog($"New version {latestVersion} is available ! \n\n Update will take few minutes. Would you like to continue ?", MyMessageBox.Buttons.Yes_No);

                        if (result == "1")
                        {
                            DownloadZipFromUrl(UpdatePackageUrl);

                        }
                        // Optionally, ask the user if they want to download the update

                    }
                    else
                    {
                        MyMessageBox.Show("You have the latest version.");
                    }
                }
                else
                {
                    MyMessageBox.Show("Application updated cannot be done in Offline mode.");
                }
            }
            catch (Exception ex) {
                ErrorLog.LogErrorToFile(ex);
            }
        }
    }
}

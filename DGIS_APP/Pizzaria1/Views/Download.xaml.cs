

using DGISAPP.SessionManagement;
using SignService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinniesMessageBox;
using WinSCP;


namespace DGISApp
{

    /// <summary>
    /// Interaction logic for MY.xaml
    /// </summary>
    /// 

    public class items
    {
        public string FullName { get; set; }
    }
    public partial class Download : UserControl
    {
        public string fullName { get; set; } = "/";
        Stack values = new Stack();

        string path = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads" + @"\";

       

       // Session session;//= new Session();

        public Download()
        {

            InitializeComponent();
                BusyBar.IsBusy = false;
                lbl2.Visibility = Visibility.Hidden;
                values.Push("/");
                pbar.Visibility = Visibility.Hidden;
               
               // pres.Visibility = Visibility.Visible;
                lbl1.Visibility = Visibility.Hidden;
                string loginstatus = ConfigurationManager.AppSettings["loginStatus"];
                if (loginstatus == "0")
                {
                    this.Mygrid.Visibility = Visibility.Hidden;
                    this.scr.Visibility = Visibility.Hidden;
                }
                else
                {
              //  ManageSessionState.session.FileTransferProgress += Session_FileTransferProgress;

                RemoteDirectoryInfo directory =
                       ManageSessionState.session.ListDirectory(ManageSessionState.path);
                location.Content = ManageSessionState.path;
                ConfigurationManager.AppSettings.Set("loginStatus", "1");
                IEnumerable<dynamic> list = directory.Files.ToList();
                list = list.Where(u => u.IsParentDirectory == false).ToList();

                Mygrid.ItemsSource = list;
                this.TrainsitionigContentSlide.Visibility = Visibility.Hidden;
                this.Mygrid.Visibility = Visibility.Visible;
                this.scr.Visibility = Visibility.Visible;
                this.TrainsitionigContentSlide.Visibility = Visibility.Hidden;
         
                this.Dispatcher.Invoke(new Action(() => this.currentdir.Visibility = Visibility.Visible));
                this.Dispatcher.Invoke(new Action(() => this.location.Visibility = Visibility.Visible));

            }
                
           

        }


        void selectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

            if (Mygrid.SelectedItems.Count != 0)
            {
                var row = Mygrid.SelectedItems[0].ToString();
                if (Path.HasExtension(row))
                {

                }
                else
                {
                    fullName += "/" + row;
                    RemoteDirectoryInfo directory =
                                 ManageSessionState.session.ListDirectory(fullName);
                    IEnumerable<dynamic> list = directory.Files.ToList();
                    list = list.Where(u => u.IsParentDirectory == false).ToList();
                    Mygrid.ItemsSource = list;
                }
            }
        }

      

        private void ViewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Mygrid.SelectedItems.Count != 0)
            {
                var row = Mygrid.SelectedItems[0].ToString();
                if (Path.HasExtension(row))
                {

                }
                else
                {
                    RemoteFileInfo ss = (RemoteFileInfo)Mygrid.SelectedItem;
                    fullName = ss.FullName;
                    values.Push(ss.FullName.Replace(ss.Name, ""));
                    RemoteDirectoryInfo directory = ManageSessionState.session.ListDirectory(ss.FullName);
                    IEnumerable<dynamic> list = directory.Files.ToList();
                    list = list.Where(u => u.IsParentDirectory == false).ToList();
                    Mygrid.ItemsSource = list;
                    this.Dispatcher.Invoke(new Action(() => this.location.Content = ss.FullName));
                   
                    ManageSessionState.path = ss.FullName;
                }
            }

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationManager.AppSettings.Set("username", textBoxEmail.Text.ToString());
            ConfigurationManager.AppSettings.Set("password", passwordBox1.Password.ToString());
            this.BusyBar.IsBusy = true;
            Thread thread = new Thread(login) { IsBackground = true };
            thread.Start();
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            if (values.Count != 0)
            {
                RemoteFileInfo ss = (RemoteFileInfo)Mygrid.SelectedItem;
                string path = values.Pop().ToString();
                RemoteDirectoryInfo directory = ManageSessionState.session.ListDirectory(path);
                IEnumerable<dynamic> list = directory.Files.ToList();
                list = list.Where(u => u.IsParentDirectory == false).ToList();
                Mygrid.ItemsSource = list;
                this.Dispatcher.Invoke(new Action(() => this.location.Content = path));

            }
        }

        private void login()
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://" + ConfigurationManager.AppSettings.Get("IP") + "/api/values");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                String username = ConfigurationManager.AppSettings.Get("auth");
                String password = ConfigurationManager.AppSettings.Get("auth");
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);
                httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = "{\"username\":\"" + ConfigurationManager.AppSettings.Get("username") + "\"," +
                                  "\"password\":\"" + ConfigurationManager.AppSettings.Get("password") + "\"}";

                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }



                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = ConfigurationManager.AppSettings["IP"],
                    UserName = ConfigurationManager.AppSettings.Get("username"),
                    Password = ConfigurationManager.AppSettings.Get("password"),
                    PortNumber = 22,
                    GiveUpSecurityAndAcceptAnySshHostKey = true
                };

                ManageSessionState.session = new Session();
               // this.Dispatcher.Invoke(new Action(() => ManageSessionState.session.FileTransferProgress += new WinniesMessageBox.Download("",new DownloadPopUp.Buttons(),new object()).SessionFileTransferProgress));
                


                ManageSessionState.session.Open(sessionOptions);

                RemoteDirectoryInfo directory =
                       ManageSessionState.session.ListDirectory("/");
                ManageSessionState.path = "/";
                ConfigurationManager.AppSettings.Set("loginStatus", "1");
                IEnumerable<dynamic> list = directory.Files.ToList();
                list = list.Where(u => u.IsParentDirectory == false).ToList();
                this.Dispatcher.Invoke(new Action(() => this.location.Content = ManageSessionState.session.HomePath));
               // ;
                this.Dispatcher.Invoke(new Action(() => Mygrid.ItemsSource = list));
                this.Dispatcher.Invoke(new Action(() => this.TrainsitionigContentSlide.Visibility = Visibility.Hidden));
                this.Dispatcher.Invoke(new Action(() => this.Mygrid.Visibility = Visibility.Visible));
                this.Dispatcher.Invoke(new Action(() => this.scr.Visibility = Visibility.Visible));

                this.Dispatcher.Invoke(new Action(() => this.currentdir.Visibility = Visibility.Visible));
                this.Dispatcher.Invoke(new Action(() => this.location.Visibility = Visibility.Visible));


            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(new Action(() => this.BusyBar.IsBusy = false));
                this.Dispatcher.Invoke(new Action(() => this.TrainsitionigContentSlide.Visibility = Visibility.Visible));
                this.Dispatcher.Invoke(new Action(() => MyMessageBox.Show(ex.Message))); 
                ErrorLog.LogErrorToFile(ex);
            }
        }
      
        private void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ManageSessionState.remotedir = Mygrid.SelectedItem;
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = ConfigurationManager.AppSettings["IP"],
                    UserName = ConfigurationManager.AppSettings.Get("username"),
                    Password = ConfigurationManager.AppSettings.Get("password"),
                    PortNumber = 22,
                    GiveUpSecurityAndAcceptAnySshHostKey = true
                };

                System.Windows.Forms.FolderBrowserDialog saveFileDialog = new System.Windows.Forms.FolderBrowserDialog();
                saveFileDialog.Description = "Select loc to save file";


                System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();



                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    path = saveFileDialog.SelectedPath + @"\";
                    DownloadPopUp.ShowDialog(path,ManageSessionState.remotedir, sessionOptions);
                }

               
                
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }

        }

     
        private void DataGridCell_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }

}

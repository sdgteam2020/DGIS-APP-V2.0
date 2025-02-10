using Newtonsoft.Json;
using SignService;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinniesMessageBox;

namespace DGISApp
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    /// 
   
    public partial class MainWindow : Window
    {
        public String Baseurl = "https://dgis.army.mil/DGIS_App_API";
        public Boolean CheckStatus;
        //public String Baseurl =  "https://localhost/DGIS_App_API";
        public MainWindow()
        {
            InitializeComponent();
            //MessageBox.Show("call CounterAPI");
            CounterAPI();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (CheckStatus==false)
            { 
                
                CheckStatus = true;
            }
        }

        public class PCDetail
        {
            public string reqData { get; set; }
        }

        async void CounterAPI()
        {
            try
            {
                String NVersion = "";
                String url = Baseurl + "/api/transaction/CountUser";
                IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;
              
                Assembly assembly = Assembly.GetEntryAssembly();
                NVersion = assembly.GetName().Version.ToString();//ConfigurationManager.AppSettings["Version"].ToString();
                string ip = a[0].ToString() + ", " + Dns.GetHostName() + "," + NVersion;

               
                    
                var request =  WebRequest.Create(url);
                request.Method = "POST";

                var PCDetail = new PCDetail
                {
                    reqData = ip
                };
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                var ReqJson = JsonConvert.SerializeObject(PCDetail);
                byte[] byteArray = Encoding.UTF8.GetBytes(ReqJson);

                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                //MessageBox.Show("Ip :" + ip + " Url :=" + url);

                var reqStream =  request.GetRequestStream();
                reqStream.Write(byteArray, 0, byteArray.Length);

                var response = await request.GetResponseAsync();
                var statusCode = ((HttpWebResponse)response).StatusCode;
                //MessageBox.Show("Log Sent");
            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
                //MessageBox.Show("Exe:-"+ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
        }
               
        private void ButtonFechar_Click(object sender, RoutedEventArgs e)
        {
            //Application.Current.Shutdown();
            this.Visibility = Visibility.Hidden;
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            //WindowState = WindowState.Minimized;
            this.Visibility = Visibility.Hidden;
        }

        private void ButtonFechar_Click1(object sender, RoutedEventArgs e)
        {
            if (ConfigurationManager.AppSettings.Get("loginStatus") == "0")
            {
                MyMessageBox.ShowDialog("you are already logged out.");
            }
            else
            {
                ConfigurationManager.AppSettings.Set("loginStatus", "0");
                GridPrincipal.Children.Clear();
                GridPrincipal.Children.Add(new Home());
                ListViewMenu.SelectedIndex = 0;
            }
        }

        private void ListViewMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ListViewMenu.SelectedIndex;
            MoveCursorMenu(index);

            switch (index)
            {
                case 0:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 1:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 2:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 3:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 4:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 5:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Watermark());
                    break;

                case 6:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new DigitalSign());
                    break;
                case 7:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new VerifyDigitalSign());
                    break;
                case 8:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new SymmetricEncrypt());
                    break;
                case 9:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new SymmetricDecryption());
                    break;
                
                case 10:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new About());
                    break;
                case 11:

                    string FileName = System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DGIS_Help.pdf";

                    FileInfo fi = new FileInfo(FileName);
                    if (fi.Exists)
                    {
                        Process.Start(FileName);
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
        private void MoveCursorMenu(int index)
        {
        }
    }
}

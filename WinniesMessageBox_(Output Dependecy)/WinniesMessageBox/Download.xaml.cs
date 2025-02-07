using DGISAPP.SessionManagement;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WinSCP;

namespace WinniesMessageBox
{
    /// <summary>
    /// Interaction logic for Download.xaml
    /// </summary>
    public partial class Download : Window
    {

        public string fullName { get; set; } = "/";
       
        private static string _lastFileName;
        private string filename = null;
        private string fullpath = null;

        public string Text { get; set; }
        bool cancel = false;
        string path = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads" + @"\";
        public string ReturnString { get; set; }


        public void download()
        {

            TransferOptions transferOptions1 = new TransferOptions();
            transferOptions1.TransferMode = TransferMode.Binary;

            TransferOperationResult transferResult1;
            transferResult1 =
                ManageSessionState.session.GetFiles(fullpath, path, false, transferOptions1);

            // Throw on any error
            transferResult1.Check();

            // Print results
            foreach (TransferEventArgs transfer in transferResult1.Transfers)
            {
                Console.WriteLine("Download of {0} succeeded", transfer.FileName);



                FileInfo fi = new FileInfo(transfer.Destination);


            }

            this.Dispatcher.Invoke(new Action(() =>
            {

                if (cancel == false)
                {
                    MyMessageBox.ShowDialog("Download completed and file saved at \n" + path);
                    ReturnString = "-1";
                    Close();

                }
                else
                {

                    this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Downloading cancel successfully.")));
                }
            }
            ));

        }

        public Download(string _path,object remotedir, SessionOptions session)
        {
           
                InitializeComponent();
           

                try
                {

                Task.Run(() =>
                {
                    ManageSessionState.session = new Session();
                    ManageSessionState.session.FileTransferProgress += SessionFileTransferProgress;
                    ManageSessionState.session.Open(session);
                });

                RemoteFileInfo ss = (RemoteFileInfo)remotedir;
                filename = ss.Name;
                fullpath = ss.FullName;

                path = _path;  
                    Task.Run(() =>
                        {

                            download();

                        });
                   
                }
                catch (Exception ex)
                {
                    MyMessageBox.ShowDialog(ex.Message);
                }
        }
      
        public void SessionFileTransferProgress(
  object sender, FileTransferProgressEventArgs e)
        {
            // New line for every new file
            if ((_lastFileName != null) && (_lastFileName != e.FileName))
            {
                Console.WriteLine();
            }
            if (cancel)
            {
                e.Cancel = true;
            }
            // Print transfer progress
            Console.Write("\r{0} ({1:P0})", e.FileName, e.FileProgress);

            // Remember a name of the last file reported
            _lastFileName = e.FileName;

            string yy = e.OverallProgress.ToString("P", CultureInfo.InvariantCulture);
            string fileProgess = e.FileProgress.ToString("P", CultureInfo.InvariantCulture);
            int index = yy.LastIndexOf(".");
            if (index > 0)
                yy = yy.Substring(0, index);

            int index1 = fileProgess.LastIndexOf(".");
            if (index1 > 0)
                fileProgess = fileProgess.Substring(0, index1);

             this.Dispatcher.Invoke(new Action(() => Console.WriteLine("FileName : " + e.FileName + " Progress" + yy)));
            this.Dispatcher.Invoke(new Action(() => this.pbar1.Value = Convert.ToInt32(yy)));
           
            Dispatcher.Invoke(new Action(() => this.txbText.Text = e.FileName));

        }


        DoubleAnimation anim;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.3));
            anim.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           // Height = (txbText.LineCount * 27) + gBar.Height + 60;
        }

        private void btnReturnValue_Click(object sender, RoutedEventArgs e)
        {
            ReturnString = ((Button)sender).Uid.ToString();
            Close();
        }
        private void gBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            cancel = true;
            ReturnString = "-1";
            Close();
        }
    }
}

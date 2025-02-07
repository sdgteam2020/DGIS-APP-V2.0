using DGISAPP.SessionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using WinSCP;

namespace WinniesMessageBox
{
    public class DownloadPopUp
    {
      
        public static string ShowDialog(string path,object remotedir, SessionOptions session)
        {
            Download messageBox = new Download(path,remotedir,session);
            messageBox.Show();
            return messageBox.ReturnString;
        }

       
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;

namespace DGISAPP.SessionManagement
{
    public static class ManageSessionState
    {
        public static Session session { get; set; }

        public static string path { get; set; }
        public static object remotedir { get; set; }

      
    }
}

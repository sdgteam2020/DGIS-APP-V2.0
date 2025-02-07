using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace SignService
{
    public class ErrorLog 
    {
        public static void LogErrorToFile(Exception ex)
        {
            IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;
           
             //string filePath = System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\ErrorLog.txt";// Path to the error log file
            //string filePath = "ErrorLog.txt"; // Path to the error log file
            string errorMessage = $"****************************************************************************************************************\n ";
            string ip = a[0].ToString();
            errorMessage += "IP Address:-"+ ip;
            errorMessage += "\n Operating System: " + Environment.OSVersion;
            errorMessage += "\n 64-bit OS: " + Environment.Is64BitOperatingSystem;
            errorMessage += "\n Machine Name: " + Environment.MachineName;
            errorMessage += "\n System Directory: " + Environment.SystemDirectory;
            errorMessage += "\n User Name: " + Environment.UserName;
            errorMessage += $"[{DateTime.Now}] \n Exception: {ex.Message}\n Stack Trace: {ex.StackTrace}";
            errorMessage += "\n*********************************************************************************************************************\n";
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string Appfolder = System.IO.Path.Combine(path, "DGIS");
                Directory.CreateDirectory(Appfolder);
                string filePath = System.IO.Path.Combine(Appfolder, "ErrorLog.txt");
                // Append the error message to the file
                File.AppendAllText(filePath, errorMessage);
            }
            catch (Exception fileEx)
            {
                Console.WriteLine($"Failed to write to log file: {fileEx.Message}");
            }
        }
    }
}
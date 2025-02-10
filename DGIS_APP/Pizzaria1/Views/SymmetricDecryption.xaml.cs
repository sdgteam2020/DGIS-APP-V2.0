﻿//  using iTextSharp.text.pdf;
using Microsoft.Win32;
using MyApp;
using SignService;
using Spire.Doc;
using Spire.Doc.Documents;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
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
    /// <summary>
    /// Interaction logic for EncryptionAndDecryption.xaml
    /// </summary>
    public partial class SymmetricDecryption : UserControl
    {
        string[] droppedFilePaths = null;
        string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";

        Aes myAes = Aes.Create();

        public SymmetricDecryption()
        {
            InitializeComponent();

        }




        private void DropList_DragEnter(object sender, DragEventArgs e)
        {

        }

        static bool ValidatePassword(string password)
        {
            const int MIN_LENGTH = 4;
            const int MAX_LENGTH = 18;

            if (password == null) throw new ArgumentNullException();

            bool meetsLengthRequirements = password.Length >= MIN_LENGTH && password.Length <= MAX_LENGTH;
            bool hasUpperCaseLetter = false;
            bool hasLowerCaseLetter = false;
            bool hasDecimalDigit = false;
            bool hasSpecialChar = false;

            if (meetsLengthRequirements)
            {
                int PasswordSpecialChar = password.Count(p => !char.IsLetterOrDigit(p));

                if (PasswordSpecialChar > 0)
                {
                    hasSpecialChar = true;
                }

                foreach (char c in password)
                {
                    if (char.IsUpper(c)) hasUpperCaseLetter = true;
                    else if (char.IsLower(c)) hasLowerCaseLetter = true;
                    else if (char.IsDigit(c)) hasDecimalDigit = true;
                }
            }

            bool isValid = meetsLengthRequirements
                        //&& hasUpperCaseLetter
                        //&& hasLowerCaseLetter
                        //&& hasDecimalDigit
                        //&& hasSpecialChar
                        ;
            return isValid;

        }
        private static byte[] GenerateSalt()
        {
            var randomBytes = Encoding.ASCII.GetBytes("original");
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        public static String betweenStrings(String text, String start, String end)
        {
            int p1 = text.IndexOf(start) + start.Length;
            int p2 = text.IndexOf(end, p1);

            if (end == "") return (text.Substring(p1));
            else return text.Substring(p1, p2 - p1)+"";
        }
        private void DropList_Drop(object sender, DragEventArgs e)
        {
            string DownloadPath = "";
            try
            {
                if (textpassword.Password.ToString() == "")
                {
                    MyMessageBox.ShowDialog("Please Enter the Password used during file Encrption.");
                    return;
                }


                
                if (ValidatePassword(textpassword.Password.ToString()))
                {

                    string Password = textpassword.Password.ToString();
                    byte[] Mykey = null;

                    if (string.IsNullOrWhiteSpace(Password) || Password.Length < AesGcm256.MinPasswordLength)
                        throw new ArgumentException(String.Format("Please enter password with atleast {0} characters as per ACSP-2017.", AesGcm256.MinPasswordLength));


                    byte[] Hashbytes = Encoding.Unicode.GetBytes(Password);
                    SHA256Managed hashstring = new SHA256Managed();
                    Mykey = hashstring.ComputeHash(Hashbytes);


                    byte[] MyIV = Encoding.ASCII.GetBytes(Password.PadRight(16, ' '));

                    myAes.Key = Mykey;
                    myAes.IV = MyIV;

                   

                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                        try
                        {
                            int processedFiles = 0;
                            int totalFiles = droppedFilePaths.Count();
                            foreach (var path in droppedFilePaths)
                            {
                                ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                                DownloadPath = System.IO.Path.GetDirectoryName(path);

                                FileInfo fi = new FileInfo(path);
                                if (fi.Extension == ".mil")
                                {
                                    FileStream stream1 = File.OpenRead(path);
                                    byte[] bytes1 = new byte[stream1.Length];
                                    stream1.Read(bytes1, 0, bytes1.Length);

                                    stream1.Close();
                                    char dd = '_';
                                    int levelOfEncryption = fi.FullName.Count(s => s == dd);
                                        new Thread(() =>
                                        {

                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));

                                            byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, textpassword.Password.ToString()); //decryptdata(bytes1, myAes.Key, myAes.IV, myAes.KeySize);
                                                if (roundtrip == null)
                                            {
                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Password incorrect !")));
                                                return;
                                            }
                                            else
                                            {
                                                string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");


                                                using (Stream file = File.OpenWrite(filePath))
                                                {

                                                    file.Write(roundtrip, 0, roundtrip.Length);
                                                }

                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));

                                                //this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Congratulations ! \n\n Document is successfully decrypted. \n" + DownloadPath)));

                                            }

                                            processedFiles++;

                                            if (processedFiles == totalFiles)
                                            {
                                                // All files processed, show congratulatory message
                                                var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                                {

                                                    return MyMessageBox.ShowDialog("Congratulations!\n\nDocument is successfully Decrypted.\n" + DownloadPath, MyMessageBox.Buttons.OK_PathOpen);
                                                }));

                                                if (result == "2")
                                                {
                                                    try
                                                    {
                                                        Process.Start(DownloadPath);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine("An error occurred: " + ex.Message);
                                                    }
                                                }
                                            }
                                        }).Start();

                                   
                                }
                                else
                                {
                                    MyMessageBox.ShowDialog("File format not supported. Please Select .mil file.");
                                }
                            }
                        }
                        catch (Exception)
                        {
                            MyMessageBox.ShowDialog("Invaild Details....");
                        }


                    }

                    else
                    {
                        textpassword.Clear();
                        MyMessageBox.ShowDialog("Password Lenght should be between 4 to 18 Characters.");
                    }
                }
                else
                {
                    textpassword.Clear();
                    MyMessageBox.ShowDialog("Password Lenght should be between 4 to 18 Characters.");
                }

            }
            catch (Exception ex)
            {
                if (ex.Message == "Value cannot be null.\r\nParameter name: value")
                {
                    MyMessageBox.ShowDialog("Please Select Key Size");
                }
                else
                {
                    MyMessageBox.ShowDialog(ex.Message);
                }
                ErrorLog.LogErrorToFile(ex);
            }

        }



        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            string DownloadPath = "";
            try
            {

                string Password = textpassword.Password.ToString();
                byte[] Mykey = null;
                if (string.IsNullOrWhiteSpace(Password) || Password.Length < AesGcm256.MinPasswordLength)
                    throw new ArgumentException(String.Format("Please enter password with atleast {0} characters as per ACSP-2017.", AesGcm256.MinPasswordLength));

                byte[] Hashbytes = Encoding.Unicode.GetBytes(Password);
                    SHA256Managed hashstring = new SHA256Managed();
                    Mykey = hashstring.ComputeHash(Hashbytes);


                byte[] MyIV = Encoding.ASCII.GetBytes(Password.PadRight(16, ' '));

                myAes.Key = Mykey;
                myAes.IV = MyIV;

                if (textpassword.Password.ToString() == "")
                {
                    MyMessageBox.ShowDialog("Please Enter Password.");
                    return;
                }
                if (ValidatePassword(textpassword.Password.ToString()))
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Multiselect = true;
                    openFileDialog.Title = "Select File To Decryption";
                    openFileDialog.Filter = "mil files (*.mil)|*.mil";
                    //openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (ConfigurationManager.AppSettings["LastSelectedLocation"] == "")
                    {
                        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                    else
                    {
                        openFileDialog.InitialDirectory = ConfigurationManager.AppSettings["LastSelectedLocation"];
                    }
                    if (openFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            int processedFiles = 0;
                            int totalFiles = openFileDialog.FileNames.Count();

                            foreach (var path in openFileDialog.FileNames)
                            {
                                ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                                DownloadPath = System.IO.Path.GetDirectoryName(path);

                                FileInfo fi = new FileInfo(path);
                                FileStream stream1 = File.OpenRead(path);
                                byte[] bytes1 = new byte[stream1.Length];
                                stream1.Read(bytes1, 0, bytes1.Length);

                                stream1.Close();


                                    new Thread(() =>
                                    {
                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));


                                        byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, textpassword.Password.ToString());

                                        if (roundtrip == null)
                                        {
                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Password incorrect..")));
                                        }

                                        else
                                        {
                                            string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");

                                            using (Stream file = File.OpenWrite(filePath))
                                            {
                                                file.Write(roundtrip, 0, roundtrip.Length);
                                            }

                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                            //this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Congratulations ! \n\n Document is successfully Decrypted. \n" + DownloadPath)));
                                        }

                                        processedFiles++;

                                        if (processedFiles == totalFiles)
                                        {
                                            // All files processed, show congratulatory message
                                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                            {

                                                return MyMessageBox.ShowDialog("Congratulations!\n\nDocument is successfully Decrypted.\n" + DownloadPath, MyMessageBox.Buttons.OK_PathOpen);
                                            }));

                                            if (result == "2")
                                            {
                                                try
                                                {
                                                    Process.Start(DownloadPath);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine("An error occurred: " + ex.Message);
                                                }
                                            }
                                        }

                                    }).Start();

                            }

                            
                        }
                        catch (Exception)
                        {
                            MyMessageBox.ShowDialog("Invaild Details....");
                        }


                    }
                }
                else
                {
                    textpassword.Clear();
                    MyMessageBox.ShowDialog("Password Lenght should be between 4 to 18 Characters.");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Value cannot be null.\r\nParameter name: value")
                {
                    MyMessageBox.ShowDialog("Please Select Key Size");
                }
                else
                {
                    MyMessageBox.ShowDialog(ex.Message);
                }
                ErrorLog.LogErrorToFile(ex);
            }
        }
    }
}

//using iTextSharp.text.pdf;
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
using System.Text.RegularExpressions;
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
    public partial class SymmetricEncrypt : UserControl
    {
        string[] droppedFilePaths = null;
        string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";

        Aes myAes = Aes.Create();
        

       
        public SymmetricEncrypt()
        {
            InitializeComponent();
        }
      
        private void DropList_DragEnter(object sender, DragEventArgs e)
        {
        }



        private void DropList_Drop(object sender, DragEventArgs e)
        {
         
            try
            {
                if (textpassword.Password.ToString() == "")
                {
                    MyMessageBox.ShowDialog("Please Enter Password for Encryption");
                    return;
                }


                string Password = textpassword.Password.ToString();                

                if (ValidatePassword(textpassword.Password.ToString()))
                {
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

                        
                            fileEncrypt(droppedFilePaths);


                    }
                }
                else
                {
                    textpassword.Clear();
                    MyMessageBox.ShowDialog("Password Lenght should be between 4 to 18 Characters.");
                }
            }
            catch(Exception ex)
            {
                if (ex.Message == "Value cannot be null.\r\nParameter name: value")
                {
                    MyMessageBox.ShowDialog("Please Select  Key Size");
                }
                else
                {
                    MyMessageBox.ShowDialog(ex.Message);
                }
                ErrorLog.LogErrorToFile(ex);
            }
        }

   
        static byte[] encryptdata(byte[] bytearraytoencrypt, byte[] Key, byte[] IV, int KeySize)
        {
            AesCryptoServiceProvider dataencrypt = new AesCryptoServiceProvider();
            dataencrypt.BlockSize = 128;
            dataencrypt.KeySize = KeySize; 
            dataencrypt.Key = Key;
            dataencrypt.IV = IV;
            dataencrypt.Padding = PaddingMode.PKCS7;
            dataencrypt.Mode = CipherMode.CBC;
            ICryptoTransform crypto1 = dataencrypt.CreateEncryptor(dataencrypt.Key, dataencrypt.IV);
            byte[] encrypteddata = crypto1.TransformFinalBlock(bytearraytoencrypt, 0, bytearraytoencrypt.Length);
            crypto1.Dispose();
            return encrypteddata;
        }

        //code to encrypt data
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


        public void fileEncrypt(string[] files)
        {

            string DownloadPath = "";
            int totalFiles = files.Count(); // Assuming files is a collection of file paths
            int processedFiles = 0;

            foreach (var path in files)
            {
                // Update the LastSelectedLocation in the AppSettings
                ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                DownloadPath = System.IO.Path.GetDirectoryName(path);

                FileInfo fi = new FileInfo(path);

                // Check if the file is already encrypted
                if (fi.Extension == ".mil")
                {
                    MyMessageBox.ShowDialog("File is already encrypted.");
                    break;
                }

                FileStream stream = File.OpenRead(path);
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                stream.Close();

                new Thread(() => {
                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                    // Encrypt the file content
                    byte[] encrypted = AesGcm256.SimpleEncryptWithPassword(bytes, textpassword.Password.ToString());

                    // Save the encrypted content to a new file
                    using (Stream file = File.OpenWrite(DownloadPath + "\\" + fi.Name + "_EN_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil"))
                    {
                        file.Write(encrypted, 0, encrypted.Length);
                    }

                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));

                    processedFiles++;

                    if (processedFiles == totalFiles)
                    {
                        // All files processed, show congratulatory message
                        var result = this.Dispatcher.Invoke(new Func<string>(() =>
                        {
        
                            return MyMessageBox.ShowDialog("Congratulations!\n\nDocument is successfully Encrypted.\n" + DownloadPath, MyMessageBox.Buttons.OK_PathOpen);
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


        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (textpassword.Password.ToString() == "")
                {
                    MyMessageBox.ShowDialog("Please Enter Password for Encryption.");
                    return;
                }

                string Password = textpassword.Password.ToString();
                if (ValidatePassword(textpassword.Password.ToString()))
                {
                    byte[] Mykey = null;

                    if (string.IsNullOrWhiteSpace(Password) || Password.Length < AesGcm256.MinPasswordLength)
                        throw new ArgumentException(String.Format("Please enter password with atleast { 0 } characters as per ACSP - 2017.", AesGcm256.MinPasswordLength));



                    byte[] Hashbytes = Encoding.Unicode.GetBytes(Password);
                    SHA256Managed hashstring = new SHA256Managed();
                    Mykey = hashstring.ComputeHash(Hashbytes);

                    byte[] MyIV = Encoding.ASCII.GetBytes(Password.PadRight(16, ' '));

                    myAes.Key = Mykey;
                    myAes.IV = MyIV;


                    

                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Select File for Enryption";
                    openFileDialog.Multiselect = true;
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

                        //System.Windows.Forms.FolderBrowserDialog saveFileDialog = new System.Windows.Forms.FolderBrowserDialog();
                        //saveFileDialog.Description = "Select path to save file";
                        //System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();

                        //if (result == System.Windows.Forms.DialogResult.OK)
                        //{
                        //    download = saveFileDialog.SelectedPath + @"\";
                            
                            fileEncrypt(openFileDialog.FileNames);
                        //}
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

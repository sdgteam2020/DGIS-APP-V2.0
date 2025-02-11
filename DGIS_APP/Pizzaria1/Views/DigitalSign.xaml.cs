
using CertTestimg.CertTestimg;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Office.Interop.Word;
using Microsoft.Win32;
using MyApp;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;
using SignService;
using Spire.Pdf.Graphics;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocToPDFConverter;
using Syncfusion.OfficeChart;
using Syncfusion.OfficeChartToImageConverter;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Windows.PdfViewer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using WinniesMessageBox;
using static iText.Signatures.PdfSigner;
using static ValidateCertificate.ValidateCert;
using Brushes = System.Windows.Media.Brushes;
using Console = System.Console;
using StringWriter = System.IO.StringWriter;
using Task = System.Threading.Tasks.Task;

namespace DGISApp
{
   
    public partial class DigitalSign : UserControl
    {
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        string[] droppedFilePaths = null;
        string message = null;
        string fileName = null;
        string DownloadPath = "";
        public string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";
        float pixelWidth = 0;
        float pixelHeight = 0;
        int PageWidth = 0;
        int PageHeight = 0;
        bool crloscp = false;
        string crlocspmsg = "";
        string CertThumbPrint = "";
       string UrlApi= ConfigurationManager.AppSettings["UrlApi"].ToString();
        public DigitalSign()
        {
            InitializeComponent();
            LoadDataAsync();
        }

        //*
        private async void LoadDataAsync()
        {
            string result = await CheckSomethingAsync();
        }
        //*
        public async Task<string> CheckSomethingAsync()
        {
            X509Certificate2 cert1 = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            X509Certificate2Collection fcollection = new X509Certificate2Collection();
            
            try
            {
                store.Open(OpenFlags.OpenExistingOnly);
                await Task.Run(() =>
                {

                    foreach (X509Certificate2 cert in store.Certificates)
                    {
                        try
                        {
                            if (!(cert.Subject.Contains("localhost") || cert.Subject.Contains("DESKTOP")))
                            {
                                if (cert.PrivateKey is RSACryptoServiceProvider rsaProvider && rsaProvider.CspKeyContainerInfo.HardwareDevice)
                                {
                                    fcollection.Add(cert);
                                }
                            }
                        }
                        catch (CryptographicException)
                        {
                            // Handle any exception when accessing the private key
                            // You can log the error or skip this certificate
                        }
                    }
                    store.Close();
                });
                if (fcollection.Count == 0)
                {
                    MyMessageBox.ShowDialog("Pl insert valid Token !");
                }
                else
                {
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                        CertThumbPrint = cert1.Thumbprint;
                    }
                    else
                    {
                        try
                        {
                            X509Certificate2Collection selectedCertificates = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection);

                            if (selectedCertificates.Count > 0)
                            {
                                cert1 = selectedCertificates[0];
                                CertThumbPrint = cert1.Thumbprint;
                            }
                        }
                        catch
                        {
                            CertThumbPrint = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (fcollection.Count == 0)
                {
                    MyMessageBox.ShowDialog("Pl insert valid Token !");
                    CertThumbPrint = "";
                }
                else
                { 
                    MyMessageBox.ShowDialog("Try again or report to ASDC. Reason1:- " + ex.Message);
                    CertThumbPrint = "";
                }
                ErrorLog.LogErrorToFile(ex);
            }
           
            return "Check completed!";
        }



        public async Task<bool> IsConnectedToInternet()
        {
            
            if (ChkCrl.IsChecked == true)
            {
                IService1 service1 = new Service1();
                var hasInternetTask =await service1.HasInternetConnectionAsyncTest();
                if (hasInternetTask == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        //static bool HasInternetConnectionAsync()
        //{
        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {

        //            httpClient.Timeout = TimeSpan.FromSeconds(1); // Adjust the timeout as needed

        //            var response = httpClient.GetAsync(ConfigurationManager.AppSettings["HasInternetConnection"]);
        //            //var response = httpClient.GetAsync("https://www.google.com");

        //            // Check if the response status code indicates success
        //            if (response.Result.IsSuccessStatusCode)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MyMessageBox.ShowDialog("Error while executing HasInternetConnectionAsync Reason:- " + ex.Message);
        //        return false; // Return false if the operation was cancelled due to network not connected
        //    }

        //}

        private void DropList_DragEnter(object sender, DragEventArgs e)
        {

           
        }

        private async void DropList_Drop(object sender, DragEventArgs e)
        {
            try
            {
                await CheckSomethingAsync();

                if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                {
                    droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                    this.DropList.IsEnabled = false;
                    this.BusyBar.IsBusy = true;


                    if (RDefault.IsChecked == true)
                    {
                        if (ChkBulkSign.IsChecked == true)
                        {
                            if (System.IO.File.Exists(droppedFilePaths[0]))
                            {
                                MyMessageBox.ShowDialog("Please select folder!");
                                this.DropList.IsEnabled = true;
                                this.BusyBar.IsBusy = false;
                                return;
                            }
                            BulkDigitalSig(droppedFilePaths[0]);
                        }
                        else
                        {
                            if (droppedFilePaths.Length > 1)
                            {
                                MyMessageBox.ShowDialog("You can select only one file for sign !");
                                this.DropList.IsEnabled = true;
                                this.BusyBar.IsBusy = false;
                                return;
                            }
                            onlineDigitalSig(droppedFilePaths);
                        }
                    }
                    else
                    {
                        Card1.Width = 750;
                        pdfviewer.Visibility = Visibility.Visible;
                        LoadPdf(droppedFilePaths);
                    }



                }
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private CancellationTokenSource cancellationTokenSource;

        private async void BulkDigitalSig(string Directory, int x = 0, int y = 0)
        {
            try
            {
                int Pagenumber = 1;

                if (this.CPage.IsChecked == true)
                {
                    if (this.TxtCPage.Text == "")
                    {
                        MyMessageBox.ShowDialog("Please Enter Page Number");
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        return;
                    }
                    else
                    {
                        Pagenumber = Convert.ToInt32(TxtCPage.Text);
                    }
                }
                
                string apiUrl = UrlApi+"/DigitalSignBulkAsync";

                List<DigitalSignData> senddataList = new List<DigitalSignData>();

               
                    DigitalSignData senddata = new DigitalSignData();
                    senddata.Thumbprint = CertThumbPrint;
                    senddata.FolderLoc = Directory;
                    senddata.OutputFolderLoc = Directory;
                    senddata.XCoordinate = x;
                    senddata.YCoordinate = y;
                    senddata.Page = Pagenumber;
                    senddataList.Add(senddata);
                

                string SendJaon = Newtonsoft.Json.JsonConvert.SerializeObject(senddataList.ToArray());
                var content = new StringContent(SendJaon, Encoding.UTF8, "application/json");
                var client = new HttpClient();
                // var response = await client.PostAsync(apiUrl, content);
                IService1 service1=new Service1();
                var apiResponse = await service1.DigitalSignBulkAsync(senddataList);

                if (apiResponse != null)
                {
                   // string ResponseContent = await response.Content.ReadAsStringAsync();
                    //ResponseBulkSign apiResponse = JsonConvert.DeserializeObject<ResponseBulkSign>(ResponseContent);
                    string resultstring = "";
                    int count = 0;
                    int Signed = 0;
                   if (apiResponse.ResponseMessage!=null)
                    {
                        resultstring = "Congratulations!\n\nDocument is successfully Signed.\n";
                        resultstring += apiResponse.ResponseMessage.Message + "\n";
                        Signed =1;
                    }
                    foreach (ResponseMessage data in apiResponse.ResponseMessagelst)
                    {
                       
                            if(count==0)
                            {
                                resultstring += "\n Opps!\nDocument is Not successfully Signed.\n";
                                resultstring += "This Docu Not Sign Either Password Protected or Page Not Found.\n";

                                count++;
                            }
                            
                            resultstring += data.Message + "\n ";

                           
                       
                       
                      
                    }
                    if (resultstring != "")
                    {
                        this.DropList.IsEnabled = true;
                        this.BusyBar.IsBusy = false;
                        var result = this.Dispatcher.Invoke(new Func<string>(() =>
                        {
                            if(Signed>0)
                            {
                                return MyMessageBox.ShowDialog(resultstring + "\n" + Directory, MyMessageBox.Buttons.OK_PathOpen);
                            }
                            else
                            {
                                return MyMessageBox.ShowDialog(resultstring , MyMessageBox.Buttons.OK_PathOpen);
                            }
                        }));

                        if (result == "2")
                        {
                            try
                            {
                                Process.Start(Directory);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("An error occurred: " + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        if(apiResponse.ResponseMessage!=null)
                        {
                            MyMessageBox.Show($"Error: {apiResponse.ResponseMessage.Message}");
                            BusyBar.IsBusy = false;
                            DropList.IsEnabled = true;
                        }
                    }
                }

                else
                {
                    MyMessageBox.Show($"Error: {apiResponse.ResponseMessage.Message}");
                    BusyBar.IsBusy = false;
                    DropList.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                BusyBar.IsBusy = false;
                DropList.IsEnabled = true;
                ErrorLog.LogErrorToFile(ex);
            }
        }


        private void onlineDigitalSig(string[] files, int x = 0, int y = 0, int pageNumber = 0)
        {
            bool CheckCrl = false;
            String NewFileName = "";
            //List<string> Passfiles = new List<string>();
            int pagecount = 0;
            int IntPrintPageNo = 1;
            Boolean custom = false;
            try
            {
                foreach (string filename in files)
                {
                nextfile:
                    string fileforloop = filename;

                    if (NewFileName != "")
                    {
                        fileforloop = NewFileName;
                    }
                    else
                    {
                        fileforloop = filename;
                    }



                    if (Path.GetExtension(fileforloop) == ".pdf")
                    {
                        


                        //Add page counting for stamping
                        //21-11-2022 by Nitesh Vishwkarma
                        Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument();
                        document.LoadFromFile(fileforloop);
                        pagecount = document.Pages.Count;
                        document.Close();


                        //Add custom location for stamping i.e. first page,last page and custom
                        //21-11-2022 by Nitesh Vishwkarma

                        if (LPage.IsChecked == true)
                        {
                            IntPrintPageNo = pagecount;
                        }

                        if (RCustom.IsChecked == true)
                        {
                            custom = true;
                            IntPrintPageNo = pageNumber;
                        }
                        else
                        {
                            DownloadPath = Path.GetDirectoryName(filename);
                            ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(filename);
                        }

                        if (ChkCrl.IsChecked == true)
                        {
                            CheckCrl = true;
                        }

                        fileName = Path.GetFileNameWithoutExtension(fileforloop);

                        BusyBar.IsBusy = true;

                        cancellationTokenSource = new CancellationTokenSource();
                        new Thread(() => SignDocument(DownloadPath, fileforloop, IntPrintPageNo, x, y, custom, CheckCrl, cancellationTokenSource.Token)).Start();
                        //Passfiles.Add(fileforloop);
                        

                    }
                    //Add for word document
                    //ver 1.2.0.1 // 27-12-2022 by Nitesh Vishwkarma
                    else if (Path.GetExtension(filename) == ".docx" || Path.GetExtension(filename) == ".doc" || Path.GetExtension(filename) == ".DOCX")
                    {

                        DropList.IsEnabled = false;
                        BusyBar.IsBusy = true;
                        String DocfileName = Path.GetFileNameWithoutExtension(filename);
                        NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";
                        ConvertPDF(filename, NewFileName, WdSaveFormat.wdFormatPDF);
                        goto nextfile;
                    }
                    else
                    {

                        MyMessageBox.ShowDialog("Support only .pdf/.doc/.docx");
                        NewFileName = "";
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                    }
                }
                //new Thread(() => SignDocumentMultiple(DownloadPath, Passfiles.ToArray(), IntPrintPageNo, x, y, custom, CheckCrl, cancellationTokenSource.Token)).Start();
            }
            catch (Exception ex)
            {

                MyMessageBox.ShowDialog("No Docu signed! Reason2:-  " + ex.Message);
                NewFileName = "";
                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                ErrorLog.LogErrorToFile(ex);
            }
        }


        public static byte[] Sign(byte[] data, X509Certificate2 certificate)
        {
            using (var key = certificate.GetRSAPrivateKey())
            {
                return key.SignData(data,
                  HashAlgorithmName.SHA256,
                  RSASignaturePadding.Pkcs1);

            }
        }

        private void LoadPdf(string[] files)
        {
            try
            {
                foreach (string filename in files)
                {
                    DownloadPath = Path.GetDirectoryName(filename);
                    ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(filename);

                    if (Path.GetExtension(filename) == ".pdf" || Path.GetExtension(filename) == ".PDF")
                    {
                       
                        this.pdfdoc.Load(filename);
                    }
                    else if (Path.GetExtension(filename) == ".docx" || Path.GetExtension(filename) == ".doc" || Path.GetExtension(filename) == ".DOCX")
                    {
                        String NewFileName = "";
                        String DocfileName = Path.GetFileNameWithoutExtension(filename);
                        NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";

                        FileInfo f1 = new FileInfo(filename);


                       if(f1.Length>0)
                        {
                            ConvertPDF(filename, NewFileName, WdSaveFormat.wdFormatPDF);

                            this.pdfdoc.Load(NewFileName);
                        }
                        else
                        {
                         

                            Card1.Width = 700;
                            pdfviewer.Visibility = Visibility.Hidden;
                            this.BusyBar.IsBusy = false;
                            this.DropList.IsEnabled = true;
                            
                        }
                    }
                    else
                    {
                        MyMessageBox.Show("Support only PDF !");
                        Card1.Width = 700;
                        pdfviewer.Visibility = Visibility.Hidden;
                        this.BusyBar.IsBusy = false;
                        this.DropList.IsEnabled = true;
                        return;
                    }

                }

                pdfdoc.ThumbnailSettings.IsVisible = false;
                pdfdoc.IsBookmarkEnabled = false;
                pdfdoc.EnableLayers = false;
                pdfdoc.PageOrganizerSettings.IsIconVisible = false;
                pdfdoc.EnableRedactionTool = false;
                pdfdoc.FormSettings.IsIconVisible = false;

                DocumentToolbar toolbar = pdfdoc.Template.FindName("PART_Toolbar", pdfdoc) as DocumentToolbar;
                System.Windows.Controls.Primitives.ToggleButton buttonbtn = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_FileToggleButton", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnsticky = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_StickyNote", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_Ink = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_Ink", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_Highlight = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_Highlight", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_Underline = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_Underline", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_Strikethrough = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_Strikethrough", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_Fill = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_Fill", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_FreeText = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_FreeText", toolbar);
                System.Windows.Controls.Button buttonTextBoxFont = (System.Windows.Controls.Button)toolbar.Template.FindName("PART_ButtonTextBoxFont", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_Stamp = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_Stamp", toolbar);
                System.Windows.Controls.Button btnPART_ButtonSignature = (System.Windows.Controls.Button)toolbar.Template.FindName("PART_ButtonSignature", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_SelectTool = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_SelectTool", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_HandTool = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_HandTool", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_MarqueeZoom = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_MarqueeZoom", toolbar);
                System.Windows.Shapes.Rectangle btnshape = (System.Windows.Shapes.Rectangle)toolbar.Template.FindName("Part_CursorTools", toolbar);
                System.Windows.Controls.Button btnPART_ButtonTextSearch = (System.Windows.Controls.Button)toolbar.Template.FindName("PART_ButtonTextSearch", toolbar);
                System.Windows.Controls.Primitives.ToggleButton btnPART_Shapes = (System.Windows.Controls.Primitives.ToggleButton)toolbar.Template.FindName("PART_Shapes", toolbar);
                System.Windows.Shapes.Rectangle btnrectangle = (System.Windows.Shapes.Rectangle)toolbar.Template.FindName("PART_AnnotationsSeparator", toolbar);


                buttonbtn.Visibility = System.Windows.Visibility.Collapsed;
                btnsticky.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_Ink.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_Highlight.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_Underline.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_Strikethrough.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_Fill.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_FreeText.Visibility = System.Windows.Visibility.Collapsed;
                buttonTextBoxFont.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_Stamp.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_ButtonSignature.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_SelectTool.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_HandTool.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_MarqueeZoom.Visibility = System.Windows.Visibility.Collapsed;
                btnshape.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_ButtonTextSearch.Visibility = System.Windows.Visibility.Collapsed;
                btnPART_Shapes.Visibility = System.Windows.Visibility.Collapsed;
                btnrectangle.Visibility = System.Windows.Visibility.Collapsed;

                PdfLoadedDocument lDoc = new PdfLoadedDocument(pdfdoc.DocumentInfo.FilePath + pdfdoc.DocumentInfo.FileName);

                //Display previous Signature

                if (lDoc.Form != null && lDoc.Form.Fields != null)
                {
                    foreach (PdfLoadedSignatureField pdfLoadedSignatureField in lDoc.Form.Fields)

                    {
                        pdfLoadedSignatureField.Flatten = true;
                    }

                }

                pdfdoc.Load(lDoc);
                pdfdoc.AnnotationMode = PdfDocumentView.PdfViewerAnnotationMode.Rectangle;
                //Getting the page size.
                SizeF size = lDoc.Pages[0].Size;
                PdfUnitConvertor unitCvtr = new PdfUnitConvertor();
                PageWidth = Convert.ToInt32(size.Width);
                PageHeight = Convert.ToInt32(size.Height);

                pixelWidth = unitCvtr.ConvertUnits(Convert.ToInt32(size.Width), PdfGraphicsUnit.Point, PdfGraphicsUnit.Pixel);
                pixelHeight = unitCvtr.ConvertUnits(Convert.ToInt32(size.Height), PdfGraphicsUnit.Point, PdfGraphicsUnit.Pixel);

            }
            catch(Exception ex)
            {
                Card1.Width = 700;
                pdfviewer.Visibility = Visibility.Hidden;
                MyMessageBox.Show(ex.Message);
                this.BusyBar.IsBusy = false;
                this.DropList.IsEnabled = true;
                ErrorLog.LogErrorToFile(ex);
            }
        }
        private async void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            listitem.Items.Clear();
            // not is imposed to by pass the ethernet check status for not check ocsp status.
            //**

            //bool isConnected = await HasInternetConnectionAsyncTest();

            //if (isConnected)
            //{
                try
                {
                await CheckSomethingAsync();
                if (ChkBulkSign.IsChecked == true)
                {
                    // For bulk signing, select a directory
                    System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                    folderBrowserDialog.Description = "Select a folder containing PDF or DOC files";

                    if (ConfigurationManager.AppSettings["LastSelectedLocation"] == "")
                    {
                        folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                    else
                    {
                        folderBrowserDialog.SelectedPath = ConfigurationManager.AppSettings["LastSelectedLocation"];
                    }

                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string selectedDirectory = folderBrowserDialog.SelectedPath;

                        this.DropList.IsEnabled = false;
                        this.BusyBar.IsBusy = true;

                        string[] fileNames = Directory.GetFiles(selectedDirectory, "*.*", SearchOption.TopDirectoryOnly);

                        if (RDefault.IsChecked == true)
                        {
                            BulkDigitalSig(selectedDirectory);
                        }
                        else
                        {
                            Card1.Width = 750;
                            pdfviewer.Visibility = Visibility.Visible;
                            
                            LoadPdf(fileNames);
                        }
                    }
                }
                else
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "files (*.pdf;*.PDF;*.docx;*.DOCX;*.doc;*.DOC)|*.pdf;*.PDF;*.docx;*.DOCX,*.doc; *.DOC";

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
                        string selectedFile = openFileDialog.FileName;

                         this.DropList.IsEnabled = false;
                        this.BusyBar.IsBusy = true;

                        if (RDefault.IsChecked == true)
                        {
                            onlineDigitalSig(new[] { selectedFile });
                        }
                        else
                        {
                            Card1.Width = 750;
                             pdfviewer.Visibility = Visibility.Visible;
                            LoadPdf(new[] { selectedFile });
                        }
                    }
                }
            }
                catch (ArgumentOutOfRangeException ex)
                {
                ErrorLog.LogErrorToFile(ex);
            }
                catch (Exception ex)
                {
                    MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
           

        }

        
        public class CertificateData
        {
            public string API { get; set; }
            public bool CRL_OCSPCheck { get; set; }
            public String CRL_OCSPMsg { get; set; }
            public string Remarks { get; set; }
            public string Status { get; set; }
            public string Thumbprint { get; set; }
            public string ValidFrom { get; set; }
            public string ValidTo { get; set; }
            public string issuer { get; set; }
            public string subject { get; set; }
            public Boolean TokenValid { get; set; }
        }

        /// <summary>
        /// Pdf signature default and custom
        /// </summary>
      
        public async void SignDocument(string downloadfilePath,string filename, int PageNum, int X = 0, int Y = 0, Boolean custom = false, Boolean BlnCheckCrl = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool ValidToken = false;
            string TokenRemarks = "";
            String FileFullName = "";
            Boolean ErrorEncountered = false;
            PdfSigner signer1 = null;
            FileStream fileStream = null;
            CertificateData certificateData = null;
            X509Certificate2 cert1 = null;
            try
            {

                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));

                bool CheckCrlTick = this.Dispatcher.Invoke(new Func<bool>(() => this.ChkCrl.IsChecked==true));


                if (CertThumbPrint == null || CertThumbPrint == "")
                {
                    string response = await GetTokenDetail(false,CertThumbPrint);
                    List<CertificateData> certificates = JsonConvert.DeserializeObject<List<CertificateData>>(response);
                    certificateData = certificates[0];
                    ValidToken = certificates[0].TokenValid;
                    TokenRemarks = certificates[0].Remarks;

                    if (ValidToken == false)
                    {
                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(TokenRemarks)));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        return;
                    }
                    else
                    {
                        bool crloscp1 = certificateData.CRL_OCSPCheck;
                        string crlocspmsg1 = certificateData.CRL_OCSPMsg;

                        if (CheckCrlTick == true)
                        { 
                            if (crloscp1 == true && crlocspmsg1 == "Digital Cert of token cannot be verified with CA due to Network issues")
                            {
                                bool CloseThread = false;
                                this.Dispatcher.Invoke(() =>
                                {
                                    if (MyMessageBox.ShowDialog("Digital Cert of token cannot be verified with CA due to Network issues. Do you want to continue ?", MyMessageBox.Buttons.Yes_No) != "1")
                                    {
                                        CloseThread = true;
                                    }
                                });

                                if (CloseThread == true)
                                {
                                    this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Signed !")));
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                    return;
                                }
                            }
                        }

                        X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadOnly);
                        X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, certificateData.Thumbprint, false);
                        store.Close();
                        cert1 = certCollection[0];
                    }
                }
                else
                {
                    X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, CertThumbPrint, false);
                    store.Close();

                    cert1 = certCollection[0];

                    //if (DateTime.Now > cert1.NotAfter)
                    //{
                    //    this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Token is expired. Pl contact issuer !")));
                    //    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                    //    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                    //    return;
                    //}

                    if (CheckCrlTick == true)
                    {
                        if (crloscp == true && crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues")
                        {
                            bool CloseThread = false;
                            this.Dispatcher.Invoke(() =>
                            {
                                if (MyMessageBox.ShowDialog("Digital Cert of token cannot be verified with CA due to Network issues. Do you want to continue ?", MyMessageBox.Buttons.Yes_No) != "1")
                                {
                                    CloseThread = true;
                                }
                            });

                            if (CloseThread == true)
                            {
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Signed !")));
                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                return;
                            }
                        }
                    }
                }

                    //check the remark value 
                    //ver 1.2.0.1 // 30-12-2022 by Nitesh Vishwkarma 
                    String StrRemark = this.Dispatcher.Invoke(new Func<string>(() => this.textRemark.Text.ToString()));

                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                    PdfReader reader = new PdfReader(filename);
                    reader.SetUnethicalReading(true);
                    Thread t = new Thread((ThreadStart)(() =>
                    {
                        try
                        {
                            IExternalSignature es = new X509Certificate2Signature(cert1, "SHA-1", ref message);
                            if (message != null)
                            {
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(message)));
                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                return;
                            }
                            else
                            {
                                if (filename != "")
                                {
                                    try
                                    {
                                        //download = saveFileDialog.SelectedPath + @"\";

                                        download = downloadfilePath + @"\";

                                        if (es.GetEncryptionAlgorithm() != null)
                                        {
                                            Org.BouncyCastle.X509.X509CertificateParser cp1 = new Org.BouncyCastle.X509.X509CertificateParser();
                                            Org.BouncyCastle.X509.X509Certificate[] chain3 = new[] { cp1.ReadCertificate(cert1.RawData) };
                                            try
                                            {
                                                StampingProperties stampProp = new StampingProperties();
                                                stampProp.PreserveEncryption();
                                                ImageData imageData = null;
                                                //change for icon in case of remark
                                                //ver 1.2.0.1 // 30-12-2022 by Nitesh Vishwkarma 
                                                if (StrRemark != "")
                                                {
                                                    using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSign.png"))
                                                    {
                                                        imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSign.png");
                                                    }
                                                }
                                                else
                                                {
                                                    using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSignWT.png"))
                                                    {
                                                        imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSignWT.png");
                                                    }
                                                }


                                                string[] SubjectSplit = cert1.Subject.Split(',');
                                                string StrName = SubjectSplit[0].ToString().Replace("CN=", "").Trim();
                                                string StrICNo = SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();
                                                string StrRank = SubjectSplit[2].ToString().Replace("T=", "").Trim();

                                                iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new PdfReader(filename));
                                                SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                                                IList<string> sigNames = signatureUtil.GetSignatureNames();
                                                pdfDocument.Close();

                                                FileFullName = downloadfilePath + "\\" + fileName + "_DS_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + ".pdf";
                                                iText.Kernel.Font.PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_BOLD));

                                                String StrSignature = "";
                                                if (StrRemark != "")
                                                {
                                                    StrSignature = StrRemark + "\n\n Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " \n © DGIS App, IA";
                                                }
                                                else
                                                {
                                                    StrSignature = "Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " \n © DGIS App, IA";
                                                }

                                                if (custom == false)
                                                {
                                                    if (sigNames.Count == 0)
                                                    {

                                                        try
                                                        {
                                                            fileStream = new FileStream(FileFullName, FileMode.Create);
                                                            //signer1 = new PdfSigner(reader, new FileStream(FileFullName, FileMode.Create), new StampingProperties());
                                                            signer1 = new PdfSigner(reader, fileStream, new StampingProperties());
                                                        }
                                                        catch (Exception)
                                                        {
                                                            //signer1.ToString();
                                                            //.SignDetached(reader, new FileStream(FileFullName, FileMode.Create), new StampingProperties()) ;
                                                        }

                                                        PdfSignatureAppearance appearance = signer1.GetSignatureAppearance()

                                                               .SetLayer2Text(StrSignature)
                                                               .SetImage(imageData).SetImageScale(-50)
                                                               .SetReuseAppearance(false);
                                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 80);
                                                        //change for width of rectangle of remark 
                                                        //ver 1.2.0.1 // 30-12-2022 by Nitesh Vishwkarma 
                                                        if (StrRemark == "")
                                                        {
                                                            rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 50);
                                                        }
                                                        else
                                                        {
                                                            rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 80);
                                                        }
                                                        appearance
                                                                .SetPageRect(rect)
                                                                .SetPageNumber(PageNum);
                                                        signer1.SetFieldName(signer1.GetNewSigFieldName());
                                                        //CADES

                                                        //change CryptoStandard from CADES to CMS for display information of token in e-office
                                                        //21-11-2022 by Nitesh Vishwkarma
                                                        try
                                                        {
                                                            signer1.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                        }
                                                        catch
                                                        {
                                                            ErrorEncountered = true;
                                                            //signer1 = null; //*
                                                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));

                                                        }
                                                    }
                                                    else
                                                    {
                                                        fileStream = new FileStream(FileFullName, FileMode.Create);
                                                        PdfSigner signer = new PdfSigner(reader, fileStream, stampProp.UseAppendMode());
                                                        PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                                             .SetLayer2Text(StrSignature)
                                                             .SetImage(imageData).SetImageScale(-50)
                                                             .SetReuseAppearance(false);
                                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 80);

                                                        //Add 10 rectangle for stamping
                                                        //21-11-2022 by Nitesh Vishwkarma

                                                        //change for width of rectangle of remark 
                                                        //ver 1.2.0.1 // 30-12-2022 by Nitesh Vishwkarma 
                                                        if (signer.GetNewSigFieldName() == "Signature1")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature2")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(40, 65, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(40, 95, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature3")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 65, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 95, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature4")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(400, 65, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(400, 95, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature5")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(40, 115, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(40, 175, 180, 80);
                                                            }

                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature6")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 115, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 175, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature7")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(400, 115, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(400, 175, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature8")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(40, 165, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(40, 225, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature9")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 165, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(220, 225, 180, 80);
                                                            }
                                                        }
                                                        else if (signer.GetNewSigFieldName() == "Signature10")
                                                        {
                                                            if (StrRemark == "")
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(400, 165, 180, 50);
                                                            }
                                                            else
                                                            {
                                                                rect = new iText.Kernel.Geom.Rectangle(400, 225, 180, 80);
                                                            }
                                                        }
                                                        appearance
                                                                .SetPageRect(rect)
                                                                .SetPageNumber(PageNum);
                                                        signer.SetFieldName(signer.GetNewSigFieldName());
                                                        //CADES

                                                        //change CryptoStandard from CADES to CMS for display information of token in e-office
                                                        //21-11-2022 by Nitesh Vishwkarma
                                                        try
                                                        {
                                                            signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                        }
                                                        catch(Exception ex)
                                                        {
                                                            
                                                            ErrorEncountered = true;
                                                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                            ErrorLog.LogErrorToFile(ex);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (sigNames.Count == 0)
                                                    {
                                                        fileStream = new FileStream(FileFullName, FileMode.Create);
                                                        PdfSigner signer = new PdfSigner(reader, fileStream, new StampingProperties());
                                                        PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                                             .SetLayer2Text(StrSignature)
                                                             .SetImage(imageData).SetImageScale(-50)
                                                             .SetReuseAppearance(false);
                                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);
                                                        //change for width of rectangle of remark 
                                                        //ver 1.2.0.1 // 30-12-2022 by Nitesh Vishwkarma 
                                                        if (StrRemark == "")
                                                        {
                                                            rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 50);
                                                        }
                                                        else
                                                        {
                                                            rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);
                                                        }
                                                        appearance
                                                                .SetPageRect(rect)
                                                                .SetPageNumber(PageNum);
                                                        signer.SetFieldName(signer.GetNewSigFieldName());
                                                        //CADES

                                                        //change CryptoStandard from CADES to CMS for display information of token in e-office
                                                        //21-11-2022 by Nitesh Vishwkarma
                                                        try
                                                        {
                                                            signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                        }
                                                        catch(Exception ex)
                                                        {
                                                            ErrorEncountered = true;
                                                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                            ErrorLog.LogErrorToFile(ex);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        fileStream = new FileStream(FileFullName, FileMode.Create);
                                                        PdfSigner signer = new PdfSigner(reader, fileStream, stampProp.UseAppendMode());
                                                        PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                                             .SetLayer2Text(StrSignature)
                                                             .SetImage(imageData).SetImageScale(-50)
                                                             .SetReuseAppearance(false);
                                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);
                                                        //change for width of rectangle of remark 
                                                        //ver 1.2.0.1 // 30-12-2022 by Nitesh Vishwkarma 
                                                        if (StrRemark == "")
                                                        {
                                                            rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 50);
                                                        }
                                                        else
                                                        {
                                                            rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);
                                                        }
                                                        appearance
                                                               .SetPageRect(rect)
                                                               .SetPageNumber(PageNum);
                                                        signer.SetFieldName(signer.GetNewSigFieldName());
                                                        //CADES

                                                        //change CryptoStandard from CADES to CMS for display information of token in e-office
                                                        //21-11-2022 by Nitesh Vishwkarma
                                                        try
                                                        {
                                                            signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                        }
                                                        catch(Exception ex)
                                                        {
                                                            ErrorEncountered = true;
                                                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                            ErrorLog.LogErrorToFile(ex);
                                                        }
                                                    }
                                                }
                                                reader.Close();
                                                if (ErrorEncountered == false)
                                                {
                                                    string Result = "0";
                                                    this.Dispatcher.Invoke(() =>
                                                    {
                                                        Result = MyMessageBox.ShowDialog("Congratulations ! \n\n Document is Digitally Signed. \n " + download, MyMessageBox.Buttons.OK_OpenFile);
                                                    });
                                                    if (Result == "2")
                                                    {
                                                        string FilePath = Path.GetDirectoryName(FileFullName);
                                                        Process.Start(FilePath);
                                                    }
                                                    else if (Result == "3")
                                                    {
                                                        try
                                                        {
                                                            Process.Start(FileFullName);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Console.WriteLine("An error occurred: " + ex.Message);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                reader.Close();
                                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                ErrorLog.LogErrorToFile(ex);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        reader.Close();
                                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                                        ErrorLog.LogErrorToFile(ex);
                                    }
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                }
                                else
                                {
                                    reader.Close();
                                    this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                            ErrorLog.LogErrorToFile(ex);
                        }
                        finally
                        {
                            reader.Close();
                        }
                    }));


                    // Run your code from a thread that joins the STA Thread
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    t.Join();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                Console.ReadLine();
                ErrorLog.LogErrorToFile(ex);
            }
            catch (CryptographicException ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
            catch (iText.Kernel.PdfException ex)
            {
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                ErrorLog.LogErrorToFile(ex);
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                ErrorLog.LogErrorToFile(ex);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                this.Dispatcher.Invoke(new Action(() => pdfdoc.Unload()));
            }
            
            if (FileFullName != "")
            {
                FileInfo fi = new FileInfo(FileFullName);
                if (fi.Length == 0)
                {
                    File.Delete(FileFullName);

                    String DocfileName = Path.GetFileNameWithoutExtension(filename);
                    string originWordFile = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";
                    FileInfo f2 = new FileInfo(originWordFile);
                    if (f2.Exists)
                    {
                        try
                        {
                            File.Delete(originWordFile);
                        }
                        catch
                        { }
                    }
                }
            }
        }

        public static ICollection<byte[]> ProcessCrl(System.Security.Cryptography.X509Certificates.X509Certificate cert, ICollection<ICrlClient> crlList)
        {
            if (crlList == null)
                return null;
            List<byte[]> crlBytes = new List<byte[]>();
            foreach (ICrlClient cc in crlList)
            {
                if (cc == null)
                    continue;
                ICollection<byte[]> b = cc.GetEncoded(cert, null);
                if (b == null)
                    continue;
                crlBytes.AddRange(b);
            }
            if (crlBytes.Count == 0)
                return null;
            else
                return crlBytes;
        }

        private void FPage_Click(object sender, RoutedEventArgs e)
        {
            TxtCPage.Visibility = Visibility.Hidden;
            CPage.Visibility = Visibility.Hidden;
            if(ChkBulkSign.IsChecked==true)
            {
                CPage.Visibility = Visibility.Visible;
                TxtCPage.Text = "";
                TxtCPage.Visibility = Visibility.Visible;
            }
        }

        private async void pdfdoc_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Drawing.Point position = new System.Drawing.Point
            {
                X = (int)e.GetPosition(pdfdoc).X,
                Y = (int)e.GetPosition(pdfdoc).Y
            };
            int x = position.X;
            int y = position.Y;

            string[] bytes = new string[1];
            bytes[0] = pdfdoc.DocumentInfo.FileName;

            if (MyMessageBox.ShowDialog("Do you want to sign here ?", MyMessageBox.Buttons.Yes_No) == "1")
            {
                if (await IsConnectedToInternet())
                {
                    onlineDigitalSig(bytes, x, y);
                }
                else
                {
                    onlineDigitalSig(bytes, x, y);
                }
                Card1.Width = 700;
                pdfviewer.Visibility = Visibility.Hidden;
            }
        }

        private void Default_Click(object sender, RoutedEventArgs e)
        {
            LPage.IsEnabled = true;
            FPage.IsEnabled = true;
            FPage.IsChecked = true;
            LPage.Visibility = Visibility.Visible;
            ChkBulkSign.IsEnabled = true;
            btnOpenFile.Content = "Select Document";
        }

        private void Custom_Click(object sender, RoutedEventArgs e)
        {
            LPage.IsEnabled = false;
            FPage.IsEnabled = false;
            ChkBulkSign.IsChecked = false;
            ChkBulkSign.IsEnabled = false;
            TxtCPage.Text = "";
            TxtCPage.Visibility = Visibility.Hidden;
            CPage.Visibility = Visibility.Hidden;
            btnOpenFile.Content = "Select Document";

        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Card1.Width = 700;
            pdfviewer.Visibility = Visibility.Hidden;
            this.BusyBar.IsBusy = false;
            this.DropList.IsEnabled = true;
        }

        private async void pdfdoc_ShapeAnnotationChanged(object sender, Syncfusion.Windows.PdfViewer.ShapeAnnotationChangedEventArgs e)
        {
            int PagePointX = 0;
            int PagePointY = 0;
            System.Drawing.RectangleF bounds = e.NewBounds;
            int x = Convert.ToInt32(bounds.X);
            int y = Convert.ToInt32(bounds.Y);

            PagePointX = x;
            //check for remark and increase/d//Add for signature
            //ver 1.2.0.1 // 30-12-2022 by Nitesh Vishwkarma 
            PdfLoadedDocument lDoc = new PdfLoadedDocument(pdfdoc.DocumentInfo.FilePath + pdfdoc.DocumentInfo.FileName);
            SizeF size = lDoc.Pages[(e.PageNumber) - 1].Size;

            PageWidth = Convert.ToInt32(size.Width);
            PageHeight = Convert.ToInt32(size.Height);


            if (textRemark.Text == "")
            {
                PagePointY = ((PageHeight - 50) - y);
            }
            else
            {
                PagePointY = ((PageHeight - 70) - y);
            }

            string[] bytes = new string[1];
            bytes[0] = pdfdoc.DocumentInfo.FilePath + pdfdoc.DocumentInfo.FileName;
            int pageNumber = e.PageNumber;
            if (MyMessageBox.ShowDialog("Do you want to sign here ?", MyMessageBox.Buttons.Yes_No) == "1")
            {
                bool isConnected = await HasInternetConnectionAsyncTest();

                if (isConnected)
                {
                    onlineDigitalSig(bytes, PagePointX, PagePointY, pageNumber);
                }
                else
                {
                    //offlineDigitalSig(bytes, PagePointX, PagePointY, pageNumber);
                    onlineDigitalSig(bytes, PagePointX, PagePointY, pageNumber);
                }
                Card1.Width = 700;
                pdfviewer.Visibility = Visibility.Hidden;
            }
            else
            {
                pdfdoc.UndoRedoSettings.PerformUndo();
            }
        }

        public static void ConvertPDF(string inputpath, string outputPath, WdSaveFormat format)
        {
            try
            {
                FileInfo f1 = new FileInfo(outputPath);
                if (f1.Exists)
                {
                    File.Delete(outputPath);
                }

                WordDocument wordDocument = new WordDocument(inputpath, Syncfusion.DocIO.FormatType.Docx);
                wordDocument.ChartToImageConverter = new ChartToImageConverter();
                wordDocument.ChartToImageConverter.ScalingMode = ScalingMode.Normal;
                DocToPDFConverter converter = new DocToPDFConverter();
                converter.Settings.EnableFastRendering = true;
                Syncfusion.Pdf.PdfDocument pdfDocument = converter.ConvertToPDF(wordDocument);
                pdfDocument.Save(outputPath);
                pdfDocument.Close(true);
                wordDocument.Close();
            }
            catch(Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private async void ChkCrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ChkCrl.IsChecked == true)
                {
                    bool isConnected = await HasInternetConnectionAsyncTest();
                    if (isConnected)
                    {
                        //await Task.Delay(10000);
                        ChkCrl.Background = Brushes.Green;

                        string Certificate = await GetTokenDetail(true, CertThumbPrint);

                        if (Certificate != "")
                        {
                            List<CertificateData> certificates = JsonConvert.DeserializeObject<List<CertificateData>>(Certificate);
                            CertificateData certificateData = certificates[0];

                            bool ValidToken = certificates[0].TokenValid;
                            string TokenRemarks = certificates[0].Remarks;
                            if (ValidToken == false)
                            {
                                MyMessageBox.ShowDialog(TokenRemarks);
                                return;
                            }
                            else
                            {
                                crloscp = certificateData.CRL_OCSPCheck;
                                crlocspmsg = certificateData.CRL_OCSPMsg;

                                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                                store.Open(OpenFlags.ReadOnly);
                                X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, certificateData.Thumbprint, false);
                                store.Close();

                                X509Certificate2 cert1 = certCollection[0];
                            }
                        }
                        //MyMessageBox.Show("Connected to the internet & CRL Checked.");

                    }
                    else

                    {
                        ChkCrl.Background = Brushes.Red;
                        crloscp = true;
                        crlocspmsg = "";
                        //MyMessageBox.Show("Pl check you Nw connection and Try Again!");
                    }
                }
                else
                {
                    crloscp = true;
                    crlocspmsg = "";
                }
            }
            catch (Exception ex)
            {
                //MyMessageBox.ShowDialog(ex.Message, MyMessageBox.Buttons.OK);
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private async Task<string> GetTokenDetail(bool IsCheckCrl,string Thumb)
        {
            try
            {
                string response = await GetRequest(UrlApi+"/FetchTokenOCSPCrlDetails?IsCheckCrl="+ IsCheckCrl +"&ThumbPrint=" + Thumb + "");
                return response;
            }
            catch (HttpRequestException)
            {
                return "";
            }
        }

        private async Task<bool> HasInternetConnectionAsyncTest()
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(2);
                    //var request = new HttpRequestMessage(HttpMethod.Head, "https://google.com");
                   // var request = new HttpRequestMessage(HttpMethod.Head, "https://portal.army.mil"); old code
                    var request = new HttpRequestMessage(HttpMethod.Head, ConfigurationManager.AppSettings["HasInternetConnection"]);
                    try
                    { 
                        var response = await httpClient.SendAsync(request);
                        return response.IsSuccessStatusCode;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        private void ChkBulkSign_Click(object sender, RoutedEventArgs e)
        {
            if (ChkBulkSign.IsChecked == true)
            {
                btnOpenFile.Content = "Select Directory";
                LPage.Visibility = Visibility.Hidden;
                TxtCPage.IsEnabled = false;
                CPage.Visibility = Visibility.Visible;
                TxtCPage.Visibility = Visibility.Visible;
                FPage.IsChecked = true;
            }
            else
            {
                btnOpenFile.Content = "Select Document";
                LPage.Visibility = Visibility.Visible;
                CPage.Visibility = Visibility.Hidden;
                TxtCPage.Visibility = Visibility.Hidden;
                FPage.IsChecked = true;
                TxtCPage.Text = "";
            }  
        }

        private void CPage_Click(object sender, RoutedEventArgs e)
        {
            TxtCPage.IsEnabled = true;
        }

        private void TxtCPage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!int.TryParse(textBox.Text, out _))
                {
                    textBox.Text = textBox.Text.Length > 0 ? textBox.Text.Substring(0, textBox.Text.Length - 1) : "";
                }
            }
        }




       
    }
}

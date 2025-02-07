
using iText.IO.Font;

using iText.IO.Font.Constants;

using iText.Kernel.Colors;

using iText.Kernel.Font;

using iText.Kernel.Pdf;

using iText.Kernel.Pdf.Canvas;

using iText.Kernel.Pdf.Extgstate;

using Microsoft.Office.Interop.Word;

using Microsoft.Win32;
using SignService;
using Syncfusion.DocIO.DLS;

using Syncfusion.DocToPDFConverter;

using Syncfusion.OfficeChart;

using Syncfusion.OfficeChartToImageConverter;

using System;

using System.Configuration;

using System.Diagnostics;

using System.IO;

using System.Net;

using System.Windows;

using System.Windows.Controls;

using WinniesMessageBox;



namespace DGISApp

{

    /// <summary>

    /// Interação lógica para UserControlEscolha.xam

    /// </summary>

    public partial class Watermark : UserControl

    {



        string[] droppedFilePaths = null;

        string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";



        public Watermark()

        {

            InitializeComponent();

        }



        void check(string filename, double pr)

        {



        }



        private void DropList_DragEnter(object sender, DragEventArgs e)

        {

        }



        private void DropList_Drop(object sender, DragEventArgs e)

        {

            try

            {

                if (datetime.IsChecked == true || ipaddress.IsChecked == true || textBoxEmail.Text != "")

                {

                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))

                    {

                        DropList.IsEnabled = false;
                        BusyBar.IsBusy = true;
                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                        this.upload();

                        DropList.IsEnabled = true;
                        BusyBar.IsBusy = false;
                    }

                }

                else

                {

                    MyMessageBox.ShowDialog("Please select atleast one option for Watermarking");

                }



            }

            catch (Exception ex)

            {

                MyMessageBox.ShowDialog(ex.Message);

                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
                ErrorLog.LogErrorToFile(ex);
            }

        }



        void upload()

        {

            string DownloadPath = "";

            String NewFileName = "";

            string WatermarkedPDFFileName = "";

            string WaterMarkingText = this.textBoxEmail.Text.ToString();



            // Split the input string by comma and store the result in a string array

            string[] stringArray = this.textBoxEmail.Text.ToString().Split(',');



            int j = 0;

            foreach (var path in droppedFilePaths)

            {

            nextfile:

                string fileforloop = path;

                ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(path);

                DownloadPath = Path.GetDirectoryName(path);



                if (NewFileName != "")

                {

                    fileforloop = NewFileName;

                }

                else

                {

                    fileforloop = path;

                }

                FileInfo fi = new FileInfo(fileforloop);

                if (fi.Extension == ".pdf")

                {



                    foreach (string item in stringArray)

                    {

                        WaterMarkingText = item;



                        WatermarkedPDFFileName = DownloadPath + "\\" + fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + "_WM_" + WaterMarkingText + "_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + ".pdf";

                        PdfDocument pdfDoc = new PdfDocument(new PdfReader(fi.FullName), new PdfWriter(WatermarkedPDFFileName));

                        PdfCanvas under = new PdfCanvas(pdfDoc.GetFirstPage().NewContentStreamBefore(), new PdfResources(), pdfDoc);

                        PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_ROMAN));



                        iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph("This watermark is added UNDER the existing content")

                                .SetFont(font)

                                .SetBold()

                                .SetFontColor(ColorConstants.RED)

                                .SetFontSize(48);



                        // Print each element of the string array







                        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)

                        {

                            PdfCanvas over = new PdfCanvas(pdfDoc.GetPage(i));

                            if (this.Dispatcher.Invoke(new Func<bool?>(() => this.datetime.IsChecked)) == true && this.Dispatcher.Invoke(new Func<bool?>(() => this.ipaddress.IsChecked)) == false)

                            {

                                paragraph = new iText.Layout.Element.Paragraph(DateTime.Now.ToString() + "\n" + this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                    .SetFont(font)

                                  .SetFontColor(ColorConstants.RED)

                                  .SetFontSize(68);

                                over.SaveState();

                                PdfExtGState gs3 = new PdfExtGState();

                                gs3.SetFillOpacity(0.5f);

                                over.SetExtGState(gs3);

                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                        .ShowTextAligned(paragraph, 297, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                canvasWatermark.Close();

                            }



                            else if (this.Dispatcher.Invoke(new Func<bool?>(() => this.ipaddress.IsChecked)) == true && this.Dispatcher.Invoke(new Func<bool?>(() => this.datetime.IsChecked)) == false)

                            {

                                IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;

                                string ip = a[0].ToString();

                                paragraph = new iText.Layout.Element.Paragraph(ip + "\n" + this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                   .SetFont(font)

                                  .SetFontColor(ColorConstants.RED)

                                  .SetFontSize(68);

                                over.SaveState();

                                PdfExtGState gs3 = new PdfExtGState();

                                gs3.SetFillOpacity(0.5f);

                                over.SetExtGState(gs3);

                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                        .ShowTextAligned(paragraph, 297, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                canvasWatermark.Close();

                            }



                            else if (this.Dispatcher.Invoke(new Func<bool?>(() => this.datetime.IsChecked)) == true && this.Dispatcher.Invoke(new Func<bool?>(() => this.ipaddress.IsChecked)) == true)

                            {

                                IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;

                                string ip = a[0].ToString();

                                paragraph = new iText.Layout.Element.Paragraph(DateTime.Now.ToString() + "\n" + ip + "\n" + this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                  .SetFont(font)

                                  .SetFontColor(ColorConstants.RED)

                                  .SetFontSize(68);

                                over.SaveState();

                                PdfExtGState gs3 = new PdfExtGState();

                                gs3.SetFillOpacity(0.5f);

                                over.SetExtGState(gs3);

                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                        .ShowTextAligned(paragraph, 200, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                canvasWatermark.Close();

                            }

                            else

                            {

                                paragraph = new iText.Layout.Element.Paragraph(this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                      .SetFont(font)

                                      .SetFontColor(ColorConstants.RED)

                                      .SetFontSize(68);

                                over.SaveState();

                                PdfExtGState gs3 = new PdfExtGState();

                                gs3.SetFillOpacity(0.5f);

                                over.SetExtGState(gs3);

                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                        .ShowTextAligned(paragraph, 297, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                canvasWatermark.Close();

                            }

                            over.RestoreState();

                        }

                        pdfDoc.Close();

                        NewFileName = "";

                    }





                    j = j + 1;

                    //MyMessageBox.ShowDialog("Congratulations ! \n\n Document is successfully WaterMarked.\n" + download);

                }

                else if (Path.GetExtension(path) == ".docx" || Path.GetExtension(path) == ".doc")

                {

                    String DocfileName = Path.GetFileNameWithoutExtension(path);

                    NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";

                    ConvertPDF(path, NewFileName, WdSaveFormat.wdFormatPDF);

                    goto nextfile;

                }

                else

                {

                    MyMessageBox.ShowDialog("Please select only PDF/Doc document for WaterMarking.");

                }

            }

            if (j == droppedFilePaths.Length)

            {

                string Result = "0";



                //MyMessageBox.ShowDialog("Congratulations ! \n\n Document is successfully WaterMarked.\n" + DownloadPath);



                Result = MyMessageBox.ShowDialog("Congratulations ! \n\n Document is successfully WaterMarked.\n" + DownloadPath, MyMessageBox.Buttons.OK_OpenFile);



                if (Result == "2")

                {

                    string FilePath = Path.GetDirectoryName(WatermarkedPDFFileName);

                    Process.Start(FilePath);

                }

                else if (Result == "3")

                {

                    try

                    {

                        Process.Start(WatermarkedPDFFileName);

                    }

                    catch (Exception ex)

                    {

                        Console.WriteLine("An error occurred: " + ex.Message);

                    }

                }



            }

            else

            {

                MyMessageBox.ShowDialog("some document not successfully WaterMarked.\n" + DownloadPath);

            }

        }



        private void Button_Click(object sender, RoutedEventArgs e)

        {



        }



        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)

        {

            try

            {

                if (datetime.IsChecked == true || ipaddress.IsChecked == true || textBoxEmail.Text != "")

                {

                    string value = datetime.IsChecked.ToString();

                    OpenFileDialog openFileDialog = new OpenFileDialog();

                    openFileDialog.Multiselect = true;

                    openFileDialog.Filter = "files (*.pdf;*.PDF;*.docx;*.DOCX;*.doc;*.DOC)|*.pdf;*.PDF;*.docx;*.DOCX,*.doc; *.DOC";

                    ///openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);



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

                        DropList.IsEnabled = false;
                        BusyBar.IsBusy = true;
                        droppedFilePaths = openFileDialog.FileNames;

                        this.upload();

                        DropList.IsEnabled = true;
                        BusyBar.IsBusy = false;

                    }

                }

                else

                {

                    MyMessageBox.ShowDialog("Please select atleast one option for Watermarking");

                }

            }

            catch (Exception ex)

            {

                MyMessageBox.ShowDialog(ex.Message);

                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
                ErrorLog.LogErrorToFile(ex);
            }

        }



        public static void ConvertPDF(string inputfilePath, string outputfilePath, WdSaveFormat format)

        {

            WordDocument wordDocument = new WordDocument(inputfilePath, Syncfusion.DocIO.FormatType.Docx);

            wordDocument.ChartToImageConverter = new ChartToImageConverter();

            wordDocument.ChartToImageConverter.ScalingMode = ScalingMode.Normal;

            DocToPDFConverter converter = new DocToPDFConverter();

            converter.Settings.EnableFastRendering = true;

            Syncfusion.Pdf.PdfDocument pdfDocument = converter.ConvertToPDF(wordDocument);

            pdfDocument.Save(outputfilePath);

            pdfDocument.Close(true);

            wordDocument.Close();

        }



    }



}
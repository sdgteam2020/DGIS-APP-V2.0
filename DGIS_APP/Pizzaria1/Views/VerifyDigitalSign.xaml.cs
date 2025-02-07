using DGISApp;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Win32;
using MyApp;
using SignService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlTypes;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using WinniesMessageBox;
using static iText.Signatures.PdfSigner;

namespace DGISApp
{
    /// <summary>
    /// Interaction logic for SumitTest.xaml
    /// </summary>
    /// 



    public partial class VerifyDigitalSign : UserControl
    {


        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        string[] droppedFilePaths = null;
        public string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";
        public VerifyDigitalSign()
        {
            InitializeComponent();

        }


        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }

        public static bool IsNetworkAvailable(long minimumSpeed)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // discard because of standard reasons
                if ((ni.OperationalStatus != OperationalStatus.Up) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                    continue;

                // this allow to filter modems, serial, etc.
                // I use 10000000 as a minimum speed for most cases
                if (ni.Speed < minimumSpeed)
                    continue;

                // discard virtual cards (virtual box, virtual pc, etc.)
                if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                    continue;

                return true;
            }
            return false;
        }

        private void DropList_DragEnter(object sender, DragEventArgs e)
        {


        }

        private void DropList_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                {
                    droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                    DropList.IsEnabled = false;
                    verifyDigitalSign(droppedFilePaths);
                    DropList.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                DropList.IsEnabled = true;
            }
        }

        void upload()
        {

        }


        public void verifyDigitalSign(string[] files)
        {
            bool NotModified = true;
            foreach (string filename in files)
            {
                ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(filename);
                string fileExtension = Path.GetExtension(filename).ToLower();
                if (fileExtension == ".pdf")
                {
                    PdfDocument pdfDocument = new PdfDocument(new PdfReader(filename));
                    // Checks that signature is genuine and the document was not modified.
                    bool genuineAndWasNotModified = false;

                    SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                    IList<string> sigNames = signatureUtil.GetSignatureNames();
                    if (sigNames.Count == 0)
                    {
                        MyMessageBox.Show("Digital Signature not found.");
                        return;
                    }
                    else
                    {
                        int numValid = 0;
                        int numinvalid = 0;
                        foreach (string sigName in sigNames)
                        {
                            try
                            {
                                PdfPKCS7 signature1 = signatureUtil.VerifySignature(sigName);
                                var documentNotModifie = signatureUtil.SignatureCoversWholeDocument(sigName);
                                NotModified = documentNotModifie;
                                var cal = signature1.GetSignDate();
                                var pkc = signature1.GetCertificates();
                                pkc = signature1.GetSignCertificateChain();

                                var revocationValid = signature1.IsRevocationValid();



                                if (pkc[0].IsValidNow)
                                {
                                    if (signature1 != null)
                                    {
                                        genuineAndWasNotModified = signature1.VerifySignatureIntegrityAndAuthenticity();
                                        if (genuineAndWasNotModified)
                                        {
                                            numValid = numValid + 1;
                                        }
                                        else
                                        {
                                            numinvalid = numinvalid + 1;
                                        }

                                        //
                                    }
                                }
                                else if (!documentNotModifie)
                                {
                                    MyMessageBox.Show("The revision of the document that was covered by this signature has not been altered; however, there have been subsequent changes in the document.");
                                    pdfDocument.Close();
                                    return;
                                }
                                else
                                {
                                    MyMessageBox.Show("The Signer's identity is invalid because it has expired or is not yet valid.");
                                    pdfDocument.Close();
                                    return;
                                }
                            }
                            catch (Exception)
                            {
                                // ignoring exceptions,
                                // we are only interested in signatures that are passing the check successfully
                            }
                        }
                        if (numValid == sigNames.Count)
                        {
                            if (!NotModified)
                            {
                                MyMessageBox.Show("Congratulations ! \n\n " + sigNames.Count + " Digital Signature(s) is/are successfully verified. \n However, there have been subsequent changes in the document.");
                                pdfDocument.Close();
                                return;
                            }
                            else
                            {
                                MyMessageBox.Show("Congratulations ! \n\n " + sigNames.Count + " Digital Signature(s) is/are successfully verified.");
                                pdfDocument.Close();
                                return;
                            }
                        }
                        else
                        {
                            if (numinvalid > 0)
                            {
                                MyMessageBox.Show("One or More Digital Signature Tampered.");
                                pdfDocument.Close();
                                return;
                            }
                        }
                    }
                }
                else if (fileExtension == ".xml")
                {
                    byte[] fileContent = File.ReadAllBytes(filename);
                    string xmlString = Encoding.UTF8.GetString(fileContent);
                    //XmlDocument xmlDoc = new XmlDocument();
                    //xmlDoc.LoadXml(xmlString);  // Load the XML from the string
                    
                    string ret= VerifySignXml(xmlString);
                    MyMessageBox.Show(ret);
                }
            }
        }

        #region Xml Signature Verification
        public string VerifySignXml(string data)
        {
            List<DigitalVerifyDetailsForUser> signers = new List<DigitalVerifyDetailsForUser>();
            DigitalVerifyDetailsForUser digitalVerifyDetails = new DigitalVerifyDetailsForUser();
            StringBuilder sb = new StringBuilder();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                string ss = data;
                xmlDoc.LoadXml(ss);
                string digital = "DigitalSignature";
                int signatureCount = CountSignatureElements(xmlDoc);
                
                if (signatureCount > 0)
                {
                    for (int i = 1; i <= signatureCount; i++)
                    {
                        XmlDocument xmlDoc1 = new XmlDocument();
                        string tagdigital = digital + i;
                        XmlElement childNodes = (XmlElement)xmlDoc.SelectSingleNode("//" + tagdigital);
                        if(childNodes!=null)
                        {
                            digitalVerifyDetails = DigitalVerify(childNodes, i);
                            signers.Add(digitalVerifyDetails);
                            sb.Append("\n Digital Signature " + i + "\n");
                            // Append format
                            sb.AppendFormat("Signature: {0}", digitalVerifyDetails.Signature + "\n");
                            sb.AppendFormat("Signature By: {0}", digitalVerifyDetails.SignatureBy + "\n");

                        }
                        else
                        {
                            digitalVerifyDetails = DigitalVerify(xmlDoc.DocumentElement, i);
                            signers.Add(digitalVerifyDetails);
                            sb.Append("\n Digital Signature " + i + "\n");
                            // Append format
                            sb.AppendFormat("Signature: {0}", digitalVerifyDetails.Signature + "\n");
                            sb.AppendFormat("Signature By: {0}", digitalVerifyDetails.SignatureBy + "\n");

                        }

                    }
                }
                else
                {
                    sb.Append("DigitalSignature \n");
                    // Append format
                    sb.AppendFormat("Signature: {0}", digitalVerifyDetails.Signature);
                    //digitalVerifyDetails.Signature = "Xml Not Signature throw DGIS Application";
                  
                    //signers.Add(digitalVerifyDetails);
                }
            }
            catch (Exception ex)
            {

                //digitalVerifyDetails.Signature = "Invalid";
                sb.AppendFormat("Signature: {0}", "Invalid");
                ErrorLog.LogErrorToFile(ex);

            }
            return sb.ToString();
        }
        public static int CountSignatureElements(XmlDocument xmlDoc)
        {
            // Create a namespace manager and add the XMLDSIG namespace
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

            // Select all <Signature> elements in the XML
            XmlNodeList signatureNodes = xmlDoc.SelectNodes("//ds:Signature", nsMgr);

            // Return the count of <Signature> elements
            return signatureNodes.Count;
        }
        public DigitalVerifyDetailsForUser DigitalVerify(XmlElement data, int count)
        {
            DigitalVerifyDetailsForUser ret = new DigitalVerifyDetailsForUser();
            try
            {

                // Load the signed XML document
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                string ss = data.OuterXml.Replace(" />", "/>");
                xmlDoc.LoadXml(ss);

                XmlDocument xmldigest = new XmlDocument();
                xmldigest.PreserveWhitespace = true;
                xmldigest.LoadXml(data.OuterXml);
                // Find the <Signature> element and remove it
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                // Find the <Signature> element (with namespace) and remove it
                // XmlNode signatureNode = xmlDoc1.SelectSingleNode("//ds:Signature", nsMgr);
                XmlNodeList signatureNode = xmldigest.SelectNodes("//ds:Signature", nsMgr);
                // Check if the <Signature> node exists
                if (signatureNode != null)
                {
                    int lastsigncount = 1;
                    foreach (XmlNode node in signatureNode)
                    {
                        if (node is XmlElement element)
                        {
                            if (lastsigncount == count)
                                node.ParentNode.RemoveChild(node);
                        }
                        lastsigncount++;
                    }
                    // Remove the <Signature> node from its parent
                    //signatureNode.ParentNode.RemoveChild(signatureNode);

                }
                // Create an XmlNamespaceManager for managing namespaces in XPath queries
                XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                nsManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

                // Find the Signature element
                XmlNodeList signatureElement1 = xmlDoc.SelectNodes("//ds:Signature", nsManager);
                XmlElement signatureElement = null;
                int countsign = 1;
                foreach (XmlNode node in signatureElement1)
                {
                    if (node is XmlElement element)
                    {
                        if (countsign == count)
                            signatureElement = element;
                    }
                    countsign++;



                }

                //XmlElement signatureElement = xmlDoc.SelectSingleNode("//ds:Signature", nsManager) as XmlElement;
                if (signatureElement == null)
                {
                   
                    ret.Signature = "Signature " + count + " element not found in the document";
                }

                // Create a SignedXml object
                SignedXml signedXml = new SignedXml(xmlDoc);

                // Load the signature element into the SignedXml object
                signedXml.LoadXml(signatureElement);

                // Check overall signature validity (optional)
                bool isSignatureValid = signedXml.CheckSignature();
                if (isSignatureValid)
                {
                   
                    ret.Signature = "Signature " + count + " is Verifed";
                    List<X509Certificate2> certificates = new List<X509Certificate2>();
                    XmlNodeList certificateNodes = xmlDoc.GetElementsByTagName("X509Certificate");
                    foreach (XmlNode node in certificateNodes)
                    {
                        string base64EncodedCertificate = node.InnerText;
                        byte[] certBytes = Convert.FromBase64String(base64EncodedCertificate);
                        X509Certificate2 certificate = new X509Certificate2(certBytes);
                        certificates.Add(certificate);

                        var subdata = certificate.Subject.Split(',');

                        ret.SignatureBy = subdata[1].Replace("SERIALNUMBER=", "") + " (" + subdata[0].Replace("CN=", "") + ") ";


                    }
                }
                else
                {
                   
                    ret.Signature = "Signature " + count + " is Not Verifed: ";
                }
                // Now handle references with missing or blank URI
                foreach (Reference reference in signedXml.SignedInfo.References)
                {
                    // Check if the URI is blank
                    if (string.IsNullOrEmpty(reference.Uri))
                    {
                        // Console.WriteLine("Blank Reference.Uri, assuming the entire document is signed.");

                        // Canonicalize the entire document (or relevant root element)
                        XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
                        transform.LoadInput(xmldigest); // Canonicalize the root element (entire document)

                        // Get canonicalized data as a byte array
                        byte[] canonicalizedData = GetCanonicalizedBytes(xmldigest);//(byte[])transform.GetOutput(typeof(byte[]));
                       
                        // Compute the digest using the specified digest method (e.g., SHA-256)
                        byte[] computedDigest;
                        using (System.Security.Cryptography.HashAlgorithm hashAlg = System.Security.Cryptography.HashAlgorithm.Create(reference.DigestMethod))
                        {
                            computedDigest = hashAlg.ComputeHash(canonicalizedData);
                        }

                        // Compare the computed digest with the digest value from the XML signature
                        bool digestValid = CompareByteArrays(computedDigest, reference.DigestValue);
                        if (digestValid == true)
                        {
                           
                            ret.Signature = "Signature " + count + " is Verifed";
                        }
                        else
                        {
                            
                            ret.Signature = "Signature " + count + " is Not Verifed: ";
                            ret.SignatureBy = "";
                        }
                    }
                    //else
                    //{
                    //    // Handle the case when a valid URI is provided (resolve the reference and verify as usual)
                    //    XmlElement referencedElement = xmlDoc.SelectSingleNode(reference.Uri) as XmlElement;
                    //    if (referencedElement == null)
                    //    {
                    //        return($"Referenced element not found for URI: {reference.Uri}");
                    //    }

                    //    // Continue with the usual process to verify the reference with a valid URI...
                    //}
                }

            }
            catch (Exception ex)
            {
                //ret.IsVerified = false;//"Signature element not found in the document.";
                if (ex.Message == "Invalid length for a Base-64 char array or string.")
                    ret.Signature = "Signature X509Certificate Invalid";
                else
                    ret.Signature = "Signature Invalid";
                ErrorLog.LogErrorToFile(ex);
            }

            return ret;
        }
        public static byte[] GetCanonicalizedBytes(XmlDocument xmlDoc)
        {
            // Create a new XmlDsigC14NTransform for canonicalization
            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();

            // Load the XML data into the transform
            transform.LoadInput(xmlDoc);

            // Get the canonicalized output as a byte array
            using (Stream stream = (Stream)transform.GetOutput(typeof(Stream)))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
        // Helper method to compare two byte arrays
        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
        #endregion

        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {

            //if (IsConnectedToInternet())
            //{
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                //*openFileDialog.Multiselect = true;
                openFileDialog.Filter = "Pdf files (*.pdf;*.PDF)|*.pdf;*.PDF|XML files (*.xml;*.XML)|*.xml;*.XML";
                if (ConfigurationManager.AppSettings["LastSelectedLocation"] == "")
                {
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                else
                {
                    openFileDialog.InitialDirectory = ConfigurationManager.AppSettings["LastSelectedLocation"];
                }
                //openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (openFileDialog.ShowDialog() == true)
                {
                    DropList.IsEnabled = false;
                    BusyBar.IsBusy = true;
                    verifyDigitalSign(openFileDialog.FileNames);
                    DropList.IsEnabled = true;
                    BusyBar.IsBusy = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
                ErrorLog.LogErrorToFile(ex);
            }
            //}
            //else
            //{
            //    MyMessageBox.Show("Digital Signatures cannot be verified in Offline mode.");
            //}

        }




    }
}

using iText.Forms.Fields;
using iText.Forms;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Signatures;
using Microsoft.Office.Interop.Word;
using Org.BouncyCastle.X509;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocToPDFConverter;
using Syncfusion.OfficeChart;
using Syncfusion.OfficeChartToImageConverter;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ValidateCertificate;
using static iText.Signatures.PdfSigner;
using Syncfusion.Pdf.Graphics;


namespace SignService
{
   public class Service1 : IService1
    {
        public static string PrevThumbNail = "";
        public string GetData(string element)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly);

            X509Certificate2 cert1 = X509Certificate2UI.SelectFromCollection(store.Certificates, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
            X509Certificate2 certificate = cert1;

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(element);
            XmlDocument xml1 = SignXML(xml, certificate);
            return string.Format("You entered: 0");
        }

        public async Task<XmlElement> SignXml(XmlElement value)
        {
            try
            {
                X509Certificate2Collection fcollection =await GetCertificates();

                if (fcollection.Count == 0)
                {
                    string message = "No Token Found";
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml("<Root>" + message + "</Root>");
                    return xml.DocumentElement;

                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }
                    X509Certificate2 certificate = cert1;


                    string result = "Success";
                    if (result == "Success")
                    {
                        XmlDocument xml = new XmlDocument();
                        XmlDocument xml1 = null;
                        xml.LoadXml(value.OuterXml);
                        int count = 0;
                        var signatureNode = xml.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
                        // Count the number of Signature elements

                        XmlDocument xmlDoc = new XmlDocument();
                        count = signatureNode.Count + 1;


                        XmlElement root = xmlDoc.CreateElement("DigitalSignature" + count);
                        xmlDoc.AppendChild(root);
                        xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xml.DocumentElement, true));




                        xml1 = SignXML(xmlDoc, certificate);

                      



                        return xml1.DocumentElement;
                    }
                    else
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(result);
                        return xml.DocumentElement;
                    }
                }
              
            }
            catch (Exception ex)
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(ex.Message);
                ErrorLog.LogErrorToFile(ex);
                return xml.DocumentElement;
                
            }
        }
        public static XmlDocument SignXML(XmlDocument doc, X509Certificate2 cert)
        {
            try
            {
                SignedXml signed = new SignedXml(doc);

                var rsaKey = cert.GetRSAPrivateKey();
                signed.SigningKey = cert.PrivateKey;

                Reference reference = new Reference();
                reference.Uri = "";
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signed.AddReference(reference);

                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));

                signed.KeyInfo = keyInfo;
                signed.ComputeSignature();
                XmlElement xmlSig = signed.GetXml();
               

                doc.DocumentElement.AppendChild(doc.ImportNode(xmlSig, true));
                return doc;
            }
            catch (Exception ex)
            {
                // Get the root element
                XmlNode rootElement = doc.SelectSingleNode("/SignXmlRequest/XmlData/RootElement");
                XmlElement Exception = doc.CreateElement("Exception");
                Exception.InnerText = ex.Message.ToString();
                // Add the new element to the root element
                if ("Hi" != null)
                {
                    rootElement.AppendChild(Exception);
                }
                ErrorLog.LogErrorToFile(ex);
                // Convert the modified XML document back to an XML string
                return doc;
                //return modifiedXmlDoc;

               
            }
        }
       
        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        // Fetch personal detail from digital certificate
        // Date : 19 Jul 2022
        // Jasjeet
        public async Task<List<TokenDetails>> FetchPersID()
        {
            
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Certificate2Collection fcollection =await GetCertificates();
                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchPersID",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!"

                    };
                    TokenDetailList.Add(TokenDetails);

                    return TokenDetailList.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }

                    //Extracting Personal No from unique token 
                    string[] SubjectSplit = cert1.Subject.Split(',');
                    string PersNo= SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();


                    bool TokenValidity = false;
                    string Remark = "";
                    if (DateTime.Now <= cert1.NotAfter)
                    {
                        TokenValidity = true;
                        Remark = "Personal No of Unique Cert is fetched for the inserted Token";
                    }
                    else
                    {
                        TokenValidity = false;
                        Remark = "Token Expired";
                    }

                    if (!string.IsNullOrEmpty(PersNo))
                        {
                        var TokenDetails = new TokenDetails
                        {
                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchPersID",
                            CRL_OCSPCheck = false,
                            subject = PersNo,//cert1.Subject,
                            issuer = null, //cert1.Issuer,
                            Thumbprint = null, //cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = Remark,
                            TokenValid = TokenValidity
                        };
                        TokenDetailList.Add(TokenDetails);
                        return TokenDetailList.ToList();
                    }
                    else{
                        throw new Exception("Personal No is Empty. Pl report and try with different Token");
                        }

                }
               
            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString()

                };
                TokenDetailList.Add(TokenDetails);
                ErrorLog.LogErrorToFile(ex);
                return TokenDetailList.ToList();
            }
          
        }

        public async Task<bool> ValidatePersID2FA(string inputPersID)
        {
            try
            {
                X509Certificate2Collection fcollection =await GetCertificates();

                if (fcollection.Count == 0)
                {
                    return false;
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }
                   
                    X509Certificate2 certificate = cert1;
                    try
                    {
                            // Validate the certificate and process result
                            string[] SubjectSplit = cert1.Subject.Split(',');
                            string response = SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();

                            if (inputPersID == response)
                            {
                                if (VerifyCertificatePassword(cert1))
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
                        //}
                    }
                    catch (CryptographicException)
                    {
                        return false;
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private bool VerifyCertificatePassword(X509Certificate2 certificate)
        {
            try
            {
                if (!certificate.HasPrivateKey)
                {
                    return false;
                }

                using (RSA rsa = certificate.GetRSAPrivateKey())
                {
                    byte[] message = Encoding.UTF8.GetBytes("2FA");
                    byte[] signature = rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    bool verified = rsa.VerifyData(message, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    return verified;
                }
            }
            catch (Exception)
            {
                //Console.Write(ex.Message);
                return false;
            }
        }


        // To validate PersID from from digital certificate
        // Date : 29-Sep-2022
        // Jasjeet 
        public async Task<List<PersIdValidation>> ValidatePersID(string inputPersID)
        {

            List<PersIdValidation> PersIdValid = new List<PersIdValidation>();

            try
            {
                X509Certificate2Collection fcollection =await GetCertificates();

                if (fcollection.Count == 0)
                {
                    var validation = new PersIdValidation
                    {
                        vaildId = false,
                        Expired=false,
                        Status="404",
                        Remark="Token Not Found !"
                    };
                    PersIdValid.Add(validation);
                    return PersIdValid.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }
                    X509Certificate2 certificate = cert1;
                    
                    string result = "Success";

                    if (result == "Success")


                    {
                        string[] SubjectSplit = cert1.Subject.Split(',');
                        string response = SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();

                        bool TokenExpity = false;
                        string StatusMsg = "200";
                       
                        if (DateTime.Now > cert1.NotAfter)
                        {
                            TokenExpity = true;
                            StatusMsg = "201";
                        }

                        if (inputPersID == response)
                        {    
                            var validation = new PersIdValidation
                            {
                                vaildId = true,
                                Expired = TokenExpity,
                                Status = StatusMsg,
                                Remark = "Token is Valid !"
                            };
                            PersIdValid.Add(validation);
                            return PersIdValid.ToList();

                        }
                        else
                        {
                            var validation = new PersIdValidation
                            {
                                vaildId = false,
                                Expired = TokenExpity,
                                Status = "200",
                                Remark = "Token is Not Valid !"
                            };
                            PersIdValid.Add(validation);
                            return PersIdValid.ToList();
                        }

                    }
                    else
                    {
                        var validation = new PersIdValidation
                        {
                            vaildId = false,
                            Expired = false
                        };
                        PersIdValid.Add(validation);
                        return PersIdValid.ToList();
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                var validation = new PersIdValidation
                {
                    vaildId = false,
                    Expired = false,
                    Status = "404",
                    Remark = "Token is Not Valid !"
                };
                PersIdValid.Add(validation);
                return PersIdValid.ToList();
            }
        }

        // Fetch unique details of token from all inserted tokens without checking CRL & OCSP 
        // Date : 29-Sep-2022
        // Jasjeet 
        public async Task<List<TokenDetails>> FetchUniqueTokenDetails()
        {
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Certificate2Collection fcollection =await GetCertificates();

                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!",
                        TokenValid= false,
                    };
                    TokenDetailList.Add(TokenDetails);
                    return TokenDetailList.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }

                    bool TokenValidity = false;
                    if (DateTime.Now <= cert1.NotAfter)
                    {
                        TokenValidity = true;
                    }
                    else
                    {
                        TokenValidity = false;
                    }

                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                        CRL_OCSPCheck = false,
                        subject = cert1.Subject,
                        issuer = cert1.Issuer,
                        Thumbprint = cert1.Thumbprint,
                        ValidFrom = cert1.NotBefore.ToString(),
                        ValidTo = cert1.NotAfter.ToString(),
                        Status = "200",
                        Remarks = "Unique Cert details of inserted Token",
                        TokenValid= TokenValidity
                    };
                    TokenDetailList.Add(TokenDetails);
                    return TokenDetailList.ToList();
                }
            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString(),
                    TokenValid=false
                };
                ErrorLog.LogErrorToFile(ex);
                TokenDetailList.Add(TokenDetails);
                return TokenDetailList.ToList();
            }

        }

        // To Fetch details of all the inserted tokens without calling CRL & OCSP 
        // Date : 12-Jul-2023
        //* Jasjeet Singh
        public async Task<List<TokenDetails>> FetchTokenDetails()
        {
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Certificate2Collection fcollection =await GetCertificates();

                if (fcollection.Count > 0)
                {
                    int i = 1;
                    foreach (X509Certificate2 cert1 in fcollection)
                    {
                        bool TokenValidity = false;
                        if (DateTime.Now <= cert1.NotAfter)
                        {
                            TokenValidity = true;
                        }
                        else
                        {
                            TokenValidity = false;
                        }

                        var detail = new TokenDetails
                        {
                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenDetails",
                            CRL_OCSPCheck = false,
                            subject = cert1.Subject,
                            issuer = cert1.Issuer,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = "Details of Cert No-" + i + "- are as given above",
                            TokenValid = TokenValidity,

                        };
                        i++;
                        TokenDetailList.Add(detail);
                    }
                    return TokenDetailList.ToList();
                }
                else
                {
                    var detail = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenDetails",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!",
                        TokenValid=false
                    };
                    TokenDetailList.Add(detail);
                    return TokenDetailList.ToList();
                }

            }
            catch (Exception ex)
            {
                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString(),
                    TokenValid=false
                };
               
                TokenDetailList.Add(TokenDetails);
                ErrorLog.LogErrorToFile(ex);
                return TokenDetailList.ToList();
            }

        }

        public async Task<List<TokenDetails>> FetchTokenOCSPCrlDetailsAsync(bool IsCheckCrl,string ThumbPrint)
        {
            string MsgCrlOCSP = "";
            bool BlnCrlOCSP = true;
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                X509Certificate2Collection fcollection = new X509Certificate2Collection();
                

                if (ThumbPrint == "")
                {
                    fcollection =await GetCertificates();
                }
                else
                {
                    X509Certificate2Collection fcol = new X509Certificate2Collection();
                    fcol =await GetCertificates();
                    
                    X509Certificate2 selectedCertificate = fcol.Cast<X509Certificate2>().FirstOrDefault(cert => cert.Thumbprint.Equals(ThumbPrint, StringComparison.OrdinalIgnoreCase));
                    if (selectedCertificate != null)
                    {
                        fcollection.Add(selectedCertificate);
                    }
                    
                }
                //store.Close();


                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                        CRL_OCSPCheck = BlnCrlOCSP,
                        CRL_OCSPMsg= MsgCrlOCSP,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!",
                        TokenValid = false
                    };
                    TokenDetailList.Add(TokenDetails);
                    return TokenDetailList.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        try
                        {
                            X509Certificate2Collection selectedCertificates = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection);

                            if (selectedCertificates.Count > 0)
                            {
                                 cert1 = selectedCertificates[0];
                            }
                            else
                            {
                                var TokenDetails = new TokenDetails
                                {

                                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                                    CRL_OCSPCheck = BlnCrlOCSP,
                                    CRL_OCSPMsg = MsgCrlOCSP,
                                    subject = null,
                                    issuer = null,
                                    Thumbprint = null,
                                    ValidFrom = null,
                                    ValidTo = null,
                                    Status = "200",
                                    Remarks = "No Certificate Selected !",
                                    TokenValid = false,
                                };
                                TokenDetailList.Add(TokenDetails);
                                return TokenDetailList.ToList();
                            }
                        }
                        catch
                        {
                            var TokenDetails = new TokenDetails
                            {

                                API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                                CRL_OCSPCheck = BlnCrlOCSP,
                                CRL_OCSPMsg = MsgCrlOCSP,
                                subject = null,
                                issuer = null,
                                Thumbprint = null,
                                ValidFrom = null,
                                ValidTo = null,
                                Status = "200",
                                Remarks = "No Certificate Selected !",
                                TokenValid = false,
                            };
                            TokenDetailList.Add(TokenDetails);
                        }
                    }


                    if (IsCheckCrl == true)
                    {
                        if (PrevThumbNail != "")
                        {
                            if (cert1.Thumbprint == PrevThumbNail)
                            {
                                IsCheckCrl = false;
                            }
                            else
                            {
                                PrevThumbNail = cert1.Thumbprint;
                            }
                        }
                        else
                        {
                            PrevThumbNail = cert1.Thumbprint;
                        }
                    }


                    var (ValidateCertificateAsyncOutput,validationMsg,CrlMsg,OCSPMsg,CrlValid,OCSPValid)  = await ValidateCertificate.ValidateCert.ValidateCertificateAsync(cert1,IsCheckCrl);


                    if (CrlValid == true || OCSPValid == true)
                    {
                        if (OCSPMsg == "Good" || CrlValid == true)
                        {
                            MsgCrlOCSP = "";
                        }
                        else if (OCSPMsg == "NotFound" || CrlValid == false)
                        {
                            MsgCrlOCSP = "Digital Cert of token cannot be verified with CA due to Network issues";
                        }
                        else
                        {
                            MsgCrlOCSP = "Crl and OCSP Not Checked";
                        }

                        BlnCrlOCSP = true;
                    }
                    else
                    {
                        MsgCrlOCSP = "Crl or OCSP is Revoked";
                        BlnCrlOCSP = false;
                    }
                    
                    if (ValidateCertificateAsyncOutput == true)
                    {
                        var TokenDetails = new TokenDetails
                        {
                           
                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                            CRL_OCSPCheck = BlnCrlOCSP,
                            CRL_OCSPMsg = MsgCrlOCSP,
                            subject = cert1.Subject,
                            issuer = cert1.Issuer,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = "Unique Cert details of inserted Token",
                            TokenValid=true,
                        };
                        TokenDetailList.Add(TokenDetails);
                    }
                    else
                    {
                        var TokenDetails = new TokenDetails
                        {

                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                            CRL_OCSPCheck = BlnCrlOCSP,
                            CRL_OCSPMsg = MsgCrlOCSP,
                            subject = cert1.Subject,
                            issuer = cert1.Issuer,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = validationMsg,
                            TokenValid = false,
                        };
                        TokenDetailList.Add(TokenDetails);
                    }
                    return TokenDetailList.ToList();
                }
            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                    CRL_OCSPCheck = BlnCrlOCSP,
                    CRL_OCSPMsg = MsgCrlOCSP,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString(),
                    TokenValid = false

                };
                TokenDetailList.Add(TokenDetails);
                ErrorLog.LogErrorToFile(ex);
                return TokenDetailList.ToList();
            }
        }
        // Export all the avlb certs to a folder in local machine @given Path
        // Date : 15-Jul-2023
        //*Jasjeet Singh
        private static void ExportAllCert()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadOnly);

            // Get all certificates from the store
            X509Certificate2Collection certificates = store.Certificates;

            string exportPath = @"D:\Certificates";
            // Export each certificate to a file
            foreach (X509Certificate2 certificate in certificates)
            {
                // Export the certificate to a byte array
                byte[] certBytes = certificate.Export(X509ContentType.Cert);

                // Create a file path for the exported certificate
                string filePath = Path.Combine(exportPath, $"{certificate.Thumbprint}.cer");

                // Write the certificate bytes to the file
                File.WriteAllBytes(filePath, certBytes);
            }
            store.Close();
        }

        public X509Certificate2 DownloadCert(string url)
        {

            try
            {
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                WebResponse myResp = myReq.GetResponse();

                byte[] b = null;
                using (Stream stream = myResp.GetResponseStream())
                using (MemoryStream ms = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        byte[] buf = new byte[1024];
                        count = stream.Read(buf, 0, 1024);
                        ms.Write(buf, 0, count);
                    } while (stream.CanRead && count > 0);
                    b = ms.ToArray();
                }

                X509Certificate2 cert = new X509Certificate2(b);
                return cert;
            }
            catch (WebException)
            {
                return null;
            }
        }

        public void updateCert(X509Certificate2 cert, string subjectName)
        {
            if (cert != null)
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

                store.Open(OpenFlags.ReadWrite);
                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindBySubjectName, subjectName, false);
                X509Certificate2 x509Certificate2 = new X509Certificate2(fcollection[0]);

                if (x509Certificate2.Thumbprint != cert.Thumbprint)
                {
                    store.Add(cert);
                }
            }
        }
        public async Task<ResponseMessage> DigitalSignAsync(List<DigitalSignData> reqData)
        {
            ResponseMessage responseMessage = new ResponseMessage();
            ResponseBulkSign apiResponse = await DigitalSignBulkAsync(reqData);

            if (apiResponse != null)
            {
                // string ResponseContent = await response.Content.ReadAsStringAsync();
                //ResponseBulkSign apiResponse = JsonConvert.DeserializeObject<ResponseBulkSign>(ResponseContent);
                string resultstring = "";
                int count = 0;
                int Signed = 0;
                if (apiResponse.ResponseMessage != null)
                {
                    resultstring = "Congratulations!\n\nDocument is successfully Signed.\n";
                    resultstring += apiResponse.ResponseMessage.Message + "\n";
                    Signed = 1;
                }
                foreach (ResponseMessage data in apiResponse.ResponseMessagelst)
                {

                    if (count == 0)
                    {
                        resultstring += "\n Opps!\nDocument is Not successfully Signed.\n";
                        resultstring += "This Docu Not Sign Either Password Protected or Page Not Found.\n";

                        count++;
                    }

                    resultstring += data.Message + "\n ";





                }
                if (resultstring != "")
                {
                    if (Signed > 0)
                    {
                        responseMessage.Message = resultstring;
                        responseMessage.Valid = true;

                    }
                    else
                    {
                        responseMessage.Message = resultstring;
                        responseMessage.Valid = false;
                    }

                }
                else
                {
                    if (apiResponse.ResponseMessage != null)
                    {
                        responseMessage.Message = $"Error:" + apiResponse.ResponseMessage.Message;
                        responseMessage.Valid = false;

                    }
                }
               
            }
            else
            {
                responseMessage.Message = $"Error:" + apiResponse.ResponseMessage.Message;
                responseMessage.Valid = false;
            }
            return responseMessage;
        }

        public async Task<ResponseBulkSign> DigitalSignBulkAsync(List<DigitalSignData> reqData)
        {
            string message = null;
            ResponseBulkSign ResponseMsgbullst = new ResponseBulkSign();

            ResponseMessage ResponseMsg = new ResponseMessage();
            List<ResponseMessage> ResponseMsglist = new List<ResponseMessage>();
            String NewFileName = "";
            int Pageno = 0;
            List<DigitalSignData> delData = new List<DigitalSignData>();
            try
            {
                string ThumbPrint = reqData.First().Thumbprint;

                X509Certificate2Collection certCollection = new X509Certificate2Collection();

                X509Certificate2Collection fcol = new X509Certificate2Collection();
                fcol =await GetCertificates();

                if (fcol.Count == 0)
                {
                    ResponseMsg.Message = "No Certificate Found !";
                    ResponseMsg.Valid = false;
                    ResponseMsgbullst.ResponseMessage = ResponseMsg;
                    return ResponseMsgbullst;
                }

                X509Certificate2 selectedCertificate = fcol.Cast<X509Certificate2>().FirstOrDefault(cert => cert.Thumbprint.Equals(ThumbPrint, StringComparison.OrdinalIgnoreCase));
                certCollection.Add(selectedCertificate);

                

                if (certCollection.Count == 0)
                {
                    ResponseMsg.Message = "Thumbprint not matched !";
                    ResponseMsg.Valid = false;
                    ResponseMsgbullst.ResponseMessage=ResponseMsg;
                    return ResponseMsgbullst;
                }

                X509Certificate2 cert1 = certCollection[0];

                if (DateTime.Now > cert1.NotAfter)
                {
                    ResponseMsg.Message = "Token Expired !";
                    ResponseMsg.Valid = false;
                    ResponseMsgbullst.ResponseMessage = ResponseMsg;
                    return ResponseMsgbullst;
                }


                string[] files = Directory.GetFiles(reqData.First().InputFileLoc);

                int totalFiles = files.Count();
                int SingedFiles = 0;
                PdfSigner signer = null;
                FileStream fileStream = null;
                string Download = reqData.First().OutputFileLoc;

                int Xaxis = reqData.First().XCoordinate;
                int Yaxis = reqData.First().YCoordinate;
                if (reqData.First().Page != 0)
                {
                    Pageno = reqData.First().Page;
                }
                else
                {
                    Pageno = 1;
                }
                foreach (string filename in files)
                {
                nextfile:
                    string fileforloop = filename;
                    ResponseMessage ResponseMsg1 = new ResponseMessage();
                    
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

                        string FileFullName = Download + '\\' + Path.GetFileName(fileforloop) + "_DS_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + ".pdf";

                        PdfReader reader = new PdfReader(fileforloop);
                        IExternalSignature es = new X509Certificate2Signature(cert1, "SHA-1", ref message);

                        if (message != null)
                        {
                            ResponseMsg.Message = message;
                            ResponseMsg.Valid = false;
                        }
                        else
                        {
                            if (es.GetEncryptionAlgorithm() != null)
                            {
                                Org.BouncyCastle.X509.X509CertificateParser cp1 = new Org.BouncyCastle.X509.X509CertificateParser();

                                Org.BouncyCastle.X509.X509Certificate[] chain3 = new[] { cp1.ReadCertificate(cert1.RawData) };

                                await System.Threading.Tasks.Task.Run(() =>
                                {
                                    StampingProperties stampProp = new StampingProperties();
                                    stampProp.PreserveEncryption();
                                    ImageData imageData = null;

                                    using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSignWT.png"))
                                    {
                                        imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSignWT.png");
                                    }

                                    string[] SubjectSplit = cert1.Subject.Split(',');
                                    string StrName = SubjectSplit[0].ToString().Replace("CN=", "").Trim();
                                    string StrICNo = SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();
                                    string StrRank = SubjectSplit[2].ToString().Replace("T=", "").Trim();

                                    iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new PdfReader(fileforloop));
                                    SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                                    IList<string> sigNames = signatureUtil.GetSignatureNames();
                                    iText.Kernel.Font.PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_BOLD));
                                    String StrSignature = "";
                                    StrSignature = "Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " \n © DGIS App, IA";

                                    try
                                    {
                                        fileStream = new FileStream(FileFullName, FileMode.Create);
                                        if (sigNames.Count == 0)
                                        {
                                            signer = new PdfSigner(reader, fileStream, new StampingProperties());
                                        }
                                        else
                                        {
                                           var getXYaxis = GetSignatureCordinate(fileforloop);
                                            if(getXYaxis!=null)
                                            {
                                                if(sigNames.Count%2==0)
                                                {
                                                    Yaxis = getXYaxis[sigNames.Count-1].YCoordinate + 50;
                                                    Xaxis = getXYaxis[0].XCoordinate;
                                                }
                                                else
                                                {
                                                    Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate;
                                                    Xaxis = getXYaxis[sigNames.Count - 1].XCoordinate+200;
                                                    if (Xaxis > 300)
                                                    {
                                                        Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate + 50;
                                                        Xaxis = getXYaxis[0].XCoordinate;
                                                    }
                                                }
                                            }
                                            signer = new PdfSigner(reader, fileStream, stampProp.UseAppendMode());
                                        }
                                        PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                            .SetLayer2Text(StrSignature)
                                            .SetImage(imageData).SetImageScale(-50)
                                            .SetReuseAppearance(false);
                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(Xaxis, Yaxis, 180, 50);
                                        if (Xaxis == 0 && Yaxis == 0)
                                        { 
                                            if (signer.GetNewSigFieldName() == "Signature1")
                                            {
                                               rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature2")
                                            {
                                               rect = new iText.Kernel.Geom.Rectangle(40, 65, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature3")
                                            {
                                               rect = new iText.Kernel.Geom.Rectangle(220, 95, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature4")
                                            {
                                               rect = new iText.Kernel.Geom.Rectangle(400, 65, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature5")
                                            {
                                               rect = new iText.Kernel.Geom.Rectangle(40, 115, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature6")
                                            {
                                               rect = new iText.Kernel.Geom.Rectangle(220, 115, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature7")
                                            {
                                               rect = new iText.Kernel.Geom.Rectangle(400, 115, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature8")
                                            {
                                                rect = new iText.Kernel.Geom.Rectangle(40, 165, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature9")
                                            {
                                                rect = new iText.Kernel.Geom.Rectangle(220, 165, 180, 50);
                                            }
                                            else if (signer.GetNewSigFieldName() == "Signature10")
                                            {
                                                rect = new iText.Kernel.Geom.Rectangle(400, 165, 180, 50);
                                            }
                                        }
                                        appearance
                                            .SetPageRect(rect)
                                            .SetPageNumber(Pageno);
                                        signer.SetFieldName(signer.GetNewSigFieldName());
                                        try
                                        {
                                            signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                            SingedFiles = SingedFiles + 1;
                                            ResponseMsg1.Message = Convert.ToString(SingedFiles) + " files Signed out of " + Convert.ToString(totalFiles) + " !";
                                            ResponseMsg1.Valid = true;
                                            ResponseMsgbullst.ResponseMessage=ResponseMsg1;
                                        }
                                        catch
                                        {
                                            reader.Close();
                                            if (fileStream != null)
                                            {
                                                fileStream.Close();
                                            }
                                            DigitalSignData filedata = new DigitalSignData();
                                            filedata.pdfpath = FileFullName;
                                            delData.Add(filedata);
                                            ResponseMsg1.Message =  Path.GetFileName(fileforloop)+" !";
                                            ResponseMsg1.Valid = true;
                                            ResponseMsglist.Add(ResponseMsg1);
                                        }
                                       
                                        reader.Close();
                                        
                                    }
                                    catch
                                    {
                                       reader.Close();
                                        if (fileStream != null)
                                        {
                                            fileStream.Close();
                                        }
                                        DigitalSignData filedata = new DigitalSignData();
                                       filedata.pdfpath = FileFullName;
                                       delData.Add(filedata);
                                       //ResponseMsg.Message = "No Docu Sign !";
                                       //ResponseMsg.Valid = false;
                                    }
                                });
                            }


                        }
                    }
                    else if (Path.GetExtension(filename) == ".docx" || Path.GetExtension(filename) == ".doc")
                    {
                        String DocfileName = Path.GetFileNameWithoutExtension(filename);
                        NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";
                        ConvertPDF(filename, NewFileName, WdSaveFormat.wdFormatPDF);
                        goto nextfile;
                    }
                }

                try
                {
                    foreach (var file in delData)
                    { 
                        if (file.pdfpath != "")
                        {
                            FileInfo fi = new FileInfo(file.pdfpath);
                            if (fi.Length == 0)
                            {
                                File.Delete(file.pdfpath);
                            }
                        }
                    }
                }
                catch(Exception ex)  
                {
                    ErrorLog.LogErrorToFile(ex);
                }
                ResponseMsgbullst.ResponseMessagelst=ResponseMsglist;
                return ResponseMsgbullst;
            }
            catch (Exception ex)
            {
                ResponseMsg.Message = "Error Occured in Signing Document " + ex.Message;
                ResponseMsg.Valid = false;
                ErrorLog.LogErrorToFile(ex);
                ResponseMsgbullst.ResponseMessage=ResponseMsg;
                return ResponseMsgbullst;
            }
        }
        public List<DigitalSignData> GetSignatureCordinate(string pdfPath)
        {
            List<DigitalSignData> lst = new List<DigitalSignData>();
            using (PdfReader reader = new PdfReader(pdfPath))
            {
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdfDoc, false);
                    if (acroForm == null)
                    {
                        Console.WriteLine("No signature fields found.");
                        return null;
                    }

                    IDictionary<string, PdfFormField> fields = acroForm.GetFormFields();

                    foreach (var field in fields)
                    {
                        DigitalSignData digitalSignData = new DigitalSignData();
                        if (field.Value is PdfSignatureFormField signatureField)
                        {
                            string signatureName = field.Key;
                            var rect = signatureField.GetWidgets()[0].GetRectangle().ToRectangle();
                            digitalSignData.XCoordinate = (int)rect.GetX();
                            digitalSignData.YCoordinate = (int)rect.GetY();

                            lst.Add(digitalSignData);
                                }
                    }
                    return lst;
                }
            }
            return null;
        }
        public async Task<ResponseMessage> ByteDigitalSignAsync(List<DigitalSignData> reqData)
        {
            string message = null;
            ResponseMessage ResponseMsg = new ResponseMessage();
            try
            {
                string ThumbPrint = reqData.First().Thumbprint;

                X509Certificate2Collection certCollection = new X509Certificate2Collection();

                X509Certificate2Collection fcol = new X509Certificate2Collection();
                fcol =await GetCertificates();

                X509Certificate2 selectedCertificate = fcol.Cast<X509Certificate2>().FirstOrDefault(cert => cert.Thumbprint.Equals(ThumbPrint, StringComparison.OrdinalIgnoreCase));

                if (selectedCertificate == null)
                {
                    ResponseMsg.Message = "No Certificate found !";
                    ResponseMsg.Valid = false;
                    return ResponseMsg;
                }
                certCollection.Add(selectedCertificate);


                if (certCollection.Count == 0)
                {
                    ResponseMsg.Message = "Thumbprint not matched !";
                    ResponseMsg.Valid = false;
                    return ResponseMsg;
                }

                X509Certificate2 cert1 = certCollection[0];

                if (DateTime.Now > cert1.NotAfter)
                {
                    ResponseMsg.Message = "Token Expired !";
                    ResponseMsg.Valid = false;
                    return ResponseMsg;
                }



                int Xaxis = reqData.First().XCoordinate;
                int Yaxis = reqData.First().YCoordinate;
                string pathss = reqData.First().pdfpath;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                WebClient client = new WebClient();
                byte[] pdfBytes = client.DownloadData(pathss);
                //byte[] pdfBytes = Convert.FromBase64String(reqData.First().Byte_pdf);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (var inputPdfStream = new MemoryStream(pdfBytes))
                    {

                        PdfReader reader = new PdfReader(inputPdfStream);
                        IExternalSignature es = new X509Certificate2Signature(cert1, "SHA-1", ref message);

                        if (message != null)
                        {
                            ResponseMsg.Message = message;
                            ResponseMsg.Valid = false;
                            return ResponseMsg;
                        }
                        else
                        {
                            if (es.GetEncryptionAlgorithm() != null)
                            {
                                Org.BouncyCastle.X509.X509CertificateParser cp1 = new Org.BouncyCastle.X509.X509CertificateParser();

                                Org.BouncyCastle.X509.X509Certificate[] chain3 = new[] { cp1.ReadCertificate(cert1.RawData) };

                                await System.Threading.Tasks.Task.Run(() =>
                                {
                                    StampingProperties stampProp = new StampingProperties();
                                    stampProp.PreserveEncryption();
                                    ImageData imageData = null;

                                    using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSignWT.png"))
                                    {
                                        imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSignWT.png");
                                    }

                                    string[] SubjectSplit = cert1.Subject.Split(',');
                                    string StrName = SubjectSplit[0].ToString().Replace("CN=", "").Trim();
                                    string StrICNo = SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();
                                    string StrRank = SubjectSplit[2].ToString().Replace("T=", "").Trim();

                                    inputPdfStream.Position = 0;

                                    iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new PdfReader(inputPdfStream));
                                    SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                                    IList<string> sigNames = signatureUtil.GetSignatureNames();
                                    iText.Kernel.Font.PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_BOLD));
                                    String StrSignature = "";
                                    StrSignature = "Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " \n © DGIS App, IA";

                                    try
                                    {
                                        PdfSigner signer = new PdfSigner(reader, ms, new StampingProperties());
                                        PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                            .SetLayer2Text(StrSignature)
                                            .SetImage(imageData).SetImageScale(-50)
                                            .SetReuseAppearance(false);
                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(Xaxis, Yaxis, 180, 50);
                                        appearance
                                            .SetPageRect(rect)
                                            .SetPageNumber(1);
                                        signer.SetFieldName(signer.GetNewSigFieldName());

                                        signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                    }
                                    catch
                                    {
                                        ResponseMsg.Message = "No Docu Sign !";
                                        ResponseMsg.Valid = false;
                                    }
                                });
                            }


                        }

                    }
                    byte[] byteArray = ms.ToArray();
                    string base64String = Convert.ToBase64String(byteArray);
                    ResponseMsg.Message = base64String;
                    ResponseMsg.Valid = true;
                    return ResponseMsg;
                }
            }
            catch (Exception ex)
            {
                ResponseMsg.Message = "Error Occured in Signing Document " + ex.Message;
                ResponseMsg.Valid = false;
                ErrorLog.LogErrorToFile(ex);
                return ResponseMsg;
            }
        }

        public static void ConvertPDF(string inputpath, string outputPath, WdSaveFormat format)
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

        public async Task<bool> HasInternetConnectionAsyncTest()
        {
            try 
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(2); // Adjust the timeout as needed
                    //var request = new HttpRequestMessage(HttpMethod.Head, "https://google.com");
                    var request = new HttpRequestMessage(HttpMethod.Head, ConfigurationManager.AppSettings["HasInternetConnection"]);
                    var response = await httpClient.SendAsync(request);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                return false; // Return false if there's an issue with the HTTP request
            }
        }

        public async Task<X509Certificate2Collection> GetCertificates()
        {
            X509Certificate2 cert1 = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            X509Certificate2Collection fcollection = new X509Certificate2Collection();

            try
            {
                store.Open(OpenFlags.OpenExistingOnly);
                await System.Threading.Tasks.Task.Run(() =>
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
             
            }
            catch (Exception ex)
            {
                fcollection = null;
                ErrorLog.LogErrorToFile(ex);
            }

            return fcollection;
        }
        public static X509Certificate2Collection GetCertificates1()
        {
            X509Certificate2Collection fcollection = new X509Certificate2Collection();

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly);

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
                        fcollection=null;
                        // Handle any exception when accessing the private key
                        // You can log the error or skip this certificate
                    }
                }
            }

            return fcollection;
        }

        public string SignHash(string message)
        {



            string status = null;
            if (message == null)
            {
                return "No value recived for Digital Signature";
            }
            try
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                X509Certificate2Collection fcollection = new X509Certificate2Collection();
                store.Open(OpenFlags.OpenExistingOnly);

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

                if (fcollection.Count == 0)
                {
                    return "No Token Found !";
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }
                    X509Certificate2 certificate = cert1;
                    Console.WriteLine("Public Key: {0}{1}", cert1.PublicKey.Key.ToXmlString(false), Environment.NewLine);
                   
                        RSACryptoServiceProvider csp = (RSACryptoServiceProvider)certificate.PrivateKey;

                        byte[] data = new ASCIIEncoding().GetBytes(message);
                        byte[] hash = new SHA1Managed().ComputeHash(data);

                    string response = Convert.ToBase64String(csp.SignHash(hash, CryptoConfig.MapNameToOID("SHA-256")));

                    return response;
                    //}
                    //else
                    //{
                    //    return result;
                    //}
                }
            }

            catch (ArgumentOutOfRangeException ex)
            {
                ErrorLog.LogErrorToFile(ex);
                return ex.Message;  //"Certificate not Found. Please connect the token and try agian";
            }
        }
        #region Xml Signature Verification
        public List<DigitalVerifyDetails> VerifySignXml(XmlElement data)
        {
            List<DigitalVerifyDetails> signers = new List<DigitalVerifyDetails>();
            DigitalVerifyDetails digitalVerifyDetails = new DigitalVerifyDetails();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                string ss = data.OuterXml.Replace(" />", "/>");
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
                        if (childNodes != null)
                        {
                            digitalVerifyDetails = DigitalVerify(childNodes, i);
                            signers.Add(digitalVerifyDetails);
                        }
                        else
                        {
                            digitalVerifyDetails = DigitalVerify(xmlDoc.DocumentElement, i);
                            signers.Add(digitalVerifyDetails);
                        }
                    }
                }
                else
                {
                    digitalVerifyDetails.IsVerified = false;
                    digitalVerifyDetails.SignatureRemarks = "Xml Not Signature";
                    digitalVerifyDetails.IsDigest = false;//"Signature element not found in the document.";
                    digitalVerifyDetails.DigestRemarks = "Reference digest is Invalid";
                    signers.Add(digitalVerifyDetails);
                }
            }
            catch (Exception ex)
            {
                digitalVerifyDetails.IsVerified = false;
                digitalVerifyDetails.SignatureRemarks = "Invalid";
                digitalVerifyDetails.IsDigest = false;//"Signature element not found in the document.";
                digitalVerifyDetails.DigestRemarks = "digest is Invalid";

                ErrorLog.LogErrorToFile(ex);
            }
            return signers;
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
        public DigitalVerifyDetails DigitalVerify(XmlElement data, int count)
        {
            DigitalVerifyDetails ret = new DigitalVerifyDetails();
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
                    ret.IsVerified = false;//"Signature element not found in the document.";
                    ret.SignatureRemarks = "Signature " + count + " element not found in the document";
                }

                // Create a SignedXml object
                SignedXml signedXml = new SignedXml(xmlDoc);

                // Load the signature element into the SignedXml object
                signedXml.LoadXml(signatureElement);

                // Check overall signature validity (optional)
                bool isSignatureValid = signedXml.CheckSignature();
                if (isSignatureValid)
                {
                    ret.IsVerified = isSignatureValid;//"Signature element not found in the document.";
                    ret.SignatureRemarks = "Signature " + count + " is Verifed";
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
                    ret.IsVerified = isSignatureValid;//"Signature element not found in the document.";
                    ret.SignatureRemarks = "Signature " + count + " is Not Verifed: ";
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
                        transform.LoadInput(xmlDoc); // Canonicalize the root element (entire document)

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
                            ret.IsDigest = true;
                            ret.DigestRemarks = "Reference " + count + " digest is valid";
                        }
                        else
                        {
                            ret.IsDigest = false;
                            ret.DigestRemarks = "Reference " + count + " digest is Invalid because the computed digest differs from the digest in the XML";
                        }
                    }
                   
                }

            }
            catch (Exception ex)
            {
                ret.IsVerified = false;//"Signature element not found in the document.";
                if (ex.Message == "Invalid length for a Base-64 char array or string.")
                    ret.SignatureRemarks = "Signature X509Certificate Invalid";
                else
                    ret.SignatureRemarks = "Signature Invalid";
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

        #region Public Key

        public async Task<List<TokenDetails>> GetPublicKey()
        {
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Certificate2Collection fcollection =await GetCertificates();

                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchPersID",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!"

                    };
                    TokenDetailList.Add(TokenDetails);

                    return TokenDetailList.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }

                    //Extracting Personal No from unique token 
                    string[] SubjectSplit = cert1.Subject.Split(',');
                    string PersNo = SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();


                    bool TokenValidity = false;
                    string Remark = "";
                    if (DateTime.Now <= cert1.NotAfter)
                    {
                        TokenValidity = true;
                        Remark = "Personal No of Unique Cert is fetched for the inserted Token";
                    }
                    else
                    {
                        TokenValidity = false;
                        Remark = "Token Expired";
                    }


                    if (!string.IsNullOrEmpty(PersNo))
                    {

                        var TokenDetails = new TokenDetails
                        {

                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchPersID",
                            CRL_OCSPCheck = false,
                            subject = PersNo,//cert1.Subject,
                            issuer = null, //cert1.Issuer,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = Remark,
                            TokenValid = TokenValidity,
                            Public_Key = Convert.ToBase64String(cert1.GetPublicKey()),

                            //Private_Key= cert1.GetRSAPrivateKey()
                        };
                        TokenDetailList.Add(TokenDetails);
                        return TokenDetailList.ToList();
                    }
                    else
                    {
                        throw new Exception("Personal No is Empty. Pl report and try with different Token");
                    }

                }
            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString()

                };
                TokenDetailList.Add(TokenDetails);
                ErrorLog.LogErrorToFile(ex);
                return TokenDetailList.ToList();
            }
        }
        #endregion
    }
}
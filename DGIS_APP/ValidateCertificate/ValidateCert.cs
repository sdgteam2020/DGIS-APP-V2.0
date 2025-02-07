﻿using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
//using System.Collections.Generic;
//using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

//using System.Security.Cryptography.Xml;

using System.Text;
using System.Threading.Tasks;


namespace ValidateCertificate
{
    public static class ValidateCert
    {
        //public static String CheckCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 cert1, string filename)
        //{
        //    try
        //    {


        //        string[] arr = CheckCertificateStatus.GetCrlDistributionPoints(cert1);
        //        string crlUrl = null;
        //        if (arr.Length > 0)
        //        {
        //            crlUrl = arr[0];
        //        }
        //        X509CrlParser crlParser = new X509CrlParser();
        //        X509Crl crl = CheckCertificateStatus.GetCrl(crlUrl);

        //        OcspClient obj = new OcspClient();

        //        X509Certificate2 certificate2 = CheckCertificateStatus.GetIssuer(cert1);

        //        Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();

        //        Org.BouncyCastle.X509.X509Certificate[] chain = new[] {
        //            cp.ReadCertificate(cert1.RawData)
        //            };
        //        Org.BouncyCastle.X509.X509Certificate[] chain1 = null;
        //        if (certificate2 != null)
        //        {
        //            chain1 = new[] {

        //                cp.ReadCertificate(certificate2.RawData)
        //            };
        //        }
        //        else
        //        {
                    
        //            return "The Cert root has missing issuer. Please Contact issuer";

        //        }

        //        if (chain[0].IsValidNow)
        //        {
        //            if (chain1 != null)
        //            {
        //                Console.WriteLine(obj.Query(chain[0], chain1[0]));
        //            }
        //        }
        //        else
        //        {
        //            return "Certificate Expired..";
        //        }

        //        if (chain[0].IsValidNow)
        //        {

        //            if (obj.Query(chain[0], chain1[0]) == CertificateStatus.Good || CheckCertificateStatus.IsCRLOK(crl, chain1[0], DateTime.Now) == true)
        //            {
        //                return "Success";



        //            }
        //            else if (obj.Query(chain[0], chain1[0]) == CertificateStatus.Revoked)
        //            {
        //                return "The Cert has been revoked. Pl contact issuer";

        //            }
        //            else if (obj.Query(chain[0], chain1[0]) == CertificateStatus.NotFound || CheckCertificateStatus.IsCRLOK(crl, chain1[0], DateTime.Now) == false)
        //            {

        //                return "The auth of the Digi Cert cannot be asccertained due to Network issue";
        //            }



        //            else
        //            {
        //                Console.WriteLine("CRL does not exists.. ");
        //                return "The Cert is invaild. Pl contact issuer";
        //            }
        //        }
        //        else
        //        {

        //            return "Certificate Expired. Pl renew it";

        //        }

        //    }
        //    catch (ArgumentOutOfRangeException)
        //    {
        //        return "";
        //    }
        //    catch (CryptographicException ex)
        //    {
        //        return ex.Message;
        //    }

        //    catch (Exception ex)
        //    {
        //        return ex.Message;

        //    }
        //}


        public static async Task<(bool, string,string,string,bool,bool)> ValidateCertificateAsync(X509Certificate2 cert,bool IsCheckCrl)
        {
            string validationErrorMessage = string.Empty;
            string ChainMsg = string.Empty;
            string CrlMsg = string.Empty;
            string OCSPMsg = string.Empty;

            bool CrlValid = true;
            bool OCSPValid = true;
            bool ChainTrust = true;
            try
            {
               
                X509Certificate2 certificate = cert;
                X509Certificate2 certificate2 = await Task.Run(() => CheckCertificateStatus.GetIssuer(cert));
                OcspClient obj = new OcspClient();
                Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();

                Org.BouncyCastle.X509.X509Certificate[] chain = new[]
                {
                    cp.ReadCertificate(cert.RawData)
                };

                Org.BouncyCastle.X509.X509Certificate[] chain1 = null;
                if (certificate2 != null)
                {
                    chain1 = new[]
                    {
                        cp.ReadCertificate(certificate2.RawData)
                    };
                }
               
                // Check for certificate expiration asynchronously
                bool isNotExpired = await Task.Run(() => DateTime.Now <= certificate.NotAfter);

                if (!isNotExpired) { throw new Exception("Token is expired. Pl contact issuer!"); }

                
                if (IsCheckCrl)
                {
                    var crlTask = IsCertificateRevokedByCRLAsync(certificate, chain1);
                    var ocspTask = IsCertificateOCSPAsync(certificate);

                    await Task.WhenAll(crlTask, ocspTask);

                    var (isRevokedByCRL, crlMessage) = crlTask.Result;
                    var (isRevokedByOCSP, ocspMessage) = ocspTask.Result;

                    CrlMsg = crlMessage;
                    CrlValid = isRevokedByCRL;

                    OCSPMsg = ocspMessage;
                    OCSPValid = isRevokedByOCSP;
                }
                else
                {
                    CrlMsg = "Crl Not Checked";
                    CrlValid = true;

                    OCSPMsg = "OCSP Not Checked";
                    OCSPValid = true;
                }


                // Check the chain of trust asynchronously

                if (IsCheckCrl)
                {
                    bool isChainValid = await Task.Run(() =>
                    {
                        X509Chain chain2 = new X509Chain();
                        chain2.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain2.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                        return chain2.Build(certificate);
                    });
                    if (!isChainValid) 
                    {
                        ChainTrust = false;
                        throw new Exception("Chain of Trust is not valid. Pl contact issuer!"); 
                    }
                }
                // If all checks pass, return true
                if (ChainTrust && isNotExpired )
                {
                    validationErrorMessage = null;
                    return (true, "Token is Valid.",CrlMsg,OCSPMsg,CrlValid,OCSPValid);
                }
                else
                {
                    // Return an appropriate error message
                    validationErrorMessage = "Token validation failed.";
                    return (false , validationErrorMessage, CrlMsg, OCSPMsg, CrlValid, OCSPValid);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during certificate validation
                validationErrorMessage = ex.Message;
                return (false, validationErrorMessage, CrlMsg, OCSPMsg, CrlValid, OCSPValid);
            }
        }

        public static async Task<(bool, string)> IsCertificateRevokedByCRLAsync(X509Certificate2 cert, Org.BouncyCastle.X509.X509Certificate[] chain2)
        {
            try
            {
                bool isCrlOk = false;

                string[] arr = await Task.Run(() => CheckCertificateStatus.GetCrlDistributionPoints(cert));

                string crlUrl = null;
                if (arr.Length > 0)
                {
                    crlUrl = arr[0];
                }
                X509CrlParser crlParser = new X509CrlParser();
                X509Crl crl = CheckCertificateStatus.GetCrl(crlUrl);
                if (crl != null)
                {
                    isCrlOk = await Task.Run(() => CheckCertificateStatus.IsCRLOK(crl, chain2[0], DateTime.Now));
                }
                else
                {
                    return (false, "Crl Not Reachable");  
                }

                if (isCrlOk)
                {
                    return (true, "Crl Check Successfully");
                }
                else
                {
                    return (true, "Crl not Checked Successfully");
                }
            }
            catch (Exception ex)
            {
                return (false, "Error while checking CRL: " + ex.Message);
            }
        }

        public static async Task<(bool, string)> IsCertificateOCSPAsync(X509Certificate2 cert)
        {
            try
            {
                // Since OcspClient.Query is not async, use Task.Run to execute it on a separate thread.
                var ocspResult = await Task.Run(() =>
                {
                    OcspClient obj = new OcspClient();
                    return obj.Query(
                        Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(cert),
                        Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(CheckCertificateStatus.GetIssuer(cert))
                    );
                });

                return (true, ocspResult.ToString());
            }
            catch (Exception ex)
            {
                return (true, "Error while checking CRL: " + ex.Message);
            }
        }

        public static async Task<string> GetRequest(string URI)
        {
            try
            {
                System.Net.WebRequest req = System.Net.WebRequest.Create(URI);
                req.ContentType = "application/json";
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback
                (
                   delegate { return true; }
                );

                await Task.Run(() =>
                {
                    using (HttpWebResponse res = (HttpWebResponse)req.GetResponse())
                    {
                        lObj.mStatusCode = (int)res.StatusCode;
                        StreamReader reader = new StreamReader(res.GetResponseStream());
                        lObj.mStrResponse = reader.ReadToEnd().Trim();
                        reader.Close();
                    }
                });
                return lObj.mStrResponse.Trim();
            }
            catch (WebException e)
            {
                var response = e.Response as HttpWebResponse;
                var reader = new StreamReader(e.Response.GetResponseStream());
                lObj.mStrResponse = reader.ReadToEnd();
                lObj.mStatusCode = (Int32)response.StatusCode;
                reader.Close();
                return lObj.mStrResponse;
            }
            catch (Exception ex)
            {
                return lObj.mStrResponse + " Message :" + ex.Message;
            }
        }
        public static class lObj
        {
            public static string mStrResponse { get; set; }
            public static int mStatusCode = 400;
            public static String gurl { get; set; }
        }
    }


}

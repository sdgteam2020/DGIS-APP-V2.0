using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using System.Xml;

namespace SignService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IService1
    {

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/SignXml", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml)]

        Task<XmlElement> SignXml(XmlElement data);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/VerifySignXml", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Json)]

        List<DigitalVerifyDetails> VerifySignXml(XmlElement data);

        // Fetch Private Key from token
        // Date : 29-Sep-2022
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/GetPublicKey")]
        Task<List<TokenDetails>> GetPublicKey();

        // Add to Fetch personal details from digital certificate
        // Date : 29-Sep-2022
        [OperationContract]
        //[WebGet(UriTemplate = "FetchPersID")]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchPersID")]
        Task<List<TokenDetails>> FetchPersID();
       

        // Add to Fetch personal details from digital certificate without checking the CRL and OCSP 
        // Date : 11-Jul-2023
        //Jasjeet
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchUniqueTokenDetails")]
        Task<List<TokenDetails>> FetchUniqueTokenDetails();
        
        // Add to Fetch personal details without checking the CRL and OCSP from digital certificate
        // Date : 11-Jul-2023
        //Jasjeet 
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchTokenDetails")]
        Task<List<TokenDetails>> FetchTokenDetails();

        // To validate PersID from from digital certificate
        // Date : 29-Sep-2022
        // Jasjeet 
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ValidatePersID", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<List<PersIdValidation>> ValidatePersID(string inputPersID);

        // Add to Fetch personal details with checking the CRL and OCSP from digital certificate
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchTokenOCSPCrlDetails?IsCheckCrl={IsCheckCrl}&ThumbPrint={ThumbPrint}")]
        Task<List<TokenDetails>> FetchTokenOCSPCrlDetailsAsync(bool IsCheckCrl,string ThumbPrint);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ValidatePersID2FA", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Boolean> ValidatePersID2FA(string inputPersID);

        // Add to Sign PDF
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/DigitalSignAsync")]
        Task<ResponseBulkSign> DigitalSignAsync(List<DigitalSignData> reqData);


        // Add to Sign byte PDF
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/ByteDigitalSignAsync")]
        Task<ResponseMessage> ByteDigitalSignAsync(List<DigitalSignData> reqData);
        //[OperationContract]
        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/ByteDigitalSignAsync")]
        //Task<ResponseMessage> ByteDigitalSignAsync(HttpContent content);


        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/HasInternetConnectionAsyncTest")]
        Task<bool> HasInternetConnectionAsyncTest();

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/SignHash", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string SignHash(string rData);

        //[OperationContract]
        //[WebInvoke(Method = "POST", UriTemplate = "/SignHash1", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        //string SignHash1(string rData);

    }
    public class DigitalVerifyDetails
    {
        public bool IsVerified { get; set; }
        public string SignatureRemarks { get; set; }
        public string SignatureBy { get; set; }
        public bool IsDigest { get; set; }
        public string DigestRemarks { get; set; }
    }
    public class DigitalVerifyDetailsForUser
    {
      
        public string Signature { get; set; }
        public string SignatureBy { get; set; }
       
    } 
    [DataContract]
    public class ResponseMessage
    {
        [DataMember]
        public bool Valid { get; set; }
        [DataMember]
        public string Message { get; set; }
    }
    [DataContract]
    public class ResponseBulkSign
    {
        public List<ResponseMessage> ResponseMessagelst { get; set; }
        public ResponseMessage ResponseMessage { get; set; }
    }

    [DataContract]
    public class PersIdValidation
    {
        [DataMember]
        public bool vaildId { get; set; }
        [DataMember] 
        public bool Expired { get; set; }
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string Remark { get; set; }
    }



    [DataContract]
    public class ResponseStatus
    {
        [DataMember]
        public String Status { get; set; }
        [DataMember]
        public String Remark { get; set; }
    }

    [DataContract]
    public class TokenDetails
    {

        [DataMember]
        public String API { get; set; }
        [DataMember]
        public Boolean CRL_OCSPCheck { get; set; }
        [DataMember]
        public String CRL_OCSPMsg { get; set; }
        [DataMember]
        public String subject { get; set; }
        [DataMember]
        public String issuer { get; set; }
        [DataMember]
        public String Thumbprint { get; set; }
        [DataMember]
        public String ValidFrom { get; set; }
        [DataMember]
        public String ValidTo { get; set; }
        [DataMember]
        public String Status { get; set; }
        [DataMember]
        public String Remarks { get; set; }
        [DataMember]
        public Boolean TokenValid { get; set; }
        [DataMember]
        public string Public_Key { get; set; }

    }


    public class DigitalSignData
    {

        [DataMember]
        public String Thumbprint { get; set; }
        [DataMember]
        public String InputFileLoc { get; set; }
        [DataMember]
        public String OutputFileLoc { get; set; }
        [DataMember]       
        public string pdfpath { get; set; }
        [DataMember]
        public int XCoordinate { get; set; }
        [DataMember]
        public int YCoordinate { get; set; }
        [DataMember]
        public int Page { get; set; }
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }



        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
        [DataMember()]
        public object ScoreData;
    }
}
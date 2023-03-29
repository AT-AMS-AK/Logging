using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Addit.AK.WBD.Logging
{
    [ServiceContract]
    public interface ILogging
    {

        [OperationContract]
        Response log(LogType logType, Source source, SHORTCODE shortcode, int userid, string dl_nr, string msg);

        [OperationContract]
        Response logWithEnvironment(LogType logType, Source source, SHORTCODE shortcode, int userid, string dl_nr, string msg, ENVIRONMENT env);
    }

    [DataContract]
    public enum LogType
    {
        [EnumMember]
        INFO,
        [EnumMember]
        WARNING,
        [EnumMember]
        ERROR
    }

    [DataContract]
    public enum Source
    {
        [EnumMember]
        DATA_SERVICE,
        [EnumMember]
        DOCUMENT_GENERATION_SERVICE,
        [EnumMember]
        BANK_RECORD_CARRIER_SERVICE,
        [EnumMember]
        AUTHENTICATION_SERVICE
    }

    [DataContract]
    public enum SHORTCODE
    {
        [EnumMember]
        DA, //Datentäger Auszahlung
        [EnumMember]
        DE, //Datenträge Einzug 
        [EnumMember]
        AD, //Anweisungsdatum
        [EnumMember]
        EX, //APP EX für wnd_Error 
        [EnumMember]
        AU, //AUTHENTICATION
        [EnumMember]
        DG, //DOCUMENT GENERATION
        [EnumMember]
        BC, //BANRECORD CARRIER SERVICE
        [EnumMember]
        NONE, //NO Shortcode available

    }

    [DataContract]
    public enum ENVIRONMENT
    {
        [EnumMember]
        WBD, //Logging for WBD
        [EnumMember]
        ANF, //Logging for ANF
        [EnumMember]
        AK_SHARED_SERVICES //Logging for AK Shared Services
    }

    [DataContract]
    public class Response
    {
        int responseCode = 0;
        string responseMsg = "";
        string exeptionMsg = "";

        [DataMember]
        public int ResponseCode
        {
            get { return responseCode; }
            set { responseCode = value; }
        }

        [DataMember]
        public string ResponseMsg
        {
            get { return responseMsg; }
            set { responseMsg = value; }
        }

        [DataMember]
        public string ExeptionMsg
        {
            get { return exeptionMsg; }
            set { exeptionMsg = value; }
        }
    }
}

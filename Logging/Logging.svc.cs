using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Addit.AK.Util;
using Addit.AK.WBD.DAL;
using Oracle.DataAccess.Client;
using System.Data;

namespace Addit.AK.WBD.Logging
{
    /// <summary>
    /// Author: Bruno Hautzenberger
    /// Creation Date: 03.2011
    /// Implements functions to log events to servers eventlog
    /// </summary>
    public class Logging : ILogging
    {
        #region private members

        /// <summary>
        /// the eventlogger
        /// </summary>
        private EventLogger logger;

        /// <summary>
        /// Oracle DataAccess Object
        /// </summary>
        private static DAL_Oracle dalWBD;

        /// <summary>
        /// Oracle DataAccess Object
        /// </summary>
        private static DAL_Oracle dalANF;

        /// <summary>
        /// Oracle DataAccess Object
        /// </summary>
        private static DAL_Oracle dalAK;

        /// <summary>
        /// encryption key for config values
        /// </summary>
        private static string cryptoKey = "8h15Tw45d3RFr3ddyS46t1chF1nD3J442b3sS3r3st4ttE4t4u5chtM1tAS0nd3RZ31Ch3nJ3tztN0ch!$!((sup1K3y)";

        #endregion

        #region private methods

        /// <summary>
        /// Author: Bruno Hautzenberger
        /// Creation Date: 04.2011
        /// returns an initialized DAL Object for WBD
        /// </summary>
        private DAL_Oracle getDalWBD()
        {
            if (dalWBD == null)
            {
                dalWBD = DAL_Oracle.getInstance();
                dalWBD.Connect(Encryptor.DecryptString(System.Web.Configuration.WebConfigurationManager.AppSettings.Get("ConnectionStringWBD"), cryptoKey), Int32.Parse(System.Web.Configuration.WebConfigurationManager.AppSettings.Get("DBConnectionPoolsize")));
            }

            return dalWBD;
        }


        /// <summary>
        /// Author: Bruno Hautzenberger
        /// Creation Date: 04.2011
        /// returns an initialized DAL Object for ANF
        /// </summary>
        private DAL_Oracle getDalANF()
        {
            if (dalANF == null)
            {
                dalANF = DAL_Oracle.getInstance();
                dalANF.Connect(Encryptor.DecryptString(System.Web.Configuration.WebConfigurationManager.AppSettings.Get("ConnectionStringANF"), cryptoKey), Int32.Parse(System.Web.Configuration.WebConfigurationManager.AppSettings.Get("DBConnectionPoolsize")));
            }

            return dalANF;
        }


        /// <summary>
        /// Author: Bruno Hautzenberger
        /// Creation Date: 04.2011
        /// returns an initialized DAL Object
        /// </summary>
        private DAL_Oracle getDalAK()
        {
            if (dalAK == null)
            {
                dalAK = DAL_Oracle.getInstance();
                dalAK.Connect(Encryptor.DecryptString(System.Web.Configuration.WebConfigurationManager.AppSettings.Get("ConnectionStringAK"), cryptoKey), Int32.Parse(System.Web.Configuration.WebConfigurationManager.AppSettings.Get("DBConnectionPoolsize")));
            }

            return dalAK;
        }


        private EventLogger getLogger()
        {
            if (logger == null)
            {
                logger = EventLogger.getInstance();
                //logger.init("WBD");
            }

            return logger;
        }

        private string formatLogMsg(Source source, string msg)
        {
            return String.Format("{0} : {1}",source,msg);
        }

        private void logErrorToDb(string shortcode,int userid, string dl_nr,string msg, ENVIRONMENT env)
        {
            StringBuilder stmnt = new StringBuilder();

            if (env == ENVIRONMENT.WBD)
            {
                stmnt.Append("Insert into WORKFLOW.WBD_ERROR ");
            }
            else if (env == ENVIRONMENT.ANF)
            {
                stmnt.Append("Insert into ANF_ERROR ");
            }
            else if (env == ENVIRONMENT.AK_SHARED_SERVICES)
            {
                stmnt.Append("Insert into AK_ERROR ");
            }

            //is dl_nr "" add -1
            if (dl_nr.Length == 0)
            {
                dl_nr = "-1";
            }

            stmnt.Append("(err_ikey, err_app, err_user, err_Datum,err_dl, err_text) ");
            stmnt.Append("Values ");

            //clean msg
            msg = msg.Replace("'", "");
            msg = msg.Replace("\"", "");
            //
            // 29-10-2015 by KJ error message for ANF is 1024 instead of 100 in WBD !!!!!
            //                  error message is also in Event Viewer available
            if (env == ENVIRONMENT.ANF)
            {
                stmnt.Append(String.Format(" (seq_error.nextval, '{0}', {1}, sysdate,  '{2}', substr('{3}',1,1024))", shortcode, userid, dl_nr, msg));
            }
            else
            {
                stmnt.Append(String.Format(" (seq_error.nextval, '{0}', {1}, sysdate,  '{2}', substr('{3}',1,100))", shortcode, userid, dl_nr, msg));
            }
            //
            // 29-10-2015 by KJ
            //
            OracleCommand cmd = new OracleCommand(stmnt.ToString());

            if (env == ENVIRONMENT.WBD)
            {
                getDalWBD().executeNonQuery(cmd);
            }
            else if (env == ENVIRONMENT.ANF)
            {
                getDalANF().executeNonQuery(cmd);
            }
            else if (env == ENVIRONMENT.AK_SHARED_SERVICES)
            {
                getDalAK().executeNonQuery(cmd);
            }
        }

        #endregion

        #region service methods

        //OLD LOG CALL FOR WBD ONLY!
        public Response log(LogType logType, Source source, SHORTCODE shortcode, int userid, string dl_nr, string msg)
        {
            Response resp = new Response();

            try
            {
                EventLogger log = getLogger();
                logger.init("WBD");

                switch (logType)
                {
                    case LogType.ERROR:
                        log.logError(formatLogMsg(source, msg));
                        logErrorToDb(shortcode.ToString(), userid, dl_nr, msg, ENVIRONMENT.WBD);
                        break;
                    case LogType.WARNING:
                        log.logWarning(formatLogMsg(source, msg));
                        break;
                    case LogType.INFO:
                        log.logInfo(formatLogMsg(source, msg));
                        break;
                    default:
                        throw new Exception("Unknow Logtype!"); //If this happens somebody really failed big time!
                }
            }
            catch (Exception ex)
            {
                resp.ResponseCode = 900;
                resp.ExeptionMsg = ex.Message;
                resp.ResponseMsg = "Logging Error!";
                return resp;
            }

            resp.ResponseCode = 0;
            resp.ResponseMsg = "OK";

            return resp;
        }

        public Response logWithEnvironment(LogType logType, Source source, SHORTCODE shortcode, int userid, string dl_nr, string msg, ENVIRONMENT env)
        {
            Response resp = new Response();

            try
            {
                EventLogger log = getLogger();

                if (env == ENVIRONMENT.WBD)
                {
                    logger.init("WBD");
                }
                else if (env == ENVIRONMENT.ANF)
                {
                    logger.init("ANF");
                }
                else if (env == ENVIRONMENT.AK_SHARED_SERVICES)
                {
                    logger.init("AK");
                }

                switch (logType)
                {
                    case LogType.ERROR:
                        log.logError(formatLogMsg(source, msg));
                        logErrorToDb(shortcode.ToString(), userid, dl_nr, msg, env);
                        break;
                    case LogType.WARNING:
                        log.logWarning(formatLogMsg(source, msg));
                        break;
                    case LogType.INFO:
                        log.logInfo(formatLogMsg(source, msg));
                        break;
                    default:
                        throw new Exception("Unknow Logtype!"); //If this happens somebody really failed big time!
                }
            }
            catch (Exception ex)
            {
                resp.ResponseCode = 900;
                resp.ExeptionMsg = ex.Message;
                resp.ResponseMsg = "Logging Error!";
                return resp;
            }

            resp.ResponseCode = 0;
            resp.ResponseMsg = "OK";

            return resp;
        }

        #endregion

    }
}

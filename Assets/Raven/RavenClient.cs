using UnityEngine;
using System;
using SharpRaven.Data;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Newtonsoft.Json;
using SharpRaven.Utilities;
using SharpRaven.Logging;

namespace SharpRaven
{
    public class RavenClient
    {

        /// 
        /// To support WebClient on Android and iOS.
        /// 
        /// See http://www.vovchik.org/blog/13001 for more details
        /// 
        public class HttpRequestCreator : IWebRequestCreate
        {
            public WebRequest Create(Uri uri)
            {
                return HttpWebRequest.Create(uri);
                //return new HttpWebRequest(uri);	
            }
        }

        /// <summary>
        /// The DSN currently being used to log exceptions.
        /// </summary>
        public DSN CurrentDSN { get; set; }

        /// <summary>
        /// Interface for providing a 'log scrubber' that removes 
        /// sensitive information from exceptions sent to sentry.
        /// </summary>
        public IScrubber LogScrubber { get; set; }

        /// <summary>
        /// Enable Gzip Compression?
        /// Defaults to true.
        /// </summary>
        public bool Compression { get; set; }

        /// <summary>
        /// Logger. Default is "root"
        /// </summary>
        public string Logger { get; set; }

        public RavenClient(string dsn)
        {

            WebRequest.RegisterPrefix("http", new HttpRequestCreator());
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback = UnsafeSecurityPolicy.Validator;

            CurrentDSN = new DSN(dsn);
            Compression = true;
            Logger = "root";
        }

        public RavenClient(DSN dsn)
        {

            WebRequest.RegisterPrefix("http", new HttpRequestCreator());
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback = UnsafeSecurityPolicy.Validator;

            CurrentDSN = dsn;
            Compression = true;
            Logger = "root";
        }

        public int CaptureException(Exception e)
        {
            return CaptureException(e, null, null);
        }

        ///
        /// @kims
        /// NOTE:
        ///   Commented out secound paramter not to have default paramter due to Unity3d does not resolve that.
        ///
        public int CaptureException(Exception e, Dictionary<string, string> tags /*= null*/)
        {
            return CaptureException(e, tags, null);
        }

        ///
        /// @kims
        /// NOTE:
        ///   Commented out secound paramter not to have default paramter due to Unity3d does not resolve that.
        ///		
        public int CaptureException(Exception e, Dictionary<string, string> tags /*= null*/, object extra = null)
        {
            JsonPacket packet = new JsonPacket(CurrentDSN.ProjectID, e);
            packet.Level = ErrorLevel.error;
            packet.Tags = tags;
            packet.Extra = extra;

            Send(packet, CurrentDSN);

            return 0;
        }

        public int CaptureUntiyLog(string log, string stack, LogType logType, Dictionary<string, string> tags = null,
            object extra = null)
        {
            JsonPacket packet = new JsonPacket(CurrentDSN.ProjectID, log, stack, logType);
            packet.Level = ErrorLevel.error;
            packet.Tags = tags;
            packet.Extra = extra;

            Send(packet, CurrentDSN);

            return 0;
        }

        public int CaptureMessage(string message)
        {
            return CaptureMessage(message, ErrorLevel.info, null, null);
        }

        public int CaptureMessage(string message, ErrorLevel level)
        {
            return CaptureMessage(message, level, null, null);
        }

        public int CaptureMessage(string message, ErrorLevel level, Dictionary<string, string> tags)
        {
            return CaptureMessage(message, level, tags, null);
        }

        public int CaptureMessage(string message, ErrorLevel level /*= ErrorLevel.info*/,
            Dictionary<string, string> tags /*= null*/, object extra /*= null*/)
        {
            JsonPacket packet = new JsonPacket(CurrentDSN.ProjectID);
            packet.Message = message;
            packet.Level = level;
            packet.Tags = tags;
            packet.Extra = extra;

            Send(packet, CurrentDSN);

            return 0;
        }

        public bool Send(JsonPacket packet, DSN dsn)
        {
            packet.Logger = Logger;

            string authHeader = PacketBuilder.CreateAuthenticationHeader(dsn);

            var postHeader = new Dictionary<string, string>();
            postHeader.Add("ContentType", "application/json");
            postHeader.Add("User-Agent", "RavenSharp/1.0");
            postHeader.Add("X-Sentry-Auth", authHeader);

            string data = packet.Serialize();
            if (LogScrubber != null)
                data = LogScrubber.Scrub(data);

            Debug.Log("Header: " + PacketBuilder.CreateAuthenticationHeader(dsn));
            Debug.Log("Packet: " + data);

            var encoding = new System.Text.UTF8Encoding();

            var www = new WWW(dsn.SentryURI, encoding.GetBytes(data), postHeader);

            while (!www.isDone)
            {
                Thread.Sleep(5);
            }

            Debug.Log("Done!");

            Debug.Log("Got: " + www.text);

            return true;
        }

        #region Deprecated methods

        ///
        /// @kims
        /// NOTE:
        ///   Commented out the followings due to Unity3d spit out error because it treats them as ambigous methods.
        ///		
        /**
         *  These methods have been deprectaed in favour of the ones
         *  that have the same names as the other sentry clients, this
         *  is purely for the sake of consistency
         */
/*         
        [Obsolete("The more common CaptureException method should be used")]
        public int CaptureEvent(Exception e)
        {
            return this.CaptureException(e);
        }

        [Obsolete("The more common CaptureException method should be used")]
        public int CaptureEvent(Exception e, Dictionary<string, string> tags)
        {
            return this.CaptureException(e, tags);
        }
*/

        #endregion

    }

    public class UnsafeSecurityPolicy
    {
        public static bool Validator(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors policyErrors)
        {

            //*** Just accept and move on...
            Debug.Log("Validation successful!");
            return true;
        }
    }
}
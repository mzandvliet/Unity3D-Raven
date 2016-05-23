using UnityEngine;
using System;
using SharpRaven.Data;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using SharpRaven.Utilities;
using SharpRaven.Logging;
using UnityEngine.Experimental.Networking;

namespace SharpRaven
{
    public class RavenClient
    {
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

        private readonly Dictionary<string, string> _postHeader;
        private readonly WWW _www;
        private readonly MyJsonSerializer _serializer;

        public RavenClient(string dsn) : this(new DSN(dsn)) { }

        public RavenClient(DSN dsn)
        {
            CurrentDSN = dsn;
            Compression = true;
            Logger = "root";

            _postHeader = new Dictionary<string, string>();
            _www = new WWW("");
            _serializer = new MyJsonSerializer(null);
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

            _postHeader.Clear();
            _postHeader.Add("ContentType", "application/json");
            _postHeader.Add("User-Agent", "RavenSharp/1.0");
            _postHeader.Add("X-Sentry-Auth", authHeader);
            string[] headers = FlattenedHeadersFrom(_postHeader);

            string data = _serializer.Serialize(packet, Formatting.None); //packet.Serialize());
//            if (LogScrubber != null)
//                data = LogScrubber.Scrub(data);

//            Debug.Log("Header: " + authHeader);
//            Debug.Log("Packet: " + data);

            var encoding = new System.Text.UTF8Encoding();

            _www.InitWWW(dsn.SentryURI, encoding.GetBytes(data), headers);
//            while (!www.isDone)
//            {
//                Thread.Sleep(5);
//            }
//
//            Debug.Log("Got: " + www.text);

            return true;
        }

        // Todo: garbage control
        private static string[] FlattenedHeadersFrom(Dictionary<string, string> headers) {
            if (headers == null)
                return (string[])null;
            string[] strArray1 = new string[headers.Count * 2];
            int num1 = 0;
            using (Dictionary<string, string>.Enumerator enumerator = headers.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    KeyValuePair<string, string> current = enumerator.Current;
                    string[] strArray2 = strArray1;
                    int index1 = num1;
                    int num2 = 1;
                    int num3 = index1 + num2;
                    string str1 = current.Key.ToString();
                    strArray2[index1] = str1;
                    string[] strArray3 = strArray1;
                    int index2 = num3;
                    int num4 = 1;
                    num1 = index2 + num4;
                    string str2 = current.Value.ToString();
                    strArray3[index2] = str2;
                }
            }
            return strArray1;
        }
    }

    public class MyJsonSerializer {
        private JsonSerializer jsonSerializer;
        private StringWriter stringWriter;
        JsonTextWriter jsonTextWriter;

        public MyJsonSerializer(JsonSerializerSettings settings) {
            jsonSerializer = JsonSerializer.Create(settings);
            stringWriter = new StringWriter(new StringBuilder(128), (IFormatProvider)CultureInfo.InvariantCulture);
            jsonTextWriter = new JsonTextWriter((TextWriter) stringWriter);
        }

        public string Serialize(object value, Formatting formatting) {
            stringWriter.GetStringBuilder().Remove(0, stringWriter.GetStringBuilder().Length);
            jsonTextWriter.Formatting = formatting;
            jsonSerializer.Serialize(jsonTextWriter, value);
            return stringWriter.ToString();
        }
    }
}
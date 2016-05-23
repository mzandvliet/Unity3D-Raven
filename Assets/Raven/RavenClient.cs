using UnityEngine;
using System.Collections;
using System;
using SharpRaven.Data;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using SharpRaven.Utilities;
using SharpRaven.Logging;

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
        private readonly UTF8Encoding _encoding;
        private readonly JsonPacketSerializer _packetSerializer;
        private readonly JsonPacketPool _packetPool;
        private readonly WWWPool _wwwPool;
        private readonly MonoBehaviour _routineRunner;

        public RavenClient(string dsn, MonoBehaviour routineRunner) : this(new DSN(dsn), routineRunner) { }

        public RavenClient(DSN dsn, MonoBehaviour routineRunner)
        {
            CurrentDSN = dsn;
            _routineRunner = routineRunner;
            Compression = true;
            Logger = "root";

            _postHeader = new Dictionary<string, string>();
            _encoding = new UTF8Encoding();
            _packetSerializer = new JsonPacketSerializer();
            _packetPool = new JsonPacketPool(4);
            _wwwPool = new WWWPool(16);
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
        public int CaptureException(Exception e, Dictionary<string, string> tags /*= null*/, object extra = null) {
            JsonPacket packet = _packetPool.Take();
            packet.Create(CurrentDSN.ProjectID, e);
            packet.Level = ErrorLevel.error;
            packet.Tags = tags;
            packet.Extra = extra;

            Send(packet, CurrentDSN);

            return 0;
        }

        public int CaptureUnityLog(string log, string stack, LogType logType, Dictionary<string, string> tags = null,
            object extra = null)
        {
            JsonPacket packet = _packetPool.Take();
            packet.Create(CurrentDSN.ProjectID, log, stack, logType);
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
            JsonPacket packet = new JsonPacket();
            packet.Project = CurrentDSN.ProjectID;
            packet.Message = message;
            packet.Level = level;
            packet.Tags = tags;
            packet.Extra = extra;

            Send(packet, CurrentDSN);

            return 0;
        }

        // Todo: pool www objects, let their queries finish, handle result/error

        public bool Send(JsonPacket packet, DSN dsn)
        {
            if (_wwwPool.Count == 0) {
                Debug.LogWarning("Skipping GetSentry exception upload, too many sends...\n" + packet.Exception);
            }

            packet.Logger = Logger;

            string authHeader = PacketBuilder.CreateAuthenticationHeader(dsn);
            //            Debug.Log("Header: " + authHeader);

            _postHeader.Clear();
            _postHeader.Add("ContentType", "application/json");
            _postHeader.Add("User-Agent", "RavenSharp/1.0");
            _postHeader.Add("X-Sentry-Auth", authHeader);
            string[] headers = FlattenedHeadersFrom(_postHeader);

            string data = _packetSerializer.Serialize(packet, Formatting.None);
            _packetPool.Return(packet);

//            if (LogScrubber != null)
//                data = LogScrubber.Scrub(data);

            //Debug.Log("Packet: " + data);

            _routineRunner.StartCoroutine(SendAsync(dsn, data, headers));

            return true;
        }

        private IEnumerator SendAsync(DSN dsn, string data, string[] headers) {
            var www = _wwwPool.Take();
            www.InitWWW(dsn.SentryURI, _encoding.GetBytes(data), headers);

            while (!www.isDone) {
                yield return null;
            }
            
            if (!string.IsNullOrEmpty(www.error)) {
                Debug.LogError("Failed: " + www.error);
            }
            else {
                Debug.Log("Response: " + www.text);
            }

            _wwwPool.Return(www);
        }

        private static string[] _flattenedHeaders;
        private static string[] FlattenedHeadersFrom(Dictionary<string, string> headers) {
            if (headers == null)
                return null;

            if (_flattenedHeaders == null) {
                _flattenedHeaders = new string[headers.Count*2];
            }

            int i = 0;
            using (Dictionary<string, string>.Enumerator enumerator = headers.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    var current = enumerator.Current;
                    _flattenedHeaders[i] = current.Key;
                    _flattenedHeaders[i + 1] = current.Value;
                }
            }
            return _flattenedHeaders;
        }
    }

    public class JsonPacketPool {
        private Queue<JsonPacket> _pool;

        public JsonPacketPool(int size) {
            _pool = new Queue<JsonPacket>(size);

            for (int i = 0; i < size; i++) {
                var packet = new JsonPacket();
                _pool.Enqueue(packet);
            }
        } 

        public JsonPacket Take() {
            if (_pool.Count == 0) {
                throw new Exception("Pool is empty");
            }

            return _pool.Dequeue();
        }

        public void Return(JsonPacket packet) {
            packet.Clear();
            _pool.Enqueue(packet);
        }
    }

    public class WWWPool {
        private Queue<WWW> _pool;

        public WWWPool(int size) {
            _pool = new Queue<WWW>(size);

            for (int i = 0; i < size; i++) {
                var item = new WWW("");
                _pool.Enqueue(item);
            }
        }

        public WWW Take() {
            if (_pool.Count == 0) {
                throw new Exception("Pool is empty");
            }

            return _pool.Dequeue();
        }

        public void Return(WWW item) {
            _pool.Enqueue(item);
        }

        public int Count {
            get { return _pool.Count; }
        }
    }
}

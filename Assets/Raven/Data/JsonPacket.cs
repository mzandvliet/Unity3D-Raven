using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharpRaven.Data {
    public class JsonPacket {
        /// <summary>
        /// Hexadecimal string representing a uuid4 value.
        /// </summary>
        [JsonProperty(PropertyName = "event_id", NullValueHandling = NullValueHandling.Ignore)]
        public string EventID { get; set; }

        /// <summary>
        /// String value representing the project
        /// </summary>
        [JsonProperty(PropertyName = "project", NullValueHandling = NullValueHandling.Ignore)]
        public string Project { get; set; }

        /// <summary>
        /// Function call which was the primary perpetrator of this event.
        /// A map or list of tags for this event.
        /// </summary>
        [JsonProperty(PropertyName = "culprit", NullValueHandling = NullValueHandling.Ignore)]
        public string Culprit { get; set; }

        /// <summary>
        /// The record severity.
        /// Defaults to error.
        /// </summary>
        [JsonProperty(PropertyName = "level", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ErrorLevel Level { get; set; }

        /// <summary>
        /// Indicates when the logging record was created (in the Sentry client).
        /// Defaults to DateTime.UtcNow()
        /// </summary>
        [JsonProperty(PropertyName = "timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The name of the logger which created the record.
        /// If missing, defaults to the string root.
        /// 
        /// Ex: "my.logger.name"
        /// </summary>
        [JsonProperty(PropertyName = "logger", NullValueHandling = NullValueHandling.Ignore)]
        public string Logger { get; set; }

        /// <summary>
        /// A string representing the platform the client is submitting from. 
        /// This will be used by the Sentry interface to customize various components in the interface.
        /// </summary>
        [JsonProperty(PropertyName = "platform", NullValueHandling = NullValueHandling.Ignore)]
        public string Platform { get; set; }

        /// <summary>
        /// User-readable representation of this event
        /// </summary>
        [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        /// <summary>
        /// A map or list of tags for this event.
        /// </summary>
        [JsonProperty(PropertyName = "tags", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Tags;

        /// <summary>
        /// An arbitrary mapping of additional metadata to store with the event.
        /// </summary>
        [JsonProperty(PropertyName = "extra", NullValueHandling = NullValueHandling.Ignore)]
        public object Extra;

        /// <summary>
        /// Identifies the host client from which the event was recorded.
        /// </summary>
        [JsonProperty(PropertyName = "server_name", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerName { get; set; }

        /// <summary>
        /// A list of relevant modules (libraries) and their versions.
        /// 
        /// Automated to report all modules currently loaded in project.
        /// </summary>
        [JsonProperty(PropertyName = "modules", NullValueHandling = NullValueHandling.Ignore)]
        public List<Module> Modules { get; set; }

        [JsonProperty(PropertyName="sentry.interfaces.Exception", NullValueHandling=NullValueHandling.Ignore)]
        public SentryException Exception { get; set; }

        [JsonProperty(PropertyName = "sentry.interfaces.Stacktrace", NullValueHandling = NullValueHandling.Ignore)]
        public SentryStacktrace StackTrace { get; set; }

        public JsonPacket(string project) {
            Initialize();
            Project = project;
        }

        public JsonPacket(string project, Exception e) {
            Initialize();
            Message = e.Message;

			if (e.TargetSite != null)
			{
// ReSharper disable ConditionIsAlwaysTrueOrFalse => not for dynamic types.
                Culprit = String.Format("{0} in {1}", ((e.TargetSite.ReflectedType == null) ? "<dynamic type>" : e.TargetSite.ReflectedType.FullName), e.TargetSite.Name);
// ReSharper restore ConditionIsAlwaysTrueOrFalse
			}

            Project = project;
            ServerName = System.Environment.MachineName;
            Level = ErrorLevel.error;

            Exception = new SentryException(e);
            Exception.Module = e.Source;
            Exception.Type = e.GetType().Name;
            Exception.Value = e.Message;

            StackTrace = new SentryStacktrace(e);
            if (StackTrace.Frames.Count == 0) {
                StackTrace = null;
            }
        }
		
		public JsonPacket(string project, string log, string stack, LogType logType)
		{
			Initialize();
			Message = log;
			
            Project = project;
            ServerName = System.Environment.MachineName;
            Level = ErrorLevel.error;
			
			Exception = new SentryException(log, stack, logType);
			Exception.Module = log;
			Exception.Type = logType.ToString();
			Exception.Value = stack;
			
			StackTrace = null; 
		}

        private void Initialize() {
            // Get assemblies.
            /*Modules = new List<Module>();
            foreach (System.Reflection.Module m in Utilities.SystemUtil.GetModules()) {
                Modules.Add(new Module() {
                    Name = m.ScopeName,
                    Version = m.ModuleVersionId.ToString()
                });
            }*/
            // The current hostname
            ServerName = System.Environment.MachineName;
            // Create timestamp
            TimeStamp = DateTime.UtcNow;
            // Default logger.
            Logger = "root";
            // Default error level.
            Level = ErrorLevel.error;
            // Create a guid.
            EventID = GenerateGuid();
            // Project
            Project = "default";
            // Platform
            Platform = "csharp";
        }

        private static string GenerateGuid() {
            //return Guid.NewGuid().ToString().Replace("-", String.Empty);
            return Guid.NewGuid().ToString("N");
        }
    }

    public class Module {
        public string Name;
        public string Version;
    }

    public class JsonPacketSerializer {
        private StringWriter stringWriter;
        private JsonTextWriter writer;

        public JsonPacketSerializer() {
            stringWriter = new StringWriter(new StringBuilder(2048), (IFormatProvider)CultureInfo.InvariantCulture);
            writer = new JsonTextWriter(stringWriter);
        }

        public string Serialize(JsonPacket packet, Formatting formatting) {
            stringWriter.GetStringBuilder().Length = 0;
            writer.Formatting = formatting;

            writer.WriteStartObject();
            {
                WritePropertyIfNotNullOrEmpty("event_id", packet.EventID);
                WritePropertyIfNotNullOrEmpty("project", packet.Project);
                WritePropertyIfNotNullOrEmpty("culprit", packet.Culprit);

                writer.WritePropertyName("level");
                writer.WriteValue(packet.Level);

                writer.WritePropertyName("timestamp");
                writer.WriteValue(packet.TimeStamp.ToString("s", CultureInfo.InvariantCulture));

                WritePropertyIfNotNullOrEmpty("logger", packet.Logger);
                WritePropertyIfNotNullOrEmpty("platform", packet.Platform);
                WritePropertyIfNotNullOrEmpty("message", packet.Message);
                WriteDictionaryPropertyIfNotNull("tags", packet.Tags);

                // Todo: 'extra' object?

                WritePropertyIfNotNullOrEmpty("message", packet.ServerName);
                WriteException(packet.Exception);
                WriteStackTraceIfNotNullOrEmpty(packet.StackTrace);

            }
            writer.WriteEndObject();

            return stringWriter.ToString();
        }

        private void WritePropertyIfNotNullOrEmpty(string name, string propertyValue) {
            if (string.IsNullOrEmpty(propertyValue)) {
                return;
            }
            writer.WritePropertyName(name);
            writer.WriteValue(propertyValue);
        }

        private void WriteDictionaryPropertyIfNotNull(string name, IDictionary<string, string> d) {
            if (d == null) {
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var entry in d) {
                writer.WriteStartArray();
                writer.WriteValue(entry.Key);
                writer.WriteValue(entry.Value);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
        }

        private void WriteListIfNotNull(string name, IList<Module> list) {
            if (list == null || list.Count == 0) {
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStartObject();
            for (int i = 0; i < list.Count; i++) {
                writer.WritePropertyName(list[i].Name);
                writer.WriteValue(list[i].Version);
            }
            writer.WriteEndObject();
        }

        private void WriteException(SentryException exception) {
            writer.WritePropertyName("sentry.interfaces.Exception");
            writer.WriteStartObject();
            {
                WritePropertyIfNotNullOrEmpty("type", exception.Type);
                WritePropertyIfNotNullOrEmpty("value", exception.Value);
                WritePropertyIfNotNullOrEmpty("module", exception.Module);
            }
            writer.WriteEndObject();
        }

        private void WriteStackTraceIfNotNullOrEmpty(SentryStacktrace trace) {
            if (trace == null || trace.Frames.Count == 0) {
                return;
            }

            writer.WritePropertyName("sentry.interfaces.Stacktrace");
            writer.WriteStartObject();
            {
                writer.WritePropertyName("frames");
                writer.WriteStartArray();
                for (int i = 0; i < trace.Frames.Count; i++) {
                    var frame = trace.Frames[i];
                    writer.WriteStartObject();
                    {
                        WritePropertyIfNotNullOrEmpty("abs_path", frame.AbsolutePath);
                        WritePropertyIfNotNullOrEmpty("filename", frame.Filename);
                        WritePropertyIfNotNullOrEmpty("module", frame.Module);
                        WritePropertyIfNotNullOrEmpty("function", frame.Function);
                        WriteDictionaryPropertyIfNotNull("vars", frame.Vars);
                        WritePropertyIfNotNullOrEmpty("lineno", frame.LineNumber.ToString());
                        WritePropertyIfNotNullOrEmpty("colno", frame.ColumnNumber.ToString());
                        // Todo: pre_context, in_app, post_context
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
    }
}


/*
{
  "event_id": "18e110117d1e474fa0f9f63a2d661ccc",
  "project": "79295",
  "culprit": "CaptureTest in PerformDivideByZero",
  "level": "error",
  "timestamp": "\/Date(1464011555794)\/",
  "logger": "C#",
  "platform": "csharp",
  "message": "Division by zero",
  "server_name": "DESKTOP-AEAMDR3",
  "sentry.interfaces.Exception": {
    "type": "DivideByZeroException",
    "value": "Division by zero",
    "module": "Assembly-CSharp"
  },
  "sentry.interfaces.Stacktrace": {
    "frames": [
      {
        "abs_path": null,
        "filename": "E:\\code\\raven_sharp_unity\\Assets\\Script\\CaptureTest.cs",
        "module": "CaptureTest",
        "function": "testWithStacktrace",
        "vars": null,
        "pre_context": null,
        "context_line": "Void testWithStacktrace()",
        "lineno": 57,
        "colno": 0,
        "in_app": false,
        "post_context": null
      },
      {
        "abs_path": null,
        "filename": "E:\\code\\raven_sharp_unity\\Assets\\Script\\CaptureTest.cs",
        "module": "CaptureTest",
        "function": "PerformDivideByZero",
        "vars": null,
        "pre_context": null,
        "context_line": "Void PerformDivideByZero()",
        "lineno": 70,
        "colno": 0,
        "in_app": false,
        "post_context": null
      }
    ]
  }
}
*/

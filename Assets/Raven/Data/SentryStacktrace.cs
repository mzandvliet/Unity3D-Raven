using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Newtonsoft.Json;

namespace SharpRaven.Data {
    public class SentryStacktrace {
        [JsonProperty(PropertyName = "frames")]
        public List<ExceptionFrame> Frames;

        public SentryStacktrace() {
            Frames = new List<ExceptionFrame>(8);
        }

        // Todo: Simplify into a string.split operation, we're not getting more out of this than contained in e.trace string anyway

        public void Create(Exception e) {
            StackTrace trace = new StackTrace(e, true);

            for (int i = 0; i < trace.FrameCount; i++) {
                var frame = trace.GetFrame(i);

                int lineNo = frame.GetFileLineNumber();

                if (lineNo == 0) {
                    //The pdb files aren't currently available
                    lineNo = frame.GetILOffset();
                }

                var method = frame.GetMethod();
                var frameData = new ExceptionFrame() {
                    Filename = frame.GetFileName(),
                    Module = (method.DeclaringType != null) ? method.DeclaringType.FullName : null,
                    Function = method.Name,
                    Source = method.ToString(),
                    LineNumber = lineNo,
                    ColumnNumber = frame.GetFileColumnNumber()
                };

                Frames.Add(frameData);
            }
        }

        public void Clear() {
            Frames.Clear();
        }
    }
}

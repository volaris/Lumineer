/* 
 * Copyright (c) 2015 Lane Haury
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software.
 * 
 * Except as contained in this notice, the name(s) of the above copyright 
 * holders shall not be used in advertising or otherwise to promote the sale, 
 * use or other dealings in this Software without prior written authorization.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OSCEndpoint
{
    [JsonConverter(typeof(MethodConverter))]
    public class OSCMethod : OSCNode
    {
        public OSCMethod() : this(string.Empty, null)
        {
        }

        public OSCMethod(string name, OSCContainer containerParent) : base(name, containerParent)
        {
            this.Arguments = new List<OSCArgument>();
        }

        public List<OSCArgument> Arguments { get; set; }

        public bool Invoke(List<OSCArgument> arguments)
        {
            if (OnInvoke == null)
            {
                return true;
            }
            MethodEventArgs args = new MethodEventArgs();
            args.OSCArgs = arguments;
            return OnInvoke(this, args);
        }

        public delegate bool InvokeHandler(object sender, MethodEventArgs args);
        public event InvokeHandler OnInvoke;

        internal static IEnumerable<OSCTypes> ConvertTypeString(string p)
        {
            List<OSCTypes> types = new List<OSCTypes>();

            for (int i = 0; i < p.Length; i++)
            {
                types.Add(OSCArgument.GetType(p[i]));
            }

            return types;
        }
    }

    public class MethodEventArgs
    {
        public List<OSCArgument> OSCArgs;
    }

    public class MethodConverter : JsonConverter
    {
        protected class MethodArgumentList : OSCNode
        {
            public string TYPE;
            public List<OSCRange> RANGE;
            public List<object> VALUE;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(OSCMethod) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            OSCMethod method = new OSCMethod();

            method.Name = obj["FULL_PATH"].Value<string>();
            method.Name = System.IO.Path.GetFileName(method.Name);

            method.Description = obj["DESCRIPTION"].Value<string>();

            IEnumerable<OSCRange> ranges = JsonConvert.DeserializeObject<IEnumerable<OSCRange>>(((JArray)obj["RANGE"]).ToString());
            IEnumerable<object> values = obj["VALUE"].Values<object>();
            IEnumerable<OSCTypes> types = OSCMethod.ConvertTypeString(obj["TYPE"].Value<string>());

            IEnumerator<OSCRange> rangeEnum = ranges.GetEnumerator();
            IEnumerator<object> valueEnum = values.GetEnumerator();
            IEnumerator<OSCTypes> typeEnum = types.GetEnumerator();

            while (rangeEnum.MoveNext() && valueEnum.MoveNext() && typeEnum.MoveNext())
            {
                OSCArgument arg = new OSCArgument();
                arg.Range = rangeEnum.Current;
                arg.Value = ((JValue)valueEnum.Current).Value;
                arg.Type = typeEnum.Current;

                method.Arguments.Add(arg);
            }

            return method;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            OSCMethod method = (OSCMethod)value;
            MethodArgumentList node = new MethodArgumentList();
            node.Parent = method.Parent;
            node.Description = method.Description;
            node.Name = method.Name;

            string types = string.Empty;

            List<OSCRange> ranges = new List<OSCRange>();
            List<OSCClipMode> clipModes = new List<OSCClipMode>();
            List<object> values = new List<object>();

            foreach (OSCArgument arg in method.Arguments)
            {
                types += OSCArgument.GetTypeChar(arg.Type);
                ranges.Add(arg.Range);
                values.Add(arg.Value);
            }

            node.TYPE = types;
            node.RANGE = ranges;
            node.VALUE = values;

            serializer.Serialize(writer, node);
        }
    }
}

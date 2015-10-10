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
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OSCEndpoint
{
    [JsonConverter(typeof(RangeConverter))]
    public class OSCRange
    {
        public enum ValidateType
        {
            In,
            OutHigh,
            OutLow,
            Out
        }

        public OSCArgument High { get; set; }

        public OSCArgument Low { get; set; }

        public List<OSCArgument> Enum { get; set; }

        public ValidateType ValidateVal(dynamic arg)
        {
            if (High != null && (dynamic)arg > (dynamic)High.Value)
            {
                return ValidateType.OutHigh;
            }

            if (Low != null && (dynamic)arg < (dynamic)Low.Value)
            {
                return ValidateType.OutLow;
            }

            if (Enum != null && Enum.Count > 0)
            {
                bool found = false;
                foreach (OSCArgument enumArg in Enum)
                {
                    if ((dynamic)enumArg.Value == (dynamic)arg)
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    return ValidateType.Out;
                }
            }

            return ValidateType.In;
        }

        public ValidateType Validate(OSCArgument arg)
        {
            return ValidateVal(arg.Value);
        }
    }

    public class RangeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            OSCRange range = (OSCRange)value;
            writer.WriteStartArray();
            if(range.Low == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(range.Low.Value);
            }
            if (range.High == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(range.High.Value);
            }
            if (range.Enum == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                foreach(OSCArgument arg in range.Enum)
                {
                    writer.WriteValue(arg.Value);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken obj = JToken.Load(reader);
            OSCRange range = null;

            if (obj is JArray)
            {
                range = new OSCRange();
                range.Low = new OSCArgument();
                range.Low.Value = ((JValue)obj[0]).Value;
                range.High = new OSCArgument();
                range.High.Value = ((JValue)obj[1]).Value;
                if (obj[2] is JArray)
                {
                    JArray enumeration = (JArray)obj[2];
                    if (enumeration != null)
                    {
                        range.Enum = new List<OSCArgument>();
                        foreach (JValue val in enumeration.Values())
                        {
                            OSCArgument arg = new OSCArgument();
                            arg.Value = val.Value;
                            range.Enum.Add(arg);
                        }
                    }
                }
            }
            return range;
        }

        public override bool CanConvert(Type objectType)
        {
            return (typeof(OSCRange) == objectType);
        }
    }
}

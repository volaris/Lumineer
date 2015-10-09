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

namespace OSCEndpoint
{
    public enum OSCTypes
    {  
        Int32,
        Float32,
        OSCString,
        OSCBlob,
        Int64,
        OSCTimetag,
        Float64,
        [Obsolete]
        AltString, // not used
        Char,
        Color,
        [Obsolete]
        MIDI,  // not used
        True,
        False,
        Nil,
        Infinitum,
        [Obsolete] // not really, the underlying OSC library doesn't support this, we do though
        Array
    }

    public enum OSCClipMode
    {
        None,
        Low,
        High,
        Both
    }

    public class OSCArgument
    {
        public OSCArgument()
        {
            this.Unit = string.Empty;
        }

        dynamic value;

        public OSCTypes Type { get; set; }
        public dynamic Value 
        {
            get
            {
                return value;
            }
            set
            {
                if(Range != null)
                {
                    OSCRange.ValidateType result = Range.ValidateVal(value);
                    switch(result)
                    {
                        case OSCRange.ValidateType.In:
                            this.value = value;
                            break;
                        case OSCRange.ValidateType.OutHigh:
                            if(this.ClipMode == OSCClipMode.High || this.ClipMode == OSCClipMode.Both)
                            {
                                this.value = Range.High.Value;
                            }
                            else
                            {
                                this.value = value;
                            }
                            break;
                        case OSCRange.ValidateType.OutLow:
                            if (this.ClipMode == OSCClipMode.Low || this.ClipMode == OSCClipMode.Both)
                            {
                                this.Value = Range.Low.Value;
                            }
                            else
                            {
                                this.value = value;
                            }
                            break;
                    }
                }
                else
                {
                    this.value = value;
                }
            }
        }
        public OSCRange Range { get; set; }
        public string Unit { get; set; }
        public OSCClipMode ClipMode { get; set; }

        public static OSCArgument Create<T>(T val)
        {
            OSCArgument argument = new OSCArgument();

            if (val == null)
            {
                argument.Type = OSCTypes.Nil;
            }
            else
            {
                switch (typeof(T).Name)
                {
                    case "Int32":
                        argument.Type = OSCTypes.Int32;
                        break;
                    case "Int64":
                        argument.Type = OSCTypes.Int64;
                        break;
                    case "Single":
                        argument.Type = float.IsInfinity((dynamic)val) ? OSCTypes.Infinitum : OSCTypes.Float32;
                        break;
                    case "Double":
                        argument.Type = double.IsInfinity((dynamic)val) ? OSCTypes.Infinitum : OSCTypes.Float64;
                        break;
                    case "String":
                        argument.Type = OSCTypes.OSCString;
                        break;
                    case "Byte[]":
                        argument.Type = OSCTypes.OSCBlob;
                        break;
                    case "OscTimeTag":
                        argument.Type = OSCTypes.OSCTimetag;
                        break;
                    case "Char":
                        argument.Type = OSCTypes.Char;
                        break;
                    case "Color":
                        argument.Type = OSCTypes.Color;
                        break;
                    case "Boolean":
                        argument.Type = (dynamic)val ? OSCTypes.True : OSCTypes.False;
                        break;
                    case "IEnumerable`1":
                        if (typeof(T).GenericTypeArguments.Length == 1 && typeof(T).GenericTypeArguments[0].Name == "OSCArgument")
#pragma warning disable 612
                            argument.Type = OSCTypes.Array;
#pragma warning restore 612
                        break;
                    default:
                        throw new ArgumentException("Type not supported");
                }
            }

            argument.Value = val;

            return argument;
        }

        public OSCRange.ValidateType Validate()
        {
            return this.Range.Validate(this);
        }

        public static char GetTypeChar(OSCTypes type)
        {
            switch(type)
            {
                case OSCTypes.Int32:
                    return 'i';
                case OSCTypes.Float32:
                    return 'f';
                case OSCTypes.OSCString:
                    return 's';
                case OSCTypes.OSCBlob:
                    return 'b';
                case OSCTypes.Int64:
                    return 'h';
                case OSCTypes.OSCTimetag:
                    return 't';
                case OSCTypes.Float64:
                    return 'd';
#pragma warning disable 612
                case OSCTypes.AltString:
#pragma warning restore 612
                    return 'S';
                case OSCTypes.Char:
                    return 'c';
                case OSCTypes.Color:
                    return 'r';
#pragma warning disable 612
                case OSCTypes.MIDI:
#pragma warning restore 612
                    return 'm';
                case OSCTypes.True:
                    return 'T';
                case OSCTypes.False:
                    return 'F';
                case OSCTypes.Nil:
                    return 'N';
                case OSCTypes.Infinitum:
                    return 'I';
#pragma warning disable 612
                case OSCTypes.Array:
#pragma warning restore 612
                    break;
            }
            throw new ArgumentException("Invalid type");
        }
    }
}

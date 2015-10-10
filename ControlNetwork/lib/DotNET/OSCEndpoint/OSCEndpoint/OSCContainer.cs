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
    [JsonConverter(typeof(OSCContainerConverter))]
    public class OSCContainer : OSCNode
    {
        public OSCContainer() : this(null, null)
        {
        }

        public OSCContainer(string name, OSCContainer containerParent)
            : base(name, containerParent)
        {
            this.Children = new Dictionary<string, OSCNode>();
        }

        [JsonProperty("CONTENTS")]
        public Dictionary<string, OSCNode> Children { get; set; }

    }

    public class OSCContainerConverter : JsonConverter
    {
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            OSCContainer container = new OSCContainer();

            container.Name = obj["FULL_PATH"].Value<string>();
            container.Name = System.IO.Path.GetFileName(container.Name);

            container.Description = obj["DESCRIPTION"].Value<string>();

            foreach (JToken content in obj["CONTENTS"])
            {
                JProperty contentProperty = (JProperty)content;
                JToken contents;
                if (((JObject)contentProperty.Value).TryGetValue("CONTENTS", out contents))
                {
                    /*Dictionary<string, OSCContainer> containerDict = JsonConvert.DeserializeObject<Dictionary<string, OSCContainer>>(content.ToString());
                    foreach(string key in containerDict.Keys)
                    {
                        container.Children.Add(key, containerDict[key]);
                    }*/
                    OSCContainer childContainer = JsonConvert.DeserializeObject<OSCContainer>(contentProperty.Value.ToString());
                    childContainer.Name = contentProperty.Name;
                    container.Children.Add(childContainer.Name, childContainer);
                    childContainer.Parent = container;
                }
                else
                {
                    /*Dictionary<string, OSCMethod> methodDict = JsonConvert.DeserializeObject<Dictionary<string, OSCMethod>>(content.ToString());
                    foreach (string key in methodDict.Keys)
                    {
                        container.Children.Add(key, methodDict[key]);
                    }*/
                    OSCMethod method = JsonConvert.DeserializeObject<OSCMethod>(contentProperty.Value.ToString());
                    method.Name = contentProperty.Name;
                    container.Children.Add(method.Name, method);
                    method.Parent = container;
                }
            }

            return container;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return (typeof(OSCContainer) == objectType);
        }
    }
}

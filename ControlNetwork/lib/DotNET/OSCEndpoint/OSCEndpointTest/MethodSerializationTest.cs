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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSCEndpoint;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OSCEndpointTest
{
    [TestClass]
    public class MethodSerializationTest
    {
        [TestMethod, TestCategory("JSON Serialization")]
        public void HasDescription()
        {
            OSCMethod method = new OSCMethod();
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("DESCRIPTION"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void DescriptionCorrect()
        {
            OSCMethod method = new OSCMethod();
            method.Description = "foo";
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("DESCRIPTION"));
            Assert.AreEqual("foo", value["DESCRIPTION"]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void DoesNotHaveName()
        {
            OSCMethod method = new OSCMethod();
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsFalse(value.ContainsKey("Name"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void DoesNotHaveParent()
        {
            OSCMethod method = new OSCMethod();
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsFalse(value.ContainsKey("Parent"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void HasFullPath()
        {
            OSCMethod method = new OSCMethod();
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("FULL_PATH"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void FullPathHasFullPath()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";
            OSCMethod method = new OSCMethod("bar", container, new List<OSCArgument>());
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("FULL_PATH"));
            Assert.AreEqual("/foo/bar", value["FULL_PATH"]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ArgumentType()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";
            OSCMethod method = new OSCMethod("bar", container, new List<OSCArgument>());

            OSCArgument arg = OSCArgument.Create<int>(1);
            method.AddArgument(arg);

            string json = JsonConvert.SerializeObject(method, Formatting.Indented);

            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("TYPE"));
            Assert.AreEqual("i", value["TYPE"]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ArgumentTypes()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";
            OSCMethod method = new OSCMethod("bar", container, new List<OSCArgument>());

            OSCArgument arg1 = OSCArgument.Create<long>(1);
            OSCArgument arg2 = OSCArgument.Create<float>(1.0f);
            method.AddArgument(arg1);
            method.AddArgument(arg2);

            string json = JsonConvert.SerializeObject(method, Formatting.Indented);

            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("TYPE"));
            Assert.AreEqual("hf", value["TYPE"]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ArgumentRanges()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";
            OSCMethod method = new OSCMethod("bar", container, new List<OSCArgument>());

            OSCRange range1 = new OSCRange();
            OSCArgument arg1 = OSCArgument.Create<long>(1);
            range1.High = OSCArgument.Create<long>(6);
            range1.Low = OSCArgument.Create<long>(0);
            arg1.Range = range1;

            OSCRange range2 = new OSCRange();
            OSCArgument arg2 = OSCArgument.Create<float>(1.0f);
            range2.High = OSCArgument.Create<float>(6.0f);
            range2.Low = OSCArgument.Create<float>(0.0f);
            arg2.Range = range2;

            method.AddArgument(arg1);
            method.AddArgument(arg2);

            string json = JsonConvert.SerializeObject(method, Formatting.Indented);

            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("RANGE"));
            Assert.AreEqual(2, ((Newtonsoft.Json.Linq.JArray)value["RANGE"]).Count);
            Newtonsoft.Json.Linq.JArray rangeList = (Newtonsoft.Json.Linq.JArray)value["RANGE"];
            Assert.AreEqual(3, ((Newtonsoft.Json.Linq.JArray)rangeList[0]).Count);
            Assert.AreEqual(0, ((Newtonsoft.Json.Linq.JArray)rangeList[0])[0]);
            Assert.AreEqual(6, ((Newtonsoft.Json.Linq.JArray)rangeList[0])[1]);

            Assert.AreEqual(3, ((Newtonsoft.Json.Linq.JArray)rangeList[1]).Count);
            Assert.AreEqual(0.0f, ((Newtonsoft.Json.Linq.JArray)rangeList[1])[0]);
            Assert.AreEqual(6.0f, ((Newtonsoft.Json.Linq.JArray)rangeList[1])[1]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ArgumentValues()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";
            OSCMethod method = new OSCMethod("bar", container, new List<OSCArgument>());

            OSCRange range1 = new OSCRange();
            OSCArgument arg1 = OSCArgument.Create<long>(1);
            range1.High = OSCArgument.Create<long>(6);
            range1.Low = OSCArgument.Create<long>(0);
            arg1.Range = range1;

            OSCRange range2 = new OSCRange();
            OSCArgument arg2 = OSCArgument.Create<float>(1.0f);
            range2.High = OSCArgument.Create<float>(6.0f);
            range2.Low = OSCArgument.Create<float>(0.0f);
            arg2.Range = range2;

            method.AddArgument(arg1);
            method.AddArgument(arg2);

            string json = JsonConvert.SerializeObject(method, Formatting.Indented);

            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("VALUE"));
            Assert.AreEqual(2, ((Newtonsoft.Json.Linq.JArray)value["VALUE"]).Count);
            Newtonsoft.Json.Linq.JArray valueList = (Newtonsoft.Json.Linq.JArray)value["VALUE"];
            Assert.AreEqual(2, valueList.Count);

            Assert.AreEqual(1, valueList[0]);

            Assert.AreEqual(1.0, valueList[1]);
        }
    }
}

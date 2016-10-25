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
    public class MethodDeserializationTest
    {
        [TestMethod, TestCategory("JSON Serialization")]
        public void Name()
        {
            OSCMethod method = new OSCMethod();
            method.Name = "bar";
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            OSCMethod method1 = JsonConvert.DeserializeObject<OSCMethod>(json);
            Assert.AreEqual("bar", method1.Name);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void Description()
        {
            OSCMethod method = new OSCMethod();
            method.Description = "foo";
            string json = JsonConvert.SerializeObject(method, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            OSCMethod method1 = JsonConvert.DeserializeObject<OSCMethod>(json);
            Assert.AreEqual("foo", method1.Description);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ArgumentType()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";

            OSCArgument arg = OSCArgument.Create<int>(1);
            OSCMethod method = new OSCMethod("bar", container, new List<OSCArgument>());
            method.AddArgument(arg);

            string json = JsonConvert.SerializeObject(method, Formatting.Indented);

            OSCMethod method1 = JsonConvert.DeserializeObject<OSCMethod>(json);
            Assert.AreEqual(1, method1.QueryArguments().Count);
            Assert.AreEqual(OSCTypes.Int32, method1.QueryArguments()[0].Type);
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

            OSCMethod method1 = JsonConvert.DeserializeObject<OSCMethod>(json);
            Assert.AreEqual(2, method1.QueryArguments().Count);
            Assert.AreEqual(OSCTypes.Int64, method1.QueryArguments()[0].Type);
            Assert.AreEqual(OSCTypes.Float32, method1.QueryArguments()[1].Type);
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

            OSCMethod method1 = JsonConvert.DeserializeObject<OSCMethod>(json);

            Assert.AreEqual(2, method1.QueryArguments().Count);
            Assert.AreEqual(0, method1.QueryArguments()[0].Range.Low.Value);
            Assert.AreEqual(6, method1.QueryArguments()[0].Range.High.Value);

            Assert.AreEqual(0.0f, method1.QueryArguments()[0].Range.Low.Value);
            Assert.AreEqual(6.0f, method1.QueryArguments()[0].Range.High.Value);
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

            OSCMethod method1 = JsonConvert.DeserializeObject<OSCMethod>(json);

            Assert.AreEqual(2, method1.QueryArguments().Count);
            Assert.AreEqual(1, method1.QueryArguments()[0].Value);

            Assert.AreEqual(1.0, method1.QueryArguments()[1].Value);
        }
    }
}

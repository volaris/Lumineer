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
    public class ContainerSerializationTest
    {
        [TestMethod, TestCategory("JSON Serialization")]
        public void HasDescription()
        {
            OSCContainer Container = new OSCContainer();
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("DESCRIPTION"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void DescriptionCorrect()
        {
            OSCContainer Container = new OSCContainer();
            Container.Description = "foo";
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("DESCRIPTION"));
            Assert.AreEqual("foo", value["DESCRIPTION"]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void DoesNotHaveName()
        {
            OSCContainer Container = new OSCContainer();
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsFalse(value.ContainsKey("Name"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void DoesNotHaveParent()
        {
            OSCContainer Container = new OSCContainer();
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsFalse(value.ContainsKey("Parent"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void HasFullPath()
        {
            OSCContainer Container = new OSCContainer();
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("FULL_PATH"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void FullPathHasFullPath1()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";
            OSCContainer Container = new OSCContainer("bar", container);
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("FULL_PATH"));
            Assert.AreEqual("/foo/bar", value["FULL_PATH"]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void FullPathHasFullPath2()
        {
            OSCContainer container1 = new OSCContainer();
            container1.Name = "foobar";
            OSCContainer container2 = new OSCContainer("foo", container1);
            OSCContainer Container = new OSCContainer("bar", container2);
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("FULL_PATH"));
            Assert.AreEqual("/foobar/foo/bar", value["FULL_PATH"]);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void HasContents()
        {
            OSCContainer container1 = new OSCContainer();
            container1.Name = "foobar";
            OSCContainer container2 = new OSCContainer("foo", container1);
            OSCContainer Container = new OSCContainer("bar", container2);
            string json = JsonConvert.SerializeObject(container1, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("CONTENTS"));
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ContentsAreCorrect()
        {
            OSCContainer container1 = new OSCContainer();
            container1.Name = "foobar";
            OSCContainer container2 = new OSCContainer("foo", container1);
            OSCContainer Container = new OSCContainer("bar", container1);
            string json = JsonConvert.SerializeObject(container1, Formatting.Indented);
            Dictionary<string, object> value = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.IsTrue(value.ContainsKey("CONTENTS"));
            Newtonsoft.Json.Linq.JObject contents = (Newtonsoft.Json.Linq.JObject)value["CONTENTS"];
            
            Assert.IsTrue(contents.Count == 2);
            Assert.AreEqual("/foobar/foo", contents["foo"].Value<string>("FULL_PATH"));
            Assert.AreEqual("/foobar/bar", contents["bar"].Value<string>("FULL_PATH"));
        }
    }
}

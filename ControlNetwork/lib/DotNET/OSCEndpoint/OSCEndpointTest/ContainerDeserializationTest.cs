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
    public class ContainerDeserializationTest
    {
        [TestMethod, TestCategory("JSON Serialization")]
        public void DescriptionCorrect()
        {
            OSCContainer Container = new OSCContainer();
            Container.Description = "foo";
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            OSCContainer value = JsonConvert.DeserializeObject<OSCContainer>(json);
            Assert.AreEqual("foo", value.Description);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void NameCorrect()
        {
            OSCContainer container = new OSCContainer();
            container.Name = "foo";
            OSCContainer Container = new OSCContainer("bar", container);
            string json = JsonConvert.SerializeObject(Container, Formatting.Indented);
            OSCContainer value = JsonConvert.DeserializeObject<OSCContainer>(json);
            Assert.AreEqual("bar", value.Name);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ContentsAreCorrect1()
        {
            OSCContainer container1 = new OSCContainer();
            container1.Name = "foobar";
            OSCContainer container2 = new OSCContainer("foo", container1);
            OSCContainer Container = new OSCContainer("bar", container1);
            string json = JsonConvert.SerializeObject(container1, Formatting.Indented);
            OSCContainer value = JsonConvert.DeserializeObject<OSCContainer>(json);

            Assert.IsTrue(value.Children.Count == 2);
            Assert.AreEqual("foobar", value.Name);
            Assert.AreEqual("foo", value.Children["foo"].Name);
            Assert.AreEqual("bar", value.Children["bar"].Name);
            Assert.AreEqual("/foobar/foo", value.Children["foo"].FullPath);
            Assert.AreEqual("/foobar/bar", value.Children["bar"].FullPath);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ContentsAreCorrect2()
        {
            OSCContainer container1 = new OSCContainer();
            container1.Name = "foobar";
            OSCContainer container2 = new OSCContainer("foo", container1);
            OSCContainer Container = new OSCContainer("bar", container2);
            string json = JsonConvert.SerializeObject(container1, Formatting.Indented);
            OSCContainer value = JsonConvert.DeserializeObject<OSCContainer>(json);
            Assert.AreEqual("foobar", value.Name);
            Assert.AreEqual("/foobar", value.FullPath);
            Assert.AreEqual("foo", value.Children["foo"].Name);
            Assert.AreEqual("/foobar/foo", value.Children["foo"].FullPath);
            Assert.AreEqual("bar", ((OSCContainer)value.Children["foo"]).Children["bar"].Name);
            Assert.AreEqual("/foobar/foo/bar",((OSCContainer) value.Children["foo"]).Children["bar"].FullPath);
        }

        [TestMethod, TestCategory("JSON Serialization")]
        public void ContentsAreCorrect3()
        {
            OSCContainer container1 = new OSCContainer();
            container1.Name = "foobar";
            OSCMethod method = new OSCMethod("barfoo", container1);

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

            method.Arguments.Add(arg1);
            method.Arguments.Add(arg2);
            OSCContainer container2 = new OSCContainer("foo", container1);
            OSCContainer Container = new OSCContainer("bar", container1);
            string json = JsonConvert.SerializeObject(container1, Formatting.Indented);
            OSCContainer value = JsonConvert.DeserializeObject<OSCContainer>(json);

            Assert.IsTrue(value.Children.Count == 3);
            Assert.AreEqual("foobar", value.Name);
            Assert.AreEqual("foo", value.Children["foo"].Name);
            Assert.AreEqual("bar", value.Children["bar"].Name);
            Assert.AreEqual("/foobar/foo", value.Children["foo"].FullPath);
            Assert.AreEqual("/foobar/bar", value.Children["bar"].FullPath);
            OSCMethod method1 = (OSCMethod)value.Children["barfoo"];

            Assert.AreEqual(2, method1.Arguments.Count);
            Assert.AreEqual(1, method1.Arguments[0].Value);

            Assert.AreEqual(1.0, method1.Arguments[1].Value);
        }
    }
}

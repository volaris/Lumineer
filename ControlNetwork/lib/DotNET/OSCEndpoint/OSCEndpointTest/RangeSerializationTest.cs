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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSCEndpoint;
using Newtonsoft.Json;

namespace OSCEndpointTest
{
    [TestClass]
    public class RangeSerializationTest
    {
        [TestMethod, TestCategory("Range Serialization")]
        public void SerializeHigh()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = OSCArgument.Create<int>(5);
            range.High = arg;
            string json = JsonConvert.SerializeObject(range, Formatting.None);
            Assert.AreEqual("[null,5,null]", json);
        }

        [TestMethod, TestCategory("Range Serialization")]
        public void SerializeLow()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = OSCArgument.Create<int>(1);
            range.Low = arg;
            string json = JsonConvert.SerializeObject(range, Formatting.None);
            Assert.AreEqual("[1,null,null]", json);
        }

        [TestMethod, TestCategory("Range Serialization")]
        public void SerializeBoth()
        {
            OSCRange range = new OSCRange();
            OSCArgument argHigh = OSCArgument.Create<int>(5);
            OSCArgument argLow = OSCArgument.Create<int>(1);
            range.High = argHigh;
            range.Low = argLow;
            string json = JsonConvert.SerializeObject(range, Formatting.None);
            Assert.AreEqual("[1,5,null]", json);
        }

        [TestMethod, TestCategory("Range Serialization")]
        public void SerializeEnum()
        {
            OSCRange range = new OSCRange();
            OSCArgument argOne = OSCArgument.Create<string>("foo");
            OSCArgument argTwo = OSCArgument.Create<string>("bar");
            range.Enum = new List<OSCArgument>();
            range.Enum.Add(argOne);
            range.Enum.Add(argTwo);
            string json = JsonConvert.SerializeObject(range, Formatting.None);
            Assert.AreEqual("[null,null,[\"foo\",\"bar\"]]", json);
        }
    }
}

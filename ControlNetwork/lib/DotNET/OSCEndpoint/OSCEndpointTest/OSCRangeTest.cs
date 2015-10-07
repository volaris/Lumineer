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

namespace OSCEndpointTest
{
    [TestClass]
    public class OSCRangeTest
    {
        [TestMethod, TestCategory("OSCRangeTest")]
        public void HasHigh()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            range.High = arg;
            Assert.AreEqual(range.High, arg);
        }

        [TestMethod, TestCategory("OSCRangeTest")]
        public void HasLow()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            range.Low = arg;
            Assert.AreEqual(range.Low, arg);
        }

        [TestMethod, TestCategory("OSCRangeTest")]
        public void HasEnum()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            Assert.IsTrue(range.Enum == null);
            List<OSCArgument> list = new List<OSCArgument>();
            range.Enum = list;
            Assert.IsTrue(range.Enum == list);
        }

        [TestMethod, TestCategory("OSCRangeTest")]
        public void CanValidateRange()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            OSCArgument argHigh = OSCArgument.Create<int>(5);
            OSCArgument argLow = OSCArgument.Create<int>(1);
            OSCArgument argIn = OSCArgument.Create<int>(2);
            OSCArgument argOutHigh = OSCArgument.Create<int>(6);
            OSCArgument argOutLow = OSCArgument.Create<int>(0);
            range.High = argHigh;
            range.Low = argLow;

            Assert.IsTrue(OSCRange.ValidateType.In == range.Validate(argIn));
            Assert.IsTrue(OSCRange.ValidateType.OutHigh == range.Validate(argOutHigh));
            Assert.IsTrue(OSCRange.ValidateType.OutLow == range.Validate(argOutLow));
        }

        [TestMethod, TestCategory("OSCRangeTest")]
        public void CanValidateEnum()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            OSCArgument argOne = OSCArgument.Create<string>("foo");
            OSCArgument argTwo = OSCArgument.Create<string>("bar");
            OSCArgument argIn = OSCArgument.Create<string>("foo");
            OSCArgument argOut = OSCArgument.Create<string>("foobar");
            range.Enum = new List<OSCArgument>();
            range.Enum.Add(argOne);
            range.Enum.Add(argTwo);

            Assert.IsTrue(OSCRange.ValidateType.In == range.Validate(argIn));
            Assert.IsTrue(OSCRange.ValidateType.Out == range.Validate(argOut));
        }
    }
}

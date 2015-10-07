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
using Bespoke.Common.Osc;
using System.Drawing;
using System.Collections.Generic;

namespace OSCEndpointTest
{
    [TestClass]
    public class OSCArgumentTest
    {
        [TestMethod, TestCategory("OSCArgument")]
        public void HasType()
        {
            OSCArgument argument = new OSCArgument();
#pragma warning disable 183
            Assert.IsTrue(argument.Type is OSCTypes);
#pragma warning restore 183
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void HasValue()
        {
            OSCArgument argument = new OSCArgument();
            Assert.IsTrue(argument.Value == null);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateInt32()
        {
            int i = 0;
            OSCArgument argument = OSCArgument.Create<int>(i);
            Assert.IsTrue(argument.Value is int);
            Assert.IsTrue((int)argument.Value == 0);
            Assert.IsTrue(argument.Type == OSCTypes.Int32);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateFloat32()
        {
            VerifyType<float>(0, OSCTypes.Float32);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateFloat64()
        {
            VerifyType<double>(0, OSCTypes.Float64);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateString()
        {
            VerifyType<string>("foo", OSCTypes.OSCString);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateTimeTag()
        {
            OscTimeTag time = new OscTimeTag();
            VerifyType<OscTimeTag>(time, OSCTypes.OSCTimetag);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateBlob()
        {
            byte[] blob = new byte[4];
            VerifyType<byte[]>(blob, OSCTypes.OSCBlob);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateChar()
        {
            VerifyType<char>('c', OSCTypes.Char);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateColor()
        {
            Color color = Color.Black;
            VerifyType<Color>(color, OSCTypes.Color);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateTrue()
        {
            VerifyType<bool>(true, OSCTypes.True);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateFalse()
        {
            VerifyType<bool>(false, OSCTypes.False);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateNil()
        {
            VerifyType<object>(null, OSCTypes.Nil);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateFloatInfinitum()
        {
            VerifyType<float>(float.PositiveInfinity, OSCTypes.Infinitum);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateDoubleInfinitum()
        {
            VerifyType<double>(double.PositiveInfinity, OSCTypes.Infinitum);
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void CanCreateArray()
        {
            List<OSCArgument> array = new List<OSCArgument>();
#pragma warning disable 612
            VerifyType<IEnumerable<OSCArgument>>(array, OSCTypes.Array);
#pragma warning restore 612
        }

        [TestMethod, TestCategory("OSCArgumentTest")]
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
            arg.Range = range;
            argOutHigh.Range = range;
            argOutLow.Range = range;

            Assert.IsTrue(OSCRange.ValidateType.In == arg.Validate());
            Assert.IsTrue(OSCRange.ValidateType.OutHigh == argOutHigh.Validate());
            Assert.IsTrue(OSCRange.ValidateType.OutLow == argOutLow.Validate());
        }

        [TestMethod, TestCategory("OSCArgumentTest")]
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

            argIn.Range = range;
            argOut.Range = range;

            Assert.IsTrue(OSCRange.ValidateType.In == argIn.Validate());
            Assert.IsTrue(OSCRange.ValidateType.Out == argOut.Validate());
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void HasUnit()
        {
            OSCArgument argument = new OSCArgument();
            Assert.IsTrue(argument.Unit == string.Empty);
            argument.Unit = "hz";
            Assert.AreEqual(argument.Unit, "hz");
        }

        [TestMethod, TestCategory("OSCArgument")]
        public void HasClipMode()
        {
            OSCArgument argument = new OSCArgument();
            Assert.IsTrue(argument.Unit == string.Empty);
            argument.ClipMode = OSCClipMode.High;
            Assert.AreEqual(argument.ClipMode, OSCClipMode.High);
        }

        [TestMethod, TestCategory("OSCArgumentTest")]
        public void ClipHighWorks()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            OSCArgument argHigh = OSCArgument.Create<int>(5);
            OSCArgument argLow = OSCArgument.Create<int>(1);
            range.High = argHigh;
            range.Low = argLow;
            arg.Range = range;
            arg.ClipMode = OSCClipMode.High;

            int inRange = 2;
            int high = 6;
            int low = 0;

            arg.Value = inRange;
            Assert.IsTrue(inRange == arg.Value);

            arg.Value = low;
            Assert.IsTrue(low == arg.Value);

            arg.Value = high;
            Assert.IsTrue(5 == arg.Value);
        }

        [TestMethod, TestCategory("OSCArgumentTest")]
        public void ClipLowWorks()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            OSCArgument argHigh = OSCArgument.Create<int>(5);
            OSCArgument argLow = OSCArgument.Create<int>(1);
            range.High = argHigh;
            range.Low = argLow;
            arg.Range = range;
            arg.ClipMode = OSCClipMode.Low;

            int inRange = 2;
            int high = 6;
            int low = 0;

            arg.Value = inRange;
            Assert.IsTrue(inRange == arg.Value);

            arg.Value = low;
            Assert.IsTrue(argLow.Value == arg.Value);

            arg.Value = high;
            Assert.IsTrue(high == arg.Value);
        }

        [TestMethod, TestCategory("OSCArgumentTest")]
        public void ClipBothWorks()
        {
            OSCRange range = new OSCRange();
            OSCArgument arg = new OSCArgument();
            OSCArgument argHigh = OSCArgument.Create<int>(5);
            OSCArgument argLow = OSCArgument.Create<int>(1);
            range.High = argHigh;
            range.Low = argLow;
            arg.Range = range;
            arg.ClipMode = OSCClipMode.Both;

            int inRange = 2;
            int high = 6;
            int low = 0;

            arg.Value = inRange;
            Assert.IsTrue(inRange == arg.Value);

            arg.Value = low;
            Assert.IsTrue(argLow.Value == arg.Value);

            arg.Value = high;
            Assert.IsTrue(argHigh.Value == arg.Value);
        }

        private void VerifyType<T>(T val, OSCTypes expected)
        {
            OSCArgument argument = OSCArgument.Create<T>(val);
            Assert.IsTrue(val == null || argument.Value is T);
            Assert.IsTrue(val == null || argument.Value.Equals(val));
            Assert.IsTrue(argument.Type == expected);
        }
    }
}

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
using System.Collections.Generic;

namespace OSCEndpointTest
{
    [TestClass]
    public class OSCMethodTest
    {
        [TestMethod, TestCategory("OSCMethod")]
        public void HasName()
        {
            OSCMethod method = new OSCMethod();
            Assert.IsTrue(method.Name is string);
            Assert.IsTrue(method.Name == string.Empty);
        }

        [TestMethod, TestCategory("OSCMethod")]
        public void HasArgumentList()
        {
            OSCMethod method = new OSCMethod();
            Assert.IsTrue(method.Arguments is List<OSCArgument>);
        }

        [TestMethod, TestCategory("OSCMethod")]
        public void CanInvoke()
        {
            OSCMethod method = new OSCMethod();
            List<OSCArgument> arguments = new List<OSCArgument>();
            Assert.IsTrue(method.Invoke(arguments));
        }

        [TestMethod, TestCategory("OSCMethod")]
        public void GetsCallback()
        {
            OSCMethod method = new OSCMethod();
            flag = false;
            method.OnInvoke += method_OnInvoke;
            List<OSCArgument> arguments = new List<OSCArgument>();
            Assert.IsTrue(method.Invoke(arguments));
            Assert.IsTrue(flag);
        }

        [TestMethod, TestCategory("OSCMethod")]
        public void ReturnsCallbackValue()
        {
            OSCMethod method = new OSCMethod();
            flag = false;
            method.OnInvoke += method_OnInvoke1;
            List<OSCArgument> arguments = new List<OSCArgument>();
            Assert.IsFalse(method.Invoke(arguments));
            flag = true;
            Assert.IsTrue(method.Invoke(arguments));
        }

        [TestMethod, TestCategory("OSCMethod")]
        public void PassesArguments()
        {
            OSCMethod method = new OSCMethod();
            flag = false;
            method.OnInvoke += method_OnInvoke2;
            List<OSCArgument> arguments = new List<OSCArgument>();
            Assert.IsTrue(method.Invoke(arguments));
            Assert.AreEqual(arguments, args);
        }

        [TestMethod, TestCategory("OSCMethod")]
        public void HasParent()
        {
            OSCMethod method = new OSCMethod();
            Assert.IsTrue(method.Parent == null);
        }

        [TestMethod, TestCategory("OSCMethod")]
        public void CanCreateChildMethod()
        {
            OSCContainer containerParent = new OSCContainer();
            OSCMethod containerChild = new OSCMethod("foo", containerParent);
            Assert.IsTrue(containerChild.Parent is OSCContainer);
            Assert.IsTrue(containerChild.Parent == containerParent);
        }

        bool method_OnInvoke(object sender, MethodEventArgs args)
        {
            flag = true;
            return flag;
        }

        bool method_OnInvoke1(object sender, MethodEventArgs args)
        {
            return flag;
        }

        bool method_OnInvoke2(object sender, MethodEventArgs args)
        {
            this.args = args.OSCArgs;
            return true;
        }

        bool flag = false;
        List<OSCArgument> args;
    }
}

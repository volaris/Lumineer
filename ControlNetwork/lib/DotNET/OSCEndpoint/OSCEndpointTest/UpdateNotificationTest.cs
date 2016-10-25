/* 
 * Copyright (c) 2016 Lane Haury
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

namespace OSCEndpointTest
{
    [TestClass]
    public class UpateNotificationTest
    {
        [TestMethod, TestCategory("Update Notifications")]
        public void OSCNodeCreationParentChildrenNotification()
        {
            OSCContainer container1 = new OSCContainer();
            bool received = false;

            container1.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(sender, container1);
                Assert.AreEqual("Children", e.PropertyName);
                received = true;
            };

            OSCContainer container2 = new OSCContainer("test", container1);
            if (!received) Assert.Fail("No Event");
        }

        [TestMethod, TestCategory("Update Notifications")]
        public void OSCNodeUpdateDescriptionNotification()
        {
            OSCContainer container1 = new OSCContainer();
            bool received = false;

            OSCNode container2 = new OSCNode("test", container1);

            container2.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(sender, container2);
                Assert.AreEqual("Description", e.PropertyName);
                received = true;
            };

            container2.Description = "test2";

            if (!received) Assert.Fail("No Event");
        }

        [TestMethod, TestCategory("Update Notifications")]
        public void OSCNodeUpdateNameNotification()
        {
            OSCContainer container1 = new OSCContainer();
            bool received = false;

            OSCNode container2 = new OSCNode("test", container1);

            container2.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(sender, container2);
                Assert.AreEqual("Name", e.PropertyName);
                received = true;
            };

            container2.Name = "test2";

            if (!received) Assert.Fail("No Event");
        }

        [TestMethod, TestCategory("Update Notifications")]
        public void OSCNodeUpdateParentNotification()
        {
            OSCContainer container1 = new OSCContainer();
            bool received = false;

            OSCNode container2 = new OSCNode("test", container1);

            container2.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(sender, container2);
                Assert.AreEqual("Parent", e.PropertyName);
                received = true;
            };

            container2.Parent = container1;

            if (!received) Assert.Fail("No Event");
        }

        [TestMethod, TestCategory("Update Notifications")]
        public void OSCMethodArgumentValueUpdateNotification()
        {
            OSCContainer container1 = new OSCContainer();
            bool received = false;

            OSCMethod method = new OSCMethod("test", container1, new System.Collections.Generic.List<OSCArgument>());
            OSCArgument arg = new OSCArgument();
            arg.Type = OSCTypes.Int32;
            arg.Value = 0;

            method.AddArgument(arg);

            method.PropertyChanged += (sender, e) =>
            {
                Assert.AreEqual(sender, method);
                received = true;
            };

            arg.Value = 3;

            if (!received) Assert.Fail("No Event");
        }
    }
}

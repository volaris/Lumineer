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
using System.IO;
using System.Net;
using System.Net.Sockets;
using OSCQuery;
using OSCEndpoint;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OSCQueryTests
{
    [TestClass]
    public class OSCQueryServerTest
    {
        [TestMethod, TestCategory("Server Setup")]
        public void CreateAndStartServer()
        {
            OSCQueryServer server = new OSCQueryServer();

            server.Start();

            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect("127.0.0.1", 8080);

            Assert.IsTrue(sock.Connected);

            server.Stop();
        }

        [TestMethod, TestCategory("Server Setup")]
        public void CustomPort()
        {
            OSCQueryServer server = new OSCQueryServer(8181);

            server.Start();

            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect("127.0.0.1", 8181);

            Assert.IsTrue(sock.Connected);

            server.Stop();
        }

        [TestMethod, TestCategory("Server Shutdown")]
        public void Stop()
        {
            OSCQueryServer server = new OSCQueryServer(8282);

            server.Start();

            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Socket sock1 = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect("127.0.0.1", 8282);

            Assert.IsTrue(sock.Connected);

            server.Stop();

            sock.Close();

            try
            {
                sock1.Connect("127.0.0.1", 8282);
            }
            catch
            {

            }

            Assert.IsFalse(sock1.Connected);
        }

        [TestMethod, TestCategory("Query")]
        public void QueryRoot()
        {
            OSCEndpoint.OSCEndpoint root = getEndpoint();
            OSCQueryServer server = new OSCQueryServer(8383, root);

            server.Start();

            ClientWebSocket socket = new ClientWebSocket();

            // need this for the registered prefixes for client.DownloadString
            System.Net.WebClient client = new WebClient();

            string data = client.DownloadString(new Uri(@"http://127.0.0.1:8383/"));

            string json = JsonConvert.SerializeObject(root.Root, Formatting.Indented);

            Assert.AreEqual(json, data);

            server.Stop();
        }

        [TestMethod, TestCategory("Query")]
        public void QueryChild()
        {
            OSCEndpoint.OSCEndpoint root = getEndpoint();
            OSCQueryServer server = new OSCQueryServer(8484, root);

            server.Start();

            ClientWebSocket socket = new ClientWebSocket();

            // need this for the registered prefixes for client.DownloadString
            System.Net.WebClient client = new WebClient();

            string data = client.DownloadString(new Uri(@"http://127.0.0.1:8484/foo"));

            string json = JsonConvert.SerializeObject(root.Root.Children["foo"], Formatting.Indented);

            Assert.AreEqual(json, data);

            server.Stop();
        }

        [TestMethod, TestCategory("Query")]
        public void QueryMethod()
        {
            OSCEndpoint.OSCEndpoint root = getEndpoint();
            OSCQueryServer server = new OSCQueryServer(8484, root);

            server.Start();

            ClientWebSocket socket = new ClientWebSocket();

            // need this for the registered prefixes for client.DownloadString
            System.Net.WebClient client = new WebClient();

            string data = client.DownloadString(new Uri(@"http://127.0.0.1:8484/barfoo"));

            string json = JsonConvert.SerializeObject(root.Root.Children["barfoo"], Formatting.Indented);

            Assert.AreEqual(json, data);

            server.Stop();
        }

        [TestMethod, TestCategory("Query")]
        public void QueryMethodValue()
        {
            OSCEndpoint.OSCEndpoint root = getEndpoint();
            OSCQueryServer server = new OSCQueryServer(8484, root);

            server.Start();

            ClientWebSocket socket = new ClientWebSocket();

            // need this for the registered prefixes for client.DownloadString
            System.Net.WebClient client = new WebClient();

            string data = client.DownloadString(new Uri(@"http://127.0.0.1:8484/barfoo?VALUE"));

            MethodArgumentList list = new MethodArgumentList();

            List<object> values = new List<object>();

            foreach (OSCArgument arg in (root.Root.Children["barfoo"] as OSCMethod).Arguments)
            {
                values.Add(arg.Value);
            }

            list.VALUE = values;

            string json = JsonConvert.SerializeObject(list, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            Assert.AreEqual(json, data);

            server.Stop();
        }

        private OSCEndpoint.OSCEndpoint getEndpoint()
        {
            OSCContainer container1 = new OSCContainer();
            OSCEndpoint.OSCEndpoint root = new OSCEndpoint.OSCEndpoint(container1);
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

            return root;
        }
    }
}

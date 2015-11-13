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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using OSCEndpoint;
using System.IO;
using Newtonsoft.Json;

namespace OSCQuery
{
    public class OSCQueryServer
    {
        private OSCEndpoint.OSCEndpoint endpoint;

        public HttpServer HttpServer { get; set; }
        public OSCEndpoint.OSCEndpoint OSCEndpoint { get { return endpoint; } }

        public OSCQueryServer() : this(8080, null)
        {
        }
        
        public OSCQueryServer(int port) : this(port, null)
        {

        }

        public OSCQueryServer(int port, OSCEndpoint.OSCEndpoint endpoint)
        {
            HttpServer = new HttpServer(port);
            HttpServer.OnGet += HttpServer_OnGet;
            this.endpoint = endpoint;
        }

        void HttpServer_OnGet(object sender, HttpRequestEventArgs e)
        {
            OSCNode node = endpoint.Root;
            string url = e.Request.Url.AbsolutePath;
            if (url.Length > 1)
            {
                string[] nodes = url.Split('/');
                foreach (string nodeName in nodes)
                {
                    if (nodeName.Length > 0 && node != null)
                    {
                        if (node is OSCContainer && (node as OSCContainer).Children.ContainsKey(nodeName))
                        {
                            node = (node as OSCContainer).Children[nodeName];
                        }
                        else
                        {
                            node = null;
                        }
                    }
                }
            }
            if(node != null)
            {
                if (e.Request.Url.Query != string.Empty)
                {
                    if(e.Request.Url.Query.Substring(1).Equals("VALUE") && node is OSCMethod)
                    {
                        MethodArgumentList list = new MethodArgumentList();

                        List<object> values = new List<object>();

                        foreach (OSCArgument arg in (node as OSCMethod).Arguments)
                        {
                            values.Add(arg.Value);
                        }

                        list.VALUE = values;

                        string json = JsonConvert.SerializeObject(list, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        e.Response.ContentType = "text/json";
                        e.Response.ContentEncoding = Encoding.UTF8;
                        e.Response.WriteContent(Encoding.UTF8.GetBytes(json));
                    }
                    else
                    {
                        e.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    string json = JsonConvert.SerializeObject(node, Formatting.Indented);
                    e.Response.ContentType = "text/json";
                    e.Response.ContentEncoding = Encoding.UTF8;
                    e.Response.WriteContent(Encoding.UTF8.GetBytes(json));
                }
            }
            else
            {
                e.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }

        public void Start()
        {
            HttpServer.Start();
        }

        public void Stop()
        {
            HttpServer.Stop();
        }
    }
}

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
using WebSocketSharp.Server;
using OSCEndpoint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OSCQuery
{
    public class OSCQueryUpdateService : WebSocketBehavior
    {
        OSCEndpoint.OSCEndpoint endpoint;
        List<string> paths = new List<string>();

        public OSCQueryUpdateService()
        {
        }

        public OSCQueryUpdateService(OSCEndpoint.OSCEndpoint endpoint)
        {
            this.endpoint = endpoint;
            subscribeToAllNotifications(this.endpoint.Root);
            this.endpoint.Root.PropertyChanged += child_PropertyChanged;
        }

        private void subscribeToAllNotifications(OSCEndpoint.OSCContainer node)
        {
            foreach(OSCNode child in node.Children.Values)
            {
                if(child is OSCContainer)
                {
                    subscribeToAllNotifications((OSCContainer)child);
                }
                child.PropertyChanged += child_PropertyChanged;
            }
        }

        void child_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(paths.Count > 0)
            {
                foreach (string path in paths)
                {
                    if (((OSCNode)sender).FullPath.StartsWith(path))
                    {
                        this.Send("{\"path\": \"" + ((OSCNode)sender).FullPath + "\"}");
                    }
                }
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if(e.IsText)
            {
                JObject obj = (JObject)JsonConvert.DeserializeObject(e.Data);
                bool listen = obj["listen"].Value<bool>();
                string path = obj["path"].Value<string>();
                if (listen)
                {
                    if (!paths.Contains(path))
                    {
                        paths.Add(path);
                    }
                }
                else
                {
                    paths.Remove(path);
                }
            }
        }
    }
}

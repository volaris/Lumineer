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
using Newtonsoft.Json;
using System.ComponentModel;

namespace OSCEndpoint
{
    public class OSCNode : INotifyPropertyChanged
    {
        private string description;
        private string name;
        private OSCContainer parent;

        public OSCNode()
        {
            this.Description = string.Empty;
            this.Name = string.Empty;
        }

        public OSCNode(string name, OSCContainer containerParent) : this()
        {
            this.Parent = containerParent;
            if (containerParent != null)
            {
                this.Name = name;
                containerParent.Children.Add(name, this);
                OnPropertyChanged("Children", containerParent);
            }
        }

        [JsonProperty("DESCRIPTION")]
        public string Description
        {
            get { return this.description; }
            set
            {
                this.description = value;
                OnPropertyChanged("Description");
            }
        }

        [JsonIgnore]
        public string Name 
        {
            get { return this.name; }
            set
            {
                this.name = value;
                OnPropertyChanged("Name");
            }
        }

        [JsonIgnore]
        public OSCContainer Parent 
        {
            get { return parent; }
            set
            {
                this.parent = value;
                OnPropertyChanged("Parent");
            }
        }

        [JsonProperty("FULL_PATH")]
        public string FullPath
        {
            get
            {
                string path = string.Empty;
                OSCNode current = this;
                path = "/" + current.Name;
                while (current.Parent != null && current.Parent.Name != String.Empty)
                {
                    current = current.Parent;
                    path = "/" + current.Name + path;
                }
                return path;
            }
        }

        protected void OnPropertyChanged(string name, object source = null)
        {
            if (source == null)
            {
                source = this;
            }
            PropertyChangedEventHandler handler = source == Parent ? Parent.PropertyChanged : PropertyChanged;
            if(handler != null)
            {
                handler(source, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

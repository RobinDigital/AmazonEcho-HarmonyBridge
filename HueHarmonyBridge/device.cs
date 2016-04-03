using System;
using System.Collections.Generic;
using System.Text;

namespace HueHarmonyBridge
{
    public class device
    {
        public String Name { get; set; }
        public Boolean State { get; set; }
        public device(String name, Boolean state)
        {
            Name = name;
            State = state;
        }
    }
}

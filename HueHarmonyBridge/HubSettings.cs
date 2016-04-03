using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace HueHarmonyBridge
{
    public class HubSettings
    {

        private static HubSettings instance;
        public System.Collections.Specialized.StringCollection AppKeys { get; set; }
        public System.Collections.Specialized.StringCollection AppNames { get; set; }
        public String HarmonyHubIP { get; set; }
        public String HarmonySession { get; set; }
        public String HarmonyUser { get; set; }
        public String HarmonyPass { get; set; }
        public String IPToUse { get; set; }

        private HubSettings() { }
        private static void Load() {
            
            
                XmlSerializer xs = new XmlSerializer(typeof(HubSettings));
                try
                {
                    using (var sr = new StreamReader(System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ) +"/settings.xml"))
                    {
                        instance = (HubSettings)xs.Deserialize(sr);
                    }
                }
                catch
                {
                    instance = new HubSettings();
                    instance.HarmonyUser = "user@email.com";
                    instance.HarmonyPass = "xxxxxx";
                    instance.IPToUse = "192.168.0.2";
                    instance.Save();
                }
                
            
        }
        public void Save()
        {
            XmlSerializer xs = new XmlSerializer(typeof(HubSettings));
            TextWriter tw = new StreamWriter(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/settings.xml");
            xs.Serialize(tw, instance);
            tw.Close();
            //And code to load garage from file:
        }
   public static HubSettings Instance
   {
      get 
      {
         if (instance == null)
         {
            Load();
         }
         return instance;
      }
   }




    }
}

using System;
using System.Collections.Generic;
using System.IO;

using System.Text;
using System.Net;
using HueHarmonyBridge.Properties;
using System.Data;

namespace HueHarmonyBridge
{
    class MyHttpServer : SimpleHttp.HttpServer
    {
        private String server_ip="";
        private int server_port = 0;
        private Dictionary<int, device> devices =null;
        public MyHttpServer(String _server_ip, int _server_port, Dictionary<int, device> _devices)
            : base(_server_port)
        {
            server_ip = _server_ip;
            server_port = _server_port;
            if (HubSettings.Instance.AppNames == null) HubSettings.Instance.AppNames = new System.Collections.Specialized.StringCollection();
            if (HubSettings.Instance.AppKeys == null) HubSettings.Instance.AppKeys = new System.Collections.Specialized.StringCollection();
            devices = _devices;
        }
        public override void handleGETRequest(SimpleHttp.HttpProcessor p)
        {
            Console.WriteLine("HTTP server responding to GET " + p.request_url.AbsolutePath.ToString() + " from " + p.RemoteIPAddress.ToString());
            if (p.request_url.ToString().Contains("description.xml"))
            {
                writeDescription(p);
            }
            else if (p.request_url.AbsolutePath.ToLower().StartsWith("/api/")&&p.request_url.AbsolutePath.ToLower().EndsWith("/lights"))
            {
                userKey(p.request_url.AbsolutePath.ToLower().Substring(5, p.request_url.AbsolutePath.ToLower().Length - 12));
                p.writeSuccess("application/json");
                String response="{";
                String seperator = "";
                foreach (KeyValuePair<int,device> item in devices)
                {
                    response+=seperator+"\""+item.Key.ToString()+"\":" + createDevice(item.Value.Name, item.Value.State) ;
                    seperator = ",";
                }
                p.outputStream.WriteLine(response+"}");
                
            }
            else if (p.request_url.AbsolutePath.ToLower().StartsWith("/api/") && p.request_url.AbsolutePath.ToLower().Contains("/lights/") && !p.request_url.AbsolutePath.ToLower().EndsWith("/state"))
            {
                userKey(p.request_url.AbsolutePath.ToLower().Substring(5, p.request_url.AbsolutePath.ToLower().IndexOf("/lights") - 5));
                int lightID = 0;
                int.TryParse(p.request_url.AbsolutePath.ToLower().Substring(p.request_url.AbsolutePath.ToLower().IndexOf("/lights/") + 8), out lightID);
                p.writeSuccess("application/json");
                p.outputStream.WriteLine( createDevice(devices[lightID].Name, devices[lightID].State) );
            }else if (p.request_url.AbsolutePath.ToLower().StartsWith("/api/"))
            {
                string key =userKey(p.request_url.AbsolutePath.ToLower().Substring(5));

                
                    p.writeSuccess("application/json");
                    p.outputStream.WriteLine("{\"success\":{\"username\":\"" + key + "\"}}");
                
            }            
            else
            {

                p.writeSuccess();
                p.outputStream.WriteLine("<html><body><h1>test server</h1>");
                p.outputStream.WriteLine("Current Time: " + DateTime.Now.ToString());
                p.outputStream.WriteLine("url : {0}", p.request_url);
                p.outputStream.WriteLine("</body></html>");
            }
        }
        public override void handlePUTRequest(SimpleHttp.HttpProcessor p, StreamReader inputData)
        {
            Console.WriteLine("HTTP server responding to PUT " + p.request_url.AbsolutePath.ToString() + " from " + p.RemoteIPAddress.ToString());
            string data = p.RawPostParams["TEXT"];
            
            if (p.request_url.AbsolutePath.ToLower().StartsWith("/api/") && p.request_url.AbsolutePath.ToLower().EndsWith("/state"))
            {
                String key = p.request_url.AbsolutePath.ToLower().Substring(5, p.request_url.AbsolutePath.ToLower().IndexOf("/", 6)-5);
                int lightID = 0;
                int.TryParse(p.request_url.AbsolutePath.ToLower().Substring(p.request_url.AbsolutePath.ToLower().IndexOf("/lights/") + 8, p.request_url.AbsolutePath.ToLower().IndexOf("/state") - p.request_url.AbsolutePath.ToLower().IndexOf("/lights/") - 8),out lightID);
                userKey(key);
                p.writeSuccess("application/json");
                HClient harmonyClient = null;
                switch (data)
                {
                    case "{\"on\": true}":
                        devices[lightID].State = true;
                        Console.WriteLine("HARMONY - Starting activity " + devices[lightID].Name);
                        harmonyClient = new HClient(HubSettings.Instance.HarmonyHubIP, HubSettings.Instance.HarmonyUser, HubSettings.Instance.HarmonyPass);
                        harmonyClient.StartActivity(lightID);
                        p.outputStream.WriteLine("{\"success\":{\"/lights/"+lightID+"/state/on\":true}}");
                        break;
                    case "{\"on\": false}":
                        devices[lightID].State = false;
                        Console.WriteLine("HARMONY - Ending activity " + devices[lightID].Name);
                        harmonyClient = new HClient(HubSettings.Instance.HarmonyHubIP, HubSettings.Instance.HarmonyUser, HubSettings.Instance.HarmonyPass);
                        harmonyClient.EndActivity(lightID);
                        p.outputStream.WriteLine("{\"success\":{\"/lights/"+lightID+"/state/on\":false}}");
                        break;
                    default:
                      
                        break;
                }

            }
        }
        public override void handlePOSTRequest(SimpleHttp.HttpProcessor p, StreamReader inputData)
        {
            string data = inputData.ReadToEnd();
            Console.WriteLine("HTTP server responding to POST " + p.request_url.AbsolutePath.ToString() + " from " + p.RemoteIPAddress.ToString());
            //new StringContent(body, Encoding.UTF8, "application/json")
            if (p.request_url.AbsolutePath.ToLower() == "/api")
            {


                string name = "";
                try
                {
                    name = JsonParsor.JsonParse(data).Rows[0].ItemArray[0].ToString();
                    string key = userKey(name);

                    p.writeSuccess("application/json");
                    p.outputStream.WriteLine("[{\"success\":{\"username\":\"" + key + "\"}}]");
                }
                catch
                {
                    p.writeFailure();
                }
            }            
            else
            {
                p.outputStream.WriteLine("<html><body><h1>test server</h1>");
                p.outputStream.WriteLine("Current Time: " + DateTime.Now.ToString());
                p.outputStream.WriteLine("url : {0}", p.request_url);
                p.outputStream.WriteLine("</body></html>");
            }
        }
        private String userKey(String name)
        {
            String key = "";
            if (HubSettings.Instance.AppNames.Contains(name))
            {
                key = HubSettings.Instance.AppKeys[HubSettings.Instance.AppNames.IndexOf(name)];
            }
            else
            {
                key = Guid.NewGuid().ToString().Replace("-", "");
                HubSettings.Instance.AppNames.Add(name);
                HubSettings.Instance.AppKeys.Add(key);
                HubSettings.Instance.Save();
            }
            return key;
        }
        private String createDevice(String name,Boolean state)
        {
            String device = " {" +
                        "\"state\": {" +
                        "    \"on\": "+(state?"true":"false")+"," +
        "    \"bri\": " + (state ? "254" : "0") + "," +
        "    \"hue\": 0," +
        "    \"sat\": 0," +
        "    \"xy\": [0,0]," +
        "    \"ct\": 0," +
        "    \"alert\": \"none\"," +
        "    \"effect\": \"none\"," +
        "    \"colormode\": \"xy\"," +
        "    \"reachable\": true" +
        "}," +
        "\"type\": \"Extended color light\"," +
        "\"name\": \""+name+"\"," +
        "\"modelid\": \"LCT001\"," +
        "\"swversion\": \"66009461\"," +
        "\"pointsymbol\": {" +
         "   \"1\": \"none\"," +
         "   \"2\": \"none\"," +
         "   \"3\": \"none\"," +
         "   \"4\": \"none\"," +
         "   \"5\": \"none\"," +
         "   \"6\": \"none\"," +
         "   \"7\": \"none\"," +
         "   \"8\": \"none\"" +
        "}" +
        "}";
            return device;
        }
      
        public override void stopServer()
        {
            //
        }
        private void writeDescription(SimpleHttp.HttpProcessor p)
        {
            p.writeSuccess("application/xml");
            p.outputStream.WriteLine("<?xml version=\"1.0\"?>");
            p.outputStream.WriteLine("<root xmlns=\"urn:schemas-upnp-org:device-1-0\">");
            p.outputStream.WriteLine("<specVersion>");
            p.outputStream.WriteLine("<major>1</major>");
            p.outputStream.WriteLine("<minor>0</minor>");
            p.outputStream.WriteLine("</specVersion>");
            p.outputStream.WriteLine("<URLBase>http://" + server_ip + ":" + server_port.ToString() + "/</URLBase>");
            p.outputStream.WriteLine("<device>");
            p.outputStream.WriteLine("<deviceType>urn:schemas-upnp-org:device:Basic:1</deviceType>");
            p.outputStream.WriteLine("<friendlyName>Amazon-Echo-Harmony bridge</friendlyName>");
            p.outputStream.WriteLine("<manufacturer>Royal Philips Electronics</manufacturer>");
            p.outputStream.WriteLine("<manufacturerURL>http://www.philips.com</manufacturerURL>");
            p.outputStream.WriteLine("<modelDescription>Philips hue Personal Wireless Lighting</modelDescription>");
            p.outputStream.WriteLine("<modelName>Philips hue bridge 2012</modelName>");
            p.outputStream.WriteLine("<modelNumber>929000226503</modelNumber>");
            p.outputStream.WriteLine("<modelURL>http://www.meethue.com</modelURL>");
            p.outputStream.WriteLine("<serialNumber>001788102201</serialNumber>");
            p.outputStream.WriteLine("<UDN>uuid:2f402f80-da50-11e1-9b23-001788102201</UDN>");
            p.outputStream.WriteLine("<serviceList>");
            p.outputStream.WriteLine("<service>");
            p.outputStream.WriteLine("<serviceType>(null)</serviceType>");
            p.outputStream.WriteLine("<serviceId>(null)</serviceId>");
            p.outputStream.WriteLine("<controlURL>(null)</controlURL>");
            p.outputStream.WriteLine("<eventSubURL>(null)</eventSubURL>");
            p.outputStream.WriteLine("<SCPDURL>(null)</SCPDURL>");
            p.outputStream.WriteLine("</service>");
            p.outputStream.WriteLine("</serviceList>");
            p.outputStream.WriteLine("<presentationURL>index.html</presentationURL>");
            p.outputStream.WriteLine("<iconList>");
            p.outputStream.WriteLine("<icon>");
            p.outputStream.WriteLine("<mimetype>image/png</mimetype>");
            p.outputStream.WriteLine("<height>48</height>");
            p.outputStream.WriteLine("<width>48</width>");
            p.outputStream.WriteLine("<depth>24</depth>");
            p.outputStream.WriteLine("<url>hue_logo_0.png</url>");
            p.outputStream.WriteLine("</icon>");
            p.outputStream.WriteLine("<icon>");
            p.outputStream.WriteLine("<mimetype>image/png</mimetype>");
            p.outputStream.WriteLine("<height>120</height>");
            p.outputStream.WriteLine("<width>120</width>");
            p.outputStream.WriteLine("<depth>24</depth>");
            p.outputStream.WriteLine("<url>hue_logo_3.png</url>");
            p.outputStream.WriteLine("</icon>");
            p.outputStream.WriteLine("</iconList>");
            p.outputStream.WriteLine("</device>");
            p.outputStream.WriteLine("</root>");
        }
    }
}

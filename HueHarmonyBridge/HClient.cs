using HarmonyHub;
using HueHarmonyBridge.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueHarmonyBridge
{
    public class HClient
    {
        private String harmonyIP;
        private String username;
        private String password;
        private int port = 5222;
        public HClient(String ip, String user, String pass)
        {            
            harmonyIP = ip;
            username = user;
            password = pass;
            if (!String.IsNullOrEmpty(HubSettings.Instance.HarmonySession))
            {
                Console.WriteLine("HARMONY - Reusing token: {0}", HubSettings.Instance.HarmonySession);
            }
            else
            {
                HubSettings.Instance.HarmonySession = LoginToLogitech(username, password, harmonyIP, port);
                HubSettings.Instance.Save();
            }
        }
         public  string LoginToLogitech(string email, string password, string ipAddress, int harmonyPort)
        {
            string userAuthToken = HarmonyLogin.GetUserAuthToken(email, password);
            if (string.IsNullOrEmpty(userAuthToken))
            {
                throw new Exception("HARMONY - Could not get token from Logitech server.");
            }
            

            var authentication = new HarmonyAuthenticationClient(ipAddress, harmonyPort);

            string sessionToken = authentication.SwapAuthToken(userAuthToken);
            if (string.IsNullOrEmpty(sessionToken))
            {
                throw new Exception("Could not swap token on Harmony Hub.");
            }


            Console.WriteLine("HARMONY - Date Time : {0}", DateTime.Now);
            Console.WriteLine("HARMONY - User Token: {0}", userAuthToken);
            Console.WriteLine("HARMONY - Sess Token: {0}", sessionToken);

            return sessionToken;
        }
         public Dictionary<int, device> findActivities()
         {
             Dictionary<int, device> devices = new Dictionary<int, device>();
             HarmonyConfigResult harmonyConfig = null;

             HarmonyClient client = null;

             client = new HarmonyClient(harmonyIP, port, HubSettings.Instance.HarmonySession);
             client.GetConfig();

             while (string.IsNullOrEmpty(client.Config)) { }

             harmonyConfig = HarmonyConfigResult.Create(client.Config);
             harmonyConfig.activity.Sort();
             foreach (var activity in harmonyConfig.activity)
             {
                 
                 int id=0;
                 int.TryParse(activity.id,out id);
                 if (id != -1)
                 {
                     Console.WriteLine("HARMONY - Activity {0}:{1}", activity.id, activity.label);
                     devices.Add(id, new device(activity.label, false));
                 }
             }             
             return devices;
         }
         public void StartActivity(int id)
         {
             try
             {
                 HarmonyClient client = null;
                 client = new HarmonyClient(harmonyIP, port, HubSettings.Instance.HarmonySession);
                 client.StartActivity(id.ToString());
                 
             }
             catch { }
         }
         public void EndActivity(int id)
         {
             try
             {
                 HarmonyClient client = null;
                 client = new HarmonyClient(harmonyIP, port, HubSettings.Instance.HarmonySession);
                 client.EndActivity(id.ToString());
             }
             catch { }
         }
    }
}

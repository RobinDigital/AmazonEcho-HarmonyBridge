using HueHarmonyBridge.Properties;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;


namespace HueHarmonyBridge
{
    class Program
    {
        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain,SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = Validator; //workaround for MONO Cert store

            if (HubSettings.Instance.HarmonyUser=="user@email.com")
            {
                Console.WriteLine(DateTime.Now.ToString()+" Please fill in the settings.xml file with a value for HarmonyUser, HarmonyPass and IPToUse");
            }
            else
            {
                String server_ip =HubSettings.Instance.IPToUse;
                int server_port = 5999;
                NetworkInterface serverInterface = findInterface(server_ip);
                String harmonyHubIP = findHarmonyHub(serverInterface);
                Console.WriteLine(DateTime.Now.ToString()+" HARMONY - Loading activities");

                HClient harmonyClient = new HClient(harmonyHubIP,HubSettings.Instance.HarmonyUser,HubSettings.Instance.HarmonyPass);
                Dictionary<int, device> devices = harmonyClient.findActivities();
                if (devices != null)
                {
                    Console.WriteLine(DateTime.Now.ToString()+" HARMONY - Connected");

                    Console.WriteLine(DateTime.Now.ToString()+" Starting UPNP server for " + server_ip);
                    Mono.Ssdp.Server discoveryServer = new Mono.Ssdp.Server("http://" + server_ip + ":" + server_port.ToString() + "/description.xml", serverInterface,server_ip);
                    discoveryServer.Announce("upnp:rootdevice", "HueHarmony", "http://" + server_ip + ":" + server_port.ToString() + "/description.xml", true);
                    discoveryServer.Announce("urn:schemas-upnp-org:device:basic:1", "HueHarmony2", "http://" + server_ip + ":" + server_port.ToString() + "/description.xml", true);
                    Console.WriteLine(DateTime.Now.ToString()+" Started UPNP server for " + server_ip);

                    Console.WriteLine(DateTime.Now.ToString()+" Starting HTTP server for " + server_ip + ":" + server_port);
                    MyHttpServer httpServer = new MyHttpServer(server_ip, server_port, devices);
                    Thread threadHttp = new Thread(new ThreadStart(httpServer.Start));
                    threadHttp.Start();
                    Console.WriteLine(DateTime.Now.ToString()+" Started HTTP server for " + server_ip + ":" + server_port);
                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString()+" HARMONY - No activities ... Quiting");
                }
            }
        }
        private static NetworkInterface findInterface(String server_ip)
        {
            // join multicast group on all available network interfaces
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface interfaceToUse = null;
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {

                if ((!networkInterface.Supports(NetworkInterfaceComponent.IPv4)) ||
                    (networkInterface.OperationalStatus != OperationalStatus.Up))
                {
                    continue;
                }

                IPInterfaceProperties adapterProperties = networkInterface.GetIPProperties();
                UnicastIPAddressInformationCollection unicastIPAddresses = adapterProperties.UnicastAddresses;
                IPAddress ipAddress = null;

                foreach (UnicastIPAddressInformation unicastIPAddress in unicastIPAddresses)
                {
                    if (unicastIPAddress.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    ipAddress = unicastIPAddress.Address;
                    break;
                }

                if (ipAddress == null)
                {
                    continue;
                }
                if (ipAddress.ToString() == server_ip)
                {
                    interfaceToUse = networkInterface;
                    break;
                }

                //UDPSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(Target_IP, ipAddress));
                //SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(Protocol.IPAddress, ipAddress));
            }
            return interfaceToUse;
        }
        private static String findHarmonyHub(NetworkInterface netwIntrf)
        {
            String foundAddress = "";
            int wantedPort = 5222;
            if (!String.IsNullOrEmpty(HubSettings.Instance.HarmonyHubIP))
            {
                Console.WriteLine("Testing previous Harmony HUB IP");
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    //try to connect
                    //sock.Connect(ipCandidate, wantedPort);
                    // Connect using a timeout (5 seconds)

                    IAsyncResult result = sock.BeginConnect(HubSettings.Instance.HarmonyHubIP, wantedPort, null, null);

                    bool success = result.AsyncWaitHandle.WaitOne(1000, true);


                    if (sock.Connected == true)  // if succesful => something is listening on this port
                    {
                        foundAddress =HubSettings.Instance.HarmonyHubIP;
                        Console.WriteLine("\tFound Harmony HUB at " +HubSettings.Instance.HarmonyHubIP);

                    }
                    sock.Close();

                }
                catch (SocketException ex)
                {
                    //TODO: if you want, do smth here
                    Console.WriteLine("\tDIDN'T work at " +HubSettings.Instance.HarmonyHubIP);
                }
                finally
                {
                    sock.Close();
                }
            }
            if (String.IsNullOrEmpty(foundAddress))
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //this is the port you want

                byte[] msg = Encoding.ASCII.GetBytes("type msg here");
                Console.WriteLine("Searching Harmony HUB - Network scan");


                Console.WriteLine("Interface name: " + netwIntrf.Name);

                Console.WriteLine("Inteface working: {0}", netwIntrf.OperationalStatus == OperationalStatus.Up);

                //if the current interface doesn't have an IP, skip it
                if (!(netwIntrf.GetIPProperties().GatewayAddresses.Count > 0))
                {
                    return "";
                }

                //Console.WriteLine("IP Address(es):");

                //get current IP Address(es)
                foreach (UnicastIPAddressInformation uniIpInfo in netwIntrf.GetIPProperties().UnicastAddresses)
                {
                    if (uniIpInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        //get the subnet mask and the IP address as bytes
                        byte[] subnetMask = new byte[]{255, 255, 255, 0};//uniIpInfo.IPv4Mask.GetAddressBytes();
                        byte[] ipAddr = uniIpInfo.Address.GetAddressBytes();

                        // we reverse the byte-array if we are dealing with littl endian.
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(subnetMask);
                            Array.Reverse(ipAddr);
                        }

                        //we convert the subnet mask as uint (just for didactic purposes (to check everything is ok now and next - use thecalculator in programmer mode)
                        uint maskAsInt = BitConverter.ToUInt32(subnetMask, 0);
                        //Console.WriteLine("\t subnet={0}", Convert.ToString(maskAsInt, 2));

                        //we convert the ip addres as uint (just for didactic purposes (to check everything is ok now and next - use thecalculator in programmer mode)
                        uint ipAsInt = BitConverter.ToUInt32(ipAddr, 0);
                        //Console.WriteLine("\t ip={0}", Convert.ToString(ipAsInt, 2));

                        //we negate the subnet to determine the maximum number of host possible in this subnet
                        uint validHostsEndingMax = ~BitConverter.ToUInt32(subnetMask, 0);
                        //Console.WriteLine("\t !subnet={0}", Convert.ToString(validHostsEndingMax, 2));

                        //we convert the start of the ip addres as uint (the part that is fixed wrt the subnet mask - from here we calculate each new address by incrementing with 1 and converting to byte[] afterwards 
                        uint validHostsStart = BitConverter.ToUInt32(ipAddr, 0) & BitConverter.ToUInt32(subnetMask, 0);
                        //Console.WriteLine("\t IP & subnet={0}", Convert.ToString(validHostsStart, 2));

                        //we increment the startIp to the number of maximum valid hosts in this subnet and for each we check the intended port (refactoring needed)
                        for (uint i = 1; i <= validHostsEndingMax; i++)
                        {
                            uint host = validHostsStart + i;
                            //byte[] hostAsBytes = BitConverter.GetBytes(host);
                            byte[] hostBytes = BitConverter.GetBytes(host);
                            if (BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(hostBytes);
                            }

                            //this is the candidate IP address in "readable format" 
                            String ipCandidate = Convert.ToString(hostBytes[0]) + "." + Convert.ToString(hostBytes[1]) + "." + Convert.ToString(hostBytes[2]) + "." + Convert.ToString(hostBytes[3]);
                            Console.WriteLine("Trying: " + ipCandidate);


                            try
                            {
                                //try to connect
                                //sock.Connect(ipCandidate, wantedPort);
                                // Connect using a timeout (5 seconds)

                                IAsyncResult result = sock.BeginConnect(ipCandidate, wantedPort, null, null);

                                bool success = result.AsyncWaitHandle.WaitOne(1000, true);


                                if (sock.Connected == true)  // if succesfull => something is listening on this port
                                {
                                    foundAddress = ipCandidate;
                                    Console.WriteLine("\tFound Harmony HUB at " + ipCandidate);

                                }
                                sock.Close();
                                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                //else -. goes to exception
                                if (!String.IsNullOrEmpty(foundAddress))
                                {
                                   HubSettings.Instance.HarmonyHubIP = foundAddress;
                                   HubSettings.Instance.Save();
                                    break;
                                }
                            }
                            catch (SocketException ex)
                            {
                                //TODO: if you want, do smth here
                                Console.WriteLine("\tDIDN'T work at " + ipCandidate);
                            }
                        }
                    }
                }


                sock.Close();

            }
            return foundAddress;
        }
    }
}

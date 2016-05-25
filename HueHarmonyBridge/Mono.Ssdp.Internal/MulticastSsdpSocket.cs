// 
// MulticastSsdpSocket.cs
//  
// Author:
//       Scott Peterson <lunchtimemama@gmail.com>
// 
// Copyright (c) 2010 Scott Peterson
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Mono.Ssdp.Internal
{
    class MulticastSsdpSocket : SsdpSocket
    {
        public MulticastSsdpSocket (NetworkInterfaceInfo networkInterfaceInfo)
            : base (IPAddress.Any)//changed from binding on IP to ANY for Mono
        {
            SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            SetSocketOption (SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, Protocol.SocketTtl);
            SetSocketOption (SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption (Protocol.IPAddress, networkInterfaceInfo.Index));
            //// join multicast group on all available network interfaces
            //NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            //foreach (NetworkInterface networkInterface in networkInterfaces)
            //{
            //    if ((!networkInterface.Supports(NetworkInterfaceComponent.IPv4)) ||
            //        (networkInterface.OperationalStatus != OperationalStatus.Up))
            //    {
            //        continue;
            //    }

            //    IPInterfaceProperties adapterProperties = networkInterface.GetIPProperties();
            //    UnicastIPAddressInformationCollection unicastIPAddresses = adapterProperties.UnicastAddresses;
            //    IPAddress ipAddress = null;

            //    foreach (UnicastIPAddressInformation unicastIPAddress in unicastIPAddresses)
            //    {
            //        if (unicastIPAddress.Address.AddressFamily != AddressFamily.InterNetwork)
            //        {
            //            continue;
            //        }

            //        ipAddress = unicastIPAddress.Address;
            //        break;
            //    }

            //    if (ipAddress == null)
            //    {
            //        continue;
            //    }


            //    //UDPSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(Target_IP, ipAddress));
            //    SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(Protocol.IPAddress, ipAddress));
            //}

        }
    }
}


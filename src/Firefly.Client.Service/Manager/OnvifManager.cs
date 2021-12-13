// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Xml;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Firefly.Client.Service
{
    public class OnvifManager
    {
        private bool _isRunning;
        private Task _discoveryTask;
        private const string WS_MULTICAST_ADDRESS = "239.255.255.250";
        private const int WS_MULTICAST_PORT = 3702;
        private const int WS_TIMEOUT = 5 * 1000;

        private static OnvifManager manager = new OnvifManager();

        public event CameraDiscoverEventHandler CameraDiscovered = null;

        public static OnvifManager Instance => manager;

        private OnvifManager()
        {
        }

        public void StartDiscover()
        {
            StopDiscover();
            Task.Run(() =>
            {
                if (_discoveryTask != null)
                {
                    _discoveryTask.Wait();
                }

                if (!_isRunning)
                {
                    _isRunning = true;

                    _discoveryTask = Task.Run(Disconvery);

                    Console.WriteLine($"StartDiscover:{_isRunning}");
                }
            });
        }

        private void Disconvery()
        {
            try
            {
                List<IPAddress> ips = GetInterIPs();

                foreach (IPAddress ip in ips)
                {
                    IPEndPoint localEndPoint = new IPEndPoint(ip, 0);
                    UdpClient udpClient = new UdpClient(localEndPoint)
                    {
                        EnableBroadcast = true
                    };

                    byte[] discoveryMessage = OnvifContentSerializer.GetDisconveryMsg();
                    IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 0);

                    IPAddress multicastAddress = IPAddress.Parse(WS_MULTICAST_ADDRESS);
                    int multicastPort = WS_MULTICAST_PORT;

                    udpClient.Send(discoveryMessage, discoveryMessage.Length, new IPEndPoint(multicastAddress, multicastPort));
                    udpClient.Client.ReceiveTimeout = WS_TIMEOUT;

                    try
                    {
                        while (_isRunning)
                        {
                            try
                            {
                                byte[] responseBytes = udpClient.Receive(ref localEP);
                                string reponseMsg = Encoding.UTF8.GetString(responseBytes);

                                if (!string.IsNullOrWhiteSpace(reponseMsg))
                                {
                                    List<CameraDiscoveryDO> cameras = OnvifContentSerializer.Convert2CameraItems(reponseMsg, localEP);

                                    if (cameras.Count > 0)
                                    {
                                        CameraDiscovered?.Invoke(this, new CameraDiscoveryEventArgs(cameras));
                                    }
                                }
                            }
                            catch
                            {
                                // timeout will break this loop
                                break;
                            }
                            finally
                            {

                            }
                        }
                    }
                    finally
                    {
                        udpClient.Close();
                        udpClient.Dispose();
                    }
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

        public void StopDiscover()
        {
            _isRunning = false;
        }

        private List<IPAddress> GetInterIPs()
        {
            List<IPAddress> ips = new List<IPAddress>();
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip);
                }
            }

            return ips;
        }
    }

    public delegate void CameraDiscoverEventHandler(object sender, CameraDiscoveryEventArgs e);

    public class CameraDiscoveryEventArgs : EventArgs
    {
        internal CameraDiscoveryEventArgs(List<CameraDiscoveryDO> cameras)
        {
            DiscoveryItems = cameras;
        }

        public List<CameraDiscoveryDO> DiscoveryItems { get; private set; }
    }

    internal static class OnvifContentSerializer
    {
        public static byte[] GetDisconveryMsg()
        {
            string disconveryMessage = Properties.Resources.DiscoveryMessage;
            disconveryMessage = string.Format(disconveryMessage, Guid.NewGuid().ToString());

            return Encoding.UTF8.GetBytes(disconveryMessage);
        }

        public static List<CameraDiscoveryDO> Convert2CameraItems(string responseMsg, IPEndPoint endPoint)
        {
            List<CameraDiscoveryDO> cameraItems = new List<CameraDiscoveryDO>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(responseMsg);
            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("d", "http://schemas.xmlsoap.org/ws/2005/04/discovery");

            XmlNodeList probeNodes = doc.SelectNodes("//d:ProbeMatch", mgr);

            if (probeNodes != null && probeNodes.Count > 0)
            {
                foreach (XmlNode probe in probeNodes)
                {
                    XmlNode scopesNode = probe.SelectSingleNode("d:Scopes", mgr);
                    if (scopesNode != null && !string.IsNullOrWhiteSpace(scopesNode.InnerText))
                    {
                        XmlNode typeNode = probe.SelectSingleNode("d:Types", mgr);
                        XmlNode xaddrsNode = probe.SelectSingleNode("d:XAddrs", mgr);
                        Regex regexModel = new Regex("(?<=hardware/).*?(?=\\s|$)", RegexOptions.Compiled);
                        string model = regexModel.Match(scopesNode.InnerText).Value;
                        Regex regexName = new Regex("(?<=name/).*?(?=\\s|$)", RegexOptions.Compiled);
                        string name = regexName.Match(scopesNode.InnerText).Value;
                        CameraDiscoveryDO camera = new CameraDiscoveryDO(endPoint.Address, xaddrsNode.InnerText, typeNode.InnerText, model, name);
                        cameraItems.Add(camera);
                        Console.WriteLine(camera);
                    }
                }
            }

            return cameraItems;
        }
    }
}
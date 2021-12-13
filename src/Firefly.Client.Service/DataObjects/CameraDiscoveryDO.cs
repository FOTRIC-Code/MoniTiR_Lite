using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Firefly.Client.Common;

namespace Firefly.Client.Service
{
    public class CameraDiscoveryDO
    {
        internal CameraDiscoveryDO(IPAddress address, string xAddrs, string types, string model, string name)
        {
            Address = address;
            XAdresses = ToArray(xAddrs);
            Types = ToArray(types);
            Model = model;
            Name = name;
            string portPattern = @"(?<=:)\d{1,5}";
            Match match = Regex.Match(XAdresses[0], portPattern);
            if (int.TryParse(match.Value, out int port))
            {
                Port = port;
            }
            else
            {
                Port = Constants.ONVIF_DC_DEFAULT_PORT;
            }
        }

        public List<string> Types { get; internal set; }

        public List<string> XAdresses { get; internal set; }

        public int Port { get; internal set; }

        public string Model { get; internal set; }

        public string Name { get; internal set; }

        public IPAddress Address { get; internal set; }

        public override string ToString()
        {
            return $"Discovery:{XAdresses[0]}:{Model}:{Name}";
        }

        public List<string> ToArray(string str, string split = null)
        {
            List<string> result = new List<string>();
            string[] splitArray = str.Split(null);
            foreach (string tempStr in splitArray)
            {
                result.Add(tempStr);
            }

            return result;
        }
    }
}

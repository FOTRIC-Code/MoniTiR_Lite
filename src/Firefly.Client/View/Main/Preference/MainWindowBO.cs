using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firefly.Client
{
    public class MainWindowBO
    {
        public int? StartCount { get; set; }
        public long? LastStartTime { get; set; }

        public string Title {  get; set; }
    }
}

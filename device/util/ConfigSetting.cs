using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GodOfBeer.util
{
    [Serializable]
    public class ConfigSetting
    {
        public static string api_server_domain { get; set; }
        public static string api_prefix { get; set; }
        public static string socketServerUrl { get; set; }
        public static string server_address { get; set; }
    }
}

using DemoImplementationConsole.Properties;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DemoImplementationConsole
{
    class ConfigValidation
    {
        static readonly ILog log = LogManager.GetLogger(typeof(ConfigValidation));

        public static void ValidateSettings()
        {
            while (string.IsNullOrWhiteSpace(Settings.Default.UserName))
            {
                log.Debug("Please enter username");
                Settings.Default.UserName = Console.ReadLine();
            }

            while (!IsIpAddressValid(Settings.Default.IpAddress))
            {
                log.Debug("Please enter ip address");
                Settings.Default.IpAddress = Console.ReadLine();
            }

            while (!IsPortValid(Settings.Default.Port))
            {
                log.Debug($"Please enter port [1024-{(1 << 16) - 1}]");
                int port = -1;
                if (int.TryParse(Console.ReadLine(), out port))
                {
                    Settings.Default.Port = port;
                }
            }

            while (!IsMemberCodeValid(Settings.Default.MemberCode))
            {
                log.Debug("Please enter valid member code");
                Settings.Default.MemberCode = Console.ReadLine();
            }

            while (!IsDealerCodeValid(Settings.Default.DealerCode))
            {
                log.Debug("Please enter valid dealer code");
                Settings.Default.DealerCode = Console.ReadLine();
            }
        }

        static bool IsMemberCodeValid(string memberCode)
        {
            if (string.IsNullOrWhiteSpace(memberCode))
            {
                return false;
            }
            if (memberCode.Length != 4)
            {
                return false;
            }
            return true;
        }

        static bool IsDealerCodeValid(string dealerCode)
        {
            if (string.IsNullOrWhiteSpace(dealerCode))
            {
                return false;
            }
            if (dealerCode.Length != 3)
            {
                return false;
            }
            return true;
        }

        static bool IsIpAddressValid(string addr)
        {
            if (string.IsNullOrWhiteSpace(addr))
            {
                return false;
            }

            IPAddress validAddr = null;
            if (!IPAddress.TryParse(addr, out validAddr))
            {
                return false;
            }
            return true;
        }

        static bool IsPortValid(int port)
        {
            return port >= 1 << 10 && port < 1 << 16;
        }
    }
}
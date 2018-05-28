using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AvventoAPILibrary
{
    public class SessionManager
    {
        static readonly ILog log = LogManager.GetLogger(typeof(SessionManager));

        bool _IsConnected;

        public bool IsConnected
        {
            get { return _IsConnected; }
            set
            {
                _IsConnected = value;
                if (!_IsConnected)
                {
                    IsLoggedIn = false;
                }
            }
        }

        public bool IsLoggedIn { get; set; }
    }
}
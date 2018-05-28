using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AvventoAPILibrary
{
    public enum AuctionMode : int
    {
        Open = 0,
        Auction = 1,
        Closed = 2,
        Suspended = 3,
        OpenOrder = 4
    }
}
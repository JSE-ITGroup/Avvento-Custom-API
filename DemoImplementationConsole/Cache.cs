using AvventoAPILibrary;
using log4net;
using nsConnectionData;
using Stt.Derivatives.Api;
using Stt.Derivatives.Api.Constants;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoImplementationConsole
{
    static class Cache
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Cache));

        public static readonly List<MessageDetails> AuditIncomingMessages = new List<MessageDetails>();
        public static readonly List<Tuple<MessageType,object>> AuditOutgoingMessages = new List<Tuple<MessageType, object>>();

        public static readonly ConcurrentDictionary<int, InstrumentStructure> Instruments = new ConcurrentDictionary<int, InstrumentStructure>();
        public static readonly ConcurrentDictionary<int, InstrumentTypeStructure> InstrumentTypes = new ConcurrentDictionary<int, InstrumentTypeStructure>();
        public static readonly ConcurrentDictionary<int, ContractDatesStructure> ContractDates = new ConcurrentDictionary<int, ContractDatesStructure>();
        public static readonly ConcurrentDictionary<int, StrikeStructure> Strikes = new ConcurrentDictionary<int, StrikeStructure>();
        public static readonly ConcurrentDictionary<int, MarkToMarketStructure> MarketToMarkets = new ConcurrentDictionary<int, MarkToMarketStructure>();
        public static readonly ConcurrentDictionary<int, HolidayStructure> Holidays = new ConcurrentDictionary<int, HolidayStructure>();
        public static readonly ConcurrentDictionary<int, DisplayStructure> Displays = new ConcurrentDictionary<int, DisplayStructure>();
        public static readonly ConcurrentDictionary<int, MemberStructure> Members = new ConcurrentDictionary<int, MemberStructure>();
        public static readonly ConcurrentDictionary<int, ClearingMemberStructure> ClearingMembers = new ConcurrentDictionary<int, ClearingMemberStructure>();
        public static readonly ConcurrentDictionary<int, ExchangeAnnouncementStructure> ExchangeAnnouncement = new ConcurrentDictionary<int, ExchangeAnnouncementStructure>();
        public static readonly ConcurrentDictionary<int, NewsServiceStructure> NewsServices = new ConcurrentDictionary<int, NewsServiceStructure>();
        public static readonly ConcurrentDictionary<int, CouponInformationStructure> Coupons = new ConcurrentDictionary<int, CouponInformationStructure>();
        public static readonly ConcurrentDictionary<int, TradingSessionStructure> TradingSessions = new ConcurrentDictionary<int, TradingSessionStructure>();
        public static readonly ConcurrentDictionary<int, IndicesStructure> Indices = new ConcurrentDictionary<int, IndicesStructure>();
        public static readonly ConcurrentDictionary<int, IndicesDataStructure> IndicesData = new ConcurrentDictionary<int, IndicesDataStructure>();
        public static readonly ConcurrentDictionary<int, SharesInIssueStructure> SharesInIssue = new ConcurrentDictionary<int, SharesInIssueStructure>();
        public static readonly ConcurrentDictionary<int, IndexDataStructure> IndexData = new ConcurrentDictionary<int, IndexDataStructure>();
        public static readonly ConcurrentDictionary<int, IndexConstituentsStructure> IndexConstituents = new ConcurrentDictionary<int, IndexConstituentsStructure>();
        public static readonly ConcurrentDictionary<int, ActiveOrderStructure> ActiveOrders = new ConcurrentDictionary<int, ActiveOrderStructure>();
        public static readonly ConcurrentDictionary<int, CompletedOrderStructure> CompletedOrders = new ConcurrentDictionary<int, CompletedOrderStructure>();
        public static readonly ConcurrentDictionary<int, DealStructure> Deals = new ConcurrentDictionary<int, DealStructure>();
        public static readonly ConcurrentDictionary<int, PositionStructure> Positions = new ConcurrentDictionary<int, PositionStructure>();
        public static readonly ConcurrentDictionary<int, DealerStructure> Dealers = new ConcurrentDictionary<int, DealerStructure>();
        public static readonly ConcurrentDictionary<int, UnmatchedAcknowledgmentStructure> TradeCaptureAcknowledgments = new ConcurrentDictionary<int, UnmatchedAcknowledgmentStructure>();
        public static readonly ConcurrentDictionary<int, ClientStructure> Clients = new ConcurrentDictionary<int, ClientStructure>();
        public static readonly ConcurrentDictionary<int, s_ClientInfo> ClientDetails = new ConcurrentDictionary<int, s_ClientInfo>();
        public static readonly ConcurrentDictionary<int, ContactPersonStructure> ContactPersons = new ConcurrentDictionary<int, ContactPersonStructure>();
        public static readonly ConcurrentDictionary<int, CountryStructure> Countries = new ConcurrentDictionary<int, CountryStructure>();
        public static readonly ConcurrentDictionary<int, CashBalanceStructure> CashBalances = new ConcurrentDictionary<int, CashBalanceStructure>();
        public static readonly ConcurrentDictionary<int, AllocationInstructionStructure> AllocationInstructions = new ConcurrentDictionary<int, AllocationInstructionStructure>();
        public static readonly ConcurrentDictionary<int, RFQStructure> RFQData = new ConcurrentDictionary<int, RFQStructure>();
        public static readonly ConcurrentDictionary<int, RFQQuoteStructure> RFQQuotes = new ConcurrentDictionary<int, RFQQuoteStructure>();
        public static readonly ConcurrentDictionary<int, MarketAnnouncementStructure> MarketAnnouncements = new ConcurrentDictionary<int, MarketAnnouncementStructure>();
        public static readonly ConcurrentDictionary<int, UnmatchedCaptureStructure> UnmatchedCaptures = new ConcurrentDictionary<int, UnmatchedCaptureStructure>();


        public static void Update<T, E>(ConcurrentDictionary<T, E> dict, T key, E item, ActionType action)
        {
            switch (action)
            {
                case ActionType.Delete:
                    Delete(dict, key, item);
                    break;

                case ActionType.Insert:
                case ActionType.Update:
                case ActionType.InsertOrUpdate:
                    AddOrUpdate(dict, key, item);
                    break;

                default:
                    Console.Error.WriteLine(action);
                    break;
            }
        }

        static void Delete<T, E>(ConcurrentDictionary<T, E> dict, T key, E itemToDelete)
        {
            E deletedItem;
            if (dict.TryRemove(key, out deletedItem))
            {
                log.Debug("Removed " + itemToDelete + " key: " + key);
            }
            else
            {
                log.Warn("Tried to remove remove " + itemToDelete + " key: " + key + "But item was not in dictionary");
            }
        }

        static void AddOrUpdate<T, E>(ConcurrentDictionary<T, E> dict, T key, E itemToAddOrUpdate)
        {
            dict.AddOrUpdate(key, itemToAddOrUpdate, (i, origIns) =>
            {
                foreach (var item in itemToAddOrUpdate.GetType().GetFields())
                {
                    item.SetValue(origIns, item.GetValue(itemToAddOrUpdate));
                }
                return origIns;
            });
        }
    }
}
using AvventoAPILibrary;
using DemoImplementationConsole.Properties;
using log4net;
using log4net.Config;
using nsConnectionChannel;
using Stt.Derivatives.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DemoImplementationConsole
{
    class Program
    {
       // static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        static void Main(string[] args)
        {
            BasicConfigurator.Configure(); // initialize log4net
            try
            {
                ConfigValidation.ValidateSettings();
                var session = new SessionManager();
                var api = new AvventoAPI(session);
                SetupCallbacks(api, session);
                api.ConnectToMarket(Settings.Default.IpAddress, Settings.Default.Port);
                RequestAllDataExample(api);

                
            }
            catch (Exception ex)
            {
                log.Error("Unexpected error occured", ex);
            }

            Console.ReadLine();
        }

        public static void RequestAllDataExample(AvventoAPI _avventoApi)
        {
            log.Debug("Requesting file downloads");

            _avventoApi.SendFileDownloadMessage(FileIdentifier.ActiveOrders, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.Instruments, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.InstrumentType, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.Strikes, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.MarkToMarket, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.Holiday, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.Member, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.ClearingMember, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.Announcements, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.CouponInformation, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.TradingSession, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.Indices, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.IndicesData, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.SharesInIssue, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.IndexData, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.IndexConstituents, Settings.Default.UserName, false, DateTime.Today);
            _avventoApi.SendFileDownloadMessage(FileIdentifier.Display, Settings.Default.UserName, false, DateTime.Today);
            
            _avventoApi.SendFuturesContractSubscribeMessage(Cache.Displays.Select(d => d.Value.Contract).ToList(), Settings.Default.MarketNumber, Settings.Default.UserName);

            /*
            for (int i = 11; i > 0; --i)
            {
                log.Debug("Sleep: " + i);
                Thread.Sleep(1000);
            }

            log.Debug("Sending buys");
            foreach (var display in Cache.Displays.Values)
            {
                var price = Math.Round(display.OpeningPrice * 1.01, 2);
                log.Debug(Utilities.ConvertFromDelphiString(display.Contract.InstrumentName) + $" {price}");
                _avventoApi.SendOrderInsert(display.Contract, Settings.Default.ClientCode, Settings.Default.MemberCode, Settings.Default.DealerCode, 'B', 100, price,
                Settings.Default.UserName, 0, 0, "L1B" + Utilities.ConvertFromDelphiString(display.Contract.InstrumentName), 0, 'P');
            }

            for (int i = 11; i > 0; --i)
            {
                log.Debug("Sleep: " + i);
                Thread.Sleep(1000);
            }

            log.Debug("Sending sells");
            foreach (var display in Cache.Displays.Values)
            {
                var price = Math.Round(display.OpeningPrice * 1.01, 2);
                log.Debug(Utilities.ConvertFromDelphiString(display.Contract.InstrumentName) + $" {price}");
                _avventoApi.SendOrderInsert(display.Contract, Settings.Default.ClientCode, Settings.Default.MemberCode, Settings.Default.DealerCode, 'S', 100, price,
                Settings.Default.UserName, 0, 0, "L1S" + Utilities.ConvertFromDelphiString(display.Contract.InstrumentName), 0, 'P');
            }
           // Thread.Sleep(3 * 60 * 1000);
           // api.SendFuturesContractUnsubscribeAlleMessage(Settings.Default.UserName);
           */
            _avventoApi.SendLogoutMessage(Settings.Default.UserName);
        }

        static void SetupCallbacks(AvventoAPI _avventoApi, SessionManager session)
        {
            _avventoApi.InternalEvents.APIRAWMessageReceived += des =>
            {
                Cache.AuditIncomingMessages.Add(des);
            };

            _avventoApi.InternalEvents.APIMessageSent += (messageType, messageStruct) =>
            {
                Cache.AuditOutgoingMessages.Add(Tuple.Create(messageType, messageStruct));
            };

            _avventoApi.IncomingMessages.ReceivedErrorCallback = s => log.Debug(s);

            _avventoApi.InternalEvents.NewConnection += (client, socket) =>
            {
                session.IsConnected = true;
                _avventoApi.IncomingMessages.ReceivedErrorCallback($"New Connection on Socket {socket}");
            };

            _avventoApi.InternalEvents.DisconnectHandler += (remoteEndPoint, exception) =>
            {
                session.IsConnected = false;
                session.IsLoggedIn = false;
                log.Error($"Disconnected from {remoteEndPoint} due to {exception}");
            };

            _avventoApi.InternalEvents.SocketErrorHandler += (IpEndPoint, exception) =>
            {
                session.IsConnected = false;
                _avventoApi.IncomingMessages.ReceivedErrorCallback($"Error from Connection {IpEndPoint} message {exception}");
            };

            _avventoApi.IncomingMessages.EncryptionKey = (details, key) =>
            {
                _avventoApi.SendLoginMessage(Settings.Default.UserName, Settings.Default.Password, key);
            };

            _avventoApi.IncomingMessages.ReceivedLoginReplyCallback = delegate
            {
                log.Debug("Login success");
                session.IsLoggedIn = true;
                //session.
            };

            _avventoApi.IncomingMessages.ReceivedHeartbeatCallback = delegate
            {
                log.Debug("Received heartbeat from server");
                log.Debug("Sending heartbeat");
                _avventoApi.SendHeartBeat(Settings.Default.UserName);
            };

      
            _avventoApi.FileDownloads.ReceivedDisplayCallback = (display, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.Displays, display[0].IdDisplay, display[0], action);
                var obj = display;
            };

            _avventoApi.FileDownloads.ReceivedInstrumentCallBack = (instrument, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.Instruments, instrument[0].IdInstrument, [instrument, action);
            };

            _avventoApi.FileDownloads.ReceivedInstrumentTypeCallBack = (instrumentType, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.InstrumentTypes, instrumentType.IdInstrumentType, instrumentType, action);
            };

            _avventoApi.FileDownloads.ReceivedContractDateCallback = (contractDate, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.ContractDates, contractDate.IdContractDate, contractDate, action);
            };

            _avventoApi.FileDownloads.ReceivedStrikeCallback = (strike, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.Strikes, strike.IdStrike, strike, action);
            };

            _avventoApi.FileDownloads.ReceivedMarkToMarketCallback = (mtm, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.MarketToMarkets, mtm.IdContractDate, mtm, action);
            };

            _avventoApi.FileDownloads.ReceivedHolidayCallback = (holiday, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.Holidays, holiday.IdHoliday, holiday, action);
            };

            _avventoApi.FileDownloads.ReceivedMemberCallBack = (member, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.Members, member.IdMember, member, action);
            };

            _avventoApi.FileDownloads.ReceivedClearingMemberCallBack = (clearingMember, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.ClearingMembers, clearingMember.IdClearingMember, clearingMember, action);
            };

            _avventoApi.FileDownloads.ReceivedMarketAnnouncementCallBack = (announcement, action) =>
            {
               // string str = string.Format("Market announcement: {0}", Utilities.ConvertFromDelphiString(announcement.Announcement));
                //log.Debug(str);

               // Cache.Update(Cache.MarketAnnouncements, announcement.IdAnnouncement, announcement, action);
            };

            _avventoApi.FileDownloads.ExchangeAnnouncement = (exchangeAnnouncement, action) =>
            {
                string str = string.Format("ErrorNumber {0} Message {1}", exchangeAnnouncement.MessageNumber, exchangeAnnouncement.Message);
                log.Debug(str);

                Cache.Update(Cache.ExchangeAnnouncement, exchangeAnnouncement.MessageNumber, exchangeAnnouncement, action);
            };

            _avventoApi.FileDownloads.ReceivedNewsServiceCallBack = (newsService, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.NewsServices, newsService.MessageNumber, newsService, action);
            };

            _avventoApi.FileDownloads.ReceivedCouponInformationCallBack = (coupon, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.Coupons, coupon.IdCouponInformation, coupon, action);
            };

            _avventoApi.FileDownloads.ReceivedTradingSessionCallBack = (tradingSession, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.TradingSessions, tradingSession.IdTradingSession, tradingSession, action);
            };

            _avventoApi.FileDownloads.ReceivedIndicesCallBack = (index, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.Indices, index.IdIndices, index, action);
            };

            _avventoApi.FileDownloads.ReceivedIndicesPriceDataCallBack = (index, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.IndicesData, index.IdIndices, index, action);
            };

            _avventoApi.FileDownloads.ReceivedSharesInIssueCallBack = (share, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.SharesInIssue, share.IdSharesInIssue, share, action);
            };

            _avventoApi.FileDownloads.ReceivedIndexDataCallBack = (indexData, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.IndexData, indexData.IdIndexData, indexData, action);
            };

            _avventoApi.FileDownloads.ReceivedIndexConstituentsCallBack = (indexConstituent, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.IndexConstituents, indexConstituent.IdIndexInstrument, indexConstituent, action);
            };

            _avventoApi.FileDownloads.ReceivedDepthCallback = depth =>
            {
                /*
                var auction = (AuctionMode)depth.Header.Auction;
                var oddlot = depth.Header.OddLot == 0 ? "Normal" : (depth.Header.OddLot == 1 ? "Odd" : "AON");
                log.Debug($"{"Contract",-10} {"Oddlot",-6} {"Auction",-9} {"High",9} {"Low",9} {"VWAP",9} {"Close",9} {"Status",-10}");
                log.Debug($"{depth.Header.Contract.ToString(),-10} {oddlot,-6} {auction,-9} {depth.Header.HighPrice,9} {depth.Header.LowPrice,9} {depth.Header.VWAP,9} {depth.Header.ClosingPrice,9} {depth.Header.ContractStatus,-10}");
                */
            };

            _avventoApi.FileDownloads.RecievedActiveOrderCallback = (ao, action, anotherSetToCome) =>
            {
                var obj = ao;
                //Cache.Update(Cache.ActiveOrders, ao.IdOrder, ao, action);
            };

            _avventoApi.FileDownloads.ReceivedCompletedOrderCallback = (co, action,lastPieceOfChunk) =>
            {
                var obj = co;
              //  Cache.Update(Cache.CompletedOrders, co.IdOrder, co, action);
            };

            _avventoApi.FileDownloads.ReceivedDealCallback = (deal, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.Deals, deal.IdDeal, deal, action);
            };

            _avventoApi.FileDownloads.ReceivedPositionDataCallback = (position, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.Positions, position.IdPosition, position, action);
            };

            _avventoApi.FileDownloads.ReceivedDealerDataCallback = (dealer, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.Dealers, dealer.IdDealer, dealer, action);
            };

            _avventoApi.FileDownloads.ReceivedTradeCaptureAcknowledgementCallback = (ack, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.TradeCaptureAcknowledgments, ack.IdUnmatch, ack, action);
            };

            _avventoApi.FileDownloads.ReceivedClientDataCallback = (client, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.Clients, client.IdClient, client, action);
            };

            _avventoApi.FileDownloads.ReceivedTripartiteSetupDataCallback = (tripartiteSetupData, action, anotherSetToCome) =>
            {
               // log.Debug($"Tripartie Setup:\nClient: {tripartiteSetupData.Client}\nMember:{tripartiteSetupData.Member}\nTripartite member:{tripartiteSetupData.TripartiteMember}");
            };

            _avventoApi.FileDownloads.ReceivedClientDetailCallback = (clientDetail, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.ClientDetails, clientDetail.Id_Client, clientDetail, action);
            };

            _avventoApi.FileDownloads.ReceivedContactPersonCallback = (contactPerson, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.ContactPersons, contactPerson.IdContactPerson, contactPerson, action);
            };

            _avventoApi.FileDownloads.ReceivedCountryDataCallback = (country, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.Countries, country.IdCountry, country, action);
            };

            _avventoApi.FileDownloads.ReceivedCashBalanceDataCallback = (cashBalance, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.CashBalances, cashBalance.IdCashBalance, cashBalance, action);
            };

            _avventoApi.FileDownloads.ReceivedAllocationInstructionCallback = (allocationInstruction, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.AllocationInstructions, allocationInstruction.IdAllocationInstruction, allocationInstruction, action);
            };

            _avventoApi.FileDownloads.ReceivedRFQDataCallback = (rfq, action, anotherSetToCome) =>
            {
                //Cache.Update(Cache.RFQData, rfq.IdRFQ, rfq, action);
            };

            _avventoApi.FileDownloads.ReceivedRFQQuoteDataCallback = (rfqQuote, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.RFQQuotes, rfqQuote.IdRFQQuote, rfqQuote, action);
            };

            _avventoApi.IncomingMessages.OrderReject = orderReject =>
            {
                log.Debug($"ErrorNumber {orderReject.Error_Number} Message {orderReject.Message} OrderRef: {Utilities.ConvertFromDelphiString(orderReject.Order_Reference)}");
            };

            _avventoApi.IncomingMessages.OrderCancelReject = ordercancelReject =>
            {
                log.Debug($"ReferenceNumber {ordercancelReject.ReferenceNumber} Message {ordercancelReject.Message} OrderRef: {Utilities.ConvertFromDelphiString(ordercancelReject.ReferenceNumber)}");
            };

            _avventoApi.IncomingMessages.BusinessMessageReject = bmr =>
            {
                string str = string.Format($"ErrorNumber BMR Message {bmr.Message} Reference {Stt.Derivatives.Api.DataConverter.ConvertFromDelphiString(bmr.ReferenceMessage)}");
                log.Debug(str);
            };

            _avventoApi.IncomingMessages.ReceivedFailoverRecoveryAnnouncmentCallback = fail =>
            {
                log.Debug($"Failover type: {fail.FailoverNoticeNumber}\n Market:{fail.MarketNumber}\nShard:{fail.ShardNumber}");
            };

            _avventoApi.FileDownloads.ReceivedUnmatchedCaptureCallback = (capture, action, anotherSetToCome) =>
            {
               // Cache.Update(Cache.UnmatchedCaptures, capture.Id_Unmatch, capture, action);
            };
         
        }
    }
}

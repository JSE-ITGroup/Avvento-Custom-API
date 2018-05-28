using log4net;
using nsConnectionChannel;
using Stt.Derivatives.Api;
using Stt.Derivatives.Api.Constants;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AvventoAPILibrary
{
    public class AvventoAPI
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        #region Member Variables

        readonly ReceiveHandlerImplementation rhi;
        readonly SessionManager session;
        readonly ConnectionManager connection;
        readonly string loggedInUser;

        #endregion Member Variables

        public AvventoAPI(SessionManager session)
        {
            this.InternalEvents = new _InternalEvents();
            this.IncomingMessages = new _IncomingMessages();
            this.FileDownloads = new _FileDownloads();
            rhi = new ReceiveHandlerImplementation(this);
            this.session = session;
            connection = new ConnectionManager(this, rhi, session);
            rhi.Start();
            
        }

        public readonly _FileDownloads FileDownloads;
        public sealed class _FileDownloads
        {
            public Action<List<DisplayStructure>, ActionType, bool> ReceivedDisplayCallback;
            public Action<List<InstrumentStructure>, ActionType, bool> ReceivedInstrumentCallBack;
            public Action<List<InstrumentTypeStructure>, ActionType, bool> ReceivedInstrumentTypeCallBack;
            public Action<List<ContractDatesStructure>, ActionType, bool> ReceivedContractDateCallback;
            public Action<List<StrikeStructure>, ActionType, bool> ReceivedStrikeCallback;
            public Action<List<MarkToMarketStructure>, ActionType, bool> ReceivedMarkToMarketCallback;
            public Action<List<HolidayStructure>, ActionType, bool> ReceivedHolidayCallback;
            public Action<List<MemberStructure>, ActionType, bool> ReceivedMemberCallBack;
            public Action<List<ClearingMemberStructure>, ActionType, bool> ReceivedClearingMemberCallBack;
            public Action<List<MarketAnnouncementStructure>, ActionType> ReceivedMarketAnnouncementCallBack;
            public Action<ExchangeAnnouncementStructure, ActionType> ExchangeAnnouncement;
            public Action<List<NewsServiceStructure>, ActionType, bool> ReceivedNewsServiceCallBack;
            public Action<List<CouponInformationStructure>, ActionType, bool> ReceivedCouponInformationCallBack;
            public Action<List<TradingSessionStructure>, ActionType, bool> ReceivedTradingSessionCallBack;
            public Action<List<IndicesStructure>, ActionType, bool> ReceivedIndicesCallBack;
            public Action<List<IndicesDataStructure>, ActionType, bool> ReceivedIndicesPriceDataCallBack;
            public Action<List<SharesInIssueStructure>, ActionType, bool> ReceivedSharesInIssueCallBack;
            public Action<List<IndexDataStructure>, ActionType, bool> ReceivedIndexDataCallBack;
            public Action<List<IndexConstituentsStructure>, ActionType, bool> ReceivedIndexConstituentsCallBack;
            public Action<Depth> ReceivedDepthCallback;
            public Action<List<ActiveOrderStructure>, ActionType, bool> RecievedActiveOrderCallback;
            public Action<List<AuditActiveOrdersStructure>, ActionType, bool> RecievedAuditActiveOrderCallback;
            public Action<List<CompletedOrderStructure>, ActionType, bool> ReceivedCompletedOrderCallback;
            public Action<List<DealStructure>, ActionType, bool> ReceivedDealCallback;
            public Action<List<PositionStructure>, ActionType, bool> ReceivedPositionDataCallback;
            public Action<List<UnmatchedAcknowledgmentStructure>, ActionType, bool> ReceivedTradeCaptureAcknowledgementCallback;
            public Action<List<DealerStructure>, ActionType, bool> ReceivedDealerDataCallback;
            public Action<List<ClientStructure>, ActionType, bool> ReceivedClientDataCallback;
            public Action<List<TripartiteStructure>, ActionType, bool> ReceivedTripartiteSetupDataCallback;
            public Action<List<s_ClientInfo>, ActionType, bool> ReceivedClientDetailCallback;
            public Action<List<ContactPersonStructure>, ActionType, bool> ReceivedContactPersonCallback;
            public Action<List<CountryStructure>, ActionType, bool> ReceivedCountryDataCallback;
            public Action<List<CSDLimitStructure>, ActionType, bool> ReceivedShareBalanceDataCallback;
            public Action<List<CashBalanceStructure>, ActionType, bool> ReceivedCashBalanceDataCallback;
            public Action<List<AllocationInstructionStructure>, ActionType, bool> ReceivedAllocationInstructionCallback;
            public Action<List<RFQStructure>, ActionType, bool> ReceivedRFQDataCallback;
            public Action<List<RFQQuoteStructure>, ActionType, bool> ReceivedRFQQuoteDataCallback;
            public Action<List<UnmatchedCaptureStructure>, ActionType, bool> ReceivedUnmatchedCaptureCallback;
            public Action<MessageDetails,bool> ReceivedFileUpdatesCallback;



        }

        public readonly _IncomingMessages IncomingMessages;
        public sealed class _IncomingMessages
        {
            public Action<string> ReceivedErrorCallback;
            public Action<Exception> ReceivedDisconnectCallback;
            public Action<LoginReply> ReceivedLoginReplyCallback;
            public Action ReceivedHeartbeatCallback;
            public Action<MessageDetails, byte[]> EncryptionKey;
            public Action<BusinessRejectStructure> BusinessMessageReject;
            public Action<OrderRejectMessage> OrderReject;
            public Action<OrderCancelRejectStructure> OrderCancelReject;
            public Action<FailoverRecoveryAnnouncmentStructure> ReceivedFailoverRecoveryAnnouncmentCallback;
            public Action<List<DepthElement>> ReceivedDisplayUpdateCallback;
        }

        public readonly _InternalEvents InternalEvents;
        public sealed class _InternalEvents
        {

            public delegate void APIRAWMessageReceivedDelegate(MessageDetails des);
            public APIRAWMessageReceivedDelegate APIRAWMessageReceived;

            public delegate void APIMessageSentDelegate(MessageType type, object messageStruct);
            public APIMessageSentDelegate APIMessageSent;

            public delegate void NewConnectionDelegate(Channel client, string socket);
            public NewConnectionDelegate NewConnection;

            public delegate void DisconnectHandlerDelegate(string remoteEndPoint, Exception exception);
            public DisconnectHandlerDelegate DisconnectHandler;

            public delegate void SocketErrorHandlerDelegate(string IpEndPoint, string exception);
            public SocketErrorHandlerDelegate SocketErrorHandler;
        }

        public void ConnectToMarket(string IPAddress, int port)
        {
            connection.ConnectToMarket(IPAddress, port);
        }

        #region Send

        public void SendLoginMessage(string userName, string Password, byte[] PublicKey)
        {
            log.Debug("Sending Login Message");
            Login loginMessage = new Login();
            loginMessage.Password = Utilities.EncryptPassword(Password, PublicKey);
            connection.Send(MessageType.MESSAGE_0_LOGIN_REQUEST, userName, loginMessage);
        }

        public void SendLogoutMessage(string userName)
        {
            log.Debug("Sending Logout Message");
            connection.Send(MessageType.MESSAGE_4_LOGOUT, userName);
        }

        public void SendFileDownloadMessage(FileDownloadHeader header, string username)
        {
            log.Debug("Sending File Download Message");
            connection.Send(MessageType.MESSAGE_36_START_OF_DAY_DOWNLOAD, username, header);
        }


        public void SendFileDownloadMessage(FileIdentifier dataType, string userName, bool reRequest, DateTime date)
        {
            var fdh = new FileDownloadHeader();

            fdh.WhichFile = (byte)dataType;
            fdh.LastPieceOfChunk = false;
            fdh.AnotherSetToCome = reRequest;
            fdh.Action = 0;
            fdh.SpecificRecord = 0;
            fdh.Date = new MitsDate(date);
            connection.Send(MessageType.MESSAGE_36_START_OF_DAY_DOWNLOAD, userName, fdh);
        }

        public void RequestDailyTrend(OrderContract orderContract, string userName)
        {
            HistoryRequestStructure request = new HistoryRequestStructure();
            request.Contract = orderContract;
            connection.Send(MessageType.MESSAGE_61_REQUEST_HISTORY, userName, request);
        }

        public void SendOrderInsert(OrderContract contract, string clientCode, string secondMemberCode, string SecondDealerCode, char buySell, int qty, double price, string userName, int bidType,
             int cancelFlag, string orderReferenceNumber, int TimeoutSeconds, char principleAgency)
        {
            MultiBid mb = new MultiBid();

            mb.NumberOfOrders = 1;
            mb.FirstOrder.BuySell = buySell;
            mb.FirstOrder.Cancel = cancelFlag;
            mb.FirstOrder.Contract = contract;
            mb.FirstOrder.DealerCode = Utilities.ConvertToDelphiString(SecondDealerCode, 4);
            mb.FirstOrder.MemberCode = Utilities.ConvertToDelphiString(secondMemberCode, 6);
            mb.FirstOrder.Price = price;
            mb.FirstOrder.Principal = Utilities.ConvertToDelphiString(clientCode, 8);
            mb.FirstOrder.PrincipleAgency = principleAgency;
            mb.FirstOrder.Quantity = qty;
            mb.FirstOrder.Reference = Utilities.ConvertToDelphiString(orderReferenceNumber, 10);
            mb.FirstOrder.Type = bidType;
            mb.FirstOrder.TimeOutSeconds = TimeoutSeconds;
            mb.FirstOrder.HoldOverDate = new MitsDate(0, 0, 0);
            mb.FirstOrder.NumberOfAllocations = 0;

            connection.Send(MessageType.MESSAGE_56_MULTIBID, userName, mb);
        }

        public void SendOrderInsert(OrderContract contract, string clientCode, string secondMemberCode, string SecondDealerCode, char buySell, int qty, double price, string userName, int bidType,
           int cancelFlag, string orderReferenceNumber, int TimeoutSeconds, DateTime ExpiryDate)
        {
            MultiBid mb = new MultiBid();


            MitsDate ExpDate = new MitsDate();

            if (ExpiryDate.CompareTo(DateTime.MinValue)==0)
            {
                ExpDate.Day = 0;
                ExpDate.Month = 0;
                ExpDate.Year = 0;

            }
            else
            {
                ExpDate.Day = ExpiryDate.Day;
                ExpDate.Month = ExpiryDate.Month;
                ExpDate.Year = ExpiryDate.Year;
            }
            
            mb.NumberOfOrders = 1;
            mb.FirstOrder.BuySell = buySell;
            mb.FirstOrder.Cancel = cancelFlag;
            mb.FirstOrder.Contract = contract;
            mb.FirstOrder.DealerCode = Utilities.ConvertToDelphiString(SecondDealerCode, 4);
            mb.FirstOrder.MemberCode = Utilities.ConvertToDelphiString(secondMemberCode, 6);
            mb.FirstOrder.Price = price;
            mb.FirstOrder.Principal = Utilities.ConvertToDelphiString(clientCode, 8);
            mb.FirstOrder.PrincipleAgency = 'A';
            mb.FirstOrder.Quantity = qty;
            mb.FirstOrder.Reference = Utilities.ConvertToDelphiString(orderReferenceNumber, 10);
            mb.FirstOrder.Type = bidType;
            mb.FirstOrder.TimeOutSeconds = TimeoutSeconds;
            mb.FirstOrder.HoldOverDate = ExpDate;
            mb.FirstOrder.NumberOfAllocations = 0;
            connection.Send(MessageType.MESSAGE_56_MULTIBID, userName, mb);
        }

        public void SuspendActiveOrder(OrderContract contract, int idActiveOrder, string userName, string dealer, string member, char buysell)
        {
            SuspendDeleteActiveOrder sus = new SuspendDeleteActiveOrder();
            

            sus.Contract = contract;
            sus.DealerCode = DataConverter.ConvertToDelphiString(dealer, 4);
            sus.IdActiveOrder = idActiveOrder;
            sus.MemberCode = DataConverter.ConvertToDelphiString(member, 6);
            sus.Action = 'S';
            sus.BuySell = buysell;
            connection.Send(MessageType.MESSAGE_8_SUSPEND_OR_CANCEL_ACTIVE_ORDER, userName, sus);

        }
        public void SuspendOrDeleteActiveOrder(OrderContract contract, int idActiveOrder, string userName, string dealer, string member, char buysell,char action)
        {
           
           SuspendDeleteActiveOrder susOrdel = new SuspendDeleteActiveOrder();

            susOrdel.Contract = contract;
            susOrdel.DealerCode = DataConverter.ConvertToDelphiString(dealer, 4);
            susOrdel.IdActiveOrder = idActiveOrder;
            susOrdel.MemberCode = DataConverter.ConvertToDelphiString(member, 6);
            susOrdel.Action = action;
            susOrdel.BuySell = buysell;
            connection.Send(MessageType.MESSAGE_8_SUSPEND_OR_CANCEL_ACTIVE_ORDER, userName, susOrdel);

        }


        //Resubmitt 27
        public void ResubmitSuspendedOrder(int IdActiveOrder, string Contract, string userName)
        {
            ResubmitOrderStructure res = new ResubmitOrderStructure();
            res.IdActiveOrder = IdActiveOrder;
            res.Contract = DataConverter.ConvertToDelphiString(Contract, 5);

            connection.Send(MessageType.MESSAGE_27_RESUBMIT_SUSPENDED_ORDER, userName, res);
            
        }

        //Reduce Order Quantity
        public void ReduceOrderQuantity(int idActiveOrder, char buySell, int quantity, OrderContract spotContract, string userName, string member)
        {
            ReduceOrderStructure red = new ReduceOrderStructure();
            red.Contract = spotContract;
            red.IdActiveOrder = idActiveOrder;
            red.MemberCode = DataConverter.ConvertToDelphiString(member, 6);
            red.BuySell = buySell;
            red.Quantity = quantity;

            connection.Send(MessageType.MESSAGE_104_ORDER_REDUCE, userName, red);

        
        }
        public void SendCancelAllOrders(OrderContract contract, char buySell, string memberCode, string dealerCode, string userName)
        {
            CancelAllOrdersStructure c = new CancelAllOrdersStructure();

            c.BuyOrSell = buySell;
            c.CancelFlag = 4;
            c.Contract = contract;
            c.DealerCode = DataConverter.ConvertToDelphiString(dealerCode, 4);
            c.MemberCode = DataConverter.ConvertToDelphiString(memberCode, 6);

            connection.Send(MessageType.MESSAGE_85_CANCEL_ORDER, userName, c);
        }
        public void EditSuspendedOrder(OrderContract Contract, char BuyOrSell, double Price, long Quantity, string Principle, string Reference,
            string Dealer, string Member, int IdActiveOrder, string userName)
        {
            EditSuspendedOrder eso = new Stt.Derivatives.Api.EditSuspendedOrder();
            eso.Contract = Contract;
            eso.BuyOrSell = BuyOrSell;
            eso.Price = Price;
            eso.Quantity = Quantity;
            eso.Principle = DataConverter.ConvertToDelphiString(Principle, 8);
            eso.Reference = DataConverter.ConvertToDelphiString(Reference, 10);
            eso.Dealer = DataConverter.ConvertToDelphiString(Dealer, 4);
            eso.Member = DataConverter.ConvertToDelphiString(Member, 6);
            eso.IdActiveOrder = IdActiveOrder;

            connection.Send(MessageType.MESSAGE_118_EDIT_SUSPENDED_ORDER, userName, eso);
            
        }

        public void EditActiveOrderBySequence(int idActiveOrder, string userName, string member, string dealer, string principle, char buySell, OrderContract orderContract,
           double rate, int quantity, string userReference, string additionalReference)
        {
           

            Stt.Derivatives.Api.EditActiveOrderByIdStructure mb = new Stt.Derivatives.Api.EditActiveOrderByIdStructure();

            mb.AdditionalUserReference = Utilities.ConvertToDelphiString(additionalReference, 10);
            mb.BuySell = buySell;
            mb.Contract = orderContract;
            mb.Dealer = Utilities.ConvertToDelphiString(dealer, 4);
            mb.IdActiveOrder = idActiveOrder;
            mb.Member = Utilities.ConvertToDelphiString(member, 6);
            mb.Principle = Utilities.ConvertToDelphiString(principle, 7);
            mb.Quantity = quantity;
            mb.Rate = rate;
            mb.UserReference = Utilities.ConvertToDelphiString(userReference, 10);

            connection.Send(MessageType.MESSAGE_160_EDITORDERBYID, userName, mb);
        
        }
        public void EditActiveOrderBySequence(int idActiveOrder, string userName, string member, string dealer, string principle, char buySell, OrderContract orderContract,
            double rate, int quantity, string userReference, string additionalReference, string Principal)
        {
            

            Stt.Derivatives.Api.EditActiveOrderByIdStructure mb = new Stt.Derivatives.Api.EditActiveOrderByIdStructure();

            mb.AdditionalUserReference = Utilities.ConvertToDelphiString(additionalReference, 10);
            mb.BuySell = buySell;
            mb.Contract = orderContract;
            mb.Dealer = Utilities.ConvertToDelphiString(dealer, 4);
            mb.IdActiveOrder = idActiveOrder;
            mb.Member = Utilities.ConvertToDelphiString(member, 6);
            mb.Principle = Utilities.ConvertToDelphiString(principle, 7);
            mb.Quantity = quantity;
            mb.Rate = rate;
            mb.UserReference = Utilities.ConvertToDelphiString(userReference, 10);

            connection.Send(MessageType.MESSAGE_160_EDITORDERBYID, userName, mb);
        }

        /// <summary>Sends the indices subscribtion.</summary>
        /// <param name="userName">Name of the user.</param>
        /// <autogeneratedoc />
        public void SendIndicesSubscribtion(string userName)
        {
            Stt.Derivatives.Api.IndicesSubscriptionStructure s = new IndicesSubscriptionStructure();

            s.Subscribe = true;

            connection.Send(MessageType.MESSAGE_98_INDICES_SUBSCRIPTION, userName, s);

        }

        public void SendFuturesContractUnsubscribeAll(string userName)
        {
            DepthRequest depthRequest = new DepthRequest();
            depthRequest.ContractsRequested = 1;
            connection.Send(MessageType.MESSAGE_42_FUTURES_SCREEN_CLOSE, userName, depthRequest);
        }

        public void SendFuturesContractSubscribeMessage(List<OrderContract> contracts, int marketNumber, string userName)
        {
            DepthRequest depthRequest = new DepthRequest();
            OrderContract[] orders = new OrderContract[40];
            int index = 0;

            foreach (var contract in contracts)
            {
                if (contract.CallPut == 'P' || contract.CallPut == 'C')
                    continue;

                orders[index] = contract;

                depthRequest.AddContract(contract);

                if (index == 39)
                {
                    connection.Send(MessageType.MESSAGE_99_FUTURES_SCREEN_OPEN, userName, depthRequest);

                    depthRequest = new DepthRequest();
                    orders = new OrderContract[40];
                    index = -1;
                }
                index++;
            }

            if (index > 0)
            {
                connection.Send(MessageType.MESSAGE_99_FUTURES_SCREEN_OPEN, userName, depthRequest);
            }
        }

        public void SendHeartBeat(string userName)
        {
            connection.Send(MessageType.MESSAGE_84_HEARTBEAT, userName);
        }



        #endregion Send
    }
}
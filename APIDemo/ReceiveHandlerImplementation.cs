using log4net;
using nsConnectionData;
using Stt.Derivatives.Api;
using Stt.Derivatives.Api.Constants;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AvventoAPILibrary
{
    public class ReceiveHandlerImplementation : nsConnectionChannel.IReceiverCallbackInterface
    {
         private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        readonly Thread receiveThread;
        readonly Dictionary<int, byte[]> fileDownloadStore = new Dictionary<int, byte[]>(); 
        readonly Dictionary<int, int> fileDownloadStoreLength = new Dictionary<int, int>(); 
        readonly BlockingCollection<ConnectionData> receiveQueue = new BlockingCollection<ConnectionData>();


        readonly AvventoAPI _avventoApi;
        public ReceiveHandlerImplementation(AvventoAPI _avventoApi)
        {
            this._avventoApi = _avventoApi;
            receiveThread = new Thread(Handler);
        }

        public void Push(ConnectionData packet)
        {
            receiveQueue.Add(packet);
        }

        public void Start()
        {
            receiveThread.Start();
        }

        public void Stop()
        {
            receiveQueue.CompleteAdding();
        }

        public void Handler()
        {
            while (!receiveQueue.IsCompleted)
            {
                ConnectionData packet = receiveQueue.Take();
                MessageDetails des = new MessageDetails(packet);
                _avventoApi.InternalEvents.APIRAWMessageReceived?.Invoke(des);
                switch (des.headerType)
                {
                    case MessageType.MESSAGE_1_LOGIN_REPLY:
                        {
                            LoginReply loginReply = Utilities.OnPacket<LoginReply>(des.data);

                            _avventoApi.IncomingMessages.ReceivedLoginReplyCallback(loginReply);
                        }
                        break;

                    case MessageType.MESSAGE_10_OUTGOING_HEARTBEAT_GENERATOR:
                        {
                            _avventoApi.IncomingMessages.ReceivedHeartbeatCallback();
                        }
                        break;

                    case MessageType.MESSAGE_16_ENCRYPTIONKEY_RESPONSE:
                        {
                            _avventoApi.IncomingMessages.EncryptionKey(des, des.data);
                        }
                        break;

                    case MessageType.MESSAGE_36_START_OF_DAY_DOWNLOAD:
                        {
                            HandleFileDownloadChunk(des);
                        }
                        break;

                    case MessageType.MESSAGE_123_FILE_UPDATE:
                        {
                            HandleFileDownloadChunk(des);
                            HandleFileUpdate(des);
                        }
                        break;

                    case MessageType.MESSAGE_125_EXCHANGEANNOUCEMENT:
                        {
                            ExchangeAnnouncementStructure announcement = Utilities.OnPacket<ExchangeAnnouncementStructure>(des.data);
                            _avventoApi.FileDownloads.ExchangeAnnouncement(announcement, ActionType.Insert);
                        }
                        break;

                    case MessageType.MESSAGE_126_ORDERREJECTMESSAGE:
                        {
                            OrderRejectMessage orm = Utilities.OnPacket<OrderRejectMessage>(des.data);
                            _avventoApi.IncomingMessages.OrderReject(orm);
                        }
                        break;

                    case MessageType.MESSAGE_59_SCREEN_UPDATE_REPLY:
                    {
                            //get the depth structure first
                            Depth depth = (Depth)DataConverter.ConvertFromBufferToStruct(des.data, 1, Depth.Length, typeof(Depth)); //skip the first byte as it only used when processing 99s

                            //get the underlying depth elements
                            int calcedLengthElements = Marshal.SizeOf<DepthElement>() * depth.Header.NumDepth;
                            byte[] depthElements = new byte[calcedLengthElements];
                            Buffer.BlockCopy(depth.DepthElements, 0, depthElements, 0, calcedLengthElements);

                            
                            List<DepthElement> elements = DataConverter.ConvertFromBufferToStructList<DepthElement>(depthElements, Marshal.SizeOf<DepthElement>());

                            _avventoApi.IncomingMessages.ReceivedDisplayUpdateCallback(elements);
                    }
                        break;

                    case MessageType.MESSAGE_132_BUSINESSMESSAGEREJECT:
                        {
                            BusinessRejectStructure bmr = Utilities.OnPacket<BusinessRejectStructure>(des.data);
                            _avventoApi.IncomingMessages.BusinessMessageReject(bmr);
                        }
                        break;

                    case MessageType.MESSAGE_133_ORDERCANCELREJECT:
                        {
                            OrderCancelRejectStructure ocr = Utilities.OnPacket<OrderCancelRejectStructure>(des.data);
                            _avventoApi.IncomingMessages.OrderCancelReject(ocr);
                        }
                        break;

                    default:
                        log.Warn($"Unknown message type: {des.headerType}");
                        break;
                }
            }
        }

        #region FileHandlers

        private void HandleDisplay(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<DisplayStructure> displays = Utilities.ConvertFromBufferToStructList<DisplayStructure>(decompressedData);

           // foreach (var display in displays)
            //{
                _avventoApi.FileDownloads.ReceivedDisplayCallback(displays, action, anotherSetToCome);
           // }
        }

        private void HandleInstruments(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<InstrumentStructure> instruments = Utilities.ConvertFromBufferToStructList<InstrumentStructure>(decompressedData);

           // foreach (var instrument in instruments)
            //{
                _avventoApi.FileDownloads.ReceivedInstrumentCallBack(instruments, action, anotherSetToCome);
            //}
        }

        private void HandleInstrumentType(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<InstrumentTypeStructure> instrumentType = Utilities.ConvertFromBufferToStructList<InstrumentTypeStructure>(decompressedData);

           // foreach (var instrumentType in InstrumentType)
            //{
                _avventoApi.FileDownloads.ReceivedInstrumentTypeCallBack(instrumentType, action, anotherSetToCome);
           // }
        }

        private void HandleContractDates(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<ContractDatesStructure> contractDates = Utilities.ConvertFromBufferToStructList<ContractDatesStructure>(decompressedData);

           // foreach (var contractDate in contractDates)
            //{
                _avventoApi.FileDownloads.ReceivedContractDateCallback(contractDates, action, anotherSetToCome);
           // }
        }

        private void HandleStrikes(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<StrikeStructure> strikes = Utilities.ConvertFromBufferToStructList<StrikeStructure>(decompressedData);

           // foreach (var strike in strikes)
           // {
                _avventoApi.FileDownloads.ReceivedStrikeCallback(strikes, action, anotherSetToCome);
           // }
        }

        private void HandleMarkToMarket(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<MarkToMarketStructure> markToMarket = Utilities.ConvertFromBufferToStructList<MarkToMarketStructure>(decompressedData);

           // foreach (var item in markToMarket)
            //{
                _avventoApi.FileDownloads.ReceivedMarkToMarketCallback(markToMarket, action, anotherSetToCome);
           // }
        }

        private void HandleHolidays(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<HolidayStructure> holidays = Utilities.ConvertFromBufferToStructList<HolidayStructure>(decompressedData);

            //foreach (var holiday in holidays)
            //{
                _avventoApi.FileDownloads.ReceivedHolidayCallback(holidays, action, anotherSetToCome);
           // }
        }

        private void HandleMember(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<MemberStructure> members = Utilities.ConvertFromBufferToStructList<MemberStructure>(decompressedData);

           // foreach (var member in members)
           // {
                _avventoApi.FileDownloads.ReceivedMemberCallBack(members, action, anotherSetToCome);
           // }
        }

        private void HandleClearingMember(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<ClearingMemberStructure> clearingMembers = Utilities.ConvertFromBufferToStructList<ClearingMemberStructure>(decompressedData);

           // foreach (var clearingMember in clearingMembers)
           // {
                _avventoApi.FileDownloads.ReceivedClearingMemberCallBack(clearingMembers, action, anotherSetToCome);
           // }
        }

        private void HandleMarketAnnouncement(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<MarketAnnouncementStructure> announcements = Utilities.ConvertFromBufferToStructList<MarketAnnouncementStructure>(decompressedData);

          //  foreach (var announcement in announcements)
          //  {
                _avventoApi.FileDownloads.ReceivedMarketAnnouncementCallBack(announcements, action);
           // }
        }

        private void HandleNewsAnnouncement(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<NewsServiceStructure> newsServices = Utilities.ConvertFromBufferToStructList<NewsServiceStructure>(decompressedData);

           // foreach (var news in newsServices)
            //{
                _avventoApi.FileDownloads.ReceivedNewsServiceCallBack(newsServices, action, anotherSetToCome);
           // }
        }

        private void HandleCouponInformation(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<CouponInformationStructure> coupons = Utilities.ConvertFromBufferToStructList<CouponInformationStructure>(decompressedData);

          //  foreach (var coupon in coupons)
           // {
                _avventoApi.FileDownloads.ReceivedCouponInformationCallBack(coupons, action, anotherSetToCome);
           // }
        }

        private void HandleTradingSession(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<TradingSessionStructure> tradingSessions = Utilities.ConvertFromBufferToStructList<TradingSessionStructure>(decompressedData);

           // foreach (var tradingsSession in tradingSessions)
           // {
                _avventoApi.FileDownloads.ReceivedTradingSessionCallBack(tradingSessions, action, anotherSetToCome);
            //}
        }

        private void HandleIndices(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<IndicesStructure> indices = Utilities.ConvertFromBufferToStructList<IndicesStructure>(decompressedData);

            //foreach (var index in indices)
           // {
                _avventoApi.FileDownloads.ReceivedIndicesCallBack(indices, action, anotherSetToCome);
           // }
        }

        private void HandleIndicesPriceData(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<IndicesDataStructure> indicesPrices = Utilities.ConvertFromBufferToStructList<IndicesDataStructure>(decompressedData);

            //foreach (var index in indicesPrices)
           // {
                _avventoApi.FileDownloads.ReceivedIndicesPriceDataCallBack(indicesPrices, action, anotherSetToCome);
            //}
        }

        private void HandleSharesInIssue(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<SharesInIssueStructure> sharesInIssue = Utilities.ConvertFromBufferToStructList<SharesInIssueStructure>(decompressedData);

            //foreach (var share in sharesInIssue)
           // {
                _avventoApi.FileDownloads.ReceivedSharesInIssueCallBack(sharesInIssue, action, anotherSetToCome);
           // }
        }

        private void HandleIndexData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<IndexDataStructure> indexData = Utilities.ConvertFromBufferToStructList<IndexDataStructure>(decompressedData);

          //  foreach (var indexDataRecord in indexData)
           // {
                _avventoApi.FileDownloads.ReceivedIndexDataCallBack(indexData, action, anotherSetToCome);
            //}
        }

        private void HandleIndexConstituentsData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<IndexConstituentsStructure> indexConstituentsData = Utilities.ConvertFromBufferToStructList<IndexConstituentsStructure>(decompressedData);

            //foreach (var indexConstituentsDataRecord in indexConstituentsData)
            //{
                _avventoApi.FileDownloads.ReceivedIndexConstituentsCallBack(indexConstituentsData, action, anotherSetToCome);
           // }
        }

        private void HandleActiveOrders(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<ActiveOrderStructure> activeOrders = Utilities.ConvertFromBufferToStructList<ActiveOrderStructure>(decompressedData);
           // foreach (var activeOrder in activeOrders)
            //{
                _avventoApi.FileDownloads.RecievedActiveOrderCallback(activeOrders, action, anotherSetToCome);
            //}
        }
        
        private void HandleAuditActiveOrders(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<AuditActiveOrdersStructure> auditActiveOrdersStructures = Utilities.ConvertFromBufferToStructList<AuditActiveOrdersStructure>(decompressedData);
            // foreach (var activeOrder in activeOrders)
            //{
            _avventoApi.FileDownloads.RecievedAuditActiveOrderCallback(auditActiveOrdersStructures, action, anotherSetToCome);
            //}
        }
        private void HandleCompletedOrders(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<CompletedOrderStructure> completedOrders = Utilities.ConvertFromBufferToStructList<CompletedOrderStructure>(decompressedData);
            //foreach (var completedOrder in completedOrders)
           // {
                _avventoApi.FileDownloads.ReceivedCompletedOrderCallback(completedOrders, action, anotherSetToCome);
            //}
        }

        private void HandleDeals(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<DealStructure> deals = Utilities.ConvertFromBufferToStructList<DealStructure>(decompressedData);
           // foreach (var deal in deals)
           // {
                _avventoApi.FileDownloads.ReceivedDealCallback(deals, action, anotherSetToCome);
           // }
        }

        private void HandlePositionData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<PositionStructure> positions = Utilities.ConvertFromBufferToStructList<PositionStructure>(decompressedData);
           // foreach (var position in positions)
           // {
                _avventoApi.FileDownloads.ReceivedPositionDataCallback(positions, action, anotherSetToCome);
            //}
        }

        private void HandleTradeCaptureAcknowledgementData(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<UnmatchedAcknowledgmentStructure> tradeCaptureAcknowledgements = Utilities.ConvertFromBufferToStructList<UnmatchedAcknowledgmentStructure>(decompressedData);
            //foreach (var tradeCaptureAcknowledgement in tradeCaptureAcknowledgements)
          //  {
                _avventoApi.FileDownloads.ReceivedTradeCaptureAcknowledgementCallback(tradeCaptureAcknowledgements, action, anotherSetToCome);
           // }
        }

        private void HandleDealerData(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<DealerStructure> dealers = Utilities.ConvertFromBufferToStructList<DealerStructure>(decompressedData);
           // foreach (var dealer in dealers)
           // {
                _avventoApi.FileDownloads.ReceivedDealerDataCallback(dealers, action, anotherSetToCome);
           // }
        }

        private void HandleClientData(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<ClientStructure> clients = Utilities.ConvertFromBufferToStructList<ClientStructure>(decompressedData);
            // foreach (var client in clients)
            //  bool lastPiece
            _avventoApi.FileDownloads.ReceivedClientDataCallback(clients, action, anotherSetToCome);
          //  }
        }

        private void HandleTripartiteSetupData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<TripartiteStructure> tripartiteSetupDataItems = Utilities.ConvertFromBufferToStructList<TripartiteStructure>(decompressedData);
           // foreach (var tripartiteSetupData in tripartiteSetupDataItems)
          //  {
                _avventoApi.FileDownloads.ReceivedTripartiteSetupDataCallback(tripartiteSetupDataItems, action, anotherSetToCome);
          //  }
        }

        private void HandleClientDetail(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<s_ClientInfo> clientDetails = Utilities.ConvertFromBufferToStructList<s_ClientInfo>(decompressedData);
          
                _avventoApi.FileDownloads.ReceivedClientDetailCallback(clientDetails, action, anotherSetToCome);
         
        }

        private void HandleContactPersonDetail(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<ContactPersonStructure> contactPersons = Utilities.ConvertFromBufferToStructList<ContactPersonStructure>(decompressedData);
            //foreach (var contactPerson in contactPersons)
            //{
                _avventoApi.FileDownloads.ReceivedContactPersonCallback(contactPersons, action, anotherSetToCome);
            //}
        }

        private void HandleCountryData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<CountryStructure> countries = Utilities.ConvertFromBufferToStructList<CountryStructure>(decompressedData);
           // foreach (var country in countries)
           // {
                _avventoApi.FileDownloads.ReceivedCountryDataCallback(countries, action, anotherSetToCome);
            //}
        }

        private void HandleShareBalanceData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<CSDLimitStructure> shareBalanceDataItems = Utilities.ConvertFromBufferToStructList<CSDLimitStructure>(decompressedData);
           // foreach (var shareBalanceData in shareBalanceDataItems)
           // {
                _avventoApi.FileDownloads.ReceivedShareBalanceDataCallback(shareBalanceDataItems, action, anotherSetToCome);
           // }
        }

        private void HandleCashBalanceData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<CashBalanceStructure> cashBalanceDataItems = Utilities.ConvertFromBufferToStructList<CashBalanceStructure>(decompressedData);
            //foreach (var cashBalanceData in cashBalanceDataItems)
            //{
                _avventoApi.FileDownloads.ReceivedCashBalanceDataCallback(cashBalanceDataItems, action, anotherSetToCome);
           // }
        }

        private void HandleAllocationInstructions(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<AllocationInstructionStructure> allocationInstructions = Utilities.ConvertFromBufferToStructList<AllocationInstructionStructure>(decompressedData);
         //   foreach (var allocationInstruction in allocationInstructions)
          //  {
                _avventoApi.FileDownloads.ReceivedAllocationInstructionCallback(allocationInstructions, action, anotherSetToCome);
           // }
        }

        private void HandleRFQData(byte[] decompressedData, ActionType action,bool anotherSetToCome =false)
        {
            List<RFQStructure> RFQs = Utilities.ConvertFromBufferToStructList<RFQStructure>(decompressedData);
          //  foreach (var RFQ in RFQs)
            //{
                _avventoApi.FileDownloads.ReceivedRFQDataCallback(RFQs, action, anotherSetToCome);
            //}
        }

        private void HandleRFQQuoteData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<RFQQuoteStructure> RFQQuotes = Utilities.ConvertFromBufferToStructList<RFQQuoteStructure>(decompressedData);
            //foreach (var RFQQuote in RFQQuotes)
            //{
                _avventoApi.FileDownloads.ReceivedRFQQuoteDataCallback(RFQQuotes, action, anotherSetToCome);
           // }
        }

        private void HandleUnmatchedDealData(byte[] decompressedData, ActionType action, bool anotherSetToCome =false)
        {
            List<UnmatchedCaptureStructure> captures = Utilities.ConvertFromBufferToStructList<UnmatchedCaptureStructure>(decompressedData);
            //foreach (var capture in captures)
            //{
                _avventoApi.FileDownloads.ReceivedUnmatchedCaptureCallback(captures, action, anotherSetToCome);
           // }
        }

        #endregion FileHandlers

        #region Handlers

        public void HandleFileUpdate(MessageDetails des)
        {
            _avventoApi.FileDownloads.ReceivedFileUpdatesCallback(des, true);
        }
        private void HandleFileDownloadChunk(MessageDetails des)
        {
            FileDownloadHeader fdh = Utilities.OnPacket<FileDownloadHeader>(des.data);
            int dataLength = des.data.Length;
            int length = dataLength - Marshal.SizeOf<FileDownloadHeader>();

            byte[] compressedData = new byte[length];

            if (fileDownloadStore.ContainsKey(fdh.WhichFile))
            {
                Buffer.BlockCopy(des.data, Marshal.SizeOf<FileDownloadHeader>(), fileDownloadStore[fdh.WhichFile], fileDownloadStoreLength[fdh.WhichFile], length);
                fileDownloadStoreLength[fdh.WhichFile] += length;

                compressedData = new byte[fileDownloadStoreLength[fdh.WhichFile]];
            }

            if (!fdh.LastPieceOfChunk)
            {
                if (!fileDownloadStore.ContainsKey(fdh.WhichFile))
                {
                    fileDownloadStore[fdh.WhichFile] = new byte[100000]; 
                    Buffer.BlockCopy(des.data, Marshal.SizeOf<FileDownloadHeader>(), fileDownloadStore[fdh.WhichFile], 0, length);
                    fileDownloadStoreLength.Add(fdh.WhichFile, length);
                }
                return;
            }

            if (!fileDownloadStore.ContainsKey(fdh.WhichFile))
            {
                fileDownloadStore[fdh.WhichFile] = new byte[length];
                Buffer.BlockCopy(des.data, Marshal.SizeOf<FileDownloadHeader>(), fileDownloadStore[fdh.WhichFile], 0, length);
                fileDownloadStoreLength.Add(fdh.WhichFile, length);
            }

            if (!fileDownloadStoreLength.ContainsKey(fdh.WhichFile))
            {
                fileDownloadStoreLength.Add(fdh.WhichFile, length);
            }

            Buffer.BlockCopy(fileDownloadStore[fdh.WhichFile], 0, compressedData, 0, fileDownloadStoreLength[fdh.WhichFile]);

            if (fdh.AnotherSetToCome)
            {
                _avventoApi.SendFileDownloadMessage((FileIdentifier)fdh.WhichFile, Utilities.ConvertFromDelphiString(des.header.UserName), true, DateTime.Today);
            }

            fileDownloadStore.Remove(fdh.WhichFile);
            fileDownloadStoreLength.Remove(fdh.WhichFile);
            HandleFileDownload(compressedData, (FileIdentifier)fdh.WhichFile, (ActionType)fdh.Action, (MessageType)des.header.MessageType, (MessageType)des.header.MessageType == MessageType.MESSAGE_36_START_OF_DAY_DOWNLOAD, fdh.AnotherSetToCome);
        }

        private void HandleFileDownload(byte[] compressedData, FileIdentifier fileIdentifier, ActionType action, MessageType messageType, bool isCompressed, bool anotherSetToCome =false)
        {
            try
            {
                //log.Debug($"Received {fileIdentifier} length: {compressedData.Length}");
                byte[] decompressedData = compressedData;

                if (isCompressed)
                {
                    decompressedData = GZip.Decompress(compressedData);

                    if (decompressedData == null)
                    {
                        return;
                    }
                }
                switch (fileIdentifier)
                {
                    case FileIdentifier.Display:
                        HandleDisplay(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Instruments:
                        HandleInstruments(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.InstrumentType:
                        HandleInstrumentType(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Strikes:
                        HandleStrikes(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.MarkToMarket:
                        HandleMarkToMarket(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Holiday:
                        HandleHolidays(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Member:
                        HandleMember(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.ClearingMember:
                        HandleClearingMember(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Announcements:
                        HandleMarketAnnouncement(decompressedData, action, anotherSetToCome);
                        break;


                    case FileIdentifier.CouponInformation:
                        HandleCouponInformation(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.TradingSession:
                        HandleTradingSession(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Indices:
                        HandleIndices(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.IndicesData:
                        HandleIndicesPriceData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.SharesInIssue:
                        HandleSharesInIssue(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.IndexData:
                        HandleIndexData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.IndexConstituents:
                        HandleIndexConstituentsData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.ActiveOrders:
                        HandleActiveOrders(decompressedData, action, anotherSetToCome);
                        break;
                    case FileIdentifier.AuditActiveOrders:
                        HandleAuditActiveOrders(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.CompletedOrders:
                        HandleCompletedOrders(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Deals:
                        HandleDeals(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Positions:
                        HandlePositionData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.UnmatchedAcknowledgment:
                        HandleTradeCaptureAcknowledgementData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Dealer:
                        HandleDealerData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Client:
                        HandleClientData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Tripartite:
                        HandleTripartiteSetupData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.ClientDetail:
                        HandleClientDetail(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.ContactPerson:
                        HandleContactPersonDetail(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.ContractDates:
                        HandleContractDates(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.Country:
                        HandleCountryData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.CSDLimit:
                        HandleShareBalanceData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.CSDCashBalance:
                        HandleCashBalanceData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.AllocationInstruction:
                        HandleAllocationInstructions(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.RFQ:
                        HandleRFQData(decompressedData, action, anotherSetToCome);
                        break;

                    case FileIdentifier.RFQQuote:
                        HandleRFQQuoteData(decompressedData, action, anotherSetToCome);
                        break;
                    case FileIdentifier.UncompletedOrders:
                        break;
                    default:
                        log.Warn("Unkown FileIdentifier " + fileIdentifier);
                        break;
                }
            }
            catch (Exception ex)
            {
                _avventoApi.IncomingMessages.ReceivedErrorCallback(ex.ToString());
            }
        }

        #endregion Handlers
    }
}
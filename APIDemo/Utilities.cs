using nsConnectionData;
using Stt.Derivatives.Api;
using Stt.Derivatives.Api.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AvventoAPILibrary
{
    public static class Utilities
    {
        public struct EmptyStruct { }

        private static void ConvertToUnmanagedMemory<T>(int structSize, T pRecord, byte[] pBuffer)
        {
            IntPtr input = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(pRecord, input, true);
            Marshal.Copy(input, pBuffer, 0, Math.Min(structSize, pBuffer.Length));
            Marshal.FreeHGlobal(input);
        }

        public static byte[] ToByteArray<T>(T structures)
        {
            byte[] elementBuffer = new byte[Marshal.SizeOf<T>()];

            Utilities.ConvertToUnmanagedMemory(Marshal.SizeOf<T>(), structures, elementBuffer);

            return elementBuffer;
        }

        public static object OnPacket(byte[] packet, Type type)
        {
            GCHandle pinnedPacket = GCHandle.Alloc(packet, GCHandleType.Pinned);
            object msg = Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), type);
            pinnedPacket.Free();
            return msg;
        }

        public static T OnPacket<T>(byte[] packet)
        {
            GCHandle pinnedPacket = GCHandle.Alloc(packet, GCHandleType.Pinned);
            T msg = Marshal.PtrToStructure<T>(pinnedPacket.AddrOfPinnedObject());
            pinnedPacket.Free();
            return msg;
        }

        public static byte[] StripPacketHeader<T>(ConnectionData packet)
        {
            int headerLength = Marshal.SizeOf<T>();
            int dataLength = packet.Data.Length - headerLength;
            byte[] data = new byte[dataLength];
            Buffer.BlockCopy(packet.Data, headerLength, data, 0, dataLength);
            return data;
        }

        public static char[] ConvertToDelphiString(this string csharpString, int bytesToFitInto)
        {
            bytesToFitInto = Math.Min(255, Math.Max(1, bytesToFitInto));

            char[] delphiString = new char[bytesToFitInto];

            if (csharpString == null)
            {
                return delphiString;
            }

            if (csharpString.Length > 0 && bytesToFitInto > 0)
            {
                delphiString[0] = (char)((csharpString.Length < bytesToFitInto) ? Math.Min(255, csharpString.Length) : bytesToFitInto - 1);

                char[] s = csharpString.ToCharArray();

                Array.Copy(s, 0, delphiString, 1, (int)delphiString[0]);
            }

            return delphiString;
        }

        public static string ConvertFromDelphiString(char[] delphiString)
        {
            if (delphiString == null)
            {
                return "";
            }
            int stringLength = (int)delphiString[0];
            int minLength = (stringLength < (delphiString.Length - 1)) ? stringLength : delphiString.Length - 1;
            return new string(delphiString, 1, minLength);
        }

        public static List<T> ConvertFromBufferToStructList<T>(byte[] pBuffer)
        {
            return ConvertFromBufferToStructList<T>(pBuffer, Marshal.SizeOf<T>());
        }

        public static List<T> ConvertFromBufferToStructList<T>(byte[] pBuffer, int structSize)
        {
            int offSet = 0;
            int numRecords = (pBuffer.GetUpperBound(0) + 1) / structSize;
            var records = new List<T>();

            for (var i = 0; i < numRecords; i++)
            {
                offSet = structSize * i;

                IntPtr input = Marshal.AllocHGlobal(structSize);
                Marshal.Copy(pBuffer, offSet, input, structSize);

                records.Add(Marshal.PtrToStructure<T>(input));

                Marshal.FreeHGlobal(input);
            }
            return records;
        }

        public static byte[] EncryptPassword(string Password, byte[] PublicKey)
        {
            //encrypt password with PublicKey
            var cryptoProvider = new RSACryptoServiceProvider();
            string publicKey = Encoding.ASCII.GetString(PublicKey);
            cryptoProvider.FromXmlString(publicKey);
            byte[] encryptedPassword = cryptoProvider.Encrypt(Encoding.ASCII.GetBytes(Password), true);
            //pad the password buffer with the length of the encryptedPassword
            byte[] passwordLength = BitConverter.GetBytes(encryptedPassword.Length);
            byte[] passwordBuffer = new byte[312];
            Buffer.BlockCopy(passwordLength, 0, passwordBuffer, 0, passwordLength.Length);
            Buffer.BlockCopy(encryptedPassword, 0, passwordBuffer, passwordLength.Length, encryptedPassword.Length);
            return passwordBuffer;
        }

        public static byte[] CreateMessageByteArray<T>(MessageType messageType, string userName, T mb)
        {
            var mess = Utilities.ToByteArray<T>(mb);
            var messageHeader = CreateMessageHeader(messageType, userName);
            byte[] header = Utilities.ToByteArray<MITS_Header>(messageHeader);

            byte[] buffer = new byte[MITS_Header.Length + Marshal.SizeOf<T>()];

            Buffer.BlockCopy(header, 0, buffer, 0, MITS_Header.Length);
            Buffer.BlockCopy(mess, 0, buffer, MITS_Header.Length, Marshal.SizeOf<T>());

            return buffer;
        }

        public static MITS_Header CreateMessageHeader(MessageType messageType, string userName)
        {
            MITS_Header header = new MITS_Header();
            header.SequenceNumber = 0;
            header.UserName = Utilities.ConvertToDelphiString(userName, 16);
            header.UserNumber = 0;
            header.MessageTime = (MitsTime)(DateTime.Now);
            header.MessageType = Convert.ToByte(messageType);

            return header;
        }

        public static bool PingServer(string IPAddress)
        {
            var ping = new Ping();
            var options = new PingOptions
            {
                DontFragment = true,
            };
            byte[] buffer = Encoding.ASCII.GetBytes("PING");
            int timeout = 500;
            PingReply reply = ping.Send(IPAddress, timeout, buffer, options);
            if (reply.Status != IPStatus.Success)
            {
                return false;
            }
            return true;
        }
    }
}
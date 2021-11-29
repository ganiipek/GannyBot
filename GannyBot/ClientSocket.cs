using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;

namespace GannyBot
{
    internal class ClientSocket
    {
        Socket _Socket;
        IPEndPoint _IPEndPoint;
        int _BufferSize = 256;

        // Socket işlemleri sırasında oluşabilecek errorları bu enum ile handle edebiliriz.
        SocketError socketError;

        #region Constructor
        public ClientSocket(IPEndPoint ipEndPoint)
        {
            _IPEndPoint = ipEndPoint;
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
        }
        #endregion

        #region Public Methods
        public bool Start()
        {
            try
            {
                _Socket.Connect(_IPEndPoint);
                return true;
            }
            catch (SocketException ex)
            {
                string exception = string.Empty;
                switch (ex.SocketErrorCode)
                {
                    case SocketError.Success:
                        exception = "Success";
                        break;
                    case SocketError.SocketError:
                        exception = "Socket Error";
                        break;
                    case SocketError.Interrupted:
                        exception = "Socket Interrupted";
                        break;
                    case SocketError.AccessDenied:
                        exception = "Access Denied";
                        break;
                    case SocketError.Fault:
                        exception = "Socket Falut";
                        break;
                    case SocketError.InvalidArgument:
                        exception = "Invalid Argument";
                        break;
                    case SocketError.TooManyOpenSockets:
                        exception = "Too Many Open Sockets";
                        break;
                    case SocketError.WouldBlock:
                        exception = "Socket Blocked";
                        break;
                    case SocketError.InProgress:
                        exception = "In progress";
                        break;
                    case SocketError.AlreadyInProgress:
                        exception = "Already in progress";
                        break;
                    case SocketError.NotSocket:
                        exception = "Not Socket";
                        break;
                    case SocketError.DestinationAddressRequired:
                        exception = "Destination Address Required";
                        break;
                    case SocketError.MessageSize:
                        exception = "Message Size Error";
                        break;
                    case SocketError.ProtocolType:
                        exception = "Protocol Type Error";
                        break;
                    case SocketError.ProtocolOption:
                        exception = "Protocol Option Error";
                        break;
                    case SocketError.ProtocolNotSupported:
                        exception = "Protocol Not Supported";
                        break;
                    case SocketError.SocketNotSupported:
                        exception = "Socket Not Supported";
                        break;
                    case SocketError.OperationNotSupported:
                        exception = "Operation Not Supported";
                        break;
                    case SocketError.ProtocolFamilyNotSupported:
                        exception = "Protocol Family Not Supported";
                        break;
                    case SocketError.AddressFamilyNotSupported:
                        exception = "Address Family Not Supported";
                        break;
                    case SocketError.AddressAlreadyInUse:
                        exception = "Adreess Already In Use";
                        break;
                    case SocketError.AddressNotAvailable:
                        exception = "Address Not Avaialble";
                        break;
                    case SocketError.NetworkDown:
                        exception = "Network Down";
                        break;
                    case SocketError.NetworkUnreachable:
                        exception = "Network Unreachable";
                        break;
                    case SocketError.NetworkReset:
                        exception = "Network Reset";
                        break;
                    case SocketError.ConnectionAborted:
                        exception = "Connection Aborted";
                        break;
                    case SocketError.ConnectionReset:
                        exception = "Connection Reset";
                        break;
                    case SocketError.NoBufferSpaceAvailable:
                        exception = "No Buffer Space Available";
                        break;
                    case SocketError.IsConnected:
                        exception = "Connected";
                        break;
                    case SocketError.NotConnected:
                        exception = "Not Connected";
                        break;
                    case SocketError.Shutdown:
                        exception = "Shutdown";
                        break;
                    case SocketError.TimedOut:
                        exception = "Timed Out";
                        break;
                    case SocketError.ConnectionRefused:
                        exception = "Connection Refused";
                        break;
                    case SocketError.HostDown:
                        exception = "Host Down";
                        break;
                    case SocketError.HostUnreachable:
                        exception = "Host Unreachable";
                        break;
                    case SocketError.ProcessLimit:
                        exception = "Process Limit";
                        break;
                    case SocketError.SystemNotReady:
                        exception = "System Not Ready";
                        break;
                    case SocketError.VersionNotSupported:
                        exception = "Version Not Supported";
                        break;
                    case SocketError.NotInitialized:
                        exception = "Not Initialized";
                        break;
                    case SocketError.Disconnecting:
                        exception = "Disconnecting";
                        break;
                    case SocketError.TypeNotFound:
                        exception = "Type Not Found";
                        break;
                    case SocketError.HostNotFound:
                        exception = "Host Not Found";
                        break;
                    case SocketError.TryAgain:
                        exception = "Try Again";
                        break;
                    case SocketError.NoRecovery:
                        exception = "No Recovery";
                        break;
                    case SocketError.NoData:
                        exception = "No Data";
                        break;
                    case SocketError.IOPending:
                        exception = "IO Pending";
                        break;
                    case SocketError.OperationAborted:
                        exception = "Operation Aborted";
                        break;
                    default:
                        exception = "Un Specified Error";
                        break;
                }
                //Output of the inner exception data and reset of communication
                System.Diagnostics.Debug.WriteLine(exception);
                return false;
            }
        }

        public void Close()
        {
            try
            {
                _Socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                _Socket.Close();
            }
        }

        public void SendData(string data)
        {
            using (var ms = new MemoryStream())
            {
                UTF8Encoding enc = new UTF8Encoding();
                byte[] byteData = enc.GetBytes(data);
                _Socket.Send(byteData);
            }
        }

        public dynamic ReceiveData()
        {
            UTF8Encoding enc = new UTF8Encoding();
            Byte[] buff = new Byte[_BufferSize];

            int bytesRec = _Socket.Receive(buff);

            string message = Encoding.UTF8.GetString(buff, 0, bytesRec);
            dynamic parsedJson = JsonConvert.DeserializeObject(message);

            return parsedJson;
        }
        #endregion
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using static Core.Enums;
using static Dictionaries;

public class Client
{
    private const int DataBufferSize = 4096;

    public readonly int _id;
    public readonly Tcp TcpInstance;
    public readonly Udp UdpInstance;
    private readonly ClientData _data;

    public Client(int clientId)
    {
        _id = clientId;
        TcpInstance = new Tcp(_id, this);
        UdpInstance = new Udp(_id);
        _data = new ClientData(clientId);
        DataManager.ConnectedUsers.Add(_data);
    }

    public class Tcp
    {
        public TcpClient Socket;
        private readonly int _id;
        private NetworkStream _stream;
        private Packet _receivedData;
        private byte[] _receiveBuffer;

        public Tcp(int id, Client myClient)
        {
            this._id = id;
        }

        /// <summary>Initializes the newly connected client's TCP-related info.</summary>
        /// <param name="socket">The TcpClient instance of the newly connected client.</param>
        public void Connect(TcpClient socket)
        {
            Socket = socket;
            Socket.ReceiveBufferSize = DataBufferSize;
            Socket.SendBufferSize = DataBufferSize;

            _stream = Socket.GetStream();

            _receivedData = new Packet();
            _receiveBuffer = new byte[DataBufferSize];

            _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null); 
            Debug.Log($"Begin Read");
            ServerSend.Welcome(_id, "Welcome to the server!");
        }

        /// <summary>Sends data to the client via TCP.</summary>
        /// <param name="packet">The packet to send.</param>
        public void SendData(Packet packet)
        {
            try
            {
                if (Socket != null)
                {
                    _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null,
                        null); // Send data to appropriate client
                }
            }
            catch
            {
                //Debug.Log($"Error sending data to player {id} via TCP: {_ex}");
            }
        }

        /// <summary>Reads incoming data from the stream.</summary>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var byteLength = _stream.EndRead(result);
                if (byteLength <= 0)
                {
                    Server.Clients[_id].Disconnect();
                    return;
                }

                var data = new byte[byteLength];
                Array.Copy(_receiveBuffer, data, byteLength);

                _receivedData.Reset(HandleData(data)); // Reset receivedData if all data was handled
                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                //
                ServerConsoleWriter.WriteLine($"Error receiving TCP data: {ex}");
                Server.Clients[_id].Disconnect();
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="data">The recieved data.</param>
        private bool HandleData(byte[] data)
        {
            var packetLength = 0;

            _receivedData.SetBytes(data);

            if (_receivedData.UnreadLength() >= 4)
            {
                // If client's received data contains a packet
                packetLength = _receivedData.ReadInt();
                if (packetLength <= 0)
                {
                    // If packet contains no data
                    return true; // Reset receivedData instance to allow it to be reused
                }
            }

            while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
            {
                // While packet contains data AND packet data length doesn't exceed the length of the packet we're reading
                var packetBytes = _receivedData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var packet = new Packet(packetBytes))
                    {
                        var packetId = packet.ReadInt();
                        Server.PacketHandlers[packetId](_id, packet); // Call appropriate method to handle the packet
                    }
                });

                packetLength = 0; // Reset packet length
                if (_receivedData.UnreadLength() >= 4)
                {
                    // If client's received data contains another packet
                    packetLength = _receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        // If packet contains no data
                        return true; // Reset receivedData instance to allow it to be reused
                    }
                }
            }

            return packetLength <= 1;
        }

        /// <summary>Closes and cleans up the TCP connection.</summary>
        /// <param name="initialize">This is called after the disconnection process has ended. To initialize a new client data for the same user account.</param>
        /// <param name="userIfNeed">PASS THE USER TO THE INITIALIZE</param>
        /// <param name="lastIdIfNeed">PASS THE LAST PLAYER ID TO THE INITIALIZE</param>
        public void Disconnect(Action<string, int> initialize, string userIfNeed = "", int lastIdIfNeed = 0)
        {
            /*Here we disconnect from the server*/
            //We check if the client dictionaries contains a key for this id, in which case we proceed with following steps
            /*
             * Execute on main thread so it is executed immediately
             * Check if player is in lobby
             * this way next check can be skipped since ForceEndLobby sets every player in that lobby match id to 0 
             * if its in lobby we end the lobby and disconnect the player 
             * Check if player is in match
             */
            var currentData = Server.Clients[_id]._data;
            ServerSend.ExitGame(_id);
            if (currentData.currentState == ClientCurrentState.STATE_LOBBY)
            {
                if (DataManager.InProgressMatches.ContainsKey(currentData.MatchId))
                {
                    DataManager.InProgressMatches[currentData.MatchId].Lobby
                        .ForceEndLobby(LobbyEndReason.PlayerDisconnected, _id);
                }
            }
            if (currentData.MatchId != 0)
            {
                if(!DataManager.InProgressMatches.ContainsKey(currentData.MatchId)) return;
                DataManager.InProgressMatches[currentData.MatchId].InGameManager
                    .RemovePlayerFromGame(_id); //Remove player from all connected clients 
            }
            MonoDebug.Instance.DisconnectUser(currentData.Username);
            currentData.MatchId = 0;
            var user = currentData.Username;
            HandleMatchMaking.RemoveFromQueue(currentData); //Remove from que if needed
            DataManager.ConnectedUsers.Remove(currentData);
            initialize?.Invoke(userIfNeed, lastIdIfNeed);
            Debug.Log($"Removed {user} from Dictionary");
            Socket?.Close();
            _stream = null;
            _receivedData = null;
            _receiveBuffer = null;
            Socket = null;
        }
    }

    public class Udp
    {
        public IPEndPoint EndPoint;

        private readonly int id;

        public Udp(int id)
        {
            this.id = id;
        }

        /// <summary>Initializes the newly connected client's UDP-related info.</summary>
        /// <param name="endPoint">The IPEndPoint instance of the newly connected client.</param>
        public void Connect(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        /// <summary>Sends data to the client via UDP.</summary>
        /// <param name="packet">The packet to send.</param>
        public void SendData(Packet packet)
        {
            Server.SendUdpData(EndPoint, packet);
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="packetData">The packet containing the recieved data.</param>
        public void HandleData(Packet packetData)
        {
            var packetLength = packetData.ReadInt();
            var packetBytes = packetData.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (var packet = new Packet(packetBytes))
                {
                    int packetId = packet.ReadInt();
                    Server.PacketHandlers[packetId](id, packet); // Call appropriate method to handle the packet
                }
            });
        }

        /// <summary>Cleans up the UDP connection.</summary>
        public void Disconnect()
        {
            EndPoint = null;
        }
    }
    
    public void RequestLogin(string username, string password)
    {
        NetworkManager.Instance.StartCoroutine(LoginStart(username, password));
    }

    private IEnumerator LoginStart(string user, string pass)
    {
        var form = new WWWForm();
        form.AddField("user", user);
        form.AddField("pass", pass);
        var www = new WWW(Constants.SqlNameServer + "login.php", form);
        yield return www;
        if (!string.IsNullOrEmpty(www.text) && www.text[0] == '0') 
        {
            var dbId = int.Parse(www.text.Split('\t')[1]);
            var lastUser = DataManager.GetClientData(user);
            if (lastUser == null)
            {
                Initialize(user, dbId);
            }
            else
            {
                lastUser.currentOwner.Disconnect(Initialize, user, dbId);
            }
        }
        else
        {
            ServerSend.LoginResult(_id, false, www.text, -9);
        }
    }
    

    private void Initialize(string user, int dataBaseId)
    {
        var newData = Server.Clients[_id]._data;
        newData.SetNewUser(user);
        newData.currentOwner = Server.Clients[_id];
        DataManager.ConnectedUsers.Add(newData);
        ServerSend.LoginResult(_id, true, "noError", dataBaseId);
        ServerConsoleWriter.WriteUserLog(user, $"New log started {user}");
        MonoDebug.Instance.WriteNewUser(user);
        Server.Connection(_id, _data);

       
    }
    // ReSharper disable Unity.PerformanceAnalysis
    private void Disconnect(Action<string, int> initialize = null, string username = "", int id = 0)
    {
        TcpInstance.Disconnect(initialize, username, id);
        UdpInstance.Disconnect();
    }
}
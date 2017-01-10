﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetworkEmulation.Editor.Element;
using NetworkEmulation.Log;
using NetworkUtilities;
using NetworkUtilities.Serialization;

namespace NetworkEmulation.Network {
    public class CableCloud : LogObject {
        private readonly SerializableDictionary<SocketNodePortPair, SocketNodePortPair> _linkDictionary;
        private readonly SerializableDictionary<int, TcpClient> _nodesTcpClients;
        private UdpClient _connectionUdpClient;

        public CableCloud() {
            _nodesTcpClients = new SerializableDictionary<int, TcpClient>();
            _linkDictionary = new SerializableDictionary<SocketNodePortPair, SocketNodePortPair>();

            Start();
        }

        public bool Online { get; private set; }

        private void Start() {
            var ipEndPoint = new IPEndPoint(IPAddress.Any, 10000);
            _connectionUdpClient = new UdpClient(ipEndPoint);

            ListenForConnectionRequests();
        }

        private void ListenForConnectionRequests() {
            Task.Run(async () => {
                using (_connectionUdpClient) {
                    Online = true;
                    while (true) {
                        var receivedData = await _connectionUdpClient.ReceiveAsync();
                        EstabilishNodeConnection((int) BinarySerializer.Deserialize(receivedData.Buffer));
                    }
                }
            });
        }

        private void EstabilishNodeConnection(int port) {
            var nodeTcpClient = new TcpClient();
            try {
                nodeTcpClient.Connect(IPAddress.Loopback, port);
                _nodesTcpClients.Add(port, nodeTcpClient);
                UpdateState("Connected to Node on TCP port: " + port);
                ListenForNodeMessages(nodeTcpClient, port);
            }
            catch (SocketException e) {
                UpdateState(e.Message);
            }
        }

        private void ListenForNodeMessages(TcpClient nodeTcpClient, int inputPort) {
            Task.Run(async () => {
                using (var ns = nodeTcpClient.GetStream()) {
                    var buffer = new byte[CableCloudMessage.MaxByteBufferSize];

                    while (true) {
                        var bytesRead = await ns.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead <= 0)
                            break;

                        var cableCloudMessage = CableCloudMessage.Deserialize(buffer);
                        UpdateState("Node [" + inputPort + "] from ATM port: " + cableCloudMessage.PortNumber + " - " +
                                    cableCloudMessage.Data.Length + " bytes recieved.");
                        var input = new SocketNodePortPair(cableCloudMessage.PortNumber, inputPort);

                        SocketNodePortPair output = null;

                        try {
                            output = LookUpLinkDictionary(input);
                            cableCloudMessage.PortNumber = output.NodePortNumber;

                            PassCableCloudMessage(cableCloudMessage, output.SocketPortNumber);
                        }
                        catch (KeyNotFoundException) {
                            UpdateState("Node [" + input.SocketPortNumber + "] to ATM port: " +
                                        cableCloudMessage.PortNumber +
                                        " - no avaliable link.");
                        }
                        catch (Exception) {
                            if (output != null) _nodesTcpClients.Remove(output.SocketPortNumber);
                            UpdateState("Node [" + input.SocketPortNumber + "] to ATM port: " +
                                        cableCloudMessage.PortNumber +
                                        " - could not connect.");
                        }
                    }
                }
            });
        }

        private SocketNodePortPair LookUpLinkDictionary(SocketNodePortPair input) {
            return _linkDictionary[input];
        }

        private void PassCableCloudMessage(CableCloudMessage cableCloudMessage, int outputPort) {
            var tcpClient = _nodesTcpClients[outputPort];

            SendBytes(cableCloudMessage.Serialize(), tcpClient);
            UpdateState("Node [" + outputPort + "] to   ATM port: " + cableCloudMessage.PortNumber + " - " +
                        cableCloudMessage.Data.Length + " bytes sent.");
        }

        private void SendBytes(byte[] data, TcpClient tcpClient) {
            tcpClient.GetStream().Write(data, 0, data.Length);
        }

        public void AddLink(SocketNodePortPair key, SocketNodePortPair value) {
            _linkDictionary.Add(key, value);
        }

        public void AddLink(Link link) {
            var key = link.Parameters.InputNodePortPair;
            var value = link.Parameters.OutputNodePortPair;

            _linkDictionary.Add(key, value);
        }

        public void RemoveLink(SocketNodePortPair key) {
            _linkDictionary.Remove(key);
        }

        public void Dispose() {
            UpdateState("Cable Cloud shutting down.");
            _connectionUdpClient.Close();
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using NetworkUtilities.Utilities;

namespace NetworkUtilities.ControlPlane {
    public class Directory : ControlPlaneElement {
        private static readonly Dictionary<string, NetworkAddress> _clientAddressDictionary =
            new Dictionary<string, NetworkAddress>();

        private static readonly Dictionary<string, SubnetworkPointPool> _snppDictionary =
            new Dictionary<string, SubnetworkPointPool>();

        public Directory(NetworkAddress localAddress) : base(localAddress, ControlPlaneElementType.Directory) {
        }

        public override void ReceiveMessage(SignallingMessage message) {
            base.ReceiveMessage(message);

            switch (message.Operation) {
                case OperationType.DirectoryAddressRequest:
                    SendDirectoryAddressRequestResponse(message);
                    break;

                case OperationType.DirectoryNameRequest:
                    SendDirectoryNameRequestResponse(message);
                    break;

                case OperationType.DirectorySnppRequest:
                    SendDirectorySnppRequestResponse(message);
                    break;
            }
        }

        private void SendDirectoryAddressRequestResponse(SignallingMessage message) {
            var clientNames = (string[]) message.Payload;

            NetworkAddress[] clientAddresses = null;
            try {
                var clientAddressA = _clientAddressDictionary[clientNames[0]];
                var clientAddressZ = _clientAddressDictionary[clientNames[1]];
                clientAddresses = new[] {clientAddressA, clientAddressZ};
                OnUpdateState($"[FOUND] {clientNames[1]}");
            }
            catch (KeyNotFoundException) {
                OnUpdateState($"[NOT_FOUND] {clientNames[1]}");
            }


            var directioryResponse = message;
            directioryResponse.Operation = OperationType.DirectoryAddressRequest;
            directioryResponse.Payload = clientAddresses;
            directioryResponse.DestinationAddress = message.SourceAddress;
            directioryResponse.DestinationControlPlaneElement =
                ControlPlaneElementType.NCC;

            SendMessage(directioryResponse);
            EndSession(message.SessionId);
        }

        private void SendDirectoryNameRequestResponse(SignallingMessage message) {
            var clientAddress = (NetworkAddress[]) message.Payload;

            var clientNameA = _clientAddressDictionary.FirstOrDefault(x => x.Value == clientAddress[0]).Key;
            var clientNameZ = _clientAddressDictionary.FirstOrDefault(x => x.Value == clientAddress[1]).Key;

            string[] clientName = {clientNameA, clientNameZ};

            var directioryResponse = message;
            directioryResponse.Operation = OperationType.DirectoryNameRequest;
            directioryResponse.Payload = clientName;
            directioryResponse.DestinationAddress = message.SourceAddress;
            directioryResponse.DestinationControlPlaneElement =
                ControlPlaneElementType.NCC;

            SendMessage(directioryResponse);
            EndSession(message.SessionId);
        }

        private void SendDirectorySnppRequestResponse(SignallingMessage message) {
            var clientNames = (string[]) message.Payload;

            SubnetworkPointPool[] snpp = null;
            try {
                var snmpA = _snppDictionary[clientNames[0]];
                var snmpZ = _snppDictionary[clientNames[1]];

                snpp = new[] {snmpA, snmpZ};
                OnUpdateState($"[FOUND] {clientNames[1]}");
            }
            catch (KeyNotFoundException e) {
                OnUpdateState($"[NOT_FOUND] {clientNames[1]}");
            }

            var directioryResponse = message;
            directioryResponse.Operation = OperationType.DirectorySnppRequest;
            directioryResponse.Payload = snpp;
            directioryResponse.DestinationAddress = message.SourceAddress;
            directioryResponse.DestinationControlPlaneElement =
                ControlPlaneElementType.NCC;

            SendMessage(directioryResponse);
            EndSession(message.SessionId);
        }

        public void UpdateDirectory(string clientName, NetworkAddress clientNetworkAddress, SubnetworkPointPool networkNodeSnpp) {
            _clientAddressDictionary.Add(clientName, clientNetworkAddress);
            _snppDictionary.Add(clientName, networkNodeSnpp);
            OnUpdateState($"[ADDED] Client {clientName} is connected to {networkNodeSnpp}");
        }
    }
}
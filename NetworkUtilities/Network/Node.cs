﻿using System.Collections.Generic;
using NetworkUtilities.ControlPlane;
using NetworkUtilities.Log;
using NetworkUtilities.Serialization;

namespace NetworkUtilities.Network {
    public abstract class Node : LogObject {
        public NetworkAddress NetworkAddress { get; }

        public int CableCloudDataPort { get; protected set; }
        protected int CableCloudListeningPort;
        private readonly ConnectionComponent _dataPlaneConnectionComponent;

        public int PathComputationServerDataPort { get; protected set; }
        protected int PathComputationServerListeningPort;
        private readonly ConnectionComponent _controlPlaneConnectionComponent;



        public bool Online => _dataPlaneConnectionComponent.Online && _controlPlaneConnectionComponent.Online;
   

        protected Node(NetworkAddress networkAddress, string ipAddress, int cableCloudListeningPort, int pathComputationServerListeningPort) {

            NetworkAddress = networkAddress;

            CableCloudListeningPort = cableCloudListeningPort;
            _dataPlaneConnectionComponent = new ConnectionComponent(NetworkAddress, ipAddress, cableCloudListeningPort);
            _dataPlaneConnectionComponent.ObjectReceived += OnCableCloudMessageReceived;
            _dataPlaneConnectionComponent.Initialize();

            PathComputationServerListeningPort = pathComputationServerListeningPort;
            _controlPlaneConnectionComponent = new ConnectionComponent(NetworkAddress, ipAddress, pathComputationServerListeningPort);
            _controlPlaneConnectionComponent.ObjectReceived += OnSignallingMessageReceived;
            _controlPlaneConnectionComponent.Initialize();
        }

        private void OnCableCloudMessageReceived(object sender, object receivedObject) {
            var cableCloudMessage = (CableCloudMessage) receivedObject;
            Receive(cableCloudMessage);
        }

        protected abstract void Receive(CableCloudMessage cableCloudMessage);

        protected void Send(CableCloudMessage cableCloudMessage) {
            _dataPlaneConnectionComponent.SendObject(cableCloudMessage);
        }

        public List<AtmCell> ExtractAtmCells(CableCloudMessage cableCloudMessage) {
            var atmCells = BinarySerializer.Deserialize(cableCloudMessage.Data) as List<AtmCell>;
            return atmCells?.FindAll(cell => cell.Valid());
        }

        private void OnSignallingMessageReceived(object sender, object receivedObject) {
            var signallingMessage = (SignallingMessage)receivedObject;
            Receive(signallingMessage);
        }

        protected abstract void Receive(SignallingMessage signallingMessage);

        protected void Send(SignallingMessage signallingMessage) {
            _controlPlaneConnectionComponent.SendObject(signallingMessage);
        }
    }
}
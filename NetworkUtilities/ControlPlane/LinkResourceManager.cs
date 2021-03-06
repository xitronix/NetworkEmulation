﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NetworkUtilities.Network;
using NetworkUtilities.Utilities;

namespace NetworkUtilities.ControlPlane {
    public class LinkResourceManager : ControlPlaneElement {
        public delegate void CommutationHandler(object sender, CommutationHandlerArgs args);

        private readonly List<Link> _nodeLinks;
        private readonly Dictionary<NetworkAddress, SubnetworkPointPool> _clientInSnpps;
        private readonly Dictionary<NetworkAddress, SubnetworkPointPool> _clientOutSnpps;
        private readonly Dictionary<Link, List<SubnetworkPoint>> _usedSubnetworkPoints;
        private readonly Dictionary<UniqueId, SubnetworkPointPortPair> _recentSnpPortPairs;

        public LinkResourceManager(NetworkAddress networkAddress) : 
            base(networkAddress, ControlPlaneElementType.LRM) {

            _nodeLinks = new List<Link>();
            _usedSubnetworkPoints = new Dictionary<Link, List<SubnetworkPoint>>();
            _clientInSnpps = new Dictionary<NetworkAddress, SubnetworkPointPool>();
            _clientOutSnpps = new Dictionary<NetworkAddress, SubnetworkPointPool>();
            _recentSnpPortPairs = new Dictionary<UniqueId, SubnetworkPointPortPair>();
        }

        public override void ReceiveMessage(SignallingMessage message) {
            base.ReceiveMessage(message);

            switch (message.Operation) {
                case OperationType.LinkConnectionAllocation:
                    HandleSnpLinkConnectionAllocation(message);
                    break;

                case OperationType.SNPNegotiation:
                    if (message.Payload is SubnetworkPointPool[])
                        HandleSnpNegotiation(message);
                    else
                        HandleSnpNegotiationResponse(message);
                    break;

                case OperationType.LinkConnectionDeallocation:
                    HandleSnpLinkConnectionDeallocation(message);
                    break;

                case OperationType.SNPRelease:
                    if (message.SourceControlPlaneElement.Equals(ControlPlaneElementType.LRM))
                        HandleSnpRelease(message);
                    else
                        HandleSnpReleaseResponse(message);
                    break;

                case OperationType.Configuration:
                    HandleConfiguration(message);
                    break;
            }
        }

        private void HandleSnpLinkConnectionAllocation(SignallingMessage message) {
            var snpps = (SubnetworkPointPool[]) message.Payload;
            OnUpdateState($"[RECEIVED_SNPP] {snpps[0]}---{snpps[1]}");

            if (_clientOutSnpps.ContainsKey(message.DestinationClientAddress)) {
                OnUpdateState("[DESTINATION_CLIENT_LOCATED]");

                var snppOut = snpps[1];
                var linkOut = FindLinkByBegin(snppOut);
                var snpOut = GenerateSnp(linkOut, message.DemandedCapacity);
                var pairOut = new SubnetworkPointPortPair(snpOut, snppOut.Id);

                _recentSnpPortPairs.Add(message.SessionId, pairOut);

                message.Payload = pairOut;
                SendSnpLinkConnectionAllocation(message);
            }
            else {
                SendSnpNegotiation(message);
            }
        }

        private void HandleSnpNegotiation(SignallingMessage message) {
            var snpps = (SubnetworkPointPool[]) message.Payload;

            OnUpdateState($"[RECEIVED_SNPP] {snpps[0]}---{snpps[1]}");

            var pairOut = _recentSnpPortPairs[message.SessionId];

            var linkIn = FindLinkByEnd(snpps[1]);
            var snpIn = GenerateSnp(linkIn, message.DemandedCapacity);
            var portIn = snpps[1].Id;
            var pairIn = new SubnetworkPointPortPair(snpIn, portIn);

            InsertRow(new []{pairIn, pairOut});

            SendSnpNegotiationResponse(message, pairIn);
        }

        private void SendSnpNegotiationResponse(SignallingMessage message, SubnetworkPointPortPair subnetworkPointPortPair) {
            message.Payload = new object[]
                {(SubnetworkPointPool[]) message.Payload, subnetworkPointPortPair};
            message.DestinationAddress = message.SourceAddress;
            message.DestinationControlPlaneElement = ControlPlaneElementType.LRM;

            SendMessage(message);
        }

        private void HandleSnpNegotiationResponse(SignallingMessage message) {
            var payload = (object[])message.Payload;

            var snpps = (SubnetworkPointPool[])payload[0];
            var nextPairOut = (SubnetworkPointPortPair)payload[1];
            var pairOut = new SubnetworkPointPortPair(nextPairOut.SubnetworkPoint, snpps[0].Id);

            if (_clientInSnpps.ContainsKey(message.SourceClientAddress)) {
                OnUpdateState("[SOURCE_CLIENT_LOCATED]");
                var snppIn = _clientInSnpps[message.SourceClientAddress];
                var linkIn = FindLinkByEnd(snppIn);
                var snpIn = GenerateSnp(linkIn, message.DemandedCapacity);
                var pairIn = new SubnetworkPointPortPair(snpIn, snppIn.Id);
                InsertRow(new []{pairIn, pairOut});

                message.Payload = pairIn;
            }
            else {
                _recentSnpPortPairs.Add(message.SessionId, pairOut);

                message.Payload = pairOut;
            }

            
            SendSnpLinkConnectionAllocation(message);
        }

        private void HandleSnpLinkConnectionDeallocation(SignallingMessage message) {

        }

        private void HandleConfiguration(SignallingMessage message) {
            var link = (Link) message.Payload;

            _nodeLinks.Add(link);
            _usedSubnetworkPoints.Add(link, new List<SubnetworkPoint>());

            NetworkAddress clientAddress;
            SubnetworkPointPool clientConnectedLocalSnpp;
            if (link.EndSubnetworkPointPool.NodeAddress.Equals(LocalAddress)) {
                OnUpdateState($"[LINK] [IN] {link}");
                if (link.IsClientLink) {
                    clientAddress = link.BeginSubnetworkPointPool.NodeAddress;
                    clientConnectedLocalSnpp = link.EndSubnetworkPointPool;
                    _clientInSnpps.Add(clientAddress, clientConnectedLocalSnpp);
                }
                else {
                    SendLocalTopology(link);
                }
            }
            else {
                OnUpdateState($"[LINK] [OUT] {link}");
                if (link.IsClientLink) {
                    clientAddress = link.EndSubnetworkPointPool.NodeAddress;
                    clientConnectedLocalSnpp = link.BeginSubnetworkPointPool;
                    _clientOutSnpps.Add(clientAddress, clientConnectedLocalSnpp);
                }
                else {
                    SendLocalTopology(link);
                }
            }

            EndSession(message.SessionId);
        }

        private SubnetworkPoint GenerateSnp(Link link, int demandedCapacity) {
            while (true) {
                SubnetworkPoint snp = SubnetworkPoint.GenerateRandom(demandedCapacity);
                OnUpdateState($"[GENERATED_SNP] {link}\n" +
                              $"                                                {snp}");
                if (!_usedSubnetworkPoints[link].Contains(snp)) {
                    _usedSubnetworkPoints[link].Add(snp);
                    link.ReserveCapacity(demandedCapacity);
                    OnUpdateState($"[MODIFIED_LINK] {link}");
                    if (!link.IsClientLink) {
                        SendLocalTopology(link);
                    }
                    return snp;
                }
            }
        }

        private void HandleSnpRelease(SignallingMessage message) {

        }

        private void HandleSnpReleaseResponse(SignallingMessage message) {
            var success = false;



            if (success) {
                
            }
            SendSnpLinkConnectionDeallocation(message);
        }

        private void SendSnpNegotiation(SignallingMessage message) {
            var snpps = (SubnetworkPointPool[]) message.Payload;
            message.Operation = OperationType.SNPNegotiation;
            message.DestinationAddress = snpps[1].NodeAddress;
            message.DestinationControlPlaneElement = ControlPlaneElementType.LRM;

            SendMessage(message);
        }

        private void SendLocalTopology(Link link) {
            var message = new SignallingMessage {
                Operation = OperationType.LocalTopology,
                DestinationAddress = LocalAddress.GetParentsAddress(),
                DestinationControlPlaneElement = ControlPlaneElementType.RC,
                Payload = link
            };
            
            SendMessage(message);

        }

        private void SendSnpLinkConnectionAllocation(SignallingMessage message) {
            message.DestinationAddress = LocalAddress;
            message.DestinationControlPlaneElement= ControlPlaneElementType.CC;
            message.Operation = OperationType.LinkConnectionAllocation;

            SendMessage(message);
        }

        private void SendSnpLinkConnectionDeallocation(SignallingMessage message) {
            
        }

        private Link FindLinkByBegin(SubnetworkPointPool snpp) {
            return _nodeLinks.Find(l => l.BeginSubnetworkPointPool.Equals(snpp));
        }
        private Link FindLinkByEnd(SubnetworkPointPool snpp) {
            return _nodeLinks.Find(l => l.EndSubnetworkPointPool.Equals(snpp));
        }

        private void InsertRow(SubnetworkPointPortPair[] r) {


            OnUpdateState($"[COMMUTATION_TABLE_UPDATE] VPI: {r[0].SubnetworkPoint.Vpi}->{r[1].SubnetworkPoint.Vpi}," +
                                                    $" VCI: {r[0].SubnetworkPoint.Vci}->{r[1].SubnetworkPoint.Vci}," +
                                                    $" Port: {r[0].Port}->{r[1].Port}");

            var rowToAdd = new CommutationTableRow(r[0].SubnetworkPoint.Vpi,
                                                   r[0].SubnetworkPoint.Vci,
                                                   r[0].Port,
                                                   r[1].SubnetworkPoint.Vpi,
                                                   r[1].SubnetworkPoint.Vci,
                                                   r[1].Port);

            OnCommutationCommand(new CommutationHandlerArgs(rowToAdd));
        }

        public event CommutationHandler CommutationCommand;
        protected virtual void OnCommutationCommand(CommutationHandlerArgs args) {
            CommutationCommand?.Invoke(this, args);
        }
    }

    public class CommutationHandlerArgs {
        public CommutationHandlerArgs(CommutationTableRow commutationTableRow) {
            CommutationTableRow = commutationTableRow;
        }

        public CommutationTableRow CommutationTableRow { get; private set; }
    }
}
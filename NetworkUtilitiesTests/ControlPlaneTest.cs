﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUtilities;
using NetworkUtilities.ControlPlane;
using NetworkUtilities.GraphAlgorithm;

namespace NetworkUtilitiesTests {
    [TestClass]
    public class ControlPlaneTest {
        private readonly Dictionary<NetworkAddress, ControlPlaneElement> _controlPlaneElements =
            new Dictionary<NetworkAddress, ControlPlaneElement>();

        [TestMethod]
        public void TestMessageTransfer() {
            var directory = new Directory(NameServer.Address);
            _controlPlaneElements.Add(NameServer.Address, directory);

            var ncc1Address = new NetworkAddress("1");
            var ncc1 = new NetworkCallController(ncc1Address);
            _controlPlaneElements.Add(ncc1Address, ncc1);

            var ncc2Address = new NetworkAddress("2");
            var ncc2 = new NetworkCallController(ncc2Address);
            _controlPlaneElements.Add(ncc2Address, ncc2);

            var cpccAAddress = new NetworkAddress("1.1");
            var cpccA = new CallingPartyCallController(cpccAAddress);
            _controlPlaneElements.Add(cpccAAddress, cpccA);
            //directory.UpdateDirectory("Abacki", cpccAAddress, new SubnetworkPointPool(1));

            var cpccBAddress = new NetworkAddress("2.2");
            var cpccB = new CallingPartyCallController(cpccBAddress);
            _controlPlaneElements.Add(cpccBAddress, cpccB);
            //directory.UpdateDirectory("Babacki", cpccBAddress, new SubnetworkPointPool(2));

            var ccAddress = new NetworkAddress("0.1");
            var cc = new ConnectionController(ccAddress);
            _controlPlaneElements.Add(ccAddress, cc);

            ncc1.MessageToSend += PassMessage;
            ncc2.MessageToSend += PassMessage;
            cc.MessageToSend += PassMessage;
            cpccA.MessageToSend += PassMessage;
            cpccB.MessageToSend += PassMessage;
            directory.MessageToSend += PassMessage;

            cpccA.SendCallRequest("Abacki", "Babacki", 20);
        }

        private void PassMessage(object sender, SignallingMessage message) {
            var destination = _controlPlaneElements[message.DestinationAddress];
            destination.ReceiveMessage(message);
        }
    }
}
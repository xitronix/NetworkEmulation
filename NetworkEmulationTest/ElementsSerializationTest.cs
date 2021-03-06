﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkEmulation.Workplace.Element;
using NetworkUtilities.Network;
using NetworkUtilities.Network.ClientNode;
using NetworkUtilities.Network.NetworkNode;
using NetworkUtilities.Utilities;
using NetworkUtilities.Utilities.Serialization;

namespace NetworkEmulationTest {
    [TestClass]
    public class ElementsSerializationTest {
        [TestMethod]
        public void SerializeNetworkNodeSerializableParametersTest() {
            var networkNodeSerializableParameters = new NetworkNodeModel {
                IpAddress = "127.0.0.1",
                CableCloudListeningPort = 10000,
                NetworkManagmentSystemListeningPort = 6666,
                NumberOfPorts = 5
            };
            var serialized = XmlSerializer.Serialize(networkNodeSerializableParameters);
        }

        [TestMethod]
        public void SerializeClientNodePictureBox() {
            var clientNodePictureBox = new ClientNodeView {
                Parameters = new ClientNodeModel {
                    ClientName = "Janusz",
                    CableCloudListeningPort = 10000,
                    IpAddress = "localhost"
                }
            };

            var serialized = XmlSerializer.Serialize(clientNodePictureBox);

            var deserialized = new ClientNodeView();
            XmlSerializer.Deserialize(deserialized, serialized);
        }

        [TestMethod]
        public void SerializeNetworkNodePictureBox() {
            var networkNodePictureBox = new NetworkNodeView {
                Parameters = new NetworkNodeModel {
                    CableCloudListeningPort = 10000,
                    IpAddress = "localhost",
                    NetworkManagmentSystemListeningPort = 6666,
                    NumberOfPorts = 8
                }
            };

            var serialized = XmlSerializer.Serialize(networkNodePictureBox);

            var deserialized = new NetworkNodeView();
            XmlSerializer.Deserialize(deserialized, serialized);
        }

        [TestMethod]
        public void SerializeLink() {
            var link = new LinkView {
                Parameters = new LinkModel {
                    BeginNodeViewId = UniqueId.Generate(),
                    EndNodeViewId = UniqueId.Generate(),
                    InputNodePortPair = new NetworkAddressNodePortPair(new NetworkAddress(4), 3),
                    OutputNodePortPair = new NetworkAddressNodePortPair(new NetworkAddress(6), 5)
                }
            };

            var serialized = XmlSerializer.Serialize(link);

            var deserialized = new LinkView();
            XmlSerializer.Deserialize(deserialized, serialized);
        }

        [TestMethod]
        public void UniqueIdTest() {
            var expected = UniqueId.Generate();
            var serialized = XmlSerializer.Serialize(expected);

            var actual = XmlSerializer.Deserialize(serialized, typeof(UniqueId));
            var notExpected = UniqueId.Generate();

            Assert.AreEqual(expected, actual);
            Assert.AreNotEqual(notExpected, actual);
        }
    }
}
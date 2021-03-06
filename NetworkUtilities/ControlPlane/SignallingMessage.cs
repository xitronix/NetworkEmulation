﻿using System;
using NetworkUtilities.Utilities;

namespace NetworkUtilities.ControlPlane {
    [Serializable]
    public class SignallingMessage {
        //Should be used only in CPCC! In other cases pass received SignallingMessage.
        public SignallingMessage() {
            SessionId = UniqueId.Generate();
        }

        public UniqueId SessionId { get; private set; }

        public NetworkAddress DestinationAddress { get; set; }
        public ControlPlaneElementType DestinationControlPlaneElement { get; set; }
        public NetworkAddress SourceAddress { get; set; }
        public ControlPlaneElementType SourceControlPlaneElement { get; set; }
        public OperationType Operation { get; set; }
        public NetworkAddress SourceClientAddress { get; set; }
        public NetworkAddress DestinationClientAddress { get; set; }
        public string SourceClientName { get; set; }
        public string DestinationClientName { get; set; }
        public int DemandedCapacity { get; set; }

        public object Payload { get; set; }

        public override string ToString() {
            if (SourceAddress == null) {
                return $"NMS->{DestinationAddress}.{DestinationControlPlaneElement} {Operation}";
            }
            return
                $"{SourceAddress}.{SourceControlPlaneElement}->{DestinationAddress}.{DestinationControlPlaneElement} {Operation}";
        }
    }
}
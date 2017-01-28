﻿using System;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using XmlSerializer = NetworkUtilities.Serialization.XmlSerializer;

namespace NetworkUtilities {
    [Serializable]
    public class NetworkAddress : IXmlSerializable {
        private const char Separator = '.';
        private string _value;

        public int Levels => _value.Split(Separator).Length;

        private NetworkAddress() {}

        public NetworkAddress(string value) {
            _value = value;
        }
        public NetworkAddress(int number) {
            _value = number.ToString();
        }

        public NetworkAddress Append(int number) {
            var value = _value + Separator + number;
            return new NetworkAddress(value);
        }

        public int GetId(int level) {
            var ids = _value.Split(Separator);

            return int.Parse(ids[level]);
        }

        public int GetLastId() {
            var ids = _value.Split(Separator);

            return int.Parse(ids[ids.Length - 1]);
        }

        public int GetParentsId() {
            var ids = _value.Split(Separator);

            return int.Parse(ids[ids.Length - 2]);
        }

        public NetworkAddress GetParentsAddress() {
            var ids = _value.Split(Separator);
            var value = string.Join(Separator.ToString(), ids.Take(ids.Length - 1));
            return new NetworkAddress(value);
        }

        public NetworkAddress GetRootFromBeginning(int level) {
            var ids = _value.Split(Separator);
            var value = string.Join(Separator.ToString(), ids.Take(level));
            return new NetworkAddress(value);
        }

        public NetworkAddress GetRootFromEnd(int level) {
            var ids = _value.Split(Separator);
            var value = string.Join(Separator.ToString(), ids.Take(ids.Length - level));
            return new NetworkAddress(value);
        }

        public override string ToString() {
            return _value;
        }

        public override bool Equals(object obj) {
            if (!(obj is NetworkAddress)) return false;

            var other = (NetworkAddress) obj;
            return _value.Equals(other._value);
        }

        public override int GetHashCode() {
            return _value.GetHashCode();
        }

        #region IXmlSerializable

        public XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader reader) {
            reader.ReadStartElement(nameof(NetworkAddress));
            _value = XmlSerializer.Deserialize<string>(reader);
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer) {
            XmlSerializer.Serialize(writer, _value);
        }

        #endregion
    }
}

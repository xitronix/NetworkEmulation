﻿using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using NetworkEmulation.Network.Element;
using NetworkUtilities;
using NetworkUtilities.Serialization;
using UniqueId = NetworkUtilities.UniqueId;

namespace NetworkEmulation.Editor.Element {
    public partial class Link : Control, IMarkable, ISerializable {
        private static readonly Pen SelectedPen = new Pen(Color.Black, 5);
        private static readonly Pen DeselectedPen = new Pen(Color.Black, 1);
        private static readonly Pen OnlinePen = new Pen(Color.Green, 5);
        private static readonly Pen OfflinePen = new Pen(Color.Red, 5);
        private Pen _pen = DeselectedPen;

        public Link() {
            InitializeComponent();
            Id = UniqueId.Generate();
            Parameters = new LinkModel();
        }

        public Link(ref NodePictureBox beginNodePictureBox, ref NodePictureBox endNodePictureBox) : this() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetAttachmentNodePictureBoxes(ref beginNodePictureBox, ref endNodePictureBox);
        }

        public NodePictureBox BeginNodePictureBox { get; private set; }
        public NodePictureBox EndNodePictureBox { get; private set; }

        public LinkModel Parameters { get; set; }

        public void MarkAsSelected() {
            ChangeStyle(SelectedPen);
        }

        public void MarkAsDeselected() {
            ChangeStyle(DeselectedPen);
        }

        public void MarkAsOnline() {
            ChangeStyle(OnlinePen);
        }

        public void MarkAsOffline() {
            ChangeStyle(OfflinePen);
        }

        public XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader reader) {
            reader.MoveToContent();
            Id = new UniqueId(reader.GetAttribute("Id"));
            reader.ReadStartElement(nameof(Link));
            Parameters = XmlSerializer.Deserialize<LinkModel>(reader);
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer) {
            writer.WriteAttributeString("Id", Id.ToString());
            XmlSerializer.Serialize(writer, Parameters);
        }

        public UniqueId Id { get; private set; }

        public void SetAttachmentNodePictureBoxes(ref NodePictureBox beginNodePictureBox,
            ref NodePictureBox endNodePictureBox) {
            BeginNodePictureBox = beginNodePictureBox;
            EndNodePictureBox = endNodePictureBox;

            Parameters.BeginNodePictureBoxId = BeginNodePictureBox.Id;
            Parameters.EndNodePictureBoxId = EndNodePictureBox.Id;

            BeginNodePictureBox.OnNodeMoving += sender => Parent.Refresh();
            EndNodePictureBox.OnNodeMoving += sender => Parent.Refresh();
        }

        private void ChangeStyle(Pen pen) {
            _pen = pen;
            Parent.Refresh();
        }

        public void DrawLink(Graphics graphics) {
            var beginPoint = BeginNodePictureBox.CenterPoint();
            var endPoint = EndNodePictureBox.CenterPoint();

            graphics.DrawLine(_pen, beginPoint, endPoint);
        }

        public bool IsBetween(NodePictureBox beginNodePictureBox, NodePictureBox endNodePictureBox) {
            return (BeginNodePictureBox.Equals(beginNodePictureBox) && EndNodePictureBox.Equals(endNodePictureBox)) ||
                   (BeginNodePictureBox.Equals(endNodePictureBox) && EndNodePictureBox.Equals(beginNodePictureBox));
        }
    }
}
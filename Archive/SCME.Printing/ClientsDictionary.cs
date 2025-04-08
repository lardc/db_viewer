using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SCME.NetworkPrinting
{
    internal class ClientRecord
    {
        internal string MMECode { get; set; }
        internal string IPAddress { get; set; }
        internal string PrinterName { get; set; }
        internal bool Use2PosTemplate { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: IP - {1}, Printer - {2}, 2P - {3}", MMECode, IPAddress, PrinterName, Use2PosTemplate);
        }
    }

    internal class ClientsEngine
    {
        internal List<ClientRecord> ClientRecords { get; private set; }

        internal ClientsEngine(string DocumentPath)
        {
            try
            {
                var doc = new XmlDocument();

                doc.Load(DocumentPath);
                ClientRecords = Load(doc);
            }
            catch (Exception)
            {
                ClientRecords = new List<ClientRecord>();
                throw;
            }
        }

        private static List<ClientRecord> Load(XmlDocument Document)
        {
            if (Document.DocumentElement == null)
                return new List<ClientRecord>();

            return new List<ClientRecord>(from XmlElement element in Document.DocumentElement
                                          select new ClientRecord
                                                          {
                                                              MMECode = element.Attributes[0].Value,
                                                              IPAddress = element.Attributes[1].Value,
                                                              PrinterName = element.Attributes[2].Value,
                                                              Use2PosTemplate = Boolean.Parse(element.Attributes[3].Value)
                                                          });
        }
    }
}
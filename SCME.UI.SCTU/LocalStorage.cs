using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace SCME.UI
{
    public class CStorageItem
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return string.Format("{0} = {1}", Name, Value);
        }
    }

    public class LocalStorage
    {
        private readonly string m_Path;
        private readonly XmlDocument m_Doc;
        
        public ObservableCollection<CStorageItem> Collection { get; private set; }

        public LocalStorage(string DocumentPath)
        {
            m_Path = DocumentPath;
            m_Doc = new XmlDocument();

            try
            {
                m_Doc.Load(DocumentPath);
            }
            catch (Exception)
            {
                m_Doc.RemoveAll();

                m_Doc = new XmlDocument
                {
                    InnerXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <Items> </Items>"
                };

                m_Doc.Save(DocumentPath);
            }

            Collection = Load(m_Doc);
        }

        internal void Save()
        {
            if (m_Doc.DocumentElement != null)
                m_Doc.DocumentElement.RemoveAll();

            foreach (var item in Collection)
                AddXmlElement(item);

            m_Doc.Save(m_Path);
        }


        internal void WriteItem(string Name, object Value)
        {
            if (Collection.Any(It => It.Name == Name))
                Collection.First(It => It.Name == Name).Value =
                    Value.ToString();
            else
                Cache.Storage.Collection.Add(new CStorageItem { Name = Name, Value = Value.ToString() });
        }

        private static ObservableCollection<CStorageItem> Load(XmlDocument Document)
        {
            if (Document.DocumentElement == null)
                return new ObservableCollection<CStorageItem>();

            return new ObservableCollection<CStorageItem>(from XmlElement element in Document.DocumentElement
                                                          select new CStorageItem
                                                          {
                                                              Name = element.Attributes[0].Value,
                                                              Value = element.Attributes[1].Value
                                                          });
        }

        private void AddXmlElement(CStorageItem Item)
        {
            var element = m_Doc.CreateNode(XmlNodeType.Element, "Item", "");
            var attr = m_Doc.CreateAttribute("Name");

            attr.Value = Item.Name;
            element.Attributes.Append(attr);

            attr = m_Doc.CreateAttribute("Value");
            attr.Value = Item.Value;
            element.Attributes.Append(attr);

            // Finish
            m_Doc.DocumentElement.AppendChild(element);
        }

    }
}
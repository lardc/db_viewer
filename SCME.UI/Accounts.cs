using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace SCME.UI
{
    public class CAccount
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", ID, Name);
        }
    }

    public class AccountEngine
    {
        public ObservableCollection<CAccount> Collection { get; private set; }

        public AccountEngine(string DocumentPath)
        {
            try
            {
                var doc = new XmlDocument();

                doc.Load(DocumentPath);
                Collection = Load(doc);
            }
            catch (Exception)
            {
                Collection = new ObservableCollection<CAccount>();
            }
        }

        private static ObservableCollection<CAccount> Load(XmlDocument Document)
        {
            if (Document.DocumentElement == null)
                return new ObservableCollection<CAccount>();

            return new ObservableCollection<CAccount>(from XmlElement element in Document.DocumentElement
                                                      select new CAccount
                                                          {
                                                              ID =
                                                                  int.Parse(element.Attributes[0].Value,
                                                                            CultureInfo.InvariantCulture),
                                                              Name = element.Attributes[1].Value,
                                                              Password = element.Attributes[2].Value
                                                          });
        }
    }
}
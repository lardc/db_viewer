using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml;

namespace SCME.UI
{
    public class CKeyboardLayout
    {
        public int ID { get; set; }
        public string Language { get; set; }

        public override string ToString()
        {
            return Language;
        }

        public string Key101 { get; set; }
        public string Key102 { get; set; }
        public string Key103 { get; set; }
        public string Key104 { get; set; }
        public string Key105 { get; set; }
        public string Key106 { get; set; }
        public string Key107 { get; set; }
        public string Key108 { get; set; }
        public string Key109 { get; set; }
        public string Key110 { get; set; }
        public string Key111 { get; set; }
        public string Key112 { get; set; }

        public string Key201 { get; set; }
        public string Key202 { get; set; }
        public string Key203 { get; set; }
        public string Key204 { get; set; }
        public string Key205 { get; set; }
        public string Key206 { get; set; }
        public string Key207 { get; set; }
        public string Key208 { get; set; }
        public string Key209 { get; set; }
        public string Key210 { get; set; }
        public string Key211 { get; set; }

        public string Key301 { get; set; }
        public string Key302 { get; set; }
        public string Key303 { get; set; }
        public string Key304 { get; set; }
        public string Key305 { get; set; }
        public string Key306 { get; set; }
        public string Key307 { get; set; }
        public string Key308 { get; set; }
        public string Key309 { get; set; }

        public CKeyboardLayout()
        {
            ID = 0;
            Language = "En";

            SetDefaultKeys();
        }

        private void SetDefaultKeys()
        {
            Key101 = "Q";
            Key102 = "W";
            Key103 = "E";
            Key104 = "R";
            Key105 = "T";
            Key106 = "Y";
            Key107 = "U";
            Key108 = "I";
            Key109 = "O";
            Key110 = "P";
            Key111 = "[";
            Key112 = "]";

            Key201 = "A";
            Key202 = "S";
            Key203 = "D";
            Key204 = "F";
            Key205 = "G";
            Key206 = "H";
            Key207 = "J";
            Key208 = "K";
            Key209 = "L";
            Key210 = ";";
            Key211 = "'";

            Key301 = "Z";
            Key302 = "X";
            Key303 = "C";
            Key304 = "V";
            Key305 = "B";
            Key306 = "N";
            Key307 = "M";
            Key308 = ",";
            Key309 = ".";
        }
    }

    public class KeyboardLayouts
    {
        public ObservableCollection<CKeyboardLayout> Collection { get; private set; }

        private readonly XmlDocument m_Doc;

        public KeyboardLayouts(string DocumentPath)
        {
            m_Doc = new XmlDocument();
            Collection = new ObservableCollection<CKeyboardLayout>();

            try
            {
                m_Doc.Load(DocumentPath);
                Collection = Load();
            }
            catch (Exception)
            {
                Collection.Add(new CKeyboardLayout());
            }
        }

        private ObservableCollection<CKeyboardLayout> Load()
        {
            var collection = new ObservableCollection<CKeyboardLayout>();
            if (m_Doc.DocumentElement == null)
                return collection;

            foreach (XmlElement element in m_Doc.DocumentElement)
            {
                var keyboard = new CKeyboardLayout
                    {
                        ID = int.Parse(element.Attributes[0].Value, CultureInfo.InvariantCulture),
                        Language = element.Attributes[1].Value
                    };

                foreach (XmlNode node in element.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Key101":
                            keyboard.Key101 = node.InnerText;
                            break;
                        case "Key102":
                            keyboard.Key102 = node.InnerText;
                            break;
                        case "Key103":
                            keyboard.Key103 = node.InnerText;
                            break;
                        case "Key104":
                            keyboard.Key104 = node.InnerText;
                            break;
                        case "Key105":
                            keyboard.Key105 = node.InnerText;
                            break;
                        case "Key106":
                            keyboard.Key106 = node.InnerText;
                            break;
                        case "Key107":
                            keyboard.Key107 = node.InnerText;
                            break;
                        case "Key108":
                            keyboard.Key108 = node.InnerText;
                            break;
                        case "Key109":
                            keyboard.Key109 = node.InnerText;
                            break;
                        case "Key110":
                            keyboard.Key110 = node.InnerText;
                            break;
                        case "Key111":
                            keyboard.Key111 = node.InnerText;
                            break;
                        case "Key112":
                            keyboard.Key112 = node.InnerText;
                            break;

                        case "Key201":
                            keyboard.Key201 = node.InnerText;
                            break;
                        case "Key202":
                            keyboard.Key202 = node.InnerText;
                            break;
                        case "Key203":
                            keyboard.Key203 = node.InnerText;
                            break;
                        case "Key204":
                            keyboard.Key204 = node.InnerText;
                            break;
                        case "Key205":
                            keyboard.Key205 = node.InnerText;
                            break;
                        case "Key206":
                            keyboard.Key206 = node.InnerText;
                            break;
                        case "Key207":
                            keyboard.Key207 = node.InnerText;
                            break;
                        case "Key208":
                            keyboard.Key208 = node.InnerText;
                            break;
                        case "Key209":
                            keyboard.Key209 = node.InnerText;
                            break;
                        case "Key210":
                            keyboard.Key210 = node.InnerText;
                            break;
                        case "Key211":
                            keyboard.Key211 = node.InnerText;
                            break;

                        case "Key301":
                            keyboard.Key301 = node.InnerText;
                            break;
                        case "Key302":
                            keyboard.Key302 = node.InnerText;
                            break;
                        case "Key303":
                            keyboard.Key303 = node.InnerText;
                            break;
                        case "Key304":
                            keyboard.Key304 = node.InnerText;
                            break;
                        case "Key305":
                            keyboard.Key305 = node.InnerText;
                            break;
                        case "Key306":
                            keyboard.Key306 = node.InnerText;
                            break;
                        case "Key307":
                            keyboard.Key307 = node.InnerText;
                            break;
                        case "Key308":
                            keyboard.Key308 = node.InnerText;
                            break;
                        case "Key309":
                            keyboard.Key309 = node.InnerText;
                            break;
                    }
                }

                collection.Add(keyboard);
            }

            return collection;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace SCME.Types.Profiles
{
    internal enum ParentType
    {
        Folder,
        Set
    }

    public class ProfileDictionary
    {
        #region Constants

        private const string PROFILE_TAG = @"Profile";
        private const string SET_TAG = @"Set";
        private const string FOLDER_TAG = @"Folder";
        private const string DT_FORMAT_STRING = @"yyyy-MM-dd HH:mm:ss.fff";

        #endregion

        #region Fields

        private readonly string m_Path;
        private readonly XmlDocument m_Doc;
        private ProfileDictionaryObject m_Root;

        #endregion

        public ProfileDictionary(string DocumentPath)
        {
            m_Path = DocumentPath;

            try
            {
                m_Doc = new XmlDocument();
                m_Doc.Load(DocumentPath);
            }
            catch (Exception ex)
            {
                m_Doc = null;
                throw new Exception(String.Format("Error while loading document: {0}", ex.Message));
            }

            Root = LoadFromXml();
        }

        public ProfileDictionary(IEnumerable<Profile> profileList)
        {
            //m_Path = DocumentPath;

            //m_Doc = new XmlDocument
            //{
            //    InnerXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <Items> </Items>"
            //};

            var root = new ProfileFolder("ROOT", Guid.Empty, DateTime.MinValue);
            foreach (var profile in profileList)
                root.ChildrenList.Add(profile);

            Root = root;

            PlainCollection = new ObservableCollection<Profile>(profileList);

        }

        public ProfileDictionaryObject Root
        {
            get { return m_Root; }
            set
            {
                m_Root = value;
                PlainCollection = GetCollection(value);
            }
        }

        public ObservableCollection<Profile> PlainCollection { get; private set; }

        public void SaveToXml(bool UseLocalCollectionAsSource = false)
        {
            if (m_Doc == null)
                return;

            if (m_Doc.DocumentElement != null)
                m_Doc.DocumentElement.RemoveAll();

            try
            {
                if (m_Doc.DocumentElement != null)
                    if (UseLocalCollectionAsSource)
                    {
                        foreach (var item in PlainCollection)
                            m_Doc.DocumentElement.AppendChild(SerializeItem(item));
                    }
                    else
                    {
                        foreach (var item in Root.ChildrenList)
                            m_Doc.DocumentElement.AppendChild(SerializeItem(item));
                    }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error while serializing document: {0}", ex.Message));
            }

            try
            {
                m_Doc.Save(m_Path);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error while saving document: {0}", ex.Message));
            }
        }

        public void SaveToDb(ControlServerProxy controlServerProxy, List<ProfileItem> profileItems)
        {
            try
            {
                controlServerProxy.SaveProfiles(profileItems);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error while saving profile to database: {0}", ex.Message));
            }
          
        }

        public static string SerializeToXmlAsString(IEnumerable<Profile> ProfileList)
        {
            var doc = new XmlDocument
            {
                InnerXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <Items> </Items>"
            };

            if (doc.DocumentElement != null)
                foreach (var item in ProfileList)
                    doc.DocumentElement.AppendChild(SerializeProfile(doc, item));

            using (var stream = new MemoryStream())
            {
                doc.Save(stream);
                stream.Flush();

                stream.Position = 0;

                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        #region Private members

        private static ObservableCollection<Profile> GetCollection(ProfileDictionaryObject Source)
        {
            var list = new ObservableCollection<Profile>();

            foreach (var profile in Source.ChildrenList.OrderBy(Item => Item.Name).SelectMany(Item => Item.ChildrenList.OfType<Profile>()))
                list.Add(profile);

            return list;
        }

        private ProfileDictionaryObject LoadFromXml()
        {
            try
            {
                var root = new ProfileFolder("ROOT", Guid.Empty, DateTime.MinValue);

                if (m_Doc.DocumentElement != null)
                    foreach (var element in
                            (from XmlElement element in m_Doc.DocumentElement select ParseElement(element, ParentType.Folder)).OrderBy(
                                Item => Item.Name))
                        root.ChildrenList.Add(element);

                return root;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error while parsing document: {0}", ex.Message));
            }
        }

        private static ProfileDictionaryObject ParseElement(XmlNode Element, ParentType Parent)
        {
            if (Element.Attributes != null)
            {
                var name = Element.Attributes[0].Value;
                var key = (Element.Attributes.Count > 1) ? Guid.Parse(Element.Attributes[1].Value) : Guid.NewGuid();
                var timestamp = (Element.Attributes.Count > 2)
                    ? DateTime.ParseExact(Element.Attributes[2].Value, DT_FORMAT_STRING,
                        CultureInfo.InvariantCulture)
                    : DateTime.Now;

                if (Element.Name == PROFILE_TAG)
                {
                    if (Parent == ParentType.Set)
                        return ParseProfile(Element, name, key, timestamp);

                    var prof = ParseProfile(Element, name, key, timestamp);
                    prof.IsTop = true;
                    var set = new ProfileSet(prof.Name, Guid.NewGuid(), prof.Timestamp);
                    set.ChildrenList.Add(prof);
                    prof.Parent = set;

                    return set;
                }

                if (Element.Name == SET_TAG)
                {
                    var set = new ProfileSet(name, key, timestamp);

                    foreach (var element in
                        (from XmlElement element in Element select ParseElement(element, ParentType.Set))
                            .OfType<Profile>().OrderByDescending(
                                Item => Item.Timestamp))
                    {
                        element.Name = name;
                        element.Parent = set;
                        set.ChildrenList.Add(element);
                    }

                    if (set.ChildrenList.Count > 0)
                        ((Profile)set.ChildrenList[0]).IsTop = true;

                    return set;
                }

                if (Element.Name == FOLDER_TAG)
                {
                    var folder = new ProfileFolder(name, key, timestamp);

                    foreach (var element in
                        (from XmlElement element in Element select ParseElement(element, ParentType.Folder)).OrderBy(
                            Item => Item.Name))
                    {
                        element.Parent = folder;
                        folder.ChildrenList.Add(element);
                    }

                    return folder;
                }

                throw new ApplicationException(string.Format("Unknown XML node: {0}", Element.Name));
            }

            throw new ApplicationException(string.Format("No required attributes found on element {0}", Element.Name));
        }

        private static Profile ParseProfile(XmlNode Element, string Name, Guid Key, DateTime Timestamp)
        {
            var nodeGate = Element["Gate"];
            var nodeVtm = Element["SL"];
            var nodeBvt = Element["BVT"];
            var nodeComm = Element["Commutation"];
            var nodeClamp = Element["Clamping"];

            try
            {
                var profile = new Profile(Name, Key, Timestamp)
                {
                    ParametersGate =
                    {
                        IsEnabled =
                            bool.Parse(nodeGate.Attributes[0].Value),
                        IsCurrentEnabled = bool.Parse(nodeGate.ChildNodes[0].Attributes[0].Value),
                        IsIhEnabled = bool.Parse(nodeGate.ChildNodes[0].Attributes[1].Value),
                        IsIhStrikeCurrentEnabled = bool.Parse(nodeGate.ChildNodes[0].Attributes[2].Value),
                        IsIlEnabled = bool.Parse(nodeGate.ChildNodes[0].Attributes[3].Value)
                    },
                    NormativesGate =
                    {
                        Resistance =
                            float.Parse(nodeGate.ChildNodes[1].Attributes[0].Value,
                                CultureInfo.InvariantCulture),
                        IGT =
                            float.Parse(nodeGate.ChildNodes[1].Attributes[1].Value,
                                CultureInfo.InvariantCulture),
                        VGT =
                            float.Parse(nodeGate.ChildNodes[1].Attributes[2].Value,
                                CultureInfo.InvariantCulture),
                        IH =
                            float.Parse(nodeGate.ChildNodes[1].Attributes[3].Value,
                                CultureInfo.InvariantCulture),
                        IL =
                            float.Parse(nodeGate.ChildNodes[1].Attributes[4].Value,
                                CultureInfo.InvariantCulture)
                    },
                    ParametersVTM =
                    {
                        IsEnabled = bool.Parse(nodeVtm.Attributes[0].Value),
                        IsSelfTest = false,
                        TestType =
                            (SL.VTMTestType)
                                Enum.Parse(typeof(SL.VTMTestType), nodeVtm.ChildNodes[0].Attributes[0].Value),
                        RampCurrent =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[1].Value,
                                CultureInfo.InvariantCulture),
                        RampTime =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[2].Value,
                                CultureInfo.InvariantCulture),
                        IsRampOpeningEnabled = bool.Parse(nodeVtm.ChildNodes[0].Attributes[3].Value),
                        RampOpeningCurrent =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[4].Value,
                                CultureInfo.InvariantCulture),
                        RampOpeningTime =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[5].Value,
                                CultureInfo.InvariantCulture),
                        SinusCurrent =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[6].Value,
                                CultureInfo.InvariantCulture),
                        SinusTime =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[7].Value,
                                CultureInfo.InvariantCulture),
                        CurveCurrent =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[8].Value,
                                CultureInfo.InvariantCulture),
                        CurveTime =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[9].Value,
                                CultureInfo.InvariantCulture),
                        CurveFactor =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[10].Value,
                                CultureInfo.InvariantCulture),
                        CurveAddTime =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[11].Value,
                                CultureInfo.InvariantCulture),
                        UseFullScale = bool.Parse(nodeVtm.ChildNodes[0].Attributes[12].Value),
                        UseLsqMethod = bool.Parse(nodeVtm.ChildNodes[0].Attributes[13].Value),
                        Count =
                            ushort.Parse(nodeVtm.ChildNodes[0].Attributes[14].Value,
                                CultureInfo.InvariantCulture)
                    },
                    NormativesVTM =
                    {
                        VTM =
                            float.Parse(nodeVtm.ChildNodes[1].Attributes[0].Value,
                                CultureInfo.InvariantCulture)
                    },
                    ParametersBVT =
                    {
                        IsEnabled = bool.Parse(nodeBvt.Attributes[0].Value),
                        TestType =
                            (BVT.BVTTestType)
                                Enum.Parse(typeof(BVT.BVTTestType), nodeBvt.ChildNodes[0].Attributes[0].Value),
                        MeasurementMode =
                            (BVT.BVTMeasurementMode)
                                Enum.Parse(typeof(BVT.BVTMeasurementMode),
                                    nodeBvt.ChildNodes[0].Attributes[1].Value),
                        VoltageLimitD =
                            ushort.Parse(nodeBvt.ChildNodes[0].Attributes[2].Value,
                                CultureInfo.InvariantCulture),
                        VoltageLimitR =
                            ushort.Parse(nodeBvt.ChildNodes[0].Attributes[3].Value,
                                CultureInfo.InvariantCulture),
                        CurrentLimit =
                            float.Parse(nodeBvt.ChildNodes[0].Attributes[4].Value,
                                CultureInfo.InvariantCulture),
                        PlateTime =
                            ushort.Parse(nodeBvt.ChildNodes[0].Attributes[5].Value,
                                CultureInfo.InvariantCulture),
                        RampUpVoltage =
                            float.Parse(nodeBvt.ChildNodes[0].Attributes[6].Value,
                                CultureInfo.InvariantCulture),
                        StartVoltage =
                            ushort.Parse(nodeBvt.ChildNodes[0].Attributes[7].Value,
                                CultureInfo.InvariantCulture),
                        VoltageFrequency =
                            ushort.Parse(nodeBvt.ChildNodes[0].Attributes[8].Value,
                                CultureInfo.InvariantCulture),
                        FrequencyDivisor =
                            ushort.Parse(nodeBvt.ChildNodes[0].Attributes[9].Value,
                                CultureInfo.InvariantCulture)
                    },
                    NormativesBVT =
                    {
                        VDRM =
                            ushort.Parse(nodeBvt.ChildNodes[1].Attributes[0].Value,
                                CultureInfo.InvariantCulture),
                        VRRM =
                            ushort.Parse(nodeBvt.ChildNodes[1].Attributes[1].Value,
                                CultureInfo.InvariantCulture),
                        IDRM =
                            float.Parse(nodeBvt.ChildNodes[1].Attributes[2].Value,
                                CultureInfo.InvariantCulture),
                        IRRM =
                            float.Parse(nodeBvt.ChildNodes[1].Attributes[3].Value,
                                CultureInfo.InvariantCulture)
                    },
                    ParametersComm =
                        (Commutation.ModuleCommutationType)
                            Enum.Parse(typeof(Commutation.ModuleCommutationType),
                                nodeComm.Attributes[0].Value),
                    ParametersClamp = float.Parse(nodeClamp.Attributes[0].Value, CultureInfo.InvariantCulture)
                };
                if (profile.ParametersGate.IsEnabled)
                    profile.TestParametersAndNormatives.Add(profile.ParametersGate);
                if (profile.ParametersVTM.IsEnabled)
                    profile.TestParametersAndNormatives.Add(profile.ParametersVTM);
                if (profile.ParametersBVT.IsEnabled)
                    profile.TestParametersAndNormatives.Add(profile.ParametersBVT);
                return profile;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(String.Format("Error during attribute reading: {0}", ex.Message));
            }
        }

        private XmlNode SerializeItem(ProfileDictionaryObject Item)
        {
            var profileItem = Item as Profile;

            if (profileItem != null)
                return SerializeProfile(m_Doc, profileItem);

            var setItem = Item as ProfileSet;

            if (setItem != null)
            {
                var setElement = m_Doc.CreateNode(XmlNodeType.Element, SET_TAG, "");
                SerializeBase(m_Doc, Item, setElement);

                foreach (var profile in Item.ChildrenList.Cast<Profile>())
                    setElement.AppendChild(SerializeProfile(m_Doc, profile));

                return setElement;
            }

            var folderElement = m_Doc.CreateNode(XmlNodeType.Element, FOLDER_TAG, "");
            SerializeBase(m_Doc, Item, folderElement);

            foreach (var folder in Item.ChildrenList)
                folderElement.AppendChild(SerializeItem(folder));

            return folderElement;
        }

        private static void SerializeBase(XmlDocument Host, ProfileDictionaryObject Item, XmlNode Element)
        {
            try
            {
                // Add name
                var attr = Host.CreateAttribute("Name");
                attr.Value = Item.Name;
                Element.Attributes.Append(attr);

                // Add key
                attr = Host.CreateAttribute("Key");
                attr.Value = Item.Key.ToString();
                Element.Attributes.Append(attr);

                // Add timestamp
                attr = Host.CreateAttribute("TS");
                attr.Value = Item.Timestamp.ToString(DT_FORMAT_STRING, CultureInfo.InvariantCulture);
                Element.Attributes.Append(attr);
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(String.Format("Error during attribute serialization: {0}", ex.Message));
            }
        }

        private static XmlNode SerializeProfile(XmlDocument Host, Profile Item)
        {
            var element = Host.CreateNode(XmlNodeType.Element, PROFILE_TAG, "");

            SerializeBase(Host, Item, element);

            try
            {
                // Add gate
                var nodeGate = Host.CreateNode(XmlNodeType.Element, "Gate", "");
                var attr = Host.CreateAttribute("Enable");
                attr.Value = Item.ParametersGate.IsEnabled.ToString();
                nodeGate.Attributes.Append(attr);

                var nodeParameters = Host.CreateNode(XmlNodeType.Element, "Parameters", "");

                attr = Host.CreateAttribute("EnableCurrent");
                attr.Value = Item.ParametersGate.IsCurrentEnabled.ToString();
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("EnableIH");
                attr.Value = Item.ParametersGate.IsIhEnabled.ToString();
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("EnableIHStrike");
                attr.Value = Item.ParametersGate.IsIhStrikeCurrentEnabled.ToString();
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("EnableIL");
                attr.Value = Item.ParametersGate.IsIlEnabled.ToString();
                nodeParameters.Attributes.Append(attr);

                nodeGate.AppendChild(nodeParameters);

                var nodeNormatives = Host.CreateNode(XmlNodeType.Element, "Normatives", "");

                attr = Host.CreateAttribute("Resistance");
                attr.Value = Item.NormativesGate.Resistance.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                attr = Host.CreateAttribute("IGT");
                attr.Value = Item.NormativesGate.IGT.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                attr = Host.CreateAttribute("VGT");
                attr.Value = Item.NormativesGate.VGT.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                attr = Host.CreateAttribute("IH");
                attr.Value = Item.NormativesGate.IH.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                attr = Host.CreateAttribute("IL");
                attr.Value = Item.NormativesGate.IL.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                nodeGate.AppendChild(nodeNormatives);

                element.AppendChild(nodeGate);

                // Add VTM
                var nodeVtm = Host.CreateNode(XmlNodeType.Element, "SL", "");

                attr = Host.CreateAttribute("Enable");
                attr.Value = Item.ParametersVTM.IsEnabled.ToString(CultureInfo.InvariantCulture);
                nodeVtm.Attributes.Append(attr);

                nodeParameters = Host.CreateNode(XmlNodeType.Element, "Parameters", "");

                attr = Host.CreateAttribute("Type");
                attr.Value = Item.ParametersVTM.TestType.ToString();
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("RampCurrent");
                attr.Value = Item.ParametersVTM.RampCurrent.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("RampTime");
                attr.Value = Item.ParametersVTM.RampTime.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("Opening");
                attr.Value = Item.ParametersVTM.IsRampOpeningEnabled.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("RampOpeningCurrent");
                attr.Value = Item.ParametersVTM.RampOpeningCurrent.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("RampOpeningTime");
                attr.Value = Item.ParametersVTM.RampOpeningTime.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("SinusCurrent");
                attr.Value = Item.ParametersVTM.SinusCurrent.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("SinusTime");
                attr.Value = Item.ParametersVTM.SinusTime.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("CurveCurrent");
                attr.Value = Item.ParametersVTM.CurveCurrent.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("CurveTime");
                attr.Value = Item.ParametersVTM.CurveTime.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("CurveFactor");
                attr.Value = Item.ParametersVTM.CurveFactor.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("CurveAddTime");
                attr.Value = Item.ParametersVTM.CurveAddTime.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("UseFullScale");
                attr.Value = Item.ParametersVTM.UseFullScale.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("UseLsqMethod");
                attr.Value = Item.ParametersVTM.UseLsqMethod.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("Count");
                attr.Value = Item.ParametersVTM.Count.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                nodeVtm.AppendChild(nodeParameters);

                nodeNormatives = Host.CreateNode(XmlNodeType.Element, "Normatives", "");

                attr = Host.CreateAttribute("VTM");
                attr.Value = Item.NormativesVTM.VTM.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                nodeVtm.AppendChild(nodeNormatives);

                element.AppendChild(nodeVtm);

                // Add BVT
                var nodeBvt = Host.CreateNode(XmlNodeType.Element, "BVT", "");

                attr = Host.CreateAttribute("Enabled");
                attr.Value = Item.ParametersBVT.IsEnabled.ToString(CultureInfo.InvariantCulture);
                nodeBvt.Attributes.Append(attr);

                nodeParameters = Host.CreateNode(XmlNodeType.Element, "Parameters", "");

                attr = Host.CreateAttribute("Type");
                attr.Value = Item.ParametersBVT.TestType.ToString();
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("MeasurementMode");
                attr.Value = Item.ParametersBVT.MeasurementMode.ToString();
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("VoltageLimitD");
                attr.Value = Item.ParametersBVT.VoltageLimitD.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("VoltageLimitR");
                attr.Value = Item.ParametersBVT.VoltageLimitR.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("CurrentLimit");
                attr.Value = Item.ParametersBVT.CurrentLimit.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("PlateTime");
                attr.Value = Item.ParametersBVT.PlateTime.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("RampupVoltage");
                attr.Value = Item.ParametersBVT.RampUpVoltage.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("StartVoltage");
                attr.Value = Item.ParametersBVT.StartVoltage.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("VoltageFrequency");
                attr.Value = Item.ParametersBVT.VoltageFrequency.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                attr = Host.CreateAttribute("FrequencyDivisor");
                attr.Value = Item.ParametersBVT.FrequencyDivisor.ToString(CultureInfo.InvariantCulture);
                nodeParameters.Attributes.Append(attr);

                nodeBvt.AppendChild(nodeParameters);

                nodeNormatives = Host.CreateNode(XmlNodeType.Element, "Normatives", "");

                attr = Host.CreateAttribute("VDRM");
                attr.Value = Item.NormativesBVT.VDRM.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                attr = Host.CreateAttribute("VRRM");
                attr.Value = Item.NormativesBVT.VRRM.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                attr = Host.CreateAttribute("IDRM");
                attr.Value = Item.NormativesBVT.IDRM.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                attr = Host.CreateAttribute("IRRM");
                attr.Value = Item.NormativesBVT.IRRM.ToString(CultureInfo.InvariantCulture);
                nodeNormatives.Attributes.Append(attr);

                nodeBvt.AppendChild(nodeNormatives);

                element.AppendChild(nodeBvt);

                // Add Commutation
                var nodeComm = Host.CreateNode(XmlNodeType.Element, "Commutation", "");

                attr = Host.CreateAttribute("CommutationType");
                attr.Value = Item.ParametersComm.ToString();
                nodeComm.Attributes.Append(attr);

                element.AppendChild(nodeComm);

                // Add clamping
                var nodeClamp = Host.CreateNode(XmlNodeType.Element, "Clamping", "");

                attr = Host.CreateAttribute("CustomForce");
                attr.Value = Item.ParametersClamp.ToString(CultureInfo.InvariantCulture);
                nodeClamp.Attributes.Append(attr);

                attr = Host.CreateAttribute("IsHeightMeasureEnabled");
                attr.Value = Item.IsHeightMeasureEnabled.ToString(CultureInfo.InvariantCulture);
                nodeClamp.Attributes.Append(attr);

                element.AppendChild(nodeClamp);
            }
            catch (NullReferenceException ex)
            {
                throw new Exception(String.Format("Error during attribute serialization: {0}", ex.Message));
            }

            return element;
        }

        #endregion
    }
}

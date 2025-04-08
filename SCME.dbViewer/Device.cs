using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCME.dbViewer
{
    public class PlaceStorage
    {
        //значение
        private string FValue;

        //ед. измерения
        private string FUm;

        //нижнее значение нормы
        private float FNrmMin;

        //верхнее значение нормы
        private float FNrmMax;
    }

    public class Device
    {
 
        private Int32 FDev_ID;
        public Int32 Dev_ID
        {
            get { return this.FDev_ID; }

            set
            {
                this.FDev_ID = value;
            }
        }

        private string FGroup_Name;
        public string Group_Name
        {
            get { return this.FGroup_Name; }

            set
            {
                this.FGroup_Name = value;
            }
        }

        private Int32 FGroup_ID;
        public Int32 Group_ID
        {
            get { return this.FGroup_ID; }

            set
            {
                this.FGroup_ID = value;
            }
        }

        private string FCode;
        public string Code
        {
            get { return this.FCode; }

            set
            {
                this.FCode = value;
            }
        }

        private string FMme_Code;
        public string Mme_Code
        {
            get { return this.FMme_Code; }

            set
            {
                this.FMme_Code = value;
            }
        }

        private DateTime FTs;
        public DateTime Ts
        {
            get { return this.FTs; }

            set
            {
                this.FTs = value;
            }
        }

        private string FUsr;
        public string Usr
        {
            get { return this.FUsr; }

            set
            {
                this.FUsr = value;
            }
        }

        private string FDeviceType;
        public string DeviceType
        {
            get { return this.FDeviceType; }

            set
            {
                this.FDeviceType = value;
            }
        }

        private Int32 FAverageCurrent;
        public Int32 AverageCurrent
        {
            get { return this.FAverageCurrent; }

            set
            {
                this.FAverageCurrent = value;
            }
        }

        private string FСonstructive;
        public string Сonstructive
        {
            get { return this.FСonstructive; }

            set
            {
                this.FСonstructive = value;
            }
        }

        private string FItem;
        public string Item
        {
            get { return this.FItem; }

            set
            {
                this.FItem = value;
            }
        }

        private Int32 FSiType;
        public Int32 SiType
        {
            get { return this.FSiType; }

            set
            {
                this.FSiType = value;
            }
        }

        private Int32 FSiOmnity;
        public Int32 SiOmnity
        {
            get { return this.FSiOmnity; }

            set
            {
                this.FSiOmnity = value;
            }
        }

        private Int32 FDeviceClass;
        public Int32 DeviceClass
        {
            get { return this.FDeviceClass; }

            set
            {
                this.FDeviceClass = value;
            }
        }

        private bool FSapID;
        public bool SapID
        {
            get { return this.FSapID; }

            set
            {
                this.FSapID = value;
            }
        }

        private string FStatus;
        public string Status
        {
            get { return this.FStatus; }

            set
            {
                this.FStatus = value;
            }
        }

        private string FReason;
        public string Reason
        {
            get { return this.FReason; }

            set
            {
                this.FReason = value;
            }
        }

        private string FCodeOfNonMatch;
        public string CodeOfNonMatch
        {
            get { return this.FCodeOfNonMatch; }

            set
            {
                this.FCodeOfNonMatch = value;
            }
        }

        private Int32 FProf_ID;
        public Int32 Prof_ID
        {
            get { return this.FProf_ID; }

            set
            {
                this.FProf_ID = value;
            }
        }

        private string FProf_Guid;
        public string Prof_Guid
        {
            get { return this.FProf_Guid; }

            set
            {
                this.FProf_Guid = value;
            }
        }

        private string FProf_Name;
        public string Prof_Name
        {
            get { return this.FProf_Name; }

            set
            {
                this.FProf_Name = value;
            }
        }

        private string FXMLProfConditions;
        public string XMLProfConditions
        {
            get { return this.FXMLProfConditions; }

            set
            {
                this.FXMLProfConditions = value;
            }
        }

        private string FXMLDeviceParameters;
        public string XMLDeviceParameters
        {
            get { return this.FXMLDeviceParameters; }

            set
            {
                this.FXMLDeviceParameters = value;
            }
        }

        private string FDeviceComments;
        public string DeviceComments
        {
            get { return this.FDeviceComments; }

            set
            {
                this.FDeviceComments = value;
            }
        }
    }
}

using System.Collections.Generic;
using SCME.Types;

namespace SCME.ExcelPrinting
{
    public class DeviceItemWithParams
    {
        public DeviceItem GeneralInfo { get; set; }
        public int DefectCode { get; set; }

        public List<ParameterItem> Parameters { get; set; }
    }

    public class ReportInfo
    {
        public string GroupName { get; set; }
        public string CustomerName { get; set; }
        public string ModuleType { get; set; }

        public List<ConditionItem> Conditions { get; set; }
        public List<ParameterNormativeItem> Normatives { get;set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCME.CustomControls.FilterAndSort
{
    public interface IFilteredSortedSourceProviderAsync
    {
        SortDescriptionList SortDescriptionList { get; }
    }
}

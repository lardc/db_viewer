using System.Collections.Specialized;

namespace SCME.CustomControls.FilterAndSort
{

    public class FilterDescriptionList : DescriptionList<FilterDescription>
    {
        public void OnCollectionReset()
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

using SCME.CustomControls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace SCME.dbViewer
{
    public class Collection<T> : AlphaChiTech.Virtualization.VirtualizingObservableCollection<T>
    {
        public Collection(AlphaChiTech.Virtualization.IItemSourceProvider<T> provider) : base(provider)
        {
        }

        /*
        private void CreateColumns()
        {
            this.CreateColumn(((MainWindow)System.Windows.Application.Current.MainWindow).dgDevices, Common.Constants.Code, Common.Constants.Code);
        }
        */
        /*
        private DataGridTextColumn CreateColumn(DataGridSqlResultBigData dg, string header, string bindPath)
        {
            DataGridTextColumn column = new DataGridTextColumn();
            column.Header = header;
            column.IsReadOnly = true;
            column.Binding = new Binding(bindPath);
            dg.Columns.Add(column);

            return column;
        }
        */

    }
}

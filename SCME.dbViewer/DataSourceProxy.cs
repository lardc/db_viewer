using AlphaChiTech.Virtualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SCME.CustomControls.FilterAndSort;
using System.Data;
using SCME.CustomControls;

namespace SCME.dbViewer
{
    public class DataSourceProxy : IPagedSourceProviderAsync<DynamicObj>, IFilteredSortedSourceProviderAsync
    {
        public DataSourceProxy(DataProvider dataProvider)
        {
            this.FDataProvider = dataProvider;
        }

        #region properties

        public FilterDescriptionList FilterDescriptionList
        {
            get { return this.FDataProvider.FilterDescriptionList; }
        }

        #endregion

        #region IFilteredSortedSourceProviderAsync Members

        public SortDescriptionList SortDescriptionList
        {
            get { return this.FDataProvider.SortDescriptionList; }
        }

        #endregion

        public bool IsSynchronized { get; }
        public object SyncRoot { get; }

        #region fields

        private readonly DataProvider FDataProvider;

        private readonly Random FRandom = new Random();

        #endregion

        #region IPagedSourceProvider<DynamicObj> Members (synchronous not available members)

        int IPagedSourceProvider<DynamicObj>.IndexOf(DynamicObj item)
        {
            throw new NotImplementedException();
        }

        public bool Contains(DynamicObj item)
        {
            throw new NotImplementedException();
        }

        public PagedSourceItemsPacket<DynamicObj> GetItemsAt(int pageOffSet, int count, bool usePlaceholder)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region public members

        public Task<bool> ContainsAsync(DynamicObj item)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCountAsync()
        {
            //вычисляем общее количество записей
            return Task.Run(() =>
            {
                return this.FDataProvider.Count();
            });
        }

        public Task<PagedSourceItemsPacket<DynamicObj>> GetItemsAtAsync(int pageOffSet, int count, bool usePlaceholder)
        {
            //возвращаем порцию данных размером count записей по смещению pageoffset относительно начальной записи
            return Task.Run(() =>
            {
                return new PagedSourceItemsPacket<DynamicObj>
                {
                    LoadedAt = DateTime.Now,
                    Items = this.FDataProvider.GetItemsAt(pageOffSet, count)
                };
            });
        }

        public DynamicObj GetPlaceHolder(int index, int page, int offset)
        {
            DynamicObj item = new DynamicObj();

            return Routines.GetPlaceHolder(item, page, offset);
        }

        /// <summary>
        ///     This returns the index of a specific item. This method is optional – you can just return -1 if you
        ///     don’t need to use IndexOf. It’s not strictly required if don’t need to be able to seeking to a
        ///     specific item, but if you are selecting items implementing this method is recommended.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Task<int> IndexOfAsync(DynamicObj item)
        {
            return Task.Run(() => { return -1; }); //return this.FDataSourceEmulation.FilteredOrderedItems.IndexOf(item);
        }

        /// <summary>
        ///     This is a callback that runs when a Reset is called on a provider. Implementing this is also optional.
        ///     If you don’t need to do anything in particular when resets occur, you can leave this method body empty.
        /// </summary>
        /// <param name="count"></param>
        public void OnReset(int count)
        {
            // Do nothing for now
        }

        #endregion
    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace SCME.dbViewer.ForSorting
{
    public class CustomComparer<T> : IComparer<T>
    {
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;

        private enum ChunkType
        {
            Alphanumeric,
            Numeric
        };

        public CustomComparer(ListSortDirection sortDirection)
        {
            this.SortDirection = sortDirection;
        }

        private bool InChunk(char ch, char otherCh)
        {
            ChunkType type = char.IsDigit(otherCh) ? ChunkType.Numeric : ChunkType.Alphanumeric;

            if ((type == ChunkType.Alphanumeric && char.IsDigit(ch)) || (type == ChunkType.Numeric && !char.IsDigit(ch)))
                return false;

            return true;
        }

        public int Compare(T x, T y)
        {
            if ((x == null) || (y == null))
            {
                if ((x == null) & (y != null))
                    return SortDirection == ListSortDirection.Ascending ? 1 : -1;

                if ((x != null) & (y == null))
                    return SortDirection == ListSortDirection.Ascending ? -1 : 1;

                return 0;
            }
            else
            {
                string xs = x as string;
                string ys = y as string;

                if ((xs == null) || (ys == null))
                    return 0;

                int xIndex = 0;
                int yIndex = 0;

                long xNumericChunk = 0;
                long yNumericChunk = 0;

                while ((xIndex < xs.Length) || (yIndex < ys.Length))
                {
                    if (xIndex >= xs.Length)
                        return SortDirection == ListSortDirection.Ascending ? -1 : 1;
                    else
                    {
                        if (yIndex >= ys.Length)
                            return SortDirection == ListSortDirection.Ascending ? 1 : -1;
                    }

                    char xCh = xs[xIndex];
                    char yCh = ys[yIndex];

                    StringBuilder xChunk = new StringBuilder();
                    StringBuilder yChunk = new StringBuilder();

                    while ((xIndex < xs.Length) && (xChunk.Length == 0 || InChunk(xCh, xChunk[0])))
                    {
                        xChunk.Append(xCh);
                        xIndex++;

                        if (xIndex < xs.Length)
                            xCh = xs[xIndex];
                    }

                    while ((yIndex < ys.Length) && (yChunk.Length == 0 || InChunk(yCh, yChunk[0])))
                    {
                        yChunk.Append(yCh);
                        yIndex++;

                        if (yIndex < ys.Length)
                            yCh = ys[yIndex];
                    }

                    int result = 0;

                    //оба куска записаны цифрами, сортируем их как цифры
                    if (char.IsDigit(xChunk[0]) && char.IsDigit(yChunk[0]))
                    {
                        xNumericChunk = Convert.ToInt64(xChunk.ToString());
                        yNumericChunk = Convert.ToInt64(yChunk.ToString());

                        if (xNumericChunk < yNumericChunk)
                            result = SortDirection == ListSortDirection.Ascending ? -1 : 1;

                        if (xNumericChunk > yNumericChunk)
                            result = SortDirection == ListSortDirection.Ascending ? 1 : -1;
                    }
                    else
                        result = SortDirection == ListSortDirection.Ascending ? xChunk.ToString().CompareTo(yChunk.ToString()) : yChunk.ToString().CompareTo(xChunk.ToString());

                    if (result != 0)
                        return result;
                }

                return 0;
            }
        }
    }
}

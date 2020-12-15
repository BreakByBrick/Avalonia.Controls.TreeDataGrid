﻿using System.Collections.Generic;
using System.Collections.Specialized;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    /// Represents a collection of cells in an <see cref="ITreeDataGridSource"/>.
    /// </summary>
    public interface ICells : IReadOnlyList<ICell>, INotifyCollectionChanged
    {
        /// <summary>
        /// Gets the number of columns.
        /// </summary>
        int ColumnCount { get; }

        /// <summary>
        /// Gets the number of rows.
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// Gets the cell at the specified coordinates.
        /// </summary>
        /// <param name="column">The column index.</param>
        /// <param name="row">The row index.</param>
        /// <returns></returns>
        ICell this[int column, int row] { get; }
    }
}
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.TreeDataGridTests
{
    public class TreeDataGridTests_Hierarchical
    {
        [Fact]
        public void Should_Display_Initial_Row_And_Cells()
        {
            using var app = App();

            var (target, _) = CreateTarget();

            Assert.NotNull(target.RowsPresenter);

            var rows = target.RowsPresenter
                .GetLogicalChildren()
                .Cast<TreeDataGridRow>()
                .ToList();
            
            Assert.Equal(2, rows.Count);

            foreach (var row in rows)
            {
                var cells = row.CellsPresenter
                    .GetLogicalChildren()
                    .Cast<TreeDataGridCell>()
                    .ToList();
                Assert.Equal(2, cells.Count);
            }
        }

        [Fact]
        public void Should_Display_Expanded_Root_Node()
        {
            using var app = App();

            var (target, source) = CreateTarget();

            Assert.NotNull(target.RowsPresenter);
            Assert.Equal(2, target.RowsPresenter!.RealizedElements.Count());
            Assert.Equal(2, target.RowsPresenter!.GetLogicalChildren().Count());

            source.Expand(new IndexPath(0));

            Assert.Equal(102, source.Rows.Count);
            Assert.Equal(102, target.RowsPresenter!.RealizedElements.Count());
            Assert.Equal(2, target.RowsPresenter!.GetLogicalChildren().Count());

            Layout(target);

            Assert.Equal(10, target.RowsPresenter!.RealizedElements.Count());
            Assert.Equal(10, target.RowsPresenter!.GetLogicalChildren().Count());
        }

        [Fact]
        public void Should_Display_Added_Root_Node()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var items = (IList<Model>)source.Items;

            Layout(target);
            items.Add(new Model { Id = -1, Title = "Added" });
            Layout(target);

            Assert.Equal(3, target.RowsPresenter!.RealizedElements.Count());
            Assert.Equal(3, target.RowsPresenter!.GetLogicalChildren().Count());
        }

        [Fact]
        public void Should_Display_Added_Child_Node()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var items = (IList<Model>)source.Items;
            var children = items[1].Children = new AvaloniaList<Model>
            {
                new Model { Id = -1, Title = "First" }
            };

            Layout(target);
            source.Expand(new IndexPath(1));
            Layout(target);
            children.Add(new Model { Id = -2, Title = "Second" });
            Layout(target);

            Assert.Equal(4, target.RowsPresenter!.RealizedElements.Count());
            Assert.Equal(4, target.RowsPresenter!.GetLogicalChildren().Count());
        }

        [Fact]
        public void Should_Subscribe_To_Models_For_Initial_Rows()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var items = (IList<Model>)source.Items;

            for (var i = 0; i < items.Count; ++i)
            {
                Assert.Equal(2, items[i].PropertyChangedSubscriberCount());
            }
        }

        [Fact]
        public void Should_Subscribe_To_Models_For_Expanded_Rows()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var items = (IList<Model>)source.Items;

            source.Expand(new IndexPath(0));
            Layout(target);

            Assert.Equal(2, items[0].PropertyChangedSubscriberCount());
            Assert.Equal(0, items[1].PropertyChangedSubscriberCount());

            var children = items[0].Children!;
            for (var i = 0; i < children.Count; ++i)
            {
                var expected = i < 9 ? 2 : 0;
                Assert.Equal(expected, children[i].PropertyChangedSubscriberCount());
            }
        }

        [Fact]
        public void Should_Subscribe_To_Correct_Models_After_Scrolling_Down_One_Row()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var items = (IList<Model>)source.Items;

            source.Expand(new IndexPath(0));
            Layout(target);
            target.Scroll!.Offset = new Vector(0, 10);
            Layout(target);

            Assert.Equal(0, items[0].PropertyChangedSubscriberCount());
            Assert.Equal(0, items[1].PropertyChangedSubscriberCount());

            var children = items[0].Children!;
            for (var i = 0; i < children.Count; ++i)
            {
                var expected = i < 10 ? 2 : 0;
                Assert.Equal(expected, children[i].PropertyChangedSubscriberCount());
            }
        }

        [Fact]
        public void Should_Unsubscribe_From_Models_When_Detached_From_Logical_Tree()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var items = (IList<Model>)source.Items;

            ((TestRoot)target.Parent).Child = null;

            for (var i = 0; i < items.Count; ++i)
            {
                Assert.Equal(0, items[i].PropertyChangedSubscriberCount());
            }
        }

        [Fact]
        public void Should_Hide_Expander_When_Node_With_No_Children_Expanded()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var cell = target.TryGetCell(0, 1);
            var expander = Assert.IsType<TreeDataGridExpanderCell>(cell);

            Assert.False(expander.IsExpanded);
            Assert.True(expander.ShowExpander);

            expander.IsExpanded = true;

            Assert.False(expander.IsExpanded);
            Assert.False(expander.ShowExpander);
        }

        [Fact]
        public void Can_Reassign_Items_When_Displaying_Child_Items_Followed_By_Root_Items()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var cell = target.TryGetCell(0, 0);
            var expander = Assert.IsType<TreeDataGridExpanderCell>(cell);

            // Add a a few more root items.
            ((AvaloniaList<Model>)source.Items).AddRange(CreateModels("Root ", 5, firstIndex: 2));

            // Expand the first root item and scroll down such that we're displaying some children
            // of the first root item together with subsequent root items.
            source.Expand(new IndexPath(0));
            Layout(target);
            target.Scroll!.Offset = new Vector(0, 9700);
            Layout(target);

            var firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.First()!;
            var lastRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.Last()!;
            var firstRowModel = (IRow<Model>)source.Rows[firstRow.RowIndex];
            var lastRowModel = (IRow<Model>)source.Rows[lastRow.RowIndex];

            Assert.Equal("Item 0-96", firstRowModel.Model.Title);
            Assert.Equal("Root 6", lastRowModel.Model.Title);

            // Replace the items with a single item.
            source.Items = new AvaloniaList<Model>
            {
                new Model
                {
                    Id = 0,
                    Title = "Root 0",
                },
            };

            Layout(target);

            firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements[0]!;
            Assert.Equal(0, firstRow.RowIndex);
            Assert.Equal(new Vector(0, 0), target.Scroll!.Offset);
        }

        [Fact]
        public void Can_Reassign_Items_When_Displaying_Grandchild_Items_Followed_By_Root_Items()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var cell = target.TryGetCell(0, 0);
            var expander = Assert.IsType<TreeDataGridExpanderCell>(cell);

            // Add a a few more root items.
            ((AvaloniaList<Model>)source.Items).AddRange(CreateModels("Root ", 5, firstIndex: 2));

            // Add some grandchildren.
            ((AvaloniaList<Model>)source.Items)[0].Children!.AddRange(CreateModels("Item 0-0-", 100));

            // Expand the first child item and scroll down such that we're displaying some children
            // of the first root item together with subsequent root items.
            source.Expand(new IndexPath(0, 0));
            Layout(target);
            target.Scroll!.Offset = new Vector(0, 9700);
            Layout(target);

            var firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.First()!;
            var lastRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.Last()!;
            var firstRowModel = (IRow<Model>)source.Rows[firstRow.RowIndex];
            var lastRowModel = (IRow<Model>)source.Rows[lastRow.RowIndex];

            Assert.Equal("Item 0-0-96", firstRowModel.Model.Title);
            Assert.Equal("Root 6", lastRowModel.Model.Title);

            // Replace the items with a single item.
            source.Items = new AvaloniaList<Model>
            {
                new Model
                {
                    Id = 0,
                    Title = "Root 0",
                },
            };

            Layout(target);

            firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements[0]!;
            Assert.Equal(0, firstRow.RowIndex);
            Assert.Equal(new Vector(0, 0), target.Scroll!.Offset);
        }

        [Fact]
        public void Can_Reset_Items_When_Displaying_Child_Items_Followed_By_Root_Items()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var cell = target.TryGetCell(0, 0);
            var expander = Assert.IsType<TreeDataGridExpanderCell>(cell);

            // Add a a few more root items.
            ((AvaloniaList<Model>)source.Items).AddRange(CreateModels("Root ", 5, firstIndex: 2));

            // Expand the first root item and scroll down such that we're displaying some children
            // of the first root item together with subsequent root items.
            source.Expand(new IndexPath(0));
            Layout(target);
            target.Scroll!.Offset = new Vector(0, 9700);
            Layout(target);

            var firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.First()!;
            var lastRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.Last()!;
            var firstRowModel = (IRow<Model>)source.Rows[firstRow.RowIndex];
            var lastRowModel = (IRow<Model>)source.Rows[lastRow.RowIndex];

            Assert.Equal("Item 0-96", firstRowModel.Model.Title);
            Assert.Equal("Root 6", lastRowModel.Model.Title);

            // Clear the items.
            ((IList)source.Items).Clear();

            Layout(target);

            firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements[0]!;
            Assert.Equal(0, firstRow.RowIndex);
            Assert.Equal(new Vector(0, 0), target.Scroll!.Offset);
        }

        [Fact]
        public void Can_Reset_Child_Items_When_Displaying_Grandchild_Items_Followed_By_Root_Items()
        {
            using var app = App();

            var (target, source) = CreateTarget();
            var cell = target.TryGetCell(0, 0);
            var expander = Assert.IsType<TreeDataGridExpanderCell>(cell);

            // Add a a few more root items.
            ((AvaloniaList<Model>)source.Items).AddRange(CreateModels("Root ", 5, firstIndex: 2));

            // Add some grandchildren.
            ((AvaloniaList<Model>)source.Items)[0].Children!.AddRange(CreateModels("Item 0-0-", 100));

            // Expand the first child item and scroll down such that we're displaying some children
            // of the first root item together with subsequent root items.
            source.Expand(new IndexPath(0, 0));
            Layout(target);
            target.Scroll!.Offset = new Vector(0, 9700);
            Layout(target);

            var firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.First()!;
            var lastRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements.Last()!;
            var firstRowModel = (IRow<Model>)source.Rows[firstRow.RowIndex];
            var lastRowModel = (IRow<Model>)source.Rows[lastRow.RowIndex];

            Assert.Equal("Item 0-0-96", firstRowModel.Model.Title);
            Assert.Equal("Root 6", lastRowModel.Model.Title);

            // Clear the child items.
            ((AvaloniaList<Model>)source.Items)[0].Children!.Clear();

            firstRow = (TreeDataGridRow)target.RowsPresenter!.RealizedElements[0]!;
            Assert.Equal(0, firstRow.RowIndex);
            Assert.Equal(new Vector(0, 0), target.Scroll!.Offset);
        }

        private static (TreeDataGrid, HierarchicalTreeDataGridSource<Model>) CreateTarget()
        {
            var items = new AvaloniaList<Model>
            {
                new Model
                {
                    Id = 0,
                    Title = "Root 0",
                    Children = new AvaloniaList<Model>(CreateModels("Item 0-", 100))
                },
                new Model
                {
                    Id = 1,
                    Title = "Root 1",
                },
            };

            var source = new HierarchicalTreeDataGridSource<Model>(items);
            source.Columns.Add(
                new HierarchicalExpanderColumn<Model>(
                    new TextColumn<Model, int>("ID", x => x.Id),
                    x => x.Children,
                    x => true));
            source.Columns.Add(new TextColumn<Model, string?>("Title", x => x.Title));

            var target = new TreeDataGrid
            {
                Template = TestTemplates.TreeDataGridTemplate(),
                Source = source,
            };

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.Is<TreeDataGridRow>())
                    {
                        Setters =
                        {
                            new Setter(TreeDataGridRow.TemplateProperty, TestTemplates.TreeDataGridRowTemplate()),
                        }
                    },
                    new Style(x => x.Is<TreeDataGridCell>())
                    {
                        Setters =
                        {
                            new Setter(TreeDataGridCell.HeightProperty, 10.0),
                        }
                    }
                },
                Child = target,
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
            return (target, source);
        }

        private static void Layout(TreeDataGrid target)
        {
            var root = (ILayoutRoot)target.GetVisualRoot();
            root.LayoutManager.ExecuteLayoutPass();
        }

        private static IDisposable App()
        {
            var scope = AvaloniaLocator.EnterScope();
            AvaloniaLocator.CurrentMutable.Bind<IStyler>().ToLazy(() => new Styler());
            return scope;
        }

        private static IEnumerable<Model> CreateModels(
            string titlePrefix,
            int count,
            int firstIndex = 0,
            int firstId = 100)
        {
            return Enumerable.Range(0, count).Select(x =>
                new Model
                {
                    Id = firstId + firstIndex + x,
                    Title = titlePrefix + (firstIndex + x),
                });
        }

        private class Model : NotifyingBase
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public AvaloniaList<Model>? Children { get; set; }
        }
    }
}

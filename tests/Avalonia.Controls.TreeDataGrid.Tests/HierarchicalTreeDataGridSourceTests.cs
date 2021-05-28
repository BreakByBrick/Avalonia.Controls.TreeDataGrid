﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Models.TreeDataGrid;
using Xunit;

namespace Avalonia.Controls.TreeDataGridTests
{
    public class HierarchicalTreeDataGridSourceTests
    {
        public class RowsAndCells
        {
            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Creates_Cells_For_Root_Models(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                AssertState(target, data, 5, sorted);
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Expanding_Root_Node_Creates_Child_Cells(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                target.Expand(new IndexPath(0));

                AssertState(target, data, 10, sorted, new IndexPath(0));
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Collapsing_Root_Node_Removes_Child_Cells(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                target.Expand(new IndexPath(0));

                Assert.Equal(10, target.Rows.Count);

                target.Collapse(new IndexPath(0));

                AssertState(target, data, 5, sorted);
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Supports_Adding_Root_Row(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                Assert.Equal(5, target.Rows.Count);

                var raised = 0;
                target.Rows.CollectionChanged += (s, e) => ++raised;

                data.Add(new Node { Id = 100, Caption = "New Node 1" });

                AssertState(target, data, 6, sorted);
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Supports_Inserting_Root_Row(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                Assert.Equal(5, target.Rows.Count);

                var raised = 0;
                target.Rows.CollectionChanged += (s, e) => ++raised;

                data.Insert(1, new Node { Id = 100, Caption = "New Node 1" });

                AssertState(target, data, 6, sorted);
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Supports_Adding_Child_Row(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                target.Expand(new IndexPath(0));

                Assert.Equal(10, target.Rows.Count);

                var raised = 0;
                target.Rows.CollectionChanged += (s, e) => ++raised;

                data[0].Children!.Add(new Node { Id = 100, Caption = "New Node 1" });

                AssertState(target, data, 11, sorted, new IndexPath(0));
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Supports_Inserting_Child_Row(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                target.Expand(new IndexPath(0));

                Assert.Equal(10, target.Rows.Count);

                var raised = 0;
                target.Rows.CollectionChanged += (s, e) => ++raised;

                data[0].Children!.Insert(1, new Node { Id = 100, Caption = "New Node 1" });

                AssertState(target, data, 11, sorted, new IndexPath(0));
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Supports_Removing_Child_Row(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                target.Expand(new IndexPath(0));
                Assert.Equal(10, target.Rows.Count);

                var raised = 0;
                target.Rows.CollectionChanged += (s, e) => ++raised;

                data[0].Children!.RemoveAt(3);

                AssertState(target, data, 9, sorted, new IndexPath(0));
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Supports_Replacing_Root_Row(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                Assert.Equal(5, target.Rows.Count);

                var raised = 0;
                target.Rows.CollectionChanged += (s, e) => ++raised;

                data[2] = new Node { Id = 100, Caption = "Replaced" };

                AssertState(target, data, 5, sorted);
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public void Supports_Moving_Root_Row(bool sorted)
            {
                var data = CreateData();
                var target = CreateTarget(data, sorted);

                Assert.Equal(5, target.Rows.Count);

                var raised = 0;
                target.Rows.CollectionChanged += (s, e) => ++raised;

                data.Move(2, 4);

                AssertState(target, data, 5, sorted);
            }

            [Fact]
            public void Setting_Sort_Updates_Rows()
            {
                var data = CreateData();
                var target = CreateTarget(data, false);

                target.Expand(new IndexPath(0));

                Assert.Equal(10, target.Rows.Count);

                target.Sort((x, y) => y.Id - x.Id);

                AssertState(target, data, 10, true, new IndexPath(0));
            }

            [Fact]
            public void Clearing_Sort_Updates_Rows()
            {
                var data = CreateData();
                var target = CreateTarget(data, true);

                target.Expand(new IndexPath(0));

                Assert.Equal(10, target.Rows.Count);

                target.Sort(null);

                AssertState(target, data, 10, false, new IndexPath(0));
            }

            [Fact]
            public void Can_Reassign_Items()
            {
                var data = CreateData();
                var target = CreateTarget(data, false);
                var rowsAddedRaised = 0;
                var rowsRemovedRaised = 0;

                Assert.Equal(5, target.Rows.Count);

                target.Rows.CollectionChanged += (s, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                        rowsAddedRaised += e.NewItems.Count;
                    else if (e.Action == NotifyCollectionChangedAction.Remove)
                        rowsRemovedRaised += e.OldItems.Count;
                };

                target.Items = CreateData(10);

                Assert.Equal(10, target.Rows.Count);
                Assert.Equal(5, rowsRemovedRaised);
                Assert.Equal(10, rowsAddedRaised);
            }
        }

        public class Expansion
        {
            [Fact]
            public void Expanding_Updates_Cell_IsExpanded()
            {
                var data = CreateData();
                var target = CreateTarget(data, false);
                var expander = (ExpanderCell<Node>)target.Rows.RealizeCell(target.Columns[0], 0, 0);
                var raised = 0;

                expander.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "IsExpanded")
                        ++raised;
                };

                target.Expand(new IndexPath(0));

                Assert.True(expander.IsExpanded);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Expanding_Previously_Expanded_Node_Creates_Expanded_Descendent()
            {
                var data = CreateData();
                var target = CreateTarget(data, false);

                data[0].Children![0].Children = new AvaloniaList<Node>
                {
                    new Node { Id = 100, Caption = "Grandchild" }
                };

                // Expand first root node.
                target.Expand(new IndexPath(0));

                AssertState(target, data, 10, false, new IndexPath(0));

                // Expand first child node.
                target.Expand(new IndexPath(0, 0));

                // Grandchild should now be visible.
                AssertState(target, data, 11, false, new IndexPath(0), new IndexPath(0, 0));

                // Collapse root node.
                target.Collapse(new IndexPath(0));
                AssertState(target, data, 5, false);

                // And expand again. Grandchild should now be visible once more.
                target.Expand(new IndexPath(0));
                AssertState(target, data, 11, false, new IndexPath(0), new IndexPath(0, 0));
            }

            [Fact]
            public void Shows_Expander_For_Row_With_Children()
            {
                var data = CreateData();
                var target = CreateTarget(data, false);
                var expander = (ExpanderCell<Node>)target.Rows.RealizeCell(target.Columns[0], 0, 0);

                Assert.True(expander.ShowExpander);
            }

            [Fact]
            public void Hides_Expander_For_Row_Without_Children()
            {
                var data = new[] { new Node { Id = 0, Caption = "Node 0" } };
                var target = CreateTarget(data, false);
                var expander = (ExpanderCell<Node>)target.Rows.RealizeCell(target.Columns[0], 0, 0);

                Assert.False(expander.ShowExpander);
            }

            [Fact]
            public void Attempting_To_Expand_Node_That_Has_No_Children_Hides_Expander()
            {
                var data = new Node { Id = 0, Caption = "Node 0" };

                // Here we return true from hasChildren selector, but there are actually no children.
                // This may happen if calculating the children is expensive.
                var target = new HierarchicalTreeDataGridSource<Node>(data)
                {
                    Columns =
                    {
                        new HierarchicalExpanderColumn<Node>(
                            new TextColumn<Node, int>("ID", x => x.Id),
                            x => x.Children,
                            x => true),
                        new TextColumn<Node, string?>("Caption", x => x.Caption),
                    }
                };

                var expander = (IExpanderCell)target.Rows.RealizeCell(target.Columns[0], 0, 0);

                target.Expand(new IndexPath(0));

                Assert.False(expander.ShowExpander);
                Assert.False(expander.IsExpanded);
            }
        }

        private static AvaloniaList<Node> CreateData(int count = 5)
        {
            var id = 0;
            var result = new AvaloniaList<Node>();

            for (var i = 0; i < count; ++i)
            {
                var node = new Node
                {
                    Id = id++,
                    Caption = $"Node {i}",
                    Children = new AvaloniaList<Node>(),
                };

                result.Add(node);

                for (var j = 0; j < 5; ++j)
                {
                    node.Children.Add(new Node
                    {
                        Id = id++,
                        Caption = $"Node {i}-{j}",
                        Children = new AvaloniaList<Node>(),
                    });
                }
            }
            ;
            return result;
        }

        private static HierarchicalTreeDataGridSource<Node> CreateTarget(IEnumerable<Node> roots, bool sorted)
        {
            var result = new HierarchicalTreeDataGridSource<Node>(roots)
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<Node>(
                        new TextColumn<Node, int>("ID", x => x.Id),
                        x => x.Children,
                        x => x.Children?.Count > 0),
                    new TextColumn<Node, string?>("Caption", x => x.Caption),
                }
            };

            if (sorted)
                result.Sort((x, y) => y.Id - x.Id);

            return result;
        }

        private static void AssertState(
            HierarchicalTreeDataGridSource<Node> target,
            IList<Node> data,
            int expectedRows,
            bool sorted,
            params IndexPath[] expanded)
        {
            Assert.Equal(2, target.Columns.Count);
            Assert.Equal(expectedRows, target.Rows.Count);

            var rowIndex = 0;

            void AssertLevel(IndexPath parent, IList<Node> levelData)
            {
                var sortedData = levelData;

                if (sorted)
                {
                    var s = new List<Node>(levelData);
                    s.Sort((x, y) => y.Id - x.Id);
                    sortedData = s;
                }

                for (var i = 0; i < levelData.Count; ++i)
                {
                    var modelIndex = parent.CloneWithChildIndex(levelData.IndexOf(sortedData[i]));
                    var model = GetModel(data, modelIndex);
                    var row = Assert.IsType<HierarchicalRow<Node>>(target.Rows[rowIndex]);
                    var shouldBeExpanded = expanded.Contains(modelIndex);

                    Assert.Equal(modelIndex, row.ModelIndexPath);
                    Assert.True(
                        row.IsExpanded == shouldBeExpanded,
                        $"Expected index {modelIndex} IsExpanded == {shouldBeExpanded}");

                    ++rowIndex;

                    if (row.IsExpanded)
                    {
                        Assert.NotNull(model.Children);
                        AssertLevel(modelIndex, model.Children!);
                    }
                }
            }

            AssertLevel(default, data);
        }

        private static Node GetModel(IList<Node> data, IndexPath path)
        {
            var depth = path.GetSize();
            Node? node = null;

            if (depth == 0)
                throw new NotSupportedException();

            for (var i = 0; i < depth; ++i)
            {
                var j = path.GetAt(i);
                node = node is null ? data[j] : node.Children![j];
            }

            return node!;
        }

        internal class Node : NotifyingBase
        {
            private int _id;
            private string? _caption;

            public int Id
            {
                get => _id;
                set => RaiseAndSetIfChanged(ref _id, value);
            }

            public string? Caption
            {
                get => _caption;
                set => RaiseAndSetIfChanged(ref _caption, value);
            }

            public AvaloniaList<Node>? Children { get; set; }
        }
    }
}

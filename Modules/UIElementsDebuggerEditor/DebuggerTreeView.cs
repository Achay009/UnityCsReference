// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal class DebuggerTreeView : VisualElement
    {
        public bool hierarchyHasChanged { get; set; }

        public IList<ITreeViewItem> treeRootItems
        {
            get
            {
                return m_TreeRootItems;
            }
            private set {}
        }

        public IEnumerable<ITreeViewItem> treeItems
        {
            get
            {
                return m_TreeView.items;
            }
        }

        private IList<ITreeViewItem> m_TreeRootItems;

        private TreeView m_TreeView;
        private HighlightOverlayPainter m_TreeViewHoverOverlay;

        private VisualElement m_Container;
        private DebuggerSearchBar m_SearchBar;

        private Action<VisualElement> m_SelectElementCallback;

        private List<VisualElement> m_SearchResultsHightlights;

        private DebuggerSelection m_DebuggerSelection;
        private IPanelDebug m_CurrentPanelDebug;

        public DebuggerTreeView(DebuggerSelection debuggerSelection, Action<VisualElement> selectElementCallback)
        {
            this.focusable = true;

            m_DebuggerSelection = debuggerSelection;
            m_DebuggerSelection.onPanelDebugChanged += pdbg => RebuildTree(pdbg);
            m_DebuggerSelection.onSelectedElementChanged += element => SelectElement(element, null);

            m_SelectElementCallback = selectElementCallback;
            hierarchyHasChanged = true;

            m_SearchResultsHightlights = new List<VisualElement>();

            this.RegisterCallback<FocusEvent>(e => m_TreeView?.Focus());

            m_TreeViewHoverOverlay = new HighlightOverlayPainter();

            m_Container = new VisualElement();
            m_Container.style.flexGrow = 1f;
            Add(m_Container);

            m_SearchBar = new DebuggerSearchBar(this);
            Add(m_SearchBar);
        }

        public void DrawOverlay()
        {
            m_TreeViewHoverOverlay.Draw();
        }

        private void ActivateSearchBar(ExecuteCommandEvent evt)
        {
            Debug.Log(evt.commandName);
            if (evt.commandName == "Find")
                m_SearchBar.Focus();
        }

        private void FillItem(VisualElement element, ITreeViewItem item)
        {
            element.Clear();

            var target = (item as TreeViewItem<VisualElement>).data;
            element.userData = target;

            var labelCont = new VisualElement();
            labelCont.AddToClassList("unity-debugger-tree-item-label-cont");
            element.Add(labelCont);

            var label = new Label(target.typeName);
            label.AddToClassList("unity-debugger-tree-item-label");
            label.AddToClassList("unity-debugger-tree-item-type");
            labelCont.Add(label);

            if (!string.IsNullOrEmpty(target.name))
            {
                var nameLabelCont = new VisualElement();
                nameLabelCont.AddToClassList("unity-debugger-tree-item-label-cont");
                element.Add(nameLabelCont);

                var nameLabel = new Label("#" + target.name);
                nameLabel.AddToClassList("unity-debugger-tree-item-label");
                nameLabel.AddToClassList("unity-debugger-tree-item-name");
                nameLabel.AddToClassList("unity-debugger-tree-item-name-label");
                nameLabelCont.Add(nameLabel);
            }

            foreach (var ussClass in target.GetClasses())
            {
                var classLabelCont = new VisualElement();
                classLabelCont.AddToClassList("unity-debugger-tree-item-label-cont");
                element.Add(classLabelCont);

                var classLabel = new Label("." + ussClass);
                classLabel.AddToClassList("unity-debugger-tree-item-label");
                classLabel.AddToClassList("unity-debugger-tree-item-classlist");
                classLabel.AddToClassList("unity-debugger-tree-item-classlist-label");
                classLabelCont.Add(classLabel);
            }
        }

        private void HighlightItemInTargetWindow(VisualElement item)
        {
            m_TreeViewHoverOverlay.ClearOverlay();
            m_TreeViewHoverOverlay.AddOverlay(item.userData as VisualElement);
            m_CurrentPanelDebug?.MarkDirtyRepaint();
        }

        private void UnhighlightItemInTargetWindow(VisualElement item)
        {
            m_TreeViewHoverOverlay.ClearOverlay();
            m_CurrentPanelDebug?.MarkDirtyRepaint();
        }

        public void RebuildTree(IPanelDebug panelDebug)
        {
            if (!hierarchyHasChanged && m_CurrentPanelDebug == panelDebug)
                return;

            m_CurrentPanelDebug = panelDebug;
            m_Container.Clear();

            int nextId = 1;
            m_TreeRootItems = GetTreeItemsFromVisualTree(panelDebug?.visualTree, ref nextId);

            if (panelDebug?.visualTree != null)
                m_TreeRootItems.Insert(0, new TreeViewItem<VisualElement>(0, panelDebug.visualTree));

            Func<VisualElement> makeItem = () =>
            {
                var element = new VisualElement();
                element.name = "unity-treeview-item-content";
                element.RegisterCallback<MouseEnterEvent>((e) =>
                {
                    HighlightItemInTargetWindow(e.target as VisualElement);
                });
                element.RegisterCallback<MouseLeaveEvent>((e) =>
                {
                    UnhighlightItemInTargetWindow(e.target as VisualElement);
                });
                return element;
            };

            // Clear selection which would otherwise persist via view data persistence.
            m_TreeView?.ClearSelection();

            m_TreeView = new TreeView(m_TreeRootItems, 20, makeItem, FillItem);
            m_TreeView.style.flexGrow = 1;
            m_TreeView.onSelectionChanged += (items) =>
            {
                if (m_SelectElementCallback == null)
                    return;

                if (items.Count == 0)
                {
                    m_SelectElementCallback(null);
                    return;
                }

                var item = items.First() as TreeViewItem<VisualElement>;
                var element = item != null ? item.data : null;
                m_SelectElementCallback(element);
            };

            m_Container.Add(m_TreeView);

            hierarchyHasChanged = false;
        }

        private TreeViewItem<VisualElement> FindElement(IEnumerable<ITreeViewItem> list, VisualElement element)
        {
            if (list == null)
                return null;

            foreach (var item in list)
            {
                var treeItem = item as TreeViewItem<VisualElement>;
                if (treeItem.data == element)
                    return treeItem;

                TreeViewItem<VisualElement> itemFoundInChildren = null;
                if (treeItem.hasChildren)
                    itemFoundInChildren = FindElement(treeItem.children, element);

                if (itemFoundInChildren != null)
                    return itemFoundInChildren;
            }

            return null;
        }

        public void ClearSearchResults()
        {
            foreach (var hl in m_SearchResultsHightlights)
                hl.RemoveFromHierarchy();

            m_SearchResultsHightlights.Clear();
        }

        public void SelectElement(VisualElement element, string query)
        {
            SelectElement(element, query, SearchHighlight.None);
        }

        public void SelectElement(VisualElement element, string query, SearchHighlight searchHighlight)
        {
            ClearSearchResults();

            var item = FindElement(m_TreeRootItems, element);
            if (item == null)
                return;

            m_TreeView.SelectItem(item.id);

            if (string.IsNullOrEmpty(query))
                return;

            var selected = m_TreeView.Query(classes: "unity-list-view__item--selected").First();
            if (selected == null || searchHighlight == SearchHighlight.None)
                return;

            var content = selected.Q("unity-treeview-item-content");
            var labelContainers = content.Query(classes: "unity-debugger-tree-item-label-cont").ToList();
            foreach (var labelContainer in labelContainers)
            {
                var label = labelContainer.Q<Label>();

                if (label.ClassListContains("unity-debugger-tree-item-type") && searchHighlight != SearchHighlight.Type)
                    continue;

                if (label.ClassListContains("unity-debugger-tree-item-name") && searchHighlight != SearchHighlight.Name)
                    continue;

                if (label.ClassListContains("unity-debugger-tree-item-classlist") && searchHighlight != SearchHighlight.Class)
                    continue;

                var text = label.text;
                var indexOf = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                if (indexOf < 0)
                    continue;

                var highlight = new VisualElement();
                m_SearchResultsHightlights.Add(highlight);
                highlight.AddToClassList("unity-debugger-highlight");
                int letterSize = 8;
                highlight.style.width = query.Length * letterSize;
                highlight.style.left = indexOf * letterSize;
                labelContainer.Insert(0, highlight);

                break;
            }
        }

        private IList<ITreeViewItem> GetTreeItemsFromVisualTree(VisualElement parent, ref int nextId)
        {
            List<ITreeViewItem> items = null;

            if (parent == null)
                return null;

            int count = parent.hierarchy.childCount;
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                if (items == null)
                    items = new List<ITreeViewItem>();

                var element = parent.hierarchy[i];

                var item = new TreeViewItem<VisualElement>(nextId, element);
                items.Add(item);
                nextId++;

                var childItems = GetTreeItemsFromVisualTree(element, ref nextId);
                if (childItems == null)
                    continue;

                item.AddChildren(childItems);
            }

            return items;
        }
    }
}

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Generic reactive list panel that performs differential updates.
    /// Replaces full rebuild pattern with smart diffing for performance.
    /// </summary>
    /// <typeparam name="T">The type of data item in the list.</typeparam>
    public partial class ReactiveListPanel<T> : VBoxContainer
    {
        private IList<T> _dataSource = new List<T>();
        private readonly Dictionary<int, Control> _itemCache = new();
        private IListAdapter<T>? _adapter;
        private int _nextItemId;

        [Export] public bool AnimateChanges { get; set; } = false;
        [Export] public float AnimationDuration { get; set; } = 0.1f;

        public void SetAdapter(IListAdapter<T> adapter)
        {
            _adapter = adapter;
        }

        public void SetData(IList<T> data)
        {
            _dataSource = data ?? new List<T>();
            Refresh();
        }

        public void AddItem(T item)
        {
            _dataSource = _dataSource.Append(item).ToList();
            AddItemDifferentially(_dataSource.Count - 1, item);
        }

        public void RemoveItem(T item)
        {
            var index = _dataSource.IndexOf(item);
            if (index >= 0)
            {
                _dataSource = _dataSource.Where((_, i) => i != index).ToList();
                RemoveItemDifferentially(index);
            }
        }

        public void UpdateItem(int index, T item)
        {
            if (index >= 0 && index < _dataSource.Count)
            {
                _dataSource[index] = item;
                if (_itemCache.TryGetValue(index, out var control) && _adapter != null)
                {
                    _adapter.UpdateItem(control, item);
                }
            }
        }

        public void Refresh()
        {
            if (_adapter == null)
            {
                GD.PrintErr("ReactiveListPanel: No adapter set");
                return;
            }

            UpdateDifferentially();
        }

        public void Clear()
        {
            foreach (var control in _itemCache.Values.ToList())
            {
                if (AnimateChanges)
                    AnimateExit(control);
                else
                    control.QueueFree();
            }
            _itemCache.Clear();
            _dataSource = new List<T>();
        }

        private void UpdateDifferentially()
        {
            var currentIds = new HashSet<int>();
            var itemsToUpdate = new List<(int index, T item, Control control)>();

            for (int i = 0; i < _dataSource.Count; i++)
            {
                var item = _dataSource[i];
                Control control;

                if (_itemCache.TryGetValue(i, out var cached))
                {
                    control = cached;
                    itemsToUpdate.Add((i, item, control));
                }
                else
                {
                    control = CreateAndAddItem(i, item);
                    itemsToUpdate.Add((i, item, control));
                }

                currentIds.Add(i);
            }

            foreach (var (index, item, control) in itemsToUpdate)
            {
                _adapter?.UpdateItem(control, item);
            }

            var toRemove = _itemCache.Keys.Except(currentIds).ToList();
            foreach (var id in toRemove)
            {
                RemoveItemDifferentially(id);
            }
        }

        private Control CreateAndAddItem(int index, T item)
        {
            if (_adapter == null)
            {
                var error = new Label { Text = "No adapter configured" };
                AddChild(error);
                return error;
            }

            var control = _adapter.CreateItem(item);
            _itemCache[index] = control;
            AddChild(control);

            if (AnimateChanges)
            {
                AnimateEntry(control);
            }

            return control;
        }

        private void AddItemDifferentially(int index, T item)
        {
            var control = CreateAndAddItem(index, item);
            RebuildCacheIndices();
        }

        private void RemoveItemDifferentially(int index)
        {
            if (!_itemCache.TryGetValue(index, out var control))
            {
                return;
            }

            _itemCache.Remove(index);

            if (AnimateChanges)
            {
                AnimateExit(control);
            }
            else
            {
                control.QueueFree();
            }

            RebuildCacheIndices();
        }

        private void RebuildCacheIndices()
        {
            var newCache = new Dictionary<int, Control>();
            var children = GetChildren().ToList();

            for (int i = 0; i < children.Count; i++)
            {
                if (_itemCache.TryGetValue(i + _itemCache.Count - children.Count, out var control))
                {
                    newCache[i] = control;
                }
            }

            foreach (var kvp in _itemCache)
            {
                if (!newCache.ContainsValue(kvp.Value))
                {
                    newCache[newCache.Count] = kvp.Value;
                }
            }

            _itemCache.Clear();
            foreach (var kvp in newCache)
            {
                _itemCache[kvp.Key] = kvp.Value;
            }
        }

        private void AnimateEntry(Control control)
        {
            control.Modulate = new Color(1, 1, 1, 0);
            var tween = CreateTween();
            tween.TweenProperty(control, "modulate:a", 1f, AnimationDuration);
        }

        private void AnimateExit(Control control)
        {
            var tween = CreateTween();
            tween.TweenProperty(control, "modulate:a", 0f, AnimationDuration);
            tween.TweenCallback(Callable.From(control.QueueFree));
        }
    }
}

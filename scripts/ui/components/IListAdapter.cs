using Godot;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Interface for list item adapters that create and update list items.
    /// </summary>
    /// <typeparam name="T">The type of data item in the list.</typeparam>
    public interface IListAdapter<T>
    {
        /// <summary>
        /// Creates a new control for displaying a data item.
        /// </summary>
        Control CreateItem(T data);

        /// <summary>
        /// Updates an existing control with new data.
        /// </summary>
        void UpdateItem(Control item, T data);

        /// <summary>
        /// Cleans up a control when it's removed from the list.
        /// </summary>
        void DestroyItem(Control item);
    }
}

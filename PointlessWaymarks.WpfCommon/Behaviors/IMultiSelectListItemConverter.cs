﻿namespace PointlessWaymarks.WpfCommon.Behaviors
{
    /// <summary>
    /// Converts items in the Master list to Items in the target list, and back again. From https://github.com/itsChris/WpfMvvmDataGridMultiselect
    /// </summary>
    public interface IMultiSelectListItemConverter
    {
        /// <summary>
        /// Converts the specified master list item.
        /// </summary>
        /// <param name="masterListItem">The master list item.</param>
        /// <returns>The result of the conversion.</returns>
        object Convert(object masterListItem);

        /// <summary>
        /// Converts the specified target list item.
        /// </summary>
        /// <param name="targetListItem">The target list item.</param>
        /// <returns>The result of the conversion.</returns>
        object ConvertBack(object targetListItem);
    }
}

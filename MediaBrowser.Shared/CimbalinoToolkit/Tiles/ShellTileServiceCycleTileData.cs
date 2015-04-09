﻿// ****************************************************************************
// <copyright file="ShellTileServiceCycleTileData.cs" company="Pedro Lamas">
// Copyright © Pedro Lamas 2013
// </copyright>
// ****************************************************************************
// <author>Pedro Lamas</author>
// <email>pedrolamas@gmail.com</email>
// <date>21-05-2013</date>
// <project>Cimbalino.Phone.Toolkit.Background</project>
// <web>http://www.pedrolamas.com</web>
// <license>
// See license.txt in this solution or http://www.pedrolamas.com/license_MIT.txt
// </license>
// ****************************************************************************

using System;
using System.Collections.Generic;
using Microsoft.Phone.Shell;

namespace Emby.WindowsPhone.CimbalinoToolkit.Tiles
{
    /// <summary>
    /// Describes a Tile template that cycles between 1 to 9 background images.
    /// </summary>
    public class ShellTileServiceCycleTileData : ShellTileServiceTileDataBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value between 1 and 99 that will be displayed in the Count field of the Tile. A value of 0 means the Count will not be displayed. If this property is not set, the Count display will not change during an update.
        /// </summary>
        /// <value>A value between 1 and 99 that will be displayed in the Count field of the Tile.</value>
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets a collection of up to 9 background images for the medium and wide Tile sizes.
        /// </summary>
        /// <value>A collection of up to 9 background images for the medium and wide Tile sizes.</value>
        public IEnumerable<Uri> CycleImages { get; set; }

        /// <summary>
        /// Gets or sets the front-side background image for the small Tile size.
        /// </summary>
        /// <value>The front-side background image for the small Tile size.</value>
        public Uri SmallBackgroundImage { get; set; }

        #endregion

        public override ShellTileData ToShellTileData()
        {
            return new CycleTileData()
            {
                Title = Title,
                Count = Count,
                CycleImages = CycleImages,
                SmallBackgroundImage = SmallBackgroundImage
            };
        }
    }
}
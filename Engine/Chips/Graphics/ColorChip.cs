﻿//   
// Copyright (c) Jesse Freeman. All rights reserved.  
//  
// Licensed under the Microsoft Public License (MS-PL) License. 
// See LICENSE file in the project root for full license information. 
// 
// Contributors
// --------------------------------------------------------
// This is the official list of Pixel Vision 8 contributors:
//  
// Jesse Freeman - @JesseFreeman
// Christer Kaitila - @McFunkypants
// Pedro Medeiros - @saint11
// Shawn Rakowski - @shwany

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PixelVisionRunner;
using PixelVisionSDK.Utils;

namespace PixelVisionSDK.Chips
{

    /// <summary>
    ///     The <see cref="ColorChip" /> represents the system colors of the engine.
    ///     It allows the engine to work in color indexes that the display can map
    ///     to actual colors in Unity via the class's set of APIs.
    /// </summary>
    public class ColorChip : AbstractChip, IInvalidate
    {

        protected string[] _colors =
        {
            "#2d1b2e",
            "#218a91",
            "#3cc2fa",
            "#9af6fd",
            "#4a247c",
            "#574b67",
            "#937ac5",
            "#8ae25d",
            "#8e2b45",
            "#f04156",
            "#f272ce",
            "#d3c0a8",
            "#c5754a",
            "#f2a759",
            "#f7db53",
            "#f9f4ea"
        };
        
//        protected int _colorsPerPage = 64;
//        protected int _pages = 4;
        protected ColorData[] colorCache;
        
        // By default, there are only 2 colors so we need 2 flags
        protected int[] invalidColors = new int[16];
        
        protected int _maxColors = -1;
        protected int _bgColor = 0;
        
        // A flag to let the game chip know the last 128  colors are reserved for palettes
//        public bool paletteMode;

        /// <summary>
        ///     The background color reference to use when rendering transparent in
        ///     the ScreenBufferTexture.
        /// </summary>
        public int backgroundColor {
            get { return _bgColor; }
            set
            {
                // We make sure that the bg color is never set to a value out of the range of the color chip
                _bgColor = value.Clamp(0, total);
                Invalidate();
            } 
        }
        
        protected string _maskColor = "#FF00FF";

        public string maskColor
        {
            get => _maskColor;
            set
            {
                if(ValidateHexColor(value))
                    _maskColor = value.ToUpper();
            }
        }
        
        /// <summary>
        ///     Defines the total number of colors per virtual page.
        /// </summary>
        /// <value>Int</value>
//        public int colorsPerPage
//        {
//            get { return _colorsPerPage; }
//            set { _colorsPerPage = value; }
//        }

        /// <summary>
        ///     Returns the total virtual pages of colors.
        /// </summary>
        /// <value>Int</value>
//        public int pages
//        {
//            get { return _pages; }
//            set
//            {
//                if (_pages == value)
//                    return;
//                
//                // The max number of colors are 512 (8 pages x 64 colors per page)
//                _pages = value;//.Clamp(1, 8);
//
//                var oldTotal = _colors.Length;
//
//                Array.Resize(ref _colors, total);
//                Array.Resize(ref invalidColors, total);
//                if (oldTotal < total)
//                    for (var i = oldTotal; i < total; i++)
//                        _colors[i] = maskColor;
//
//                Invalidate();
//            }
//        }

        /// <summary>
        ///     Get and Set the <see cref="supportedColors" /> number of <see cref="colors" />
        ///     in the palette. Changing the <see cref="supportedColors" /> will clear the
        ///     palette when it resizes.
        /// </summary>
        /// <value>Int</value>
        // TODO need to change this to totalSet colors or something more descriptive
        public int totalUsedColors
        {
            get
            {
                return hexColors.Length - hexColors.ToList().RemoveAll(c => c == maskColor);
            }
        }

        //TODO need to figure out a better way to set this up?
        public int maxColors
        {
            get { return _maxColors == -1 ? total : _maxColors; }
            set { _maxColors = value; }
        }

        public string[] hexColors
        {
            get
            {
                var colors = new string[total];

                Array.Copy(_colors, colors, total);

                return colors;
            }
        }

        /// <summary>
        ///     The <see cref="supportedColors" /> number of <see cref="colors" /> the chip can
        ///     support. This lock makes the sure that the <see cref="colors" />
        ///     array will never be larger than this value.
        /// </summary>
        /// <value>Int</value>
        public int total
        {
            get => _colors.Length;
            set
            {
                var oldTotal = _colors.Length;

                Array.Resize(ref _colors, value);
                Array.Resize(ref invalidColors, value);
                if (oldTotal < value)
                    for (var i = oldTotal; i < value; i++)
                        _colors[i] = maskColor;

                Invalidate();
            }
        }

        /// <summary>
        ///     Returns a list of color data to be used for rendering.
        /// </summary>
        /// <value>ColorData[]</value>
        public IColor[] colors
        {
            get
            {
                if (invalid)
                {
                    var t = total;
                    colorCache = new ColorData[t];

                    for (var i = 0; i < t; i++)
                    {

                        var colorHex = _colors[i];
                        
                        if (colorHex == maskColor && debugMode == false)
                        {
                            colorHex = _colors[backgroundColor];
                        }

                        var color = new ColorData(colorHex) {flag = invalidColors[i]};
                        colorCache[i] = color;
                    }

                    ResetValidation();
                }

                return colorCache;
            }
        }

        public string ReadColorAt(int index)
        {
            return index < 0 || index > _colors.Length - 1 ? maskColor : _colors[index];
        }

        public int FindColorID(string color)
        {
            return Array.IndexOf(_colors, color);
        }

        public void Clear()
        {
            
            var t = _colors.Length;
            for (var i = 0; i < t; i++)
                UpdateColorAt(i, maskColor);
        }

        public virtual void UpdateColorAt(int index, string color)
        {
            if (index >= _colors.Length || index < 0)
                return;

            // Make sure that all colors are uppercase
            color = color.ToUpper();

            if (ColorData.ValidateColor(color))
            {
                _colors[index] = color;
                invalidColors[index] = 1;
                Invalidate();
            }
        }

        public bool invalid { get; protected set; }

        private bool _debugMode;
        public bool unique;

        // Setting this to true will use the mask color for empty colors instead of replacing them with the bg color
        public bool debugMode
        {
            get { return _debugMode; }
            set
            {
                _debugMode = value;
                Invalidate();
            }
        }

        public void Invalidate()
        {
            invalid = true;
        }

        public void ResetValidation(int value = 0)
        {
            invalid = false;
            var total = invalidColors.Length;
            for (var i = 0; i < total; i++)
                invalidColors[i] = value;
        }

        /// <summary>
        ///     This method configures the chip. It registers itself with the
        ///     engine as the default ColorChip, it sets the supported
        ///     <see cref="colors" /> to the maximum value of 256 and calls
        ///     <see cref="RevertColors" /> to add the default <see cref="colors" />
        ///     to the <see cref="colors" /> array.
        /// </summary>
        public override void Configure()
        {
            engine.colorChip = this;
            backgroundColor = -1;
            
//            RebuildColorPages(16);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            engine.colorChip = null;
        }

        /// <summary>
        ///     Recalculates the total number of pages based on the new total
        ///     number of colors.
        /// </summary>
        /// <param name="total"></param>
//        public void RebuildColorPages(int total)
//        {
//            pages = (int)Math.Ceiling(total / (float)colorsPerPage);
//        }
       
        public bool ValidateHexColor(string inputColor)
        {
            return (Regex.Match(inputColor, "^#(?:[0-9a-fA-F]{3}){1,2}$").Success);
        }
        

    }

}
﻿using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Media;
using Windows.UI.Xaml.Media;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Shapes
{
	public partial class Path
	{
		protected override CGPath GetPath()
		{
			var streamGeometry = Data?.ToStreamGeometry();
			return streamGeometry?.ToCGPath();
		}

		partial void OnDataChanged()
		{
			this.SetNeedsLayout();
		}
	}
}

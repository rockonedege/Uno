﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CoreGraphics;
using UIKit;
using Windows.UI;

namespace Uno.UI.Extensions
{
	internal static class NSUIImageExtensions
	{
		internal static UIImage AsMonochrome(this UIImage image, Color foreground)
		{
			var width = (int)image.Size.Width;
			var height = (int)image.Size.Height;
			var imageRect = new CGRect(0, 0, image.Size.Width, image.Size.Height);

			var rawData = new byte[width * height * 4];
			var outputData = new byte[width * height * 4];
			var handle = GCHandle.Alloc(rawData);

			try
			{
				using (var colorSpace = CGColorSpace.CreateDeviceRGB())
				{
					using (var context = new CGBitmapContext(
						data: rawData,
						width: (nint)imageRect.Width,
						height: (nint)imageRect.Height,
						bitsPerComponent: 8,
						bytesPerRow: (nint)imageRect.Width * 4,
						colorSpace: colorSpace,
						bitmapInfo: CGImageAlphaInfo.PremultipliedLast
						)
					)
					{
						context.DrawImage(imageRect, image.CGImage);

						var foregroundColor = foreground.R | (foreground.R << 8) | (foreground.R << 16) | (0xFF << 24);

						for (int x = 0; x < width; x++)
						{
							for (int y = 0; y < height; y++)
							{
								var index = x * 4 + y * height * 4;

								var red = rawData[index + 0];
								var green = rawData[index + 1];
								var blue = rawData[index + 2];
								var alpha = rawData[index + 3];

								if (red + green + blue != 0)
								{
									outputData[index + 0] = foreground.R;
									outputData[index + 1] = foreground.G;
									outputData[index + 2] = foreground.B;
									outputData[index + 3] = 0xFF;
								}
								else
								{
									outputData[index + 0] = 0;
									outputData[index + 1] = 0;
									outputData[index + 2] = 0;
									outputData[index + 3] = 0;
								}
							}
						}
					}
				}
			}
			finally
			{
				handle.Free();
			}

			using (var dataProvider = new CGDataProvider(outputData, 0, outputData.Length))
			{
				using (var colorSpace = CGColorSpace.CreateDeviceRGB())
				{
					var bitsPerComponent = 8;
					var bytesPerPixel = 4;

					using (var cgImage = new CGImage(
						width,
						height,
						bitsPerComponent,
						bitsPerComponent * bytesPerPixel,
						bytesPerPixel * width,
						colorSpace,
						CGImageAlphaInfo.Last,
						dataProvider,
						null,
						false,
						CGColorRenderingIntent.Default
					))
					{
						return UIImage.FromImage(cgImage);
					}
				}
			}
		}
	}
}

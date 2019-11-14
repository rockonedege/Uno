using System;

using Uno.Extensions;
using Uno.UI.Views.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.UI.Services;
using Foundation;
using UIKit;
using CoreGraphics;
using Uno.Disposables;
using Uno.Diagnostics.Eventing;
using Windows.UI.Core;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;
using Uno.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class Image : UIImageView, IImage
	{
        private Size _sourceImageSize;

        /// <summary>
        /// The size of the native image data
        /// </summary>
        private Size SourceImageSize
        {
            get
            {
                return _sourceImageSize;
            }
            set
            {
                _sourceImageSize = value;

                if (Source is BitmapSource bitmapSource)
                {
                    bitmapSource.PixelWidth = (int)_sourceImageSize.Width;
                    bitmapSource.PixelHeight = (int)_sourceImageSize.Height;
                }
            }
        }

		public Image()
		{
			Initialize();
			ClipsToBounds = true;
			UserInteractionEnabled = true;
			UpdateContentMode(Stretch); // Set the default value of the UIImageView
		}

		partial void HitCheckOverridePartial(ref bool hitCheck)
		{
			hitCheck = Image != null;
		}

		public Image(IntPtr handle)
			: base(handle)
		{
			Initialize();
			ClipsToBounds = true;
		}

		private void TryOpenImage()
		{
			//Skip opening the image source source is already loaded or if the view isn't loaded
			if (_openedImage == Source)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because Source has not changed");
				}
				return;
			}

			if (Window == null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because view is not loaded");
				}
				return;
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + " TryOpenImage - proceeding");
			}

			using (
				_imageTrace.WriteEventActivity(
					TraceProvider.Image_SetSourceStart,
					TraceProvider.Image_SetSourceStop,
					new object[] { this.GetDependencyObjectId() }
				)
			)
			{
				_openedImage = Source;

				if (_openedImage is WriteableBitmap writeableBitmap)
				{
					SetImageFromWriteableBitmap(writeableBitmap);
				}
				else if (_openedImage == null || !_openedImage.HasSource())
				{
					Image = null;
					SetNeedsLayoutOrDisplay();
					_imageFetchDisposable.Disposable = null;
				}
				else if (_openedImage.ImageData != null)
				{
					SetImage(_openedImage.ImageData);
					_imageFetchDisposable.Disposable = null;
				}
				else
				{
					// The Jupiter behavior is to reset the visual right away, displaying nothing
					// then show the new image. We're rescheduling the work below, so there is going
					// to be a visual blank displayed.
					Image = null;

					Func<CancellationToken, Task> scheduledFetch = async (ct) =>
					{
						using (
						   _imageTrace.WriteEventActivity(
							   TraceProvider.Image_SetUriStart,
							   TraceProvider.Image_SetUriStop,
							   new object[] { this.GetDependencyObjectId() }
						   )
						)
						{
							//_openedImage could be set to null while trying to access it on the thread pool
							var image = await Task.Run(() => _openedImage?.Open(ct));

							//if both image and _openedImage are null this is ok just return;
							//otherwise call SetImage with null which will raise the OnImageFailed event
							if (ct.IsCancellationRequested ||
								(image == null && _openedImage == null))
							{
								return;
							}

							SetImage(image);
						}
					};

					Execute(scheduledFetch);
				}
			}
		}

		private void SetImageFromWriteableBitmap(WriteableBitmap writeableBitmap)
		{
			if(writeableBitmap.PixelBuffer is InMemoryBuffer memoryBuffer)
			{
				// Convert RGB colorspace.
				var bgraBuffer = memoryBuffer.Data;
				var rgbaBuffer = new byte[memoryBuffer.Data.Length];

				for (int i = 0; i < memoryBuffer.Data.Length; i += 4)
				{
					rgbaBuffer[i + 3] = bgraBuffer[i + 3]; // a
					rgbaBuffer[i + 0] = bgraBuffer[i + 2]; // r
					rgbaBuffer[i + 1] = bgraBuffer[i + 1]; // g
					rgbaBuffer[i + 2] = bgraBuffer[i + 0]; // b
				}

				using (var dataProvider = new CGDataProvider(rgbaBuffer, 0, rgbaBuffer.Length))
				{
					using (var colorSpace = CGColorSpace.CreateDeviceRGB())
					{
						var bitsPerComponent = 8;
						var bytesPerPixel = 4;

						using (var cgImage = new CGImage(
							writeableBitmap.PixelWidth,
							writeableBitmap.PixelHeight,
							bitsPerComponent,
							bitsPerComponent * bytesPerPixel,
							bytesPerPixel * writeableBitmap.PixelWidth,
							colorSpace,
							CGImageAlphaInfo.Last,
							dataProvider,
							null,
							false,
							CGColorRenderingIntent.Default
						))
						{
							SetImage(UIImage.FromImage(cgImage));
						}
					}
				}
			}
		}

		private void SetImage(UIImage image)
		{
			using (
				_imageTrace.WriteEventActivity(
					TraceProvider.Image_SetImageStart,
					TraceProvider.Image_SetImageStop,
					new object[] { this.GetDependencyObjectId() }
				)
			)
			{
				if (MonochromeColor != null)
				{
					image = image.AsMonochrome(MonochromeColor.Value);
				}

				Image = image;

				SourceImageSize = image?.Size.ToFoundationSize() ?? default(Size);
			}

			SetNeedsLayoutOrDisplay();
			if (Image != null)
			{
				OnImageOpened(image);
			}
			else
			{
				OnImageFailed(image);
			}
		}

		private void SetNeedsLayoutOrDisplay()
		{
			if (ShouldDowngradeLayoutRequest())
			{
				SetNeedsDisplay();
			}
			else
			{
				SetNeedsLayout();
			}
		}

		private void UpdateContentMode(Stretch stretch)
		{
			switch (stretch)
			{
				case Stretch.Uniform:
					ContentMode = UIViewContentMode.ScaleAspectFit;
					break;

				case Stretch.None:
					ContentMode = UIViewContentMode.Center;
					break;

				case Stretch.UniformToFill:
					ContentMode = UIViewContentMode.ScaleAspectFill;
					break;

				case Stretch.Fill:
					ContentMode = UIViewContentMode.ScaleToFill;
					break;

				default:
					throw new NotSupportedException("Stretch mode {0} is not supported".InvariantCultureFormat(stretch));
			}
		}


		partial void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
			UpdateContentMode(newValue);
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			size = IFrameworkElementHelper.SizeThatFits(this, size);

			size = _layouter.Measure(size.ToFoundationSize());

			return size;
		}
	}
}


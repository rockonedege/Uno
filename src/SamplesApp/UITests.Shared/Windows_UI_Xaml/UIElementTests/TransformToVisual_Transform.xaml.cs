﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.UIElementTests
{
	[SampleControlInfo("UIElement", "TransformToVisual_Transform")]
	public sealed partial class TransformToVisual_Transform : UserControl
	{
		public TransformToVisual_Transform()
		{
			this.InitializeComponent();

			var tests = this
				.GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Where(method => method.Name.StartsWith("When_"))
				.Select(testMethod => testMethod.Name);
			foreach (var test in tests)
			{
				Outputs.Children.Add(new TextBlock
				{
					Name = test + "_Result",
					Text = test
				});
			}

			Loaded += TransformToVisual_Transform_Loaded;
		}

		private async void TransformToVisual_Transform_Loaded(object sender, RoutedEventArgs e)
		{
			await Task.Yield();

			Run(() => When_TransformToRoot());
			Run(() => When_TransformToRoot_With_TranslateTransform());
			Run(() => When_TransformToRoot_With_InheritedTranslateTransform_And_Margin());
			Run(() => When_TransformToParent_With_Margin());
			Run(() => When_TransformToParent_With_InheritedMargin());
			Run(() => When_TransformToParent_With_CompositeTransform());
			Run(() => When_TransformToParent_With_InheritedCompositeTransform_And_Margin());
			Run(() => When_TransformToAnotherBranch_With_InheritedCompositeTransform_And_Margin());

			// Mark tests as completed
			TestsStatus.Text = "OK";
		}

		public void When_TransformToRoot()
		{
			var windowBounds = Windows.UI.Xaml.Window.Current.Bounds;
			var originAbs = new Point(windowBounds.Width - Border1.ActualWidth, windowBounds.Height - Border1.ActualHeight);

			var sut = Border1.TransformToVisual(null);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(originAbs.X, originAbs.Y, 50, 50), result);
		}

		public void When_TransformToRoot_With_TranslateTransform()
		{
			var windowBounds = Windows.UI.Xaml.Window.Current.Bounds;
			var originAbs = new Point(windowBounds.Width - Border2.ActualWidth, windowBounds.Height - Border2.ActualHeight);
			const int tX = -50;
			const int tY = -50;

			var sut = Border2.TransformToVisual(null);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(originAbs.X + tX, originAbs.Y + tY, 50, 50), result);
		}

		public void When_TransformToRoot_With_InheritedTranslateTransform_And_Margin()
		{
			var windowBounds = Windows.UI.Xaml.Window.Current.Bounds;
			var originAbs = new Point(windowBounds.Width - Border2.ActualWidth, windowBounds.Height - Border2.ActualHeight);
			const int tX = -50;
			const int tY = -50;
			const int marginX = 0;
			const int marginY = 30;

			var sut = Border2Child.TransformToVisual(null);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(originAbs.X + tX + marginX, originAbs.Y + tY + marginY, 50, 50), result);
		}

		public void When_TransformToParent_With_Margin()
		{
			const int marginX = 0;
			const int marginY = 30;

			var sut = Border2Child.TransformToVisual(Border2);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(marginX, marginY, 50, 50), result);
		}

		public void When_TransformToParent_With_InheritedMargin()
		{
			const int marginX = 0;
			const int marginY = 30;

			var sut = Border2SubChild.TransformToVisual(Border2);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(marginX, marginY, 50, 50), result);
		}

		public void When_TransformToParent_With_CompositeTransform()
		{
			const double w = 50;
			const double h = 50;

			const double tX = -100;
			const double tY = -100;
			const double scale = 2;
			const double angle = 45 * Math.PI / 180.0;

			var sut = Border3.TransformToVisual(Border3Parent);
			var result = sut.TransformBounds(new Rect(0, 0, w, h));

			var length = w * scale * Math.Cos(angle) + h * scale * Math.Cos(angle);
			var expected = new Rect(tX - length / 2, tY /* Origin of rotation is top/left, so as angle < 90, origin Y is not impacted */, length, length);

			Assert.IsTrue(RectCloseComparer.UI.Equals(expected, result));
		}

		public void When_TransformToParent_With_InheritedCompositeTransform_And_Margin()
		{
			const double w = 50;
			const double h = 50;

			const double tX = -100;
			const double tY = -100;
			const double scale = 2;
			const double angle = 45 * Math.PI / 180.0;

			const double marginX = 0;
			const double marginY = 30;

			var sut = Border3Child.TransformToVisual(Border3Parent);
			var result = sut.TransformBounds(new Rect(0, 0, w, h));

			var length = w * scale * Math.Cos(angle) + h * scale * Math.Cos(angle);
			var marginXProjection = marginX * scale * Math.Cos(angle); // as angle == 45 degree and we are using squares, projection is the same on X and Y
			var marginYProjection = marginY * scale * Math.Cos(angle);
			var origin = new Point(
				tX - length / 2 + marginXProjection - marginYProjection,
				tY - marginXProjection + marginYProjection);
			var expected = new Rect(origin, new Size(length, length));

			Assert.IsTrue(RectCloseComparer.UI.Equals(expected, result));
		}

		public void When_TransformToAnotherBranch_With_InheritedCompositeTransform_And_Margin()
		{
			const double w = 50;
			const double h = 50;

			const double tX = -100;
			const double tY = -100;
			const double scale = 2;
			const double angle = 45 * Math.PI / 180.0;

			const double marginX = 0;
			const double marginY = 30;

			var sut = Border3Child.TransformToVisual(Border2);
			var result = sut.TransformBounds(new Rect(0, 0, w, h));

			// Get expected compared to Border3Parent (like previous test: When_TransformToParent_With_InheritedCompositeTransform_And_Margin)
			var length = w * scale * Math.Cos(angle) + h * scale * Math.Cos(angle);
			var marginXProjection = marginX * scale * Math.Cos(angle); // as angle == 45 degree and we are using squares, projection is the same on X and Y
			var marginYProjection = marginY * scale * Math.Cos(angle);
			var origin = new Point(
				tX - length / 2 + marginXProjection - marginYProjection,
				tY - marginXProjection + marginYProjection);
			var expected = new Rect(origin, new Size(length, length));

			// Then apply the translation of B2
			expected = new Rect(expected.X + 50, expected.Y + 50, expected.Width, expected.Height);

			Assert.IsTrue(RectCloseComparer.UI.Equals(expected, result));
		}

		private void Run(Expression<Action> test)
		{
			var testMethod = (test.Body as MethodCallExpression)?.Method;
			if (testMethod == null || testMethod.Name.IsNullOrWhiteSpace())
			{
				throw new InvalidOperationException("Failed to get test");
			}

			if (testMethod.GetParameters().Any(p => !p.HasDefaultValue))
			{
				throw new InvalidOperationException("The test method must not have any required parameter");
			}

			if (!(FindName(testMethod.Name + "_Result") is TextBlock output))
			{
				throw new InvalidOperationException("Failed to get test output");
			}

			try
			{
				test.Compile()();

				output.Text = $"{testMethod.Name}: SUCCESS";
			}
			catch (Exception e)
			{
				output.Text = $"{testMethod.Name}: FAILED ({e.Message})";
			}
		}

		private class RectCloseComparer : IEqualityComparer<Rect>
		{
			private readonly double _epsilon;

			public static RectCloseComparer Default { get; } = new RectCloseComparer(double.Epsilon);

			public static RectCloseComparer UI { get; } = new RectCloseComparer(.0001);

			public RectCloseComparer(double epsilon)
			{
				_epsilon = epsilon;
			}

			/// <inheritdoc />
			public bool Equals(Rect left, Rect right)
				=> Math.Abs(left.X - right.X) < _epsilon
				&& Math.Abs(left.Y - right.Y) < _epsilon
				&& Math.Abs(left.Width - right.Width) < _epsilon
				&& Math.Abs(left.Height - right.Height) < _epsilon;

			/// <inheritdoc />
			public int GetHashCode(Rect obj)
				=> ((int)obj.Width)
				^ ((int)obj.Height);
		}

		private static class Assert
		{
			public static void IsTrue(bool actual)
			{
				if (!actual)
				{
					throw new AssertionFailedException(true, actual);
				}
			}

			public static void IsTrue(bool actual, string message)
			{
				if (!actual)
				{
					throw new AssertionFailedException(true, actual, message);
				}
			}

			public static void AreEqual(object expected, object actual)
			{
				if (!EqualityComparer<object>.Default.Equals(expected, actual))
				{
					throw new AssertionFailedException(expected, actual);
				}
			}

			public static void AreEqual(object expected, object actual, string message)
			{
				if (!EqualityComparer<object>.Default.Equals(expected, actual))
				{
					throw new AssertionFailedException(expected, actual, message);
				}
			}
		}

		private class AssertionFailedException : Exception
		{
			public AssertionFailedException(object expected, object actual)
				: base($"Assertion failed\r\n\texpected: '{expected?.ToString() ?? "null"}'\r\n\tactual: '{actual?.ToString() ?? "null"}'")
			{
			}

			public AssertionFailedException(object expected, object actual, string message)
				: base($"Assertion failed {message}\r\n\texpected: '{expected?.ToString() ?? "null"}'\r\n\tactual: '{actual?.ToString() ?? "null"}'")
			{
			}
		}
	}
}

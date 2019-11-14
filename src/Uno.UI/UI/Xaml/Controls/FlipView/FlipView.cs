﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		public FlipView()
		{
			this.RegisterDisposablePropertyChangedCallback(SelectedIndexProperty, (s, e) => (s as FlipView)?.OnSelectedIndexChanged((int)e.OldValue, (int)e.NewValue));
		}

		public bool UseTouchAnimationsForAllNavigation
		{
			get { return (bool)GetValue(UseTouchAnimationsForAllNavigationProperty); }
			set { SetValue(UseTouchAnimationsForAllNavigationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for UseTouchAnimationsForAllNavigation.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty UseTouchAnimationsForAllNavigationProperty =
			DependencyProperty.Register("UseTouchAnimationsForAllNavigation", typeof(bool), typeof(FlipView), new PropertyMetadata(true));

		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);

			if (HasItems)
			{
				this.SelectedIndex = 0;
			}
		}

		protected override void OnItemsChanged(object e)
		{
			base.OnItemsChanged(e);

			if (HasItems)
			{
				this.SelectedIndex = 0;
			}
		}

		private void OnSelectedIndexChanged(int oldValue, int newValue)
		{
			// Never animate for changes greater than next/previous item
			var smallChange = Math.Abs(newValue - oldValue) <= 1;
			OnSelectedIndexChangedPartial(oldValue, newValue, smallChange && UseTouchAnimationsForAllNavigation);
		}

		partial void OnSelectedIndexChangedPartial(int oldValue, int newValue, bool animateChange);
		
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new FlipViewItem() { IsGeneratedContainer = true };
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is FlipViewItem;
		}
	}
}

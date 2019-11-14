﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.ListViewBaseTests
{
	[TestClass]
	public class Given_ListViewBase
	{
#if !NETFX_CORE
		[TestMethod]
		public void When_MultiSelectedItem()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ListViewBase))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ItemsPresenter()
						)
					)
				}
			};

			var panel = new StackPanel();

			var SUT = new ListViewBase()
			{
				Style = style,
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					new Border { Name = "b1" },
					new Border { Name = "b2" }
				}
			};

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));
			Assert.IsNotNull(panel.FindName("b2"));

			SUT.SelectionMode = ListViewSelectionMode.Multiple;

			SUT.OnItemClicked(0);

			Assert.AreEqual(1, SUT.SelectedItems.Count);

			SUT.OnItemClicked(1);

			Assert.AreEqual(2, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_SingleSelectedItem_Event()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ListViewBase))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ItemsPresenter()
						)
					)
				}
			};

			var panel = new StackPanel();

			var item = new Border { Name = "b1" };
			var SUT = new ListViewBase()
			{
				Style = style,
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					item
				}
			};

			var model = new MyModel
			{
				SelectedItem = (object)null
			};

			SUT.SetBinding(
				Selector.SelectedItemProperty,
				new Binding()
				{
					Path = "SelectedItem",
					Source = model,
					Mode = BindingMode.TwoWay
				}
			);

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));

			var selectionChanged = 0;

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged++;
				Assert.AreEqual(item, SUT.SelectedItem);

				// In windows, when programmatically changed, the bindings are updated *after*
				// the event is raised, but *before* when the SelectedItem is changed from the UI.
				Assert.IsNull(model.SelectedItem);
			};

			SUT.SelectedIndex = 0;

			Assert.AreEqual(item, model.SelectedItem);

			Assert.IsNotNull(SUT.SelectedItem);
			Assert.AreEqual(1, selectionChanged);
			Assert.AreEqual(1, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_ResetItemsSource()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ListViewBase))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ItemsPresenter()
						)
					)
				}
			};

			var panel = new StackPanel();

			var SUT = new ListViewBase()
			{
				Style = style,
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ItemsSource = new int[] { 1, 2, 3 };
			SUT.OnItemClicked(0);

			SUT.ItemsSource = null;
		}
#endif

		[TestMethod]
		public void When_SelectionChanged_Changes_Selection()
		{
			var list = new ListView();
			list.ItemsSource = Enumerable.Range(0, 20);

			list.SelectionChanged += OnSelectionChanged;
			list.SelectedItem = 7;

			Assert.AreEqual(14, list.SelectedItem);
			Assert.AreEqual(14, list.SelectedIndex);

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				var l = sender as ListViewBase;
				l.SelectedItem = 14;
			}
		}

		[TestMethod]
		public void When_SelectionChanged_Changes_Selection_Repeated()
		{
			var list = new ListView();
			list.ItemsSource = Enumerable.Range(0, 20);
			var callbackCount = 0;

			list.SelectionChanged += OnSelectionChanged;
			list.SelectedItem = 7;

			Assert.AreEqual(14, list.SelectedItem);
			Assert.AreEqual(14, list.SelectedIndex);
			Assert.AreEqual(8, callbackCount); //Unlike eg TextBox.TextChanged there is no guard on reentrant modification

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				callbackCount++;
				var l = sender as ListViewBase;
				var selected = (int)l.SelectedItem;
				if (selected < 14)
				{
					selected++;
					l.SelectedItem = selected;
				}
			}
		}
	}

	public class MyModel
	{
		public object SelectedItem { get; set; }
	}

	public class MyItemsControl : ItemsControl
	{
		public int OnItemsChangedCallCount { get; private set; }

		protected override void OnItemsChanged(object e)
		{
			OnItemsChangedCallCount++;
			base.OnItemsChanged(e);
		}
	}
}

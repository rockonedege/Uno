#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.UI.Core;

namespace Windows.ApplicationModel.Core
{
	public partial class CoreApplicationView
	{
		private CoreApplicationViewTitleBar _titleBar;

		public CoreApplicationView()
		{
		}

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		public CoreApplicationViewTitleBar TitleBar
		{
			get
			{
				if (_titleBar == null)
				{
					_titleBar = new CoreApplicationViewTitleBar();
				}

				return _titleBar;
			}
		}
	}
}

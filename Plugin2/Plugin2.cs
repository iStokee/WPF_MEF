using MefApp.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Plugin2
{
	internal class Plugin2 : IPlugin
	{
		private Plugin2Window _plugin1Window;

		// IsVisible property
		public bool IsVisible { get; private set; }

		public string Name => "Example 2 Plugin";

		public void Execute()
		{
			if (_plugin1Window == null)
			{
				_plugin1Window = new Plugin2Window();
				_plugin1Window.Closed += (s, e) =>
				{
					IsVisible = false;
					_plugin1Window = null; // Cleanup reference
				};
			}

			if (!IsVisible)
			{
				IsVisible = true;
				_plugin1Window.Show();
			}
			else
			{
				_plugin1Window.Activate(); // Bring to front if already visible
			}
		}
	}
}

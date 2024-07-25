using MefApp.sdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Plugin1
{
    internal class Plugin1 : IPlugin
	{
		private Plugin1Window _plugin1Window;

		// IsVisible property
		public bool IsVisible { get; private set; }

		public string Name => "Example Plugin 1";

		public void Execute()
		{
			if (_plugin1Window == null)
			{
				_plugin1Window = new Plugin1Window();
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

		public void Close() 
		{
			_plugin1Window.Close();
		}
	}
}


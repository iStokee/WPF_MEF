using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using MefApp.sdk.Interfaces;
using MefApp.sdk.Classes;
using WPF_MEF.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WpfMefApp.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private List<IPlugin> _plugins = new List<IPlugin>();
		private ObservableCollection<ButtonModel> _buttons;
		private List<PluginLoadContext> _contexts = new List<PluginLoadContext>();

		public ObservableCollection<ButtonModel> Buttons
		{
			get { return _buttons; }
			set
			{
				_buttons = value;
				OnPropertyChanged(nameof(Buttons));
			}
		}

		public MainViewModel()
		{
			LoadPlugins();
			DisplayPlugins();
		}

		// Command to reload plugins
		public ICommand ReloadPluginsCommand => new RelayCommand(async () =>
		{
			// Capture currently open windows
			var openWindows = new List<string>();
			foreach (var plugin in _plugins)
			{
				if (plugin.IsVisible)
				{
					openWindows.Add(plugin.Name);
					// Close the window
					plugin.Close();
				}
			}

			UnloadPlugins();
			LoadPlugins();
			DisplayPlugins();


			// Reopen the windows that were open before
			foreach (var window in openWindows)
			{
				foreach (var plugin in _plugins)
				{
					if (plugin.Name == window)
					{
						plugin.Execute();
					}
				}
			}
			openWindows.Clear();

			await ClearShadowCopies();
		});

		private void CreateReloaderButton()
		{
			AddButton("Reload Plugins", ReloadPluginsCommand);
		}

		private void LoadPlugins()
		{
			string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

			if (!Directory.Exists(pluginsPath))
				Directory.CreateDirectory(pluginsPath);

			string shadowCopyRoot = Path.Combine(pluginsPath, "ShadowCopy");

			foreach (string dll in Directory.GetFiles(pluginsPath, "*.dll"))
			{
				try
				{
					// Create a unique shadow copy path for each plugin
					string shadowCopyDir = Path.Combine(shadowCopyRoot, Guid.NewGuid().ToString());
					Directory.CreateDirectory(shadowCopyDir);
					string shadowCopyPath = Path.Combine(shadowCopyDir, Path.GetFileName(dll));
					File.Copy(dll, shadowCopyPath, true);

					var alc = new PluginLoadContext(shadowCopyPath);
					_contexts.Add(alc);
					Assembly assembly = alc.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(shadowCopyPath)));
					var pluginTypes = assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

					foreach (var type in pluginTypes)
					{
						IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
						_plugins.Add(plugin);
					}
				}
				catch (Exception ex)
				{
					// Handle exceptions (e.g., log them)
					Console.WriteLine($"Error loading plugin from {dll}: {ex.Message}");
				}
			}
		}

		private void DisplayPlugins()
		{
			CreateReloaderButton();
			foreach (var plugin in _plugins)
			{
				if (Buttons == null)
					Buttons = new ObservableCollection<ButtonModel>();

				Button btn = new Button { Content = plugin.Name };
				btn.Click += (s, e) => plugin.Execute();
				AddButton(plugin.Name, new RelayCommand(() => plugin.Execute()));
			}
		}

		private async Task ClearShadowCopies()
		{
			string shadowCopyRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "ShadowCopy");
			if (Directory.Exists(shadowCopyRoot))
			{
				foreach (var dir in Directory.GetDirectories(shadowCopyRoot))
				{
					try
					{
						// Ensure the directory is no longer in use by waiting for successful unload
						await Task.Delay(1000); // Wait a bit more to ensure all resources are released
						Directory.Delete(dir, true);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error deleting shadow copy directory {dir}: {ex.Message}");
					}
				}
			}
		}

		private void UnloadPlugins()
		{
			foreach (var context in _contexts.ToList()) // Use ToList to avoid modification during iteration
			{
				context.Unload();
			}

			_contexts.Clear();
			_plugins.Clear();
			Buttons.Clear();

			// Force garbage collection multiple times to cleanup unloaded contexts
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		private async Task DeleteDirectoryWithRetryAsync(string dir, int maxRetries = 10, int delayMilliseconds = 100)
		{
			for (int i = 0; i < maxRetries; i++)
			{
				try
				{
					// First try and release all handles on the directory
					Directory.Delete(dir, true);
					return;
				}
				catch (IOException) when (i < maxRetries - 1)
				{
					await Task.Delay(delayMilliseconds);
				}
				catch (UnauthorizedAccessException) when (i < maxRetries - 1)
				{
					await Task.Delay(delayMilliseconds);
				}
			}
			// If the directory still cannot be deleted, throw an exception
			Directory.Delete(dir, true);
		}

		public void AddButton(string name, ICommand command)
		{
			if (Buttons == null)
				Buttons = new ObservableCollection<ButtonModel>();
			Buttons.Add(new ButtonModel { Name = name, Command = command });
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class PluginLoadContext : AssemblyLoadContext
	{
		private AssemblyDependencyResolver _resolver;

		public PluginLoadContext(string pluginPath) : base(isCollectible: true)
		{
			_resolver = new AssemblyDependencyResolver(pluginPath);
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
			if (assemblyPath != null)
			{
				return LoadFromAssemblyPath(assemblyPath);
			}
			return null;
		}
	}
}

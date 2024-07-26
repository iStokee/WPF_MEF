using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using MefApp.sdk.Interfaces;
using WPF_MEF.Models;
using System.Threading;
using MefApp.sdk.Classes;

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

		public ICommand ReloadPluginsCommand => new RelayCommand(() =>
		{
			var openWindows = new List<string>();
			foreach (var plugin in _plugins)
			{
				if (plugin.IsVisible)
				{
					openWindows.Add(plugin.Name);
				}
			}

			Thread.Sleep(500);

			UnloadPlugins();
			_plugins.Clear();
			Buttons.Clear();
			LoadPlugins();
			DisplayPlugins();

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
		});

		public ICommand UnloadPluginsCommand => new RelayCommand(() =>
		{
			UnloadPlugins();
			_plugins.Clear();
			Buttons.Clear();
			CreateReloaderButton();
			CreatePrinterButton();
		});

		public ICommand PrintPluginsCommand => new RelayCommand(() =>
		{
			Console.WriteLine("Loaded plugins:");
			foreach (var plugin in _plugins)
			{
				Console.WriteLine(plugin.Name);
			}

			Console.WriteLine("Loaded Contexts:");
			foreach (var context in _contexts)
			{
				foreach (var assembly in context.Assemblies)
				{
					Console.WriteLine($"Context loaded: {assembly.FullName}");
				}
			}
		});

		private void CreateReloaderButton()
		{
			AddButton("Reload Plugins", ReloadPluginsCommand);
		}

		private void CreateUnloaderButton()
		{
			AddButton("Unload Plugins", UnloadPluginsCommand);
		}

		private void CreatePrinterButton()
		{
			AddButton("Print Plugins", PrintPluginsCommand);
		}

		private void LoadPlugins()
		{
			string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

			if (!Directory.Exists(pluginsPath))
				Directory.CreateDirectory(pluginsPath);

			foreach (string dll in Directory.GetFiles(pluginsPath, "*.dll"))
			{
				try
				{
					var alc = new PluginLoadContext(dll);
					_contexts.Add(alc);
					Assembly assembly = alc.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(dll)));
					var pluginTypes = assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

					foreach (var type in pluginTypes)
					{
						IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
						_plugins.Add(plugin);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error loading plugin from {dll}: {ex.Message}");
				}
			}
		}

		private void DisplayPlugins()
		{
			CreateReloaderButton();
			CreateUnloaderButton();
			CreatePrinterButton();
			foreach (var plugin in _plugins)
			{
				if (Buttons == null)
					Buttons = new ObservableCollection<ButtonModel>();

				AddButton(plugin.Name, new RelayCommand(() => plugin.Execute()));
			}
		}

		private void UnloadPlugins()
		{
			foreach (var plugin in _plugins)
			{
				plugin.Dispose();
			}

			foreach (var context in _contexts)
			{
				context.Unload();
			}

			_plugins.Clear();
			_contexts.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
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

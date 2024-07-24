using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using MefApp.sdk;
using System.Reflection;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace WpfMefApp.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private List<IPlugin> _plugins = new List<IPlugin>();
		private ObservableCollection<ButtonModel> _buttons;

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

		private void LoadPlugins()
		{
			string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

			if (!Directory.Exists(pluginsPath))
				Directory.CreateDirectory(pluginsPath);

			foreach (string dll in Directory.GetFiles(pluginsPath, "*.dll"))
			{
				try
				{
					Assembly assembly = Assembly.LoadFrom(dll);
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
			foreach (var plugin in _plugins)
			{
				if (Buttons == null)
					Buttons = new ObservableCollection<ButtonModel>();

				Button btn = new Button { Content = plugin.Name };
				btn.Click += (s, e) => plugin.Execute();
				AddButton(plugin.Name, new RelayCommand(() => plugin.Execute()));
			}
		}

		public void AddButton(string name, ICommand command)
		{
			Buttons.Add(new ButtonModel { Name = name, Command = command });
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ButtonModel
	{
		public string Name { get; set; }
		public ICommand Command { get; set; }
	}
	public class RelayCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public RelayCommand(Action execute, Func<bool> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
		public void Execute(object parameter) => _execute();
		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}

}
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace WPF_MEF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		public App()
		{
			var assemblies = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
				.Select(Assembly.LoadFrom)
				.ToArray();

			ServiceCollection serviceCollection = new();
			serviceCollection.Scan(scan => 
				scan.FromAssemblies(assemblies)
				.AddClasses()
				.AsImplementedInterfaces()
				.WithSingletonLifetime());

			ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

			var mainWindow = (Window)serviceProvider.GetRequiredService<IAddChild>();
			mainWindow.Show();

		}
	}
}

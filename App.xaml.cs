using FastDevTool.ViewMode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace FastDevTool {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public IServiceProvider Services { get; }
        public new static App Current => (App)Application.Current;


        public App() { 
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices() {
            var services = new ServiceCollection();

            //services.AddSingleton<ILogger>(_ =>
            //{
            //    return new LoggerConfiguration().MinimumLevel
            //        .Debug()
            //        .WriteTo.File("log.txt")
            //        .CreateLogger();
            //});

            services.AddTransient<MainWindowViewMode>();
            services.AddTransient<MainWindow>(sp => new MainWindow { DataContext = sp.GetService<MainWindowViewMode>() });

            return services.BuildServiceProvider();
        }

        private void Application_Startup(object sender, StartupEventArgs e) {
            var mainWindow = Services.GetService<MainWindow>();
            mainWindow!.Show();
        }
    }
}

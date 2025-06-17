using Microsoft.Extensions.DependencyInjection;
using SimpleMD.Services;
using SimpleMD.ViewModels;

namespace SimpleMD.Helpers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSimpleMdServices(this IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IMarkdownService, MarkdownService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDialogService, DialogService>();
            
            // Register ViewModels
            services.AddTransient<MainViewModel>();
            
            // Register Windows
            services.AddSingleton<MainWindow>();
            
            return services;
        }
    }
}

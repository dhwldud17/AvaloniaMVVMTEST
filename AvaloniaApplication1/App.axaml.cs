using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaApplication1.ViewModels;
using AvaloniaApplication1.Views;
using Autofac;
using Basler.Pylon;
using System.Threading;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1;

public partial class App : Application
{

    public static IContainer Container { get; private set; }



    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var containerBuilder = new ContainerBuilder();

        // 의존성 등록
        containerBuilder.RegisterType<CameraManager>().AsSelf();
        containerBuilder.RegisterType<ImageManager>().AsSelf();
        containerBuilder.RegisterType<MainWindowViewModel>().AsSelf();
        containerBuilder.RegisterType<MainWindow>().AsSelf(); // MainWindow 등록 추가
        containerBuilder.RegisterType<Basler.Pylon.Camera>().AsSelf();
   

        Container = containerBuilder.Build();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Container.Resolve<MainWindow>();
            mainWindow.DataContext = Container.Resolve<MainWindowViewModel>();

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

}
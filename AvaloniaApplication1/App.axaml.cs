using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaApplication1.ViewModels;
using AvaloniaApplication1.Views;
using Autofac;
using Basler.Pylon;
using System.Threading;

namespace AvaloniaApplication1;

public partial class App : Application
{

    public static IContainer Container { get; private set; }



    public override void Initialize()
    {

        var builder = new ContainerBuilder();

        //viewmodel 등록
        builder.RegisterType<MainWindowViewModel>().InstancePerDependency(); //매번 새로운 객체생성

        //AsSelf()???
        //Camera serialNumber따라 새로생성되도록 등록
        builder.Register((c, p) => //lambda
        {
            var serial = p.Named<string>("serial");
            return new Camera(serial);
        }).InstancePerDependency(); //매번 새로운 객체생성

        //CancellationTokenSource 등록
        builder.RegisterType<CancellationTokenSource>().
            InstancePerDependency(); //매번 새로운 객체생성

        Container = builder.Build();
        //AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //var mainWindow = new MainWindow
            //{
            //    DataContext = Container.Resolve<MainWindowViewModel>(), // MainWindowViewModel을 DI로 주입
            //};
            var viewModel = Container.Resolve<MainWindowViewModel>();
            var mainWindow = new MainWindow(viewModel);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

}
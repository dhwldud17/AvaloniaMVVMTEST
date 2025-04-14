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

        //viewmodel ���
        builder.RegisterType<MainWindowViewModel>().InstancePerDependency(); //�Ź� ���ο� ��ü����

        //AsSelf()???
        //Camera serialNumber���� ���λ����ǵ��� ���
        builder.Register((c, p) => //lambda
        {
            var serial = p.Named<string>("serial");
            return new Camera(serial);
        }).InstancePerDependency(); //�Ź� ���ο� ��ü����

        //CancellationTokenSource ���
        builder.RegisterType<CancellationTokenSource>().
            InstancePerDependency(); //�Ź� ���ο� ��ü����

        Container = builder.Build();
        //AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //var mainWindow = new MainWindow
            //{
            //    DataContext = Container.Resolve<MainWindowViewModel>(), // MainWindowViewModel�� DI�� ����
            //};
            var viewModel = Container.Resolve<MainWindowViewModel>();
            var mainWindow = new MainWindow(viewModel);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

}
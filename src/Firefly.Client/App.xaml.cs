// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows;
using System.Threading;
using System.Reflection;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Runtime.InteropServices;

using log4net;

using Amur.Common;

using Firefly.Client.Common;
using Firefly.Client.Control;
using Firefly.Client.Service;

namespace Firefly.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MainWindow MainView { get; private set; }

        public const int SW_HIDE = 0; //隐藏窗口
        public const int SW_SHOW = 5; //Activates the window and displays it in its current size and position.

        private static ILog _logger = LogManager.GetLogger(typeof(App));
        private static ISplashScreen _splashScreen;

        // Start splash window is 2 seconds
        private const int SPLASH_TIME = 2 * 1000;

        private ManualResetEvent _resetSplashCreated;
        private Thread _splashThread;
        private string _title;

        protected override async void OnStartup(StartupEventArgs e)
        {
            string appName = Assembly.GetExecutingAssembly().GetName().Name;
            using (Mutex mutex = new Mutex(true, appName, out bool createdNew))
            {
                try
                {
                    //If process exists in task manager, kill current process with show message
                    if (!createdNew)
                    {
                        IRtekMessageBox.ShowDialog(null, I18n.Properties.Resources.AlreadyRunning);
                        Environment.Exit(Environment.ExitCode);
                    }
                    else
                    {
                        AppConfig.Instance = new AppConfig();
                        AppConfig.Instance.Load();

                        // disable HW acceleration to avoid ui cannot refresh ui after unlock screen.
                        // https://github.com/dotnet/wpf/issues/4276
                        if (!AppConfig.Instance.EnableHardwareAcc)
                        {
                            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                        }

                        SetupExceptionHandling();

                        AppPreference.Instance.Load();

                        // Lite UI always displays Celsius; sample/alarm data from the engine is Kelvin.
                        TemperatureFormatInfo.CurrentInfo.Unit = TemperatureUnit.Celsiur;
                        TemperatureDifferenceFormatInfo.CurrentInfo.Unit = TemperatureUnit.Celsiur;

                        // ManualResetEvent acts as a block.  It waits for a signal to be set.
                        _resetSplashCreated = new ManualResetEvent(false);

                        // Create a new thread for the splash screen to run on
                        _splashThread = new Thread(ShowSplash);
                        _splashThread.SetApartmentState(ApartmentState.STA);
                        _splashThread.IsBackground = true;
                        _splashThread.Start();

                        // Wait for the blocker to be signaled before continuing. This is essentially the same as: while(ResetSplashCreated.NotSet) {}
                        _resetSplashCreated.WaitOne();

                        base.OnStartup(e);

                        LivePreviewPreference.Intance.Load();
                        SystemConfigPreference.Intance.Load();
                        MainWindowPreference.Instance.Load();

                        TrackingService.Instance.Start();

                        Firefly.Client.Service.Abstractions.FireRuntimeServices.ConfigureFromAppSettings();
                        Firefly.Client.Service.Abstractions.FireRuntimeServices.CaptureUiSynchronizationContext();

                        // Store + Fire both run in-process inside MoniTiRLite.exe.
                        await Firefly.Client.Service.Abstractions.StoreRuntimeServices.StartAsync();
                        await Firefly.Client.Service.Abstractions.FireRuntimeServices.StartAsync();

                        // OWB/Fire init may change display unit; keep UI on Celsius.
                        TemperatureFormatInfo.CurrentInfo.Unit = TemperatureUnit.Celsiur;
                        TemperatureDifferenceFormatInfo.CurrentInfo.Unit = TemperatureUnit.Celsiur;

                        if (AppConfig.Instance.ShowSplash)
                        {
                            Thread.Sleep(SPLASH_TIME);
                        }
                        _splashScreen?.LoadComplete();

                        log4net.GlobalContext.Properties["MainThread"] = Thread.CurrentThread.ManagedThreadId;

                        MainWindowPreference.Instance.Title = _title;
                        MainWindowPreference.Instance.Save();

                        MainView = new MainWindow();
                        this.MainWindow = MainView;
                        MainView.ShowDialog();
                    }
                }
                finally
                {
                    try
                    {
                        Firefly.Client.Service.Abstractions.StoreRuntimeServices.StopAsync()
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }

                    try
                    {
                        Firefly.Client.Service.Abstractions.FireRuntimeServices.StopAsync()
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }

                    LiteServerHost.StopOwned();
                    Environment.Exit(0);
                    // close resource handle
                }
            }
        }

        private void ShowSplash()
        {
            if (AppConfig.Instance.ShowSplash)
            {
                try
                {
                    bool isAlarmOutEnable = true;
                    string title = string.Empty;
                    Task task = Task.Run(async () =>
                    {
                        string language = AppPreference.Instance.SystemLanguage;
                        var tempLanguage = await SystemConfigManager.Instance.GetI18n();
                        if (!string.IsNullOrWhiteSpace(tempLanguage))
                        {
                            language = tempLanguage;
                        }
                        var commonConfig = await SystemConfigManager.Instance.GetCommonConfig();
                        isAlarmOutEnable = commonConfig?.IsAlarmOutEnable == true;

                        AdminAgentConfig adminAgentConfig = await AgentManager.Instance.GetAdminAgentConfig(ServerManager.Instance.ServerManagerURL);

                        if (adminAgentConfig != null)
                        {
                            if (language == Constants.ZH_CN_LANGUAGE)
                            {
                                title = adminAgentConfig.Name;
                            }
                            else
                            {
                                title = adminAgentConfig.NameEN;
                            }
                        }
                        AppPreference.Instance.SystemLanguage = language;
                        AppPreference.Instance.Save();
                    });
                    task.Wait(3000);
                    _title = title;
                    AppPreference.Instance.IsAlarmOutEnable = isAlarmOutEnable;
                }
                catch
                {
                }
                // Create the window
                SplashWindow splashWindow = new SplashWindow();
                _splashScreen = splashWindow;

                // Show it
                splashWindow.Show();
            }

            // Now that the window is created, allow the rest of the startup to run
            _resetSplashCreated.Set();
            System.Windows.Threading.Dispatcher.Run();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            };

            DispatcherUnhandledException += (s, e) =>
            {
                e.Handled = true;
                Exception baseException = e.Exception.GetBaseException();
                if (baseException.GetType() == typeof(NetworkException))
                {
                    if (App.Current.Windows.Count > 0)
                    {
                        for (int i = 0; i < App.Current.Windows.Count; i++)
                        {
                            if (App.Current.Windows[i].GetType() == typeof(IRtekMessageBox))
                            {
                                return;
                            }
                        }
                    }
                    //IRtekMessageBox.ShowDialog(App.Current.MainWindow, I18n.Properties.Resources.NetworkException, I18n.Properties.Resources.Tip);
                    LogUnhandledException(baseException, "");
                    return;
                }

                if (baseException.GetType() == typeof(Exception))

                    LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

                //MessageBox.Show(e.Exception.ToString());
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception ex, string source)
        {
            string message = "";

            try
            {
                message = $"{source}\n{ex}";
            }
            finally
            {
                TrackingService.Instance.HasAError(ex.GetType());
                _logger.Error(message);
            }
        }
    }
}
using System.Windows;
using BusinessInventorySystem.Services;

namespace BusinessInventorySystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InventoryService.Instance.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // The service autosaves after each mutation; this is a final safety net.
            InventoryService.Instance.Save();
            base.OnExit(e);
        }
    }
}

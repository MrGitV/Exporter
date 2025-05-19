using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace Exporter
{
    // Handles application startup and culture configuration
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set application-wide culture settings
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Set XML language for proper localization
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);
        }
    }
}

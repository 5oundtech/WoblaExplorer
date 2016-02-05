using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using WoblaExplorer.Properties;

namespace WoblaExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static List<CultureInfo> m_Languages = new List<CultureInfo>();

        public static List<CultureInfo> Languages
        {
            get { return m_Languages; }
        }

        public static event EventHandler LanguageChanged;
        public static CultureInfo Language
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value == Thread.CurrentThread.CurrentUICulture) return;

                Thread.CurrentThread.CurrentUICulture = value;

                ResourceDictionary dict = new ResourceDictionary();
                switch (value.Name)
                {
                    case "ru-RU":
                        dict.Source = new Uri($"Resources/lang.{value.Name}.xaml", UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("Resources/lang.xaml", UriKind.Relative);
                        break;
                }

                ResourceDictionary oldDict = (from d in Current.Resources.MergedDictionaries
                    where d.Source != null && d.Source.OriginalString.StartsWith("Resources/lang.")
                    select d).First();

                if (oldDict != null)
                {
                    int ind = Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Current.Resources.MergedDictionaries.Remove(oldDict);
                    Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Current.Resources.MergedDictionaries.Add(dict);
                }

                LanguageChanged(Current, new EventArgs());
            }
        }

        public App()
        {
            LanguageChanged += App_LanguageChanged;

            m_Languages.Clear();
            m_Languages.Add(new CultureInfo("en-GB"));
            m_Languages.Add(new CultureInfo("ru-RU"));

        }

        private void Application_LoadCompleted(object sender, NavigationEventArgs e)
        {
            Language = Settings.Default.DefaultLanguage;
        }

        private void App_LanguageChanged(object sender, EventArgs e)
        {
            Settings.Default.DefaultLanguage = Language;
            Settings.Default.Save();
        }
    }
}

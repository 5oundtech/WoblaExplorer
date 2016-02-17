using System.Threading;

namespace WoblaExplorer.Util
{
    public static class Constants
    {
        public static string DateTimeFormat
        {
            get
            {
                if (App.Language.Name == "ru-RU")
                {
                    return "dd.MM.yyyy HH:mm";
                }
                return "dd.MM.yyyy h:mm tt";
            }
        }
    }
}

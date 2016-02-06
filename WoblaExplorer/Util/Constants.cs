using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WoblaExplorer.Util
{
    public static class Constants
    {
        //public const string DateTimeFormat = "dd.MM.yyyy HH:mm";

        public static string DateTimeFormat
        {
            get
            {
                if (Thread.CurrentThread.CurrentCulture.Name == "ru-RU")
                {
                    return "dd.MM.yyyy HH:mm";
                }
                return "dd.MM.yyyy h:mm tt";
            }
        }
    }
}

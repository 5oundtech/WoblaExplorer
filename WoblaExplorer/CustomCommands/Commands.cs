using System.Windows.Input;

namespace WoblaExplorer.CustomCommands
{
    public static class Commands
    {
        public static RoutedCommand ShowInExplorer = new RoutedCommand("ShowInExplorer", typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.E, ModifierKeys.Control)});

        public static RoutedCommand Rename = new RoutedCommand("Rename", typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.R, ModifierKeys.Control)});

        public static RoutedCommand CopyTo = new RoutedCommand("CopyTo", typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.C, ModifierKeys.Control)});

        public static RoutedCommand Remove = new RoutedCommand("Remove", typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.Delete, ModifierKeys.Shift)});

        public static RoutedCommand Exit = new RoutedCommand("Exit", typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.F4, ModifierKeys.Alt)});

        public static RoutedCommand AboutDialog = new RoutedCommand("AboutDialog", typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.F1, ModifierKeys.None)});

        public static RoutedCommand PropertiesDialog = new RoutedCommand(nameof(PropertiesDialog), typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.F12)});

        public static RoutedCommand ReRunAsAdministrator = new RoutedCommand(nameof(ReRunAsAdministrator),
            typeof (Commands), new InputGestureCollection {});

        public static RoutedCommand CreateDirectory = new RoutedCommand(nameof(CreateDirectory), typeof (Commands),
            new InputGestureCollection {new KeyGesture(Key.N, ModifierKeys.Control)});

        public static RoutedCommand CheckForUpdates = new RoutedCommand(nameof(CheckForUpdates), typeof (Commands),
            new InputGestureCollection());

        public static RoutedCommand ModifySelection = new RoutedCommand(nameof(ModifySelection), typeof (Commands));
    }
}

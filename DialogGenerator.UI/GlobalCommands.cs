using System.Windows.Input;

namespace DialogGenerator.UI
{
    public static class GlobalCommands
    {
        public static RoutedUICommand OpenCharacterFormCommand { get; } = new RoutedUICommand("", "OpenCharacterFormCommand", typeof(GlobalCommands));
        public static RoutedUICommand ImportCharacterCommand { get; } = new RoutedUICommand("", "ImportCharacterCommand", typeof(GlobalCommands));
        public static RoutedUICommand ExportCharacterCommand { get; } = new RoutedUICommand("", "ExportCharacterCommand", typeof(GlobalCommands));
        public static RoutedUICommand EditWithJSONEditorCommand { get; } = new RoutedUICommand("", "ExportWithJSONEditorCommand", typeof(GlobalCommands));
    }
}

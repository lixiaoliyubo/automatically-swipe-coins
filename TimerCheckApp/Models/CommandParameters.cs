using System.Windows.Input;

namespace TimerCheckApp.Models;

public class CommandParameters
{
    public CommandParameters(object _FirstParameter, RoutedEventArgs _MouseEventArgs)
    {
        FirstParameter = _FirstParameter;
        MouseEventArgs = _MouseEventArgs;
    }
    public object FirstParameter { get; set; }
    public RoutedEventArgs MouseEventArgs { get; set; }
}

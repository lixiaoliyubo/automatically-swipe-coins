
namespace TimerCheckApp.Models;

public class EnumerationMember
{
    public string Description { get; set; } = "";
    public object Value { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsChecked { get; set; } = false;
}

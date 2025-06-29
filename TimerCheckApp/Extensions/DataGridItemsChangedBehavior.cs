using Microsoft.Xaml.Behaviors;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using DataGrid = System.Windows.Controls.DataGrid;

namespace TimerCheckApp.Extensions;

public class DataGridItemsChangedBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        var itemsControl = AssociatedObject as ItemsControl;
        var itemsSourcePropertyDescriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty, typeof(ItemsControl));

        itemsSourcePropertyDescriptor.AddValueChanged(itemsControl, ItemsSourceChanged);
    }

    private void ItemsSourceChanged(object sender, EventArgs e)
    {
        var dataGrid = sender as DataGrid;
        if (dataGrid?.ItemsSource is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged -= ItemsCollectionChanged;
            incc.CollectionChanged += ItemsCollectionChanged;
        }
    }

    private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // 数据变化时执行的操作
        var command = GetValue(ScrollCommandProperty) as ICommand;
        command?.Execute(AssociatedObject);
    }

    public static readonly DependencyProperty ScrollCommandProperty =
        DependencyProperty.Register("ScrollCommand", typeof(ICommand), typeof(DataGridItemsChangedBehavior));

    public ICommand ScrollCommand
    {
        get => (ICommand)GetValue(ScrollCommandProperty);
        set => SetValue(ScrollCommandProperty, value);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        var itemsControl = AssociatedObject as ItemsControl;
        var itemsSourcePropertyDescriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty, typeof(ItemsControl));

        itemsSourcePropertyDescriptor.RemoveValueChanged(itemsControl, ItemsSourceChanged);
    }
}
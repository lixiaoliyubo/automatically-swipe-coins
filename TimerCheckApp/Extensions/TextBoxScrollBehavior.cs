using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TimerCheckApp.Extensions
{
    /// <summary>
    /// TextBox滚动行为，用于在文本更新时自动滚动到底部
    /// </summary>
    public class TextBoxScrollBehavior : Behavior<System.Windows.Controls.TextBox>
    {
        /// <summary>
        /// 滚动命令依赖属性
        /// </summary>
        public static readonly DependencyProperty ScrollCommandProperty =
            DependencyProperty.Register("ScrollCommand", typeof(ICommand), typeof(TextBoxScrollBehavior), new PropertyMetadata(null));

        /// <summary>
        /// 滚动命令
        /// </summary>
        public ICommand ScrollCommand
        {
            get { return (ICommand)GetValue(ScrollCommandProperty); }
            set { SetValue(ScrollCommandProperty, value); }
        }

        /// <summary>
        /// 附加行为时调用
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.TextChanged += AssociatedObject_TextChanged;
        }

        /// <summary>
        /// 分离行为时调用
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
            base.OnDetaching();
        }

        /// <summary>
        /// 文本变更事件处理
        /// </summary>
        private void AssociatedObject_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ScrollCommand != null && ScrollCommand.CanExecute(null))
            {
                ScrollCommand.Execute(null);
            }
            else
            {
                // 如果没有命令，则自动滚动到底部
                AssociatedObject.ScrollToEnd();
            }
        }
    }
}
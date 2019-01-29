using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace 资源搜索神器
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ResourceAddress> sa = new ObservableCollection<ResourceAddress>();
        private Searcher sear;
        public MainWindow()
        {
            InitializeComponent();
            result.ItemsSource = sa;
        }

        private void SearchStart(object sender, RoutedEventArgs e)
        {
            if (sear == null || !sear.isrun)
            {
                int time, maxthread;
                try
                {
                    time = int.Parse(SearchTime.Text) * 1000;
                }
                catch
                {
                    MessageBox.Show("不能输入字符或小数哦！");
                    SearchTime.Text = "";
                    return;
                }
                try
                {
                    maxthread = int.Parse(MaxThread.Text);
                }
                catch
                {
                    MessageBox.Show("不能输入字符或小数哦！");
                    MaxThread.Text = "";
                    return;
                }
                sear = new Searcher(time, maxthread);
                sear.AddAddress += (s) =>
                {
                    this.Dispatcher.Invoke(delegate {
                        for(int i = 0;i<s.Count;i++)
                            sa.Add(s[i]);
                    });
                };
                sear.End += delegate
                {
                    this.Dispatcher.Invoke(() => Search.Content = "搜索");
                    MessageBox.Show("搜索完成，共搜到" + sear.SourceAddresses.Count + "个地址");
                };
                sa.Clear();
                sear.Start(SearchText.Text);
                Search.Content = "停止";
            }
            else
            {
                sear.Stop();
            }
        }

        private void AddResourceType(object sender, RoutedEventArgs e)
        {
            new AddTypeWindow().ShowDialog();
        }
    }
}

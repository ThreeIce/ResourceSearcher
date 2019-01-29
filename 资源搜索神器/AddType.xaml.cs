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
using System.Windows.Shapes;

namespace 资源搜索神器
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class AddTypeWindow : Window
    {
        public AddTypeWindow()
        {
            InitializeComponent();
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            if(Name.Text == null || Name.Text == "")
            {
                MessageBox.Show("必须填名称！");
                return;
            }
            if(AddressRegex.Text == null || AddressRegex.Text == "")
            {
                MessageBox.Show("必须填资源匹配正则！");
                return;
            }
            Searcher.AddResourceType(new ResourceTypeInfo(Name.Text, AddressRegex.Text, pwRegex.Text, verifyRegex.Text));
            MessageBox.Show("添加成功");
            this.Close();
        }
    }
}

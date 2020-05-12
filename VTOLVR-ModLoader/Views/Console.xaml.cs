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

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class Console : UserControl
    {
        public List<Feed> consoleFeed = new List<Feed>();
        public Console()
        {
            InitializeComponent();
        }

        public void UpdateFeed()
        {
            console.ItemsSource = consoleFeed;
        }
        public void UpdateFeed(string newMessage)
        {
            consoleFeed.Add(new Feed(newMessage));
            console.ItemsSource = consoleFeed.ToArray();
            System.Console.WriteLine(newMessage);
        }

        public class Feed
        {
            public string message { get; set; }

            public Feed(string message)
            {
                this.message = message;
            }
        }
    }
}

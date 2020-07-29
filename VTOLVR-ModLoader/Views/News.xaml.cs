using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using VTOLVR_ModLoader.Classes;

namespace VTOLVR_ModLoader.Views
{
    public partial class News : UserControl
    {
        public News()
        {
            InitializeComponent();
        }

        public void LoadNews()
        {
            if (Program.Releases != null)
            {
                updateFeed.ItemsSource = Program.Releases.ToArray();
            }
            else
            {
                NoInternet();
            }
        }

        private void NoInternet()
        {
            updateFeed.ItemsSource = new Release[1] { new Release() };
            Console.Log("Can't connect to internet");
        }
    }
}

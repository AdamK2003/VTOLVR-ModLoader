using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using VTOLVR_ModLoader.Classes;

namespace VTOLVR_ModLoader.Windows
{
    /// <summary>
    /// Interaction logic for Notification.xaml
    /// </summary>
    public partial class Notification : Window
    {
        public enum Results { None, Ok,No,Yes,Cancel}
        public enum Buttons { None, Ok, NoYes, OkCancel}
        public Notification()
        {
            InitializeComponent();
        }
        public static void Show(string message, string title = "Message", Buttons buttons = Buttons.None)
        {
            Notification window = new Notification();
            window.textBlock.Text = message;
            window.Title = title;
            window.titleText.Text = " " + title;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.Topmost = true;
            window.Show();
        }
        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Moving Window
        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private void TopBarDown(object sender, MouseButtonEventArgs e)
        {
            holdingDown = true;
            lm = Mouse.GetPosition(this);
        }

        private void TopBarUp(object sender, MouseButtonEventArgs e)
        {
            holdingDown = false;
        }

        private void TopBarMove(object sender, MouseEventArgs e)
        {
            if (holdingDown)
            {
                this.Left += Mouse.GetPosition(this).X - lm.X;
                this.Top += Mouse.GetPosition(this).Y - lm.Y;
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {

        }

        private void TopBarLeave(object sender, MouseEventArgs e)
        {
            holdingDown = false;
        }

        #endregion
    }
}

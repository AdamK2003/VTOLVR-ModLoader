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
    /// Interaction logic for ModCreator.xaml
    /// </summary>
    public partial class ProjectManager : UserControl
    {
        public ProjectManager()
        {
            InitializeComponent();
        }

        public void SetUI()
        {
            settingsText.Visibility = Visibility.Visible;
            newProjectButton.IsEnabled = false;

            CheckProjectPath();
        }

        private void CheckProjectPath()
        {
            if (string.IsNullOrEmpty(Settings.projectsFolder))
                return;
            settingsText.Visibility = Visibility.Hidden;
            newProjectButton.IsEnabled = true;
        }

        private void NewProject(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenPage(new NewProject());
        }
    }
}

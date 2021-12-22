using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Core.Classes;
using Core.Jsons;
using Console = Launcher.Views.Console;

namespace Launcher.Windows
{
    public partial class MaterialWindow : Window
    {
        private List<Property> _properties = new List<Property>();
        private List<string> _fileNames;
        private DirectoryInfo _root;
        private string _name;
        private BaseItem _item;

        public MaterialWindow(DirectoryInfo directory, string materialName, ref BaseItem item)
        {
            _root = directory;
            _name = materialName;
            _item = item;
            UpdateFilesList();
            InitializeComponent();
            Title.Text = $"Editing {_name}";
        }
        
        private void AddProperty(object sender, RoutedEventArgs e)
        {
            _properties.Add(new Property(ref _fileNames)
            {
                PropertyName = _properties.Count.ToString()
            });
            UpdateList();
        }

        private void UpdateList()
        {
            List.ItemsSource = _properties;
        }

        private void UpdateFilesList()
        {
            FileInfo[] textures = _root.GetFiles("*.png");
            _fileNames = new List<string>(textures.Length);

            for (int i = 0; i < textures.Length; i++)
            {
                _fileNames.Add(textures[i].Name);
            }
        }

        private void UpdatePropertyList(ref BaseItem item)
        {
            
        }
        
        private class Property
        {
            public string PropertyName { get; set; }
            public string FileName { get; set; }
            public List<string> FilesList { get; }

            public Property(ref List<string> files)
            {
                FilesList = files;
                FileName = files.First();
            }
        }

        private void FileSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Console.Log($"{((Property)comboBox.DataContext).FileName}");    
        }
    }
}
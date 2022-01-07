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
        public Material Material;
        
        private List<Property> _properties = new List<Property>();
        private List<string> _fileNames;
        private DirectoryInfo _root;
        private string _name;
        private BaseItem _item;

        public MaterialWindow(ref Material material, ref BaseItem item)
        {
            DataContext = this;
            _root = item.Directory;
            _name = material.Name;
            _item = item;
            Material = material;
            UpdateFilesList();
            InitializeComponent();
            GetProperties();
            TitleText.Text = $"Editing {_name}";
            Title = TitleText.Text;
            MaterialNameInput.Text = _name;
        }

        private void GetProperties()
        {
            foreach (KeyValuePair<string,string> valuePair in Material.Textures)
            {
                _properties.Add(new Property(ref _fileNames)
                {
                    PropertyName = valuePair.Key,
                    FileName = valuePair.Value
                });
            }
            UpdateList();
        }
        
        private void AddProperty(object sender, RoutedEventArgs e)
        {
            // This makes it so you can't get the same name twice in 
            // dictionary 
            int number = _properties.Count;
            string name = number.ToString();
            while (Material.Textures.TryGetValue(name, out string value))
            {
                number++;
                name = number.ToString();
            }
            
            Property newProperty = new Property(ref _fileNames)
            {
                PropertyName = name
            };
            _properties.Add(newProperty);
            AddTexture(newProperty);
            UpdateList();
        }

        private void AddTexture(Property property)
        {
            Material.Textures.Add(property.PropertyName, property.FileName);
        }

        private void UpdateList()
        {
            List.ItemsSource = _properties;
            List.Items.Refresh();
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

        private void UpdatePropertyList()
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

        private void MaterialNameChanged(object sender, TextChangedEventArgs e)
        {
            if (MaterialNameInput == null)
                return;
            
            _name = MaterialNameInput.Text;
            TitleText.Text = $"Editing {_name}";
            Title = TitleText.Text;
            Material.Name = _name;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RelicHelper
{
    public partial class SpellBookWindow : Window
    {
        public ObservableCollection<SpellBookEntry> Spells { get; set; }

        public SpellBookWindow()
        {
            InitializeComponent();
            
            // Populate Hotkey ComboBox - Only F1 to F12
            var keys = Enum.GetValues(typeof(Key)).Cast<Key>().Where(k => (k >= Key.F1 && k <= Key.F12)).OrderBy(k => k.ToString());
            HotkeyColumn.ItemsSource = keys;

            Spells = LoadSpells();
            SpellDataGrid.ItemsSource = Spells;
        }

        private ObservableCollection<SpellBookEntry> LoadSpells()
        {
            try
            {
                string json = Properties.Settings.Default.SpellBookData;
                if (!string.IsNullOrEmpty(json))
                {
                    var list = JsonSerializer.Deserialize<List<SpellBookEntry>>(json);
                    if (list != null) return new ObservableCollection<SpellBookEntry>(list);
                }
            }
            catch (Exception)
            {
                // Silent catch, return empty
            }
            return new ObservableCollection<SpellBookEntry>();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure we commit any active edits in the datagrid
                SpellDataGrid.CommitEdit();
                
                string json = JsonSerializer.Serialize(Spells.ToList());
                Properties.Settings.Default.SpellBookData = json;
                Properties.Settings.Default.Save();
                
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving: " + ex.Message);
            }
        }

        private void AddDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            Spells.Add(new SpellBookEntry("Haste", Key.F6, 30.0));
            Spells.Add(new SpellBookEntry("Strong Haste", Key.F7, 20.0));
            Spells.Add(new SpellBookEntry("Healing Flow", Key.F5, 18.0));
            Spells.Add(new SpellBookEntry("Invisible", Key.F8, 200.0));
            Spells.Add(new SpellBookEntry("Magic Shield", Key.F9, 60.0));
        }

        private void AddSpellButton_Click(object sender, RoutedEventArgs e)
        {
            Spells.Add(new SpellBookEntry("New Spell", Key.None, 2.0));
        }

        private void DeleteSpell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SpellBookEntry entry)
            {
                Spells.Remove(entry);
            }
        }
    }
}

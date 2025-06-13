using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
// Assuming SettingInfo might be defined elsewhere or is simple enough to define here
// If DAS.Model.SettingInfo exists and is needed, ensure the reference is correct.
// For now, let's define a simple SettingInfo within IniManager.
// using DAS.Model; // Remove or adjust if SettingInfo is defined differently

namespace OthinCloud.API // Changed namespace to match project structure
{
    public class IniManager
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private string filepath;
        public IniManager(string filepath)
        {
            // Use Path.Combine for better path handling, especially if using relative paths
            this.filepath = Path.GetFullPath(filepath); // Store the full path
            Sections = new List<Section>();
            Load();
        }

        public void WriteIni(string section, string key, string val)
        {
            WritePrivateProfileString(section, key, val, filepath);
        }

        public string ReadIni(string section, string key, string defaultValue = "") // Added default value parameter
        {
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, temp, 255, filepath);
            return temp.ToString();
        }
        public List<string> GetKeys(string section)
        {
            var sectionData = Sections.FirstOrDefault(s => s.SectionName == section);
            return sectionData?.SectionItem.Select(item => item.Key).ToList() ?? new List<string>();
        }
        public List<Section> Sections { get; set; }
        public bool KeyExists(string section, string key)
        {
            var sectionData = Sections.FirstOrDefault(s => s.SectionName == section);
            if (sectionData != null)
            {
                return sectionData.SectionItem.Any(item => item.Key == key);
            }
            return false;
        }
        private void Load()
        {
            if (!File.Exists(filepath))
            {
                 // Consider creating the file or logging a warning instead of throwing immediately
                 Console.WriteLine($"Warning: INI file not found at {filepath}. Default values will be used.");
                 // Or: throw new FileNotFoundException("INI 文件未找到", filepath);
                 return; // Exit load if file doesn't exist
            }

            Sections.Clear(); // Clear existing sections before loading
            Section currentSection = null;
            try
            {
                foreach (var line in File.ReadLines(filepath))
                {
                    string trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith(";") || string.IsNullOrWhiteSpace(trimmedLine)) // Ignore comments and empty lines
                    {
                        continue;
                    }

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        string sectionName = trimmedLine.Trim('[', ']');
                        currentSection = Sections.FirstOrDefault(s => s.SectionName == sectionName);
                        if (currentSection == null) // Only add if it doesn't exist
                        {
                             currentSection = new Section { SectionName = sectionName };
                             Sections.Add(currentSection);
                        }
                    }
                    else if (currentSection != null && trimmedLine.Contains("="))
                    {
                        var parts = trimmedLine.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                             string key = parts[0].Trim();
                             string value = parts[1].Trim(' ', '"'); // Trim spaces and quotes

                             // Update existing key or add new one
                             var existingSetting = currentSection.SectionItem.FirstOrDefault(si => si.Key == key);
                             if (existingSetting != null)
                             {
                                 existingSetting.Value = value;
                             }
                             else
                             {
                                 currentSection.SectionItem.Add(new SettingInfo
                                 {
                                     Key = key,
                                     Value = value
                                 });
                             }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading INI file: {ex.Message}");
                // Handle the error appropriately, maybe re-throw or log
            }
        }

        // Define Section and SettingInfo classes within IniManager or ensure they are accessible
        public class Section
        {
            public string SectionName { get; set; }
            public List<SettingInfo> SectionItem { get; set; } = new List<SettingInfo>();
        }

        public class SettingInfo
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}

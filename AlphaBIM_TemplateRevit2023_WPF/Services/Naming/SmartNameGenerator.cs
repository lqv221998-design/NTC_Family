using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NTC.FamilyManager.Core.Models.Naming;

namespace NTC.FamilyManager.Services.Naming
{
    public class SmartNameGenerator : IDisposable
    {
        private NamingConfig _config;
        private List<NamingRule> _rules;
        private const string RulesFileName = "naming_rules.json";
        
        private readonly FileSystemWatcher _watcher;
        private readonly object _lock = new object();
        private Timer _reloadTimer;

        public SmartNameGenerator()
        {
            LoadRules();
            
            // Setup File Watcher for Hot Reload
            try
            {
                string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string resourcesPath = Path.Combine(assemblyPath, "Resources");
                
                if (Directory.Exists(resourcesPath))
                {
                    _watcher = new FileSystemWatcher(resourcesPath, RulesFileName);
                    _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
                    _watcher.Changed += OnRulesChanged;
                    _watcher.Created += OnRulesChanged;
                    _watcher.Renamed += OnRulesChanged;
                    _watcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Could not setup hot reload: {ex.Message}");
            }
        }

        private void OnRulesChanged(object sender, FileSystemEventArgs e)
        {
            try 
            {
                // Debounce: Wait 500ms after last change event to avoid reading while file is locked/writing
                if (_reloadTimer == null)
                {
                    _reloadTimer = new Timer(_ => LoadRules(), null, 500, Timeout.Infinite);
                }
                else
                {
                    _reloadTimer.Change(500, Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling rule change: {ex.Message}");
            }
        }

        private void LoadRules()
        {
            lock (_lock)
            {
                try
                {
                    // Tìm file trong thư mục output (bin/Debug/Resources/...)
                    string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string jsonPath = Path.Combine(assemblyPath, "Resources", RulesFileName);

                    if (File.Exists(jsonPath))
                    {
                        // Use FileShare.ReadWrite to allow reading even if requested by editor
                        using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            _config = JsonConvert.DeserializeObject<NamingConfig>(json);
                            _rules = _config?.Rules ?? new List<NamingRule>();
                        }
                        System.Diagnostics.Debug.WriteLine($"Naming rules reloaded! Total rules: {_rules.Count}");
                    }
                    else
                    {
                        _config = new NamingConfig { DefaultAuthor = "BimTeam", VersionPrefix = "2023" };
                        _rules = new List<NamingRule>();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading rules: {ex.Message}");
                    // Keep old rules if reload fails
                    if (_rules == null) _rules = new List<NamingRule>();
                }
            }
        }

        public (string ProposedName, string Category, string Discipline, string Description) SuggestName(string originalPath)
        {
            lock (_lock)
            {
                if (_rules == null || !_rules.Any()) return (null, null, null, null);

                string fileName = Path.GetFileNameWithoutExtension(originalPath);
                string normalizedName = NormalizeString(fileName);

                // Tìm rule khớp nhất
                // Ưu tiên 1: Priority cao nhất
                // Ưu tiên 2: Khớp nhiều từ khóa nhất (hoặc từ khóa dài nhất)
                var matchedRule = _rules
                    .Where(r => r.Keywords.Any(k => normalizedName.Contains(NormalizeString(k))))
                    .OrderByDescending(r => r.Priority)
                    .ThenByDescending(r => r.Keywords.Max(k => k.Length))
                    .FirstOrDefault();

                if (matchedRule != null)
                {
                    // Format chuẩn: NTC_<Bộ môn>_<Hạng mục>_<Mô tả>
                    string newName = $"NTC_{matchedRule.Discipline}_{matchedRule.Category.Replace(" ", "")}_{matchedRule.Description.Replace(" ", "")}";

                    return (newName, matchedRule.Category, matchedRule.Discipline, matchedRule.Description);
                }

                return (null, null, null, null);
            }
        }

        private string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string normalized = RemoveAccents(input.ToLower().Trim());
            return normalized;
        }

        private string RemoveAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Standard Vietnamese accent removal
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                {
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
                }
            }
            return text;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _reloadTimer?.Dispose();
        }
    }
}

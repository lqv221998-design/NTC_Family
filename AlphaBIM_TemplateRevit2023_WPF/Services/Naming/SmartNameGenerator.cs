using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public (string ProposedName, string Category, string Discipline, string Description) SuggestName(string originalPath, string category = null, string version = null)
        {
            lock (_lock)
            {
                string fileName = Path.GetFileNameWithoutExtension(originalPath);
                string normalizedOriginalName = fileName.Replace(" ", "_").Replace("-", "_").ToLower();
                
                // 1. Tìm rule khớp nhất để lấy Discipline/Description nếu cần (vẫn giữ logic thông minh)
                NamingRule matchedRule = null;
                if (_rules != null && _rules.Any())
                {
                    string searchName = NormalizeString(fileName);
                    matchedRule = _rules
                        .Where(r => r.Keywords.Any(k => searchName.Contains(NormalizeString(k))))
                        .OrderByDescending(r => r.Priority)
                        .ThenByDescending(r => r.Keywords.Max(k => k.Length))
                        .FirstOrDefault();
                }

                // 2. Metadata Context
                string finalCat = category ?? matchedRule?.Category ?? "Generic Models";
                string finalDisc = matchedRule?.Discipline ?? "GEN";
                string finalDesc = matchedRule?.Description ?? "Auto-generated";
                string finalYear = version ?? _config?.VersionPrefix ?? "2023";

                // 3. Format V4.2 Standard: NTC_[Families]_[originalNameLowercase]_v[year]
                // Families = Category (tên rút gọn, bỏ dấu cách)
                string sanitizedCat = SanitizeCategory(finalCat);
                string catShort = sanitizedCat.Replace(" ", "").ToLower();
                string proposedName = $"NTC_{catShort}_{normalizedOriginalName}_v{finalYear}";

                return (proposedName, sanitizedCat, finalDisc, finalDesc);
            }
        }

        private string SanitizeCategory(string rawCategory)
        {
            if (string.IsNullOrEmpty(rawCategory)) return "Generic Models";

            // 1. Loại bỏ mã OmniClass (vđ: 23.25.30.11.14...)
            string sanitized = Regex.Replace(rawCategory, @"\d{2}(\.\d{2})+", "").Trim();

            // 2. Loại bỏ gạch chân/mã hiệu lạ ở cuối nếu có dính "std:oc1"
            sanitized = Regex.Replace(sanitized, @"std:oc\d+", "").Trim();

            // 3. Loại bỏ Revit Internal Tags (adsk:revit:...)
            sanitized = Regex.Replace(sanitized, @"adsk:revit:[a-zA-Z]+", "").Trim();

            // 4. Nếu bị dính liền (vđ: "Generic Modelsadsk..."), lấy phần trước adsk
            int internalIdx = sanitized.IndexOf("adsk:", StringComparison.OrdinalIgnoreCase);
            if (internalIdx > 0)
            {
                sanitized = sanitized.Substring(0, internalIdx).Trim();
            }

            // 5. Nếu sau khi lọc mà rỗng hoặc quá ngắn, trả về mặc định
            if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length < 2)
                return "Generic Models";

            return sanitized;
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

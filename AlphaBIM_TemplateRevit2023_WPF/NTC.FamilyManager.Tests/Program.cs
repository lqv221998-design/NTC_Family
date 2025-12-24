using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NTC.FamilyManager.Services.Naming;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace NTC.FamilyManager.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== TESTING SMART NAME GENERATOR ===");

            // Ensure resources directory exists for test
            string execPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string resourceDir = Path.Combine(execPath, "Resources");
            if (!Directory.Exists(resourceDir)) Directory.CreateDirectory(resourceDir);
            
            // Copy rule file to bin/Debug/.../Resources
            string sourceJson = "naming_rules.json";
            string destJson = Path.Combine(resourceDir, "naming_rules.json");
            File.Copy(sourceJson, destJson, true);

            using (var generator = new SmartNameGenerator())
            {
                // Test 1: Basic Matching
                TestName(generator, "Gian giao khung H 1.7m.rfa");
                TestName(generator, "AHU_Floor_01.rfa");
                TestName(generator, "Window_Sliding 2000x1200.rfa");
                TestName(generator, "May_Lanh_Cassette.rfa"); // Not in original, wait... Keywords check

                // Test 2: Hot Reload
                Console.WriteLine("\n=== TESTING HOT RELOAD (Wait 2s) ===");
                
                string newRule = @"{
                  ""default_author"": ""TestUser"",
                  ""version_prefix"": ""2025"",
                  ""rules"": [
                    {
                      ""keywords"": [ ""cassette"", ""may lanh"" ],
                      ""category"": ""MechEquip"",
                      ""description"": ""May_Lanh_Cassette_Moi"",
                      ""discipline"": ""MEP"",
                      ""priority"": 20
                    }
                  ]
                }";
                
                try 
                {
                    File.WriteAllText(destJson, newRule);
                    Console.WriteLine("Updated naming_rules.json with new rules.");
                }
                catch (Exception ex) 
                {
                     Console.WriteLine($"Error writing file: {ex.Message}");
                }

                await Task.Delay(2000); // Wait for debounce and reload

                TestName(generator, "May_Lanh_Cassette.rfa");
            }
        }

        static void TestName(SmartNameGenerator gen, string input)
        {
            var result = gen.SuggestName(input);
            if (result.ProposedName != null)
            {
                 Console.WriteLine($"[PASS] '{input}' -> '{result.ProposedName}'");
                 Console.WriteLine($"       Cat: {result.Category}, Disc: {result.Discipline}");
            }
            else
            {
                 Console.WriteLine($"[FAIL] '{input}' -> No match found.");
            }
        }
    }
}

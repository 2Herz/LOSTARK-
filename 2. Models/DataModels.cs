using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LostArkCalculator.Models
{
    public class ItemData
    {
        public double TopQty { get; set; }
        public double BottomQty { get; set; }
        [JsonIgnore] public double CalculatedTopValue { get; set; }
        [JsonIgnore] public double CalculatedBotValue { get; set; }
    }

    public class Character
    {
        public Dictionary<string, ItemData> Materials { get; set; } = new Dictionary<string, ItemData>();
        public Character()
        {
            string[] keys = { "Shard", "Dest", "Guard", "Leap", "Fusion", "Yong", "Bing" };
            foreach (var k in keys) Materials[k] = new ItemData();
        }
    }

    public class PriceData
    {
        public double Current { get; set; }
        public double Previous { get; set; }
        public double Diff => Current - Previous;
    }

    public class UserData
    {
        public Dictionary<string, Character> Characters { get; set; } = new Dictionary<string, Character>();
        public SortedDictionary<string, double> History { get; set; } = new SortedDictionary<string, double>();
    }

    public class CharacterInfo
    {
        public string CharacterName { get; set; }
        public string CharacterClassName { get; set; }
        public object ItemMaxLevel { get; set; }
        public object ItemAvgLevel { get; set; }

        [JsonIgnore]
        public double NumericItemLevel
        {
            get
            {
                string levelStr = ItemMaxLevel?.ToString() ?? ItemAvgLevel?.ToString();
                if (string.IsNullOrEmpty(levelStr)) return 0;
                string cleanStr = levelStr.Replace(",", "");
                if (double.TryParse(cleanStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                    return result;
                return 0;
            }
        }
    }

    public class ServerConfig
    {
        public bool IsMaintenance { get; set; }
        public string MaintenanceMessage { get; set; }
        public string LatestVersion { get; set; }
    }
}
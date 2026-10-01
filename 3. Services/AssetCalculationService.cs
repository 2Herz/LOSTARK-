using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LostArkCalculator.Core;
using LostArkCalculator.Models;

namespace LostArkCalculator.Services
{
    public class AssetCalculationService : IAssetCalculator
    {
        private readonly IDataRepository _db;
        public event Action OnAssetCalculated;

        public Dictionary<string, PriceData> TopPrices { get; } = new Dictionary<string, PriceData>();
        public Dictionary<string, PriceData> BotPrices { get; } = new Dictionary<string, PriceData>();

        public AssetCalculationService(IDataRepository db)
        {
            _db = db;
        }

        public async Task FetchMarketPricesAsync(IMarketApi api)
        {
            var topList = new[] {
                new { Name = "운명의 파편 주머니(대)", Col = 1, Shard = true, Bundle = false },
                new { Name = "운명의 파괴석 결정", Col = 2, Shard = false, Bundle = true },
                new { Name = "운명의 수호석 결정", Col = 3, Shard = false, Bundle = true },
                new { Name = "위대한 운명의 돌파석", Col = 4, Shard = false, Bundle = false },
                new { Name = "상급 아비도스 융화 재료", Col = 5, Shard = false, Bundle = false },
                new { Name = "용암의 숨결", Col = 6, Shard = false, Bundle = false },
                new { Name = "빙하의 숨결", Col = 7, Shard = false, Bundle = false }
            };

            foreach (var i in topList)
                UpdatePriceDict(TopPrices, AppConstants.MatKeys[i.Col], await api.GetPriceAsync(i.Name, i.Shard, i.Bundle));

            UpdatePriceDict(BotPrices, AppConstants.MatKeys[1], TopPrices[AppConstants.MatKeys[1]].Current);
            UpdatePriceDict(BotPrices, AppConstants.MatKeys[6], TopPrices[AppConstants.MatKeys[6]].Current);
            UpdatePriceDict(BotPrices, AppConstants.MatKeys[7], TopPrices[AppConstants.MatKeys[7]].Current);

            var botList = new[] {
                new { Name = "운명의 파괴석", Col = 2, Bundle = true }, 
                new { Name = "운명의 수호석", Col = 3, Bundle = true },
                new { Name = "운명의 돌파석", Col = 4, Bundle = false }, 
                new { Name = "아비도스 융화 재료", Col = 5, Bundle = false }
            };

            foreach (var i in botList)
                UpdatePriceDict(BotPrices, AppConstants.MatKeys[i.Col], await api.GetPriceAsync(i.Name, false, i.Bundle));
        }

        private void UpdatePriceDict(Dictionary<string, PriceData> dict, string key, double newPrice)
        {
            if (!dict.ContainsKey(key)) dict[key] = new PriceData();
            dict[key].Previous = dict[key].Current == 0 ? newPrice : dict[key].Current;
            dict[key].Current = newPrice;
        }

        public void CalculateCharacterAssets(string charName)
        {
            if (!_db.Data.Characters.ContainsKey(charName)) return;
            var charData = _db.Data.Characters[charName];

            foreach (var key in AppConstants.MatKeys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                double pT = TopPrices.ContainsKey(key) ? TopPrices[key].Current : 0;
                double pB = BotPrices.ContainsKey(key) ? BotPrices[key].Current : 0;

                charData.Materials[key].CalculatedTopValue = Math.Floor(charData.Materials[key].TopQty * pT);
                charData.Materials[key].CalculatedBotValue = Math.Floor(charData.Materials[key].BottomQty * pB);
            }
            OnAssetCalculated?.Invoke(); // 계산 완료 방송
        }

        public void CalculateRosterHistory()
        {
            double totalRosterAsset = 0;
            foreach (var charData in _db.Data.Characters.Values)
            {
                foreach (var key in AppConstants.MatKeys)
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    double pT = TopPrices.ContainsKey(key) ? TopPrices[key].Current : 0;
                    double pB = BotPrices.ContainsKey(key) ? BotPrices[key].Current : 0;
                    totalRosterAsset += Math.Floor(charData.Materials[key].TopQty * pT) + Math.Floor(charData.Materials[key].BottomQty * pB);
                }
            }
            _db.Data.History[DateTime.Now.ToString("MM-dd")] = totalRosterAsset;
            _db.Save();
            OnAssetCalculated?.Invoke();
        }
    }
}
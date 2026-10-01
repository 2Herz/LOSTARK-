using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LostArkCalculator.Models;

namespace LostArkCalculator.Core
{
    public interface IDataRepository
    {
        UserData Data { get; }
        void Save();
    }

    public interface IMarketApi
    {
        Task<double> GetPriceAsync(string itemName, bool isShard, bool isBundle);
        Task<List<CharacterInfo>> GetRosterAsync(string charName);
    }

    public interface IAssetCalculator
    {
        event Action OnAssetCalculated;
        Dictionary<string, PriceData> TopPrices { get; }
        Dictionary<string, PriceData> BotPrices { get; }

        Task FetchMarketPricesAsync(IMarketApi api);
        void CalculateCharacterAssets(string charName);
        void CalculateRosterHistory();
    }
}
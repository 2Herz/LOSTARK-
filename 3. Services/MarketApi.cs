using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LostArkCalculator.Core;
using LostArkCalculator.Models;

namespace LostArkCalculator.Services
{
    public class MarketApi : IMarketApi
    {
        private readonly HttpClient _client = new HttpClient();

        public MarketApi(string apiKey)
        {
            // 🌟 1. 생성자에서 딱 한 번만 인증 헤더를 셋팅하여 충돌 원천 차단!
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("accept", "application/json");
            _client.DefaultRequestHeaders.Add("authorization", "Bearer " + apiKey);
        }

        public async Task<double> GetPriceAsync(string itemName, bool isShard, bool isBundle)
        {
            var payload = new JObject { { "Sort", "CURRENT_MIN_PRICE" }, { "CategoryCode", 50000 }, { "ItemName", itemName }, { "PageNo", 1 }, { "SortCondition", "ASC" } };
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

            // 🌟 2. 요청할 때마다 헤더를 건드리는 위험한 코드 삭제됨
            var res = await _client.PostAsync("https://developer-lostark.game.onstove.com/markets/items", content);

            // 어디서 실패했는지 알기 쉽게 에러 메시지 세분화
            if (!res.IsSuccessStatusCode) throw new Exception("거래소 API 요청 실패 (서버 오류 또는 호출 한도 초과)");

            var json = JObject.Parse(await res.Content.ReadAsStringAsync());
            var items = json["Items"] as JArray;
            if (items == null || items.Count == 0) return 0;

            double p = items[0]["RecentPrice"]?.Value<double>() ?? 0;
            if (isShard) return Math.Round(p / 1500.0, 2);
            if (isBundle) return Math.Round(p / 100.0, 2);
            return p;
        }

        public async Task<List<CharacterInfo>> GetRosterAsync(string charName)
        {
            var res = await _client.GetAsync($"https://developer-lostark.game.onstove.com/characters/{Uri.EscapeDataString(charName)}/siblings");

            if (!res.IsSuccessStatusCode) throw new Exception("원정대 API 요청 실패 (닉네임 확인 요망)");

            return JsonConvert.DeserializeObject<List<CharacterInfo>>(await res.Content.ReadAsStringAsync());
        }
    }
}
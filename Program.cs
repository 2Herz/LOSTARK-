using System;
using System.Net.Http;
using System.Windows.Forms;
using Newtonsoft.Json;
using LostArkCalculator.Core;
using LostArkCalculator.Services;
using LostArkCalculator.Views;
using LostArkCalculator.Models;
using LOSTARK_귀속재료_값어치_계산기.Properties;

namespace LostArkCalculator
{
    static class Program
    {
        // 🌟 수정 1: 긴 암호(해시)를 완전히 지운 '진짜 최신 추적 주소'
        private const string ConfigUrl = "https://gist.githubusercontent.com/2Herz/d075350e67a061d494e47703c717f3d7/raw/AppConfig.json";
        private const string CurrentVersion = "1.0.2";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!CheckServerStatus()) return;

            string apiKey = LOSTARK_귀속재료_값어치_계산기.Properties.Settings.Default.UserApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                using (var keyForm = new ApiKeyForm())
                {
                    if (keyForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }
                apiKey = LOSTARK_귀속재료_값어치_계산기.Properties.Settings.Default.UserApiKey;
            }

            IDataRepository db = new JsonDataRepository();
            IMarketApi api = new MarketApi(apiKey);
            IAssetCalculator calc = new AssetCalculationService(db);

            Application.Run(new Form1(db, api, calc));
        }

        private static bool CheckServerStatus()
        {
            // 🌟 수정 2: 무사통과 함정 제거. 주소가 비어있을 때만 통과하도록 수정.
            if (string.IsNullOrEmpty(ConfigUrl)) return true;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = ConfigUrl + "?t=" + DateTime.Now.Ticks;

                    string json = client.GetStringAsync(requestUrl).Result;
                    var config = JsonConvert.DeserializeObject<ServerConfig>(json);

                    if (config.IsMaintenance)
                    {
                        MessageBox.Show(config.MaintenanceMessage, "서버 점검 안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    if (config.LatestVersion != CurrentVersion)
                    {
                        MessageBox.Show($"새로운 버전({config.LatestVersion})이 출시되었습니다!\n최신 버전으로 업데이트를 권장합니다.", "업데이트 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                var result = MessageBox.Show($"서버 상태를 확인할 수 없습니다.\n(상세: {ex.Message})\n\n강제로 실행하시겠습니까?",
                    "통신 오류", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                return result == DialogResult.Yes;
            }
        }
    }
}
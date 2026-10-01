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
        private const string ConfigUrl = "여기에_Gist_Raw_주소를_붙여넣으세요";
        private const string CurrentVersion = "1.0.1";

        // ❌ 기존 비밀 파일(secrets.txt) 경로는 지웠습니다.

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!CheckServerStatus()) return;

            // 🌟 1. 윈도우 설정 공간에서 API 키를 꺼내옴
            string apiKey = LOSTARK_귀속재료_값어치_계산기.Properties.Settings.Default.UserApiKey;

            // 🌟 2. 키가 비어있다면? (처음 실행하는 사용자)
            if (string.IsNullOrEmpty(apiKey))
            {
                // API 키 입력 전용 창을 띄움 (.ShowDialog()로 창이 닫힐 때까지 대기)
                using (var keyForm = new ApiKeyForm())
                {
                    if (keyForm.ShowDialog() != DialogResult.OK)
                    {
                        // 사용자가 창의 X 버튼을 눌러서 강제로 닫아버리면 프로그램 종료
                        return;
                    }
                }

                // 정상적으로 입력하고 저장 버튼을 눌렀다면 다시 키를 꺼내옴
                apiKey = LOSTARK_귀속재료_값어치_계산기.Properties.Settings.Default.UserApiKey;
            }

            // 🌟 3. 완벽하게 주입 후 본 게임(Form1) 시작!
            IDataRepository db = new JsonDataRepository();
            IMarketApi api = new MarketApi(apiKey);
            IAssetCalculator calc = new AssetCalculationService(db);

            Application.Run(new Form1(db, api, calc));
        }

        // =====================================
        // [유틸리티] 서버 상태 확인 로직 (기존과 동일)
        // =====================================
        private static bool CheckServerStatus()
        {
            if (ConfigUrl == "여기에_Gist_Raw_주소를_붙여넣으세요" || string.IsNullOrEmpty(ConfigUrl)) return true;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = client.GetStringAsync(ConfigUrl).Result;
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
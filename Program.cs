using System;
using System.Net.Http;
using System.Windows.Forms;
using Newtonsoft.Json;
using LostArkCalculator.Core;
using LostArkCalculator.Services;
using LostArkCalculator.Views;
using LostArkCalculator.Models;

namespace LostArkCalculator
{
    static class Program
    {
        // 🌟 아까 복사한 GitHub Gist의 'Raw' 주소를 여기에 넣습니다.
        private const string ConfigUrl = "YOUR_RAW_URL_HERE";

        // 내 프로그램의 현재 버전 (배포할 때마다 숫자를 올립니다)
        private const string CurrentVersion = "1.0.0";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 🌟 1. 폼(UI)을 띄우기 전에 서버 상태부터 체크 (검문소)
            if (!CheckServerStatus())
            {
                return; // 점검 중이거나 치명적 오류 시 여기서 프로그램 즉시 종료
            }

            // 🌟 2. 검문소를 통과했다면 정상적으로 의존성 조립 및 부팅
            IDataRepository db = new JsonDataRepository();
            IMarketApi api = new MarketApi(AppConstants.ApiKey);
            IAssetCalculator calc = new AssetCalculationService(db);

            Application.Run(new Form1(db, api, calc));
        }

        // 🌟 서버 상태를 확인하는 신호등 로직
        private static bool CheckServerStatus()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 깃허브에서 JSON 텍스트를 읽어옴 (.Result를 써서 동기적으로 기다림)
                    string json = client.GetStringAsync(ConfigUrl).Result;
                    var config = JsonConvert.DeserializeObject<ServerConfig>(json);

                    // 🛑 점검 모드 스위치가 켜져(true) 있다면?
                    if (config.IsMaintenance)
                    {
                        MessageBox.Show(config.MaintenanceMessage, "서버 점검 안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false; // 실행 차단
                    }

                    // 🔄 버전이 다르다면? (선택 사항: 강제 종료하거나 안내만 하거나)
                    if (config.LatestVersion != CurrentVersion)
                    {
                        MessageBox.Show($"새로운 버전({config.LatestVersion})이 출시되었습니다!\n최신 버전으로 업데이트를 권장합니다.", "업데이트 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // return false; // 버전을 다르면 강제 종료시키고 싶다면 주석 해제
                    }

                    return true; // 무사 통과
                }
            }
            catch (Exception ex)
            {
                // 인터넷이 끊겼거나 깃허브 접속이 안 될 때의 안전장치
                var result = MessageBox.Show($"서버 상태를 확인할 수 없습니다.\n인터넷 연결을 확인해 주세요.\n(상세: {ex.Message})\n\n그래도 강제로 실행하시겠습니까?",
                    "통신 오류", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                return result == DialogResult.Yes;
            }
        }
    }
}
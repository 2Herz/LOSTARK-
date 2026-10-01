using System;
using System.IO;
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
        // 🌟 1. 원격 제어(GitHub Gist) Raw URL 입력 (아까 만드신 Gist 주소)
        private const string ConfigUrl = "여기에_Gist_Raw_주소를_붙여넣으세요";

        // 🌟 2. 현재 배포 버전
        private const string CurrentVersion = "1.0.0";

        // 🌟 3. 로컬 비밀 키 파일 경로 (깃허브에 절대 올라가지 않는 파일)
        private const string SecretFilePath = "secrets.txt";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // [검문소 1단계] 서버 상태 체크 (점검 중이면 강제 종료)
            if (!CheckServerStatus()) return;

            // [검문소 2단계] 로컬 비밀 키(secrets.txt) 읽기
            string apiKey = LoadLocalSecretKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("비밀 키 파일(secrets.txt)을 찾을 수 없습니다.\n프로젝트 폴더(exe 파일이 있는 곳)에 secrets.txt를 만들고 API 키를 넣어주세요.",
                                "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // [최종 실행] 완벽하게 조립된 의존성을 폼에 주입하며 프로그램 실행! (DI 적용)
            IDataRepository db = new JsonDataRepository();
            IMarketApi api = new MarketApi(apiKey); // AppConstants.ApiKey 대신 로컬 파일에서 읽은 키를 안전하게 주입
            IAssetCalculator calc = new AssetCalculationService(db);

            Application.Run(new Form1(db, api, calc));
        }

        // =====================================
        // [유틸리티] 서버 상태 확인 로직
        // =====================================
        private static bool CheckServerStatus()
        {
            // 아직 Gist 주소를 넣지 않았을 때를 대비한 방어 코드
            if (ConfigUrl == "여기에_Gist_Raw_주소를_붙여넣으세요" || string.IsNullOrEmpty(ConfigUrl)) return true;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 깃허브에서 JSON 텍스트를 읽어옴
                    string json = client.GetStringAsync(ConfigUrl).Result;
                    var config = JsonConvert.DeserializeObject<ServerConfig>(json);

                    // 🛑 점검 모드 스위치가 켜져(true) 있다면?
                    if (config.IsMaintenance)
                    {
                        MessageBox.Show(config.MaintenanceMessage, "서버 점검 안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false; // 프로그램 실행 차단
                    }

                    // 🔄 버전이 다르다면? 안내창 띄우기
                    if (config.LatestVersion != CurrentVersion)
                    {
                        MessageBox.Show($"새로운 버전({config.LatestVersion})이 출시되었습니다!\n최신 버전으로 업데이트를 권장합니다.", "업데이트 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        // =====================================
        // [유틸리티] 로컬 비밀 키 읽기 로직
        // =====================================
        private static string LoadLocalSecretKey()
        {
            // 컴파일된 빌드 폴더(bin/Debug 등)에 secrets.txt가 있어야 읽어옵니다.
            if (File.Exists(SecretFilePath))
            {
                return File.ReadAllText(SecretFilePath).Trim();
            }
            return null;
        }
    }
}
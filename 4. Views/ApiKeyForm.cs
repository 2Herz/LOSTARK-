using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using LOSTARK_귀속재료_값어치_계산기.Properties;

namespace LostArkCalculator.Views
{
    // Form을 상속받아 코드로만 UI를 그리는 클래스
    public class ApiKeyForm : Form
    {
        private TextBox txtApiKey;
        private Button btnSave;
        private LinkLabel linkLabel;

        public ApiKeyForm()
        {
            // 1. 기본 창 크기 및 설정
            this.Text = "초기 설정 - API 키 입력";
            this.Size = new Size(520, 200);
            this.StartPosition = FormStartPosition.CenterScreen; // 화면 정중앙에 띄우기
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 2. 안내 텍스트
            Label lblInfo = new Label()
            {
                Text = "원활한 시세 동기화를 위해 스토브 개발자 센터의 API 키를 입력해 주세요.",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };

            // 3. 발급처 링크 (클릭 시 인터넷 창 열림)
            linkLabel = new LinkLabel()
            {
                Text = "▶ API 키 발급받는 곳 (클릭)",
                Location = new Point(20, 45),
                AutoSize = true
            };
            linkLabel.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo("https://developer-lostark.game.onstove.com/") { UseShellExecute = true });

            // 4. 키 입력 칸
            txtApiKey = new TextBox()
            {
                Location = new Point(20, 75),
                Width = 460
            };

            // 5. 저장 버튼
            btnSave = new Button()
            {
                Text = "저장 및 시작",
                Location = new Point(380, 110),
                Width = 100,
                Height = 30
            };
            btnSave.Click += BtnSave_Click;

            // 창에 부품들 조립
            this.Controls.Add(lblInfo);
            this.Controls.Add(linkLabel);
            this.Controls.Add(txtApiKey);
            this.Controls.Add(btnSave);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                MessageBox.Show("API 키를 입력해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🌟 핵심: 방금 1단계에서 만든 공간에 사용자가 친 키를 영구 저장!
            LOSTARK_귀속재료_값어치_계산기.Properties.Settings.Default.UserApiKey = txtApiKey.Text.Trim();
            LOSTARK_귀속재료_값어치_계산기.Properties.Settings.Default.Save();

            this.DialogResult = DialogResult.OK; // 정상 통과 신호
            this.Close();
        }
    }
}
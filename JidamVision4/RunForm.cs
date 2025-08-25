using JidamVision4.Core;
using JidamVision4.Setting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace JidamVision4
{
    public partial class RunForm: DockContent
    {
        public RunForm()
        {
            InitializeComponent();
            this.KeyPreview = true;      // 폼이 키를 먼저 받도록
            this.AcceptButton = btnStart;   // Enter == btnStart 클릭
        }
        public void StartFromHotkey()
        {
            if (!btnStart.Enabled) return;  // 연속 중 재시작 막기
            btnStart.PerformClick();        // 기존 버튼 경로(비동기) 그대로 사용
        }

        // 폼 레벨에서 Enter를 '검사'로 강제 라우팅
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Enter)
            {
                // 텍스트 입력 중(멀티라인 등) 예외가 필요하면 여기서 걸러주세요
                StartFromHotkey();
                return true;               // ← 기본/포커스 버튼 클릭을 차단
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool _hotkeyBusy = false;

        public async Task StartCurrentInspectAsync()
        {
            if (_hotkeyBusy) return;
            _hotkeyBusy = true;
            try
            {
                // 연속검사 중엔 엔터 무시 (원하면 허용 로직으로 바꿔도 됨)
                if (Global.Inst.InspStage.InspWorker?.IsRunning == true)
                    return;

                // 현재 프레임만 검사 (그랩 금지)
                await Task.Run(() =>
                {
                    Global.Inst.InspStage.InspectCurrentOnly();
                });
            }
            finally { _hotkeyBusy = false; }
        }

        private void btnGrab_Click(object sender, EventArgs e)
        {
            //#13_SET_IMAGE_BUFFER#3 그랩시 이미지 버퍼를 먼저 설정하도록 변경
            Global.Inst.InspStage.CheckImageBuffer();
            Global.Inst.InspStage.Grab(0);
        }

        //#8_INSPECT_BINARY#20 검사 시작 버튼을 디자인창에서 만들고, 검사 함수 호출
        private async void btnStart_Click(object sender, EventArgs e)
        {

            string serialID = $"{DateTime.Now:MM-dd HH:mm:ss}";
            Global.Inst.InspStage.InspectReady("LOT_NUMBER", serialID);

            // 시작 누르면 일단 Start 비활성화
            btnStart.Enabled = false;

            if (SettingXml.Inst.CamType == Grab.CameraType.None)
            {
                bool cycleMode = SettingXml.Inst.CycleMode;

                if (cycleMode)
                {
                    // ✅ 연속 검사: Stop 활성화 유지, Start 비활성화 유지
                    btnStop.Enabled = true;
                    Global.Inst.InspStage.CycleInspect(true);   // 내부 워커가 루프 시작하고 바로 리턴
                }
                else
                {
                    // ✅ 단발 검사: Stop은 항상 비활성
                    btnStop.Enabled = false;

                    // 단발은 UI 멈춤 방지 차원에서 백그라운드로 1회 실행
                    await Task.Run(() => Global.Inst.InspStage.CycleInspect(false));

                    // 끝나면 Start만 다시 활성화
                    btnStart.Enabled = true;
                }
            }
            else
            {
                // 카메라 자동 런은 연속 검사 취급
                btnStop.Enabled = true;          // ✅ 연속은 Stop 활성
                Global.Inst.InspStage.StartAutoRun(); // 내부 시퀀스 시작 후 바로 리턴(비동기 루프)
                                                      // Start는 연속 동안 비활성 유지
            }
        }
        


        private void btnStop_Click(object sender, EventArgs e)
        {
            Global.Inst.InspStage.StopCycle();   // 현재 이미지 끝나면 루프 종료

            // ✅ 정지 누르면 Stop 비활성, Start 활성
            btnStop.Enabled = false;
            btnStart.Enabled = true;
        }

        //#8_LIVE#3 라이브 모드 버튼 추가
        private void btnLive_Click(object sender, EventArgs e)
        {
            Global.Inst.InspStage.LiveMode = !Global.Inst.InspStage.LiveMode;

            //#17_WORKING_STATE#6 LIVE 상태 화면 표시
            if (Global.Inst.InspStage.LiveMode)
            {
                Global.Inst.InspStage.SetWorkingState(WorkingState.LIVE);

                //#13_SET_IMAGE_BUFFER#4 그랩시 이미지 버퍼를 먼저 설정하도록 변경
                Global.Inst.InspStage.CheckImageBuffer();
                Global.Inst.InspStage.Grab(0);
            }
            else
            {
                Global.Inst.InspStage.SetWorkingState(WorkingState.NONE);
            }
        }

    }
}

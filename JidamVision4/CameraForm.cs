using JidamVision4.Algorithm;
using JidamVision4.Core;
using JidamVision4.Teach;
using JidamVision4.UIControl;
using JidamVision4.Util;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace JidamVision4
{
    //#2_DOCKPANEL#3 CameraForm 클래스 는 도킹 가능하도록 상속을 변경
    //public partial class CameraForm: Form
    public partial class CameraForm : DockContent
    {

        // === 커서 정보 바 ===
        private Panel _cursorBar;
        private Label _lblXY;
        private Label _lblPix;

        //보드 길이 측정 #16
        public JidamVision4.UIControl.ImageViewCtrl GetImageView()
        {
            return imageViewer; // ← 실제 컨트롤명으로 변경
        }

        //#18_IMAGE_CHANNEL#3 현재 선택된 이미지 채널을 저장하는 변수
        //_currentImageChannel 변수 모두 찾아서, 관련 코드 수정할것
        eImageChannel _currentImageChannel = eImageChannel.Gray;


        public void SetWindowVisible(InspWindow win, bool visible)
        {
            imageViewer?.SetWindowVisible(win, visible);
        }

        public CameraForm()
        {
            InitializeComponent();
            this.Resize += CameraForm_Resize;

            _cursorBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.FromArgb(245, 245, 245) // 밝은 회색
            };
            _lblXY = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Left,
                Width = 180,
                Text = "X: -, Y: -",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            _lblPix = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Gray/RGB: -",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            _cursorBar.Controls.Add(_lblPix);
            _cursorBar.Controls.Add(_lblXY);
            Controls.Add(_cursorBar);

            // ===== ImageViewCtrl 이벤트 구독 =====
            imageViewer.MouseImageMoved += ImageViewer_MouseImageMoved;
            imageViewer.MouseImageLeaved += ImageViewer_MouseImageLeaved;



            this.FormClosed += CameraForm_FormClosed;

            //#10_INSPWINDOW#23 ImageViewCtrl에서 발생하는 이벤트 처리
            imageViewer.DiagramEntityEvent += ImageViewer_DiagramEntityEvent;

            //#18_IMAGE_CHANNEL#1 메인툴바 이벤트 처리
            mainViewToolbar.ButtonChanged += Toolbar_ButtonChanged;
        }

        private void ImageViewer_DiagramEntityEvent(object sender, DiagramEntityEventArgs e)
        {
            SLogger.Write($"ImageViewer Action {e.ActionType.ToString()}");
            switch (e.ActionType)
            {
                case EntityActionType.Select:
                    Global.Inst.InspStage.SelectInspWindow(e.InspWindow);
                    imageViewer.Focus();
                    break;
                case EntityActionType.Inspect:
                    UpdateDiagramEntity();
                    imageViewer.ClearNudgePreview();  // ★ 프리뷰 정리
                    Global.Inst.InspStage.TryInspection(e.InspWindow);
                    break;
                case EntityActionType.Add:
                    Global.Inst.InspStage.AddInspWindow(e.WindowType, e.Rect);
                    break;
                case EntityActionType.Copy:
                    Global.Inst.InspStage.AddInspWindow(e.InspWindow, e.OffsetMove);
                    break;
                case EntityActionType.Move:
                    Global.Inst.InspStage.MoveInspWindow(e.InspWindow, e.OffsetMove);
                    // ★ 풀 리빌드 대신 미리보기만 그리기
                    imageViewer.SetNudgePreview(e.Rect);
                    // UpdateDiagramEntity();  // ← 이 줄은 지우거나 주석 (전체 오버레이 리셋됨)
                    break;

                case EntityActionType.Resize:
                    Global.Inst.InspStage.ModifyInspWindow(e.InspWindow, e.Rect);
                    // ★ 리사이즈 미리보기
                    imageViewer.SetNudgePreview(e.Rect);
                    // UpdateDiagramEntity();  // ← 주석
                    break;
                case EntityActionType.Delete:
                    Global.Inst.InspStage.DelInspWindow(e.InspWindow);
                    break;
                case EntityActionType.DeleteList:
                    Global.Inst.InspStage.DelInspWindow(e.InspWindowList);
                    break;
            }
        }

        //#3_CAMERAVIEW_PROPERTY#1 이미지 경로를 받아 PictureBox에 이미지를 로드하는 메서드
        public void LoadImage(string filePath)
        {
            if (File.Exists(filePath) == false)
                return;

            //#4_IMAGE_VIEWER#6 이미지 뷰어 컨트롤을 사용하여 이미지를 로드
            //picMainview.Image = Image.FromFile(filePath);
            Image bitmap = Image.FromFile(filePath);
            imageViewer.LoadBitmap((Bitmap)bitmap);
        }

        public Mat GetDisplayImage()
        {
            return Global.Inst.InspStage.ImageSpace.GetMat(0, _currentImageChannel);
        }

        private void CameraForm_Resize(object sender, EventArgs e)
        {
            //#18_IMAGE_CHANNEL#4 메인툴바 너비를 제외하고 이미지 뷰어의 크기를 조정

            //int margin = 0;
            //imageViewer.Width = this.Width - mainViewToolbar.Width - margin * 2;
            //imageViewer.Height = this.Height - margin * 2;
            //imageViewer.Location = new System.Drawing.Point(margin, margin);

            int margin = 0;
            int barH = (_cursorBar != null) ? _cursorBar.Height : 0;

            imageViewer.Width = this.Width - mainViewToolbar.Width - margin * 2;
            imageViewer.Height = this.Height - margin * 2 - barH; // ← barH 사용
            imageViewer.Location = new System.Drawing.Point(margin, margin);
        }

        private void SetCursorInfo(string xy, string pix)
        {
            if (_lblXY != null) _lblXY.Text = xy;
            if (_lblPix != null) _lblPix.Text = pix;
        }

        private void ImageViewer_MouseImageMoved(System.Drawing.Point? imgPt)
        {
            if (imgPt == null)
            {
                SetCursorInfo("X: -, Y: -", "Gray/RGB: -");
                return;
            }

            var p = imgPt.Value;

            var bmp = Global.Inst.InspStage.GetBitmap(
                          Global.Inst.InspStage.SelBufferIndex, eImageChannel.None);
            if (bmp == null || p.X < 0 || p.Y < 0 || p.X >= bmp.Width || p.Y >= bmp.Height)
            {
                SetCursorInfo("X: -, Y: -", "Gray/RGB: -");
                return;
            }

            string xy = $"X: {p.X}, Y: {p.Y}";

            var pix = bmp.GetPixel(p.X, p.Y);

            // ★ 현재 선택 채널 기준으로 표기(정확)
            string pv;
            if (_currentImageChannel == eImageChannel.Gray)
            {
                // 그레이 채널이면 R=G=B 형태일 가능성 높음 → 하나만 표시
                pv = $"Gray: {pix.R}";
            }
            else if (_currentImageChannel == eImageChannel.Red)
            {
                pv = $"R: {pix.R}";
            }
            else if (_currentImageChannel == eImageChannel.Green)
            {
                pv = $"G: {pix.G}";
            }
            else if (_currentImageChannel == eImageChannel.Blue)
            {
                pv = $"B: {pix.B}";
            }
            else // Color
            {
                pv = $"R: {pix.R}, G: {pix.G}, B: {pix.B}";
            }

            SetCursorInfo(xy, pv);
        }

        private void ImageViewer_MouseImageLeaved()
        {
            SetCursorInfo("X: -, Y: -", "Gray/RGB: -");
        }



        public void UpdateDisplay(Bitmap bitmap = null)
        {
            if (bitmap == null)
            {
                //#6_INSP_STAGE#3 업데이트시 bitmap이 없다면 InspSpace에서 가져온다
                bitmap = Global.Inst.InspStage.GetBitmap(0, _currentImageChannel);
                if (bitmap == null)
                    return;
            }

            if (imageViewer != null)
                imageViewer.LoadBitmap(bitmap);
        }

        public void UpdateImageViewer()
        {
            imageViewer.UpdateInspParam();
            imageViewer.Invalidate();
        }

        //#10_INSPWINDOW#23 모델 정보를 이용해, ROI 갱신
        public void UpdateDiagramEntity()
        {
            imageViewer.ResetEntity();

            Model model = Global.Inst.InspStage.CurModel;
            List<DiagramEntity> diagramEntityList = new List<DiagramEntity>();

            foreach (InspWindow window in model.InspWindowList)
            {
                if (window is null)
                    continue;

                DiagramEntity entity = new DiagramEntity()
                {
                    LinkedWindow = window,
                    EntityROI = new Rectangle(
                        window.WindowArea.X, window.WindowArea.Y,
                            window.WindowArea.Width, window.WindowArea.Height),
                    EntityColor = imageViewer.GetWindowColor(window.InspWindowType),
                    IsHold = window.IsTeach
                };
                diagramEntityList.Add(entity);
            }

            imageViewer.SetDiagramEntityList(diagramEntityList);
        }

        public void SelectDiagramEntity(InspWindow window)
        {
            imageViewer.SelectDiagramEntity(window);
        }

        //#8_INSPECT_BINARY#18 imageViewer에 검사 결과 정보를 연결해주기 위한 함수
        public void ResetDisplay()
        {
            imageViewer.ResetEntity();
        }

        //검사 결과를 그래픽으로 출력하기 위한 정보를 받는 함수
        public void AddRect(List<DrawInspectInfo> rectInfos)
        {
            imageViewer.AddRect(rectInfos);
        }

        //#10_INSPWINDOW#24 새로운 ROI를 추가하는 함수
        public void AddRoi(InspWindowType inspWindowType)
        {
            imageViewer.NewRoi(inspWindowType);
        }

        //#13_INSP_RESULT#6 검사 양불판정 갯수 설정 함수
        public void SetInspResultCount(int totalArea, int okCnt, int ngCnt)
        {
            imageViewer.SetInspResultCount(new InspectResultCount(totalArea, okCnt, ngCnt));
        }

        //#17_WORKING_STATE#5 작업 상태 화면 표시 설정
        public void SetWorkingState(WorkingState workingState)
        {
            string state = "";
            switch (workingState)
            {
                case WorkingState.INSPECT:
                    state = "INSPECT";
                    break;

                case WorkingState.LIVE:
                    state = "LIVE";
                    break;

                case WorkingState.ALARM:
                    state = "ALARM";
                    break;
            }

            imageViewer.WorkingState = state;
            imageViewer.Invalidate();
        }

        //#18_IMAGE_CHANNEL#2 메인툴바의 버튼 이벤트를 처리하는 함수
        private void Toolbar_ButtonChanged(object sender, ToolbarEventArgs e)
        {
            switch (e.Button)
            {
                case ToolbarButton.ShowROI:
                    if (e.IsChecked)
                        UpdateDiagramEntity();
                    else
                        imageViewer.ResetEntity();
                    break;
                case ToolbarButton.ChannelColor:
                    _currentImageChannel = eImageChannel.Color;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelGray:
                    _currentImageChannel = eImageChannel.Gray;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelRed:
                    _currentImageChannel = eImageChannel.Red;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelGreen:
                    _currentImageChannel = eImageChannel.Green;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelBlue:
                    _currentImageChannel = eImageChannel.Blue;
                    UpdateDisplay();
                    break;
            }
        }

        public void SetImageChannel(eImageChannel channel)
        {
            mainViewToolbar.SetSelectButton(channel);
            UpdateDisplay();
        }

        private void CameraForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //mainViewToolbar.ButtonChanged -= Toolbar_ButtonChanged;
            //imageViewer.DiagramEntityEvent -= ImageViewer_DiagramEntityEvent;
            //this.FormClosed -= CameraForm_FormClosed;

            imageViewer.MouseImageMoved -= ImageViewer_MouseImageMoved;
            imageViewer.MouseImageLeaved -= ImageViewer_MouseImageLeaved;

            mainViewToolbar.ButtonChanged -= Toolbar_ButtonChanged;
            imageViewer.DiagramEntityEvent -= ImageViewer_DiagramEntityEvent;
            this.FormClosed -= CameraForm_FormClosed;

            this.Resize -= CameraForm_Resize; // ← 추가(권장)
        }

        public void SwitchToCurrentModel()
        {
            imageViewer.ResetEntity();             // ROI/선택/오버레이 리스트 클리어 일부
            imageViewer.ClearVisibility();         // 숨김(체크 해제) 목록 클리어
            imageViewer.ClearOverlaysAndCounts();  // 검사 오버레이/카운트/상태 텍스트 클리어

            MainForm.GetDockForm<ModelTreeForm>().UpdateDiagramEntity(); // 모델 트리 갱신
            MainForm.GetDockForm<ResultForm>().ClearResults();
        }



    }
}

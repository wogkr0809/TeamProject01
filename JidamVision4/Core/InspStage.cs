using JidamVision4.Algorithm;
using JidamVision4.Grab;
using JidamVision4.Teach;
using JidamVision4.Setting;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using JidamVision4.Inspect;
using JidamVision4.Util;
using System.Windows.Forms;
using Microsoft.Win32;
using JidamVision4.Sequence;
using static JidamVision4.MainForm;
using System.Threading;

namespace JidamVision4.Core
{
    /*
    #6_INSP_STAGE# - <<<비전검사를 위한 클래스 구현>>> 
    InspStage는 비전 검사 시스템의 핵심 클래스로, 
    카메라 인터페이스와 이미지 처리 기능을 통합하여 검사 프로세스를 관리합니다.
    1) ImageSpace 클래스 구현
    2) InspStage 클래스 구현
    3) Global 클래스 구현
    4) RunForm 클래스 구현
    */

    /*
    #16_LAST_MODELOPEN# - <<<마지막에 사용한 모델 파일 자동 로딩>>>     
    */

    /*
    #17_WORKING_STATE# - <<<현재 운영 상태를 화면에 표시>>>     
    */

    //검사와 관련된 클래스를 관리하는 클래스
    public class AccumCounter
    {
        public long Total { get; private set; } 
        public long OK { get; private set; }
        public long NG { get; private set; }

        public void Reset() { Total = OK = NG = 0; }
        public void Add(int total, int ok, int ng)
        { Total += total; OK += ok; NG += ng; }
    }
    
    public class InspStage : IDisposable
    {

        // === [ROI Name] 동일 타입의 "가장 작은 미사용 번호" 생성 ===
        private string GenerateNextName(InspWindowType type)
        {
            string prefix = type.ToString();  // "Chip", "lead", ...
            var used = new HashSet<int>();

            var list = _model?.InspWindowList;
            if (list != null)
            {
                foreach (var w in list)
                {
                    if (w == null || w.InspWindowType != type) continue;

                    string label = !string.IsNullOrWhiteSpace(w.Name) ? w.Name
                                 : !string.IsNullOrWhiteSpace(w.UID) ? w.UID
                                 : null;
                    if (string.IsNullOrWhiteSpace(label)) continue;

                    int n = ExtractNumber(label, prefix);
                    if (n > 0) used.Add(n);
                }
            }

            int candidate = 1;
            while (used.Contains(candidate)) candidate++;
            return $"{prefix}_{candidate:000000}";
        }

        // label에서 번호 추출: "Chip" -> 1, "Chip_0003" -> 3, 매치 실패시 -1
        private static int ExtractNumber(string label, string prefix)
        {
            if (string.IsNullOrWhiteSpace(label)) return -1;
            if (label.Trim().Equals(prefix, StringComparison.OrdinalIgnoreCase)) return 1;

            var m = System.Text.RegularExpressions.Regex.Match(
                label, @"^" + System.Text.RegularExpressions.Regex.Escape(prefix) + @"_(\d+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > 0) return n;
            return -1;
        }

        // 타입별로 1부터 다시 매겨서 '구멍' 제거(삭제 후 재사용 보장)
        private void NormalizeWindowNamesForType(InspWindowType type)
        {
            if (_model?.InspWindowList == null) return;

            string prefix = type.ToString();
            var same = _model.InspWindowList
                .Where(w => w != null && w.InspWindowType == type)
                .Select((w, idx) => new { w, idx, n = ExtractNumber(!string.IsNullOrWhiteSpace(w.Name) ? w.Name : w.UID, prefix) })
                .OrderBy(x => x.n > 0 ? x.n : int.MaxValue)   // 기존 번호 있으면 그 순서 우선
                .ThenBy(x => x.idx)                            // 없으면 등장 순서
                .ToList();

            int i = 1;
            foreach (var x in same)
                x.w.Name = $"{prefix}_{i++:000000}";
        }

        // === [ROI Name] 강제 배정 ===
        private void ForceAssignNameByRule(InspWindow w)
        {
            if (w == null) return;
            w.Name = GenerateNextName(w.InspWindowType);
        }

        public NgCategoryCounter NgCounter { get; } = new NgCategoryCounter();

        public event Action<IReadOnlyDictionary<string, long>> CategoryChanged
        {
            add { NgCounter.Changed += value; }
            remove { NgCounter.Changed -= value; }
        }

        public void ResetCategory() => NgCounter.Reset();

        public AccumCounter Accum { get; } = new AccumCounter();
        public event Action<AccumCounter> AccumChanged;

        public static readonly int MAX_GRAB_BUF = 1;

        private ImageSpace _imageSpace = null;

        //#5_CAMERA_INTERFACE#4 Dispose도 GrabModel에서 상속받아 사용
        //private HikRobotCam _grabManager = null;

        private GrabModel _grabManager = null;
        private CameraType _camType = CameraType.WebCam;

        SaigeAI _saigeAI; // SaigeAI 인스턴스

        //#7_BINARY_PREVIEW#1 이진화 프리뷰에 필요한 변수 선언
        private PreviewImage _previewImage = null;
        
        //#10_INSPWINDOW#8 모델과 선택된 ROI 윈도우 변수 선언
        private Model _model = null;

        private InspWindow _selectedInspWindow = null;

        //#15_INSP_WORKER#5 InspWorker 클래스 선언
        private InspWorker _inspWorker = null;
        private ImageLoader _imageLoader = null;


        //#16_LAST_MODELOPEN#1 가장 최근 모델 파일 경로와 저장할 REGISTRY 키 변수 선언

        // 레지스트리 키 생성 또는 열기
        RegistryKey _regKey = null;

        //가장 최근 모델 파일 경로를 저장하는 변수
        private bool _lastestModelOpen = false;

        public bool UseCamera { get; set; } = false;

        private string _lotNumber;
        private string _serialID;

        public InspStage() { }
        public ImageSpace ImageSpace
        {
            get => _imageSpace;
        }

        public SaigeAI AIModule
        {
            get
            {
                if (_saigeAI is null)
                    _saigeAI = new SaigeAI();
                return _saigeAI;
            }
        }

        public PreviewImage PreView
        {
            get => _previewImage;
        }

        //#15_INSP_WORKER#6 InspWorker 프로퍼티
        public InspWorker InspWorker
        {
            get => _inspWorker;
        }

        //#10_INSPWINDOW#9 현재 모델 프로퍼티 생성
        public Model CurModel
        {
            get => _model;
            set => _model = value; //쓰기도 가능
        }

        //#8_LIVE#1 LIVE 모드 프로퍼티
        public bool LiveMode { get; set; } = false;

        public int SelBufferIndex { get; set; } = 0;
        public eImageChannel SelImageChannel { get; set; } = eImageChannel.Gray;

        public bool Initialize()
        {
            SLogger.Write("InspStage 초기화!");
            _imageSpace = new ImageSpace();

            //#7_BINARY_PREVIEW#3 이진화 알고리즘과 프리뷰 변수 인스턴스 생성
            _previewImage = new PreviewImage();

            //#15_INSP_WORKER#7 InspWorker 인스턴스 생성
            _inspWorker = new InspWorker();
            _imageLoader = new ImageLoader();

            //#16_LAST_MODELOPEN#2 REGISTRY 키 생성
            _regKey = Registry.CurrentUser.CreateSubKey("Software\\JidamVision");

            //#10_INSPWINDOW#10 모델 인스턴스 생성
            _model = new Model();

            LoadSetting();

            switch (_camType)
            {
                //#5_CAMERA_INTERFACE#5 타입에 따른 카메라 인스턴스 생성
                case CameraType.WebCam:
                    {
                        _grabManager = new WebCam();
                        break;
                    }
                case CameraType.HikRobotCam:
                    {
                        _grabManager = new HikRobotCam();
                        break;
                    }
            }

            if (_grabManager != null && _grabManager.InitGrab() == true)
            {
                _grabManager.TransferCompleted += _multiGrab_TransferCompleted;

                InitModelGrab(MAX_GRAB_BUF);
            }

            //#19_VISION_SEQUENCE#3 VisionSequence 초기화
            VisionSequence.Inst.InitSequence();
            VisionSequence.Inst.SeqCommand += SeqCommand;

            //#16_LAST_MODELOPEN#5 마지막 모델 열기 여부 확인
            if (LastestModelOpen())
                UiHelpers.UpdateUiByModelStateSafe();
            else
            {
                UiHelpers.UpdateUiByModelStateSafe(); // ← 없으니 비활성으로 토글
            }

            return true;
        }

        private void LoadSetting()
        {
            //카메라 설정 타입 얻기
            _camType = SettingXml.Inst.CamType;
        }

        public void InitModelGrab(int bufferCount)
        {
            if (_grabManager == null)
                return;

            int pixelBpp = 8;
            _grabManager.GetPixelBpp(out pixelBpp);

            int inspectionWidth;
            int inspectionHeight;
            int inspectionStride;
            _grabManager.GetResolution(out inspectionWidth, out inspectionHeight, out inspectionStride);

            if (_imageSpace != null)
            {
                _imageSpace.SetImageInfo(pixelBpp, inspectionWidth, inspectionHeight, inspectionStride);
            }

            SetBuffer(bufferCount);

            //#18_IMAGE_CHANNEL#7 카메라 칼라 여부에 따라, 기본 채널 설정
            eImageChannel imageChannel = (pixelBpp == 24) ? eImageChannel.Color : eImageChannel.Gray;
            SetImageChannel(imageChannel);
            
            //_grabManager.SetExposureTime(25000);
        }

        /*
        #13_SET_IMAGE_BUFFER# - <<<이미지 크기에 맞는 버퍼 설정>>> 
        이미지 파일을 열거나, 카메라를 사용할 때, 다른 해상도일 때 버퍼 처리
        1) SetImageBuffer(string filePath) 구현
        2) CheckImageBuffer() 구현
        3) #13_SET_IMAGE_BUFFER#1 ~ 4 구현
        */
        public void SetImageBuffer(string filePath)
        {
            SLogger.Write($"Load Image : {filePath}");

            Mat matImage = Cv2.ImRead(filePath);

            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                CurrentImagePath = filePath;
                if (CurModel != null) CurModel.InspectImagePath = filePath;
            }

            int pixelBpp = 8;
            int imageWidth;
            int imageHeight;
            int imageStride;

            if (matImage.Type() == MatType.CV_8UC3)
                pixelBpp = 24;

            imageWidth = (matImage.Width + 3) / 4 * 4;
            imageHeight = matImage.Height;

            // 4바이트 정렬된 새로운 Mat 생성
            Mat alignedMat = new Mat();
            Cv2.CopyMakeBorder(matImage, alignedMat, 0, 0, 0, imageWidth - matImage.Width, BorderTypes.Constant, Scalar.Black);

            imageStride = imageWidth * matImage.ElemSize();

            if (_imageSpace != null)
            {
                if (_imageSpace.ImageSize.Width != imageWidth || _imageSpace.ImageSize.Height != imageHeight)
                {
                    _imageSpace.SetImageInfo(pixelBpp, imageWidth, imageHeight, imageStride);
                    SetBuffer(_imageSpace.BufferCount);
                }
            }

            int bufferIndex = 0;

            // Mat의 데이터를 byte 배열로 복사
            int bufSize = (int)(alignedMat.Total() * alignedMat.ElemSize());
            Marshal.Copy(alignedMat.Data, ImageSpace.GetInspectionBuffer(bufferIndex), 0, bufSize);

            _imageSpace.Split(bufferIndex);

            DisplayGrabImage(bufferIndex);
        }

        public void CheckImageBuffer()
        {
            if (_grabManager != null && SettingXml.Inst.CamType != CameraType.None)
            {
                int imageWidth;
                int imageHeight;
                int imageStride;
                _grabManager.GetResolution(out imageWidth, out imageHeight, out imageStride);

                if (_imageSpace.ImageSize.Width != imageWidth || _imageSpace.ImageSize.Height != imageHeight)
                {
                    int pixelBpp = 8;
                    _grabManager.GetPixelBpp(out pixelBpp);

                    _imageSpace.SetImageInfo(pixelBpp, imageWidth, imageHeight, imageStride);
                    SetBuffer(_imageSpace.BufferCount);
                }
            }
        }


        //#10_INSPWINDOW#11 속성창 업데이트 기준을 알고리즘에서 InspWindow로 변경
        private void UpdateProperty(InspWindow inspWindow)
        {
            if (inspWindow is null)
                return;

            PropertiesForm propertiesForm = MainForm.GetDockForm<PropertiesForm>();
            if (propertiesForm is null)
                return;

            propertiesForm.UpdateProperty(inspWindow);
        }

        //#11_MATCHING#6 패턴매칭 속성창과 연동된 패턴 이미지 관리 함수
        public void UpdateTeachingImage(int index)
        {
            if (_selectedInspWindow is null)
                return;

            SetTeachingImage(_selectedInspWindow, index);
        }

        public void DelTeachingImage(int index)
        {
            if (_selectedInspWindow is null)
                return;

            InspWindow inspWindow = _selectedInspWindow;

            inspWindow.DelWindowImage(index);

            MatchAlgorithm matchAlgo = (MatchAlgorithm)inspWindow.FindInspAlgorithm(InspectType.InspMatch);
            if (matchAlgo != null)
            {
                UpdateProperty(inspWindow);
            }
        }

        public void SetTeachingImage(InspWindow inspWindow, int index = -1)
        {
            if (inspWindow is null)
                return;

            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm is null)
                return;

            Mat curImage = cameraForm.GetDisplayImage();
            if (curImage is null)
                return;

            if (inspWindow.WindowArea.Right >= curImage.Width ||
                inspWindow.WindowArea.Bottom >= curImage.Height)
            {
                SLogger.Write("ROI 영역이 잘못되었습니다!");
                return;
            }

            Mat windowImage = curImage[inspWindow.WindowArea];

            if (index < 0)
                inspWindow.AddWindowImage(windowImage);
            else
                inspWindow.SetWindowImage(windowImage, index);

            inspWindow.IsPatternLearn = false;

            MatchAlgorithm matchAlgo = (MatchAlgorithm)inspWindow.FindInspAlgorithm(InspectType.InspMatch);
            if (matchAlgo != null)
            {
                //#18_IMAGE_CHANNEL#8 패턴매칭 이미지 채널 설정, 칼라인 경우 그레이로 변경
                matchAlgo.ImageChannel = SelImageChannel;
                if (matchAlgo.ImageChannel == eImageChannel.Color)
                    matchAlgo.ImageChannel = eImageChannel.Gray;

                UpdateProperty(inspWindow);
            }
        }

        //#13_SET_IMAGE_BUFFER#1 InitImageSpace를 먼저 실행하도록 수정
        public void SetBuffer(int bufferCount)
        {
            _imageSpace.InitImageSpace(bufferCount);

            if (_grabManager != null)
            {
                _grabManager.InitBuffer(bufferCount);

                for (int i = 0; i < bufferCount; i++)
                {
                    _grabManager.SetBuffer(
                        _imageSpace.GetInspectionBuffer(i),
                        _imageSpace.GetnspectionBufferPtr(i),
                        _imageSpace.GetInspectionBufferHandle(i),
                        i);
                }
            }

            SLogger.Write("버퍼 초기화 성공!");
        }

        //#15_INSP_WORKER#8 TryInspection를 InspWorker로 이동
        public void TryInspection(InspWindow inspWindow)
        {
            UpdateDiagramEntity();
            InspWorker.TryInspect(inspWindow, InspectType.InspNone);
        }

        //#10_INSPWINDOW#13 ImageViewCtrl에서 ROI 생성,수정,이동,선택 등에 대한 함수
        public void SelectInspWindow(InspWindow inspWindow)
        {
            _selectedInspWindow = inspWindow;

            var propForm = MainForm.GetDockForm<PropertiesForm>();
            if (propForm != null)
            {
                if (inspWindow is null)
                {
                    propForm.ResetProperty();
                    return;
                }

                //속성창을 현재 선택된 ROI에 대한 것으로 변경
                propForm.ShowProperty(inspWindow);
            }

            UpdateProperty(inspWindow);

            Global.Inst.InspStage.PreView.SetInspWindow(inspWindow);
        }

        //ImageViwer에서 ROI를 추가하여, InspWindow생성하는 함수
        public void AddInspWindow(InspWindowType windowType, Rect rect)
        {
            var inspWindow = _model.AddInspWindow(windowType);
            if (inspWindow is null) return;

            inspWindow.Name = GenerateNextName(windowType);   // ★ 여기서 반드시 부여

            inspWindow.WindowArea = rect;
            inspWindow.IsTeach = false;

            SetTeachingImage(inspWindow);
            UpdateProperty(inspWindow);
            UpdateDiagramEntity();

            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SelectDiagramEntity(inspWindow);
                SelectInspWindow(inspWindow);
            }
        }

        public bool AddInspWindow(InspWindow sourceWindow, OpenCvSharp.Point offset)
        {
            var cloneWindow = sourceWindow?.Clone(offset);
            if (cloneWindow is null) return false;

            cloneWindow.Name = GenerateNextName(cloneWindow.InspWindowType); // ★ 복제도 새 번호

            if (!_model.AddInspWindow(cloneWindow)) return false;

            UpdateProperty(cloneWindow);
            UpdateDiagramEntity();

            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SelectDiagramEntity(cloneWindow);
                SelectInspWindow(cloneWindow);
            }
            return true;
        }


        //입력된 윈도우 이동
        public void MoveInspWindow(InspWindow inspWindow, OpenCvSharp.Point offset)
        {
            if (inspWindow == null)
                return;

            inspWindow.OffsetMove(offset);
            UpdateProperty(inspWindow);
        }

        //#MODEL#10 기존 ROI 수정되었을때, 그 정보를 InspWindow에 반영
        public void ModifyInspWindow(InspWindow inspWindow, Rect rect)
        {
            if (inspWindow == null)
                return;

            inspWindow.WindowArea = rect;
            inspWindow.IsTeach = false;

            UpdateProperty(inspWindow);
        }

        //#MODEL#11 InspWindow 삭제하기
        public void DelInspWindow(InspWindow inspWindow)
        {
            _model.DelInspWindow(inspWindow);
            if (inspWindow != null)
                NormalizeWindowNamesForType(inspWindow.InspWindowType);  // ★ 재정렬
            UpdateDiagramEntity();
        }


        public void DelInspWindow(List<InspWindow> inspWindowList)
        {
            _model.DelInspWindowList(inspWindowList);
            foreach (var g in inspWindowList.Where(w => w != null).GroupBy(w => w.InspWindowType))
                NormalizeWindowNamesForType(g.Key);                             // ★ 재정렬
            UpdateDiagramEntity();
        }

        public bool Grab(int bufferIndex)
        {
            if (_grabManager == null)
                return false;

            if (!_grabManager.Grab(bufferIndex, true))
                return false;

            return true;
        }


        //영상 취득 완료 이벤트 발생시 후처리
        private async void _multiGrab_TransferCompleted(object sender, object e)
        {
            CurrentImagePath = null;

            int bufferIndex = (int)e;
            SLogger.Write($"TransferCompleted {bufferIndex}");

            _imageSpace.Split(bufferIndex);

            DisplayGrabImage(bufferIndex);

            //#8_LIVE#2 LIVE 모드일때, Grab을 계속 실행하여, 반복되도록 구현
            //이 함수는 await를 사용하여 비동기적으로 실행되어, 함수를 async로 선언해야 합니다.
            if (LiveMode)
            {
                SLogger.Write("Grab");
                await Task.Delay(100);  // 비동기 대기
                _grabManager.Grab(bufferIndex, true);  // 다음 촬영 시작
            }
        }

        private void DisplayGrabImage(int bufferIndex)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateDisplay();
            }
        }

        public void UpdateDisplay(Bitmap bitmap)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateDisplay(bitmap);
            }
        }

        //#18_IMAGE_CHANNEL#6 프리뷰 이미지 채널을 설정하는 함수
        public void SetPreviewImage(eImageChannel channel)
        {
            if (_previewImage is null)
                return;

            Bitmap bitmap = ImageSpace.GetBitmap(0, channel);
            _previewImage.SetImage(BitmapConverter.ToMat(bitmap));

            SetImageChannel(channel);
        }

        //#18_IMAGE_CHANNEL#5 이미지 채널을 설정하는 함수
        public void SetImageChannel(eImageChannel channel)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SetImageChannel(channel);
            }
        }

        //비트맵 이미지 요청시, 이미지 채널이 있다면 SelImageChangel에 설정
        public Bitmap GetBitmap(int bufferIndex = -1, eImageChannel imageChannel = eImageChannel.None)
        {
            if (bufferIndex >= 0)
                SelBufferIndex = bufferIndex;

            //#BINARY FILTER#13 채널 정보가 유지되도록, eImageChannel.None 타입을 추가
            if (imageChannel != eImageChannel.None)
                SelImageChannel = imageChannel;

            if (Global.Inst.InspStage.ImageSpace is null)
                return null;

            return Global.Inst.InspStage.ImageSpace.GetBitmap(SelBufferIndex, SelImageChannel);
        }

        //#7_BINARY_PREVIEW#4 이진화 프리뷰를 위해, ImageSpace에서 이미지 가져오기
        public Mat GetMat(int bufferIndex = -1, eImageChannel imageChannel = eImageChannel.None)
        {
            if (bufferIndex >= 0)
                SelBufferIndex = bufferIndex;

            return Global.Inst.InspStage.ImageSpace.GetMat(SelBufferIndex, imageChannel);
        }

        //#10_INSPWINDOW#14 변경된 모델 정보 갱신하여, ImageViewer와 모델트리에 반영
        public void UpdateDiagramEntity()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateDiagramEntity();
            }

            ModelTreeForm modelTreeForm = MainForm.GetDockForm<ModelTreeForm>();
            if (modelTreeForm != null)
            {
                modelTreeForm.UpdateDiagramEntity();
            }
        }

        //#7_BINARY_PREVIEW#5 이진화 임계값 변경시, 프리뷰 갱신
        public void RedrawMainView()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateImageViewer();
            }
        }
        public void ResetDisplay()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.ResetDisplay();
            }
        }

        //#12_MODEL SAVE#4 Mainform에서 호출되는 모델 열기와 저장 함수        
        public bool LoadModel(string filePath)
        {
            SLogger.Write($"모델 로딩:{filePath}");

            _model = _model.Load(filePath);

            

            if (_model is null)
            {
                SLogger.Write($"모델 로딩 실패:{filePath}");
                return false;
            }

            string inspImagePath = _model.InspectImagePath;
            if (File.Exists(inspImagePath))
            {
                Global.Inst.InspStage.SetImageBuffer(inspImagePath);
            }

            // 로드 직후 한 번 전체 정리(기존 모델 호환)
            foreach (InspWindowType t in Enum.GetValues(typeof(InspWindowType)))
                if (t != InspWindowType.None) NormalizeWindowNamesForType(t);

            UpdateDiagramEntity();

            //#16_LAST_MODELOPEN#3 마지막 저장 모델 경로를 레지스트리에 저장
            _regKey.SetValue("LastestModelPath", filePath);

            return true;
        }

        

        /// <summary>
        /// ".xml" 확장자 보장 + 절대경로화 + 파일명(확장자 제외) 추출
        /// </summary>
        private static void NormalizeModelPath(
            string inputPath,
            out string normalizedFullPath,
            out string nameWithoutExt)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                throw new ArgumentException("filePath is null or empty");

            string path = inputPath.Trim();

            // 확장자 보장
            if (!string.Equals(Path.GetExtension(path), ".xml", StringComparison.OrdinalIgnoreCase))
                path += ".xml";

            normalizedFullPath = Path.GetFullPath(path);
            nameWithoutExt = Path.GetFileNameWithoutExtension(normalizedFullPath);
        }

        private bool LastestModelOpen()
        {
            if (_lastestModelOpen)
                return true;

            _lastestModelOpen = true;

            string lastestModel = (string)_regKey.GetValue("LastestModelPath");
            if (File.Exists(lastestModel) == false)
                return true;

            DialogResult result = MessageBox.Show($"최근 모델을 로딩할까요?\r\n{lastestModel}", "Question", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
                return true;

            return LoadModel(lastestModel);
        }

        //#15_INSP_WORKER#9 자동 연속 검사 함수
        public void CycleInspect(bool isCycle)
        {
            if (InspWorker.IsRunning)
                return;

            if (!UseCamera)
            {
                string inspImagePath = CurModel.InspectImagePath;
                if (inspImagePath == "")
                    return;

                string inspImageDir = Path.GetDirectoryName(inspImagePath);
                if (!Directory.Exists(inspImageDir))
                    return;

                if (!_imageLoader.IsLoadedImages())
                    _imageLoader.LoadImages(inspImageDir);
            }

            if (isCycle)
                _inspWorker.StartCycleInspectImage();
            else
                OneCycle();
        }

        public bool OneCycle()
        {
            if (UseCamera)
            {
                if (!Grab(0))
                    return false;
            }
            else
            {
                if (!VirtualGrab())
                    return false;
            }

            ResetDisplaySafe();   // ★ 교체

            bool isDefect;
            if (!_inspWorker.RunInspect(out isDefect))
                return false;

            return true;
        }

        public void StopCycle()
        {
            if (_inspWorker != null)
                _inspWorker.Stop();

            //#19_VISION_SEQUENCE#4 시퀀스 정지
            VisionSequence.Inst.StopAutoRun();

            SetWorkingState(WorkingState.NONE);
        }

        public bool VirtualGrab()
        {
            if (_imageLoader is null)
                return false;

            string imagePath = _imageLoader.GetNextImagePath();
            if (imagePath == "")
                return false;

            // ★ 경로 갱신
            CurrentImagePath = imagePath;
            if (CurModel != null) CurModel.InspectImagePath = imagePath;

            Global.Inst.InspStage.SetImageBuffer(imagePath);
            _imageSpace.Split(0);
            DisplayGrabImage(0);
            return true;
        }

        //#19_VISION_SEQUENCE#7 시퀀스 명령 처리
        private void SeqCommand(object sender, SeqCmd seqCmd, object Param)
        {
            switch (seqCmd)
            {
                case SeqCmd.InspStart:
                    {
                        //#WCF_FSM#5 카메라 촬상 후, 검사 진행
                        SLogger.Write("MMI : InspStart", SLogger.LogType.Info);

                        //검사 시작
                        string errMsg;

                        if (UseCamera)
                        {
                            if (!Grab(0))
                            {
                                errMsg = string.Format("Failed to grab");
                                SLogger.Write(errMsg, SLogger.LogType.Error);
                            }
                        }
                        else
                        {
                            if (!VirtualGrab())
                            {
                                errMsg = string.Format("Failed to virtual grab");
                                SLogger.Write(errMsg, SLogger.LogType.Error);
                            }
                        }
                        Thread.Sleep(500);

                        bool isDefect = false;
                        if (!_inspWorker.RunInspect(out isDefect))
                        {
                            errMsg = string.Format("Failed to inspect");
                            SLogger.Write(errMsg, SLogger.LogType.Error);
                        }
                        
                        //#WCF_FSM#6 비젼 -> 제어에 검사 완료 및 결과 전송
                        VisionSequence.Inst.VisionCommand(Vision2Mmi.InspDone, isDefect);
                    }
                    break;
                case SeqCmd.InspEnd:
                    {
                        SLogger.Write("MMI : InspEnd", SLogger.LogType.Info);

                        //모든 검사 종료
                        string errMsg = "";

                        //검사 완료에 대한 처리
                        SLogger.Write("검사 종료");

                        VisionSequence.Inst.VisionCommand(Vision2Mmi.InspEnd, errMsg);
                    }
                    break;
            }
        }


        //검사를 위한 준비 작업
        public bool InspectReady(string lotNumber, string serialID)
        {
            _lotNumber = lotNumber;
            _serialID = serialID;

            LiveMode = false;
            UseCamera = SettingXml.Inst.CamType != CameraType.None ? true : false;

            Global.Inst.InspStage.CheckImageBuffer();

            ResetDisplay();

            return true;
        }

        public bool StartAutoRun()
        {
            SLogger.Write("Action : StartAutoRun");

            string modelPath = CurModel.ModelPath;
            if (modelPath == "")
            {
                SLogger.Write("열려진 모델이 없습니다!", SLogger.LogType.Error);
                MessageBox.Show("열려진 모델이 없습니다!");
                return false;
            }

            LiveMode = false;
            UseCamera = SettingXml.Inst.CamType != CameraType.None ? true : false;

            SetWorkingState(WorkingState.INSPECT);

            //#19_VISION_SEQUENCE#5 자동검사 시작
            string modelName = Path.GetFileNameWithoutExtension(modelPath);
            VisionSequence.Inst.StartAutoRun(modelName);
            return true;
        }

        //#17_WORKING_STATE#2 작업 상태 설정
        public void SetWorkingState(WorkingState workingState)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SetWorkingState(workingState);
            }
        }
        public void ResetAccum()
        {
            Accum.Reset();
            AccumChanged?.Invoke(Accum);
        }
        public void AddAccumCount(int total, int ok, int ng)
        {
            Accum.Add(total, ok, ng);
            AccumChanged?.Invoke(Accum);
        }

        // InspStage 클래스 필드/프로퍼티 구역
        public string CurrentImagePath { get; private set; } = null;

        private void ResetDisplaySafe()
        {
            var cam = MainForm.GetDockForm<CameraForm>();
            if (cam != null && cam.IsHandleCreated)
                cam.BeginInvoke((Action)(() => ResetDisplay()));
            else
                ResetDisplay();
        }

        public bool InspectCurrentOnly()
        {
            // 연속검사 돌고 있으면 무시 (경합 방지)
            if (_inspWorker != null && _inspWorker.IsRunning)
                return false;

            // 현재 프레임 유효 여부(예: 채널0 체크) — 필요에 맞게 보완
            var m = GetMat(0, 0);
            if (m == null || m.Empty())
                return false; // 아직 한 번도 그랩 안 함

            ResetDisplaySafe(); // UI는 메인스레드에서 (앞서 만든 래퍼)

            bool isDefect;
            return _inspWorker.RunInspect(out isDefect);
        }

        public void ResetToEmptyModel()
        {

            if (CurModel == null)
                CurModel = new Model();
            else
                CurModel.Reset();

            SLogger.Write("InspStage: 모델을 빈 상태로 전환");
        }

        #region Disposable

        private bool disposed = false; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.

                    //#19_VISION_SEQUENCE#6 시퀀스 이벤트 해제
                    VisionSequence.Inst.SeqCommand -= SeqCommand;

                    if (_saigeAI != null)
                    {
                        _saigeAI.Dispose();
                        _saigeAI = null;
                    }
                    if (_grabManager != null)
                    {
                        _grabManager.Dispose();
                        _grabManager = null;
                    }

                    //#16_LAST_MODELOPEN#4 registry 키를 닫습니다.
                    _regKey.Close();
                }

                // Dispose unmanaged managed resources.
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion //Disposable
        
    }
}

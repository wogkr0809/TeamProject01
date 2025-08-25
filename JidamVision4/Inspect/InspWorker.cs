using JidamVision4.Algorithm;
using JidamVision4.Core;
using JidamVision4.Teach;
using JidamVision4.Util;
using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JidamVision4.Inspect
{
    /*
    #15_INSP_WORKER# - <<<자동 검사 시스템 구현>>> 
    검사 관리 클래스 : 전체 검사 또는 개별 검사 동작
    1) InspBoard 클래스 생성 - Board 단위로 검사하는 클래스
    2) InspWorker 클래스 생성 - 검사 관리 클래스
    3) ImageFileSorter 클래스 추가 - 이미지 파일을 이름에 따라 정렬하는 클래스
    4) ImageLoader 클래스 추가 - 선택 폴더에 있는 이미지 파일 리스트 관리 클래스
    5) 
   */

    //검사 관련 처리 클래스
    public class InspWorker
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private InspectBoard _inspectBoard = new InspectBoard();

        public bool IsRunning { get; set; } = false;

        public InspWorker()
        {
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public void StartCycleInspectImage()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => InspectionLoop(this, _cts.Token));
        }

        private void InspectionLoop(InspWorker inspWorker, CancellationToken token)
        {
            Global.Inst.InspStage.SetWorkingState(WorkingState.INSPECT);

            SLogger.Write("InspectionLoop Start");

            IsRunning = true;

            while (!token.IsCancellationRequested)
            {
                Global.Inst.InspStage.OneCycle();

                Thread.Sleep(300); // 주기 설정
            }

            IsRunning = false;

            SLogger.Write("InspectionLoop End");
        }

        //InspStage내의 모든 InspWindow들을 검사하는 함수
        public bool RunInspect(out bool isDefect)
        {
            isDefect = false;
            Model curMode = Global.Inst.InspStage.CurModel;
            var inspWindowList = curMode.InspWindowList;

            // ★ 비활성(체크 해제/숨김) ROI 제외
            var active = inspWindowList
                .Where(w => w != null && w.IgnoreInsp)  // <- TreeView 체크와 동기화된 플래그
                .ToList();

            // 검사 전 데이터 갱신도 활성만
            foreach (var w in active)

                UpdateInspData(w);

            // ★ 활성만 보드에 전달
            if (active.Count > 0)
                _inspectBoard.InspectWindowList(active);

            int totalCnt = 0, okCnt = 0, ngCnt = 0;
            var allRects = new List<DrawInspectInfo>();

            // ★ 결과 집계/표시도 활성만
            foreach (var w in active)
            {
                totalCnt++;
                if (w.IsDefect()) { isDefect = true; ngCnt++; }
                else okCnt++;

                var rects = CollectResultRects(w, InspectType.InspNone);
                if (rects != null && rects.Count > 0)
                    allRects.AddRange(rects);
            }

            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null && cameraForm.IsHandleCreated)
            {
                cameraForm.BeginInvoke((Action)(() =>
                {
                    cameraForm.AddRect(allRects);
                    cameraForm.SetInspResultCount(totalCnt, okCnt, ngCnt);
                }));
            }
            //

            var resultForm = MainForm.GetDockForm<ResultForm>();
            if (resultForm != null && resultForm.IsHandleCreated)
            {
                resultForm.BeginInvoke((Action)(() =>
                {
                    resultForm.AddModelResult(curMode);
                }));
            }

            Global.Inst.InspStage.AddAccumCount(1, isDefect ? 0 : 1, isDefect ? 1 : 0);
            // 활성 ROI 리스트: active
            // ★ ROI별 NG 누적 (각 ROI가 NG면 해당 카테고리에 +1)
            Global.Inst.InspStage.NgCounter.AddFromWindows(active);
            return true;

        }

        //특정 InspWindow에 대한 검사 진행
        //inspType이 있다면 그것만을 검사하고, 없다면 InpsWindow내의 모든 알고리즘 검사
        public bool TryInspect(InspWindow inspObj, InspectType inspType)
        {
            if (inspObj != null)
            {
                if (!inspObj.IgnoreInsp)
                    return true;

                if (!UpdateInspData(inspObj))
                    return false;

                _inspectBoard.Inspect(inspObj);

                DisplayResult(inspObj, inspType);
            }
            else
            {
                bool isDefect = false;
                RunInspect(out isDefect);
            }

            ResultForm resultForm = MainForm.GetDockForm<ResultForm>();
            if (resultForm != null)
            {
                if (inspObj != null)
                    resultForm.AddWindowResult(inspObj);
                else
                {
                    Model curMode = Global.Inst.InspStage.CurModel;
                    resultForm.AddModelResult(curMode);
                }
            }

            return true;
        }

        //각 알고리즘 타입 별로 검사에 필요한 데이터를 입력하는 함수
        private bool UpdateInspData(InspWindow inspWindow)
        {
            if (inspWindow is null)
                return false;

            // ▼ 추가: Scratch/Soldering 윈도우는 InspBinary를 아예 리스트에서 제거
            if (inspWindow.InspWindowType == InspWindowType.Scratch ||
                inspWindow.InspWindowType == InspWindowType.Soldering)
            {
                inspWindow.AlgorithmList?.RemoveAll(a => a.InspectType == InspectType.InspBinary);
            }

            Rect windowArea = inspWindow.WindowArea;

            inspWindow.PatternLearn();


            foreach (var inspAlgo in inspWindow.AlgorithmList)
            {
                // 1) 검사 영역 초기화
                inspAlgo.TeachRect = windowArea;
                inspAlgo.InspRect = windowArea;
                // 2) 원본 소스 전달(채널별)
                Mat srcImage = Global.Inst.InspStage.GetMat(0, inspAlgo.ImageChannel);
                if (srcImage == null || srcImage.Empty())
                    continue;
                inspAlgo.SetInspData(srcImage);

                var surf = inspAlgo as JidamVision4.Algorithm.SurfaceDefectAlgorithm;
                if (surf != null)
                {
                    var model = Global.Inst.InspStage.CurModel;
                    var others = model.InspWindowList
                                      .Where(w => w != null && !ReferenceEquals(w, inspWindow))
                                      .Select(w => w.WindowArea)
                                      .ToList();
                }
            }

            return true;
        }

        //InspWindow내의 알고리즘 중에서, 인자로 입력된 알고리즘과 같거나,
        //인자가 None이면 모든 알고리즘의 검사 결과(Rect 영역)를 얻어, cameraForm에 출력한다.
        private bool DisplayResult(InspWindow inspObj, InspectType inspType)
        {
            if (inspObj is null)
                return false;

            List<DrawInspectInfo> totalArea = new List<DrawInspectInfo>();

            List<InspAlgorithm> inspAlgorithmList = inspObj.AlgorithmList;
            foreach (var algorithm in inspAlgorithmList)
            {
                if ((inspObj.InspWindowType == InspWindowType.Scratch ||
                inspObj.InspWindowType == InspWindowType.Soldering) &&
                algorithm.InspectType == InspectType.InspBinary)
                    continue;

                if (algorithm.InspectType != inspType && inspType != InspectType.InspNone)
                    continue;

                List<DrawInspectInfo> resultArea = new List<DrawInspectInfo>();
                int resultCnt = algorithm.GetResultRect(out resultArea);
                if (resultCnt > 0)
                {
                    totalArea.AddRange(resultArea);
                }
            }

            if (totalArea.Count > 0)
            {
                //찾은 위치를 이미지상에서 표시
                var cameraForm = MainForm.GetDockForm<CameraForm>();
                if (cameraForm != null)
                {
                    cameraForm.AddRect(totalArea);
                }
            }

            return true;
        }
        private List<DrawInspectInfo> CollectResultRects(InspWindow inspObj, InspectType inspType)
        {
            var totalArea = new List<DrawInspectInfo>();
            if (inspObj == null) return totalArea;

            foreach (var algorithm in inspObj.AlgorithmList)
            {
                if ((inspObj.InspWindowType == InspWindowType.Scratch ||
                inspObj.InspWindowType == InspWindowType.Soldering) &&
                algorithm.InspectType == InspectType.InspBinary)
                    continue;
                if (algorithm.InspectType != inspType && inspType != InspectType.InspNone)
                    continue;

                if (algorithm.GetResultRect(out var resultArea) > 0 && resultArea != null)
                    totalArea.AddRange(resultArea);
            }
            return totalArea;
        }
    }
}

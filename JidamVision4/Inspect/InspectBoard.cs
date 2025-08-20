using JidamVision4.Algorithm;
using JidamVision4.Core;
using JidamVision4.Teach;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace JidamVision4.Inspect
{
    public class InspectBoard
    {
        public InspectBoard()
        {
        }

        public bool Inspect(InspWindow window)
        {
            if (window is null)
                return false;

            if (!InspectWindow(window))
                return false;
            
            return true;
        }

        private bool InspectWindow(InspWindow window)
        {
            window.ResetInspResult();
            foreach (InspAlgorithm algo in window.AlgorithmList)
            {
                if (algo.IsUse == false)
                    continue;

                if (!algo.DoInspect())
                    return false;

                string resultInfo = string.Join("\r\n", algo.ResultString);

                InspResult inspResult = new InspResult
                {
                    ObjectID = window.UID,
                    InspType = algo.InspectType,
                    IsDefect = algo.IsDefect,
                    ResultInfos = resultInfo
                };

                switch (algo.InspectType)
                {
                    case InspectType.InspMatch:
                        MatchAlgorithm matchAlgo = algo as MatchAlgorithm;
                        inspResult.ResultValue = $"{matchAlgo.OutScore}";
                        break;
                    case InspectType.InspBinary:
                        BlobAlgorithm blobAlgo = algo as BlobAlgorithm;
                        int min = blobAlgo.BlobFilters[blobAlgo.FILTER_COUNT].min;
                        int max = blobAlgo.BlobFilters[blobAlgo.FILTER_COUNT].max;

                        inspResult.ResultValue = $"{blobAlgo.OutBlobCount}/{min}~{max}";
                        break;
                }

                List<DrawInspectInfo> resultArea = new List<DrawInspectInfo>();
                int resultCnt = algo.GetResultRect(out resultArea);
                inspResult.ResultRectList = resultArea;

                window.AddInspResult(inspResult);
            }

            return true;
        }

        public bool InspectWindowList(List<InspWindow> windowList)
        {
            if (windowList == null || windowList.Count == 0) return false;

            // 1) ID 매칭으로 오프셋 획득
            Point alignOffset = new Point(0,0);
            var idWindow = windowList.Find(w => w.InspWindowType == InspWindowType.ID);
            if (idWindow != null)
            {
                var match = idWindow.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
                if (match != null && match.IsUse)
                {
                    if (!InspectWindow(idWindow)) return false;
                    if (match.IsInspected)
                    {
                        alignOffset = match.GetOffset();
                        // (선택) 표시용
                        idWindow.InspArea = idWindow.WindowArea + alignOffset;
                    }
                }
            }

            // 2) 모든 창 검사 (ID는 재검사 스킵)
            foreach (var win in windowList)
            {
                if (win == idWindow) continue;

                // (권장) 명시적으로 각 알고리즘의 Rect 세팅
                var inspArea = win.WindowArea;
                inspArea.X += alignOffset.X; inspArea.Y += alignOffset.Y;

                foreach (var algo in win.AlgorithmList)
                {
                    algo.TeachRect = win.WindowArea; // 학습 기준 유지
                    algo.InspRect = inspArea;       // 검사 좌표 보정
                    var src = Global.Inst.InspStage.GetMat(0, algo.ImageChannel);
                    algo.SetInspData(src);
                }

                if (!InspectWindow(win)) return false;
            }

            foreach (var win in windowList)
            {
                if (!InspectWindow(win)) return false;
            }

            // 3) 선택: ROI를 모델에 반영(영구 이동)할지 여부
            bool commitRoiShift = true; // ← 옵션 플래그
            if (commitRoiShift && (alignOffset.X != 0 || alignOffset.Y != 0))
            {
                foreach (var win in windowList)
                {
                    var r = win.WindowArea; // OpenCvSharp.Rect
                    r.X += alignOffset.X;
                    r.Y += alignOffset.Y;
                    win.WindowArea = r;
                }

                // UI 갱신(네 프로젝트에 이미 있는 훅을 사용)
                // 예: CameraForm.UpdateDiagramEntity() 같은 메서드가 있다면 호출
                var cam = MainForm.GetDockForm<CameraForm>();
                cam?.UpdateDiagramEntity(); // 모델 변경을 뷰어로 반영
            }

            return true;
        }
    }
}

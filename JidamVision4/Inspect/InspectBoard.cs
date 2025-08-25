using JidamVision4.Algorithm;
using JidamVision4.Core;
using JidamVision4.Teach;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

            // 1) ID 윈도우로 정렬 오프셋 계산(순차)
            Point alignOffset = new Point(0, 0);
            var idWindow = windowList.Find(w => w.InspWindowType == InspWindowType.ID);
            if (idWindow != null)
            {
                var match = idWindow.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
                if (match != null && match.IsUse)
                {
                    idWindow.ResetInspResult(); // 결과 초기화 (thread-safe 구현은 패치 2 참고)
                    foreach (var algo in idWindow.AlgorithmList)
                    {
                        if (!algo.IsUse || algo.InspectType != InspectType.InspMatch) continue;
                        if (!algo.DoInspect()) break; // UI 접근 금지
                    }
                    if (match.IsInspected)
                    {
                        alignOffset = match.GetOffset();
                        // (선택) 표시용
                        idWindow.InspArea = idWindow.WindowArea + alignOffset;
                    }
                }
            }

            // 2) 대상 ROI 준비(ID 제외) + 오프셋 적용(사전 세팅만)
            var targets = windowList.Where(w => w != null && w != idWindow).ToList();
            foreach (var win in targets)
            {
                var inspArea = win.WindowArea;
                inspArea.X += alignOffset.X;
                inspArea.Y += alignOffset.Y;

                foreach (var algo in win.AlgorithmList)
                {
                    // TeachRect/SetInspData는 RunInspect()→UpdateInspData()에서 이미 세팅됨
                    algo.InspRect = inspArea; // 보정된 검사영역 적용
                }
            }

            // 3) 병렬 검사 (UI 접근 금지)
            int anyFail = 0;
            var po = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
            };

            Parallel.ForEach(targets, po, win =>
            {
                try
                {
                    win.ResetInspResult(); // thread-safe (패치 2 참조)
                    foreach (var algo in win.AlgorithmList)
                    {
                        if (!algo.IsUse) continue;
                        if (!algo.DoInspect())
                        {
                            Interlocked.Exchange(ref anyFail, 1);
                            return;
                        }
                        // ★ 여기서 결과를 채운다
                        var res = new InspResult
                        {
                            ObjectID = win.UID,
                            InspType = algo.InspectType,
                            IsDefect = algo.IsDefect,
                            ResultValue = GetResultValue(algo),
                            ResultInfos = (algo.ResultString != null) ? string.Join("\r\n", algo.ResultString) : null
                        };
                        win.AddInspResult(res); // thread-safe 버전
                    }
                }
                catch
                {
                    Interlocked.Exchange(ref anyFail, 1);
                }
            });

            if (alignOffset.X != 0 || alignOffset.Y != 0)
            {
                foreach (var win in windowList)
                {
                    var r = win.WindowArea;
                    r.X += alignOffset.X;
                    r.Y += alignOffset.Y;
                    win.WindowArea = r;                 // ← ROI 실제 좌표 이동(커밋)
                }

                var cam = MainForm.GetDockForm<CameraForm>();
                if (cam != null && cam.IsHandleCreated)
                    cam.BeginInvoke((Action)(() => cam.UpdateDiagramEntity())); // 뷰 갱신
            }
            return anyFail == 0;
        }
        private static string GetResultValue(InspAlgorithm algo)
        {
            if (algo is MatchAlgorithm m) return m.OutScore.ToString();
            if (algo is BlobAlgorithm b) return b.OutBlobCount.ToString(); // 프로젝트 명칭에 맞게
            return null;
        }
    }
}

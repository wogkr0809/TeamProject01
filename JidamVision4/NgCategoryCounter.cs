using JidamVision4.Core;
using JidamVision4.Teach;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JidamVision4
{
    public class NgCategoryCounter
    {
        public bool LastRunHasScratch { get; private set; }
        public bool LastRunHasSolder { get; private set; }

        // 원하는 카테고리만 고정 목록으로 관리
        public static readonly string[] Keys = { "Chip", "Lead", "Resistance", "Scratch", "Soldering" };

        readonly Dictionary<string, long> _map =
            Keys.ToDictionary(k => k, _ => 0L, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, long> Map => _map;

        public event Action<IReadOnlyDictionary<string, long>> Changed;

        public void Reset()
        {
            foreach (var k in Keys) _map[k] = 0; // 표 0으로 초기화
            LastRunHasScratch = false;           // ★ 플래그도 초기화
            LastRunHasSolder = false;
            Changed?.Invoke(_map);               // UI 즉시 0 표시
        }

        public void Add(string key, int n = 1)
        {
            if (!_map.ContainsKey(key)) return;   // 목록 외는 무시(원하면 추가로 허용)
            _map[key] += n;
            Changed?.Invoke(_map);
        }

        // ROI 목록에서 NG인 것만 카테고리별로 1회씩 누적
        public void AddFromWindows(IEnumerable<InspWindow> wins)
        {
            if (wins == null) return;

            foreach (var w in wins)
            {
                if (w == null) continue;

                // 1) SurfaceDefectAlgorithm(스크래치/솔더링) → 도형 라벨로 개수 집계
                var surfResults = w.InspResultList?
                    .Where(r => r.IsDefect &&
                                r.InspType == InspectType.InspSurfaceDefect)
                    .ToList();

                if (surfResults != null && surfResults.Count > 0)
                {
                    int scratch = 0, solder = 0;

                    foreach (var r in surfResults)
                    {
                        var rects = r.ResultRectList;
                        if (rects == null) continue;

                        foreach (var di in rects)
                        {
                            var label = di?.info?.Trim();
                            if (string.IsNullOrEmpty(label)) continue;

                            if (label.Equals("Scratch", StringComparison.OrdinalIgnoreCase)) scratch++;
                            else if (label.Equals("Soldering", StringComparison.OrdinalIgnoreCase)) solder++;
                        }
                    }

                    if (scratch > 0) { Add("Scratch", scratch); LastRunHasScratch = true; }   // ★ 플래그 ON
                    if (solder > 0) { Add("Soldering", solder); LastRunHasSolder = true; } // ★ 플래그 ON
                }

                // 2) 그 밖의 ROI 타입은 NG일 때 1회 누적 (Scratch/Soldering은 위에서 처리)
                if (w.IsDefect())
                {
                    string cat = GetCategory(w);
                    if (!cat.Equals("Scratch", StringComparison.OrdinalIgnoreCase) &&
                        !cat.Equals("Soldering", StringComparison.OrdinalIgnoreCase))
                    {
                        Add(cat, 1);
                    }
                }
            }
            LastRunHasScratch = _map.TryGetValue("Scratch", out var sc) && sc > 0;
            LastRunHasSolder = _map.TryGetValue("Soldering", out var so) && so > 0;
        }

        // ROI → 카테고리명 매핑 (enum 또는 이름에서 유추)
        private static string GetCategory(InspWindow w)
        {
            var id = (w?.UID ?? "").Trim().ToLowerInvariant();

            if (id.StartsWith("chip")) return "Chip";
            if (id.StartsWith("lead")) return "Lead";
            if (id.StartsWith("res") || id.StartsWith("resi")) return "Resistance";
            if (id.StartsWith("scratch")) return "Scratch";
            if (id.StartsWith("sold")) return "Soldering";

            return "Chip";
        }
    }
}

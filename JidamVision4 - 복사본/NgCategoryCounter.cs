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
        // 원하는 카테고리만 고정 목록으로 관리
        public static readonly string[] Keys = { "Chip", "Lead", "Resistance", "Scratch", "Soldering" };

        readonly Dictionary<string, long> _map =
            Keys.ToDictionary(k => k, _ => 0L, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, long> Map => _map;

        public event Action<IReadOnlyDictionary<string, long>> Changed;

        public void Reset()
        {
            foreach (var k in Keys) _map[k] = 0;
            Changed?.Invoke(_map);
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
            foreach (var w in wins)
            {
                if (w == null) continue;
                if (!w.IsDefect()) continue;           // 해당 ROI가 NG일 때만

                string cat = GetCategory(w);
                Add(cat, 1);
            }
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

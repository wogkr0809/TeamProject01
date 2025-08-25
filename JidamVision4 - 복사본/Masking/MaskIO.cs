using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JidamVision4.Masking
{
    /// <summary>
    /// 마스크 비트맵 로드/저장 유틸. 항상 쓰기 가능한 32bpp로 보장.
    /// </summary>
    public static class MaskIO
    {
        /// <summary>인덱스 포맷(예: 8bpp)으로 로드된 이미지를 32bpp ARGB로 변환해 반환.</summary>
        public static Bitmap EnsureEditable(Bitmap src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));

            // 인덱스 포맷이면 32bpp로 복사
            if ((src.PixelFormat & PixelFormat.Indexed) != 0)
            {
                var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(dst))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.DrawImageUnscaled(src, 0, 0);
                }
                src.Dispose();
                return dst;
            }
            return src;
        }

        /// <summary>파일에서 마스크 로드(없으면 새로 생성). 항상 32bpp ARGB 반환.</summary>
        public static Bitmap LoadMask(string path, Size sizeIfCreate, bool createWhite = true)
        {
            if (File.Exists(path))
            {
                // File lock 피하려고 복사본 생성
                using (var tmp = (Bitmap)Image.FromFile(path))
                    return EnsureEditable(new Bitmap(tmp));
            }

            // 신규 생성
            var bmp = new Bitmap(sizeIfCreate.Width, sizeIfCreate.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(createWhite ? Color.White : Color.Black);
            }
            return bmp;
        }

        /// <summary>PNG로 저장(32bpp 보장 상태에서 저장 권장).</summary>
        public static void SaveMask(Bitmap mask, string path)
        {
            if (mask == null) return;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            mask.Save(path, ImageFormat.Png);
        }
    }
}


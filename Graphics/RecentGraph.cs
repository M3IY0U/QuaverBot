using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuaverBot.Graphics
{
    public class RecentGraph
    {
        public static MemoryStream CreateGraphBanner(string url, List<long> hitdata)
        {
            using (var client = new WebClient())
                client.DownloadFile(url, "background.jpg");

            using (var background = Image.Load<Rgba32>("background.jpg"))
            using (var output = new Image<Rgba32>(900, 250))
            using (var img = new Image<Rgba32>(900, 250))
            {
                for (var i = 1; i < hitdata.Count - 1; ++i)
                {
                    var x = 900.0f / hitdata.Count * i;
                    var y = 250 / 2 + hitdata[i];
                    if (Math.Abs(hitdata[i]) > 127)
                        y = 250 / 2;
                    img.Mutate(mut => mut.Fill(HitToColor(hitdata[i]), new EllipsePolygon(new PointF(x, y), 2)));
                }

                output.Mutate(o => o
                    .DrawImage(background, 0.3f)
                    .DrawImage(img, 1f)
                );

                var memStream = new MemoryStream();
                output.Save(memStream, PngFormat.Instance);
                memStream.Seek(0, SeekOrigin.Begin);
                return memStream;
            }
        }

        private static Color HitToColor(long input)
        {
            return Math.Abs(input) switch
            {
                <= 18 => Color.Silver,
                > 18 and <= 43 => Color.Gold,
                > 43 and <= 76 => Color.Green,
                > 76 and <= 106 => Color.Aqua,
                > 106 and <= 127 => Color.Magenta,
                > 127 => Color.Red
            };
        }
    }
}
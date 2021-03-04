using System;
using System.IO;
using System.Net;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace QuaverBot.Graphics
{
    public class RecentGraph
    {
        private const int BannerWidth = 900;
        private const int BannerHeight = 250;

        public static MemoryStream CreateGraphBanner(string url, List<long> hitData)
        {
            using (var client = new WebClient())
                client.DownloadFile(url, "background.jpg");

            var background = Image.Load<Rgba32>("background.jpg");
            var graph = new Image<Rgba32>(BannerWidth, BannerHeight);
            graph.Mutate(mut => mut.DrawLines(Pens.Solid(Brushes.BackwardDiagonal(Color.White), 1f),
                new PointF(0, BannerHeight / 2f), new PointF(BannerWidth, BannerHeight / 2f)));

            for (var i = 0; i < hitData.Count; i++)
            {
                var x = (float) BannerWidth / hitData.Count * i;
                var y = BannerHeight / 2 + hitData[i];
                Action<IImageProcessingContext> dings;
                if (Math.Abs(hitData[i]) > 127)
                    dings = mut => mut.DrawLines(Pens.Solid(HitToColor(hitData[i]), 0.5f),
                        new PointF(x, 0), new PointF(x, graph.Height));
                else
                    dings = mut => mut.Fill(HitToColor(hitData[i]), new EllipsePolygon(new PointF(x, y), 2));
                graph.Mutate(dings);
            }

            using (var output = new Image<Rgba32>(BannerWidth, BannerHeight))
            {
                output.Mutate(o => o
                    .Fill(Color.Black)
                    .DrawImage(background, 0.3f)
                    .DrawImage(graph, 1f)
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
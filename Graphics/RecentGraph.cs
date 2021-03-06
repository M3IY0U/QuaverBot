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

        public static MemoryStream CreateGraphBanner(string url, List<long> hitData, bool containsMisses,
            double progress)
        {
            // setup
            var background = Image.Load(new WebClient().DownloadData(url));
            var graph = new Image<Rgba32>(BannerWidth, BannerHeight);

            // if there are hits, draws a dotted line in the center
            if (hitData.Count > 0)
                graph.Mutate(mut => mut.DrawLines(Pens.Solid(Brushes.BackwardDiagonal(Color.White), 1f),
                    new PointF(0, BannerHeight / 2f), new PointF(BannerWidth, BannerHeight / 2f)));

            for (var i = 0; i < hitData.Count; i++)
            {
                // calculate x and y position of the hit in relation to the banner dimensions
                var x = (float) BannerWidth / hitData.Count * i;
                var y = BannerHeight / 2 + hitData[i];
                // copy to local variable because rider keeps yelling at me
                var localI = i;

                // mutation is either: a) draw a red vertical line, indicating a miss or b) draw a normal hit 
                Action<IImageProcessingContext> mutationAction;
                if (Math.Abs(hitData[i]) > 127 && containsMisses)
                    mutationAction = mut => mut.DrawLines(Pens.Solid(HitToColor(hitData[localI]), 0.5f),
                        new PointF(x, 0), new PointF(x, graph.Height));
                else
                    mutationAction = mut =>
                        mut.Fill(HitToColor(hitData[localI]), new EllipsePolygon(new PointF(x, y), 2));

                graph.Mutate(mutationAction);
            }

            // if map was not completed, grayscale the incomplete part
            if (progress < 100)
                background.Mutate(b => b.Grayscale(.9f, new Rectangle(
                    (int) (BannerWidth * ((float) progress / 100f)), 0,
                    BannerWidth - (int) (BannerWidth * ((float) progress / 100f)), BannerHeight)));

            // bring everything together, with a dim background if there are hits, full brightness if not
            using var output = new Image<Rgba32>(BannerWidth, BannerHeight);
            output.Mutate(o => o
                .Fill(Color.Black)
                .DrawImage(background, hitData.Count > 0 ? 0.3f : 1f)
                .DrawImage(graph, 1f)
            );

            // return the image
            var memStream = new MemoryStream();
            output.Save(memStream, PngFormat.Instance);
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }

        private static Color HitToColor(long input)
            => Math.Abs(input) switch
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using QuaverBot.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuaverBot.Graphics
{
    public class RankGraph
    {
        private const int GraphWidth = 800;
        private const int GraphHeight = 300;
        private const string BackgroundUrl =
            "https://raw.githubusercontent.com/Quaver/Quaver.Resources/3fd8e5be56f56a1d721efb3b344c99f96e16c900/Quaver.Resources/Textures/UI/MainMenu/triangles.png";

        public static MemoryStream CreateGraphBanner(List<RankAtTime> data, long currentRank)
        {
            // setup
            data = data.TakeLast(10).ToList();
            if(data.Last().Rank != currentRank)
                data[9].Rank = currentRank;
            var background = Image.Load(new WebClient().DownloadData(BackgroundUrl));
            var font = SystemFonts.CreateFont("Arial", 15);
            var graph = new Image<Rgba32>(800, 300);
            var minRank = data.Min(x => x.Rank);
            var maxRank = data.Max(x => x.Rank);
            // draw background & text in the top left
            graph.Mutate(x =>
                x.DrawImage(background, 1)
                    .DrawText("Ranking Graph", font, Color.LightGray, new PointF(5, 5)));

            // convert rank data to y coord, x will be set later
            var points = data.Select(x => new PointF(0, CalculateY(x.Rank, minRank, maxRank, 40))).ToArray();

            for (var i = 0; i < data.Count; i++)
            {
                // copy to local variable because rider keeps yelling at me
                var localI = i;
                // set x position for ith ranking data
                var x = (float) GraphWidth / data.Count * localI;

                // draw text at the bottom with 25 offset because that's about half the text length
                graph.Mutate(g =>
                    g.DrawText(data[localI].Timestamp.ToString("dd/MM"), font, Color.White,
                        new PointF(x + 25, GraphHeight - 20)));
                // set set x coord for graph with slightly higher offset than the text
                points[i].X = x + 40;
            }

            // draw ranking numbers & dots for the graph 
            var j = 0;
            data.ForEach(x => graph.Mutate(g =>
            {
                g.DrawText($"#{x.Rank}", font, Color.White, points[j]);
                g.Fill(Color.White, new EllipsePolygon(points[j++], 3));
            }));

            // draw the actual graph
            graph.Mutate(x => x.DrawLines(Pens.Solid(Color.White, 1), points));

            // return the image
            var memStream = new MemoryStream();
            graph.Save(memStream, PngFormat.Instance);
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }

        // thanks twarq for this math bs 🙏
        private static float CalculateY(long yOld, long yMin, long yMax, int padding)
            => ((float) yOld - yMin) / ((float) yMax - yMin) * (GraphHeight - padding * 2) + padding;
    }
}
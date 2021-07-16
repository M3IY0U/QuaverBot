using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Extend;
using FFMpegCore.Pipes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using PointF = SixLabors.ImageSharp.PointF;

namespace QuaverBot.Graphics
{
    public class MapPreview
    {
        private const int Width = 400;
        private const int Height = 600;
        private const int Speed = 30;

        public static async Task RenderMap(string fileName, int id, double begin, int length)
        {
            var frames = new RawVideoPipeSource(GenerateFrames(fileName, begin, length)) {FrameRate = 30};
            await FFMpegArguments
                .FromPipeInput(frames, options => options
                    .UsingMultithreading(true))
                .OutputToFile($"{id}.mp4")
                .ProcessAsynchronously();
        }

        private static IEnumerable<IVideoFrame> GenerateFrames(string fileName, double begin, int length)
        {
            var result = new List<BitmapVideoFrameWrapper>();
            var objects = ParseMapFile(fileName).ToList();

            try
            {
                var end = Math.Abs(objects.Last().Y);
                begin = end * begin / 100d;

                for (var i = 0; i < end; i += 34)
                {
                    objects.ForEach(x => x.Update());
                    Console.WriteLine(i);
                    objects.RemoveAll(note => note.GetType() != typeof(Slider) && note.Y > Height);
                    objects.RemoveAll(note => note.GetType() == typeof(Slider) && ((Slider) note).EndTime > Height);
                    if (i > begin + length) break;
                    if (i < begin) continue;
                    using var img = new Image<Rgba32>(Width, Height);
                    foreach (var note in objects.Where(note => note.IsOnScreen()))
                    {
                        if (note.GetType() == typeof(Note))
                        {
                            img.Mutate(context => context.Fill(Color.Red, new EllipsePolygon(note.X, note.Y, 30)));
                        }
                        else
                        {
                            var slider = (Slider) note;
                            img.Mutate(context => context
                                .Fill(Color.Blue, new EllipsePolygon(slider.X, slider.Y, 30))
                                .Fill(Color.Blue, new EllipsePolygon(slider.X, slider.EndTime, 30))
                                .Fill(Color.Blue,
                                    new RectangularPolygon(new PointF(slider.X - 30, slider.EndTime),
                                        new PointF(slider.X + 30, slider.Y))));
                        }
                    }

                    result.Add(ImgToBmp(img));
                    Console.WriteLine($"Added img #{i}/{begin + length}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }

        private static BitmapVideoFrameWrapper ImgToBmp(Image input)
        {
            using var mem = new MemoryStream();
            var encoder = input.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
            input.Save(mem, encoder);
            mem.Seek(0, SeekOrigin.Begin);
            return new BitmapVideoFrameWrapper(new Bitmap(mem));
        }

        private static IEnumerable<Note> ParseMapFile(string fileName)
        {
            var result = new List<Note>();
            var file = File.ReadAllText(fileName);
            var read = file.Substring(file.IndexOf("HitObjects", StringComparison.Ordinal) + 10).Split('-').ToList();
            read.RemoveAt(0);

            foreach (var line in read)
            {
                var split = line.Split("\r\n").ToList();
                var y = split.Find(x => x.Contains("StartTime"))?.Split(": ")[1];
                var lane = split.Find(x => x.Contains("Lane"))?.Split(": ")[1];
                if (split.Exists(x => x.Contains("EndTime")))
                {
                    var end = split.Find(x => x.Contains("EndTime"))?.Split(": ")[1];
                    result.Add(new Slider(y, lane, end));
                }
                else
                {
                    result.Add(new Note(y, lane));
                }
            }

            return result;
        }

        private class Note
        {
            public long X { get; }
            public long Y { get; protected set; }

            public Note(string y, string lane)
            {
                Y = -Convert.ToInt32(y);
                X = lane switch
                {
                    "1" => 110,
                    "2" => 170,
                    "3" => 230,
                    "4" => 290,
                    _ => throw new Exception("was")
                };
            }

            public virtual void Update()
                => Y += Speed;

            public virtual bool IsOnScreen()
                => Y is >= 0 and <= Height;
        }

        private class Slider : Note
        {
            public long EndTime { get; private set; }

            public Slider(string y, string lane, string endTime) : base(y, lane)
            {
                EndTime = -Convert.ToInt32(endTime);
            }

            public override void Update()
            {
                Y += Speed;
                EndTime += Speed;
            }

            public override bool IsOnScreen()
                => Y >= 0 || EndTime >= 0;
        }
    }
}
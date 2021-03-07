using System.IO;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QuaverBot.Graphics
{
    public class CompareBanner
    {
        public static MemoryStream CreateBannerImage(string url1, string url2)
        {
            // create images
            using var client = new WebClient();
            var avatar1 = Image.Load(client.DownloadData(url1));
            var avatar2 = Image.Load(client.DownloadData(url2));
            var result = new Image<Rgba32>(250, 50);
            
            // resize avatars
            avatar1.Mutate(a=>a.Resize(50,50));
            avatar2.Mutate(a=>a.Resize(50,50));
            
            // draw avatars
            result.Mutate(r=> r.DrawImage(avatar1, 1));
            result.Mutate(r=> r.DrawImage(avatar2, new Point(200,0), 1));
            
            // return the image
            var memStream = new MemoryStream();
            result.Save(memStream, PngFormat.Instance);
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }
    }
}
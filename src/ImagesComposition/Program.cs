using System;
using System.Drawing;
using System.Linq;

namespace ImagesComposition {
    class Program {
        static void Main(string[] args) {
            var images = args.Select(x => (Bitmap)Bitmap.FromFile(x)).Take(args.Length - 1).ToArray();
            var outputFile = args[args.Length - 1];
            images[0] = Processor.SetImageOpacity(images[0], 0.5f);
            var result = Processor.CreateCompositeBitmap(images);
            result.Save(outputFile);
        }
    }
}

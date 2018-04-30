using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace ImagesComposition {
    public static class Processor {
        public static Bitmap SetImageOpacity(Bitmap image, float opacity) {
            Bitmap bmp = new Bitmap(image.Width, image.Height);

            //create a graphics object from the image
            using (Graphics gfx = Graphics.FromImage(bmp)) {

                //create a color matrix object
                ColorMatrix matrix = new ColorMatrix();

                //set the opacity
                matrix.Matrix33 = opacity;

                //create image attributes
                ImageAttributes attributes = new ImageAttributes();

                //set the color(opacity) of the image
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                //now draw the image
                gfx.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        public static Bitmap CreateCompositeBitmap(IEnumerable<Bitmap> bitmaps) {
            Bitmap[] bitmapArray = bitmaps.ToArray();
            var cancellationTokenSource = new CancellationTokenSource();

            // Compute the maximum width and height components of all
            // bitmaps in the collection.

            var minWidth = bitmaps.OrderBy(x => x.Width).First();
            var minHeight = bitmaps.OrderBy(x => x.Height).First();

            Rectangle largest = new Rectangle();
            // foreach (var bitmap in bitmapArray) {
            //     if (bitmap.Width > largest.Width)
            //         largest.Width = bitmap.Width;
            //     if (bitmap.Height > largest.Height)
            //         largest.Height = bitmap.Height;
            // }
            largest.Width = minWidth.Width;
            largest.Height = minHeight.Height;

            // Create a 32-bit Bitmap object with the greatest dimensions.
            Bitmap result = new Bitmap(largest.Width, largest.Height,
               PixelFormat.Format32bppArgb);

            // Lock the result Bitmap.
            var resultBitmapData = result.LockBits(
               new Rectangle(new Point(), result.Size), ImageLockMode.WriteOnly,
               result.PixelFormat);

            // Lock each source bitmap to create a parallel list of BitmapData objects.
            var bitmapDataList = (from bitmap in bitmapArray
                                  select bitmap.LockBits(
                                    new Rectangle(new Point(), bitmap.Size),
                                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
                                 .ToList();

            // Compute each column in parallel.
            Parallel.For(0, largest.Width, new ParallelOptions {
                CancellationToken = cancellationTokenSource.Token
            },
            i => {
                // Compute each row.
                for (int j = 0; j < largest.Height; j++) {
                    // Counts the number of bitmaps whose dimensions
                    // contain the current location.
                    int count = 0;

                    // The sum of all alpha, red, green, and blue components.
                    float a = 0, r = 0, g = 0, b = 0;

                    // For each bitmap, compute the sum of all color components.
                    foreach (var bitmapData in bitmapDataList) {
                        // Ensure that we stay within the bounds of the image.
                        if (bitmapData.Width > i && bitmapData.Height > j) {
                            unsafe {
                                byte* row = (byte*)(bitmapData.Scan0 + (j * bitmapData.Stride));
                                byte* pix = (byte*)(row + (4 * i));
                                a += *pix; pix++;
                                r += *pix; pix++;
                                g += *pix; pix++;
                                b += *pix;
                            }
                            count++;
                        }
                    }

                    unsafe {
                        // Compute the average of each color component.
                        a /= count;
                        r /= count;
                        g /= count;
                        b /= count;

                        // Set the result pixel.
                        byte* row = (byte*)(resultBitmapData.Scan0 + (j * resultBitmapData.Stride));
                        byte* pix = (byte*)(row + (4 * i));
                        *pix = (byte)a; pix++;
                        *pix = (byte)r; pix++;
                        *pix = (byte)g; pix++;
                        *pix = (byte)b;
                    }
                }
            });

            // Unlock the source bitmaps.
            for (int i = 0; i < bitmapArray.Length; i++) {
                bitmapArray[i].UnlockBits(bitmapDataList[i]);
            }

            // Unlock the result bitmap.
            result.UnlockBits(resultBitmapData);

            // Return the result.
            return result;
        }
    }
}
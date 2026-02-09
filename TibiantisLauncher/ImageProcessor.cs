using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace RelicHelper
{
    internal class ImageProcessor
    {
        internal struct ImageLocationOptions
        {
            public int? MatchLimit { get; set; } = null;
            public double MatchTreshold { get; set; } = 0.65;

            public ImageLocationOptions() { }
        };

        private Mat _experienceMat;
        private Mat[] _digitMats;

        public ImageProcessor() {
            _experienceMat = Properties.Resources.experience.ToMat();
            _digitMats= new Mat[10] {
                Properties.Resources.d0.ToMat(),
                Properties.Resources.d1.ToMat(),
                Properties.Resources.d2.ToMat(),
                Properties.Resources.d3.ToMat(),
                Properties.Resources.d4.ToMat(),
                Properties.Resources.d5.ToMat(),
                Properties.Resources.d6.ToMat(),
                Properties.Resources.d7.ToMat(),
                Properties.Resources.d8.ToMat(),
                Properties.Resources.d9.ToMat()
            };
        }

        public async Task<int?> ExtractExperiencePointsAsync(Bitmap sourceBitmap)
        {
            Mat sourceMat = sourceBitmap.ToMat();
            Point? expLabelLocation = await LocateSingleImage(sourceMat, _experienceMat);
            if (expLabelLocation == null)
                return null;

            //cut off label area and crop only experience points area
            var xpRect = new Rectangle(expLabelLocation.Value.X + 65, expLabelLocation.Value.Y, 70, 10);
            var xpBitmap = sourceBitmap.Clone(xpRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var xpMat = xpBitmap.ToMat();
            

            SortedDictionary<int, string> digits = new SortedDictionary<int, string>();

            for (int i = 0; i < _digitMats.Length; i++)
            {
                List<Point> digitLocations = await LocateImage(xpMat, _digitMats[i]);

                foreach (var point in digitLocations)
                {
                    digits.Add(point.X, i.ToString());
                }
            }

            string numberStr = string.Join(string.Empty, digits.Values.ToArray());
            
            if (int.TryParse(numberStr, out var number))
                return number;

            return null;
        }

        private static async Task<Point?> LocateSingleImage(Mat sourceMat, Mat probeMat, ImageLocationOptions? options = null)
        {
            options ??= new ImageLocationOptions();
            ImageLocationOptions _options = (ImageLocationOptions)options;
            _options.MatchLimit = 1;

            List<Point> result = await LocateImage(sourceMat, probeMat, _options);
            if (result.Count == 0)
                return null;

            return result[0];
        }

        private static async Task<List<Point>> LocateImage(Mat sourceMat, Mat probeMat, ImageLocationOptions? options = null)
        {
            options ??= new ImageLocationOptions();
            List<Point> resultList = new List<Point>();
            Mat resultMat = new Mat();

            await Task.Run(() => {
                CvInvoke.MatchTemplate(sourceMat, probeMat, resultMat, TemplateMatchingType.CcoeffNormed);
                
                int tries = 0;

                while(options.Value.MatchLimit == null || tries < options.Value.MatchLimit)
                {
                    double minVal = 0, maxVal = 0;
                    Point minLoc = new Point(), maxLoc = new Point();
                    CvInvoke.MinMaxLoc(resultMat, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    // No proper match found
                    if (maxVal <= options.Value.MatchTreshold)
                        break;

                    resultList.Add(maxLoc);

                    // Zero out the region around the found match to ignore it in the next iteration
                    CvInvoke.Rectangle(resultMat, new Rectangle(maxLoc, probeMat.Size), new MCvScalar(0), -1);

                    tries++;
                }
            });

            resultMat.Dispose();

            return resultList;
        }
    }
}

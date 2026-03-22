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

        public int GetMissilePixelCount(Bitmap centerArea)
        {
            using (Mat sourceMat = centerArea.ToMat())
            using (Mat hsvMat = new Mat())
            using (Mat mask = new Mat())
            {
                CvInvoke.CvtColor(sourceMat, hsvMat, ColorConversion.Bgr2Hsv);
                
                using (Mat maskPurple = new Mat())
                using (Mat maskBlue = new Mat())
                using (Mat maskRed1 = new Mat())
                using (Mat maskRed2 = new Mat())
                {
                    // Increased Saturation and Value thresholds to 120/120 to avoid floor/background noise
                    // SD Purple/Pink: H 135-165
                    CvInvoke.InRange(hsvMat, new ScalarArray(new MCvScalar(135, 120, 120)), new ScalarArray(new MCvScalar(165, 255, 255)), maskPurple);
                    // HMM/Explo Blue: H 90-135
                    CvInvoke.InRange(hsvMat, new ScalarArray(new MCvScalar(90, 120, 120)), new ScalarArray(new MCvScalar(135, 255, 255)), maskBlue);
                    // GFB Red: H 0-10 and 165-180
                    CvInvoke.InRange(hsvMat, new ScalarArray(new MCvScalar(0, 120, 120)), new ScalarArray(new MCvScalar(10, 255, 255)), maskRed1);
                    CvInvoke.InRange(hsvMat, new ScalarArray(new MCvScalar(165, 120, 120)), new ScalarArray(new MCvScalar(180, 255, 255)), maskRed2);
                    
                    CvInvoke.BitwiseOr(maskPurple, maskBlue, mask);
                    CvInvoke.BitwiseOr(mask, maskRed1, mask);
                    CvInvoke.BitwiseOr(mask, maskRed2, mask);
                    
                    return CvInvoke.CountNonZero(mask);
                }
            }
        }
        public bool DetectSpellText(Bitmap gameScreen)
        {
            // Tibia Golden/Yellow text for spells
            // We'll use a range to be more robust.
            // Typical color: (255, 255, 128) or similar.
            
            using (Mat sourceMat = gameScreen.ToMat())
            using (Mat hsvMat = new Mat())
            using (Mat mask = new Mat())
            {
                // Convert to HSV for better color detection
                CvInvoke.CvtColor(sourceMat, hsvMat, ColorConversion.Bgr2Hsv);
                
                // Yellow/Golden HSV range
                // Hue: 20-40 (Yellow/Orange)
                // Saturation: 100-255
                // Value: 150-255
                ScalarArray lower = new ScalarArray(new MCvScalar(20, 100, 150));
                ScalarArray upper = new ScalarArray(new MCvScalar(40, 255, 255));
                
                CvInvoke.InRange(hsvMat, lower, upper, mask);
                
                // Use Blobs/Contours to filter for the specific string shape.
                // 'exura res sio' is roughly 4-5 times wider than it is high.
                using (Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(mask, contours, null, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                    
                    for (int i = 0; i < contours.Size; i++)
                    {
                        Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                        
                        // Aspect ratio and size check (Width should be larger than height)
                        double ratio = (double)rect.Width / rect.Height;
                        
                        // Relaxed spell size heuristic: Width 20-200, Height 5-20
                        if (rect.Width > 20 && rect.Width < 200 && rect.Height > 5 && rect.Height < 20 && ratio > 1.2)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
        }
    }
}

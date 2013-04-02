using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using UniPhotoGallery.DomainModel;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.Services
{
    public interface IImageProcessingService
    {
        string GetPhotoFullPath(Photo photo, PhotoType photoType);
        int[] GetImageDimensions(Photo photo, PhotoType photoType);
        int[] GetImageDimensions(string fullImagePath);
        void ResizeImage(Photo photo, PhotoType fromType, PhotoType targetType);
        void ResizeImage(Photo photo, PhotoType fromType, PhotoType targetType, string savePath);
    }
    
    public class ImageProcessingService : IImageProcessingService
    {
        public IPhotoService PhotoService { get; set; }
        
        public string GetPhotoFullPath(Photo photo, PhotoType photoType)
        {
            if (photo == null || photoType == null) { throw new ArgumentException("Missing Photo or PhotoType"); }

            if (photo.PhotoTypes != null && photo.PhotoTypes.Any(f => f.SystemName.ToLower() == photoType.SystemName.ToLower()))
            {
                var typeFolder = photo.PhotoTypes.First(f => f.SystemName.ToLower() == photoType.SystemName.ToLower()).Directory;
                return HttpContext.Current.Server.MapPath(string.Format("{0}/{1}/{2}/{3}", photo.BasePhotoVirtualPath, photo.Owner.OwnerDirectory, typeFolder, photo.FileName));
            }

            throw new Exception(string.Format("Original photo not defined for Photo Id: {0}!", photo.PhotoId));
        }
        
        
        #region ImageDimensions
        public int[] GetImageDimensions(Photo photo, PhotoType photoType)
        {
            var fullPath = GetPhotoFullPath(photo, photoType);
            return GetImageDimensions(fullPath);
        }

        public int[] GetImageDimensions(string fullImagePath)
        {
            try
            {
                using (Image origImage = Image.FromFile(fullImagePath))
                {
                    var x = origImage.Width;
                    var y = origImage.Height;

                    return new [] { x, y };
                }
            }
            catch (Exception ex)
            {
                throw;
                //neni mozne nahrat obrazek z disku. 
                //jedna se vyznamnou vyjimku, posleme ji mailem:

                /*
                var messageBody = new StringBuilder();
                messageBody.AppendFormat("Load path: {0}", _loadPath);
                messageBody.Append(Environment.NewLine);
                messageBody.Append("Exception:");
                messageBody.Append(Environment.NewLine);
                messageBody.Append(ex);

                var message = new MailMessage("admin@bohemians1905.cz", "antonin.jelinek@gmail.com")
                {
                    Subject = "Chyba při čtení obrázku pro ImageDimension - VĎ",
                    Body = messageBody.ToString(),
                    BodyEncoding = Encoding.UTF8
                };

                Emails.EnqueuEmail(message);
                */
                //nechame ulozit k obrazku rozmery x = -1, y = -1
                //x = -1;
                //y = -1;
            }
        }


        #endregion

        public void ResizeImage(Photo photo, PhotoType fromType, PhotoType targetType)
        {
            if(PhotoTypeExist(photo, targetType))
            {
                var photoTypeFileName = GetPhotoFullPath(photo, targetType); //photo.GetPhotoUrl(targetType.SystemName);

                if(File.Exists(photoTypeFileName))
                {
                    return;    
                }
            }
            
            ResizeImage(photo, fromType, targetType, null);
        }

        private bool PhotoTypeExist(Photo photo, PhotoType photoType)
        {
            return photo.PhotoTypes.Any(tf => tf.PhotoTypeId == photoType.PhotoTypeId);
        }

        public void ResizeImage(Photo photo, PhotoType fromType, PhotoType targetType, string savePath)
        {
            var thumb = DoResizeImage(photo, fromType, targetType);

            if(string.IsNullOrEmpty(savePath))
            {
                SaveImage(thumb, photo, targetType);
            }
            else
            {
                SaveImage(thumb, savePath, photo.FileName);
            }
        }

        private Bitmap DoResizeImage(Photo photo, PhotoType fromType, PhotoType targetType)
        {
            var fullPath = GetPhotoFullPath(photo, fromType);
            var origImage = LoadImage(fullPath);

            int resX = targetType.X;
            int resY;
            float ratio;

            //TODO: tady je chyba!!!!!! + tahle metoda se vola divne moc casto ???

            if (targetType.Y.HasValue) //mam zadanou vysku
            {
                resY = targetType.Y.Value;
                
                if (resX == resY) //ctvercova transformace
                {
                    return MakeSquareTransfrom(resX, resY, origImage);
                }
            }

            //obrazek na sirku nebo ctverec:
            if (origImage.Width >= origImage.Height)
            {
                ratio = (float)origImage.Height / (float)origImage.Width;
                resY = Convert.ToInt32(resX * ratio);
            }
            else //obrazek na vysku. 
            {
                if(targetType.Y.HasValue) //mam zadanou vysku
                {
                    resY = targetType.Y.Value;                    
                    ratio = (float)origImage.Width / (float)origImage.Height;
                    resX = Convert.ToInt32(resY * ratio);
                }
                else //pokud nemam zadanou vysku, musim to resizovat podle zadane sirky
                {
                    ratio = (float)origImage.Height / (float)origImage.Width;
                    resY = Convert.ToInt32(resX * ratio);
                }
            }

            var thumbImage = new Bitmap(resX, resY, origImage.PixelFormat);
            Graphics g = Graphics.FromImage(thumbImage);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            var oRectangle = new Rectangle(0, 0, resX, resY);
            g.DrawImage(origImage, oRectangle);

            if (resX < origImage.Width)
            {
                var si = new SharpeningImage();
                si.Filter(thumbImage);
            }

            origImage.Dispose();

            return thumbImage;
        }

        private Bitmap MakeSquareTransfrom(int resX, int resY, Image origImage)
        {
            var squareThumbImage = new Bitmap(resX, resY, origImage.PixelFormat);
            Graphics gr = Graphics.FromImage(squareThumbImage);
            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gr.SmoothingMode = SmoothingMode.HighQuality;
            gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
            gr.CompositingQuality = CompositingQuality.HighQuality;

            float origSizeX = origImage.Width;
            float origSizeY = origImage.Height;

            if (origSizeX == origSizeY) //ctverec, jen zmensime
            {
                squareThumbImage = GetThumbImage(origImage, resX, resY);
            }

            if (origSizeX >= origSizeY) //na sirku - orizneme na kazde strane pulku rozdilu (sirka - vyska)
            {
                int rozdil = Convert.ToInt32(origSizeX - origSizeY);
                int rozdilPul = Convert.ToInt32(rozdil/2);

                gr.DrawImage(origImage, new Rectangle(0, 0, resX, resY), rozdilPul, 0, (origSizeX - rozdil), origSizeY,
                             GraphicsUnit.Pixel);
            }
            else // na vysku - orez se provadi v dolni casti
            {
                int rozdil = Convert.ToInt32(origSizeY - origSizeX);
                gr.DrawImage(origImage, new Rectangle(0, 0, resX, resY), 0, 0, origSizeX, (origSizeY - rozdil),
                             GraphicsUnit.Pixel);
            }

            var simRAC1 = new SharpeningImage();
            simRAC1.Filter(squareThumbImage);

            return squareThumbImage;
        }

        private Image LoadImage(string imagePath)
        {
            try
            {
                Image origImage = Image.FromFile(imagePath);
                return origImage;
            }
            catch (Exception ex)
            {
                throw new Exception("loadImage Error: " + ex.Message);
            }
        }


        private void SaveImage(Bitmap thumbImage, Photo photo, PhotoType targetType)
        {
            var targetTypeDirectory = HttpContext.Current.Server.MapPath(string.Format(@"{0}\{1}\{2}", photo.BasePhotoVirtualPath, photo.Owner.OwnerDirectory, targetType.Directory));
            
            SaveImage(thumbImage, targetTypeDirectory, photo.FileName);
        }

        private void SaveImage(Bitmap thumbImage, string targetDir, string targetFileName)
        {
            // create directory if it doesn't exist
            var di = new DirectoryInfo(targetDir);
            if (!di.Exists)
            {
                di.Create();
            }

            var fullSavePath = string.Format(@"{0}\{1}", targetDir, targetFileName);

            var fi = new FileInfo(fullSavePath);
            if (fi.Exists)
            {
                fi.Delete();
            }
            
            ImageCodecInfo[] info = ImageCodecInfo.GetImageEncoders();
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

            thumbImage.Save(fullSavePath, info[1], encoderParameters);
        }

        /*
        private static void ApplyTransform(string imageName, string imagePath, string transformType)
        {
            int resX = transformType.X;
            int resY = transformType.Y;
            string saveAsPath = transformType.SaveAsPath;

            Image origImage = loadImage(imageName, imagePath);
            float resYPom = ((float)origImage.Height / (float)origImage.Width);
            resYPom = resYPom * resX;

            if (resY == 0)
            {
                resY = Convert.ToInt32(resYPom);
            }

            Bitmap thumbImage;
            Graphics g;

            switch (transformType)
            {
                case "resizeByLongerSide":
                    float origResX = origImage.Width;
                    float origResY = origImage.Height;

                    if (origResX > origResY)
                    {
                        resY = Convert.ToInt32((origResY / origResX) * resX);
                    }
                    else
                    {
                        if (origResY > origResX)
                        {
                            resX = Convert.ToInt32((origResX / origResY) * resY);
                        }
                    }
                    thumbImage = GetThumbImage(origImage, resX, resY);

                    break;

                case "ResizeAndCrop":
                    thumbImage = new Bitmap(resX, resY, origImage.PixelFormat);
                    g = Graphics.FromImage(thumbImage);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    float origSizeX = origImage.Width;
                    float origSizeY = origImage.Height;

                    if (origSizeX == origSizeY) //ctverec, jen zmensime
                    {
                        thumbImage = GetThumbImage(origImage, resX, resY);
                    }

                    if (origSizeX >= origSizeY) //na sirku - orizneme na kazde strane pulku rozdilu (sirka - vyska)
                    {
                        int rozdil = Convert.ToInt32(origSizeX - origSizeY);
                        int rozdilPul = Convert.ToInt32(rozdil / 2);

                        g.DrawImage(origImage, new Rectangle(0, 0, resX, resY), rozdilPul, 0, (origSizeX - rozdil), origSizeY, GraphicsUnit.Pixel);
                    }
                    else // na vysku - orez se provadi v dolni casti
                    {
                        int rozdil = Convert.ToInt32(origSizeY - origSizeX);
                        g.DrawImage(origImage, new Rectangle(0, 0, resX, resY), 0, 0, origSizeX, (origSizeY - rozdil), GraphicsUnit.Pixel);
                    }

                    var simRAC1 = new SharpeningImage();
                    simRAC1.Filter(thumbImage);

                    break;
                default: //Zadne jmeno transformace nepasne, udela se jen resize
                    thumbImage = GetThumbImage(origImage, resX, resY);
                    break;
            }

            SaveImage(thumbImage, saveAsPath);

        }
        */

        private Bitmap GetThumbImage(Image origImage, int resX, int resY)
        {
            var thumbImage = new Bitmap(resX, resY, origImage.PixelFormat);
            var g = Graphics.FromImage(thumbImage);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            var oRectangle = new Rectangle(0, 0, resX, resY);
            g.DrawImage(origImage, oRectangle);

            if (resX < origImage.Width)
            {
                var si = new SharpeningImage();
                si.Filter(thumbImage);
            }

            return thumbImage;
        }
    }

    public class SharpMatrix
    {
        public int TopLeft = 0, TopRight = 0, BottomLeft = 0, BottomRight = 0;
        public int TopMid = -2, MidLeft = -2, MidRight = -2, BottomMid = -2;
        public int Pixel = 20;
        public int Factor = 12;
        public int Offset = 0;
    }

    public class SharpeningImage
    {
        public bool Filter(Bitmap b)
        {
            var m = new SharpMatrix();
            return Conv3x3(b, m);
        }
        public static bool Conv3x3(Bitmap b, SharpMatrix m)
        {
            // Avoid divide by zero errors
            if (0 == m.Factor)
                return false; Bitmap

            // GDI+ still lies to us - the return format is BGR, NOT RGB. 
            bSrc = (Bitmap)b.Clone();
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                                                    ImageLockMode.ReadWrite,
                                                    PixelFormat.Format24bppRgb);
            BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height),
                                                 ImageLockMode.ReadWrite,
                                                 PixelFormat.Format24bppRgb);
            int stride = bmData.Stride;
            int stride2 = stride * 2;

            System.IntPtr Scan0 = bmData.Scan0;
            System.IntPtr SrcScan0 = bmSrc.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                byte* pSrc = (byte*)(void*)SrcScan0;
                int nOffset = stride - b.Width * 3;
                int nWidth = b.Width - 2;
                int nHeight = b.Height - 2;

                int nPixel;

                for (int y = 0; y < nHeight; ++y)
                {
                    for (int x = 0; x < nWidth; ++x)
                    {
                        nPixel = ((((pSrc[2] * m.TopLeft) +
                                (pSrc[5] * m.TopMid) +
                                (pSrc[8] * m.TopRight) +
                                (pSrc[2 + stride] * m.MidLeft) +
                                (pSrc[5 + stride] * m.Pixel) +
                                (pSrc[8 + stride] * m.MidRight) +
                                (pSrc[2 + stride2] * m.BottomLeft) +
                                (pSrc[5 + stride2] * m.BottomMid) +
                                (pSrc[8 + stride2] * m.BottomRight))
                                / m.Factor) + m.Offset);

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;
                        p[5 + stride] = (byte)nPixel;

                        nPixel = ((((pSrc[1] * m.TopLeft) +
                                (pSrc[4] * m.TopMid) +
                                (pSrc[7] * m.TopRight) +
                                (pSrc[1 + stride] * m.MidLeft) +
                                (pSrc[4 + stride] * m.Pixel) +
                                (pSrc[7 + stride] * m.MidRight) +
                                (pSrc[1 + stride2] * m.BottomLeft) +
                                (pSrc[4 + stride2] * m.BottomMid) +
                                (pSrc[7 + stride2] * m.BottomRight))
                                / m.Factor) + m.Offset);

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;
                        p[4 + stride] = (byte)nPixel;

                        nPixel = ((((pSrc[0] * m.TopLeft) +
                                                     (pSrc[3] * m.TopMid) +
                                                     (pSrc[6] * m.TopRight) +
                                                     (pSrc[0 + stride] * m.MidLeft) +
                                                     (pSrc[3 + stride] * m.Pixel) +
                                                     (pSrc[6 + stride] * m.MidRight) +
                                                     (pSrc[0 + stride2] * m.BottomLeft) +
                                                     (pSrc[3 + stride2] * m.BottomMid) +
                                                     (pSrc[6 + stride2] * m.BottomRight))
                                / m.Factor) + m.Offset);

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;
                        p[3 + stride] = (byte)nPixel;

                        p += 3;
                        pSrc += 3;
                    }

                    p += nOffset;
                    pSrc += nOffset;
                }
            }

            b.UnlockBits(bmData);
            bSrc.UnlockBits(bmSrc);
            return true;
        }
    }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using com.google.zxing;
using com.google.zxing.qrcode.decoder;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 图片编码处理类
    /// </summary>
    public class QREncodeHelper
    {
        static string[] legalExts = { ".jpg", ".png", ".bmp" };
        private static double midImgScale = 1 / 3.5;

        public static string CreateQRCodePic(string info, string midImgPath, string saveDir, string fileName, int width = 300, int height = 300)
        {
            return CreateQRCodePic(info, midImgPath, saveDir, fileName, ErrorCorrectionLevel.M, width, height);
        }

        public static string CreateQRCodePic(string info, string saveDir, string fileName, ErrorCorrectionLevel ecLevel, int width = 300, int height = 300)
        {
            return CreateQRCodePic(info, null, saveDir, fileName, ecLevel, width, height);
        }

        public static string CreateQRCodePic(string info,string saveDir,string fileName,int width = 300, int height = 300)
        {
            return CreateQRCodePic(info,saveDir,fileName, ErrorCorrectionLevel.M, width, height);
        }

        public static string CreateQRCodePic(string info, string midImgPath, string saveDir, string fileName, ErrorCorrectionLevel ecLevel, int width = 300, int height = 300)
        {
            string QRImgPath = string.Empty;
            if (string.IsNullOrEmpty(info))//要存储的信息不能为空，否则异常
                return QRImgPath;
            try
            {
                MultiFormatWriter writer = new MultiFormatWriter();
                //构造创建参数；
                Hashtable hints = new Hashtable();
                hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");
                hints.Add(EncodeHintType.ERROR_CORRECTION, ecLevel);
                Bitmap QRImg = writer.encode(info, BarcodeFormat.QR_CODE, width, height, hints).ToBitmap();//生成不带图片的BitMap;
                Image midImg = null;
                if (!string.IsNullOrEmpty(midImgPath) && File.Exists(midImgPath) && legalExts.Contains(Path.GetExtension(midImgPath)))//验证图片存在且后缀名符合
                {
                    try//确保即使中间图片加载失败也能生成无中间图的QR码
                    {
                        midImg = Image.FromFile(midImgPath);
                    }
                    catch { }
                }

                Bitmap bmpimg = new Bitmap(QRImg.Width, QRImg.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmpimg))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.DrawImage(QRImg, 0, 0);
                }
                if (midImg != null)
                {
                    Size QRSize = writer.GetEncodeSize(info, BarcodeFormat.QR_CODE, width, height);
                    //计算中间图片大小和位置
                    int midImgW = Math.Min((int)(QRSize.Width * midImgScale), midImg.Width);
                    int midImgH = Math.Min((int)(QRSize.Height * midImgScale), midImg.Height);
                    int midImgL = (QRImg.Width - midImgW) / 2;
                    int midImgT = (QRImg.Height - midImgH) / 2;
                    Graphics myGraphic = System.Drawing.Graphics.FromImage(bmpimg);
                    myGraphic.FillRectangle(Brushes.White, midImgL, midImgT, midImgW, midImgH);
                    myGraphic.DrawImage(midImg, midImgL, midImgT, midImgW, midImgH);
                    myGraphic.Dispose();
                    midImg.Dispose();
                }
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);
                QRImgPath = Path.Combine(saveDir, fileName + ".jpg");
                bmpimg.Save(QRImgPath, ImageFormat.Jpeg);
            }
            catch (Exception e){ 
                
            }
            return QRImgPath;
        }
    }
}
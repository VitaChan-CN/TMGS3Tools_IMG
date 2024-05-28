using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TMGS3Tools_IMG.Util;

namespace TMGS3Tools_IMG
{
    public class MainMethod
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path_input"></param>
        /// <param name="path_output"></param>
        /// <param name="path_org"></param>
        public void Import2(string path_input_fileOrfolder, string path_output_filename, string path_org_filename, string path_pngquant)
        {
            var bytes_org = File.ReadAllBytes(path_org_filename);
            byte[] fontbytes = File.ReadAllBytes(path_org_filename);
            StructReader Reader0 = new StructReader(new MemoryStream(fontbytes));
            var Header = new PIM2HEADER();
            try
            {
                Reader0.ReadStruct(ref Header);
                if (!Header.Signature.StartsWith("PIM2"))
                {
                    Console.WriteLine("非PIM2图片");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            Rect[] rects = new Rect[] { };
            if (Header.BUW == "BUW\0")
            {
                uint count = Reader0.ReadUInt32();
                rects = new Rect[count];
                var rect = new Rect();
                for (int i = 0; i < count; i++)
                {
                    Reader0.ReadStruct(ref rect);
                    rects[i] = rect;
                }
            }

            int width = Header.Width;
            int height = Header.Heigth;
            var ms = new MemoryStream(fontbytes);
            ms.Seek(16 + Header.HeaderByteLength, SeekOrigin.Begin);
            StructReader Reader = new StructReader(ms);
            var list = new Queue<Byte>();
            var list2 = new List<Byte>();

            //int countcp = Header.ColorPanelCount / 256;
            //List<Color>[] colors = new List<Color>[countcp];

            Dictionary<int, Color[]> colors_dic = new Dictionary<int, Color[]>();


            if (Header.ColorPanelCount == 16)
            {
                Console.WriteLine("16Bit 请用Export[可能是未实现方法]");
                return;
            }
            else
            {

                //单个图片没BUW的
                if (rects.Length == 0)
                {
                    rects = new Rect[1];
                    rects[0] = new Rect();
                    rects[0].h = Header.Heigth;
                    rects[0].w = Header.Width;
                    rects[0].color_index = 0;
                    rects[0].colorcount = (byte)(Header.ColorPanelCount - 1);
                    rects[0].x = 0;
                    rects[0].y = 0;
                    if (Header.ColorPanelCount == 256)
                    {
                        rects[0].type = 0x0305;
                    }
                    else if (Header.ColorPanelCount == 16)
                    {
                        rects[0].type = 0x0304;
                    }
                }

                for (int k = 0; k < Header.PixelByteLength; k++)
                {
                    list.Enqueue(Reader.ReadByte());
                }
                for (int i = 0; i < rects.Length; i++)
                {
                    if (!colors_dic.ContainsKey(rects[i].color_index))
                        colors_dic.Add(rects[i].color_index, new Color[rects[i].colorcount + 1]);
                }
            }

            //导入
            var isDir = IsDir(path_input_fileOrfolder);
            if (isDir) //输入的是文件夹
            {
                Dictionary<Point, byte> dicPic = new Dictionary<Point, byte>();
                for (int i = 0; i < Header.Heigth; i++)
                {
                    for (int j = 0; j < Header.Width; j++)
                    {
                        Point point = new Point(j, i);
                        dicPic.Add(point, 0);
                    }
                }
                DirectoryInfo dir = new DirectoryInfo(path_input_fileOrfolder);
                //var files = dir.GetFiles("*.png");
                foreach (var item in colors_dic)//for (int i = 0; i < countcp; i++)
                {
                    int colortype = colors_dic[item.Key].Length;

                    //int index = item.Key >> 4;
                    //int index_sub = item.Key & 0x00f;

                    string name = Path.Combine(path_input_fileOrfolder, $"{string.Format("{0:X4}", item.Key)}_{colortype}.png");
                    string name1 = Path.Combine(path_input_fileOrfolder, $"{string.Format("{0:X4}", item.Key)}_{colortype}_1.png");
                    Console.WriteLine("Reading:" + name);
                    if (File.Exists(name))
                    {
                        var img = new Bitmap(name);
                        var img1 = new Bitmap(name);//16bit的第二张图
                        if (File.Exists(name1))
                        {
                            img1 = new Bitmap(name1);
                            Console.WriteLine("Reading:" + name1);
                            var cp1 = CheckColor(img);
                            var cp2 = CheckColor(img1);
                            List<Color> cpMerged = cp1.Union(cp2).ToList();

                            if(cpMerged.Count > 16)
                            {
                                Console.WriteLine("2个16色图片检测:两个图片合成一个img img1");
                                Bitmap mergedImage = new Bitmap(img.Width + img1.Width, Math.Max(img.Height, img1.Height));
                                using (Graphics g = Graphics.FromImage(mergedImage))
                                {
                                    g.DrawImage(img, new Point(0, 0));
                                    g.DrawImage(img1, new Point(img1.Width, 0));
                                }
                                //保存合并后的图像
                                mergedImage.Save("merged_img.png");
                                //img.Dispose();
                                //img1.Dispose();
                                mergedImage.Dispose();

                                //降低颜色
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    var psi = new ProcessStartInfo(path_pngquant) { RedirectStandardOutput = true };
                                    psi.Arguments = $" 15 merged_img.png --posterize 4 -o merged_img_16.png ";
                                    var proc = Process.Start(psi);
                                    if (proc == null)
                                    {
                                        Console.WriteLine("Can not exec.");
                                    }
                                    else
                                    {
                                        using (var sr = proc.StandardOutput)
                                        {
                                            while (!sr.EndOfStream)
                                            {
                                                Console.WriteLine(sr.ReadLine());
                                            }

                                            if (!proc.HasExited)
                                            {
                                                proc.Kill();
                                            }
                                        }
                                    }
                                }
                                var mergedImage_16 = System.Drawing.Image.FromFile("merged_img_16.png");
                                Rectangle sourceRect = new Rectangle(0, 0, img.Width, img.Height);
                                Rectangle sourceRect1 = new Rectangle(img.Width, 0, img1.Width, img1.Height);
                                Bitmap img_new  = new Bitmap(img.Width, img.Height);
                                Bitmap img1_new = new Bitmap(img1.Width, img1.Height);
                                using (Graphics g = Graphics.FromImage(img_new))
                                {
                                    // 将图像的矩形区域绘制到新位图上
                                    g.DrawImage(mergedImage_16, new Rectangle(0, 0, img_new.Width, img_new.Height), sourceRect, GraphicsUnit.Pixel);
                                }
                                using (Graphics g = Graphics.FromImage(img1_new))
                                {
                                    // 将图像的矩形区域绘制到新位图上
                                    g.DrawImage(mergedImage_16, new Rectangle(0, 0, img1_new.Width, img1_new.Height), sourceRect1, GraphicsUnit.Pixel);
                                }
                                img_new.Save("merged_img_16_splitimg.png");
                                img_new.Save("merged_img_16_splitimg1.png");
                                // 释放资源
                                //img_new.Dispose();
                                //img1_new.Dispose();
                                mergedImage_16.Dispose();

                                img = img_new;
                                img1 = img1_new;//16bit的第二张图
                                File.Delete("merged_img.png");
                                File.Delete("merged_img_16.png");
                                File.Delete("merged_img_16_splitimg.png");
                                File.Delete("merged_img_16_splitimg1.png");
                            }
                            else
                            {
                                Console.WriteLine("两个图不需要重新生成颜色表");
                            }
                        }

                        List<Color> listcp = new List<Color>();
                        for (int y = 0; y < img.Height; y++)
                        {
                            for (int x = 0; x < img.Width; x++)
                            {
                                var color = img.GetPixel(x, y);
                                //统一透明色W
                                if (color.A == 0)
                                {
                                    color = Color.FromArgb(0, 0, 0, 0);
                                }
                                if (!listcp.Contains(color))
                                {
                                    listcp.Add(color);
                                }
                            }
                        }

                        //颜色版 透明色统一问题
                        //List<Color> listcpClean = new List<Color>();
                        //listcpClean.Add(Color.FromArgb(0, 255, 255, 255));
                        //for (int i = 0; i < listcp.Count; i++)
                        //{
                        //    if(listcp[0].A == Color.FromArgb(0, 0, 0, 0).A)
                        //    {
                        //        //A=0
                        //    }
                        //    else
                        //    {
                        //        listcpClean.Add(listcp[i]);
                        //    }
                        //}
                        //listcp = listcpClean;

                        //if (listcp[0].A == Color.FromArgb(0, 0, 0, 0).A)
                        //{
                        //    listcp.RemoveAt(0);
                        //}

                        int colorcheck = 256;
                        if (listcp.Count >= 256 && colortype == 256)
                        {
                            colorcheck = 256;
                            Console.WriteLine($"Over {colorcheck} Color!!" + name);
                            if (File.Exists(path_pngquant))
                            {
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    var psi = new ProcessStartInfo(path_pngquant) { RedirectStandardOutput = true };
                                    psi.Arguments = " 255 " + name + " --ext .tmp ";
                                    var proc = Process.Start(psi);
                                    if (proc == null)
                                    {
                                        Console.WriteLine("Can not exec.");
                                    }
                                    else
                                    {
                                        using (var sr = proc.StandardOutput)
                                        {
                                            while (!sr.EndOfStream)
                                            {
                                                Console.WriteLine(sr.ReadLine());
                                            }

                                            if (!proc.HasExited)
                                            {
                                                proc.Kill();
                                            }
                                        }
                                    }

                                    string pathtmp = name.Replace(".png", ".tmp");
                                    img = new Bitmap(new MemoryStream(File.ReadAllBytes(pathtmp)));
                                    listcp = new List<Color>();
                                    for (int y = 0; y < img.Height; y++)
                                    {
                                        for (int x = 0; x < img.Width; x++)
                                        {
                                            var color = img.GetPixel(x, y);
                                            //统一透明色
                                            if(color.A == 0)
                                            {
                                                color = Color.FromArgb(0, 0, 0, 0);
                                            }
                                            if (!listcp.Contains(color))
                                            {
                                                listcp.Add(color);
                                            }
                                        }
                                    }
                                    for (int i = 0; i < listcp.Count; i++)
                                    {
                                        colors_dic[item.Key][i] = listcp[i];
                                    }
                                    File.Delete(pathtmp);
                                }
                            }

                        }
                        else if (listcp.Count >= 16 && colortype == 16)
                        {
                            colorcheck = 16;
                            Console.WriteLine($"Over {colorcheck} Color!!" + name);
                            if (File.Exists(path_pngquant))
                            {
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    var psi = new ProcessStartInfo(path_pngquant) { RedirectStandardOutput = true };
                                    psi.Arguments = $" 15 {name} --posterize 4  --ext .tmp  ";
                                    var proc = Process.Start(psi);
                                    if (proc == null)
                                    {
                                        Console.WriteLine("Can not exec.");
                                    }
                                    else
                                    {
                                        using (var sr = proc.StandardOutput)
                                        {
                                            while (!sr.EndOfStream)
                                            {
                                                Console.WriteLine(sr.ReadLine());
                                            }

                                            if (!proc.HasExited)
                                            {
                                                proc.Kill();
                                            }
                                        }
                                    }

                                    string pathtmp = name.Replace(".png", ".tmp");
                                    img = new Bitmap(new MemoryStream(File.ReadAllBytes(pathtmp)));
                                    listcp = new List<Color>();
                                    for (int y = 0; y < img.Height; y++)
                                    {
                                        for (int x = 0; x < img.Width; x++)
                                        {
                                            var color = img.GetPixel(x, y);
                                            if (color.A == 0)
                                            {
                                                color = Color.FromArgb(0, 0, 0, 0);
                                            }
                                            if (!listcp.Contains(color))
                                            {
                                                listcp.Add(color);
                                            }
                                        }
                                    }
                                    for (int i = 0; i < listcp.Count; i++)
                                    {
                                        colors_dic[item.Key][i] = listcp[i];
                                    }

                                    File.Delete(pathtmp);
                                }
                            }
                        }
                        else if (colortype == 16 || colortype == 256)
                        {
                            //16_1 check 16色第二个检查
                            if (colortype == 16 && listcp.Count != 16)
                            {
                                for (int y = 0; y < img1.Height; y++)
                                {
                                    for (int x = 0; x < img1.Width; x++)
                                    {
                                        var color = img1.GetPixel(x, y);
                                        if (color.A == 0)
                                        {
                                            color = Color.FromArgb(0, 0, 0, 0);
                                        }
                                        if (!listcp.Contains(color))
                                        {
                                            listcp.Add(color);
                                        }
                                    }
                                }
                                //if (listcp[0].A == Color.FromArgb(0, 0, 0, 0).A)
                                //{
                                //    listcp.RemoveAt(0);
                                //}

                                if (listcp.Count > 16)
                                {
                                    Console.WriteLine($"===============================================================");
                                    Console.WriteLine($"[错误！！图片大于16色]listcp Color Count " + listcp.Count());
                                    Console.WriteLine($"请把{name}和{name1}的颜色控制在16色以及以内,两个加起来不能大于16色");
                                    Console.WriteLine($"===============================================================");
                                }
                            }

                            Console.WriteLine($"listcp Color Count " + listcp.Count());
                            for (int i = 0; i < listcp.Count; i++)
                            {
                                colors_dic[item.Key][i] = listcp[i];
                            }
                        }
                        else
                        {
                            Console.WriteLine("colortype未实现方法:" + colortype);
                        }

                        for (int y1 = 0; y1 < img.Height; y1++)
                        {
                            for (int x1 = 0; x1 < img.Width; x1++)
                            {
                                var color = img.GetPixel(x1, y1);
                                if (color.A == 0)
                                {
                                    color = Color.FromArgb(0, 0, 0, 0);
                                }
                                if (color.A != 0)
                                {
                                    int colorindex = listcp.IndexOf(color);
                                    if (colorindex >= 0)
                                    {
                                        Point point = new Point(x1, y1);
                                        if (colortype == 256)
                                        {
                                            if (dicPic.ContainsKey(point))
                                            {
                                                dicPic[point] = (byte)colorindex;
                                            }
                                            else
                                            {
                                                dicPic.Add(point, (byte)colorindex);
                                            }
                                        }
                                        else if (colortype == 16)
                                        {
                                            //int index = item.Key >> 4;
                                            //int index_sub = item.Key & 0x00f;

                                            //int bit4 = b;
                                            //if (index_sub == 0)
                                            //{
                                            //    bit4 = bit4 >> 0;
                                            //}
                                            //else if (index_sub == 1)
                                            //{
                                            //    bit4 = bit4 >> 4;
                                            //}
                                            int b = colorindex;
                                            //if (index_sub == 0)
                                            //{
                                            //    //b = b & 0x0f;
                                            //}
                                            //else if (index_sub == 1)
                                            //{
                                            //    b = b << 4;
                                            //    //b = b & 0xf0;
                                            //}

                                            if (dicPic.ContainsKey(point))
                                            {
                                                dicPic[point] += (byte)b;
                                            }
                                            else
                                            {
                                                dicPic.Add(point, (byte)b);
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("colortype未实现方法:" + colortype);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"{name}图片颜色错误:{x1},{y1},{color}！");
                                        Console.ReadLine();
                                    }
                                }
                            }
                        }

                        if (colortype == 16)
                        {
                            for (int y1 = 0; y1 < img1.Height; y1++)
                            {
                                for (int x1 = 0; x1 < img1.Width; x1++)
                                {
                                    var color = img1.GetPixel(x1, y1);
                                    if (color.A == 0)
                                    {
                                        color = Color.FromArgb(0, 0, 0, 0);
                                    }
                                    if (color.A != 0)
                                    {
                                        int colorindex = listcp.IndexOf(color);
                                        if (colorindex >= 0)
                                        {
                                            Point point = new Point(x1, y1);
                                            if (colortype == 16)
                                            {

                                                int b = colorindex;
                                                b = b << 4;
                                                //if (index_sub == 0)
                                                //{
                                                //    //b = b & 0x0f;
                                                //}
                                                //else if (index_sub == 1)
                                                //{
                                                //    b = b << 4;
                                                //    //b = b & 0xf0;
                                                //}

                                                if (dicPic.ContainsKey(point))
                                                {
                                                    dicPic[point] += (byte)b;
                                                }
                                                else
                                                {
                                                    dicPic.Add(point, (byte)b);
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("colortype未实现方法:" + colortype);
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"{name}图片颜色错误:{x1},{y1},{color}！");
                                            Console.ReadLine();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(name + "文件不存在!!!");
                    }
                }

                //List<byte> bytes = new List<byte>();
                //for (int p = 0; p < rects.Length; p++)
                //{
                //    var rect = rects[p];
                //    //bytes.Clear();
                //    // 
                //    string name = Path.Combine(path_input_fileOrfolder, $"{string.Format("{0:D4}", rect.index)}.png");
                //    if (File.Exists(name)){
                //        //png to 256 color
                //        var img = new Bitmap(name);
                //    }
                //    else
                //    {
                //        Console.WriteLine($"{name}文件不存在！");
                //        Console.ReadLine();
                //    }
                //}


                int xpart_pixel = 16;
                int ypart_pixel = 8;
                int xpart_count = width / xpart_pixel;
                int ypart_count = height / ypart_pixel;
                //font split 512/8 part y
                for (int y = 0; y < ypart_count; y++)
                {
                    //font split 16 part x
                    for (int x = 0; x < xpart_count; x++)
                    {
                        for (int y1 = 0; y1 < 8; y1++)
                        {
                            for (int x1 = 0; x1 < (width / xpart_count); x1++)
                            {
                                byte index = dicPic[new Point(x * (width / xpart_count) + x1, y * 8 + y1)];
                                list2.Add(index);
                            }
                        }
                    }
                }


                //list2
                if (list.Count != list2.Count)
                {
                    Console.WriteLine("原像素个数和新像素个数不符合");
                    Console.ReadLine();
                }
                for (int i = 0; i < list2.Count; i++)
                {
                    bytes_org[16 + Header.HeaderByteLength + i] = list2[i];
                }
                List<byte> bytecpall = new List<byte>();
                //todo
                // for (int i = 0; i < countcp; i++)
                foreach (var item in colors_dic)
                {
                    var li = item.Value;
                    for (int j = 0; j < item.Value.Length; j++)
                    {
                        Color color = Color.FromArgb(0, 0, 0, 0);
                        if (j < li.Length - 1)
                        {
                            color = li[j];
                        }
                        bytecpall.Add(color.R);
                        bytecpall.Add(color.G);
                        bytecpall.Add(color.B);
                        bytecpall.Add(color.A);
                    }
                }
                for (int i = 0; i < bytecpall.Count; i++)
                {
                    bytes_org[16 + Header.HeaderByteLength + list2.Count + i] = bytecpall[i];
                }
                Console.WriteLine("输出成功:" + path_output_filename);
                File.WriteAllBytes(path_output_filename, bytes_org);

            }
            else //输入的是文件
            {
                Console.WriteLine("请输入文件夹");
            }
        }

        public void Export2(string path, string path_output)
        {
            FileInfo dinfo = new FileInfo(path_output);
            string pathbase = "";
            byte[] fontbytes = File.ReadAllBytes(path);
            StructReader Reader0 = new StructReader(new MemoryStream(fontbytes));
            var Header = new PIM2HEADER();
            try
            {
                Reader0.ReadStruct(ref Header);
                if (!Header.Signature.StartsWith("PIM2"))
                {
                    Console.WriteLine("非PIM2图片");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            Rect[] rects = new Rect[] { };
            if (Header.BUW == "BUW\0")
            {
                uint count = Reader0.ReadUInt32();
                rects = new Rect[count];
                var rect = new Rect();
                var byte2 = new byte[2];
                for (int i = 0; i < count; i++)
                {
                    Reader0.ReadStruct(ref rect);
                    rects[i] = rect;
                }
            }
            if (rects.Length == 0)
            {
                rects = new Rect[1];
                rects[0] = new Rect();
                rects[0].h = Header.Heigth;
                rects[0].w = Header.Width;
                rects[0].color_index = 0;
                rects[0].colorcount = (byte)(Header.ColorPanelCount - 1);
                rects[0].x = 0;
                rects[0].y = 0;
                if (Header.ColorPanelCount == 256)
                {
                    rects[0].type = 0x0305;
                }
                else if (Header.ColorPanelCount == 16)
                {
                    rects[0].type = 0x0304;
                }
            }


            int width = Header.Width;
            int height = Header.Heigth;

            var ms = new MemoryStream(fontbytes);
            ms.Seek(16 + Header.HeaderByteLength, SeekOrigin.Begin);
            StructReader Reader = new StructReader(ms);
            var list = new Queue<Byte>();

            Dictionary<int, Color[]> colors_dic = new Dictionary<int, Color[]>();
            //List<Color[]> colors = new List<Color[]>();
            //int indexmax = rects.Select(x => x.color_index).Max();
            //var indexedPicture = new Bitmap(width, height, PixelFormat.Format8bppIndexed);


            if (Header.ColorPanelCount == 16)
            {
                Export(path, path_output);
            }
            else
            {
                for (int k = 0; k < Header.PixelByteLength; k++)
                {
                    list.Enqueue(Reader.ReadByte());
                }
                for (int i = 0; i < rects.Length; i++)
                {
                    if (!colors_dic.ContainsKey(rects[i].color_index))
                        colors_dic.Add(rects[i].color_index, new Color[rects[i].colorcount + 1]);
                }
                foreach (var item in colors_dic)
                {
                    for (int j = 0; j < item.Value.Length; j++)
                    {
                        int R = Reader.ReadByte();
                        int G = Reader.ReadByte();
                        int B = Reader.ReadByte();
                        int A = Reader.ReadByte();
                        item.Value[j] = Color.FromArgb(A, R, G, B);
                    }
                }

                //获取像素分块
                Dictionary<Point, byte> dicPic = new Dictionary<Point, byte>();
                int xpart_pixel = 16;
                int ypart_pixel = 8;
                int xpart_count = width / xpart_pixel;
                int ypart_count = height / ypart_pixel;
                //font split 512/8 part y
                for (int y = 0; y < ypart_count; y++)
                {
                    //font split 16 part x
                    for (int x = 0; x < xpart_count; x++)
                    {
                        for (int y1 = 0; y1 < 8; y1++)
                        {
                            for (int x1 = 0; x1 < (width / xpart_count); x1++)
                            {
                                byte index = list.Dequeue();
                                dicPic.Add(new Point(x * (width / xpart_count) + x1, y * 8 + y1), index);
                            }
                        }
                    }
                }

                //颜色样本测试输出
                int count = colors_dic.Count;
                pathbase = Path.Combine(dinfo.Directory.FullName, dinfo.Name) + "_PNGSampleColorTest";
                //Bitmap[] bitmaps = new Bitmap[count];//(width, height, PixelFormat.Format32bppArgb)
                Dictionary<int, Bitmap[]> bitmaps = new Dictionary<int, Bitmap[]>();
                foreach (var item in colors_dic)
                {
                    if (item.Value.Length == 16)
                    {
                        bitmaps.Add(item.Key, new Bitmap[] { new Bitmap(width, height, PixelFormat.Format32bppArgb), new Bitmap(width, height, PixelFormat.Format32bppArgb) });
                    }
                    else
                    {
                        bitmaps.Add(item.Key, new Bitmap[] { new Bitmap(width, height, PixelFormat.Format32bppArgb) });
                    }

                }
                foreach (var item in bitmaps)
                {
                    for (int index = 0; index < item.Value.Length; index++)
                    {
                        int colortype = colors_dic[item.Key].Length;

                        //int index = item.Key >> 4;
                        //int index_sub = item.Key & 0x00f;

                        //item.Value = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                byte b = dicPic[new Point(j, i)];
                                Color color = Color.FromArgb(0, 0, 0, 0);
                                if (colortype == 256)
                                {
                                    //256
                                    color = colors_dic[item.Key][b];
                                    bitmaps[item.Key][0].SetPixel(j, i, color);
                                }
                                else if (colortype == 16)
                                {
                                    //bit4_type
                                    //16
                                    int bit4 = b;
                                    if (index == 0)
                                    {
                                        bit4 = bit4 >> 0;
                                    }
                                    else if (index == 1)
                                    {
                                        bit4 = bit4 >> 4;
                                    }
                                    bit4 = bit4 & 0x000F;
                                    color = colors_dic[item.Key][bit4];
                                    bitmaps[item.Key][index].SetPixel(j, i, color);
                                }

                            }
                        }
                        string name = Path.Combine(pathbase, $"{string.Format("{0:X4}", item.Key)}.png");
                        if (!Directory.Exists(pathbase))
                        {
                            Directory.CreateDirectory(pathbase);
                        }
                        Console.WriteLine("Export:" + name);
                        #region DEBUG
                        if (bitmaps[item.Key].Length == 1)
                        {
                            bitmaps[item.Key][0].Save(name, ImageFormat.Png);
                        }
                        else if (bitmaps[item.Key].Length == 2)
                        {
                            name = Path.Combine(pathbase, $"{string.Format("{0:X4}", item.Key)}.png");
                            bitmaps[item.Key][0].Save(name, ImageFormat.Png);
                            name = Path.Combine(pathbase, $"{string.Format("{0:X4}_1", item.Key)}.png");
                            bitmaps[item.Key][1].Save(name, ImageFormat.Png);
                        }
                        #endregion
                    }
                }


                //按色表输出_part1
                Dictionary<int, Bitmap[]> bitmapbycolor = new Dictionary<int, Bitmap[]>();
                foreach (var item in colors_dic)
                {
                    if (item.Value.Length == 16)
                    {
                        bitmapbycolor.Add(item.Key, new Bitmap[] { new Bitmap(width, height, PixelFormat.Format32bppArgb), new Bitmap(width, height, PixelFormat.Format32bppArgb) });
                    }
                    else
                    {
                        bitmapbycolor.Add(item.Key, new Bitmap[] { new Bitmap(width, height, PixelFormat.Format32bppArgb) });
                    }
                }
                var pathbase_ByColorPanel = Path.Combine(dinfo.Directory.FullName, dinfo.Name) + "_PNGByColorPanel";
                if (!Directory.Exists(pathbase_ByColorPanel))
                {
                    Directory.CreateDirectory(pathbase_ByColorPanel);
                }

                //按rect输出
                for (int p = 0; p < rects.Length; p++)
                {
                    var rect = rects[p];
                    Bitmap Picture = new Bitmap(rect.w, rect.h, PixelFormat.Format32bppArgb);
                    Queue<Color> queue = new Queue<Color>();
                    for (int i = rect.y; i < rect.y + rect.h; i++)
                    {
                        for (int j = rect.x; j < rect.x + rect.w; j++)
                        {
                            byte b = dicPic[new Point(j, i)];
                            Color color = Color.FromArgb(0, 0, 0, 0);
                            if (rect.type == 0x0305)
                            {
                                //256
                                color = colors_dic[rect.color_index][b];
                            }
                            else if (rect.type == 0x0304)
                            {
                                //16
                                int bit4 = b >> rects[p].bit4_type;
                                bit4 = bit4 & 0x000F;
                                color = colors_dic[rect.color_index][bit4];
                            }
                            queue.Enqueue(color);
                        }
                    }


                    for (int i = 0; i < rect.h; i++)
                    {
                        for (int j = 0; j < rect.w; j++)
                        {
                            var color = queue.Dequeue();
                            Picture.SetPixel(j, i, color);
                            if (rect.type == 0x0305)
                            {
                                //256
                                bitmapbycolor[rect.color_index][0].SetPixel(rect.x + j, rect.y + i, color);
                            }
                            else if (rect.type == 0x0304)
                            {
                                //16
                                if (rect.bit4_type == 0)
                                {
                                    bitmapbycolor[rect.color_index][0].SetPixel(rect.x + j, rect.y + i, color);
                                }
                                else if (rect.bit4_type == 4)
                                {
                                    bitmapbycolor[rect.color_index][1].SetPixel(rect.x + j, rect.y + i, color);
                                }

                            }
                        }
                    }

                    pathbase = Path.Combine(dinfo.Directory.FullName, dinfo.Name) + "_PNGARGB8ByRect"; ;
                    string name = Path.Combine(pathbase, $"{string.Format("{0:D4}", p)}_{string.Format("{0:X4}", rect.color_index) }.png");
                    if (!Directory.Exists(pathbase))
                    {
                        Directory.CreateDirectory(pathbase);
                    }
                    #region DEBUG
                    Picture.Save(name, ImageFormat.Png);
                    #endregion
                    Console.WriteLine(name);
                }

                //按色表输出_part2
                foreach (var item in bitmapbycolor)
                {
                    if (item.Value.Length == 1)
                    {
                        string name = Path.Combine(pathbase_ByColorPanel, $"{string.Format("{0:X4}", item.Key)}_{colors_dic[item.Key].Length}.png");
                        bitmapbycolor[item.Key][0].Save(name, ImageFormat.Png);
                        Console.WriteLine(name);
                    }
                    else if (item.Value.Length == 2)
                    {

                        string name = Path.Combine(pathbase_ByColorPanel, $"{string.Format("{0:X4}", item.Key)}_{colors_dic[item.Key].Length}.png");
                        bitmapbycolor[item.Key][0].Save(name, ImageFormat.Png);
                        Console.WriteLine(name);
                        name = Path.Combine(pathbase_ByColorPanel, $"{string.Format("{0:X4}", item.Key)}_{colors_dic[item.Key].Length}_1.png");
                        bitmapbycolor[item.Key][1].Save(name, ImageFormat.Png);
                        Console.WriteLine(name);

                    }

                }
            }
        }

        //α值 透明度 为colors ，RGB为 FF,FF,FF
        //int[] colors = new int[] { 0, 0, 11, 25, 36, 55, 77, 92, 109, 127, 146, 166, 187, 209, 231, 255 };
        public void Import(string path, string path_output, string path_org)
        {
            var img = new Bitmap(path);
            var bytes_org = File.ReadAllBytes(path_org);
            //check color count
            Dictionary<Color, int> dic = new Dictionary<Color, int>();
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    Color c = img.GetPixel(x, y);
                    if (dic.ContainsKey(c))
                    {
                        dic[c]++;
                    }
                    else
                    {
                        dic.Add(c, 1);
                    }
                }
            }
            Color[] colors = new Color[16];
            if (dic.Count <= 16)
            {
                //color ojbk
                List<Color> colors1 = new List<Color>();
                foreach (var item in dic)
                {
                    colors1.Add(item.Key);
                }
                while (colors1.Count < 16)
                {
                    colors1.Add(Color.FromArgb(0, 0, 0, 0));
                }
                colors = colors1.ToArray();
            }
            else
            {
                //color not ojbk
                //load color table from org
                int start = 64 + (512 * 512 / 2);
                var colorsbyte = new List<byte>();
                for (int i = start; i < start + 4 * 16; i++)
                {
                    colorsbyte.Add(bytes_org[i]);
                }
                StructReader Reader = new StructReader(new MemoryStream(colorsbyte.ToArray()));
                for (int i = 0; i < 16; i++)
                {
                    int B = Reader.ReadByte();
                    int G = Reader.ReadByte();
                    int R = Reader.ReadByte();
                    int A = Reader.ReadByte();
                    colors[i] = Color.FromArgb(A, R, G, B);
                }
            }

            //color table 2 α table
            int[] colors2 = new int[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors2[i] = colors[i].A;
            }

            Queue<int> queue = new Queue<int>();
            //font split 512/8 part y
            for (int y = 0; y < 512 / 8; y++)
            {
                //font split 16 part x
                for (int x = 0; x < 16; x++)
                {
                    for (int y1 = 0; y1 < 8; y1++)
                    {
                        for (int x1 = 0; x1 < 32; x1++)
                        {
                            try
                            {
                                Color c = img.GetPixel(x * 32 + x1, y * 8 + y1);
                                int index = GetColorIndex(colors2, c);
                                queue.Enqueue(index);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }

                        }
                    }
                }
            }
            List<byte> bytes = new List<byte>();
            //header
            for (int i = 0; i < 64; i++)
            {
                bytes.Add(bytes_org[i]);
            }
            //to byte[]
            while (queue.Count > 0)
            {
                int bit4_1 = queue.Dequeue();
                int bit4_2 = queue.Dequeue() << 4;
                int b = bit4_1 + bit4_2;
                bytes.Add((byte)b);
            }

            foreach (var item in colors)
            {
                bytes.Add((byte)item.B);
                bytes.Add((byte)item.G);
                bytes.Add((byte)item.R);
                bytes.Add((byte)item.A);
            }
            File.WriteAllBytes(path_output, bytes.ToArray());
        }

        public void Export(string path, string path_output)
        {
            byte[] fontbytes = File.ReadAllBytes(path);
            Bitmap Picture = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
            var ms = new MemoryStream(fontbytes);
            ms.Seek(64, SeekOrigin.Begin);
            StructReader Reader = new StructReader(ms);
            var list = new Queue<int>();
            for (int k = 0; k < 512 * 512 / 2; k++)
            {
                byte b = Reader.ReadByte();
                int bit2_1 = b & 0x0F;
                int bit2_2 = (b & 0xF0) >> 4;
                list.Enqueue(bit2_1);
                list.Enqueue(bit2_2);
            }
            Color[] colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                int B = Reader.ReadByte();
                int G = Reader.ReadByte();
                int R = Reader.ReadByte();
                int A = Reader.ReadByte();
                colors[i] = Color.FromArgb(A, R, G, B);
            }
            //font split 512/8 part y
            for (int y = 0; y < 512 / 8; y++)
            {
                //font split 16 part x
                for (int x = 0; x < 16; x++)
                {
                    for (int y1 = 0; y1 < 8; y1++)
                    {
                        for (int x1 = 0; x1 < 32; x1++)
                        {
                            int index = list.Dequeue();
                            Picture.SetPixel(x * 32 + x1, y * 8 + y1, colors[index]);
                        }
                    }
                }
            }
            Console.WriteLine("Export:" + path_output);
            if (path_output.ToLower().Contains(".png"))
            {
                Picture.Save(path_output, ImageFormat.Png);
            }
            else
            {
                Picture.Save(path_output + ".png", ImageFormat.Png);
            }

        }


        public List<Color> CheckColor(Bitmap img)
        {
            List<Color> listcp = new List<Color>();
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var color = img.GetPixel(x, y);
                    if (!listcp.Contains(color))
                    {
                        listcp.Add(color);
                    }
                }
            }
            return listcp;
        }

        public void CheckColor2(string name)
        {
            var img = new Bitmap(name);
            List<Color> listcp = new List<Color>();
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var color = img.GetPixel(x, y);
                    if (!listcp.Contains(color))
                    {
                        listcp.Add(color);
                    }
                }
            }
            Console.WriteLine($"TotalColorCount:{listcp.Count}");
            foreach (var item in listcp)
            {
                Console.WriteLine($"图片颜色:{item}！");
                //Console.WriteLine($"A:{item.A},R:{item.R},G:{item.G}");
            }
        }

        public void CheckColor(string name, string path_pngquant)
        {
            var img = new Bitmap(name);
            List<Color> listcp = new List<Color>();
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var color = img.GetPixel(x, y);
                    if (!listcp.Contains(color))
                    {
                        listcp.Add(color);
                    }
                }
            }

            if (listcp.Count >= 256 && File.Exists(path_pngquant))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo(path_pngquant) { RedirectStandardOutput = true };
                    psi.Arguments = " 255 " + name + " --ext .tmp ";
                    var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        Console.WriteLine("Can not exec.");
                    }
                    else
                    {
                        using (var sr = proc.StandardOutput)
                        {
                            while (!sr.EndOfStream)
                            {
                                Console.WriteLine(sr.ReadLine());
                            }

                            if (!proc.HasExited)
                            {
                                proc.Kill();
                            }
                        }
                    }
                    string pathtmp = name.Replace(".png", ".tmp");
                    img = new Bitmap(new MemoryStream(File.ReadAllBytes(pathtmp)));
                    listcp = new List<Color>();
                    for (int y = 0; y < img.Height; y++)
                    {
                        for (int x = 0; x < img.Width; x++)
                        {
                            var color = img.GetPixel(x, y);
                            if (!listcp.Contains(color))
                            {
                                listcp.Add(color);
                            }
                        }
                    }
                    File.Delete(pathtmp);
                }
            }
            if (listcp[0] == Color.FromArgb(0, 0, 0, 0))
            {
                listcp.RemoveAt(0);
            }
        }

        public int GetColorIndex(int[] colors, Color c)
        {
            int index = 1;
            if (c.A == 0)
            {
                index = 1;
            }
            else
            {
                //有相应的数值
                if (colors.Contains(c.A))
                {
                    for (int i = 1; i < colors.Length; i++)
                    {
                        if (colors[i] == c.A)
                        {
                            index = i;
                            break;
                        }
                    }
                }
                else
                {
                    //无相应数值
                    int min = 255;
                    int minindex = 1;
                    for (int i = 1; i < colors.Length; i++)
                    {
                        int tempdiff = Math.Abs(c.A - colors[i]);
                        if (tempdiff < min)
                        {
                            min = tempdiff;
                            minindex = i;
                        }
                    }
                    index = minindex;
                }
            }
            return index;
        }

        /// <summary>
        /// 判断目标是文件夹还是目录(目录包括磁盘)
        /// </summary>
        /// <param name="filepath">路径</param>
        /// <returns>返回true为一个文件夹，返回false为一个文件</returns>
        public static bool IsDir(string filepath)
        {
            FileInfo fi = new FileInfo(filepath);
            if ((fi.Attributes & FileAttributes.Directory) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

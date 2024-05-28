使用说明


PIM2/PM2 图片 解包 打包

PIM2/PM2 Image Unpack Repack


```shell
字库文件PIM2文件
TMGS3Tools_IMG.exe Export inputfilename outputfilename
TMGS3Tools_IMG.exe Import inputfilename outputfilename

字库以外的PIM2文件用
TMGS3Tools_IMG.exe Export2 inputfilename outputfilename
TMGS3Tools_IMG.exe Import2 path_input_fileOrfolder path_output_filename path_org_filename path_pngquant
path_pngquant 为外部调用 请指定路径

```

由于解包使用Bitmap类 Bitmap类用到GDI+ 只能在windows下正常工作

其他平台能兼容 但是解析出的图片显示并不正确 只做测试使用！！！

MacOS、Linux 下开启System.Drawing.EnableUnixSupport(已经写入程序) ，安装 libgdiplus 之后才能运行。


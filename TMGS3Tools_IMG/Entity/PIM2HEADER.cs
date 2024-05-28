using TMGS3Tools_IMG.Util;

namespace TMGS3Tools_IMG
{
    public struct  PIM2HEADER
    {
        //50 49 4D 32 04 00 01 00 00 00 00 00 00 00 00 00
        //70 80 00 00 //除了头的字节数32880
        //00 08 00 00 //颜色表字节数512*4=2048
        //00 78 00 00 //图片点阵的字节数30720
        //70 00 //头长度=32880-2048-30720
        //00 02 //颜色表数量
        //05 00 83 05 //未知类型0
        //80 00 //128 长和宽 
        //F0 00 //240
        //00 80 30 1D//unk1
        //02 00 80 00//unk2
        //60 02 00 00//unk3
        //00 00 00 00//unk4
        //00 00 00 00//unk5
        //00 00 00 00//unk6
        //42 55 57 00 04 00 00 00 00 00 00 00 4A 00 4A 00
        //00 00 05 03 00 FF 00 00 4A 00 44 00 50 00 00 00
        //05 03 00 FF 44 00 4A 00 3A 00 50 00 10 00 05 03
        //00 FF 00 00 9A 00 3A 00 50 00 10 00 05 03 00 FF
        [FString(Length = 16)]
        public string Signature;
        public uint TotalByteLength;//除了头的字节长度
        public uint ColorPanelByteLength;//颜色表的字节长度
        public uint PixelByteLength;//像素部分的字节长度
        public ushort HeaderByteLength;//头的字节长度
        public ushort ColorPanelCount;//颜色表数量
        public uint unk0;//
        public ushort Width;
        public ushort Heigth;
        public uint unk1;//
        public uint unk2;//
        public uint unk3;//
        public uint unk4;//
        public uint unk5;//
        public uint unk6;//
        [FString(Length = 4)]
        public string BUW;//
    }
}

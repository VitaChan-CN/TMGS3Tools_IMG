namespace TMGS3Tools_IMG
{
    public struct Rect
    {         
        //00 00 00 00 E0 01 10 01 00 00 05 03 00 FF 
        //00 00 10 01 E0 01 10 01 10 00 05 03 00 FF 
        //00 00 20 02 E0 01 10 01 20 00 04 03 00 0F 
        //00 00 20 02 E0 01 10 01 21 00 04 03 04 0F 
        public ushort x;
        public ushort y;
        public ushort w;
        public ushort h;
        public ushort color_index;//00 21

        //[Ignore]
        // public ushort color_index_sub;//颜色
        public ushort type;//04 03 =16色 //05 03 =256色
        public byte bit4_type;
        public byte colorcount;

    }
     
}

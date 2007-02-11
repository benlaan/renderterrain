using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace Laan.Drawing
{
    public class Bitmap
    {
        string  _fileName;
        int     _height;
        int     _width;
        int     _bitRate;
        int[,] _data;
        int     _offset;

        public Bitmap()
        {
            _fileName = "";
        }

        public Bitmap(string fileName)
        {
            Load(fileName);
        }

        public void Load(string fileName)
        {
            _fileName = fileName;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    ReadHeader(reader);
                    ReadPixels(reader);
                }
            }
        }

        private void ReadHeader(BinaryReader reader)
        {
            char[] bm = reader.ReadChars(2);
            //char[] expected =  new char[] { 'B', 'M' };
            //if (bm != expected)
            //    throw new InvalidDataException("Expected Bitmap");

            int fileSize = reader.ReadInt32();
            int zero = reader.ReadInt32();
            _offset = reader.ReadInt32();
            int forty = reader.ReadInt32();

            _width = reader.ReadInt32();
            _height = reader.ReadInt32();

            int alwaysOne = reader.ReadInt16();

            // Get Bit Rate
            _bitRate = reader.ReadByte();
            if (_bitRate != 24)
                throw new InvalidDataException("Must be a 24bit Bitmap");
            reader.ReadBytes(3);

            int compression = reader.ReadInt32();
            reader.ReadBytes(16);

            _data = new int[_width, _height];
        }

        private void ReadPixels(BinaryReader reader)
        {
            reader.ReadBytes(_offset - 26);
        
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    _data[_width - 1 - x, _height - 1 - y] = reader.ReadInt32();
        }

        public void Save()
        {
            throw new Exception("Not Yet Implemented");
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public int[,] PixelData
        {
            get { return _data; }
        }
    }
}

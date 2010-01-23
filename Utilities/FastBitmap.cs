using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Laan.Drawing
{

    public struct ColorARGB
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;

        public ColorARGB(Color color)
        {
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public ColorARGB(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }
    
        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }
    }

    unsafe class FastBitmap : IDisposable
    {

        System.Drawing.Bitmap     _bitmap;
        BitmapData _bitmapData;
        int        _width;
        int        _height;
        ColorARGB* _startingPosition;

        public FastBitmap()
        {
        }
    
        public FastBitmap(string fileName)
        {
            _bitmap = new System.Drawing.Bitmap(fileName);
            Load();
        }

        public FastBitmap(System.Drawing.Bitmap bitmap)
        {
            _bitmap = bitmap;
            Load();
        }

        public void Dispose()
        {
            if(_bitmap != null)
                _bitmap.UnlockBits(_bitmapData);
        }

        public void Load()
        {
            _width  = _bitmap.Width;
            _height = _bitmap.Height;

            _bitmapData = _bitmap.LockBits(
                new Rectangle(0, 0, _width, _height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );

            _startingPosition = (ColorARGB*)_bitmapData.Scan0;
        }
    
        public Color GetPixel(int x, int y)
        {
            ColorARGB* position = _startingPosition + y * _height + x;
            return Color.FromArgb(position->A, position->R, position->G, position->B);
        }

        public void SetPixel(int x, int y, Color color)
        {
            ColorARGB* position = _startingPosition + y * _height + x;
            position->A = color.A;
            position->R = color.R;
            position->G = color.G;
            position->B = color.B;
        }

        public void Invert()
        {
            for (int y = 0; y < _width; y++)
            {
                ColorARGB* pos = _startingPosition + y * _height;
                for (int x = 0; x < _height; x++)
                {
                    pos->A = (byte)(255 - pos->A);
                    pos->R = (byte)(255 - pos->R);
                    pos->G = (byte)(255 - pos->G);
                    pos->B = (byte)(255 - pos->B);
                    pos++;
                }
            }
        }
    }
}

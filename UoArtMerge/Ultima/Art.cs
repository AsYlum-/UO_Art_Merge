/* Adapted from UO-SDK
* 
* Copyright (C) 2013 Ian Karlinsey
* 
* 
* UOArtMerge is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
* 
* UOArtMerge is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with UltimaLive.  If not, see <http://www.gnu.org/licenses/>. 
*/

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace UoArtMerge.Ultima
{
    public class Art
    {
        private readonly FileIndex _fileIndex;
        private readonly Bitmap[] _cache;
        private readonly bool[] _removed;
        private byte[] _validBuffer;
        private byte[] _streamBuffer;

        public bool Modified;

        struct CheckSums
        {
            public byte[] checksum;
            public int pos;
            public int length;
            public int index;
        }
        private List<CheckSums> _checksumsLand;
        private List<CheckSums> _checksumsStatic;

        public Art(string mulPath, string idxPath)
        {
            _fileIndex = new FileIndex(idxPath, mulPath, 4);
            _cache = new Bitmap[GetIdxLength()];
            _removed = new bool[GetIdxLength()];
        }

        public bool IsUOAHS()
        {
            return GetIdxLength() == 0x13FDC;
        }

        public int GetMaxItemID()
        {
            if (GetIdxLength() == 0xC000)
            {
                return 0x7FFF;
            }

            if (GetIdxLength() == 0x13FDC)
            {
                return 0xFFDB;
            }

            return 0x3FFF;
        }

        public int GetIdxLength()
        {
            return (int)(_fileIndex.IdxLength / 12);
        }

        public ushort GetLegalItemID(int itemId, bool checkMaxId = true)
        {
            if (itemId < 0)
            {
                return 0;
            }

            if (!checkMaxId)
            {
                return (ushort)itemId;
            }

            int max = GetMaxItemID();
            if (itemId > max)
            {
                return 0;
            }

            return (ushort)itemId;
        }

        public void ReplaceStatic(int index, Bitmap bmp)
        {
            index = GetLegalItemID(index);
            index += 0x4000;

            _cache[index] = bmp;
            _removed[index] = false;
            Modified = true;
        }

        public void ReplaceLand(int index, Bitmap bmp)
        {
            index &= 0x3FFF;
            _cache[index] = bmp;
            _removed[index] = false;
            Modified = true;
        }

        public void RemoveStatic(int index)
        {
            index = GetLegalItemID(index);
            index += 0x4000;

            _removed[index] = true;
            Modified = true;
        }

        public void RemoveLand(int index)
        {
            index &= 0x3FFF;
            _removed[index] = true;
            Modified = true;
        }

        public unsafe bool IsValidStatic(int index)
        {
            index = GetLegalItemID(index);
            index += 0x4000;

            if (_removed[index])
            {
                return false;
            }

            if (_cache[index] != null)
            {
                return true;
            }

            Stream stream = _fileIndex.Seek(index, out int _, out int _);

            if (stream == null)
            {
                return false;
            }

            if (_validBuffer == null)
            {
                _validBuffer = new byte[4];
            }

            stream.Seek(4, SeekOrigin.Current);
            stream.Read(_validBuffer, 0, 4);
            fixed (byte* b = _validBuffer)
            {
                short* dat = (short*)b;
                if (*dat++ <= 0 || *dat <= 0)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsValidLand(int index)
        {
            index &= 0x3FFF;
            if (_removed[index])
            {
                return false;
            }

            return _cache[index] != null || _fileIndex.Valid(index, out int _, out int _, out bool _);
        }

        public Bitmap GetLand(int index)
        {
            index &= 0x3FFF;

            if (_removed[index])
            {
                return null;
            }

            if (_cache[index] != null)
            {
                return _cache[index];
            }

            Stream stream = _fileIndex.Seek(index, out int length, out int _);
            if (stream == null)
            {
                return null;
            }

            return _cache[index] = LoadLand(stream, length);
        }

        private unsafe Bitmap LoadLand(Stream stream, int length)
        {
            Bitmap bmp = new Bitmap(44, 44, PixelFormat.Format16bppArgb1555);
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, 44, 44), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
            if (_streamBuffer == null || _streamBuffer.Length < length)
                _streamBuffer = new byte[length];
            stream.Read(_streamBuffer, 0, length);
            stream.Close();
            fixed (byte* bindata = _streamBuffer)
            {
                ushort* bdata = (ushort*)bindata;
                int xOffset = 21;
                int xRun = 2;

                ushort* line = (ushort*)bd.Scan0;
                int delta = bd.Stride >> 1;

                for (int y = 0; y < 22; ++y, --xOffset, xRun += 2, line += delta)
                {
                    ushort* cur = line + xOffset;
                    ushort* end = cur + xRun;

                    while (cur < end)
                        *cur++ = (ushort)(*bdata++ | 0x8000);
                }

                xOffset = 0;
                xRun = 44;

                for (int y = 0; y < 22; ++y, ++xOffset, xRun -= 2, line += delta)
                {
                    ushort* cur = line + xOffset;
                    ushort* end = cur + xRun;

                    while (cur < end)
                        *cur++ = (ushort)(*bdata++ | 0x8000);
                }
            }
            bmp.UnlockBits(bd);
            return bmp;
        }

        public byte[] GetRawLand(int index)
        {
            index &= 0x3FFF;

            int length, extra;
            Stream stream = _fileIndex.Seek(index, out length, out extra);
            if (stream == null)
                return null;
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            stream.Close();
            return buffer;
        }

        public Bitmap GetStatic(int index, bool checkmaxid = true)
        {
            index = GetLegalItemID(index, checkmaxid);
            index += 0x4000;


            if (_removed[index])
                return null;
            if (_cache[index] != null)
                return _cache[index];

            int length, extra;
            Stream stream = _fileIndex.Seek(index, out length, out extra);
            if (stream == null)
                return null;

            return _cache[index] = LoadStatic(stream, length);
        }

        private unsafe Bitmap LoadStatic(Stream stream, int length)
        {
            Bitmap bmp;
            if (_streamBuffer == null || _streamBuffer.Length < length)
                _streamBuffer = new byte[length];
            stream.Read(_streamBuffer, 0, length);
            stream.Close();

            fixed (byte* data = _streamBuffer)
            {
                ushort* bindata = (ushort*)data;
                int count = 2;
                //bin.ReadInt32();
                int width = bindata[count++];
                int height = bindata[count++];

                if (width <= 0 || height <= 0)
                    return null;

                int[] lookups = new int[height];

                int start = height + 4;

                for (int i = 0; i < height; ++i)
                    lookups[i] = (int)(start + bindata[count++]);

                bmp = new Bitmap(width, height, PixelFormat.Format16bppArgb1555);
                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);


                ushort* line = (ushort*)bd.Scan0;
                int delta = bd.Stride >> 1;


                for (int y = 0; y < height; ++y, line += delta)
                {
                    count = lookups[y];

                    ushort* cur = line;
                    ushort* end;
                    int xOffset, xRun;

                    while ((xOffset = bindata[count++]) + (xRun = bindata[count++]) != 0)
                    {
                        if (xOffset > delta)
                            break;
                        cur += xOffset;
                        if (xOffset + xRun > delta)
                            break;
                        end = cur + xRun;

                        while (cur < end)
                            *cur++ = (ushort)(bindata[count++] ^ 0x8000);
                    }
                }
                bmp.UnlockBits(bd);
            }
            return bmp;
        }

        public byte[] GetRawStatic(int index)
        {
            index = GetLegalItemID(index);
            index += 0x4000;

            int length, extra;
            Stream stream = _fileIndex.Seek(index, out length, out extra);
            if (stream == null)
                return null;
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            stream.Close();
            return buffer;
        }

        public unsafe static void Measure(Bitmap bmp, out int xMin, out int yMin, out int xMax, out int yMax)
        {
            xMin = yMin = 0;
            xMax = yMax = -1;

            if (bmp == null || bmp.Width <= 0 || bmp.Height <= 0)
                return;

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555);

            int delta = (bd.Stride >> 1) - bd.Width;
            int lineDelta = bd.Stride >> 1;

            ushort* pBuffer = (ushort*)bd.Scan0;
            ushort* pLineEnd = pBuffer + bd.Width;
            ushort* pEnd = pBuffer + bd.Height * lineDelta;

            bool foundPixel = false;

            int x = 0, y = 0;

            while (pBuffer < pEnd)
            {
                while (pBuffer < pLineEnd)
                {
                    ushort c = *pBuffer++;

                    if ((c & 0x8000) != 0)
                    {
                        if (!foundPixel)
                        {
                            foundPixel = true;
                            xMin = xMax = x;
                            yMin = yMax = y;
                        }
                        else
                        {
                            if (x < xMin)
                                xMin = x;

                            if (y < yMin)
                                yMin = y;

                            if (x > xMax)
                                xMax = x;

                            if (y > yMax)
                                yMax = y;
                        }
                    }
                    ++x;
                }

                pBuffer += delta;
                pLineEnd += lineDelta;
                ++y;
                x = 0;
            }

            bmp.UnlockBits(bd);
        }

        public unsafe void Save(string path)
        {
            _checksumsLand = new List<CheckSums>();
            _checksumsStatic = new List<CheckSums>();
            string idx = Path.Combine(path, "artidx_.mul");
            string mul = Path.Combine(path, "art_.mul");
            using (FileStream fsidx = new FileStream(idx, FileMode.Create, FileAccess.Write, FileShare.Write),
                              fsmul = new FileStream(mul, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                MemoryStream memidx = new MemoryStream();
                MemoryStream memmul = new MemoryStream();
                SHA256Managed sha = new SHA256Managed();
                //StreamWriter Tex = new StreamWriter(new FileStream("d:/artlog.txt", FileMode.Create, FileAccess.ReadWrite));

                using (BinaryWriter binidx = new BinaryWriter(memidx),
                                    binmul = new BinaryWriter(memmul))
                {
                    for (int index = 0; index < GetIdxLength(); index++)
                    {
                        if (_cache[index] == null)
                        {
                            if (index < 0x4000)
                                _cache[index] = GetLand(index);
                            else
                                _cache[index] = GetStatic(index - 0x4000, false);
                        }
                        Bitmap bmp = _cache[index];
                        if (bmp == null || _removed[index])
                        {
                            binidx.Write((int)-1); // lookup
                            binidx.Write((int)0); // length
                            binidx.Write((int)-1); // extra
                            //Tex.WriteLine(System.String.Format("0x{0:X4} : 0x{1:X4} 0x{2:X4}", index, (int)-1, (int)-1));
                        }
                        else if (index < 0x4000)
                        {
                            MemoryStream ms = new MemoryStream();
                            bmp.Save(ms, ImageFormat.Bmp);
                            byte[] checksum = sha.ComputeHash(ms.ToArray());
                            CheckSums sum;
                            if (compareSaveImagesLand(checksum, out sum))
                            {
                                binidx.Write((int)sum.pos); //lookup
                                binidx.Write((int)sum.length);
                                binidx.Write((int)0);
                                //Tex.WriteLine(System.String.Format("0x{0:X4} : 0x{1:X4} 0x{2:X4}", index, (int)sum.pos, (int)sum.length));
                                //Tex.WriteLine(System.String.Format("0x{0:X4} -> 0x{1:X4}", sum.index, index));
                                continue;
                            }
                            //land
                            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555);
                            ushort* line = (ushort*)bd.Scan0;
                            int delta = bd.Stride >> 1;
                            binidx.Write((int)binmul.BaseStream.Position); //lookup
                            int length = (int)binmul.BaseStream.Position;
                            int x = 22;
                            int y = 0;
                            int linewidth = 2;
                            for (int m = 0; m < 22; ++m, ++y, line += delta, linewidth += 2)
                            {
                                --x;
                                ushort* cur = line;
                                for (int n = 0; n < linewidth; ++n)
                                    binmul.Write((ushort)(cur[x + n] ^ 0x8000));
                            }
                            x = 0;
                            linewidth = 44;
                            y = 22;
                            line = (ushort*)bd.Scan0;
                            line += delta * 22;
                            for (int m = 0; m < 22; m++, y++, line += delta, ++x, linewidth -= 2)
                            {
                                ushort* cur = line;
                                for (int n = 0; n < linewidth; n++)
                                    binmul.Write((ushort)(cur[x + n] ^ 0x8000));
                            }
                            int start = length;
                            length = (int)binmul.BaseStream.Position - length;
                            binidx.Write(length);
                            binidx.Write((int)0);
                            bmp.UnlockBits(bd);
                            CheckSums s = new CheckSums() { pos = start, length = length, checksum = checksum, index = index };
                            //Tex.WriteLine(System.String.Format("0x{0:X4} : 0x{1:X4} 0x{2:X4}", index, start, length));
                            _checksumsLand.Add(s);
                        }
                        else
                        {
                            MemoryStream ms = new MemoryStream();
                            bmp.Save(ms, ImageFormat.Bmp);
                            byte[] checksum = sha.ComputeHash(ms.ToArray());
                            CheckSums sum;
                            if (CompareSaveImagesStatic(checksum, out sum))
                            {
                                binidx.Write((int)sum.pos); //lookup
                                binidx.Write((int)sum.length);
                                binidx.Write((int)0);
                                //Tex.WriteLine(System.String.Format("0x{0:X4} -> 0x{1:X4}", sum.index, index));
                                //Tex.WriteLine(System.String.Format("0x{0:X4} : 0x{1:X4} 0x{2:X4}", index, sum.pos, sum.length));
                                continue;
                            }

                            // art
                            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555);
                            ushort* line = (ushort*)bd.Scan0;
                            int delta = bd.Stride >> 1;
                            binidx.Write((int)binmul.BaseStream.Position); //lookup
                            int length = (int)binmul.BaseStream.Position;
                            binmul.Write((int)1234); // header
                            binmul.Write((short)bmp.Width);
                            binmul.Write((short)bmp.Height);
                            int lookup = (int)binmul.BaseStream.Position;
                            int streamloc = lookup + bmp.Height * 2;
                            int width = 0;
                            for (int i = 0; i < bmp.Height; ++i)// fill lookup
                                binmul.Write(width);
                            int X = 0;
                            for (int Y = 0; Y < bmp.Height; ++Y, line += delta)
                            {
                                ushort* cur = line;
                                width = (int)(binmul.BaseStream.Position - streamloc) / 2;
                                binmul.BaseStream.Seek(lookup + Y * 2, SeekOrigin.Begin);
                                binmul.Write(width);
                                binmul.BaseStream.Seek(streamloc + width * 2, SeekOrigin.Begin);
                                int i = 0;
                                int j = 0;
                                X = 0;
                                while (i < bmp.Width)
                                {
                                    i = X;
                                    for (i = X; i <= bmp.Width; ++i)
                                    {
                                        //first pixel set
                                        if (i < bmp.Width)
                                        {
                                            if (cur[i] != 0)
                                                break;
                                        }
                                    }
                                    if (i < bmp.Width)
                                    {
                                        for (j = i + 1; j < bmp.Width; ++j)
                                        {
                                            //next non set pixel
                                            if (cur[j] == 0)
                                                break;
                                        }
                                        binmul.Write((short)(i - X)); //xoffset
                                        binmul.Write((short)(j - i)); //run
                                        for (int p = i; p < j; ++p)
                                            binmul.Write((ushort)(cur[p] ^ 0x8000));
                                        X = j;
                                    }
                                }
                                binmul.Write((short)0); //xOffset
                                binmul.Write((short)0); //Run
                            }
                            int start = length;
                            length = (int)binmul.BaseStream.Position - length;
                            binidx.Write(length);
                            binidx.Write((int)0);
                            bmp.UnlockBits(bd);
                            CheckSums s = new CheckSums() { pos = start, length = length, checksum = checksum, index = index };
                            //Tex.WriteLine(System.String.Format("0x{0:X4} : 0x{1:X4} 0x{2:X4}", index, start, length));
                            _checksumsStatic.Add(s);
                        }
                    }
                    memidx.WriteTo(fsidx);
                    memmul.WriteTo(fsmul);
                }
            }
        }

        private bool compareSaveImagesLand(byte[] newChecksum, out CheckSums sum)
        {
            sum = new CheckSums();
            for (int i = 0; i < _checksumsLand.Count; ++i)
            {
                byte[] cmp = _checksumsLand[i].checksum;
                if (cmp == null || newChecksum == null || cmp.Length != newChecksum.Length)
                {
                    return false;
                }

                bool valid = !cmp.Where((t, j) => t != newChecksum[j]).Any();

                if (!valid)
                {
                    continue;
                }

                sum = _checksumsLand[i];
                return true;
            }

            return false;
        }

        private bool CompareSaveImagesStatic(byte[] newChecksum, out CheckSums sum)
        {
            sum = new CheckSums();
            for (int i = 0; i < _checksumsStatic.Count; ++i)
            {
                byte[] cmp = _checksumsStatic[i].checksum;
                if (cmp == null || newChecksum == null || cmp.Length != newChecksum.Length)
                {
                    return false;
                }

                bool valid = !cmp.Where((t, j) => t != newChecksum[j]).Any();

                if (!valid)
                {
                    continue;
                }

                sum = _checksumsStatic[i];
                return true;
            }

            return false;
        }
    }
}

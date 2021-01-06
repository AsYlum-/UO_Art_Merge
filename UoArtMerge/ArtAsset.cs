/*
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

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using UoArtMerge.Ultima;

namespace UOArtMerge
{
    public class ArtAsset : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public static BitmapImage GetBitmapImage(Bitmap bmp)
        {
            if (bmp == null)
            {
                return null;
            }

            byte[] imageBytes;
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                imageBytes = stream.ToArray();
            }

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(imageBytes);
            bitmapImage.EndInit();
            return bitmapImage;
        }

/*
        static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
            {
                return true;
            }

            if (a1 == null || a2 == null)
            {
                return false;
            }

            if (a1.Length != a2.Length)
            {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }
*/

        public void Save()
        {
            if (ArtInstance == null)
            {
                return;
            }

            if (_artType == ArtType.Item)
            {
                ArtInstance.ReplaceStatic(Index, _bmp);
            }
            else
            {
                ArtInstance.ReplaceLand(Index, _bmp);
            }

            if (TileDataInstance == null)
            {
                return;
            }

            switch (_artType)
            {
                case ArtType.Item when _itemDatum.HasValue:
                    TileDataInstance.ItemTable[Index] = _itemDatum.Value;
                    break;
                case ArtType.Item:
                    TileDataInstance.ItemTable[Index] = new ItemData();
                    break;
                case ArtType.Land when _landDatum.HasValue:
                    TileDataInstance.LandTable[Index] = _landDatum.Value;
                    break;
                case ArtType.Land:
                    TileDataInstance.LandTable[Index] = new LandData();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void DeleteArt()
        {
            if (ArtInstance == null)
            {
                return;
            }

            if (_artType == ArtType.Item)
            {
                ArtInstance.RemoveStatic(Index);
            }
            else
            {
                ArtInstance.RemoveLand(Index);
            }
        }

        public void DeleteTileData()
        {
            if (TileDataInstance == null)
            {
                return;
            }

            switch (_artType)
            {
                case ArtType.Land:
                    TileDataInstance.LandTable[_index] = new LandData();
                    break;
                case ArtType.Item:
                    TileDataInstance.ItemTable[_index] = new ItemData();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private BitmapImage _bmpImage;
        public BitmapImage BmpImage
        {
            get
            {
                return _bmpImage ?? (_bmpImage = GetBitmapImage(Bmp));
            }

            set
            {
                _bmpImage = value;
                OnPropertyChanged("BmpImage");
            }
        }

        private Bitmap _bmp;
        public Bitmap Bmp
        {
            get
            {
                if (_bmp != null)
                {
                    return _bmp;
                }

                switch (_artType)
                {
                    case ArtType.Item when _art != null:
                        _bmp = _art.GetStatic(_index);
                        break;
                    case ArtType.Land when _art != null:
                        _bmp = _art.GetLand(_index);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return _bmp;
            }

            set
            {
                _bmp = value;
                OnPropertyChanged("Bmp");
            }
        }

        private TileData _tileDataInstance;
        public TileData TileDataInstance
        {
            get
            {
                return _tileDataInstance;
            }

            set
            {
                _tileDataInstance = value;
                OnPropertyChanged("TileData");
            }
        }


        private LandData? _landDatum;
        public LandData? LandDatum
        {
            get
            {
                return _landDatum;
            }

            set
            {
                _landDatum = value;
                OnPropertyChanged("LandDatum");
            }
        }

        private ItemData? _itemDatum;
        public ItemData? ItemDatum
        {
            get
            {
                return _itemDatum;
            }

            set
            {
                _itemDatum = value;
                OnPropertyChanged("ItemDatum");
            }
        }

        private int _index;
        public int Index
        {
            get
            {
                return _index;
            }

            set
            {
                _index = value;
                OnPropertyChanged("Index");
            }
        }

        private Art _art;
        public Art ArtInstance
        {
            get
            {
                return _art;
            }

            set
            {
                _art = value;
                OnPropertyChanged("ArtInstance");
            }
        }

        private readonly ArtType _artType;

        public enum ArtType
        {
            Land,
            Item
        }

        public ArtAsset(int idx, Art art, ArtType artType, LandData? landDatum, ItemData? itemDatum, TileData tileData)
        {
            Index = idx;
            _art = art;
            _artType = artType;
            _landDatum = landDatum;
            _itemDatum = itemDatum;
            _tileDataInstance = tileData;
        }

        public object Clone()
        {
            return new ArtAsset(Index, _art, _artType, null, null, _tileDataInstance)
            {
                _bmp = _bmp,
                _bmpImage = _bmpImage
            };
        }
    }
}
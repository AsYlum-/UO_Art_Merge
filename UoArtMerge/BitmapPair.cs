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

using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace UoArtMerge
{
    public class BitmapPair : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            handler?.Invoke(this, new PropertyChangedEventArgs(name));
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

        private BitmapImage _image1;
        public BitmapImage Image1
        {
            get
            {
                return _image1;
            }

            set
            {
                _image1 = value;
                OnPropertyChanged("Image1");
            }
        }

        private BitmapImage _image2;
        public BitmapImage Image2
        {
            get
            {
                return _image2;
            }

            set
            {
                _image2 = value;
                OnPropertyChanged("Image2");
            }
        }
    }
}

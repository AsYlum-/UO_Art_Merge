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
using UoArtMerge.Ultima;

namespace UOArtMerge
{
    public class ArtSet : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Path { get; set; }
        public Art ArtInstance { get; set; }
        public TileData TileDataInstance { get; set; }
        public BindingList<ArtAsset> Items { get; set; }
        public BindingList<ArtAsset> Land { get; set; }

        public ArtSet(string path)
        {
            Path = path;
            Items = new BindingList<ArtAsset>();
            Land = new BindingList<ArtAsset>();
        }

        public void SetItem(int idx, ArtAsset asset)
        {
            Items[idx] = asset;
            OnPropertyChanged("Items");
        }

        public void Save()
        {
            ArtInstance.Save(Path);
            TileDataInstance.SaveTileData(Path);
        }

        public void Load()
        {
            Items.AllowEdit = true;
            Items.AllowNew = true;
            Items.AllowRemove = true;
            Items.RaiseListChangedEvents = false;

            Land.AllowEdit = true;
            Land.AllowNew = true;
            Land.AllowRemove = true;
            Land.RaiseListChangedEvents = false;

            ArtInstance = new Art(System.IO.Path.Combine(Path, "Art.mul"), System.IO.Path.Combine(Path, "Artidx.mul"));
            TileDataInstance = new TileData(ArtInstance);
            TileDataInstance.Initialize(System.IO.Path.Combine(Path, "TileData.mul"));

            int maxId = ArtInstance.GetMaxItemID();
            for (int i = 0; i < maxId; ++i)
            {
                LandData? landData = null;
                if (i < TileDataInstance.LandTable.Length)
                {
                    landData = TileDataInstance.LandTable[i];
                }

                ItemData? itemData = null;
                if (i < TileDataInstance.ItemTable.Length)
                {
                    itemData = TileDataInstance.ItemTable[i];
                }

                Items.Add(new ArtAsset(i, ArtInstance, ArtAsset.ArtType.Item, landData, itemData, TileDataInstance));
            }

            for (int i = 0; i < 0x3FFF; ++i)
            {
                LandData? landData = null;
                if (i < TileDataInstance.LandTable.Length)
                {
                    landData = TileDataInstance.LandTable[i];
                }

                ItemData? itemData = null;
                if (i < TileDataInstance.ItemTable.Length)
                {
                    itemData = TileDataInstance.ItemTable[i];
                }

                Land.Add(new ArtAsset(i, ArtInstance, ArtAsset.ArtType.Land, landData, itemData, TileDataInstance));
            }

            Items.RaiseListChangedEvents = true;
            Items.ResetBindings();

            Land.RaiseListChangedEvents = true;
            Land.ResetBindings();
        }
    }
}

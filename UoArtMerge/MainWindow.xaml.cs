using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;

namespace UOArtMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static BitmapImage GetBitmapImage(Bitmap bmp)
        {
            if (bmp == null)
            {
                return null;
            }

            byte[] imageBytes;
            using (MemoryStream stream = new())
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

        public ArtSet ArtSet1 { get; set; }

        public ArtSet ArtSet2 { get; set; }

        public BindingList<ArtAsset> ClipBoardItems { get; set; }

        public BindingList<ArtAsset> ClipBoardLand { get; set; }

        private bool _linked = true;

        private bool _displayItemData = true;

        public bool DisplayItemData
        {
            get
            {
                return _displayItemData;
            }

            set
            {
                _displayItemData = value;
                NotifyPropertyChanged(nameof(DisplayItemData));
            }
        }

        private bool _modifyTileData = true;
        public bool ModifyTileData
        {
            get
            {
                return _modifyTileData;
            }

            set
            {
                _modifyTileData = value;
                NotifyPropertyChanged(nameof(ModifyTileData));
            }
        }

        public Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }

            if (element.GetType() == type)
            {
                return element;
            }

            Visual foundElement = null;
            if (element is FrameworkElement frameworkElement)
            {
                frameworkElement.ApplyTemplate();
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }

            return foundElement;
        }

        private void ArtSet1_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_linked)
            {
                return;
            }

            ScrollViewer listboxScrollViewer2 = GetDescendantByType(ArtList2, typeof(ScrollViewer)) as ScrollViewer;

            if (GetDescendantByType(ArtList1, typeof(ScrollViewer)) is ScrollViewer listboxScrollViewer1)
            {
                listboxScrollViewer2?.ScrollToVerticalOffset(listboxScrollViewer1.VerticalOffset);
            }
        }

        private void ArtSet2_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_linked)
            {
                return;
            }

            ScrollViewer listboxScrollViewer1 = GetDescendantByType(ArtList1, typeof(ScrollViewer)) as ScrollViewer;

            if (GetDescendantByType(ArtList2, typeof(ScrollViewer)) is ScrollViewer listboxScrollViewer2)
            {
                listboxScrollViewer1?.ScrollToVerticalOffset(listboxScrollViewer2.VerticalOffset);
            }
        }

        private void LandSet1_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_linked)
            {
                return;
            }

            ScrollViewer listboxScrollViewer2 = GetDescendantByType(LandList2, typeof(ScrollViewer)) as ScrollViewer;

            if (GetDescendantByType(LandList1, typeof(ScrollViewer)) is ScrollViewer listboxScrollViewer1)
            {
                listboxScrollViewer2?.ScrollToVerticalOffset(listboxScrollViewer1.VerticalOffset);
            }
        }

        private void LandSet2_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_linked)
            {
                return;
            }

            ScrollViewer listboxScrollViewer1 = GetDescendantByType(LandList1, typeof(ScrollViewer)) as ScrollViewer;

            if (GetDescendantByType(LandList2, typeof(ScrollViewer)) is ScrollViewer listboxScrollViewer2)
            {
                listboxScrollViewer1?.ScrollToVerticalOffset(listboxScrollViewer2.VerticalOffset);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            ClipBoardItems = new BindingList<ArtAsset>();
            ClipBoardLand = new BindingList<ArtAsset>();

            NotifyPropertyChanged(nameof(ClipBoardItems));
            NotifyPropertyChanged(nameof(ClipBoardLand));
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            _linked = !_linked;

            if (_linked)
            {
                LinkedButtonPath.Style = Resources["LinkedIcon"] as Style;
            }
            else
            {
                LinkedButtonPath.Style = Resources["UnlinkedIcon"] as Style;
            }

            NotifyPropertyChanged("LinkedButtonText");
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (_displayItemData)
            {
                switch (button.CommandParameter.ToString())
                {
                    case "1":
                        DeleteSelectedItemsFromList(ArtList1.SelectedItems, ArtSet1.Items);
                        break;
                    case "2":
                        DeleteSelectedItemsFromList(ArtList2.SelectedItems, ArtSet2.Items);
                        break;
                }
            }
            else
            {
                switch (button.CommandParameter.ToString())
                {
                    case "1":
                        DeleteSelectedItemsFromList(LandList1.SelectedItems, ArtSet1.Land);
                        break;
                    case "2":
                        DeleteSelectedItemsFromList(LandList2.SelectedItems, ArtSet2.Land);
                        break;
                }
            }
        }

        private void Click_Load(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            FolderBrowserDialog fbd = new();
            DialogResult result = fbd.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string path = fbd.SelectedPath;

            switch (button.CommandParameter.ToString())
            {
                case "1":
                    ArtSet1 = new ArtSet(path);
                    ArtSet1.Load();
                    NotifyPropertyChanged(nameof(ArtSet1));
                    break;
                case "2":
                    ArtSet2 = new ArtSet(path);
                    ArtSet2.Load();
                    NotifyPropertyChanged(nameof(ArtSet2));
                    break;
            }
        }

        private void Click_Save(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            switch (button.CommandParameter.ToString())
            {
                case "1":
                {
                    SaveDialog dialog = new(1, this);
                    dialog.ShowDialog();
                    break;
                }
                case "2":
                    ArtSet2.Save();
                    break;
            }
        }

        public void Save(int set)
        {
            switch (set)
            {
                case 1:
                    ArtSet1.Save();
                    break;
                case 2:
                    ArtSet2.Save();
                    break;
            }
        }

        private void ClearClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (_displayItemData)
            {
                ClipBoardItems.Clear();
            }
            else
            {
                ClipBoardLand.Clear();
            }
        }

        private void DeleteItemsFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (_displayItemData)
            {
                RemoveSelectedItemsFromList(ClipboardItemList.SelectedItems, ClipBoardItems);
            }
            else
            {
                RemoveSelectedItemsFromList(ClipboardLandList.SelectedItems, ClipBoardLand);
            }
        }

        private static void RemoveSelectedItemsFromList(IEnumerable selectedItems, ICollection<ArtAsset> list)
        {
            foreach (ArtAsset asset in selectedItems.Cast<ArtAsset>().ToList())
            {
                list.Remove(asset);
            }
        }

        private void DeleteSelectedItemsFromList(IEnumerable selectedItems, IList<ArtAsset> list)
        {
            foreach (ArtAsset asset in selectedItems.Cast<ArtAsset>().ToList())
            {
                asset.DeleteArt();
                if (ModifyTileData)
                {
                    asset.DeleteTileData();
                }
                asset.Bmp = null;
                asset.BmpImage = null;

                list.Remove(asset);
                list.Insert(asset.Index, asset);
            }
        }

        private void CopyFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (_displayItemData)
            {
                switch (button.CommandParameter.ToString())
                {
                    case "1" when ArtSet1 != null:
                        MoveSelectedItems(ClipboardItemList.SelectedItems, ArtSet1, ArtList1.SelectedItems);
                        break;
                    case "2" when ArtSet2 != null:
                        MoveSelectedItems(ClipboardItemList.SelectedItems, ArtSet2, ArtList2.SelectedItems);
                        break;
                }
            }
            else
            {
                switch (button.CommandParameter.ToString())
                {
                    case "1" when ArtSet1 != null:
                        MoveSelectedItems(ClipboardLandList.SelectedItems, ArtSet1, LandList1.SelectedItems);
                        break;
                    case "2" when ArtSet2 != null:
                        MoveSelectedItems(ClipboardLandList.SelectedItems, ArtSet2, LandList2.SelectedItems);
                        break;
                }
            }
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            IList selectedItems;
            BindingList<ArtAsset> targetList;

            if (_displayItemData)
            {
                targetList = ClipBoardItems;
                selectedItems = button.CommandParameter.ToString() == "1"
                    ? ArtList1.SelectedItems
                    : ArtList2.SelectedItems;
            }
            else
            {
                targetList = ClipBoardLand;
                selectedItems = button.CommandParameter.ToString() == "1"
                    ? LandList1.SelectedItems
                    : LandList2.SelectedItems;
            }

            if (selectedItems.Count <= 0)
            {
                return;
            }

            foreach (ArtAsset asset in selectedItems)
            {
                if (asset.Clone() is not ArtAsset newAsset)
                {
                    continue;
                }

                if (ModifyTileData)
                {
                    newAsset.ItemDatum = asset.ItemDatum;
                    newAsset.LandDatum = asset.LandDatum;
                }
                newAsset.Index = -1;
                newAsset.ArtInstance = null;
                newAsset.TileDataInstance = null;
                targetList.Add(newAsset);
            }
        }

        private void MoveRight_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button))
            {
                return;
            }

            if (_displayItemData)
            {
                MoveSelectedItems(ArtList1.SelectedItems, ArtSet2, ArtList2.SelectedItems);
            }
            else
            {
                MoveSelectedItems(LandList1.SelectedItems, ArtSet2, LandList2.SelectedItems);
            }
        }

        private void MoveSelectedItems(IEnumerable sourceSelection, ArtSet destination, IList destinationSelection)
        {
            if (sourceSelection == null || destination == null || destinationSelection == null || destinationSelection.Count < 1)
            {
                return;
            }

            if (destinationSelection[0] is not ArtAsset destAsset)
            {
                return;
            }

            int idx = destAsset.Index;

            if (idx < 0 || idx >= destination.Items.Count)
            {
                return;
            }

            foreach (ArtAsset asset in sourceSelection)
            {
                if (idx >= destination.Items.Count)
                {
                    continue;
                }

                if (asset.Clone() is not ArtAsset clone)
                {
                    continue;
                }

                clone.Index = idx;
                clone.ArtInstance = destination.ArtInstance;
                clone.TileDataInstance = destination.TileDataInstance;

                if (ModifyTileData)
                {
                    clone.ItemDatum = asset.ItemDatum;
                    clone.LandDatum = asset.LandDatum;
                }
                else
                {
                    ArtAsset existingDestinationAsset = _displayItemData
                        ? destination.Items[idx]
                        : destination.Land[idx];

                    if (existingDestinationAsset != null)
                    {
                        clone.ItemDatum = existingDestinationAsset.ItemDatum;
                        clone.LandDatum = existingDestinationAsset.LandDatum;
                    }
                }

                if (_displayItemData)
                {
                    destination.Items.RemoveAt(idx);
                    destination.Items.Insert(idx, clone);
                }
                else
                {
                    destination.Land.RemoveAt(idx);
                    destination.Land.Insert(idx, clone);
                }
                clone.Save();
                idx++;
            }
        }

        private void MoveLeft_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button))
            {
                return;
            }

            if (_displayItemData)
            {
                MoveSelectedItems(ArtList2.SelectedItems, ArtSet1, ArtList1.SelectedItems);
            }
            else
            {
                MoveSelectedItems(LandList2.SelectedItems, ArtSet1, LandList1.SelectedItems);
            }
        }
    }
}

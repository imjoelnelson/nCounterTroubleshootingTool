using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class CartridgeItem : INotifyPropertyChanged
    {
        public CartridgeItem(string barcode, List<string> associatedFiles)
        {
            cartName = barcode;
            files = associatedFiles;
            selected = true;
            slat = "Run SLAT";
        }

        public CartridgeItem(string name, string runName, List<Lane> associatedLanes)
        {
            cartName = name;
            cartID = runName;
            lanes = associatedLanes;
            slat = "Click";
        }

        private string CartName;
        public string cartName
        {
            get { return CartName; }
            set
            {
                if (CartName != value)
                {
                    CartName = value;
                    NotifyPropertyChanged("cartName");
                }
            }
        }

        private string CartID;
        public string cartID
        {
            get { return CartID; }
            set
            {
                if(CartID != value)
                {
                    CartID = value;
                    NotifyPropertyChanged("cartID");
                }
            }
        }

        private bool Selected;
        public bool selected
        {
            get { return Selected; }
            set
            {
                if (Selected != value)
                {
                    Selected = value;
                    NotifyPropertyChanged("selected");
                }
            }
        }

        public string slat { get; set; }
        public List<string> files { get; set; }
        public List<Lane> lanes { get; set; }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

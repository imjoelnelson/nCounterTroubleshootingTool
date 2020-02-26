using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class CartridgeItem2 : INotifyPropertyChanged
    {
        public CartridgeItem2(string id, List<Lane> includedLanes)
        {
            CartID = id;
            lanes = includedLanes;
            date = includedLanes.Select(x => x.Date).FirstOrDefault() ?? "";
            selected = true;
        }

        public string CartID;
        public string cartId
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

        private string Date;
        public string date
        {
            get { return Date; }
            set
            {
                if(Date != value)
                {
                    Date = value;
                    NotifyPropertyChanged("date");
                }
            }
        }

        public List<Lane> lanes;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class AnnotItem : INotifyPropertyChanged
    {
        public AnnotItem(string filename, string _annot)
        {
            Filename = filename;
            Annot = _annot;
        }

        public string Filename { get; set; }
        private string annot;
        public string Annot
        {
            get { return annot; }
            set
            {
                if (annot != value)
                {
                    annot = value;
                    NotifyPropertyChanged("Annot");
                }
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

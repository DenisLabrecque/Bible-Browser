using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace BibleBrowser
{
   /// <summary>
   /// Implements an ObservableCollection that fires not only when an object is added or deleted, but also when certain properties change.
   /// </summary>
   /// <typeparam name="T">Any object implementing INotifyPropertyChanged</typeparam>
   public sealed class TrulyObservableCollection<T> : ObservableCollection<T>
       where T : INotifyPropertyChanged
   {
      public TrulyObservableCollection()
      {
         CollectionChanged += FullObservableCollectionCollectionChanged;
      }

      public TrulyObservableCollection(IEnumerable<T> pItems) : this()
      {
         foreach (var item in pItems)
         {
            this.Add(item);
         }
      }

      private void FullObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         if (e.NewItems != null)
         {
            foreach (Object item in e.NewItems)
            {
               ((INotifyPropertyChanged)item).PropertyChanged += ItemPropertyChanged;
            }
         }
         if (e.OldItems != null)
         {
            foreach (Object item in e.OldItems)
            {
               ((INotifyPropertyChanged)item).PropertyChanged -= ItemPropertyChanged;
            }
         }
      }

      private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
         OnCollectionChanged(args);
      }
   }
}

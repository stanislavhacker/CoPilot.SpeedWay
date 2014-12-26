using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WC = Microsoft.Phone.Maps.Controls;

namespace CoPilot.Speedway.Data
{
    public static class MapDependency
    {
        /// <summary>
        /// Items source property
        /// </summary>
        public static readonly DependencyProperty PathProperty = DependencyProperty.RegisterAttached("ItemsSource", typeof(GeoCoordinateCollection), typeof(MapDependency), new PropertyMetadata(OnPathPropertyChanged));

        /// <summary>
        /// On change
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;
            WC.Map map = (WC.Map)uie;

            GeoCoordinateCollection path = (GeoCoordinateCollection)e.NewValue;
            map.MapElements.Clear();
            if (path != null)
            {
                MapPolyline polyline = new MapPolyline();
                polyline.StrokeColor = (Color)App.Current.Resources["PhoneAccentColor"];
                polyline.StrokeThickness = 8;
                polyline.Path = path;
                map.MapElements.Add(polyline);
            }
        }


        #region Getters and Setters

        /// <summary>
        /// Get items source
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static GeoCoordinateCollection GetPath(DependencyObject obj)
        {
            return (GeoCoordinateCollection)obj.GetValue(PathProperty);
        }

        /// <summary>
        /// Set items source
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetPath(DependencyObject obj, GeoCoordinateCollection value)
        {
            obj.SetValue(PathProperty, value);
        }

        #endregion
    }
}

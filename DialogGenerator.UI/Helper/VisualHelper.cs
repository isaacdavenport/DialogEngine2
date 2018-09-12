using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DialogGenerator.UI.Helper
{
    /// <summary>
    /// Helper class which helps to search nodes in visual tree of the application
    /// </summary>
    public static class VisualHelper
    {
        /// <summary>
        /// Returns firt container of initial with type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_initial"></param>
        /// <returns>Returns firt container of initial with type T </returns>
        public static T GetNearestContainer<T>(DependencyObject _initial) where T : DependencyObject
        {
            DependencyObject _visual = _initial;

            if (_visual is Visual || _visual is System.Windows.Media.Media3D.Visual3D)
            {
                while (_visual != null && _visual.GetType() != typeof(T))
                {
                    _visual = VisualTreeHelper.GetParent(_visual);
                }
            }

            return _visual as T;
        }

        /// <summary>
        /// Returns first child of type T for parent object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_parent"></param>
        /// <returns>Returns first child of type T for parent object</returns>
        public static T GetVisualChild<T>(this DependencyObject _parent) where T : FrameworkElement
        {
            T _child = default(T);

            int _numVisuals = VisualTreeHelper.GetChildrenCount(_parent);

            for (int i = 0; i < _numVisuals; i++)
            {
                Visual _visual = (Visual)VisualTreeHelper.GetChild(_parent, i);
                _child = _visual as T;

                if (_child == null)
                {
                    _child = GetVisualChild<T>(_visual);
                }

                if (_child != null)
                {
                    break;
                }
            }
            return _child;
        }

        /// <summary>
        /// Recursive goes through children of control and yield to result child of specified type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="depObj">Container</param>
        /// <returns>List with children of specified type T</returns>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}

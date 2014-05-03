using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DynamicStyle
{
    public interface IDynamicRule
    {
        string Name { get; set; }

        bool IsMatch(Size size);
    }

    public class ShapeRule : IDynamicRule
    {
        public string Name { get; set; }

        public bool IsMatch(Size size)
        {
            if ((MinHeight > 0) && (size.Height < MinHeight))
                return false;
            if ((MaxHeight > 0) && (size.Height > MaxHeight))
                return false;
            if ((MinWidth > 0) && (size.Width < MinWidth))
                return false;
            if ((MaxWidth > 0) && (size.Width > MaxWidth))
                return false;
            if (IsWide && (size.Height >= size.Width))
                return false;
            if (IsTall && (size.Width >= size.Height))
                return false;

            return true;
        }

        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }

        public bool IsWide { get; set; }            // Landscape
        public bool IsTall { get; set; }            // Portrait
    }

    public class Dynamic
    {
        public static List<IDynamicRule> DefaultStyleRules = new List<IDynamicRule>
        {
            new ShapeRule { Name = "XWide", IsWide = true, MinWidth = 1366 },
            new ShapeRule { Name = "Wide", IsWide = true },
            new ShapeRule { Name = "Tall", IsTall = true }
        };

        private static List<FrameworkElement> FrameworkElementsWithStyle = new List<FrameworkElement>();
        private static bool WindowSizeChangedHandled = false;

        public static List<IDynamicRule> StyleRules { get; set; }


        /*******************************************************************/

        public static readonly DependencyProperty StyleProperty = DependencyProperty.RegisterAttached(
            "Style", 
            typeof(string), 
            typeof(Dynamic), 
            new PropertyMetadata(default(string)));

        public static void SetStyle(DependencyObject element, string value)
        {
            element.SetValue(StyleProperty, value);
            var frameworkElement = element as FrameworkElement;
            if (frameworkElement == null) return;

            frameworkElement.Loaded += (sender, args) => FrameworkElementsWithStyle.Add(frameworkElement);
            frameworkElement.Unloaded += (sender, args) => FrameworkElementsWithStyle.Remove(frameworkElement);

            EnsureWindowSizeChanged();

            var rules = StyleRules ?? DefaultStyleRules;

            var name = GetRuleName(rules, new Size(Window.Current.Bounds.Width, Window.Current.Bounds.Height));
            SetFrameworkStyle(frameworkElement, name);
        }

        public static string GetStyle(DependencyObject element)
        {
            return (string)element.GetValue(StyleProperty);
        }

        /*******************************************************************/

        public static readonly DependencyProperty StyleRulesProperty = DependencyProperty.RegisterAttached(
            "StyleRules", typeof (List<IDynamicRule>), typeof (Dynamic), new PropertyMetadata(new List<IDynamicRule>()));

        public static void SetStyleRules(DependencyObject element, List<IDynamicRule> rules)
        {
            StyleRules = rules ?? DefaultStyleRules;
        }

        public static List<IDynamicRule> GetStyleRules(DependencyObject element)
        {
            return StyleRules ?? DefaultStyleRules;
        }

        /*******************************************************************/

        public static readonly DependencyProperty VisualStateRulesProperty = DependencyProperty.RegisterAttached(
            "VisualStateRules", typeof (List<IDynamicRule>), typeof (Dynamic), new PropertyMetadata(new List<IDynamicRule>()));


        /*
        public static void SetVisualStateRules(DependencyObject element, List<ShapeRule> rules)
        {
            element.SetValue(VisualStateRulesProperty, rules);
            ValidateRules(rules);

            var control = element as Control;
            if (control == null) return;

            control.Loaded += (sender, args) =>
            {
                control.SizeChanged += ElementOnSizeChanged;
                UpdateDynamicVisualState(control);
            };
            control.Unloaded += (sender, args) => control.SizeChanged -= ElementOnSizeChanged;
        }
        */

        public static List<IDynamicRule> GetVisualStateRules(DependencyObject element)
        {
            var list = (element.GetValue(VisualStateRulesProperty) as List<IDynamicRule>) ?? new List<IDynamicRule>();

            var control = element as Control;
            if (control == null) return list;

            control.Loaded += (sender, args) =>
            {
                control.SizeChanged += ElementOnSizeChanged;
                UpdateDynamicVisualState(control);
            };
            control.Unloaded += (sender, args) => control.SizeChanged -= ElementOnSizeChanged;

            return list;
        }


        /// Whenever an element is resized, then evaluate
        private static void ElementOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            var control = sender as Control;
            if (control == null) return;

            UpdateDynamicVisualState(control);
        }


        /// Set the visual state based on the controls size
        public static void UpdateDynamicVisualState(Control control)
        {
            var rules = GetVisualStateRules(control);
            if ((rules == null) || (rules.Count == 0)) return;

            var width = (Math.Abs(control.ActualWidth) > 0.001) ? control.ActualWidth : control.Width;
            var height = (Math.Abs(control.ActualHeight) > 0.001) ? control.ActualHeight : control.Height;

            var name = GetRuleName(rules, new Size(width, height));
            if (string.IsNullOrWhiteSpace(name)) return;

            VisualStateManager.GoToState(control, name, true);
        }

        /*******************************************************************/


        private static void EnsureWindowSizeChanged()
        {
            if (WindowSizeChangedHandled) return;
            WindowSizeChangedHandled = true;

            Window.Current.SizeChanged += (sender, args) =>
            {
                var rules = StyleRules ?? DefaultStyleRules;
                if ((rules == null) || (rules.Count == 0)) return;

                var name = GetRuleName(rules, args.Size);

                foreach (var control in FrameworkElementsWithStyle)
                    SetFrameworkStyle(control, name);
            };
        }


        private static void SetFrameworkStyle(FrameworkElement frameworkElement, string name)
        {
            var styleName = GetStyle(frameworkElement);
            if (string.IsNullOrWhiteSpace(styleName)) return;

            Style style = null;
            if (!string.IsNullOrWhiteSpace(name))
            {
                string modifiedStyleName = styleName + "." + name;
                if (Application.Current.Resources.ContainsKey(modifiedStyleName))
                    style = Application.Current.Resources[modifiedStyleName] as Style;
            }
            if (style == null)
                style = Application.Current.Resources[styleName] as Style;

            if (style != null)
                frameworkElement.Style = style;
        }


        /*
        private static void ValidateRules(IEnumerable<IDynamicRule> rules)
        {
            foreach (var rule in rules)
            {
                if (rule.IsTall && rule.IsWide)
                    throw new ArgumentException("A rule cannot have both IsTall and IsWide");

                if (rule.MinWidth > rule.MaxWidth)
                    throw new ArgumentException("Rule MinWidth cannot be more than MaxWidth");

                if (rule.MinHeight > rule.MaxHeight)
                    throw new ArgumentException("Rule MinHeight cannot be more than MaxHeight");
            }
        }
        */

        private static string GetRuleName(IEnumerable<IDynamicRule> rules, Size size)
        {
            foreach (var rule in rules)
            {
                if (rule.IsMatch(size))
                    return rule.Name;
            }
            return null;
        }
    }
}

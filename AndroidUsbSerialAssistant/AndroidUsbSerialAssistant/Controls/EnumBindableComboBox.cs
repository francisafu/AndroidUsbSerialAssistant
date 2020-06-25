using System;
using System.Linq;
using Syncfusion.DataSource.Extensions;
using Syncfusion.XForms.ComboBox;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using SelectionChangedEventArgs = Syncfusion.XForms.ComboBox.SelectionChangedEventArgs;

namespace AndroidUsbSerialAssistant.Controls
{
    public class EnumBindableComboBox<T> : SfComboBox where T : struct
    {
        public new static BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem),
                typeof(T),
                typeof(EnumBindableComboBox<T>),
                default(T),
                propertyChanged: OnItemSourcePropertyChanged,
                defaultBindingMode: BindingMode.TwoWay);

        public EnumBindableComboBox()
        {
            SelectionChanged += OnSelectionChanged;
            DataSource = Enum.GetValues(typeof(T)).ToList<object>();
        }

        public new T SelectedItem
        {
            get => (T) GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private void OnSelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (SelectedIndex < 0 || SelectedIndex > DataSource.Count() - 1)
                SelectedItem = default;
            else
                SelectedItem = (T) Enum.Parse(typeof(T),
                    DataSource.ElementAt(SelectedIndex).ToString());
        }

        private static void OnItemSourcePropertyChanged(BindableObject bindable,
            object oldvalue,
            object newvalue)
        {
            if (newvalue != null && bindable is EnumBindableComboBox<T> picker)
                picker.SelectedIndex = picker.DataSource.IndexOf((T) newvalue);
        }
    }
}
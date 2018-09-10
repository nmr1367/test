using RayanSolution.Base.Tools;
using RayanSolution.DataProvider.Infrastructure.Entity;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace RayanSolution.Base.UI
{
    public class RadDropDownListX : RadDropDownList
    {
        public event EventHandler SelectedObjectChanged;

        private object selectedObject;
        private bool isInitializingDataSource;
        private RadLabel label = new RadLabel();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedObject
        {
            get { return selectedObject; }
            set
            {
                if (selectedObject != value)
                {
                    if (value == DBNull.Value || value == null)
                    {
                        SelectedItem = null;
                        selectedObject = null;
                        Text = string.Empty;
                        label.Text = string.Empty;
                    }
                    else
                    {
                        var item = Items.FirstOrDefault(x => value.Equals(x.DataBoundItem));
                        SelectedIndex = Items.IndexOf(item);
                        //SelectedItem = item;
                        selectedObject = value;
                        //Text = item.ToString();
                        label.Location = this.Location;
                        label.Size = this.Size;
                        label.Text = item == null ? "" : item.ToString();
                        label.Anchor = this.Anchor;
                    }

                    OnSelectedObjectChanged();
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadLabel DisableLabel => label;

        public new object DataSource
        {
            get
            {
                return base.DataSource;
            }
            set
            {
                isInitializingDataSource = true;

                if (value is IList list)
                {
                    if (list.Count > 0 && list[0] is IActive)
                    {
                        for (int i = list.Count - 1; i >= 0; i--)
                        {
                            var active = list[i] as IActive;
                            if (!active.IsEnabled()) list.RemoveAt(i);
                        }
                    }
                }
                base.DataSource = value;
                SynchronizeDataSource(true, (DataBindings.Count == 0 ? null : DataBindings[0]));

                isInitializingDataSource = false;
            }
        }

        public override string ThemeClassName
        {
            get
            {
                return "Telerik.WinControls.UI.RadDropDownList";
            }
            set
            {
                base.ThemeClassName = value;
            }
        }

        public RadDropDownListX()
        {
            ListElement.DataLayer.ChangeCurrentOnAdd = false;
            DropDownListElement.AutoCompleteMode = AutoCompleteMode.Suggest;
            DropDownListElement.DropDownMinSize = new System.Drawing.Size(164, 0);
            DropDownListElement.AutoCompleteSuggest.DropDownList.DropDownMinSize = new System.Drawing.Size(164, 0);
            DropDownListElement.AutoCompleteSuggest.SuggestMode = SuggestMode.Contains;
            DropDownListElement.ListElement.DataLayer.ChangeCurrentOnAdd = false;
            DropDownListElement.Size = new System.Drawing.Size(this.Width, 25);

            DataBindings.CollectionChanging += DataBindings_CollectionChanging;
            label.Visible = false;
            label.AutoSize = false;
            label.Name = "lblDisabled_" + this.Name;
            label.Size = this.Size;
            label.Anchor = this.Anchor;

            isInitializingDataSource = false;

            //this.AutoSizeItems = true;

            label.TextChanged += label_TextChanged;
            VisualListItemFormatting += RadDropDownListX_VisualListItemFormatting;
            this.AutoSize = true;

            // this.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
        }

        public void Clear()
        {
            SelectedIndex = -1;
            SelectedObject = null;
        }

        private void RadDropDownListX_VisualListItemFormatting(object sender, VisualItemFormattingEventArgs args)
        {
            args.VisualItem.ToolTipText = args.VisualItem.Text;
        }

        private void DataBindings_CollectionChanging(object sender, CollectionChangeEventArgs e)
        {
            if (e.Action == CollectionChangeAction.Add)
            {
                if (this.DataSource == null)
                    this.SelectedIndex = -1;
                else
                {
                    if (String.IsNullOrEmpty(this.ValueMember))
                        throw new Exception("ValueMember is not assigned!");

                    Binding binding = e.Element as Binding;
                    if (binding != null)
                    {
                        var objValue =
                            binding.DataSource.GetType()
                                .GetProperty(binding.BindingMemberInfo.BindingField)
                                .GetValue(binding.DataSource, null);

                        bool isEntityBase = objValue is EntityBase;
                        bool isExist = false;
                        PropertyInfo dsProperty = null;

                        if (objValue.HasProperty("id")) objValue = objValue.GetPropertyValue<long?>("id");

                        foreach (var item in (IEnumerable)this.DataSource)
                        {
                            if (dsProperty == null)
                                dsProperty = item.GetType().GetProperty(this.ValueMember);

                            if (dsProperty.GetValue(item, null).Equals(objValue))
                            {
                                isExist = true;
                                break;
                            }
                        }

                        if (!isExist)
                        {
                            if (isEntityBase)
                            {
                                SynchronizeDataSource(false, binding);
                            }
                            else
                            {
                                this.SelectedIndex = -1;
                                this.Text = String.Empty;
                            }
                        }
                    }
                }
            }
        }

        protected override void OnSelectedValueChanged(object sender, int newIndex, object oldValue, object newValue)
        {
            if (isInitializingDataSource) return;

            if (SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }
            else
            {
                SelectedObject = this.SelectedItem.DataBoundItem;
            }

            foreach (Binding binding in DataBindings)
            {
                if (binding.DataSourceUpdateMode == DataSourceUpdateMode.OnPropertyChanged)
                {
                    binding.WriteValue();

                    //try
                    //{
                    //    binding.WriteValue();
                    //}
                    //catch (ArgumentException)
                    //{
                    //    /* چنانچه مقدار
                    //     * property
                    //     * شی اصلی در لیست آیتمهای کنترل نباشد، تنها مقدار متن کنترل با مقدار
                    //     * property
                    //     * مورد نظر پر می شود
                    //     * توجه شود که برای انجام این کار
                    //     * DropDownStyle
                    //     * باید با مقدار
                    //     * RadDropDownStyle.DropDown
                    //     * پر شده باشد */

                    //    //object referencedObject = binding.DataSource.GetPropertyValue(binding.BindingMemberInfo.BindingMember);
                    //    /* در زمانی که فرم جدید با شی جدید ساخته می شود، شی جدید هنوز دارای مقدار برای
                    //     * Property
                    //     * های خود نیست */
                    //    //if (referencedObject != null) Text = referencedObject.GetPropertyValue(DisplayMember).ToString();

                    //    try
                    //    {
                    //        Text = binding.DataSource.GetPropertyValue(binding.BindingMemberInfo.BindingMember).GetPropertyValue(DisplayMember).ToString();
                    //    }
                    //    catch (NullReferenceException)
                    //    {
                    //        /* در زمانی که فرم جدید با شی جدید ساخته می شود، شی جدید هنوز دارای مقدار برای
                    //         * Property
                    //         * های خود نیست بنابراین
                    //         * NullReferenceException
                    //         * رخ می دهد */
                    //    }
                    //}
                }
            }

            base.OnSelectedValueChanged(sender, newIndex, oldValue, newValue);
        }

        protected override void OnSelectedIndexChanged(object sender, int newIndex)
        {
            if (isInitializingDataSource) return;
            base.OnSelectedIndexChanged(sender, newIndex);
        }

        protected override void OnLoad(Size desiredSize)
        {
            base.OnLoad(desiredSize);

            this.EnabledChanged += RadDropDownListX_EnabledChanged;
            this.VisibleChanged += (s, e) => { if (!this.Visible) label.Visible = false; };
            this.DropDownListElement.DropDownMinSize = new Size(164, 0);
        }

        protected virtual void OnSelectedObjectChanged()
        {
            SelectedObjectChanged?.Invoke(this, EventArgs.Empty);
        }

        #region show_Label

        private void RadDropDownListX_EnabledChanged(object sender, EventArgs e)
        {
            //if (this.Visible)
            //{
            if (!Enabled)
            {
                label.Visible = true;
                label.Size = this.Size;
                label.Location = this.Location;
                label.Text = this.Text;
                label.Anchor = this.Anchor;

                if (this.Parent != null)
                {
                    if (label.Parent == null)
                        this.Parent.Controls.Add(label);

                    label.RightToLeft = this.Parent.RightToLeft;
                }

                if (this.DropDownListElement.TextBox.TextAlign == HorizontalAlignment.Right)
                    label.TextAlignment = ContentAlignment.MiddleRight;
                else if (this.DropDownListElement.TextBox.TextAlign == HorizontalAlignment.Left)
                    label.TextAlignment = ContentAlignment.MiddleLeft;

                label.BringToFront();
                label.BringToFront();

                Helper.SetRadLableBackColor(label);
            }
            else
            {
                label.Visible = false;
                this.BringToFront();
                this.BringToFront();
            }
            //}
        }

        private void label_TextChanged(object sender, EventArgs e)
        {
            Helper.SetRadLableBackColor(label);
        }

        #endregion show_Label

        private void SynchronizeDataSource(bool checkPropertyModificationBeforeSynch, Binding binding)
        {
            if (this.FindForm().GetPropertyValue<bool>("IsBinding"))
            {
                IList dataSource = DataSource as IList;
                if (dataSource != null && binding != null)
                {
                    EntityBase entity = binding.DataSource as EntityBase;
                    if (!entity.IsNew)
                    {
                        object value = entity.GetPropertyValue(binding.BindingMemberInfo.BindingField);
                        if (!checkPropertyModificationBeforeSynch || (entity.OriginalValues.ContainsKey(binding.BindingMemberInfo.BindingField) && value != entity.OriginalValues[binding.BindingMemberInfo.BindingField]))
                        {
                            dataSource.AddIfNotExists(value);
                            SelectedObject = value;
                            SelectedText = entity.GetPropertyValue<string>(DisplayMember);
                        }
                    }
                }
            }
        }
    }
}

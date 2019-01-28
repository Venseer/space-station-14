using System;
using System.Globalization;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.ViewVariables.Editors
{
    public class ViewVariablesPropertyEditorUIBox2 : ViewVariablesPropertyEditor
    {
        private readonly BoxType _type;

        public ViewVariablesPropertyEditorUIBox2(BoxType type)
        {
            _type = type;
        }

        protected override Control MakeUI(object value)
        {
            var hBoxContainer = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(200, 0),
            };

            var left = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Left",
                ToolTip = "Left"
            };

            var top = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Top",
                ToolTip = "Top"
            };

            var right = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Right",
                ToolTip = "Right"
            };

            var bottom = new LineEdit
            {
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                PlaceHolder = "Bottom",
                ToolTip = "Bottom"
            };

            // Assign default text to editors.
            switch (_type)
            {
                case BoxType.Box2:
                {
                    var box = (Box2) value;
                    left.Text = box.Left.ToString(CultureInfo.InvariantCulture);
                    top.Text = box.Top.ToString(CultureInfo.InvariantCulture);
                    right.Text = box.Right.ToString(CultureInfo.InvariantCulture);
                    bottom.Text = box.Bottom.ToString(CultureInfo.InvariantCulture);
                    break;
                }
                case BoxType.Box2i:
                {
                    var box = (Box2i) value;
                    left.Text = box.Left.ToString(CultureInfo.InvariantCulture);
                    top.Text = box.Top.ToString(CultureInfo.InvariantCulture);
                    right.Text = box.Right.ToString(CultureInfo.InvariantCulture);
                    bottom.Text = box.Bottom.ToString(CultureInfo.InvariantCulture);
                    break;
                }
                case BoxType.UIBox2:
                {
                    var box = (UIBox2) value;
                    left.Text = box.Left.ToString(CultureInfo.InvariantCulture);
                    top.Text = box.Top.ToString(CultureInfo.InvariantCulture);
                    right.Text = box.Right.ToString(CultureInfo.InvariantCulture);
                    bottom.Text = box.Bottom.ToString(CultureInfo.InvariantCulture);
                    break;
                }
                case BoxType.UIBox2i:
                {
                    var box = (UIBox2i) value;
                    left.Text = box.Left.ToString(CultureInfo.InvariantCulture);
                    top.Text = box.Top.ToString(CultureInfo.InvariantCulture);
                    right.Text = box.Right.ToString(CultureInfo.InvariantCulture);
                    bottom.Text = box.Bottom.ToString(CultureInfo.InvariantCulture);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Organization of the editors is different when left vs right handed.
            switch (_type)
            {
                case BoxType.Box2:
                case BoxType.Box2i:
                    hBoxContainer.AddChild(left);
                    hBoxContainer.AddChild(bottom);
                    hBoxContainer.AddChild(right);
                    hBoxContainer.AddChild(top);
                    break;
                case BoxType.UIBox2:
                case BoxType.UIBox2i:
                    hBoxContainer.AddChild(left);
                    hBoxContainer.AddChild(top);
                    hBoxContainer.AddChild(right);
                    hBoxContainer.AddChild(bottom);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            void OnEntered(LineEdit.LineEditEventArgs e)
            {
                switch (_type)
                {
                    case BoxType.Box2:
                    {
                        var leftVal = float.Parse(left.Text, CultureInfo.InvariantCulture);
                        var topVal = float.Parse(top.Text, CultureInfo.InvariantCulture);
                        var rightVal = float.Parse(right.Text, CultureInfo.InvariantCulture);
                        var bottomVal = float.Parse(bottom.Text, CultureInfo.InvariantCulture);
                        ValueChanged(new Box2(leftVal, bottomVal, rightVal, topVal));
                        break;
                    }
                    case BoxType.Box2i:
                    {
                        var leftVal = int.Parse(left.Text, CultureInfo.InvariantCulture);
                        var topVal = int.Parse(top.Text, CultureInfo.InvariantCulture);
                        var rightVal = int.Parse(right.Text, CultureInfo.InvariantCulture);
                        var bottomVal = int.Parse(bottom.Text, CultureInfo.InvariantCulture);
                        ValueChanged(new Box2i(leftVal, bottomVal, rightVal, topVal));
                        break;
                    }
                    case BoxType.UIBox2:
                    {
                        var leftVal = float.Parse(left.Text, CultureInfo.InvariantCulture);
                        var topVal = float.Parse(top.Text, CultureInfo.InvariantCulture);
                        var rightVal = float.Parse(right.Text, CultureInfo.InvariantCulture);
                        var bottomVal = float.Parse(bottom.Text, CultureInfo.InvariantCulture);
                        ValueChanged(new UIBox2(leftVal, topVal, rightVal, bottomVal));
                        break;
                    }
                    case BoxType.UIBox2i:
                    {
                        var leftVal = int.Parse(left.Text, CultureInfo.InvariantCulture);
                        var topVal = int.Parse(top.Text, CultureInfo.InvariantCulture);
                        var rightVal = int.Parse(right.Text, CultureInfo.InvariantCulture);
                        var bottomVal = int.Parse(bottom.Text, CultureInfo.InvariantCulture);
                        ValueChanged(new UIBox2i(leftVal, topVal, rightVal, bottomVal));
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (!ReadOnly)
            {
                left.OnTextEntered += OnEntered;
                top.OnTextEntered += OnEntered;
                right.OnTextEntered += OnEntered;
                bottom.OnTextEntered += OnEntered;
            }

            return hBoxContainer;
        }

        public enum BoxType
        {
            Box2,
            Box2i,
            UIBox2,
            UIBox2i,
        }
    }
}

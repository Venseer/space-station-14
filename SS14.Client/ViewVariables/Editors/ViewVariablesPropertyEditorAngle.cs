using System.Globalization;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.ViewVariables.Editors
{
    public class ViewVariablesPropertyEditorAngle : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object value)
        {
            var hBox = new HBoxContainer("ViewVariablesPropertyEditorAngle")
            {
                CustomMinimumSize = new Vector2(200, 0)
            };
            var angle = (Angle) value;
            var lineEdit = new LineEdit
            {
                Text = angle.Degrees.ToString(CultureInfo.InvariantCulture),
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand
            };
            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e =>
                    ValueChanged(Angle.FromDegrees(double.Parse(e.Text, CultureInfo.InvariantCulture)));
            }

            hBox.AddChild(lineEdit);
            hBox.AddChild(new Label {Text = "deg"});
            return hBox;
        }
    }
}

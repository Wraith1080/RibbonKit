using System.ComponentModel;

namespace RibbonKit.WinForms;

internal interface IRibbonEditorItem
{
    string Value { get; set; }
    int InputWidth { get; }
    int MaxLength { get; }
    object CaptureEdit();
    bool IsEditChanged(object baseline);
    void ValidateEdit(CancelEventArgs args);
    void AcceptEdit(bool changed);
    void CancelEdit(object baseline);
}

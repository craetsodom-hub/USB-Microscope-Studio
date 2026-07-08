using System.Collections.ObjectModel;
using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public sealed class AnnotationHistory
{
    private readonly Stack<IReadOnlyList<InspectionAnnotation>> _undo = [];
    private readonly Stack<IReadOnlyList<InspectionAnnotation>> _redo = [];

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Capture(IReadOnlyList<InspectionAnnotation> current)
    {
        _undo.Push(Clone(current));
        _redo.Clear();
    }

    public void Undo(ObservableCollection<InspectionAnnotation> annotations)
    {
        if (!CanUndo)
        {
            return;
        }

        _redo.Push(Clone(annotations));
        ReplaceWith(annotations, _undo.Pop());
    }

    public void Redo(ObservableCollection<InspectionAnnotation> annotations)
    {
        if (!CanRedo)
        {
            return;
        }

        _undo.Push(Clone(annotations));
        ReplaceWith(annotations, _redo.Pop());
    }

    private static IReadOnlyList<InspectionAnnotation> Clone(IEnumerable<InspectionAnnotation> annotations) =>
        annotations.Select(annotation => annotation with { Points = [.. annotation.Points] }).ToList();

    private static void ReplaceWith(ObservableCollection<InspectionAnnotation> target, IEnumerable<InspectionAnnotation> source)
    {
        target.Clear();
        foreach (var annotation in source)
        {
            target.Add(annotation);
        }
    }
}

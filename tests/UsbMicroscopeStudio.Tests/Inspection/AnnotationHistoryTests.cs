using System.Collections.ObjectModel;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Inspection;

public sealed class AnnotationHistoryTests
{
    [Fact]
    public void UndoRedo_RestoresAnnotationSnapshots()
    {
        var annotations = new ObservableCollection<InspectionAnnotation>();
        var history = new AnnotationHistory();
        var first = new InspectionAnnotation { Tool = InspectionTool.Line, Points = [new(0, 0), new(1, 1)] };
        var second = new InspectionAnnotation { Tool = InspectionTool.Rectangle, Points = [new(2, 2), new(3, 3)] };

        history.Capture(annotations);
        annotations.Add(first);
        history.Capture(annotations);
        annotations.Add(second);

        history.Undo(annotations);
        Assert.Single(annotations);
        Assert.Equal(first.Id, annotations[0].Id);

        history.Redo(annotations);
        Assert.Equal(2, annotations.Count);
        Assert.Equal(second.Id, annotations[1].Id);
    }
}

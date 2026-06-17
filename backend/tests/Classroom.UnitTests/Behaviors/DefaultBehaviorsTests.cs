using Classroom.Domain.Behaviors;

namespace Classroom.UnitTests.Behaviors;

public class DefaultBehaviorsTests
{
    [Fact]
    public void Template_is_non_empty_and_has_both_positive_and_negative_values()
    {
        Assert.NotEmpty(DefaultBehaviors.Template);
        Assert.Contains(DefaultBehaviors.Template, t => t.DefaultPoints > 0);
        Assert.Contains(DefaultBehaviors.Template, t => t.DefaultPoints < 0);
    }

    [Fact]
    public void CreateFor_materializes_one_row_per_template_entry_stamped_with_the_class_id()
    {
        var classId = Guid.NewGuid();

        var behaviors = DefaultBehaviors.CreateFor(classId);

        Assert.Equal(DefaultBehaviors.Template.Count, behaviors.Count);
        Assert.All(behaviors, b => Assert.Equal(classId, b.ClassId));
        Assert.All(behaviors, b => Assert.NotEqual(Guid.Empty, b.Id));

        // Names and points match the template, in order.
        var produced = behaviors.Select(b => (b.Name, b.DefaultPoints)).ToList();
        Assert.Equal(DefaultBehaviors.Template.ToList(), produced);
    }

    [Fact]
    public void CreateFor_produces_independent_rows_for_different_classes()
    {
        var first = DefaultBehaviors.CreateFor(Guid.NewGuid());
        var second = DefaultBehaviors.CreateFor(Guid.NewGuid());

        // Distinct entities with distinct ids — copies, not shared references.
        var firstIds = first.Select(b => b.Id).ToHashSet();
        Assert.DoesNotContain(second, b => firstIds.Contains(b.Id));
    }
}

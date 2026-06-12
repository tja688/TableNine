using QFramework;

public sealed class TriggerSkillSystemCommand : AbstractCommand
{
    public TriggerSkillSystemCommand(SkillTrigger trigger, TriggerContext context)
    {
        Trigger = trigger;
        Context = context;
    }

    public SkillTrigger Trigger { get; }
    public TriggerContext Context { get; }

    protected override void OnExecute()
    {
        this.GetSystem<ISkillSystem>().Trigger(Trigger, Context, this);
    }
}

using System.Collections.Generic;
using QFramework;

public interface INarrativeSystem : ISystem
{
    void EnqueueMessage(string message);
}

public sealed class NarrativeSystem : AbstractSystem, INarrativeSystem
{
    private readonly Queue<string> mPendingMessages = new Queue<string>();

    protected override void OnInit()
    {
        this.RegisterEvent<GameplayMessageEvent>(e => EnqueueMessage(e.Message));
        this.RegisterEvent<DialogueCompletedEvent>(_ => TryStartNextMessage());
    }

    public void EnqueueMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        mPendingMessages.Enqueue(message);
        TryStartNextMessage();
    }

    private void TryStartNextMessage()
    {
        var flowModel = this.GetModel<IFlowModel>();
        if (flowModel.HasLock(InputLockReason.DialogueRunning) || mPendingMessages.Count == 0)
        {
            return;
        }

        ((IBelongToArchitecture)this).GetArchitecture().SendCommand(new StartDialogueCommand(mPendingMessages.Dequeue()));
    }
}

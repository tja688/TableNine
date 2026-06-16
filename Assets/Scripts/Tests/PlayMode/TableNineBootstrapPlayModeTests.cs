using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineBootstrapPlayModeTests
{
    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        TableNine.ResetForTests();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        TableNine.ResetForTests();
        yield return null;
    }

    [UnityTest]
    public IEnumerator BootstrapScene_Starts_Without_ConsoleErrors()
    {
        var errors = new List<string>();
        Application.LogCallback captureErrors = (condition, stackTrace, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors.Add(condition);
            }
        };

        Application.logMessageReceived += captureErrors;
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return null;
        Application.logMessageReceived -= captureErrors;

        Assert.That(errors, Is.Empty);
    }

    [UnityTest]
    public IEnumerator GameplayBootstrap_Initializes_TableNine()
    {
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return null;

        Assert.That(TableNine.IsInitialized, Is.True);
        Assert.That(TableNine.Current, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator BootstrapScene_AutoStarts_Playable_Demo_And_Card_Visuals_Do_Not_Block_Slot_Input()
    {
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return WaitForOpeningDealToComplete(6f);

        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var occupiedSlots = Enumerable.Range(1, 9)
            .Count(slot => boardModel.GetCardAt(new BoardSlotNo(slot)).HasValue);
        var boardRoot = GameObject.Find("NineGrid Main CardSlots");
        var itemRoot = FindSceneObject("Item CardSlots");
        var dock = Object.FindObjectOfType<DockCardsWorldDemo>(true);
        var slot3 = GameObject.Find("NineGrid Main CardSlots/CardSlot3");
        var slot3Hits = Physics2D.OverlapPointAll(slot3.transform.position);

        Assert.That(runModel.IsRunActive.Value, Is.True);
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl));
        Assert.That(flowModel.IsInputLocked, Is.False);
        Assert.That(occupiedSlots, Is.EqualTo(9));
        Assert.That(boardRoot, Is.Not.Null);
        Assert.That(slot3.GetComponent<BoardSlotClickProxy>(), Is.Not.Null);
        Assert.That(slot3.GetComponent<Collider2D>().enabled, Is.True);
        Assert.That(slot3Hits.Any(hit => hit.GetComponent<BoardSlotClickProxy>() != null), Is.True);
        Assert.That(slot3Hits.Any(hit => hit.name.StartsWith("BoardCardView")), Is.False);
        Assert.That(dock, Is.Not.Null);
        Assert.That(dock.gameObject.activeInHierarchy, Is.True);
        Assert.That(dock.UsesRuntimeItemSlots, Is.True);
        Assert.That(itemRoot, Is.Not.Null);
        Assert.That(itemRoot.activeInHierarchy, Is.False);
        Assert.That(Object.FindObjectOfType<UIGameplayPanel>(true), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Picking_Board_Help_Card_Adds_A_Runtime_Dock_Card()
    {
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return WaitForOpeningDealToComplete(6f);

        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var dock = Object.FindObjectOfType<DockCardsWorldDemo>(true);
        CardUid? helpUid = null;
        for (var slot = 1; slot <= 9; slot++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            if (uid.HasValue &&
                collectionModel.TryGetCard(uid.Value, out var runtime) &&
                runtime.CardType == CardType.Help)
            {
                helpUid = uid.Value;
                break;
            }
        }

        Assert.That(dock, Is.Not.Null);
        Assert.That(helpUid.HasValue, Is.True, "Bootstrap board should include at least one help card.");

        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(helpUid.Value));
        yield return null;
        yield return null;

        Assert.That(deckModel.ItemSlots.Any(uid => uid.HasValue && uid.Value.Equals(helpUid.Value)), Is.True);
        Assert.That(dock.VisibleCardCount, Is.GreaterThanOrEqualTo(1));
    }

    private static IEnumerator WaitForOpeningDealToComplete(float timeoutSeconds)
    {
        var deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (TableNine.IsInitialized &&
                TableNine.Interface.GetModel<IFlowModel>().Phase.Value == FlowPhase.PlayerControl &&
                !TableNine.Interface.GetModel<IFlowModel>().IsInputLocked)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail("Timed out waiting for opening deal presentation to finish.");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        var objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (var i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null && objects[i].name == objectName && objects[i].scene.isLoaded)
            {
                return objects[i];
            }
        }

        return null;
    }
}

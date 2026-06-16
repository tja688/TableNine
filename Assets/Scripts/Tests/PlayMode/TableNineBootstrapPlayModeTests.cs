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
        yield return null;
        yield return null;

        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var occupiedSlots = Enumerable.Range(1, 9)
            .Count(slot => boardModel.GetCardAt(new BoardSlotNo(slot)).HasValue);
        var boardRoot = GameObject.Find("NineGrid Main CardSlots");
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
        Assert.That(Object.FindObjectOfType<UIGameplayPanel>(true), Is.Not.Null);
    }
}

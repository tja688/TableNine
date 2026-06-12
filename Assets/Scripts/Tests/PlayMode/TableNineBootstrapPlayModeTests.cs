using System.Collections;
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
        LogAssert.ignoreFailingMessages = false;
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return null;

        LogAssert.NoUnexpectedReceived();
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
    public IEnumerator BootstrapScene_Does_Not_Auto_Start_Legacy_Gameplay()
    {
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return null;

        var legacyBoardRoot = Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(transform => transform.name == "NineGrid CardSlots")
            ?.gameObject;

        Assert.That(TableNine.Interface.GetModel<IRunModel>().IsRunActive.Value, Is.False);
        Assert.That(UIKit.GetPanel<UIGameplayPanel>(), Is.Null);
        Assert.That(legacyBoardRoot, Is.Not.Null);
        Assert.That(legacyBoardRoot.activeSelf, Is.False);
        Assert.That(GameObject.Find("DeckCountText"), Is.Null);
        Assert.That(GameObject.Find("ClearBanner"), Is.Null);
        Assert.That(GameObject.Find("AttributeChoiceOverlay"), Is.Null);
    }
}

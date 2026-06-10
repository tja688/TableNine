using System;
using QFramework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class TableNineUIKitConfig : UIKitConfig
{
    public TableNineUIKitConfig(TableNineUIPanelRegistry registry)
    {
        PanelLoaderPool = new RegistryPanelLoaderPool(registry);
    }

    private sealed class RegistryPanelLoaderPool : AbstractPanelLoaderPool
    {
        private readonly TableNineUIPanelRegistry mRegistry;

        public RegistryPanelLoaderPool(TableNineUIPanelRegistry registry)
        {
            mRegistry = registry;
        }

        protected override IPanelLoader CreatePanelLoader()
        {
            return new RegistryPanelLoader(mRegistry);
        }
    }

    private sealed class RegistryPanelLoader : IPanelLoader
    {
        private readonly TableNineUIPanelRegistry mRegistry;
        private GameObject mPrefab;

        public RegistryPanelLoader(TableNineUIPanelRegistry registry)
        {
            mRegistry = registry;
        }

        public GameObject LoadPanelPrefab(PanelSearchKeys panelSearchKeys)
        {
            mPrefab = mRegistry != null ? mRegistry.GetPanelPrefab(panelSearchKeys) : null;
            if (mPrefab == null)
            {
                var panelName = panelSearchKeys.GameObjName ?? panelSearchKeys.PanelType?.Name ?? "<unknown>";
                throw new InvalidOperationException($"UIKit panel prefab is not registered: {panelName}");
            }

            return mPrefab;
        }

        public void LoadPanelPrefabAsync(PanelSearchKeys panelSearchKeys, Action<GameObject> onPanelPrefabLoad)
        {
            onPanelPrefabLoad?.Invoke(LoadPanelPrefab(panelSearchKeys));
        }

        public void Unload()
        {
            mPrefab = null;
        }
    }
}

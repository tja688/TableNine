using UnityEngine;

public sealed class BakedCardFaceDemo : MonoBehaviour
{
    [SerializeField] private float mRefreshInterval = 0.6f;
    [SerializeField] private float mPixelsPerUnit = 100f;
    [SerializeField] private string mSortingLayerName = "Default";
    [SerializeField] private int mSortingOrder = 250;

    private readonly BakedCardWorldView[] mViews = new BakedCardWorldView[3];
    private readonly BakedCardFaceRenderData[] mData = new BakedCardFaceRenderData[3];
    private BakedCardFaceComposer mComposer;
    private float mNextRefreshTime;
    private int mTick;

    private void Start()
    {
        mComposer = new BakedCardFaceComposer();
        CreateDemoCards();
        RebuildAllFaces();
    }

    private void Update()
    {
        if (Time.time < mNextRefreshTime)
        {
            return;
        }

        mNextRefreshTime = Time.time + mRefreshInterval;
        mTick++;

        for (var i = 0; i < mData.Length; i++)
        {
            mData[i].Attack = 1 + ((mTick + i * 2) % 12);
            mData[i].Defense = 2 + ((mTick * 2 + i) % 18);
            mData[i].Life = 10 + ((mTick * 3 + i * 5) % 35);
        }

        RebuildAllFaces();
    }

    private void OnDestroy()
    {
        if (mComposer != null)
        {
            mComposer.Dispose();
            mComposer = null;
        }
    }

    private void CreateDemoCards()
    {
        mData[0] = new BakedCardFaceRenderData
        {
            DisplayName = "Small Help",
            Template = BakedCardFaceTemplate.CardExample,
            Size = BakedCardFaceSize.Small,
            StatMode = BakedCardFaceStatMode.MainIconOnly,
            StampedIconCount = 3
        };

        mData[1] = new BakedCardFaceRenderData
        {
            DisplayName = "Medium Monster",
            Template = BakedCardFaceTemplate.CardExample,
            Size = BakedCardFaceSize.Medium,
            StatMode = BakedCardFaceStatMode.FullStats,
            StampedIconCount = 2
        };

        mData[2] = new BakedCardFaceRenderData
        {
            DisplayName = "Large Player",
            Template = BakedCardFaceTemplate.PlayerCard,
            Size = BakedCardFaceSize.Large,
            StatMode = BakedCardFaceStatMode.FullStats,
            StampedIconCount = 3
        };

        CreateView(0, "BakedCard_Small", new Vector3(-3.6f, 0.15f, 0f), 1.7f);
        CreateView(1, "BakedCard_Medium", new Vector3(-1.45f, 0.15f, 0f), 2.2f);
        CreateView(2, "BakedCard_Large", new Vector3(1.25f, 0.15f, 0f), 2.9f);
    }

    private void CreateView(int index, string name, Vector3 localPosition, float worldHeight)
    {
        var viewObject = new GameObject(name);
        viewObject.transform.SetParent(transform, false);
        viewObject.transform.localPosition = localPosition;

        var view = viewObject.AddComponent<BakedCardWorldView>();
        view.Initialize(mSortingLayerName, mSortingOrder + index, worldHeight);
        mViews[index] = view;
    }

    private void RebuildAllFaces()
    {
        if (mComposer == null)
        {
            return;
        }

        for (var i = 0; i < mViews.Length; i++)
        {
            var face = mComposer.Compose(mData[i], mPixelsPerUnit, out var back);
            mViews[i].SetSprites(face, back);
        }
    }
}

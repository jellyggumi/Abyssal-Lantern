using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace CastleBusters
{
    public static class MobileStoreCatalog
    {
        // This identifier must be created unchanged in both App Store Connect and Google Play Console.
        public const string ChronicleProductId = "com.jangyoung.unknowncastle.chronicle_pack";

        public static ProductDefinition CreateChronicleProductDefinition()
        {
            return new ProductDefinition(ChronicleProductId, ProductType.NonConsumable);
        }
    }

    /// <summary>
    /// Explicitly non-authoritative prototype replay cache for the Chronicle product.
    /// It is neither platform ownership nor payment proof.
    /// </summary>
    public static class MobileStoreEntitlements
    {
        private const string ChroniclePackKey = "CastleBusters.MobileStore.ChroniclePack";

        public static bool HasChroniclePack => PlayerPrefs.GetInt(ChroniclePackKey, 0) == 1;

        public static void GrantChroniclePack()
        {
            if (HasChroniclePack) return;
            PlayerPrefs.SetInt(ChroniclePackKey, 1);
            PlayerPrefs.Save();
        }

        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(ChroniclePackKey);
            PlayerPrefs.Save();
        }
    }

    public enum MobileStorefrontState
    {
        NotStarted,
        Initializing,
        Ready,
        Purchasing,
        Restoring,
        Unavailable,
        Failed,
    }

    /// <summary>
    /// Unity IAP 5 adapter for the native App Store and Google Play billing flows.
    /// It owns no gameplay state: the single product only unlocks a replayable prologue.
    /// </summary>
    public sealed class MobileStorefront : MonoBehaviour
    {
        private static MobileStorefront instance;

        private readonly HashSet<string> inFlightConfirmationTransactionIds = new HashSet<string>();
        private StoreController controller;
        private MobileStorefrontState state = MobileStorefrontState.NotStarted;
        private string statusMessage = "스토어 연결 준비 중";
        private int revision;

        public static MobileStorefront Instance => instance;
        public MobileStorefrontState State => state;
        public string StatusMessage => statusMessage;
        public int Revision => revision;
        public bool HasChroniclePack => MobileStoreEntitlements.HasChroniclePack;
        public bool CanPurchase => state == MobileStorefrontState.Ready && !HasChroniclePack && GetChronicleProduct() != null && GetChronicleProduct().availableToPurchase;

        public string ChroniclePrice
        {
            get
            {
                var product = GetChronicleProduct();
                return product != null && product.metadata != null ? product.metadata.localizedPriceString : string.Empty;
            }
        }

        public static MobileStorefront EnsureInstance()
        {
            if (instance != null) return instance;

            instance = FindObjectOfType<MobileStorefront>();
            if (instance != null) return instance;

            var go = new GameObject("MobileStorefront");
            instance = go.AddComponent<MobileStorefront>();
            return instance;
        }

        public static void OpenStore()
        {
            var storefront = EnsureInstance();
            storefront.InitializeIfSupported();
            MobileStorefrontPanel.Open(storefront);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeIfSupported();
        }

        private void OnDestroy()
        {
            UnregisterStoreCallbacks();
            if (instance == this) instance = null;
        }

        public void InitializeIfSupported()
        {
            if (controller != null || state == MobileStorefrontState.Initializing) return;

#if UNITY_EDITOR
            SetState(MobileStorefrontState.Unavailable, "모바일 스토어 결제는 Google Play 또는 App Store 빌드에서 사용할 수 있습니다.");
            return;
#else
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
            {
                SetState(MobileStorefrontState.Unavailable, "모바일 스토어 결제는 Google Play 또는 App Store 빌드에서 사용할 수 있습니다.");
                return;
            }

            SetState(MobileStorefrontState.Initializing, "스토어 상품을 불러오는 중…");
            controller = UnityIAPServices.StoreController();
            controller.ProcessPendingOrdersOnPurchasesFetched(false);
            RegisterStoreCallbacks();
            ConnectStoreAsync();
#endif
        }

        private async void ConnectStoreAsync()
        {
            try
            {
                await controller.Connect();
            }
            catch (Exception exception)
            {
                SetState(MobileStorefrontState.Failed, $"스토어 연결 실패: {exception.Message}");
            }
        }

        public void BeginChroniclePurchase()
        {
            if (HasChroniclePack)
            {
                SetState(MobileStorefrontState.Ready, "이미 Chronicle Edition을 소장하고 있습니다.");
                return;
            }

            InitializeIfSupported();
            if (state != MobileStorefrontState.Ready || controller == null) return;

            var product = GetChronicleProduct();
            if (product == null || !product.availableToPurchase)
            {
                SetState(MobileStorefrontState.Failed, "이 상품은 현재 스토어에서 구매할 수 없습니다.");
                return;
            }

            SetState(MobileStorefrontState.Purchasing, "스토어 승인을 기다리는 중…");
            controller.PurchaseProduct(product);
        }

        public void RestorePurchases()
        {
            InitializeIfSupported();
            if (state != MobileStorefrontState.Ready || controller == null) return;

            SetState(MobileStorefrontState.Restoring, "기존 구매 내역을 복원하는 중…");
            controller.RestoreTransactions(OnRestoreComplete);
        }

        private void RegisterStoreCallbacks()
        {
            controller.OnStoreConnected += OnStoreConnected;
            controller.OnStoreDisconnected += OnStoreDisconnected;
            controller.OnProductsFetched += OnProductsFetched;
            controller.OnProductsFetchFailed += OnProductsFetchFailed;
            controller.OnPurchasesFetched += OnPurchasesFetched;
            controller.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            controller.OnPurchasePending += OnPurchasePending;
            controller.OnPurchaseDeferred += OnPurchaseDeferred;
            controller.OnPurchaseConfirmed += OnPurchaseConfirmed;
            controller.OnPurchaseFailed += OnPurchaseFailed;
        }

        private void UnregisterStoreCallbacks()
        {
            if (controller == null) return;

            controller.OnStoreConnected -= OnStoreConnected;
            controller.OnStoreDisconnected -= OnStoreDisconnected;
            controller.OnProductsFetched -= OnProductsFetched;
            controller.OnProductsFetchFailed -= OnProductsFetchFailed;
            controller.OnPurchasesFetched -= OnPurchasesFetched;
            controller.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            controller.OnPurchasePending -= OnPurchasePending;
            controller.OnPurchaseDeferred -= OnPurchaseDeferred;
            controller.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            controller.OnPurchaseFailed -= OnPurchaseFailed;
        }

        private void OnStoreConnected()
        {
            controller.FetchProducts(new List<ProductDefinition>
            {
                MobileStoreCatalog.CreateChronicleProductDefinition(),
            });
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            SetState(MobileStorefrontState.Failed, $"스토어 연결 실패: {failure.Message}");
        }

        private void OnProductsFetched(List<Product> products)
        {
            if (GetChronicleProduct() == null)
            {
                SetState(MobileStorefrontState.Failed, "Chronicle Edition 상품을 스토어에서 찾을 수 없습니다.");
                return;
            }

            controller.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            SetState(MobileStorefrontState.Failed, $"스토어 상품을 불러오지 못했습니다: {failure.FailureReason}");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            var restoredChroniclePurchase = false;
            foreach (var pendingOrder in orders.PendingOrders)
            {
                if (!IsSingleChronicleOrder(pendingOrder))
                {
                    SetState(MobileStorefrontState.Failed, "알 수 없는 스토어 상품을 받았습니다.");
                    return;
                }

                restoredChroniclePurchase = true;
                OnPurchasePending(pendingOrder);
                if (state == MobileStorefrontState.Failed) return;
            }

            foreach (var confirmedOrder in orders.ConfirmedOrders)
            {
                if (!IsSingleChronicleOrder(confirmedOrder)) continue;

                MobileStoreEntitlements.GrantChroniclePack();
                restoredChroniclePurchase = true;
            }
            if (inFlightConfirmationTransactionIds.Count > 0)
            {
                SetState(MobileStorefrontState.Purchasing, "스토어 승인을 기다리는 중…");
                return;
            }


            if (state == MobileStorefrontState.Restoring)
            {
                SetState(MobileStorefrontState.Ready, restoredChroniclePurchase
                    ? "기존 구매 내역을 복원했습니다."
                    : "복원할 Chronicle Edition 구매 내역이 없습니다.");
                return;
            }

            SetState(MobileStorefrontState.Ready, HasChroniclePack
                ? "Chronicle Edition을 소장하고 있습니다."
                : "스토어 연결 완료");
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            SetState(MobileStorefrontState.Failed, $"구매 내역 조회 실패: {failure.FailureReason} · {failure.Message}");
        }

        private void OnPurchasePending(PendingOrder pendingOrder)
        {
            if (!IsSingleChronicleOrder(pendingOrder))
            {
                SetState(MobileStorefrontState.Failed, "알 수 없는 스토어 상품을 받았습니다.");
                return;
            }

            if (controller == null) return;

            var transactionId = pendingOrder.Info.TransactionID;
            if (string.IsNullOrEmpty(transactionId))
            {
                SetState(MobileStorefrontState.Failed, "구매 거래 ID를 확인할 수 없어 구매를 적용하지 않았습니다.");
                return;
            }

            if (!inFlightConfirmationTransactionIds.Add(transactionId)) return;

            MobileStoreEntitlements.GrantChroniclePack();
            controller.ConfirmPurchase(pendingOrder);
        }

        private void OnPurchaseDeferred(DeferredOrder deferredOrder)
        {
            if (!IsSingleChronicleOrder(deferredOrder))
            {
                SetState(MobileStorefrontState.Failed, "알 수 없는 스토어 상품을 받았습니다.");
                return;
            }

            SetState(MobileStorefrontState.Purchasing, "보호자 승인을 기다리는 중…");
        }

        private void OnPurchaseConfirmed(Order confirmedOrder)
        {
            if (confirmedOrder is FailedOrder failedOrder)
            {
                OnPurchaseFailed(failedOrder);
                return;
            }

            if (confirmedOrder is ConfirmedOrder && IsSingleChronicleOrder(confirmedOrder))
            {
                var transactionId = confirmedOrder.Info.TransactionID;
                if (!string.IsNullOrEmpty(transactionId))
                {
                    inFlightConfirmationTransactionIds.Remove(transactionId);
                }

                if (inFlightConfirmationTransactionIds.Count == 0)
                {
                    SetState(MobileStorefrontState.Ready, "Chronicle Edition이 영구 해금되었습니다.");
                    return;
                }

                SetState(MobileStorefrontState.Purchasing, "스토어 승인을 기다리는 중…");
            }
        }

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            var transactionId = failedOrder.Info.TransactionID;
            if (!string.IsNullOrEmpty(transactionId))
            {
                inFlightConfirmationTransactionIds.Remove(transactionId);
            }

            if (inFlightConfirmationTransactionIds.Count > 0)
            {
                SetState(MobileStorefrontState.Purchasing, "스토어 승인을 기다리는 중…");
                return;
            }

            SetState(MobileStorefrontState.Failed,
                $"구매 실패: {failedOrder.FailureReason} · {failedOrder.Details}");
        }

        private Product GetChronicleProduct()
        {
            return controller != null ? controller.GetProductById(MobileStoreCatalog.ChronicleProductId) : null;
        }

        private static bool IsSingleChronicleOrder(Order order)
        {
            if (order == null || order.CartOrdered == null) return false;

            var items = order.CartOrdered.Items();
            if (items == null || items.Count != 1) return false;
            var item = items[0];

            return item != null
                && item.Quantity == 1
                && item.Product != null
                && item.Product.definition != null
                && item.Product.definition.id == MobileStoreCatalog.ChronicleProductId
                && item.Product.definition.type == ProductType.NonConsumable;
        }

        private void OnRestoreComplete(bool succeeded, string error)
        {
            if (!succeeded)
            {
                SetState(MobileStorefrontState.Failed, $"구매 복원 실패: {error}");
                return;
            }

            controller.FetchPurchases();
        }

        private void SetState(MobileStorefrontState nextState, string nextMessage)
        {
            state = nextState;
            statusMessage = nextMessage;
            revision++;
        }
    }

    /// <summary>
    /// Runtime store presentation. Price text always comes from the platform product metadata.
    /// </summary>
    public sealed class MobileStorefrontPanel : MonoBehaviour
    {
        private static MobileStorefrontPanel activePanel;

        private MobileStorefront storefront;
        private TextMeshProUGUI ownershipLabel;
        private TextMeshProUGUI priceLabel;
        private TextMeshProUGUI statusLabel;
        private Button purchaseButton;
        private Button restoreButton;
        private int renderedRevision = -1;

        public static void Open(MobileStorefront storefront)
        {
            if (activePanel != null)
            {
                activePanel.storefront = storefront;
                activePanel.Refresh(force: true);
                return;
            }

            var go = new GameObject("MobileStorefrontPanel");
            activePanel = go.AddComponent<MobileStorefrontPanel>();
            activePanel.storefront = storefront;
            activePanel.Build();
        }

        private void OnDestroy()
        {
            if (activePanel == this) activePanel = null;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;
            var dim = CreateChild<Image>("Dim", canvas.transform);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.82f);
            MobileSafeArea.ConfigureCanvas(canvas);
            var contentRoot = MobileSafeArea.GetContentRoot(canvas);

            var panel = CreateChild<Image>("StoreCard", contentRoot);
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(920f, 640f);
            panel.color = new Color(0.1f, 0.08f, 0.16f, 0.98f);
            var cardSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (cardSprite != null)
            {
                panel.sprite = cardSprite;
                panel.type = Image.Type.Sliced;
            }

            var title = CreateText("Title", panel.transform, new Vector2(0.5f, 0.82f), new Vector2(760f, 90f), 52f);
            title.text = "CHRONICLE EDITION";
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.84f, 0.42f, 1f);

            ownershipLabel = CreateText("Entitlement", panel.transform, new Vector2(0.5f, 0.67f), new Vector2(760f, 64f), 30f);
            ownershipLabel.color = new Color(0.92f, 0.96f, 1f, 1f);

            var description = CreateText("Description", panel.transform, new Vector2(0.5f, 0.53f), new Vector2(760f, 116f), 28f);
            description.text = "영구 소장 · 프롤로그 다시보기 해금\n게임 수치·전투력에는 영향을 주지 않습니다.";
            description.enableWordWrapping = true;
            description.color = new Color(0.84f, 0.89f, 0.96f, 1f);

            priceLabel = CreateText("LocalizedPrice", panel.transform, new Vector2(0.5f, 0.37f), new Vector2(600f, 52f), 32f);
            priceLabel.fontStyle = FontStyles.Bold;
            priceLabel.color = new Color(0.98f, 0.94f, 0.78f, 1f);

            purchaseButton = BuildButton("PurchaseButton", panel.transform, new Vector2(0.5f, 0.235f), "스토어 구매", new Color(0.96f, 0.68f, 0.25f, 1f), () => storefront.BeginChroniclePurchase());
            restoreButton = BuildButton("RestorePurchasesButton", panel.transform, new Vector2(0.31f, 0.105f), "구매 복원", new Color(0.44f, 0.68f, 0.95f, 1f), () => storefront.RestorePurchases());
            BuildButton("CloseButton", panel.transform, new Vector2(0.69f, 0.105f), "닫기", new Color(0.45f, 0.5f, 0.62f, 1f), () => Destroy(gameObject));

            statusLabel = CreateText("StoreStatus", panel.transform, new Vector2(0.5f, -0.05f), new Vector2(760f, 52f), 21f);
            statusLabel.color = new Color(0.82f, 0.87f, 0.96f, 1f);
            statusLabel.enableWordWrapping = true;

            Refresh(force: true);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        private void Refresh(bool force)
        {
            if (storefront == null) return;
            if (!force && renderedRevision == storefront.Revision) return;
            renderedRevision = storefront.Revision;

            var owned = storefront.HasChroniclePack;
            ownershipLabel.text = owned ? "소장됨 · PROLOGUE REPLAY UNLOCKED" : "미소장 · ONE-TIME NON-CONSUMABLE";
            priceLabel.text = string.IsNullOrEmpty(storefront.ChroniclePrice)
                ? "현지 통화 가격을 불러오는 중…"
                : storefront.ChroniclePrice;
            statusLabel.text = storefront.StatusMessage;
            purchaseButton.interactable = storefront.CanPurchase;
            restoreButton.interactable = storefront.State == MobileStorefrontState.Ready;
        }

        private static Button BuildButton(string name, Transform parent, Vector2 anchor, string label, Color color, Action onClick)
        {
            var image = CreateChild<Image>(name, parent);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
            image.rectTransform.sizeDelta = new Vector2(320f, 74f);
            var sprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            image.color = color;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var text = CreateText("Label", image.transform, new Vector2(0.5f, 0.5f), new Vector2(280f, 58f), 28f);
            text.text = label;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.08f, 0.05f, 0.03f, 1f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 28f;
            return button;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchor, Vector2 size, float fontSize)
        {
            var text = CreateChild<TextMeshProUGUI>(name, parent);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor;
            text.rectTransform.sizeDelta = size;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);
            return text;
        }

        private static T CreateChild<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

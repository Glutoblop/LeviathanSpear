using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeviathanSpear
{
    // Tracks spears we've thrown that are lying in the world, draws a countdown icon over each one in range,
    // and recalls ready ones when a dodge roll starts.
    internal class SpearTracker : MonoBehaviour
    {
        private const float RecallRetrySeconds = 1.5f; // how long to keep asking for ownership after a roll

        private class Entry
        {
            public ItemDrop Drop;
            public float LandedAt;
            public float RecallUntil = -1f; // >0 while we're waiting to own the drop so we can pick it up
            public RectTransform Root;
            public Image Fill;
            public Image Icon;
        }

        private static readonly List<Entry> Entries = new List<Entry>();
        private static Sprite _ringSprite;
        private static Sprite _discSprite;

        private readonly List<ItemDrop.ItemData> _pendingAutoEquips = new List<ItemDrop.ItemData>();
        private bool _wasDodging;

        internal static void Register(ItemDrop drop)
        {
            foreach (var e in Entries)
                if (e.Drop == drop) return;
            Entries.Add(new Entry { Drop = drop, LandedAt = Time.time });
        }

        private void OnDestroy()
        {
            foreach (var e in Entries) DestroyUi(e);
            Entries.Clear();
        }

        private void LateUpdate()
        {
            Player player = Player.m_localPlayer;

            // Forget spears that were picked up, destroyed or unloaded.
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                if (Entries[i].Drop == null || !Entries[i].Drop.m_nview || !Entries[i].Drop.m_nview.IsValid())
                {
                    DestroyUi(Entries[i]);
                    Entries.RemoveAt(i);
                }
            }

            if (!Plugin.Enabled.Value || player == null || player.IsDead() || Hud.instance == null)
            {
                foreach (var e in Entries) Hide(e);
                _pendingAutoEquips.Clear();
                _wasDodging = false;
                return;
            }

            float radius = Plugin.Radius.Value;
            Vector3 playerPos = player.transform.position;

            bool dodging = player.InDodge();
            if (dodging && !_wasDodging && Plugin.RecallEnabled.Value)
            {
                foreach (var e in Entries)
                    if (IsReady(e) && InRange(e, playerPos, radius))
                        e.RecallUntil = Time.time + RecallRetrySeconds;
            }
            _wasDodging = dodging;

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Entry e = Entries[i];
                if (e.RecallUntil > 0f)
                {
                    if (Time.time > e.RecallUntil) e.RecallUntil = -1f;
                    else if (TryRecall(player, e))
                    {
                        DestroyUi(e);
                        Entries.RemoveAt(i);
                        continue;
                    }
                }

                if (InRange(e, playerPos, radius)) Draw(e);
                else Hide(e);
            }

            // EquipItem rejects equipment changes during a dodge.
            if (!dodging)
            {
                foreach (var item in _pendingAutoEquips)
                {
                    if (!CanAutoEquip(player)) break;
                    player.EquipItem(item);
                }
                _pendingAutoEquips.Clear();
            }
        }

        private static bool IsReady(Entry e) => Time.time - e.LandedAt >= Plugin.RecallDelay.Value;

        private static bool InRange(Entry e, Vector3 playerPos, float radius) =>
            (e.Drop.transform.position - playerPos).sqrMagnitude <= radius * radius;

        // Same steps as Humanoid.Pickup, but into a specific empty hotbar slot (row 0).
        private bool TryRecall(Player player, Entry e)
        {
            Inventory inv = player.GetInventory();
            int slot = -1;
            for (int x = 0; x < inv.GetWidth(); x++)
            {
                if (inv.GetItemAt(x, 0) == null) { slot = x; break; }
            }
            if (slot < 0)
            {
                e.RecallUntil = -1f;
                player.Message(MessageHud.MessageType.Center, "No empty hotbar slot for your spear");
                return false;
            }

            ItemDrop drop = e.Drop;
            drop.Load();
            if (!drop.CanPickup(false))
            {
                drop.RequestOwn(); // retried next frame until RecallUntil runs out
                return false;
            }

            ItemDrop.ItemData item = drop.m_itemData;
            if (inv.ContainsItem(item) || !inv.AddItem(item, new Vector2i(slot, 0)))
            {
                e.RecallUntil = -1f;
                return false;
            }

            ZNetScene.instance.Destroy(drop.gameObject);
            if (item.IsWeapon() && CanAutoEquip(player))
                _pendingAutoEquips.Add(item);

            player.m_pickupEffects.Create(player.transform.position, Quaternion.identity, null, 1f, -1, player.GetZDOID());
            player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, 1, item.GetIcon());
            return true;
        }

        // Match the hand checks in Humanoid.Pickup.
        private static bool CanAutoEquip(Player player) =>
            player.m_rightItem == null && player.m_hiddenRightItem == null
            && (player.m_leftItem == null || !player.m_leftItem.IsTwoHanded())
            && (player.m_hiddenLeftItem == null || !player.m_hiddenLeftItem.IsTwoHanded());

        // ---- UI -------------------------------------------------------------------------------------------

        private static void Draw(Entry e)
        {
            Camera cam = Utils.GetMainCamera();
            if (cam == null) { Hide(e); return; }

            Vector3 world = e.Drop.transform.position + Vector3.up * Plugin.IconHeight.Value;
            // Scaled: the main camera may render below screen resolution (render scale), like vanilla EnemyHud.
            Vector3 screen = cam.WorldToScreenPointScaled(world);
            if (screen.z <= 0f) { Hide(e); return; }

            if (e.Root == null && !Build(e)) return;

            var parent = (RectTransform)e.Root.parent;
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out Vector2 local))
            {
                Hide(e);
                return;
            }

            e.Root.localPosition = new Vector3(local.x, local.y, 0f);
            float s = Plugin.IconSize.Value;
            e.Root.sizeDelta = new Vector2(s, s);

            float delay = Plugin.RecallDelay.Value;
            float progress = delay <= 0f ? 1f : Mathf.Clamp01((Time.time - e.LandedAt) / delay);
            e.Fill.fillAmount = progress;
            e.Fill.color = progress >= 1f ? Plugin.RingReadyColor.Value : Plugin.RingColor.Value;
            if (e.Icon.sprite == null) e.Icon.sprite = e.Drop.m_itemData?.GetIcon();

            if (!e.Root.gameObject.activeSelf) e.Root.gameObject.SetActive(true);
        }

        private static void Hide(Entry e)
        {
            if (e.Root != null && e.Root.gameObject.activeSelf) e.Root.gameObject.SetActive(false);
        }

        private static void DestroyUi(Entry e)
        {
            if (e.Root != null) Object.Destroy(e.Root.gameObject);
            e.Root = null;
        }

        private static bool Build(Entry e)
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null) return false;
            EnsureSprites();

            Transform parent = hud.m_rootObject.transform;
            var go = new GameObject("LeviathanSpear_Icon", typeof(RectTransform), typeof(CanvasGroup));
            go.layer = parent.gameObject.layer;
            e.Root = (RectTransform)go.transform;
            e.Root.SetParent(parent, false);
            e.Root.SetAsFirstSibling(); // behind the rest of the HUD
            e.Root.anchorMin = e.Root.anchorMax = new Vector2(0.5f, 0.5f);
            e.Root.pivot = new Vector2(0.5f, 0.5f);
            var group = go.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            Image disc = NewImage("Back", e.Root, _discSprite, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            disc.color = new Color(0f, 0f, 0f, 0.35f);

            Image track = NewImage("Track", e.Root, _ringSprite, Vector2.zero, Vector2.one);
            track.color = Plugin.RingBackgroundColor.Value;

            e.Fill = NewImage("Sweep", e.Root, _ringSprite, Vector2.zero, Vector2.one);
            e.Fill.type = Image.Type.Filled;
            e.Fill.fillMethod = Image.FillMethod.Radial360;
            e.Fill.fillOrigin = (int)Image.Origin360.Top;
            e.Fill.fillClockwise = true;
            e.Fill.fillAmount = 0f;

            e.Icon = NewImage("Icon", e.Root, e.Drop.m_itemData?.GetIcon(), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            e.Icon.preserveAspect = true;
            return true;
        }

        private static Image NewImage(string name, RectTransform parent, Sprite sprite, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static void EnsureSprites()
        {
            if (_ringSprite == null) _ringSprite = MakeCircleSprite("LeviathanSpear_Ring", 0.78f);
            if (_discSprite == null) _discSprite = MakeCircleSprite("LeviathanSpear_Disc", 0f);
        }

        private static Sprite MakeCircleSprite(string name, float innerRadius)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            float aa = 1.5f / c;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01((1f - r) / aa);
                    if (innerRadius > 0f)
                        a = Mathf.Min(a, Mathf.Clamp01((r - innerRadius) / aa));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Object.DontDestroyOnLoad(tex);
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            Object.DontDestroyOnLoad(sprite);
            return sprite;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

// Person 1 — Economy composition root.
// DefenseModuleInstaller.Start() calls Initialize(context) after all Awakes.
public class EconomyFeature : DefenseModule
{
    private CowHealth[] cowHealths;

    private void Awake()
    {
        RefreshCows();
    }

    public void RefreshCows()
    {
        // Ưu tiên tìm trong module hierarchy
        cowHealths = GetComponentsInChildren<CowHealth>(true);

        // Nếu prefab module chưa chứa bò mà bò đặt sẵn ở scene workspace
        if (cowHealths == null || cowHealths.Length == 0)
        {
            cowHealths = FindObjectsByType<CowHealth>(FindObjectsInactive.Include);
        }
    }

    protected override void OnConnected()
    {
        RefreshCows();
        if (cowHealths != null)
        {
            foreach (var cow in cowHealths)
            {
                if (cow != null)
                {
                    cow.RegisterWith(Context);
                }
            }
        }

        Debug.Log($"[EconomyFeature] Connected to DefenseSceneContext. Registered {cowHealths?.Length ?? 0} cows.", this);
    }

    protected override void OnDisconnected()
    {
        if (cowHealths != null)
        {
            foreach (var cow in cowHealths)
            {
                if (cow != null)
                {
                    cow.Unregister();
                }
            }
        }

        Debug.Log("[EconomyFeature] Disconnected from DefenseSceneContext.", this);
    }

    /// <summary>
    /// Đăng ký một con bò mới (ví dụ bò vừa mua ở CowPurchaseShop) để ruồi có thể lây bệnh.
    /// </summary>
    public bool RegisterCow(CowHealth cow)
    {
        if (cow == null) return false;
        if (cowHealths == null) cowHealths = new CowHealth[0];

        foreach (var registered in cowHealths)
        {
            if (registered == cow) return false;
        }

        var list = new List<CowHealth>(cowHealths) { cow };
        cowHealths = list.ToArray();

        if (Context != null)
        {
            cow.RegisterWith(Context);
            return true;
        }

        return false;
    }

    // --- Context Menus for Testing in Inspector ---
    [ContextMenu("Test/Infect All Cows (Lây bệnh cho tất cả bò)")]
    public void TestInfectAllCows()
    {
        RefreshCows();
        if (cowHealths != null)
        {
            foreach (var cow in cowHealths)
            {
                if (cow != null) cow.Infect();
            }
        }
    }

    [ContextMenu("Test/Heal All Cows (Chữa khỏi cho tất cả bò)")]
    public void TestHealAllCows()
    {
        RefreshCows();
        if (cowHealths != null)
        {
            foreach (var cow in cowHealths)
            {
                if (cow != null) cow.TestHeal();
            }
        }
    }

    [ContextMenu("Test/Add Test Resources (+100 Credits, +10 Medicine, +20 Feed)")]
    public void TestAddResources()
    {
        if (Context != null && Context.Inventory != null)
        {
            Context.Inventory.Add(ItemType.Credits, 100);
            Context.Inventory.Add(ItemType.Medicine, 10);
            Context.Inventory.Add(ItemType.Feed, 20);
            Debug.Log("[EconomyFeature] Đã cộng 100 Credits, 10 Medicine, 20 Feed vào kho.", this);
        }
    }
}

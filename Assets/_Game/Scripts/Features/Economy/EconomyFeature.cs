using System.Collections.Generic;
using UnityEngine;

// Person 1 — Economy composition root.
public class EconomyFeature : DefenseModule
{
    private CowHealth[] cowHealths;

    private void Awake() => RefreshCows();

    public void RefreshCows()
    {
        cowHealths = GetComponentsInChildren<CowHealth>(true);
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
                if (cow != null) cow.RegisterWith(Context);
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
                if (cow != null) cow.Unregister();
            }
        }

        Debug.Log("[EconomyFeature] Disconnected from DefenseSceneContext.", this);
    }

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

    [ContextMenu("Test/Infect All Cows")]
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

    [ContextMenu("Test/Heal All Cows")]
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

    [ContextMenu("Test/Add Test Resources")]
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

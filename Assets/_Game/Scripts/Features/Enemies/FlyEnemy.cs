using UnityEngine;

// Ruồi gây bệnh: bay thẳng tới bò gần nhất (ưu tiên bò khỏe), chạm bò thì Infect() 1 lần rồi biến mất.
// Hết bò thì bay về thành (castleDamage của ruồi hiện là 0).
public class FlyEnemy : EnemyController
{
    private ICowDiseaseTarget targetCow;

    public override bool IsFlying => true;

    protected override void OnSpawned() { targetCow = null; }

    protected override void Move()
    {
        if (!CanInfect(targetCow)) targetCow = FindCow();   // bò cũ mất / hết bệnh thêm được thì chọn lại

        if (targetCow == null)
        {
            if (MoveTowards(Context.CastleTarget.position)) ReachCastle();
        }
        else if (MoveTowards(targetCow.TargetTransform.position))
        {
            targetCow.Infect();   // ruồi chỉ gọi Infect, mức bệnh do phần bò quản lý
            Despawn();
        }
    }

    // Bò gần nhất; bò đã bệnh bị cộng thêm 1000 vào khoảng cách để bò khỏe luôn được chọn trước
    private ICowDiseaseTarget FindCow()
    {
        ICowDiseaseTarget best = null;
        float bestScore = float.MaxValue;
        foreach (ICowDiseaseTarget cow in Context.Cows)
        {
            if (!CanInfect(cow)) continue;
            float score = Vector3.Distance(transform.position, cow.TargetTransform.position) + (cow.IsHealthy ? 0f : 1000f);
            if (score < bestScore)
            {
                bestScore = score;
                best = cow;
            }
        }
        return best;
    }

    // Bò còn tồn tại (chưa bị Destroy) và còn bệnh thêm được
    private static bool CanInfect(ICowDiseaseTarget cow)
    {
        if (cow == null || (cow is Object unityObject && unityObject == null)) return false;
        return cow.TargetTransform != null && cow.CanBeInfected;
    }
}

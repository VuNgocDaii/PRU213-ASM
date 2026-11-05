using System;

[Serializable]
public class OrderData
{
    public string orderName;
    public int requiredBottledMilk;
    public int rewardCredits;

    public OrderData(string name, int milkAmount, int creditsReward)
    {
        orderName = name;
        requiredBottledMilk = milkAmount;
        rewardCredits = creditsReward;
    }
}
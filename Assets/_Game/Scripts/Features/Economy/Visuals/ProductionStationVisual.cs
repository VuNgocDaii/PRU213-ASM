using UnityEngine;

// Artwork adapter: production timers, ingredients and rewards stay in ProductionMachine.
public class ProductionStationVisual : MonoBehaviour
{
    [SerializeField] private ProductionMachine machine;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1)] private float framesPerSecond = 7;
    private float animationTime;
    private void LateUpdate()
    {
        if (machine == null || body == null || frames == null || frames.Length < 6) return;
        int frame = 0;
        if (machine.IsReady) frame = 5;
        else if (machine.IsProcessing && machine.HasPower)
        {
            animationTime += Time.deltaTime;
            frame = 1 + (int)(animationTime * framesPerSecond) % 4;
        }
        else if (!machine.IsProcessing) animationTime = 0;
        body.sprite = frames[frame];
        body.color = machine.HasPower ? Color.white : new Color(0.55f,0.6f,0.65f);
    }
}

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PowerSource : MonoBehaviour
{
    [SerializeField] private bool hasPower = true;
    public bool HasPower { get { return hasPower; } }
    public event Action<bool> OnPowerChanged;

    public void SetPower(bool value)
    {
        if (hasPower == value) return;
        hasPower = value;
        OnPowerChanged?.Invoke(value);
    }

    [ContextMenu("Power On")]
    private void PowerOn() { SetPower(true); }

    [ContextMenu("Power Off")]
    private void PowerOff() { SetPower(false); }
}

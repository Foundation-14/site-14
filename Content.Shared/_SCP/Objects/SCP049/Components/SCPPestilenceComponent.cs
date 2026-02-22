// Content.Shared._SCP.SCP049.Components.PestilenceComponent.cs
using Robust.Shared.GameStates;

namespace Content.Shared._SCP.SCP049.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SCPPestilenceComponent : Component
{
    /// <summary>
    /// Множитель шанса заражения для этой цели.
    /// Например: 1.5 = +50% шанс, 0.5 = -50% шанс, 1.0 = без изменений
    /// </summary>
    [DataField("infectionChanceMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float InfectionChanceMultiplier = 1.0f;
    
    /// <summary>
    /// Аддитивная модификация шанса заражения.
    /// Например: 0.2 = +20% шанс, -0.2 = -20% шанс
    /// </summary>
    [DataField("infectionChanceBonus"), ViewVariables(VVAccess.ReadWrite)]
    public float InfectionChanceBonus = 0.0f;
}

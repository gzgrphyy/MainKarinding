using UnityEngine;

namespace MainKarinding.Crafting
{
    /// <summary>
    /// Jenis bambu Scene 5. Tua = kolot/garing, Tipis = ipis/kopong, Pas = matang padat.
    /// Dipakai CraftingHotspot (metadata), BambooInventory (isi tas), NPCHandoffDropTarget (reaksi).
    /// </summary>
    public enum BambooTag
    {
        Tua = 0,
        Tipis = 1,
        Pas = 2,
    }
}

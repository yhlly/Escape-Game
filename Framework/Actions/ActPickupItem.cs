using System.Reflection;
using UnityEngine;

/// <summary>
/// PuzzleAction: pick up an item and add it to inventory.
/// Uses reflection to reduce coupling to GameManager API names.
/// </summary>
public class ActPickupItem : PuzzleAction
{
    public string itemId = "Key";
    public bool destroyOnPickup = true;

    public override void Execute(GameManager gm)
    {
        if (gm == null)
        {
            Debug.LogError("[ActPickupItem] GameManager is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogWarning("[ActPickupItem] itemId is empty, ignoring.");
            return;
        }

        if (!TryAddItemToGM(gm, itemId))
        {
            Debug.LogError("[ActPickupItem] Could not add item. Please add a method on GameManager like AddItem(string).");
            return;
        }

        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    private bool TryAddItemToGM(GameManager gm, string id)
    {
        var t = gm.GetType();

        // 你项目里常见命名：AddItem / PickupItem / InventoryAdd
        string[] methodNames = { "AddItem", "PickupItem", "InventoryAdd" };

        foreach (var name in methodNames)
        {
            var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) continue;

            var ps = m.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
            {
                m.Invoke(gm, new object[] { id });
                return true;
            }
        }

        return false;
    }
}

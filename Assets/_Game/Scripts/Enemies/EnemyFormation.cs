using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Formation")]
public sealed class EnemyFormation : ScriptableObject
{
    [SerializeField] private Vector2[] slots = new Vector2[0];
    public int Count => slots.Length;
    public Vector2 GetSlot(int index) => slots[index];
}

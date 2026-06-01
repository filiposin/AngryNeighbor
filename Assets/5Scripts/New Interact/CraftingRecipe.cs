using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Результат крафта")]
    public ItemDefinition resultItem; // Топор

    [Header("Необходимые материалы")]
    // Список предметов, которые нужны (например: Лезвие, Палка)
    public List<ItemDefinition> ingredients; 
}
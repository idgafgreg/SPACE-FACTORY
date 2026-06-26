using System;
using UnityEngine;

public class Processor : MachineBase
{
    [Serializable]
    public struct Recipe
    {
        public ResourceTypeId input;
        public int            inputAmount;
        public ResourceTypeId output;
        public int            outputAmount;
        public float          processTime;
    }

    [Header("Processor Config")]
    public Recipe recipe;

    float _timer;
    bool  _processing;

    /// <summary>True while a recipe is actively running.</summary>
    public bool IsProcessing => _processing;

    /// <summary>0–1 fill value. Poll this from a UI script to drive a progress bar.</summary>
    public float Progress => _processing && recipe.processTime > 0f
        ? 1f - (_timer / recipe.processTime)
        : 0f;

    protected override void Tick(float dt)
    {
        var inv = ResourceInventory.Instance;

        if (!_processing)
        {
            if (inv.CanAfford(recipe.input, recipe.inputAmount))
            {
                inv.Spend(recipe.input, recipe.inputAmount);
                _timer      = recipe.processTime;
                _processing = true;
            }
        }
        else
        {
            _timer -= dt;
            if (_timer <= 0f)
            {
                inv.Add(recipe.output, recipe.outputAmount);
                _processing = false;
            }
        }
    }
}

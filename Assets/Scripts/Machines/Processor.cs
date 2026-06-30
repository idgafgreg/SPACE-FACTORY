using System;
using UnityEngine;

public class Processor : MachineBase, IItemReceiver
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

    [Header("Input Buffer")]
    [Tooltip("Max input units the processor can hold waiting to be refined. Fed by a ConveyorBelt — NOT the global stockpile.")]
    public int inputBufferCapacity = 20;

    float _timer;
    bool  _processing;
    int   _inputBuffer;

    /// <summary>True while a recipe is actively running.</summary>
    public bool IsProcessing => _processing;

    /// <summary>Input units currently buffered, waiting to be refined.</summary>
    public int InputBuffer => _inputBuffer;

    /// <summary>0–1 fill value. Poll this from a UI script to drive a progress bar.</summary>
    public float Progress => _processing && recipe.processTime > 0f
        ? 1f - (_timer / recipe.processTime)
        : 0f;

    /// <summary>Belt hand-off: accept the input resource into the buffer if there's room.</summary>
    public bool TryAcceptItem(ResourceTypeId resource)
    {
        if (resource != recipe.input)      return false;
        if (_inputBuffer >= inputBufferCapacity) return false;
        _inputBuffer++;
        return true;
    }

    protected override void Tick(float dt)
    {
        if (!_processing)
        {
            // Pull from the belt-fed input buffer — placement of a feeding belt
            // is what lets this run at all. A processor with no belt does nothing.
            if (_inputBuffer >= recipe.inputAmount)
            {
                _inputBuffer -= recipe.inputAmount;
                _timer        = recipe.processTime;
                _processing   = true;
            }
        }
        else
        {
            _timer -= dt;
            if (_timer <= 0f)
            {
                ResourceInventory.Instance?.Add(recipe.output, recipe.outputAmount);
                _processing = false;
            }
        }
    }
}

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

    [Header("Output Routing (optional — enables multi-stage chains)")]
    [Tooltip("If set (and carrying), refined output is pushed onto this belt toward a downstream machine instead of the global stockpile.")]
    public ConveyorBelt outputBelt;
    [Tooltip("If set (and no belt), refined output is handed directly to this downstream IItemReceiver (e.g. a second Processor). Falls back to the stockpile if it rejects the item.")]
    public MonoBehaviour outputReceiver;

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
                EmitOutput(recipe.output, recipe.outputAmount);
                _processing = false;
            }
        }
    }

    /// <summary>
    /// Routes refined output. Priority: connected belt (toward a downstream
    /// machine) → explicit downstream receiver → the global stockpile. This is
    /// what lets a Processor feed a second Processor and form a real multi-stage
    /// chain (e.g. Scrap → ConstructionParts → AdvancedParts).
    /// </summary>
    void EmitOutput(ResourceTypeId type, int amount)
    {
        if (amount <= 0) return;

        if (outputBelt != null && outputBelt.CanCarry)
        {
            for (int i = 0; i < amount; i++) outputBelt.PushItem(type);
            return;
        }

        if (outputReceiver is IItemReceiver receiver)
        {
            int accepted = 0;
            for (int i = 0; i < amount; i++)
                if (receiver.TryAcceptItem(type)) accepted++;
                else break;

            int overflow = amount - accepted;
            if (overflow > 0) ResourceInventory.Instance?.Add(type, overflow);
            return;
        }

        ResourceInventory.Instance?.Add(type, amount);
    }
}

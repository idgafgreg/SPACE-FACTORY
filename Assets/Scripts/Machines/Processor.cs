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
            // InfectionRateMult < 1 stretches craft time (L17 residue).
            _timer -= dt * InfectionRateMult;
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

        var belt = ResolveOutputBelt();
        if (belt != null)
        {
            int accepted = 0;
            for (int i = 0; i < amount; i++)
                if (belt.TryAcceptItem(type)) accepted++;
                else break;
            int overflow = amount - accepted;
            if (overflow > 0) ResourceInventory.Instance?.Add(type, overflow);
        }
        else if (outputReceiver is IItemReceiver receiver)
        {
            int accepted = 0;
            for (int i = 0; i < amount; i++)
                if (receiver.TryAcceptItem(type)) accepted++;
                else break;

            int overflow = amount - accepted;
            if (overflow > 0) ResourceInventory.Instance?.Add(type, overflow);
        }
        else
        {
            ResourceInventory.Instance?.Add(type, amount);
        }

        OnEmitFx(type, amount);
    }

    ConveyorBelt ResolveOutputBelt()
    {
        if (outputBelt != null && outputBelt.CanCarry) return outputBelt;

        // Player-built processors often leave outputBelt null — find a nearby relay.
        ConveyorBelt best = null;
        float bestScore = float.MaxValue;
        foreach (var col in Physics.OverlapSphere(transform.position, 1.3f))
        {
            var b = col.GetComponentInParent<ConveyorBelt>();
            if (b == null || !b.CanCarry) continue;
            Vector3 intake = b.startPoint != null ? b.startPoint.position : b.transform.position;
            float score = (intake - transform.position).sqrMagnitude;
            if (score < bestScore) { bestScore = score; best = b; }
        }
        return best;
    }

    void OnEmitFx(ResourceTypeId type, int amount)
    {
        Color tint = type switch
        {
            ResourceTypeId.EnergyCells       => new Color(1f, 0.9f, 0.35f),
            ResourceTypeId.CircuitComponents => new Color(0.4f, 0.9f, 1f),
            ResourceTypeId.ConstructionParts => new Color(0.75f, 0.8f, 0.85f),
            _                                => new Color(0.9f, 0.7f, 0.4f),
        };
        ImpactFX.Impact(transform.position + Vector3.up * 0.9f, tint, 0.35f);
        if (amount > 0)
            FloatingText.Spawn(transform.position, $"+{amount}", tint, 0.75f);
    }
}
